using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;

namespace RmqBindingTest.Consumer
{
    class ConsumerProgram
    {
        static async Task Main(string[] args)
        {
            var (instanceName, minValue, maxValue) = ParseArgs(args);

            var cts = new CancellationTokenSource();
            CancelKeyPress += (_, cancelArgs) =>
            {
                cts.Cancel();
                cts.Dispose();
                cancelArgs.Cancel = true;
            };

            var connectionFactory = new ConnectionFactory 
            { 
                UserName = Connection.User,
                Password = Connection.Password,
                HostName = Connection.Host,
                VirtualHost = Connection.Vhost,
                ClientProvidedName = $"BindingTest Consumer {instanceName}"
            };
            var connection = connectionFactory.CreateConnection();
            var model = connection.CreateModel();

            model.ExchangeDeclare(Connection.Exchange, ExchangeType.Topic);
            var queueName = $"BindingTest-{instanceName}";
            model.QueueDeclare(queueName);

            for(var i=minValue; i<=maxValue; i++)
            {
                var bindingKey = i.ToString(@"00\.0\.0");
                model.QueueBind(queueName, Connection.Exchange, bindingKey);
            }

            var consumer = new EventingBasicConsumer(model);
            var count = 0;
            consumer.Received += (ch, ea) =>
            {
                count++;
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                WriteLine(message);

                model.BasicAck(ea.DeliveryTag, false);
            };
            var consumerTag = model.BasicConsume(queueName, false, consumer);

            WriteLine("Consumer started. Ctrl-C to exit.");

            // delay indefinitely until token is cancelled.
            try
            {
                await Task.Delay(-1, cts.Token);
            }
            catch (TaskCanceledException) { }

            var statsString = $"CON|{instanceName}|{count}";
            var statsBody = Encoding.UTF8.GetBytes(statsString);
            model.ExchangeDeclare(Connection.StatsExchange, ExchangeType.Direct);
            model.BasicPublish(Connection.StatsExchange, "STAT", null, statsBody);

            WriteLine($"Published stats: {statsString}");

            model.Dispose();
            connection.Dispose();
        }

        static (string instanceName, int minValue, int maxValue) ParseArgs(string[] args)
        {
            if(args.Length == 3 
                && int.TryParse(args[1], out int minValue) 
                && int.TryParse(args[2], out int maxValue)
                && (minValue is >= 0 and <= 9999)
                && (maxValue is >= 0 and <= 9999)
                && (maxValue >= minValue))
            {
                WriteLine($"Instance {args[0]}, {minValue}-{maxValue}");
                return (args[0], minValue, maxValue);
            }

            WriteLine("Invalid args expected: <instance name> <min value int 0-9999> <max value int 0-9999>");
            WriteLine("Using defaults 'default' 0 9999");
            return ("default", 0, 9999);
        }
    }
}
