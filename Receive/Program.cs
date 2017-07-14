using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Receive {
    class Program {
    public static void Main () {
    //1. 创建基于本地的连接工厂
            var factory = new ConnectionFactory () { HostName = "localhost" };
            //2. 建立连接
            using (var connection = factory.CreateConnection ()) {
                //3. 创建频道
                using (var channel = connection.CreateModel ()) {
                    //4. 申明队列
                    channel.QueueDeclare (queue: "hello", durable : false, exclusive : false, autoDelete : false, arguments : null);

                    //5. 构造消费者实例
                    var consumer = new EventingBasicConsumer (channel);
                    //6. 绑定消息接收后的事件委托
                    consumer.Received += (model, ea) => {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString (body);
                        Console.WriteLine (" [x] Received {0}", message);
                    };
                    //7. 启动消费者
                    channel.BasicConsume (queue: "hello", autoAck : true, consumer : consumer);

                    Console.WriteLine (" Press [enter] to exit.");
                    Console.ReadLine ();
                }
            }
        }
    }
}