# Satisfaction Metric
## Description
Resident Satisfaction is used to gauge how satisfied a single resident is with their city's public transit. City Satisfaction is used to gauge how satisfied the entire city is with public transit. Both satisfaction metrics are calculated separately from P(BusUse).

## Formulas
```
ResidentSatisfaction = 1 - Disutility
```
- Disutility is the same one that is used for P(BusUse)
- Satisfaction ranges from 0 (very dissatisfied) to 1 (very satisfied)
- If no bus route connects a resident to their destination, Satisfaction is 0

```
CitySatisfaction = Average(ResidentSatisfaction)
```

## Player-Facing Info
Satisfaction shall be displayed in two places:

| Location | Scope | Representation |
|---|---|---|
| Information Panel | The selected resident or neighborhood | Emoji |
| Level UI (persistent, near the budget) | The whole city | Progress Bar |

### Emoji Mapping
| Satisfaction | Emoji | Meaning |
|---|---|---|
| 0.00 – 0.20 | 😠 | Very dissatisfied |
| 0.21 – 0.40 | 🙁 | Dissatisfied |
| 0.41 – 0.60 | 😐 | Neutral |
| 0.61 – 0.80 | 🙂 | Satisfied |
| 0.81 – 1.00 | 😄 | Very satisfied |
Ranges will be tuned during testing.