using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ChatApplication.Server.Data;

namespace ChatApplication.Server.Models;

public class ChatMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string SenderId { get; set; } = string.Empty;

    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    [Required]
    public string ConversationId { get; set; } = string.Empty;

    [Required]
    public string Text { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeliveredAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public MessageStatus Status { get; set; } = MessageStatus.Sent;

    // Navigation properties
    [ForeignKey(nameof(SenderId))]
    public virtual ApplicationUser? Sender { get; set; }

    [ForeignKey(nameof(ReceiverId))]
    public virtual ApplicationUser? Receiver { get; set; }

    [ForeignKey(nameof(ConversationId))]
    public virtual Conversation? Conversation { get; set; }
}
