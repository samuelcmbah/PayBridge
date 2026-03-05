namespace PayBridge.Infrastructure.Messaging
{
    public record RabbitMqSettings
    {
        public string Host { get; init; } = default!;
        public int Port { get; init; }
        public string VirtualHost { get; init; } = default!;
        public string Username { get; init; } = default!;
        public string Password { get; init; } = default!;
    }
}