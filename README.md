# BandwidthScheduler

Takes user availabilities and generates a schedule based on number of employees needed for any given thirty minute time interval.

## User Availability Form

The user has the ability to make availability windows as small as 30 minutes in a 24 hour period.<br />
Here are four users that will be rendered later.
<br />
<img src="https://github.com/matjmase/BandwidthScheduler/blob/main/Screenshots/UserAvailabilities.jpg" width="600" />

## User Proposal Form Generator Form

The scheduler has the ability to propose a custom bandwidth for any given 30 minute time window.<br />
This form allows them to choose the date and the max employees.
<br />
<img src="https://github.com/matjmase/BandwidthScheduler/blob/main/Screenshots/ScheduleProposalForm.png" width="300" />

## User Proposal vs Generated Solution

The scheduler will be presented with a solution to the bandwidth scheduling demands.<br />
_When there are more users than slots, the algorithm will grandfather currently on-shift people_<br />
_When there are more users than slots, the algorithm will also choose pseudo random users to fill the slots_
<br />
<img src="https://github.com/matjmase/BandwidthScheduler/blob/main/Screenshots/DesiredProposedComparison.jpg" width="600" />

## Future Development

User weighting/priority for getting the shifts.<br />
Team building page.<br />
Published schedule viewing.<br />
User notifications for published dates.<br />
