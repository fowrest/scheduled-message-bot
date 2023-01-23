using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

class DelayedRabbitHandler
{
    private IModel channel;

    private event EventHandler<BasicDeliverEventArgs>? OnMessageHandler; // (model, ea) => { Console.WriteLine("Defalt behaviour"); };

    public DelayedRabbitHandler()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "queuedCommands",
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var dict = new Dictionary<string, object>();
        dict.Add("x-delayed-type", "direct");
        channel.ExchangeDeclare("health-bot", "x-delayed-message", true, false, dict);
        channel.QueueBind("queuedCommands", "health-bot", "");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += this.OnMessage;

        channel.BasicConsume(queue: "queuedCommands", autoAck: false, consumer: consumer);

        this.channel = channel;
    }

    private void OnMessage(object? sender, BasicDeliverEventArgs data)
    {
        Console.WriteLine("Mesage");
        if (this.OnMessageHandler == null)
        {
            Console.WriteLine("No handler");
            return;
        }

        this.OnMessageHandler(sender, data);
    }

    public void RegisterOnMessageHandler(EventHandler<BasicDeliverEventArgs> handler)
    {
        this.OnMessageHandler = handler;
    }

    public void QueueCommand(string text, int waitTime)
    {
        var body = Encoding.UTF8.GetBytes(text);

        IBasicProperties props = channel.CreateBasicProperties();
        props.Headers = new Dictionary<string, object>();
        props.Headers.Add("x-delay", waitTime); // expity.TotalMilliseconds

        channel.BasicPublish(exchange: "health-bot",
                             routingKey: "",
                             basicProperties: props,
                             body: body);


        Console.WriteLine("Enqueuing message");
    }
}