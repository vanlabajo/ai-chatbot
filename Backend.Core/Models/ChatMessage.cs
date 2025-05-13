namespace Backend.Core.Models
{
    public class ChatMessage
    {
        public required ChatRole Role { get; set; }
        public required string Content { get; set; }
    }

    public class ChatRole
    {
        public static readonly ChatRole User = new("user");
        public static readonly ChatRole Assistant = new("assistant");
        public static readonly ChatRole System = new("system");

        public string Value { get; }

        private ChatRole(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;

        // Optional: Equality overrides for easier comparisons
        public override bool Equals(object? obj) => obj is ChatRole other && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(ChatRole left, ChatRole right) => left.Equals(right);
        public static bool operator !=(ChatRole left, ChatRole right) => !(left == right);
    }
}
