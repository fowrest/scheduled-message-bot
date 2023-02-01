using System.Text;
using Database;
using RabbitMQ.Client.Events;
using Twitch;

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
            this.rabbitHandler.RegisterOnMessageHandler(this.HandleRabbitMessage);

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
            var userTimerId = toEnqueue[0];

            int time = -1;
            try
            {
                time = Int32.Parse(toEnqueue[1]);
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
                var timer = new Database.Timer(timerId, message.Channel, userTimerId, time, timerMessage);

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

                // TODO: the time argument here is not correct, probably want to supply both the time and possibly expirt date to be able to list timers.
                // For the repeat case we need to update the expity time on each re-queue
                var timer = new Database.RepeatTimer(timerId, message.Channel, userTimerId, time, timerMessage);

                TimerContext.Add(timer);
                TimerContext.SaveChanges();

                this.rabbitHandler.QueueCommand(REPEAT_TIMER + ":" + timerId, time * 1000);
            }
            else if (trimmedMessage.StartsWith(REMOVE_TIMER))
            {
                // Remove timer from DB
            }
            else if (trimmedMessage.StartsWith(LIST_TIMERS))
            {
                // Get all from DB and list
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
                    this.rabbitHandler.QueueCommand(REPEAT_TIMER + ":" + entityId, (int)timerData.ExpireTime * 1000);
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
        }

    }
}
