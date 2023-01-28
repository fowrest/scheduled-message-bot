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
        private const string START_TIMER = "timer";
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

            if (trimmedMessage.StartsWith(START_TIMER))
            {
                // Add to rabbit and DB?
                var messageContent = trimmedMessage.Substring(START_TIMER.Length + 1);
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
                    return;
                }

                if (time < 1)
                {
                    Console.WriteLine("Time is to smol");
                    return;
                }

                var timerMessage = toEnqueue[2];
                var timerId = Guid.NewGuid().ToString();
                var timer = new Database.Timer(timerId, message.Channel, userTimerId, 1, timerMessage);

                TimerContext.Add(timer);
                TimerContext.SaveChanges();

                this.rabbitHandler.QueueCommand(timerId, time * 1000);
            }
            else if (message.Message.StartsWith(REPEAT_TIMER))
            {
                // Enqueue and save to DB
            }
            else if (message.Message.StartsWith(REMOVE_TIMER))
            {
                // Remove timer from DB
            }
            else if (message.Message.StartsWith(LIST_TIMERS))
            {
                // Get all from DB and list
            }
        }

        private async void HandleRabbitMessage(object? sender, BasicDeliverEventArgs data)
        {
            var timerId = Encoding.UTF8.GetString(data.Body.ToArray());

            if (timerId == null)
            {
                return;
            }

            var timerData = await TimerContext.timers.FindAsync(timerId);

            if (timerData != null)
            {
                Console.WriteLine("Dink donk: " + timerData.Channel + timerData.TimerName + timerData.Message);
                TimerContext.timers.Remove(timerData);
                TimerContext.SaveChanges();
            }
            else
            {
                Console.WriteLine("Timer expiered, but the data could not be found in the DB");
            }
        }

        public async void Start()
        {
            await this.twitchConnection.Connect();
            await this.twitchConnection.JoinChannel("faowrest");
        }

    }
}
