using System.Text;
using Database;
using RabbitMQ.Client.Events;
using Twitch;
using Microsoft.EntityFrameworkCore;

namespace Health
{
    /*
        Consumes commands in the form of:
        @healhbot timer <timer name> <time> <message>
        @healhbot repeat <repeat name> <time> <message>
        @healhbot remove <timer name>
        @healhbot list
     */
    public class HealthBot
    {
        TwitchConnection twitchConnection;
        DelayedRabbitHandler rabbitHandler;
        private const string TIMER = "timer";
        private const string REPEAT_TIMER = "repeat";
        private const string REMOVE_TIMER = "remove";
        private const string LIST_TIMERS = "list";
        private static string[] keywords = { "healthBot" };
        private TimerContext TimerContext = new TimerContext();

        public HealthBot(string username, string password)
        {
            this.twitchConnection = new TwitchConnection(username, password, keywords);
            twitchConnection.OnMessage += this.HandleChatMessage;

            this.rabbitHandler = new DelayedRabbitHandler();

            TimerContext.Database.EnsureDeleted(); // TODO: This is for testing
            TimerContext.Database.EnsureCreated();
        }

        private void HandleChatMessage((string Username, string Message, string Channel) message)
        {
            Console.WriteLine(message.Username + " | " + message.Message);
            var trimmedMessage = message.Message.Substring("healthBot".Length + 1);

            var messageContent = trimmedMessage.Substring(trimmedMessage.IndexOf(" ") + 1);
            Console.WriteLine(messageContent);
            var toEnqueue = messageContent.Split(" ", 3);
            var timerName = toEnqueue[0];

            int time = -1;
            try
            {
                if (toEnqueue.Length > 1)
                {
                    time = Int32.Parse(toEnqueue[1]);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Bad time: " + e.Message);
                // return;
            }

            if (trimmedMessage.StartsWith(TIMER))
            {
                // Add to rabbit and DB?
                if (time < 1)
                {
                    Console.WriteLine("Time is to smol");
                    return;
                }


                var timerMessage = toEnqueue[2];
                var timerId = Guid.NewGuid().ToString();
                var timer = new Database.Timer(timerId, message.Channel, timerName, time, timerMessage);

                TimerContext.Add(timer);
                TimerContext.SaveChanges();

                this.rabbitHandler.QueueCommand(TIMER + ":" + timerId, time * 1000);
            }
            else if (trimmedMessage.StartsWith(REPEAT_TIMER))
            {
                if (time < 1)
                {
                    Console.WriteLine("Time is to smol");
                    return;
                }
                // Enqueue and save to DB

                var timerMessage = toEnqueue[2];
                var timerId = Guid.NewGuid().ToString();
                var timer = new Database.RepeatTimer(timerId, message.Channel, timerName, DateTime.Now.AddSeconds(time).ToUniversalTime(), time, timerMessage);

                TimerContext.Add(timer);
                TimerContext.SaveChanges();

                this.rabbitHandler.QueueCommand(REPEAT_TIMER + ":" + timerId, time * 1000);
            }
            else if (trimmedMessage.StartsWith(REMOVE_TIMER))
            {
                this.DeleteWithoutQuery(message.Channel, timerName);
            }
            else if (trimmedMessage.StartsWith(LIST_TIMERS))
            {
                // Get all from DB and list
                var timers = TimerContext.Timers.Where(timer => timer.Channel == message.Channel).ToArray();
                var repeatTimers = TimerContext.RepeatTimers.Where(repeatTimer => repeatTimer.Channel == message.Channel).ToArray();

                var messageOut = "";

                foreach (var timer in timers)
                {
                    messageOut += timer.TimerName + " : " + timer.ExpireTime + " : " + timer.Message + "\n";
                }

                foreach (var timer in repeatTimers)
                {
                    messageOut += timer.TimerName + " : " + timer.NextExpireTime + " : " + timer.TimerInterval + " : " + timer.Message + "\n";
                }

                // TODO: Send messageOut to chat and tag user
                Console.WriteLine(messageOut);
            }
            else
            {
                Console.WriteLine("Catch all, did not match any message type");
            }
        }

        private async void HandleRabbitMessage(object? sender, BasicDeliverEventArgs data)
        {
            var messageData = Encoding.UTF8.GetString(data.Body.ToArray());
            var splittedMessageData = messageData.Split(":");
            var entityType = splittedMessageData[0];
            var entityId = splittedMessageData[1];

            if (entityType == null || entityId == null)
            {
                Console.WriteLine("Missing type or id, return");
                return;
            }

            if (entityType == TIMER)
            {
                var timerData = await TimerContext.Timers.FindAsync(entityId);

                if (timerData != null)
                {
                    Console.WriteLine("Dink donk: " + timerData.Channel + timerData.TimerName + timerData.Message);
                    TimerContext.Timers.Remove(timerData);
                    TimerContext.SaveChanges();
                }
                else
                {
                    Console.WriteLine("Timer expiered, but the data could not be found in the DB");
                }
            }
            else if (entityType == REPEAT_TIMER)
            {
                var timerData = await TimerContext.RepeatTimers.FindAsync(entityId);

                if (timerData != null)
                {
                    Console.WriteLine("Dink donk: " + timerData.Channel + timerData.TimerName + timerData.Message);
                    // TODO: To prevent drift of repeats, need to adjust since we might be of by a second or so.
                    this.rabbitHandler.QueueCommand(REPEAT_TIMER + ":" + entityId, timerData.TimerInterval * 1000);
                    timerData.NextExpireTime = DateTime.Now.AddSeconds(timerData.TimerInterval).ToUniversalTime();
                    TimerContext.SaveChanges();
                }
            }
            else
            {
                Console.WriteLine("No support for entity type" + entityType);
            }
        }

        public async void Start()
        {
            await this.twitchConnection.Connect();
            await this.twitchConnection.JoinChannel("faowrest");
            this.rabbitHandler.RegisterOnMessageHandler(this.HandleRabbitMessage);
        }


        private void DeleteWithoutQuery(string channel, string timerName)
        {
            var timer = TimerContext.Timers.Local.Where(timer => timer.Channel == channel && timer.TimerName == timerName).SingleOrDefault();
            if (timer != null)
            {
                TimerContext.Timers.Entry(timer).State = EntityState.Deleted;
                TimerContext.SaveChanges();
            }
            else
            {
                var removedTimers = TimerContext.Database.ExecuteSqlInterpolated($"DELETE FROM \"Timers\" WHERE \"Channel\"={channel} AND \"TimerName\"={timerName}");
            }

            var repeatTimer = TimerContext.RepeatTimers.Local.Where(timer => timer.Channel == channel && timer.TimerName == timerName).SingleOrDefault();
            if (repeatTimer != null)
            {
                TimerContext.RepeatTimers.Entry(repeatTimer).State = EntityState.Deleted;
                TimerContext.SaveChanges();
            }
            else
            {
                var removedRepeaters = TimerContext.Database.ExecuteSqlInterpolated($"DELETE FROM \"RepeatTimers\" WHERE \"Channel\"={channel} AND \"TimerName\"={timerName}");
            }
        }
    }
}
