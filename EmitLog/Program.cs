using System;
using System.Text;
using RabbitMQ.Client;

namespace EmitLog
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionFactory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = connectionFactory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    // 生成随机队列名称
                    var queueName = channel.QueueDeclare().QueueName;
                    //使用fanout exchange type，指定exchange名称
                    channel.ExchangeDeclare(exchange: "logs", type: "fanout");

                    var message = GetMessage(args);
                    var body = Encoding.UTF8.GetBytes(message);
                    //发布到指定exchange，fanout类型无需指定routingKey
                    channel.BasicPublish(exchange: "logs", routingKey: "", basicProperties: null, body: body);
                    Console.WriteLine(" [x] Sent {0}", message);
                }
            }

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }

        private static string GetMessage(string[] args)
        {
            return ((args.Length > 0) ?
                string.Join(" ", args) :
                "info: Hello World!");
        }
    }
}