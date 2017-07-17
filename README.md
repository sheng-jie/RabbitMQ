![](http://upload-images.jianshu.io/upload_images/2799767-82c5402158929477.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)


# 1.引言
RabbitMQ——Rabbit Message Queue的简写，但不能仅仅理解其为消息队列，消息代理更合适。RabbitMQ 是一个由 Erlang 语言开发的AMQP（高级消息队列协议）的开源实现，其内部结构如下：

![RabbitMQ 内部结构](http://upload-images.jianshu.io/upload_images/2799767-05b3dc7216205c41.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

RabbitMQ作为一个消息代理，主要和**消息**打交道，负责接收并转发消息。RabbitMQ提供了可靠的消息机制、跟踪机制和灵活的消息路由，支持消息集群和分布式部署。适用于排队算法、秒杀活动、消息分发、异步处理、数据同步、处理耗时任务、CQRS等应用场景。

下面我们就来学习下RabbitMQ。

# 2. 环境搭建
本文主要基于Windows下使用Vs Code 基于.net core进行demo演示。开始之前我们需要准备好以下环境。
* 安装Erlang运行环境
下载安装[Erlang](http://www.erlang.org/downloads)。
* 安装RabbitMQ
下载安装Windows版本的[RabbitMQ](http://www.rabbitmq.com/install-windows.html)。
* 启动RabbitMQ Server
点击Windows开始按钮，输入RabbitMQ找到`RabbitMQ Comman Prompt`，以管理员身份运行。
* 依次执行以下命令启动RabbitMQ服务
 ```
rabbitmq-service install
rabbitmq-service enable
rabbitmq-service start
 ```
* 执行`rabbitmqlctl status`检查RabbitMQ状态
* 安装管理平台插件
执行`rabbitmq-plugins enable rabbitmq_management`即可成功安装，使用默认账号密码（guest/guest）登录[http://localhost:15672/](http://localhost:15672/ )即可。

# 3. Hello RabbitMQ
在开始之前我们先来了解下消息模型：
![消息流](http://upload-images.jianshu.io/upload_images/2799767-a5e45f97bec36c8a.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)
消费者（consumer）订阅某个队列。生产者（producer）创建消息，然后发布到队列（queue）中，队列再将消息发送到监听的消费者。

下面我们我们通过demo来了解RabbitMQ的基本用法。

## 3.1.消息的发送和接收

创建RabbitMQ文件夹，打开命令提示符，分别创建两个控制台项目Send、Receive。
```
dotnet new console --name Send //创建发送端控制台应用
cd Send //进入Send目录
dotnet add package RabbitMQ.Client //添加RabbitMQ.Client包
dotnet restore //恢复包
```
```
dotnet new console --name Receive //创建接收端控制台应用
cd Receive //进入Receive目录
dotnet add package RabbitMQ.Client //添加RabbitMQ.Client包
dotnet restore //恢复包
```
我们先来添加消息发送端逻辑：
```
//Send.cs 
public static void Main(string[] args)
{
    //1.1.实例化连接工厂
    var factory = new ConnectionFactory() { HostName = "localhost" };
    //2. 建立连接
    using (var connection = factory.CreateConnection())
    {
        //3. 创建信道
        using (var channel = connection.CreateModel())
        {
            //4. 申明队列
            channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);
            //5. 构建byte消息数据包
            string message = args.Length > 0 ? args[0] : "Hello RabbitMQ!";
            var body = Encoding.UTF8.GetBytes(message);
            //6. 发送数据包
            channel.BasicPublish(exchange: "", routingKey: "hello", basicProperties: null, body: body);
            Console.WriteLine(" [x] Sent {0}", message);
        }
    }
}
```
再来完善消息接收端逻辑：
```
//Receive.cs  省略部分代码
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
            //5. 构造消费者实例
            var consumer = new EventingBasicConsumer(channel);
            //6. 绑定消息接收后的事件委托
            consumer.Received += (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body);
                Console.WriteLine(" [x] Received {0}", message);
                Thread.Sleep(6000);//模拟耗时
                Console.WriteLine (" [x] Done");
            };
            //7. 启动消费者
            channel.BasicConsume(queue: "hello", autoAck: true, consumer: consumer);
            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
```
先运行消息接收端，再运行消息发送端，结果如下图。

![运行结果](http://upload-images.jianshu.io/upload_images/2799767-9079418f0a3f1ccd.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

从上面的代码中可以看出，发送端和消费端的代码前4步都是一样的。主要的区别在于发送端调用`channel.BasicPublish`方法发送消息；而接收端需要实例化一个`EventingBasicConsumer`实例来进行消息处理逻辑。另外一点需要注意的是：消息接收端和发送端的队列名称（queue）必须保持一致，这里指定的队列名称为hello。

## 3.2. 循环调度
使用工作队列的好处就是它能够并行的处理队列。如果堆积了很多任务，我们只需要添加更多的工作者（workers）就可以了。我们先启动两个接收端，等待消息接收，再启动一个发送端进行消息发送。

![消息分发](http://upload-images.jianshu.io/upload_images/2799767-283ced13913a0aac.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

我们增加运行一个消费端后的运行结果：

![循环调度](http://upload-images.jianshu.io/upload_images/2799767-906a1ab86a7459d8.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

从图中可知，我们循环发送4条信息，两个消息接收端按顺序被循环分配。
默认情况下，RabbitMQ将按顺序将每条消息发送给下一个消费者。平均每个消费者将获得相同数量的消息。这种分发消息的方式叫做循环（round-robin）。

## 3.3. 消息确认
按照我们上面的demo，一旦RabbitMQ将消息发送到消费端，消息就会立即从内存中移出，无论消费端是否处理完成。在这种情况下，消息就会丢失。

为了确保一个消息永远不会丢失，RabbitMQ支持**消息确认（message acknowledgments）**。当消费端接收消息并且处理完成后，会发送一个ack（消息确认）信号到RabbitMQ，RabbitMQ接收到这个信号后，就可以删除掉这条已经处理的消息任务。但如果消费端挂掉了（比如，通道关闭、连接丢失等）没有发送ack信号。RabbitMQ就会明白某个消息没有正常处理，RabbitMQ将会重新将消息入队，如果有另外一个消费端在线，就会快速的重新发送到另外一个消费端。

RabbitMQ中没有消息超时的概念，只有当消费端关闭或奔溃时，RabbitMQ才会重新分发消息。

微调下Receive中的代码逻辑：
```
 //5. 构造消费者实例
 var consumer = new EventingBasicConsumer(channel);
 //6. 绑定消息接收后的事件委托
 consumer.Received += (model, ea) =>
 {
     var message = Encoding.UTF8.GetString(ea.Body);
     Console.WriteLine(" [x] Received {0}", message);
     Thread.Sleep(6000);//模拟耗时
     Console.WriteLine(" [x] Done");
     // 7. 发送消息确认信号（手动消息确认）
     channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
 };
 //8. 启动消费者
 //autoAck:true；自动进行消息确认，当消费端接收到消息后，就自动发送ack信号，不管消息是否正确处理完毕
 //autoAck:false；关闭自动消息确认，通过调用BasicAck方法手动进行消息确认
 channel.BasicConsume(queue: "hello", autoAck: false, consumer: consumer);
```

主要改动的是将 `autoAck:true`修改为`autoAck:fasle`，以及在消息处理完毕后手动调用`BasicAck`方法进行手动消息确认。

![](http://upload-images.jianshu.io/upload_images/2799767-d781dd054f6d2c77.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

从图中可知，消息发送端连续发送4条消息，其中消费端1先被分配处理第一条消息，消费端2被循环分配第二条消息，第三条消息由于没有空闲消费者仍然在队列中。
在消费端2未处理完第一条消息之前，手动中断（ctrl+c）。我们可以发现RabbitMQ在下一次分发时，会优先将被中断的消息分发给消费端1处理。

## 3.4. 消息持久化
消息确认确保了即使消费端异常，消息也不会丢失能够被重新分发处理。但是如果RabbitMQ服务端异常，消息依然会丢失。除非我们指定`durable:true`，否则当RabbitMQ退出或奔溃时，消息将依然会丢失。通过指定`durable:true`，并指定`Persistent=true`，来告知RabbitMQ将消息持久化。
```
//send.cs
//4. 申明队列(指定durable:true,告知rabbitmq对消息进行持久化)
channel.QueueDeclare(queue: "hello", durable: true, exclusive: false, autoDelete: false, arguments
//将消息标记为持久性 - 将IBasicProperties.SetPersistent设置为true
var properties = channel.CreateBasicProperties();
properties.Persistent = true;
//5. 构建byte消息数据包
string message = args.Length > 0 ? args[0] : "Hello RabbitMQ!";
var body = Encoding.UTF8.GetBytes(message);
//6. 发送数据包(指定basicProperties)
channel.BasicPublish(exchange: "", routingKey: "hello", basicProperties: properties, body: body);
```
将消息标记为持久性不能完全保证消息不会丢失。虽然它告诉RabbitMQ将消息保存到磁盘，但是当RabbitMQ接受消息并且还没有保存时​​，仍然有一个很短的时间窗口。RabbitMQ 可能只是将消息保存到了缓存中，并没有将其写入到磁盘上。持久化是不能够一定保证的，但是对于一个简单任务队列来说已经足够。如果需要确保消息队列的持久化，可以使用[publisher confirms](https://www.rabbitmq.com/confirms.html).

## 3.5. 公平分发
RabbitMQ的消息分发默认按照消费端的数量，按顺序循环分发。这样仅是确保了消费端被平均分发消息的数量，但却忽略了消费端的闲忙情况。这就可能出现某个消费端一直处理耗时任务处于阻塞状态，某个消费端一直处理一般任务处于空置状态，而只是它们分配的任务数量一样。

![](http://upload-images.jianshu.io/upload_images/2799767-d2bb1f2ac63fdb15.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

但我们可以通过`channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);`
设置`prefetchCount : 1`来告知RabbitMQ，在未收到消费端的消息确认时，不再分发消息，也就确保了当消费端处于忙碌状态时，不再分配任务。
```
//Receive.cs
//4. 申明队列
channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);
//设置prefetchCount : 1来告知RabbitMQ，在未收到消费端的消息确认时，不再分发消息，也就确保了当消费端处于忙碌状态时
channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
```

这时你需要注意的是如果所有的消费端都处于忙碌状态，你的队列可能会被塞满。你需要注意这一点，要么添加更多的消费端，要么采取其他策略。

# 4. Exchange
细心的你也许发现上面的demo，生产者和消费者直接是通过相同队列名称进行匹配衔接的。消费者订阅某个队列，生产者创建消息发布到队列中，队列再将消息转发到订阅的消费者。这样就会有一个局限性，即消费者一次只能发送消息到某一个队列。

那消费者如何才能发送消息到多个消息队列呢？
RabbitMQ提供了**Exchange**，它类似于路由器的功能，它用于对消息进行路由，将消息发送到多个队列上。Exchange一方面从生产者接收消息，另一方面将消息推送到队列。但exchange必须知道如何处理接收到的消息，是将其附加到特定队列还是附加到多个队列，还是直接忽略。而这些规则由exchange type定义，exchange的原理如下图所示。
![Exchange](http://upload-images.jianshu.io/upload_images/2799767-0b4fba202e525745.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

常见的exchange type 有以下几种：
* direct（明确的路由规则：消费端绑定的队列名称必须和消息发布时指定的路由名称一致）
* topic （模式匹配的路由规则：支持通配符）
* fanout （消息广播，将消息分发到exchange上绑定的所有队列上）

下面我们就来一一这介绍它们的用法。

## 4.1 fanout
本着先易后难的思想，我们先来了解下**fanout**的广播路由机制。fanout的路由机制如下图，即发送到 fanout 类型exchange的消息都会分发到所有绑定该exchange的队列上去。

![fanout 路由机制](http://upload-images.jianshu.io/upload_images/2799767-3afd7b874221a9a2.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)


生产者示例代码：
```
// 生成随机队列名称
var queueName = channel.QueueDeclare().QueueName;
//使用fanout exchange type，指定exchange名称
channel.ExchangeDeclare(exchange: "fanoutEC", type: "fanout");
var message = "Hello Rabbit!";
var body = Encoding.UTF8.GetBytes(message);
//发布到指定exchange，fanout类型无需指定routingKey
channel.BasicPublish(exchange: "fanoutEC", routingKey: "", basicProperties: null, body: body);
```
消费者示例代码：
```
//申明fanout类型exchange
channel.ExchangeDeclare (exchange: "fanoutEC", type: "fanout");
//申明随机队列名称
var queuename = channel.QueueDeclare ().QueueName;
//绑定队列到指定fanout类型exchange，无需指定路由键
channel.QueueBind (queue : queuename, exchange: "fanoutEC", routingKey: "");
```

## 4.2. direct
direct相对于fanout就属于完全匹配、单播的模式，路由机制如下图，即队列名称和消息发送时指定的路由完全匹配时，消息才会发送到指定队列上。
![direct路由机制](http://upload-images.jianshu.io/upload_images/2799767-6c78ab57fe06c6ce.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

生产者示例代码：
```
// 生成随机队列名称
var queueName = channel.QueueDeclare().QueueName;
//使用direct exchange type，指定exchange名称
channel.ExchangeDeclare(exchange: "directEC", type: "direct");
var message = "Hello Rabbit!";
var body = Encoding.UTF8.GetBytes(message);
//发布到direct类型exchange，必须指定routingKey
channel.BasicPublish(exchange: "directEC", routingKey: "green", basicProperties: null, body: body);
```
消费者示例代码：
```
//申明direct类型exchange
channel.ExchangeDeclare (exchange: "directEC", type: "direct");
//绑定队列到direct类型exchange，需指定路由键routingKey
channel.QueueBind (queue : green, exchange: "directEC", routingKey: "green");
```

## 4.3. topic
topic是direct的升级版，是一种模式匹配的路由机制。它支持使用两种通配符来进行模式匹配：符号**`#`**和符号**`*`**。其中**`*`**匹配一个单词， **`#`**则表示匹配0个或多个单词，单词之间用**`.`**分割。如下图所示。
![topic路由机制](http://upload-images.jianshu.io/upload_images/2799767-3196a1c3880b3466.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

生产者示例代码：
```
// 生成随机队列名称
var queueName = channel.QueueDeclare().QueueName;
//使用topic exchange type，指定exchange名称
channel.ExchangeDeclare(exchange: "topicEC", type: "topic");
var message = "Hello Rabbit!";
var body = Encoding.UTF8.GetBytes(message);
//发布到topic类型exchange，必须指定routingKey
channel.BasicPublish(exchange: "topicEC", routingKey: "first.green.fast", basicProperties: null, body: body);
```
消费者示例代码：
```
//申明topic类型exchange
channel.ExchangeDeclare (exchange: "topicEC", type: "topic");
//申明随机队列名称
var queuename = channel.QueueDeclare ().QueueName;
//绑定队列到topic类型exchange，需指定路由键routingKey
channel.QueueBind (queue : queuename, exchange: "topicEC", routingKey: "#.*.fast");
```

# 5. RPC
RPC——Remote Procedure Call，远程过程调用。
那RabbitMQ如何进行远程调用呢？示意图如下：
![RPC机制](http://upload-images.jianshu.io/upload_images/2799767-121e20f9b512c406.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)
第一步，主要是进行远程调用的客户端需要指定接收远程回调的队列，并申明消费者监听此队列。
第二步，远程调用的服务端除了要申明消费端接收远程调用请求外，还要将结果发送到客户端用来监听回调结果的队列中去。

远程调用客户端：
```
 //申明唯一guid用来标识此次发送的远程调用请求
 var correlationId = Guid.NewGuid().ToString();
 //申明需要监听的回调队列
 var replyQueue = channel.QueueDeclare().QueueName;
 var properties = channel.CreateBasicProperties();
 properties.ReplyTo = replyQueue;//指定回调队列
 properties.CorrelationId = correlationId;//指定消息唯一标识
 string number = args.Length > 0 ? args[0] : "30";
 var body = Encoding.UTF8.GetBytes(number);
 //发布消息
 channel.BasicPublish(exchange: "", routingKey: "rpc_queue", basicProperties: properties, body: body);
 Console.WriteLine($"[*] Request fib({number})");
 // //创建消费者用于处理消息回调（远程调用返回结果）
 var callbackConsumer = new EventingBasicConsumer(channel);
 channel.BasicConsume(queue: replyQueue, autoAck: true, consumer: callbackConsumer);
 callbackConsumer.Received += (model, ea) =>
 {
      //仅当消息回调的ID与发送的ID一致时，说明远程调用结果正确返回。
     if (ea.BasicProperties.CorrelationId == correlationId)
     {
         var responseMsg = $"Get Response: {Encoding.UTF8.GetString(ea.Body)}";
         Console.WriteLine($"[x]: {responseMsg}");
     }
 };
```

远程调用服务端：
```
//申明队列接收远程调用请求
channel.QueueDeclare(queue: "rpc_queue", durable: false,
    exclusive: false, autoDelete: false, arguments: null);
var consumer = new EventingBasicConsumer(channel);
Console.WriteLine("[*] Waiting for message.");
//请求处理逻辑
consumer.Received += (model, ea) =>
{
    var message = Encoding.UTF8.GetString(ea.Body);
    int n = int.Parse(message);
    Console.WriteLine($"Receive request of Fib({n})");
    int result = Fib(n);
    //从请求的参数中获取请求的唯一标识，在消息回传时同样绑定
    var properties = ea.BasicProperties;
    var replyProerties = channel.CreateBasicProperties();
    replyProerties.CorrelationId = properties.CorrelationId;
    //将远程调用结果发送到客户端监听的队列上
    channel.BasicPublish(exchange: "", routingKey: properties.ReplyTo,
        basicProperties: replyProerties, body: Encoding.UTF8.GetBytes(result.ToString()));
    //手动发回消息确认
    channel.BasicAck(ea.DeliveryTag, false);
    Console.WriteLine($"Return result: Fib({n})= {result}");
};
channel.BasicConsume(queue: "rpc_queue", autoAck: false, consumer: consumer);
```

# 6. 总结
基于上面的demo和对几种不同exchange路由机制的学习，我们发现RabbitMQ主要是涉及到以下几个核心概念：
1. Publisher：生产者，消息的发送方。
2. Connection：网络连接。
3. Channel：信道，多路复用连接中的一条独立的双向数据流通道。
4. Exchange：交换器（路由器），负责消息的路由到相应队列。
5. Binding：队列与交换器间的关联绑定。消费者将关注的队列绑定到指定交换器上，以便Exchange能准确分发消息到指定队列。
6. Queue：队列，消息的缓冲存储区。
7. Virtual Host：虚拟主机，虚拟主机提供资源的逻辑分组和分离。包含连接，交换，队列，绑定，用户权限，策略等。
8. Broker：消息队列的服务器实体。
9. Consumer：消费者，消息的接收方。 

这次作为入门就讲到这里，下次我们来讲解下**EventBus + RabbitMQ**如何实现事件的分发。

>参考资料：
[RabbitMQ Tutorials](http://www.rabbitmq.com/getstarted.html)
[Demo路径——RabbitMQ](https://github.com/yanshengjie/RabbitMQ)
