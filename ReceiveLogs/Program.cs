using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ReceiveLogs {
    class Program {
    static void Main (string[] args) {
            var connectionFactory = new ConnectionFactory () { HostName = "localhost" };
            using (var connection = connectionFactory.CreateConnection ()) {
                using (var channel = connection.CreateModel ()) {
                    //申明exchange
                    channel.ExchangeDeclare (exchange: "logs", type: "fanout");
                    //申明随机队列名称
                    var queuename = channel.QueueDeclare ().QueueName;
                    //绑定队列到指定exchange,使用默认路由
                    channel.QueueBind (queue : queuename, exchange: "logs", routingKey: "");
                    Console.WriteLine ("[*] Waitting for logs.");
                    //申明consumer
                    var consumer = new EventingBasicConsumer (channel);
                    //绑定消息接收后的事件委托
                    consumer.Received += (model, ea) => {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString (body);
                        Console.WriteLine ("[x] {0}", message);

                    };

                    channel.BasicConsume (queue : queuename, autoAck : true, consumer : consumer);

                    Console.WriteLine (" Press [enter] to exit.");
                    Console.ReadLine ();
                }
            }
        }
    }
}