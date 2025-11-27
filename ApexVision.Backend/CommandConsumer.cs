using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ApexVision.Backend
{
    public class CommandConsumer
    {
        private readonly RabbitMqConnection _connection;
        private readonly CommandExecutor _executor;
        private readonly string _queueName;

        public CommandConsumer(RabbitMqConnection connection, CommandExecutor executor, IConfiguration config)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _queueName = config?["RabbitMQ:QueueName"] ?? throw new ArgumentNullException("RabbitMQ:QueueName no está configurado");
        }

        public void StartListening()
        {
            var consumer = new EventingBasicConsumer(_connection.Channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                Console.WriteLine($"[RABBIT] Recibido: {message}");
                await _executor.ExecuteCommandAsync(message);
                
                // Confirmar mensaje procesado
                _connection.Channel.BasicAck(ea.DeliveryTag, false);
            };

            _connection.Channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
            Console.WriteLine($"[*] Escuchando en cola: {_queueName}");
        }
    }
}