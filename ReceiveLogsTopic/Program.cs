using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ReceiveLogsTopic
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: "topic_logs", type: "topic");
                    var queuename = channel.QueueDeclare().QueueName;
                    if (args.Length < 1)
                    {
                        Console.Error.WriteLine("Specify binding_key");
                        Environment.Exit(1);
                        return;
                    }
                    foreach (var bindingKey in args)
                    {
                        channel.QueueBind(queue: queuename, exchange: "topic_logs", routingKey: bindingKey);
                    }

                    Console.WriteLine("[*] Waiting for message.");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);
                        var routingKey = ea.RoutingKey;
                        Console.WriteLine("[x] Received '{0}':'{1}'", ea.RoutingKey, message);
                    };

                    channel.BasicConsume(queue: queuename, autoAck: true, consumer: consumer);

                    Console.WriteLine("Press any key exit.");
                    Console.ReadLine();
                }
            }
        }
    }
}
