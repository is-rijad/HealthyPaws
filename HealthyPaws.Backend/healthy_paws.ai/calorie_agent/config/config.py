"""
Oznake porijekla svakog parametra:
  [LIT]   potvrđeno literaturom (izvor u zagradi)
  [LIT~]  literatura daje raspon/orijentir; vrijednost je izbor unutar njega
  [ODLUKA] dizajnerska odluka, nema literaturnog izvora (namjerno)
  [SIM]   izmjereno/odabrano simulacijom (`python -m calorie_agent_simulation.sensitivity`)
  [KAL]   kalibrirati na stvarnim podacima (vaga, hranilica, korisnici)

Izvori:
  [PNA]   Pet Nutrition Alliance, "Calculating Calories Based on Pet Needs" (RER = 70*kg^0.75)
  [MSD]   Merck/MSD Vet Manual, "Daily Maintenance Energy Requirements for Dogs and Cats"
  [HILL]  Hill's, "Estimated Energy Requirements" (canine/feline DER faktori)
  [AAHA14] 2014 AAHA Weight Management Guidelines for Dogs and Cats (J Am Anim Hosp Assoc 50)
  [AAHA21] 2021 AAHA Nutrition and Weight Management Guidelines
  [LIND13] Linder et al., BMC Vet Res 2013, 9:219 (unos pri gubitku: 62 (44-74) kcal/kg^0.75 ciljne tež.)
  [BCOHORT] Cohort study, PMC4597434 (median 0.6 %/sedm., raspon 0.2-1.4; unos 61 (44-74))
  [CWL]   "Performance of a canine weightloss program in clinical practice" (7.920 kcal/kg adipoznog
          tkiva; post-gubitak MER medijan 86 (68-123) kcal/kg^0.75)
  [SACN5] Small Animal Clinical Nutrition, 5. izd. (7.920 kcal/kg adipoznog tkiva)
  [TVP]   Today's Veterinary Practice, "Effectively communicating with clients about pet obesity"
"""
from dataclasses import dataclass


