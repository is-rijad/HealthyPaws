from collections import defaultdict


class Explanations:
    _TEMPLATES: dict[str, str] = {
        "INITIAL_PLAN": "Početni plan iz formule (RER × faktori) prilagođen stanju psa: {new_kcal:.0f} kcal/dan.",
        "FIRST_MEASUREMENT": "Prvo mjerenje: još nema historije za procjenu, plan ostaje isti.",
        "STALE_MEASUREMENT": "Mjerenje nije novije od zadnjeg obrađenog, preskačem ga.",
        "MEASUREMENT_TOO_SOON": "Od zadnjeg mjerenja prošlo je samo {dt_days:.1f} dana, što je premalo za pouzdan zaključak.",
        "INVALID_MEASUREMENT": "Unesena težina ili BCS nisu valjani ({detail}). Molim ponovite vaganje.",
        "INVALID_INPUT": "Ulazni podaci nisu valjani ({detail}). Potrebna je provjera.",
        "WEIGHT_OUT_OF_RANGE_CONFIRMED": (
            "Ponovljeno vaganje potvrđuje težinu od {weight:.1f} kg, koja nije u skladu sa zadanim rasponom ({detail}). "
            "Provjerite raspon težine (MinWeight/MaxWeight) i podatke o psu."),
        "INTAKE_IMPLAUSIBLE": (
            "Zabilježeni unos hranilice ({eaten_kcal:.0f} kcal/dan) nije vjerodostojan za psa ove veličine. "
            "Provjerite hranilicu i psa; plan ne mijenjam."),
        "NO_INTAKE_DATA": "Hranilica nema dovoljno podataka o unosu za ovaj period, pa ne mijenjam plan.",

        "REMEASURE_IMPLAUSIBLE_CHANGE": (
            "Promjena težine sa {prev_weight:.1f} na {weight:.1f} kg je prevelika za taj razmak. "
            "Molim ponovite vaganje."),
        "RAPID_CHANGE_CONFIRMED": (
            "Ponovljeno vaganje potvrđuje nagli skok težine ({weight:.1f} kg). "
            "Preporučuje se pregled kod veterinara."),
        "CONTRADICTORY_BCS_WEIGHT": "BCS i težina govore suprotno (BCS {bcs}, težina {weight:.1f} kg). Provjerite oba podatka.",
        "REDUCED_APPETITE": (
            "Pas je pojeo samo {eaten_ratio:.0%} ponuđene količine. Plan ne mijenjam; "
            "preporučuje se provjera zdravlja."),
        "BCS_UNCONFIRMED": "BCS ({bcs}) se ne slaže s težinom (u zdravom je rasponu) i nije potvrđen prošlim mjerenjem; čekam potvrdu.",
        "LEARNED_STATE_RESET": "Pohranjeno naučeno stanje je oštećeno i odbačeno; procjena kreće ispočetka iz formule.",
        "UNSUPPORTED_LIFE_STAGE": "Štenad, gravidnost i laktacija nisu podržani u ovoj verziji; potrebna je ručna procjena.",

        "MODEL_ERROR_LARGE": "Stvarna promjena težine se jako razlikuje od očekivane (z={z:.1f}); ovaj period ne koristim za učenje.",
        "MODEL_MISMATCH_PERSISTENT": (
            "Predviđanja se uzastopno ne poklapaju sa stvarnom težinom. Model ne opisuje ovog psa "
            "(moguće hrana mimo hranilice ili zdravstveni problem)."),
        "ESTIMATE_FAR_FROM_PRIOR": (
            "Procijenjena potrošnja ({mer_est:.0f} kcal) jako odstupa od formule ({prior_mer:.0f} kcal). "
            "Preporučuje se provjera."),
        "UNEXPLAINED_WEIGHT_LOSS": "Težina stalno pada ({slope:+.2f} kg/sedmično) iako to nije cilj. Preporučuje se pregled kod veterinara.",
        "RAPID_LOSS_ON_DIET": "Gubitak težine je brži od sigurnog ({slope:+.2f} kg/sedmično). Preporučuje se provjera.",
        "PLAN_AT_BOUND_NO_PROGRESS": (
            "Unos je na sigurnosnoj granici ({new_kcal:.0f} kcal), a težina se ne kreće prema cilju. "
            "Hrana mimo hranilice ili zdravstveni problem? Potrebna je provjera."),

        "TARGET_REACHED": "Pas je stigao u idealno stanje; plan se postavlja na procijenjeno održavanje.",
        "IN_DEAD_ZONE": "Težina je u zdravom rasponu, BCS idealan i trend miran: plan ostaje isti.",
        "WEIGHT_LOSS_TARGET": "Cilj je postepeno smanjenje težine (~{rate:+.2f} kg/sedmično).",
        "WEIGHT_GAIN_TARGET": "Cilj je postepeno povećanje težine (~{rate:+.2f} kg/sedmično).",
        "MAINTENANCE_TARGET": "Cilj je održavanje težine; unos se usklađuje s procijenjenom potrošnjom.",
        "CHANGE_BELOW_THRESHOLD": "Predložena promjena je premala da bi se isplatila; plan ostaje isti.",

        "RATE_LIMIT_APPLIED": "Promjena je ograničena na najviše {step_pct:.0f}% po ciklusu.",
        "WARMUP_STEP_LIMIT": "Agent tek upoznaje psa, pa su koraci manji (najviše {step_pct:.0f}%).",
        "CONFLICT_STEP_LIMIT": "BCS i težina se ne slažu u potpunosti, pa su koraci manji (najviše {step_pct:.0f}%).",
        "FLOOR_APPLIED": "Unos ne smije pasti ispod sigurnosnog minimuma ({floor:.0f} kcal).",
        "CEILING_APPLIED": "Unos ne smije preći sigurnosni maksimum ({ceiling:.0f} kcal).",
        "BOUNDS_OVERRIDE_STEP": "Sigurnosne granice imaju prednost nad ograničenjem koraka.",
        "PLAN_OUTSIDE_SAFE_BOUNDS": "Prethodni plan je bio izvan sigurnosnih granica i vraćen je unutar njih.",
    }

    @classmethod
    def explain(cls, codes: list[str] | tuple[str, ...], ctx: dict) -> str:
        values = defaultdict(lambda: float("nan"), ctx)
        parts: list[str] = []
        for code in codes:
            template = Explanations._TEMPLATES.get(code)
            if template is None:
                parts.append(code)
                continue
            try:
                parts.append(template.format_map(values))
            except (ValueError, KeyError, IndexError):
                parts.append(code)
        return " ".join(parts) if parts else "Bez promjene."