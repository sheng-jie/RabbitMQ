using System;
using System.Linq;
using System.Text;
using RabbitMQ.Client;

namespace EmitLogDirect {
    class Program {
    static void Main (string[] args) {
            var factory = new ConnectionFactory () { HostName = "localhost" };
            using (var connection = factory.CreateConnection ())
            using (var channel = connection.CreateModel ()) {
                channel.ExchangeDeclare (exchange: "direct_logs",
                    type: "direct");

                var logLevel = (args.Length > 0) ? args[0] : "info";
                var message = (args.Length > 1) ?
                    string.Join (" ", args.Skip (1).ToArray ()) :
                    "Hello RabbitMQ!";
                var body = Encoding.UTF8.GetBytes (message);
                channel.BasicPublish (exchange: "direct_logs",
                    routingKey : logLevel,
                    basicProperties : null,
                    body : body);
                Console.WriteLine (" [x] Sent '{0}':'{1}'", logLevel, message);
            }
        }
    }
}