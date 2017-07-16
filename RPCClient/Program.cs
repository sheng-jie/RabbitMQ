using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RPCClient
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
                    channel.QueueDeclare(queue: "rpc_queue", exclusive: false);

                    var correlationId = Guid.NewGuid().ToString();
                    var replyQueue = channel.QueueDeclare().QueueName;

                    var properties = channel.CreateBasicProperties();
                    properties.ReplyTo = replyQueue;
                    properties.CorrelationId = correlationId;

                    string message = args.Length > 0 ? $"Request fib({args[0]}." : "Request fib(30)";
                    var body = Encoding.UTF8.GetBytes(message);
                    //发布消息
                    channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: properties, body: body);

                    Console.WriteLine($"[*] {message}");

                    // //创建消费者用于消息回调
                    // var callbackConsumer = new EventingBasicConsumer(channel);
                    // channel.BasicConsume(queue: replyQueue, autoAck: true, consumer: callbackConsumer);

                    // callbackConsumer.Received += (model, ea) =>
                    // {
                    //     if (ea.BasicProperties.CorrelationId == correlationId)
                    //     {
                    //         var responseMsg = $"Get Response: {Encoding.UTF8.GetString(ea.Body)}";

                    //         Console.WriteLine($"[x]: {responseMsg}");
                    //     }
                    // };


                    Console.WriteLine("Press any key exit.");
                    Console.ReadLine();

                }
            }
        }
    }
}