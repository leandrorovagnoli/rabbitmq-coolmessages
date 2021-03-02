using System;

namespace CoolMessages.Models
{
    /// <summary>
    /// Represents a message from a user (FromId), to a user (ToId), with the Content.
    /// </summary>
    public class MessageInputModel
    {
        public int FromId { get; set; }
        public int ToId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
