namespace Backend.Core.Models
{
    public class ChatSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? Subject { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<ChatMessage> Messages { get; set; } = [];
    }
}
