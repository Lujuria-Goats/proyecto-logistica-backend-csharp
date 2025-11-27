namespace ApexVision.Backend
{
    public class Worker : BackgroundService
    {
        private readonly CommandConsumer _consumer;

        public Worker(CommandConsumer consumer)
        {
            _consumer = consumer;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.StartListening();
            // Mantenemos el servicio vivo esperando
            return Task.Delay(-1, stoppingToken);
        }
    }
}