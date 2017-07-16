using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Receive
{
    class Program
    {
        public static void Main()
        {
            //1.实例化连接工厂
            var factory = new ConnectionFactory() { HostName = "localhost" };
            //2. 建立连接
            using (var connection = factory.CreateConnection())
            {
                //3. 创建信道
                using (var channel = connection.CreateModel())
                {
                    //4. 申明队列
                    channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);

                    //设置prefetchCount : 1来告知RabbitMQ，在未收到消费端的消息确认时，不再分发消息，也就确保了当消费端处于忙碌状态时，不再分配任务。
                    channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                    
                    //5. 构造消费者实例
                    var consumer = new EventingBasicConsumer(channel);
                    //6. 绑定消息接收后的事件委托
                    consumer.Received += (model, ea) =>
                    {
                        var message = Encoding.UTF8.GetString(ea.Body);
                        Console.WriteLine(" [x] Received {0}", message);
                        Thread.Sleep(20000);//模拟耗时
                        Console.WriteLine(" [x] Done");
                        // 7. 发送消息确认信号（手动消息确认）
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    };
                    //8. 启动消费者
                    //autoAck:true；自动进行消息确认，当消费端接收到消息后，就自动发送ack信号，不管消息是否正确处理完毕
                    //autoAck:false；关闭自动消息确认，通过调用BasicAck方法手动进行消息确认
                    channel.BasicConsume(queue: "hello", autoAck: false, consumer: consumer);

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
        }
    }
}