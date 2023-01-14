using System.Net.Sockets;
using System.Threading.Channels;

namespace Twitch
{
    public class TwitchConnection
    {
        const string ip = "irc.chat.twitch.tv";
        const int port = 6667;

        private string username;
        private string password;
        private StreamReader? streamReader;
        private StreamWriter? streamWriter;
        private TaskCompletionSource<int> connected = new TaskCompletionSource<int>();
        public event ChatMessageHandler OnMessage = delegate { };
        public delegate void ChatMessageHandler((string Username, string Message, string Channel) chatMessage);
        private Channel<(string Username, string Message, string Channel)> ingestQueue = Channel.CreateUnbounded<(string Username, string Message, string Channel)>();

        public TwitchConnection(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        public async Task Connect()
        {
            Console.WriteLine("Connect!");
            var tcpClient = new TcpClient();
            tcpClient.Connect(ip, port);
            streamReader = new StreamReader(tcpClient.GetStream());
            streamWriter = new StreamWriter(tcpClient.GetStream()) { NewLine = "\r\n", AutoFlush = true };

            await streamWriter.WriteLineAsync("PASS " + this.password);
            await streamWriter.WriteLineAsync("NICK " + this.username);
            connected.SetResult(0);

            Task.Run(Read);
            Task.Run(Consume);
        }

        private (string Username, string Message, string Channel) ParseMessage(string[] split)
        {
            int exclamationPointPosition = split[0].IndexOf("!");
            string username = split[0].Substring(1, exclamationPointPosition - 1);
            string channel = split[2].TrimStart('#');
            string message = split[3].Substring(1);

            return (username, message, channel);
        }

        private async void Read()
        {
            while (true)
            {
                if (streamReader == null)
                {
                    continue;
                }
                string line = streamReader.ReadLine(); // :96allskills!96allskills@96allskills.tmi.twitch.tv PRIVMSG #alveussanctuary :waaa Spaceee a password

                if (line == null)
                {
                    Console.WriteLine("Skipping!");
                    continue;
                }

                string[] split = line.Split(" ", 4); // [name, message type, channel, message]
                if (line.StartsWith("PING"))
                {
                    Console.WriteLine("PONG");
                    await streamWriter.WriteLineAsync($"PONG {split[1]}");
                }

                if (split.Length > 2 && split[1] == "PRIVMSG")
                {
                    var message = this.ParseMessage(split);
                    // TODO: Quick rule here to filter out messages that are completly unrelevant. Maybe have to start with "!" to be considered valid.
                    // Or maybe they have to be mod/brodcaster
                    await ingestQueue.Writer.WriteAsync(message);
                }
            }
        }

        private async void Consume()
        {
            while (true)
            {
                var message = await ingestQueue.Reader.ReadAsync();
                this.OnMessage(message);
            }
        }

        public async Task SendMessage(string channel, string message)
        {
            await connected.Task;
            await streamWriter.WriteLineAsync($"PRIVMSG #{channel} :{message}");
        }

        public async Task JoinChannel(string channel)
        {
            await connected.Task;
            await streamWriter.WriteLineAsync($"JOIN #{channel}");
        }
    }
}