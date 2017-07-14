using System;
using System.Text;
using RabbitMQ.Client;

namespace NewTask {
    class Program {
    public static void Main (string[] args) {
    //1.创建基于本地的连接工厂
            var factory = new ConnectionFactory () { HostName = "localhost" };
            //2. 建立连接
            using (var connection = factory.CreateConnection ()) {
                //3. 创建频道
                using (var channel = connection.CreateModel ()) {
                    //4. 申明队列(指定durable:true,告知rabbitmq对消息进行持久化)
                    channel.QueueDeclare (queue: "work_queue", durable : true, exclusive : false, autoDelete : false, arguments : null);
                    //将消息标记为持久性 - 将IBasicProperties.SetPersistent设置为true
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent=true;

                    //5. 构建byte消息数据包
                    string message = GetMessage (args);
                    var body = Encoding.UTF8.GetBytes (message);

                    //6. 发送数据包
                    channel.BasicPublish (exchange: "", routingKey: "work_queue", basicProperties : null, body : body);
                    Console.WriteLine (" [x] Sent {0}", message);
                }
            }
        }
        private static string GetMessage (string[] args) {
            return ((args.Length > 0) ? string.Join (" ", args) : "Hello World!");
        }
    }
}