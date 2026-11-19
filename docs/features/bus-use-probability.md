## P(BusUse)
```
WalkPenalty    = MilesWalked^k / (MilesWalked^k + HalfPenaltyMiles^k)

TimeDifference = max(0, BusMinutes − CarMinutes)
Tolerance      = BaseToleranceMinutes + ToleranceFraction × CarMinutes
TimePenalty    = TimeDifference^k / (TimeDifference^k + Tolerance^k)

BusStopAmenityPenalty = BaseAmenityPenalty × (1 − BenchRelief × hasBench − RoofRelief × hasRoof)
AmenityPenalty        = average of BusStopAmenityPenalty over the commute's boarding stops

Disutility     = WalkWeight × WalkPenalty
               + TimeWeight × TimePenalty
               + AmenityWeight × AmenityPenalty
P(BusUse)      = 1 / (1 + e^(GrowthRate × (Disutility − Midpoint)))
```

A **boarding stop** is any stop where the resident gets on a bus during the commute. A commute with transfers has one boarding stop per bus taken, not just the first.

## Inputs
| Variable | Description |
|---|---|
| **MilesWalked** | Total miles the resident walks on the bus route (to the stop, between transfers, and from the final stop to the destination). |
| **BusMinutes** | Door-to-door duration of the closest full bus trip, including walking, waiting and riding. |
| **CarMinutes** | Duration of the car route from the resident's house to the nearest destination. |
| **hasBench** | 1 if a boarding stop has a bench, otherwise 0. Evaluated per boarding stop. |
| **hasRoof** | 1 if a boarding stop has a roof or shelter, otherwise 0. Evaluated per boarding stop. |

## Given values (constants) 
These values should be set by the developers and are to be tweaked manually.

| Constant | Suggested starting value | Description |
|---|---|---|
| **HalfPenaltyMiles** | 0.5 | Miles walked at which `WalkPenalty` is 0.5. Lower means residents dislike walking more. |
| **BaseToleranceMinutes** | 5 | Extra minutes a resident tolerates on even a very short trip. This is the baseline for `Tolerance`. |
| **ToleranceFraction** | 0.5 | How much extra tolerance each minute of car trip adds. At 0.5, a 30-minute drive adds 15 minutes of tolerance. Set it to 0 and tolerance no longer depends on trip length. |
| **BaseAmenityPenalty** | 1.0 | Amenity penalty of a boarding stop with no bench and no roof. Keep it between 0 and 1. |
| **BenchRelief** | 0.3 | Fraction of `BaseAmenityPenalty` removed by a bench. |
| **RoofRelief** | 0.4 | Fraction of `BaseAmenityPenalty` removed by a roof. `BenchRelief + RoofRelief` should not exceed 1 so the penalty never goes negative. |
| **WalkWeight** | 0.35 | How much walking matters in `Disutility`. |
| **AmenityWeight** | 0.15 | How much stop amenities (bench, roof) matter in `Disutility`. |
| **TimeWeight** | 0.5 | How much extra travel time matters. It is not set independently: `TimeWeight = 1 − WalkWeight − AmenityWeight`, so the three always add up to 1. |
| **GrowthRate** | 10 | Steepness of the sigmoid. Higher means P switches from "rides" to "doesn't ride" over a narrower range of `Disutility`. Keep it positive. |
| **Midpoint** | 0.4 | The `Disutility` at which P(BusUse) is exactly 0.5. Raise it to make residents more willing to ride overall, and lower it to make them less willing. |
| **Steepness (k)** | 2 | Exponent shared by both penalty curves. Treat it as a fixed constant and don't tune it. At 2, small walks and delays are mostly ignored, then the penalty rises quickly around the half-point. At 1, penalties rise immediately. |

## Calculated values
These should not be set manually and are derived from other values.
| Variable | Explanation |
|---|---|
| **WalkPenalty** | `MilesWalked` converted to a 0-1 scale, where 0 means no walking and values near 1 mean a very long walk. |
| **TimeDifference** | Minutes the bus takes beyond the car. It is 0 if the bus is as fast as the car or faster. |
| **Tolerance** | Extra minutes at which `TimePenalty` is 0.5. It is larger for longer car trips. |
| **TimePenalty** | `TimeDifference` converted to a 0-1 scale, relative to `Tolerance`. |
| **BusStopAmenityPenalty** | Amenity penalty of a single boarding stop. Starts at `BaseAmenityPenalty` and is reduced by a bench and/or a roof. |
| **AmenityPenalty** | Average of `BusStopAmenityPenalty` over all boarding stops on the commute. |
| **Disutility** | Weighted total friction, from 0 (no friction) to just under 1. |
| **P(BusUse)** | The output: probability, between 0 and 1, that the resident uses the bus. |