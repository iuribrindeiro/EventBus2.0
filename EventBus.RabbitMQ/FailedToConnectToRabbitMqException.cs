using System;

namespace EventBus.RabbitMQ
{
    public class FailedToConnectToRabbitMqException : Exception
    {
        public FailedToConnectToRabbitMqException() : base("Failed to connect to RabbitMq")
        {

        }

        public FailedToConnectToRabbitMqException(Exception innerException) : base("Failed to connect to RabbitMq", innerException)
        {

        }
    }
}