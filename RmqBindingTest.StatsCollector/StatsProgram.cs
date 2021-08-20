using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;

namespace RmqBindingTest.StatsCollector
{
    class StatsProgram
    {
        static async Task Main()
        {
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
                ClientProvidedName = $"Stats Collector"
            };
            var connection = connectionFactory.CreateConnection();
            var model = connection.CreateModel();

            model.ExchangeDeclare(Connection.StatsExchange, ExchangeType.Direct);
            var queueName = $"StatsQueue";
            model.QueueDeclare(queueName);
            model.QueueBind(queueName, Connection.StatsExchange, "STAT");

            var consumer = new EventingBasicConsumer(model);
            long pubTotal = 0;
            long conTotal = 0;
            consumer.Received += (ch, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                var messageArgs = message.Split('|');
                if(messageArgs.Length == 3 && long.TryParse(messageArgs[2], out long count))
                {
                    if(messageArgs[0] == "PUB")
                    {
                        pubTotal += count;
                    }
                    if(messageArgs[0] == "CON")
                    {
                        conTotal += count;
                    }
                    WriteLine($"PUB: {pubTotal, 10}, CON: {conTotal, 10}, MSG: {message}");
                }
                else
                {
                    WriteLine($"Invalid status message: {message}");
                }

                model.BasicAck(ea.DeliveryTag, false);
            };
            var consumerTag = model.BasicConsume(queueName, false, consumer);

            WriteLine("Stats consumer started. Ctrl-C to exit.");

            // delay indefinitely until token is cancelled.
            try
            {
                await Task.Delay(-1, cts.Token);
            }
            catch (TaskCanceledException) { }

            model.Dispose();
            connection.Dispose();
        }
    }
}
