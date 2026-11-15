import numpy as np
import pytest

from meal_scheduler.constants import FEATURE_DIM
from meal_scheduler.data.arm_state import ArmState, get_or_create_arm
from meal_scheduler.data.lin_ucb_config import LinUCBConfig
from meal_scheduler.data.lin_ucb_state import LinUCBState, check_x
from meal_scheduler.lin_ucb import LinUcb

D = 3
X = [1.0, 0.5, -0.5]


def fresh(d=D) -> LinUCBState:
    return LinUCBState.new(d)


class TestState:
    def test_new_state_is_empty(self):
        s = fresh()
        assert (s.d, s.version, s.arms) == (D, 0, {})

    def test_new_arm_is_lambda_identity_and_zero_b(self):
        cfg = LinUCBConfig(lam=2.5)
        arm = get_or_create_arm(fresh(), 10, cfg)
        np.testing.assert_allclose(arm.A, 2.5 * np.eye(D))
        np.testing.assert_array_equal(arm.b, np.zeros(D))
        assert arm.n == 0

    def test_get_or_create_does_not_mutate_state(self):
        s = fresh()
        get_or_create_arm(s, 10, LinUCBConfig())
        assert s.arms == {}

    def test_get_or_create_handles_arms_none(self):
        arm = get_or_create_arm(LinUCBState(d=D, arms=None), 0, LinUCBConfig())
        assert arm.n == 0

    def test_get_or_create_returns_existing(self):
        existing = ArmState(A=np.eye(D) * 3, b=np.ones(D), n=7)
        s = LinUCBState(d=D, arms={5: existing})
        assert get_or_create_arm(s, 5, LinUCBConfig()) is existing

    @pytest.mark.parametrize("x", [[1, 2], [1, 2, 3, 4], [[1, 2, 3]]])
    def test_check_x_rejects_wrong_shape(self, x):
        with pytest.raises(ValueError, match="dimenziju"):
            check_x(fresh(), x)

    @pytest.mark.parametrize("bad", [float("nan"), float("inf"), float("-inf")])
    def test_check_x_rejects_non_finite(self, bad):
        with pytest.raises(ValueError, match="NaN"):
            check_x(fresh(), [1.0, bad, 0.0])

    def test_check_x_returns_float_array(self):
        v = check_x(fresh(), [1, 2, 3])
        assert v.dtype == float and v.shape == (D,)


class TestScores:
    def test_untrained_arm_score_is_pure_exploration_bonus(self):
        cfg = LinUCBConfig(alpha=0.5, lam=1.0)
        sc = LinUcb.scores(fresh(), X, [0], cfg)
        # A = I, b = 0  ->  score = alpha * sqrt(x'x)
        assert sc[0] == pytest.approx(0.5 * np.sqrt(np.dot(X, X)))

    def test_alpha_zero_gives_zero_for_untrained_arm(self):
        sc = LinUcb.scores(fresh(), X, [0, 5], LinUCBConfig(alpha=0.0))
        assert sc == {0: 0.0, 5: 0.0}

    def test_score_matches_closed_form(self):
        cfg = LinUCBConfig(alpha=0.7, lam=1.0)
        s = LinUcb.update(fresh(), 0, X, 0.8, cfg)
        arm = s.arms[0]
        expected_theta = np.linalg.inv(arm.A) @ arm.b
        xv = np.array(X)
        expected = expected_theta @ xv + 0.7 * np.sqrt(xv @ np.linalg.inv(arm.A) @ xv)
        assert LinUcb.scores(s, X, [0], cfg)[0] == pytest.approx(expected)

    def test_scores_do_not_mutate_state(self):
        s = fresh()
        LinUcb.scores(s, X, [0, 5, -5])
        assert s.arms == {} and s.version == 0

    def test_returns_score_per_requested_arm_only(self):
        s = LinUcb.update(fresh(), 10, X, 1.0)
        assert set(LinUcb.scores(s, X, [0, 5])) == {0, 5}

    def test_exploration_bonus_shrinks_with_observations(self):
        cfg = LinUCBConfig(alpha=1.0)
        s, bonuses = fresh(), []
        for _ in range(5):
            bonuses.append(LinUcb.scores(s, X, [0], cfg)[0])  # reward 0 -> theta = 0 -> samo bonus
            s = LinUcb.update(s, 0, X, 0.0, cfg)
        assert all(a > b for a, b in zip(bonuses, bonuses[1:]))

    def test_learns_high_reward_context(self):
        cfg = LinUCBConfig(alpha=0.1)
        s = fresh()
        for _ in range(30):
            s = LinUcb.update(s, 0, X, 1.0, cfg)
            s = LinUcb.update(s, 5, X, 0.0, cfg)
        sc = LinUcb.scores(s, X, [0, 5], cfg)
        assert sc[0] > sc[5]
        assert sc[0] == pytest.approx(1.0, abs=0.1)

    def test_rejects_bad_context(self):
        with pytest.raises(ValueError):
            LinUcb.scores(fresh(), [1.0], [0])

    def test_negative_variance_from_rounding_is_clamped(self):
        # nearly-singular but valid A: bonus must never be NaN
        arm = ArmState(A=np.full((D, D), 1e-18) + 1e-18 * np.eye(D), b=np.zeros(D), n=1)
        s = LinUCBState(d=D, arms={0: arm})
        assert np.isfinite(LinUcb.scores(s, [1e-9, 0, 0], [0])[0])


