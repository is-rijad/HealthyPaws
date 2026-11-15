FEATURE_DIM = 8              # [bias, hoursSince, avgEaten, treatShare (sve centrirano), mealIndex==0, mealIndex==1, mealIndex==2, mealIndex==3]
MAX_OFFSET_MIN = 60          # guardrail: pomak od osnovnog termina je najviše ±60 min
MAX_ARMS = 5
HOURS_SCALE = 12.0           # hoursSinceLastMeal / 12, ograničeno na [0,1]
TREAT_SHARE_SCALE = 0.2      # treatBudgetShare / 0.2, ograničeno na [0,1] (poslastice su preporučeno <= 10-20% budžeta)
MAX_MEAL_INDEX = 3