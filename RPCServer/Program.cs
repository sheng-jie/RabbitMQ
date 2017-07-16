using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RPCServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var conection = factory.CreateConnection())
            {
                using (var channel = conection.CreateModel())
                {
                    channel.QueueDeclare(queue: "rpc_queue", durable: false,
                        exclusive: false, autoDelete: false, arguments: null);

                    var consumer = new EventingBasicConsumer(channel);
                    Console.WriteLine("[*] Waiting for message.");

                    consumer.Received += (model, ea) =>
                    {
                        var message = Encoding.UTF8.GetString(ea.Body);
                        int n = int.Parse(message);
                        Console.WriteLine($"Receive request of Fib({n})");
                        int result = Fib(n);

                        var properties = ea.BasicProperties;
                        var replyProerties = channel.CreateBasicProperties();
                        replyProerties.CorrelationId = properties.CorrelationId;

                        channel.BasicPublish(exchange: "", routingKey: properties.ReplyTo,
                            basicProperties: replyProerties, body: Encoding.UTF8.GetBytes(result.ToString()));

                        channel.BasicAck(ea.DeliveryTag, false);
                        Console.WriteLine($"Return result: Fib({n})= {result}");

                    };
                    channel.BasicConsume(queue: "rpc_queue", autoAck: false, consumer: consumer);

                    Console.ReadLine();
                }
            }

        }

        private static int Fib(int n)
        {
            if (n == 0 || n == 1)
            {
                return n;
            }
            return Fib(n - 1) + Fib(n - 2);
        }
    }
}