class TestSelect:
    def test_empty_arms_raises(self):
        with pytest.raises(ValueError, match="prazna"):
            LinUcb.select(fresh(), X, [])

    def test_duplicate_arms_raises(self):
        with pytest.raises(ValueError, match="jedinstveni"):
            LinUcb.select(fresh(), X, [0, 0])

    def test_single_arm_is_selected(self):
        arm, sc = LinUcb.select(fresh(), X, [15])
        assert arm == 15 and set(sc) == {15}

    def test_ties_resolve_to_smallest_absolute_offset(self):
        # svi armovi netrenirani -> isti score -> bira se najmanji |pomak|
        arm, _ = LinUcb.select(fresh(), X, [-30, 30, 15, -10, 20])
        assert arm == -10

    def test_equal_abs_tie_prefers_negative(self):
        # key = (abs, a): -15 < 15
        arm, _ = LinUcb.select(fresh(), X, [15, -15])
        assert arm == -15

    def test_zero_offset_wins_ties(self):
        arm, _ = LinUcb.select(fresh(), X, [30, 0, -30])
        assert arm == 0

    def test_selection_is_independent_of_arm_order(self):
        arms = [-30, -15, 0, 15, 30]
        picks = {LinUcb.select(fresh(), X, list(p))[0] for p in (arms, arms[::-1], [0, 30, -15, 15, -30])}
        assert len(picks) == 1

    def test_picks_trained_best_arm(self):
        cfg = LinUCBConfig(alpha=0.05)
        s = fresh()
        for _ in range(25):
            s = LinUcb.update(s, 15, X, 0.9, cfg)
            s = LinUcb.update(s, 0, X, 0.1, cfg)
            s = LinUcb.update(s, -15, X, 0.2, cfg)
        arm, sc = LinUcb.select(s, X, [-15, 0, 15], cfg)
        assert arm == 15 and sc[15] == max(sc.values())

    def test_exploration_tries_unseen_arm_with_high_alpha(self):
        cfg = LinUCBConfig(alpha=3.0)
        s = LinUcb.update(fresh(), 0, X, 0.5, cfg)
        arm, _ = LinUcb.select(s, X, [0, 30], cfg)
        assert arm == 30

    def test_contextual_policy_differs_per_context(self):
        """Prava kontekstualna svojstva: arm A najbolji u kontekstu 1, arm B u kontekstu 2."""
        cfg = LinUCBConfig(alpha=0.05)
        ctx1, ctx2 = [1.0, 1.0, 0.0], [1.0, -1.0, 0.0]
        s = fresh()
        for _ in range(40):
            s = LinUcb.update(s, 0, ctx1, 1.0, cfg)
            s = LinUcb.update(s, 0, ctx2, 0.0, cfg)
            s = LinUcb.update(s, 30, ctx1, 0.0, cfg)
            s = LinUcb.update(s, 30, ctx2, 1.0, cfg)
        assert LinUcb.select(s, ctx1, [0, 30], cfg)[0] == 0
        assert LinUcb.select(s, ctx2, [0, 30], cfg)[0] == 30

    def test_regret_converges_on_simulated_bandit(self):
        """Simulacija: arm 15 ima najveću očekivanu nagradu; kasni izbori se moraju koncentrirati na njega."""
        rng = np.random.default_rng(42)
        cfg = LinUCBConfig(alpha=1.0)  # alpha mora premašiti razmak nagrada, inače greedy zaglavi na armu 0
        true_theta = {-15: np.array([0.2, 0, 0]), 0: np.array([0.4, 0, 0]), 15: np.array([0.8, 0, 0])}
        s, picks = fresh(), []
        for _ in range(300):
            x = [1.0, rng.uniform(-.5, .5), rng.uniform(-.5, .5)]
            arm, _ = LinUcb.select(s, x, list(true_theta), cfg)
            reward = float(true_theta[arm] @ np.array(x) + rng.normal(0, 0.05))
            s = LinUcb.update(s, arm, x, reward, cfg)
            picks.append(arm)
        assert picks[-100:].count(15) >= 90


