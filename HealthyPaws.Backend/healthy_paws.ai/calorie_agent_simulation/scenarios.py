"""Scenariji iz odluke 7. Svaki je jedan virtualni pas sa skrivenom istinom."""
from __future__ import annotations

from .harness import Scenario


def obese_dog() -> Scenario:
    return Scenario(
        name="obese", description="Običan debeo pas (12.5 kg, ideal ~10 kg); sporiji metabolizam od formule",
        start_weight_kg=12.5, min_weight_kg=9.0, max_weight_kg=11.0, mer_ratio=0.90, weeks=45, seed=11)


def underweight_dog() -> Scenario:
    return Scenario(
        name="underweight", description="Običan mršav pas (8.0 kg, ideal ~10 kg)",
        start_weight_kg=8.0, min_weight_kg=9.0, max_weight_kg=11.0, mer_ratio=1.05, weeks=40, seed=12)


def ideal_dog() -> Scenario:
    return Scenario(
        name="ideal", description="Pas u idealnoj težini; skrivena potrošnja odgovara formuli",
        start_weight_kg=10.0, min_weight_kg=9.0, max_weight_kg=11.0, mer_ratio=1.0, weeks=40, seed=13)


def secret_treats_dog() -> Scenario:
    return Scenario(
        name="secret_treats", description="Debeo pas koji od sedmice 6 dobija 250 kcal/dan poslastica koje hranilica ne vidi",
        start_weight_kg=12.5, min_weight_kg=9.0, max_weight_kg=11.0, mer_ratio=0.90, weeks=45, seed=14,
        events={6: {"hidden_treats_kcal": 250.0}})


def sick_dog() -> Scenario:
    return Scenario(
        name="sick", description="Pas u idealnoj težini koji od sedmice 8 gubi težinu uz normalan apetit (skriveni odliv energije)",
        start_weight_kg=10.0, min_weight_kg=9.0, max_weight_kg=11.0, mer_ratio=1.0, weeks=30, seed=15,
        events={8: {"drain_kcal": 300.0}})


def slow_drift_dog() -> Scenario:
    # 110 kcal/dan * 7 / 7700 ~ 0.1 kg/sedm. gubitka koji se vidi tek preko više tačaka
    return Scenario(
        name="slow_drift", description="Pas u idealnoj težini koji od sedmice 8 polako gubi težinu (~0.1 kg/sedm., skriveni odliv 110 kcal/dan)",
        start_weight_kg=10.0, min_weight_kg=9.0, max_weight_kg=11.0, mer_ratio=1.0, weeks=40, seed=19,
        events={8: {"drain_kcal": 110.0}})


def poor_appetite_dog() -> Scenario:
    return Scenario(
        name="poor_appetite", description="Pas koji od sedmice 8 jede samo 50% ponuđene hrane",
        start_weight_kg=10.0, min_weight_kg=9.0, max_weight_kg=11.0, mer_ratio=1.0, weeks=24, seed=16,
        events={8: {"appetite": 0.5}})


def owner_typo_dog() -> Scenario:
    return Scenario(
        name="owner_typo", description="Debeo pas; vlasnik u sedmicama 10 i 20 unese pogrešnu težinu, a u 15 i 25 preskoči vaganje",
        start_weight_kg=12.5, min_weight_kg=9.0, max_weight_kg=11.0, mer_ratio=0.90, weeks=40, seed=17,
        typos={10: 21.4, 20: 1.4}, missed_weeks={15, 25})


def noisy_scale_dog() -> Scenario:
    return Scenario(
        name="noisy_scale", description="Debeo pas, jako šumna vaga (σ=0.35 kg) i netačan BCS u 20% slučajeva",
        start_weight_kg=12.5, min_weight_kg=9.0, max_weight_kg=11.0, mer_ratio=0.90, weeks=45, seed=18,
        weight_noise_kg=0.35, bcs_flip_prob=0.2)


ALL = {
    "obese": obese_dog, "underweight": underweight_dog, "ideal": ideal_dog,
    "secret_treats": secret_treats_dog, "sick": sick_dog, "slow_drift": slow_drift_dog, "poor_appetite": poor_appetite_dog,
    "owner_typo": owner_typo_dog, "noisy_scale": noisy_scale_dog,
}
