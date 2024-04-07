# BandwidthScheduler

Takes user availabilities and generates a schedule based on number of employees needed for any given thirty minute time interval.

## User Availability Form

The user has the ability to make availability windows as small as 30 minutes in a 24 hour period.
Here are four users that will be rendered later.
<img src="https://github.com/matjmase/BandwidthScheduler/blob/main/Screenshots/UserAvailabilities.jpg" width="600" />

## User Proposal Form Generator Form

The scheduler has the ability to propose a custom bandwidth for any given 30 minute time window.
This form allows them to choose the date and the max employees.
<img src="https://github.com/matjmase/BandwidthScheduler/blob/main/Screenshots/ScheduleProposalForm.png" width="300" />

## User Proposal vs Generated Solution

The scheduler will be presented with a solution to the bandwidth scheduling demands.
_When there are more users than slots, the algorithm will grandfather currently on-shift people_
_When there are more users than slots, the algorithm will also choose pseudo random users to fill the slots_
<img src="https://github.com/matjmase/BandwidthScheduler/blob/main/Screenshots/DesiredProposedComparison.jpg" width="600" />

## Future Development

User weighting/priority for getting the shifts.
Team building page.
Published schedule viewing.
User notifications for published dates.
