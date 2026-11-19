## Description
Bus lines generate revenue for the player. Revenue accrues while the day is in progress based on how well each line serves the city, and is paid out to the player at the end of the day. Revenue earned in one day becomes the money the player starts the next day with.

Revenue is calculated per line, so the player can see which lines are profitable and which aren’t.
## Purpose
- Give the player a reason to build and maintain bus lines
- Create an economy loop: build lines, earn money, expand
- Reward well-designed lines over simply building more of them
- Give the player a measurable way to judge how well a line is performing
## Revenue Calculation
Each bus line earns revenue through fares per passenger.
### Formula
```
PassengersCarried = Σ ( 1 if Roll(HouseID, Day) < P(BusUse), else 0 )

Revenue = PassengersCarried × Fare
```
- Fare is a flat value set by the player in-game
- A line that serves no buildings produces no revenue
- The Roll function should be a deterministic value 0.00-1.00 from a hash function that takes in HouseID and Day
	- Note: **It is necessary to use a hash function** in order to have reproducible randomness. This allows us to have reproducible bugs and consistency when saving and loading the game as a player

Example calculation of `PassengersCarried`:
Imagine there are two residents with P(BusUse) of 0.5, 0.3, and 0.6, respectively.
Call the Roll function on each resident. Let 0.5, 0.7, and 0.2 be the result of each roll, respectively.
0.5 == 0.5 -> did not use bus -> 0
0.7 > 0.3 -> did not use bus -> 0
0.2 < 0.6 -> used bus -> 1
So, `PassengersCarried` = 0 + 0 + 1 = 1.