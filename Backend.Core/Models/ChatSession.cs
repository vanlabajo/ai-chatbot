namespace Backend.Core.Models
{
    public class ChatSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string UserId { get; set; }
        public string? Title { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<ChatMessage> Messages { get; set; } = [];
    }
}
