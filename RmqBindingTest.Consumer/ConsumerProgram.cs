using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
                cancelArgs.Cancel = true;
                WriteLine("Shutting down. Wait for stats publication.");
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
            model.QueueDeclare(queueName, true, false, false);

            WriteLine();
            for(var i=minValue; i<=maxValue; i++)
            {
                if(cts.IsCancellationRequested)
                {
                    break;
                }

                var bindingKey = i.ToString(@"00\.0\.0");
                model.QueueBind(queueName, Connection.Exchange, bindingKey);

                if(i % ((maxValue - minValue)/10) == 0)
                {
                    Write("#");
                }
            }

            WriteLine();
            WriteLine("Bindings Complete");

            var consumer = new EventingBasicConsumer(model);
            var count = 0;
            consumer.Received += (ch, ea) =>
            {
                count++;
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                WriteLine(message);
                RecordTimeStats(message);

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

            WriteTimeStats(instanceName);

            model.QueueDelete(queueName);
            WriteLine($"Queue {queueName} deleted.");

            model.Dispose();
            connection.Dispose();
            cts.Dispose();
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

        private static readonly Regex regex = new(@"\[(.*)\]");
        private static readonly List<(DateTime, TimeSpan)> timeStats = new();

        private static void RecordTimeStats(string message)
        {
            if(regex.Match(message) is Match { Success: true } match)
            {
                WriteLine(match.Groups[1].Value);
                if(DateTime.TryParse(match.Groups[1].Value, out var publishedTime))
                {
                    var elapsed = DateTime.UtcNow - publishedTime;
                    timeStats.Add((publishedTime, elapsed));
                }
            }
        }

        private static void WriteTimeStats(string instanceName)
        {
            var statsDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "BindingTest");
            if(!Directory.Exists(statsDir))
            {
                Directory.CreateDirectory(statsDir);
            }
            var statsFile = Path.Combine(statsDir, $"BindingTest-TimeStats-{instanceName}.csv");

            File.WriteAllLinesAsync(statsFile, timeStats.Select(x => $"{x.Item1.ToLongTimeString()}, {x.Item2.TotalSeconds}"));

            WriteLine($"TimeStats written to: {statsFile}");
        }

        static void Spike()
        {
            WriteLine(Environment.GetEnvironmentVariable("TEMP"));

            var text = $"[{DateTime.UtcNow}] and some other stuff.";
            WriteLine(text);

            if(regex.Match(text) is Match { Success: true } match)
            {
                WriteLine(match.Groups[1].Value);
                if(DateTime.TryParse(match.Groups[1].Value, out var publishedTime))
                {
                    WriteLine(publishedTime);
                }
            }
            else
            {
                WriteLine("No match");
            }
        }
    }
}
