namespace TICinema.Notification.Configurations;

public class RabbitMqSettings
{
    public string Url { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
}