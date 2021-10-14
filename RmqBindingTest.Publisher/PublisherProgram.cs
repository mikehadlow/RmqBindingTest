using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;

namespace RmqBindingTest
{
    class PublisherProgram
    {
        static Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            CancelKeyPress += (_, cancelArgs) => 
            {
                cts.Cancel();
                cancelArgs.Cancel = true;
            };

            var parsedArgs = ParseArgs(args);
            WriteLine($"Starting {parsedArgs.instanceName}, {parsedArgs.minValue}-{parsedArgs.maxValue}. Ctrl-C to stop.");
            return RunLoop(parsedArgs, cts.Token);
        }

        static async Task RunLoop(
            (string instanceName, int minValue, int maxValue) parsedArgs,
            CancellationToken cancellation)
        {
            var connectionFactory = new ConnectionFactory 
            { 
                UserName = Connection.User,
                Password = Connection.Password,
                HostName = Connection.Host,
                VirtualHost = Connection.Vhost,
                ClientProvidedName = $"BindingTest Publisher {parsedArgs.instanceName}"
            };

            using var connection = connectionFactory.CreateConnection();
            using var model = connection.CreateModel();

            var count = 0;
            try
            {
                model.ExchangeDeclare(Connection.Exchange, ExchangeType.Topic);

                while (!cancellation.IsCancellationRequested)
                {
                    await Task.Delay(1, cancellation);

                    var i = (count % (parsedArgs.maxValue - parsedArgs.minValue)) + parsedArgs.minValue;
                    var routingKey = i.ToString(@"00\.0\.0");
                    var body = Encoding.UTF8.GetBytes($"[{DateTime.UtcNow}] Message number: {count,10}, RoutingKey={routingKey}");

                    model.BasicPublish(Connection.Exchange, routingKey, null, body);
                    count++;
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception exception)
            {
                WriteLine(exception.ToString());
            }
            finally
            {
                var statsString = $"PUB|{parsedArgs.instanceName}|{count}";
                var statsBody = Encoding.UTF8.GetBytes(statsString);
                model.ExchangeDeclare(Connection.StatsExchange, ExchangeType.Direct);
                model.BasicPublish(Connection.StatsExchange, "STAT", null, statsBody);

                WriteLine($"Published stats: {statsString}");
            }
        }

        static (string instanceName, int minValue, int maxValue) ParseArgs(string[] args)
        {
            if(args.Length == 3 
                && int.TryParse(args[1], out int minValue)
                && int.TryParse(args[2], out int maxValue)
                && (minValue is >= 0 and <= 999999)
                && (maxValue is >= 0 and <= 999999)
                && (maxValue >= minValue))
            {
                WriteLine($"Instance {args[0]}, {minValue}-{maxValue}");
                return (args[0], minValue, maxValue);
            }

            WriteLine("Invalid args expected: <instance name> <min value int 0-999999> <max value int 0-999999>");
            WriteLine("Using defaults 'default' 0 9999");
            return ("default", 0, 9999);
        }
    }
}
