using CoolMessages.Models;
using CoolMessages.Options;
using CoolMessages.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoolMessages.Consumers
{
    /// <summary>
    /// Service that will run when the application starts.
    /// It will be responsible for "listening" for new messages from the RabbitMq Queue.
    /// </summary>
    public class ProcessMessageConsumer : BackgroundService
    {
        private readonly RabbitMqConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Connection to RabbitMq, channel creation, queue declaration (it will create the queue if it doesn't exist)
        /// </summary>
        /// <param name="option"></param>
        public ProcessMessageConsumer(IOptions<RabbitMqConfiguration> option, IServiceProvider serviceProvider)
        {
            _configuration = option.Value;
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory
            {
                HostName = _configuration.Host
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(
                queue: _configuration.Queue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Definition of an EventingBasicConsumer object, which will be responsible 
            // for the configuration of the consumption-related events, and for their effective start
            var consumer = new EventingBasicConsumer(_channel);

            // Definition of the Received event, where you have access to the message 
            // received in the queue, through the eventArgs.Body property
            consumer.Received += (sender, eventArgs) =>
            {
                var contentArray = eventArgs.Body.ToArray();
                var contentString = Encoding.UTF8.GetString(contentArray);
                var message = JsonConvert.DeserializeObject<MessageInputModel>(contentString);

                NotifyUser(message);

                // Acknowledges the message as delivered 
                _channel.BasicAck(eventArgs.DeliveryTag, false);
            };

            // Start of consumption
            _channel.BasicConsume(_configuration.Queue, false, consumer);

            return Task.CompletedTask;
        }

        public void NotifyUser(MessageInputModel message)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                notificationService.NotifyUser(message.FromId, message.ToId, message.Content);
            }
        }
    }
}
