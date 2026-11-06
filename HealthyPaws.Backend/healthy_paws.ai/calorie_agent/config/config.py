from dataclasses import dataclass


@dataclass(frozen=True)
class CalorieAgentConfig:
    # --- energetika -------------------------------------------------------
    # Energija po kg promjene tjelesne mase. PROVJERI izvor (literatura daje raspon).
    kcal_per_kg_body_change: float = 7000.0
    rer_coeff: float = 70.0  # RER = 70 * kg^0.75
    rer_exponent: float = 0.75
    mer_factor_neutered: float = 1.6  # PROVJERI
    mer_factor_intact: float = 1.8  # PROVJERI

    # --- šum mjerenja (ručno vaganje na kućnoj vagi) ------------------------
    # PRETPOSTAVKA dok se ne kalibrira: izvaži istog psa 3x zaredom i pogledaj raspon.
    weight_sigma_abs_kg: float = 0.15
    weight_sigma_rel: float = 0.005  # dio težine (veći pas -> veći apsolutni šum)

    # --- validacija ---------------------------------------------------------
    min_period_days: float = 4.0  # kraće od ovoga: ne zaključuj
    max_plausible_change_pct_per_week: float = 4.0
    remeasure_confirm_tolerance_pct: float = 3.0  # "drugo vaganje se slaže s prvim"

    # --- podaci o unosu -----------------------------------------------------
    min_intake_coverage: float = 0.7  # dio perioda s podacima hranilice
    appetite_ratio_threshold: float = 0.7  # pojedeno/ponuđeno ispod ovoga = eskalacija

    # --- procjenitelj potrošnje (Kalman, 1D) -------------------------------
    prior_rel_sd: float = 0.20  # nesigurnost formule (20% od MER)
    process_sd_kcal_per_week: float = 20.0  # koliko potrošnja smije "lutati"
    innovation_z_threshold: float = 3.0  # "loša sedmica" ako je |z| veći
    bad_weeks_before_escalation: int = 3  # odluka 6b (potvrđeno)
    warmup_updates: int = 3  # prvih N ažuriranja: mali koraci
    prior_deviation_max: float = 0.40  # |procjena/formula - 1| > ovo = eskalacija (PROVJERI)

    # --- trend --------------------------------------------------------------
    # 8 tačaka (odluka 4): se nagiba je ~3x manja nego sa 4 tačke, pa se vidi i spor drift (~0.1 kg/sedm.)
    trend_points: int = 8
    stable_slope_pct_per_week: float = 0.5
    stable_sigma: float = 2.5  # "miran" = nagib nije razlučiv od nule na 2.5 sigma
    unexplained_loss_pct_per_week: float = 1.5  # PROVJERI
    rapid_loss_pct_per_week: float = 2.0  # PROVJERI (gubitak na dijeti brži od ovoga)
    # ukupni gubitak u historijskom prozoru (robustan na šum, za razliku od nagiba). PROVJERI
    window_loss_pct: float = 5.0  # pas koji NIJE na dijeti
    window_loss_pct_on_diet: float = 12.0  # pas na dijeti (prozor ~8 sedmica)
    window_min_rows: int = 4  # manje redova: ne zaključuj
    trend_alarm_sigma: float = 2.0  # nagib: alarm ako je pravi gubitak PREKO praga uz ~97.7% pouzdanosti
    window_alarm_sigma: float = 3.0  # prozor: prag ne smije biti ispod 3 sigma šuma razlike prosjeka

    # --- cilj ---------------------------------------------------------------
    target_horizon_weeks: float = 8.0
    max_loss_pct_per_week: float = 1.0  # PROVJERI
    max_gain_pct_per_week: float = 1.0  # PROVJERI

    # --- guardrails ---------------------------------------------------------
    # izmjereno u simulaciji (obese: 22 promjene/12 obrata -> 9/3 uz sličnu konačnu težinu)
    plan_smoothing: float = 0.5  # 1 = bez izglađivanja; 0.5 = pola puta prema prijedlogu
    max_step_pct: float = 10.0
    early_step_pct: float = 5.0
    conflict_step_scale: float = 0.5
    min_change_pct: float = 4.0  # promjena ispod ovoga je ispod šuma mjerenja (20 kcal ~ 0.02 kg/sedm.)
    min_rer_multiple: float = 1.0  # donja granica = RER(ciljne težine)
    max_rer_multiple: float = 3.0  # PROVJERI
    bound_weeks_before_escalation: int = 3

    # --- validacija ulaza (sve PROVJERI: granice zdravog razuma, ne klinički pragovi) --------
    weight_min_kg: float = 0.5  # lakši "pas" je greška unosa (ili nije pas)
    weight_max_kg: float = 120.0
    range_max_ratio: float = 2.5  # MaxWeight/MinWeight; šire od ovoga je tipfeler u rasponu
    weight_vs_range_ratio: float = 2.0  # težina mora biti u [min/2, max*2]; inače pogrešan pas/jedinica
    food_kcal_min: float = 0.5  # mokra hrana ~1, suha ~3-4.5, suhomesnato do ~6
    food_kcal_max: float = 6.0
    days_covered_slack: float = 0.5  # daysCovered smije prelaziti periodDays za najviše ovoliko dana
    intake_over_offered_warn: float = 1.25  # pojedeno > 125% ponuđenog: upozorenje (ne blokira)
    intake_rer_min: float = 0.2  # pojedeno kcal / RER ispod ovoga: nevjerodostojno nisko -> eskalacija
    intake_rer_max: float = 5.0  # iznad ovoga: kvar senzora -> NoChange

    # --- ostalo -------------------------------------------------------------
    next_evaluation_days: float = 7.0
    bcs_low_max: int = 3
    bcs_high_min: int = 6

    activity_factors = [1.2]
    valid_bcs = [1, 3, 5, 7, 9]

DEFAULT_AGENT_CONFIG = CalorieAgentConfig()