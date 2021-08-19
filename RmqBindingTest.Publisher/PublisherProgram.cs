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
                cts.Dispose();
                cancelArgs.Cancel = true;
            };

            WriteLine("Starting loop. Ctrl-C to stop.");
            return RunLoop(ParseArgs(args), cts.Token);
        }

        static async Task RunLoop((string instanceName, int minValue, int maxValue) parsedArgs, CancellationToken cancellation)
        {
            try
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

                model.ExchangeDeclare(Connection.Exchange, ExchangeType.Topic);

                var i = parsedArgs.minValue;
                while (!cancellation.IsCancellationRequested)
                {
                    await Task.Delay(1, cancellation);

                    var routingKey = i.ToString(@"00\.0\.0");
                    var body = Encoding.UTF8.GetBytes($"Message number {i}, RoutingKey={routingKey}");

                    model.BasicPublish(Connection.Exchange, routingKey, null, body);

                    i++;
                    if (i > parsedArgs.maxValue) i = parsedArgs.minValue;
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception exception)
            {
                WriteLine(exception.ToString());
            }
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