@dataclass(frozen=True)
class CalorieAgentConfig:
    # --- energetika -------------------------------------------------------
    # [LIT~] Adipozno tkivo: 7.920 kcal/kg [CWL][SACN5] (85% masti 9.1 kcal/g + 15% nemasno).
    # Ali stvarni gubitak mase uključuje i nemasno tkivo (~75% vode, energetski "jeftino"):
    # pas 17.2 -> 11.8 kg = -3.7 kg masti, -1.7 kg nemasnog => ~6.600 kcal/kg
    # (proračun sa 9.1 kcal/g za mast i PRETPOSTAVLJENIH ~1.1 kcal/g za nemasno; jedan primjer).
    # 7000 leži između 6.600 (mješoviti gubitak) i 7.920 (čista mast). Realna nesigurnost ~±15%.
    # Brzi gubitak => više nemasnog tkiva => niža efektivna vrijednost.
    kcal_per_kg_body_change: float = 7000.0
    rer_coeff: float = 70.0  # [LIT] [PNA][AAHA21]; RER = 70*kg^0.75, namijenjeno psima ~2-45 kg
    rer_exponent: float = 0.75  # [LIT]
    # [LIT] 1.6 kastriran / 1.8 intaktan [MSD][HILL]. (obesity-prone 1.4, weight loss 1.0 [MSD][HILL].)
    # Samo POČETNA procjena [MSD]. Nakon gubitka težine medijan MER = 86 (68-123) kcal/kg^0.75
    # = ~1.23x RER (raspon 0.97-1.76) [CWL] => formula 1.6 može precijeniti ~25% => zato Kalman.
    mer_factor_neutered: float = 1.6
    mer_factor_intact: float = 1.8

    # --- šum mjerenja (ručno vaganje na kućnoj vagi) ------------------------
    # [KAL] Nema literature: izvaži istog psa 3x zaredom i pogledaj raspon.
    weight_sigma_abs_kg: float = 0.15
    weight_sigma_rel: float = 0.005  # [KAL] dio težine (veći pas -> veći apsolutni šum)
    # [ODLUKA] Šum vage se dodatno procjenjuje iz historije (rezidual od linearne interpolacije susjeda) i koristi
    # ako je VEĆI od konfiguriranog. Bez toga agent sa vagom 2.3x šumnijom od pretpostavke (sigma 0.35 naspram 0.15)
    # odbacuje većinu sedmica kao "loše" i ostaje na formuli. Simulacija noisy_scale, 20 seedova: raspon konačne
    # težine 7.05-14.39 kg (pas se udebljao sa 12.5 na 14.4) i 5.1 eskalacija/psu -> 9.18-12.73 kg i 1.1/psu.
    # Fiksno povećanje sigma je lošije: usporava agenta na normalnim vagama (obese: 11/20 izvan raspona umjesto 1/20).
    noise_min_points: int = 6  # najmanje tačaka (uključujući trenutnu); sa 5 (3 reziduala) je procjena nepouzdana
    noise_max_scale: float = 4.0  # procjena ne smije preći ovoliko puta konfiguriran šum

    # --- validacija ---------------------------------------------------------
    min_period_days: float = 4.0  # [ODLUKA] kraće od ovoga: ne zaključuj
    # [ODLUKA] 4%/sedm. je iznad fiziološki mogućeg gubitka (literatura: 1-2%/sedm. [AAHA14]) => vjerovatno greška vage
    max_plausible_change_pct_per_week: float = 4.0
    remeasure_confirm_tolerance_pct: float = 3.0  # [KAL] "drugo vaganje se slaže s prvim"

    # --- podaci o unosu -----------------------------------------------------
    min_intake_coverage: float = 0.7  # [ODLUKA] dio perioda s podacima hranilice
    appetite_ratio_threshold: float = 0.7  # [ODLUKA] pojedeno/ponuđeno ispod ovoga = eskalacija

    # --- procjenitelj potrošnje (Kalman, 1D) -------------------------------
    # [LIT~] Međuindividualni SD unosa pri gubitku = 63 ± 10.2 kcal/kg^0.75 [AAHA21] => ~16%.
    # 20% je blago šire od toga: razuman prior.
    prior_rel_sd: float = 0.20
    # [SIM][KAL] koliko potrošnja smije "lutati". Sweep 5-60 (calorie_agent_simulation.sensitivity, 8 seedova):
    # 5 sporije uči skrivene poslastice (secret_treats kraj 12.49 kg naspram 11.72 kg); 40-60 daje više promjena
    # plana i obrata (obese 7.5-8.4 promjena / 1.4-2.0 obrata naspram 6.1 / 0.9) te 5-10 eskalacija/psu na
    # secret_treats. 20 je kompromis, ne dokazani optimum; fino podešavanje tek na stvarnim podacima [KAL].
    process_sd_kcal_per_week: float = 20.0
    innovation_z_threshold: float = 3.0  # [ODLUKA] "loša sedmica" ako je |z| veći
    bad_weeks_before_escalation: int = 3  # [ODLUKA] 6b (potvrđeno)
    warmup_updates: int = 3  # [ODLUKA] prvih N ažuriranja: mali koraci
    # [ODLUKA] Raspon u literaturi: unos pri gubitku 44-74 kcal/kg^0.75 oko 61-62 (-29%/+19%) [LIND13];
    # post-gubitak MER 0.97-1.76 RER naspram 1.6 => -39%/+10% [CWL]. 0.40 pokriva uglavnom cijeli opaženi raspon.
    prior_deviation_max: float = 0.40

    # --- trend --------------------------------------------------------------
    # 8 tačaka (odluka 4): se nagiba je ~3x manja nego sa 4 tačke, pa se vidi i spor drift (~0.1 kg/sedm.)
    # [ODLUKA] Analitički: SE nagiba pada s brojem tačaka. Simulacija to NE potvrđuje za detekciju: spori drift
    # (~0.1 kg/sedm.) se otkriva ~7.2 sedmice nakon početka za 4, 6, 8 i 12 tačaka (hvata ga prozor, ne nagib).
    # Jedini mjerljiv efekat: noisy_scale MER greška 13.6% (4) -> 6.5% (8) -> 7.3% (12); iznad 8 nema dobiti.
    trend_points: int = 8
    stable_slope_pct_per_week: float = 0.5  # [ODLUKA]
    stable_sigma: float = 2.5  # [ODLUKA] "miran" = nagib nije razlučiv od nule na 2.5 sigma
    # [ODLUKA] Nema literaturnog praga za psa BEZ dijete. (Pas na dijeti: 1-2%/sedm. je cilj [AAHA14].)
    unexplained_loss_pct_per_week: float = 1.5
    # [LIT] Gornja granica željenog tempa gubitka = 2%/sedm. [AAHA14][TVP]
    rapid_loss_pct_per_week: float = 2.0
    # ukupni gubitak u historijskom prozoru (robustan na šum, za razliku od nagiba).
    window_loss_pct: float = 5.0  # [ODLUKA] pas koji NIJE na dijeti
    # [ODLUKA] ~8 sedmica x 1.5%/sedm. ~ 12% => prag malo iznad očekivanog gubitka na dijeti
    window_loss_pct_on_diet: float = 12.0
    window_min_rows: int = 4  # [ODLUKA] manje redova: ne zaključuj
    trend_alarm_sigma: float = 2.0  # [ODLUKA] nagib: alarm ako je pravi gubitak PREKO praga uz ~97.7% pouzdanosti
    window_alarm_sigma: float = 3.0  # [ODLUKA] prozor: prag ne smije biti ispod 3 sigma šuma razlike prosjeka

    # --- cilj ---------------------------------------------------------------
    target_horizon_weeks: float = 8.0  # [ODLUKA]
    # [LIT~] Željeni tempo gubitka za pse 1-2%/sedm. [AAHA14]; 1.0 = donja granica (konzervativno).
    # Sporije (0.25-0.5%) kod sistemskih bolesti [TVP]; stvarni medijan u praksi 0.6% [BCOHORT].
    max_loss_pct_per_week: float = 1.0
    # [ODLUKA][SIM] Literatura NE daje stopu dobitka u %/sedm. za odrasle pse: ASPCA Pro protokol za mršave pse
    # (aspcapro.org, "Feeding Protocol & Nutrition Best Practices for Emaciated Dogs") određuje energiju, ne stopu:
    # 7 dana na DER trenutne težine, pa DER idealne težine (dan 14), uz nadzor refeeding sindroma. "1-2%/sedm."
    # s blogova nije potvrđeno primarnim izvorom. Zato vrijednost proizlazi iz simulacije (20 seedova):
    # underweight (8 kg, cilj 9-11): 1.0 => medijan 33 sedmice do raspona, 5/20 ga nikad ne dosegne u 40 sedmica;
    # 1.5 => medijan 19, svi dosegnu; 2.0 => medijan 13. Bez prekoračenja gornje granice, a ostali scenariji
    # su nepromijenjeni (parametar djeluje samo na ispodnormalnu težinu). Najveći tjedni dobitak: 1.5 => 1.8%.
    # Jako mršav pas (6.5 kg, BCS 1) i dalje treba veterinara: i sa 2.0 ga 9/20 ne dosegne raspon. 1.5 je
    # umjereno povećanje; 2.0 je moguće tek uz nadzor refeeding sindroma.
    max_gain_pct_per_week: float = 1.5

    # --- guardrails ---------------------------------------------------------
    # izmjereno u simulaciji (obese: 22 promjene/12 obrata -> 9/3 uz sličnu konačnu težinu)
    # [SIM] 1 = bez izglađivanja; 0.5 = pola puta prema prijedlogu. Sweep (8 seedova): obese 13.9 promjena /
    # 7.9 obrata (1.0) -> 6.1 / 0.9 (0.5), uz sličnu konačnu težinu (10.79 -> 10.89 kg). 0.25 je presporo
    # (obese kraj ~11.9 kg). Vrijedi za agent v. ovog repozitorija; ponoviti nakon izmjene regulatora.
    plan_smoothing: float = 0.5
    # [LIT~] Korekcije kalorija u koracima 5-10% [VPN/AAHA]; drugi izvor 5-20% [TVP]. Ovo je unutar raspona.
    max_step_pct: float = 10.0
    early_step_pct: float = 5.0
    conflict_step_scale: float = 0.5  # [ODLUKA]
    estimated_food_step_scale: float = 0.5  # [ODLUKA] kcal hrane procijenjen iz sastava (±15%): manji koraci
    # [SIM] promjena ispod ovoga je ispod šuma mjerenja (20 kcal ~ 0.02 kg/sedm.). Sweep (8 seedova): 0% =>
    # obese 30 promjena / 11.8 obrata; 4% => 6.1 / 0.9; 8% => pas presporo reagira (obese kraj 11.81 kg,
    # secret_treats 12.69 kg, izvan raspona 9-11).
    min_change_pct: float = 4.0
    # [ODLUKA] Namjerna sigurnosna granica = 1.0 x RER(ciljne težine) [HILL: weight loss = 1.0 x RER].
    # NAPOMENA: stvarni unosi uspješnih programa su u prosjeku NIŽI: 61-63 kcal/kg^0.75 = ~0.9 x RER,
    # raspon 0.63-1.06 x RER [LIND13][BCOHORT]. Prag 1.0 blokira donji dio tog raspona (svjesna konzervativnost).
    min_rer_multiple: float = 1.0
    # [ODLUKA] Literatura nema gornju granicu za kućne pse; za rad [HILL]: lagani 2x, umjereni 3x, teški 4-8x RER.
    max_rer_multiple: float = 3.0
    bound_weeks_before_escalation: int = 3  # [ODLUKA]

    # --- validacija ulaza (granice zdravog razuma, ne klinički pragovi) -----
    weight_min_kg: float = 0.5  # [ODLUKA] lakši "pas" je greška unosa (ili nije pas)
    weight_max_kg: float = 120.0  # [ODLUKA] (RER formula validirana ~2-45 kg [PNA]; iznad toga je ekstrapolacija)
    range_max_ratio: float = 2.5  # [ODLUKA] MaxWeight/MinWeight; šire od ovoga je tipfeler u rasponu
    weight_vs_range_ratio: float = 2.0  # [ODLUKA] težina mora biti u [min/2, max*2]; inače pogrešan pas/jedinica
    # [ODLUKA] mokra hrana ~1, suha ~3-4.5, suhomesnato do ~6; dijetne hrane tipično < ~3.3-3.4 kcal/g
    food_kcal_min: float = 0.5
    food_kcal_max: float = 6.0
    days_covered_slack: float = 0.5  # [ODLUKA] daysCovered smije prelaziti periodDays za najviše ovoliko dana
    intake_over_offered_warn: float = 1.25  # [KAL] pojedeno > 125% ponuđenog: upozorenje (ne blokira)
    intake_rer_min: float = 0.2  # [ODLUKA] pojedeno kcal / RER ispod ovoga: nevjerodostojno nisko -> eskalacija
    intake_rer_max: float = 5.0  # [ODLUKA] iznad ovoga: kvar senzora -> NoChange

    # --- ostalo -------------------------------------------------------------
    # [LIT~] Praćenje svake 2 sedmice dok se ne uspostavi tempo gubitka [AAHA14]; 7 dana je češće.
    next_evaluation_days: float = 7.0
    # [LIT~] Idealan BCS je 4-5/9 [TVP]; 6-7 = prekomjerna težina. bcs_high_min=7 propušta BCS 6 (blagi višak).
    bcs_low_max: int = 3
    bcs_high_min: int = 7

    # [ODLUKA] Standardne 5-stepene skale aktivnosti u literaturi nema.
    activity_factors = [1.2, 1.4, 1.6, 1.8, 2.5]
    valid_bcs = [1, 3, 5, 7, 9]

DEFAULT_AGENT_CONFIG = CalorieAgentConfig()