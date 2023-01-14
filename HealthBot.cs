using Twitch;

namespace Health
{
    public class HealthBot
    {

        TwitchConnection twitchConnection;

        public HealthBot(string username, string password)
        {
            this.twitchConnection = new TwitchConnection(username, password);
            twitchConnection.OnMessage += (message) =>
            {
                Console.WriteLine(message.Username + " | " + message.Message);
            };
        }

        public async void Start()
        {
            await this.twitchConnection.Connect();
            await this.twitchConnection.JoinChannel("forsen");
        }

    }
}
