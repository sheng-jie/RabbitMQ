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
                    channel.QueueDeclare(queue: "rpc_queue", exclusive: false);

                    var consumer = new EventingBasicConsumer(channel);
                    Console.WriteLine("[*] Waiting for message.");

                    consumer.Received += (model, ea) =>
                    {
                        var message = Encoding.UTF8.GetString(ea.Body);
                        int n = int.Parse(message);
                        var properties = ea.BasicProperties;
                        int result = Fib(n);
                        Console.WriteLine($"Receive request of Fib({n})");
                        
                        var replyProerties = channel.CreateBasicProperties();
                        replyProerties.CorrelationId = properties.CorrelationId;

                        channel.BasicPublish(exchange: "", routingKey: properties.ReplyTo,
                         basicProperties: replyProerties, body: Encoding.UTF8.GetBytes(result.ToString()));

                        channel.BasicAck(ea.DeliveryTag, false);



                    };
                    channel.BasicConsume(queue: "rpc_queue", autoAck: false, consumer: consumer);

                    Console.WriteLine("Press any key exit.");
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