class TestUpdate:
    def test_update_increments_version_and_arm_count(self):
        s = LinUcb.update(fresh(), 0, X, 0.5)
        assert s.version == 1 and s.arms[0].n == 1
        s = LinUcb.update(s, 0, X, 0.5)
        assert s.version == 2 and s.arms[0].n == 2

    def test_update_is_pure(self):
        s0 = fresh()
        s1 = LinUcb.update(s0, 0, X, 0.5)
        assert s0.arms == {} and s0.version == 0
        s2 = LinUcb.update(s1, 5, X, 0.5)
        assert set(s1.arms) == {0} and set(s2.arms) == {0, 5}

    def test_update_only_touches_chosen_arm(self):
        s = LinUcb.update(fresh(), 0, X, 0.5)
        s2 = LinUcb.update(s, 5, X, 1.0)
        assert s2.arms[0] is s.arms[0]

    def test_rank_one_update_formula(self):
        cfg = LinUCBConfig(lam=1.0, gamma=1.0)
        s = LinUcb.update(fresh(), 0, X, 0.7, cfg)
        xv = np.array(X)
        np.testing.assert_allclose(s.arms[0].A, np.eye(D) + np.outer(xv, xv))
        np.testing.assert_allclose(s.arms[0].b, 0.7 * xv)

    def test_updates_accumulate(self):
        s = fresh()
        for r in (0.2, 0.4):
            s = LinUcb.update(s, 0, X, r)
        xv = np.array(X)
        np.testing.assert_allclose(s.arms[0].A, np.eye(D) + 2 * np.outer(xv, xv))
        np.testing.assert_allclose(s.arms[0].b, 0.6 * xv)

    def test_A_stays_symmetric_positive_definite(self):
        rng = np.random.default_rng(0)
        s = fresh()
        for _ in range(50):
            s = LinUcb.update(s, 0, rng.normal(size=D), rng.uniform())
        A = s.arms[0].A
        np.testing.assert_allclose(A, A.T)
        assert np.all(np.linalg.eigvalsh(A) > 0)

    @pytest.mark.parametrize("bad", [float("nan"), float("inf"), float("-inf")])
    def test_non_finite_reward_rejected(self, bad):
        with pytest.raises(ValueError, match="reward"):
            LinUcb.update(fresh(), 0, X, bad)

    def test_bad_context_rejected_before_state_change(self):
        s = fresh()
        with pytest.raises(ValueError):
            LinUcb.update(s, 0, [1.0, 2.0], 0.5)
        assert s.arms == {}

    def test_zero_context_leaves_A_and_b_unchanged(self):
        s = LinUcb.update(fresh(), 0, [0.0, 0.0, 0.0], 1.0)
        np.testing.assert_allclose(s.arms[0].A, np.eye(D))
        np.testing.assert_allclose(s.arms[0].b, np.zeros(D))

    def test_discount_gamma_forgets_old_observations(self):
        cfg = LinUCBConfig(lam=1.0, gamma=0.5)
        s = LinUcb.update(fresh(), 0, X, 1.0, cfg)
        s = LinUcb.update(s, 0, [0.0, 0.0, 0.0], 0.0, cfg)
        # nulti kontekst: A = g*A_prev + (1-g)*lam*I  -> vraća se prema lam*I
        xv = np.array(X)
        A1 = 0.5 * np.eye(D) + 0.5 * np.eye(D) + np.outer(xv, xv)
        np.testing.assert_allclose(s.arms[0].A, 0.5 * A1 + 0.5 * np.eye(D))
        np.testing.assert_allclose(s.arms[0].b, 0.5 * xv)

    def test_gamma_one_equals_no_discount(self):
        a = LinUcb.update(fresh(), 0, X, 0.3, LinUCBConfig(gamma=1.0))
        np.testing.assert_allclose(a.arms[0].A, np.eye(D) + np.outer(X, X))

    def test_gamma_keeps_A_bounded_under_repeated_updates(self):
        cfg = LinUCBConfig(gamma=0.9)
        s = fresh()
        for _ in range(500):
            s = LinUcb.update(s, 0, X, 0.5, cfg)
        assert np.all(np.isfinite(s.arms[0].A))
        assert np.linalg.norm(s.arms[0].A) < 1e3  # bez gamma bi rastao linearno s n

    def test_works_with_production_feature_dim(self):
        s = LinUCBState.new(FEATURE_DIM)
        x = np.linspace(-0.5, 0.5, FEATURE_DIM)
        s = LinUcb.update(s, 15, x, 0.9)
        arm, sc = LinUcb.select(s, x, [0, 15])
        assert set(sc) == {0, 15} and arm in (0, 15)
