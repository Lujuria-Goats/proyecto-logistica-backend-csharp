using RabbitMQ.Client;

namespace ApexVision.Backend
{
    public class RabbitMqConnection : IDisposable
    {
        private readonly IConnection _connection;
        public RabbitMQ.Client.IModel Channel { get; }

        public RabbitMqConnection(IConfiguration configuration)
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"],
                UserName = configuration["RabbitMQ:UserName"],
                Password = configuration["RabbitMQ:Password"],
                VirtualHost = configuration["RabbitMQ:VirtualHost"]
            };

            // Conectar al servidor en la nube
            _connection = factory.CreateConnection();
            Channel = _connection.CreateModel();
            
            // Asegurar que la cola existe
            Channel.QueueDeclare(
                queue: configuration["RabbitMQ:QueueName"],
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        public void Dispose()
        {
            Channel?.Close();
            _connection?.Close();
        }
    }
}