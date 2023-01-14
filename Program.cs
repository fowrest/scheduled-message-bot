using Health;

static async Task Start()
{
    string password = "ABC123";
    string username = "justinfan1233211";

    var twitchBot = new HealthBot(username, password);
    twitchBot.Start();
    Console.WriteLine("Started, press any key to exit");
    Console.ReadKey();
    Console.WriteLine("Exiting");
}

Start();