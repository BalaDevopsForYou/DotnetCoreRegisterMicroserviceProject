using System;
using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory() { HostName = "localhost" };
using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    channel.QueueDeclare(queue: "user_login_queue",
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);

}
