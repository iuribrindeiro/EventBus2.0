namespace EventBus.RabbitMQ
{
    public interface IConcurrentConsumerHandler
    {
        public void ConcurrentlySubscribe(string concurenceId, string queueName, string eventName);
    }
}