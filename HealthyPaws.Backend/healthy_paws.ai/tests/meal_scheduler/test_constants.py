import numpy as np

from meal_scheduler.constants import FEATURE_DIM
from meal_scheduler.parsers.features import build_features


def test_feature_dim_matches_build_features():
    """Regresija: ugovor stanja (state.d) i vektora konteksta moraju imati istu dimenziju."""
    x = build_features({"mealIndex": 0})
    assert isinstance(x, np.ndarray) and x.shape == (FEATURE_DIM,)
