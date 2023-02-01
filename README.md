# Scheduled Message Bot/Health boy
This is a message bot to be used on Twitch.tv. 
Health bot has the ability to set repeating timers and regular timers. Perfect for reminders to take a break or stretch.

## Usage
Health bot works via the chat and supports the following commands.

### Regular timer
`@healhbot timer <timer name> <time> <message>`

Example:
`@healthbot timer microwave 120 Microwave is done`

This will set a timer that will after 120 seconds expire and type "Microwave is done" in chat.

### Repating timer
`@healhbot repeat <repeat name> <time> <message>`

Example:
`@healthbot repeat stretch 3600 Time to stretch`

Repeating timer that will type "Time to stretch" in chat every hour.

### Removing timer
`@healhbot remove <timer name>`

Example:
`@healthbot remove stretch`

Removes an active timer.

### Listing active timers
`@healhbot list`

Lists all active timers.

