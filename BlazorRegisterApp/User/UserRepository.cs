using MySql.Data.MySqlClient;
using RabbitMQ.Client;
using System;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace BlazorRegisterApp.User
{
    public class UserRepository
    {
        private readonly IConfiguration _configuration;

        public UserRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public user AddUser(user usr)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Registrations (Name, Email, Password, Contact, Image, Role, RoleDescription) VALUES (@Name, @Email, @Password, @Contact, @Image, @Role, @RoleDescription)";
                    command.Parameters.AddWithValue("@Name", usr.Name);
                    command.Parameters.AddWithValue("@Email", usr.Email);
                    command.Parameters.AddWithValue("@Password", usr.Password);
                    command.Parameters.AddWithValue("@Contact", usr.Contact);
                    command.Parameters.AddWithValue("@Image", usr.Image);
                    command.Parameters.AddWithValue("@Role", usr.Role);
                    command.Parameters.AddWithValue("@RoleDescription", usr.RoleDescription);
                    int i = command.ExecuteNonQuery();
                    if (i > 0)
                    {
                        usr.Id = (int)command.LastInsertedId;
                        // After inserting the user, publish the user logged-in data to RabbitMQ
                        PublishUserLoggedInMessage (usr);
                        return usr;
                    }
                }
            }
            return null;
        }

        public user AuthenticateUser(user usr)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Registrations WHERE Email = @Email AND Password = @Password";
                    command.Parameters.AddWithValue("@Email", usr.Email);
                    command.Parameters.AddWithValue("@Password", usr.Password);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var uesr = new user
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                Email = reader["Email"].ToString(),
                                Password = reader["Password"].ToString(),
                                Contact = reader["Contact"].ToString(),
                                Image = reader["Image"].ToString(),
                                Role = reader["Role"].ToString(),
                                RoleDescription = reader["RoleDescription"].ToString()
                            };

                            // After successfully authenticating the user, publish the user logged-in data to RabbitMQ
                            PublishUserLoggedInMessage(usr);

                            return uesr;
                        }
                    }
                }
            }

            return null;
        }

        private void PublishUserLoggedInMessage(user usr)
        {
            // Create a connection to RabbitMQ
           // var factory = new ConnectionFactory() { HostName = "http://52.91.196.195/" };
            var RabbitconnectionString = _configuration.GetConnectionString("RabbitMQConnection");

            var factory = new ConnectionFactory()
            {
                Uri = new Uri(RabbitconnectionString)
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Declare a queue
                channel.QueueDeclare(queue: "user_login_queue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                // Convert user data to JSON
                var jsonUserData = JsonConvert.SerializeObject(usr);

                // Convert JSON data to bytes
                var body = Encoding.UTF8.GetBytes(jsonUserData);

                // Publish the message to RabbitMQ
                channel.BasicPublish(exchange: "",
                                     routingKey: "user_login_queue",
                                     basicProperties: null,
                                     body: body);

                Console.WriteLine(" [x] Sent user login message");
            }
        }
    }
}
