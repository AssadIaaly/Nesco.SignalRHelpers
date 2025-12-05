using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ChatApplication.Server.Data;

namespace ChatApplication.Server.Models;

public class Conversation
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string User1Id { get; set; } = string.Empty;

    [Required]
    public string User2Id { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastMessageAt { get; set; }

    public string? LastMessagePreview { get; set; }

    // Navigation properties
    [ForeignKey(nameof(User1Id))]
    public virtual ApplicationUser? User1 { get; set; }

    [ForeignKey(nameof(User2Id))]
    public virtual ApplicationUser? User2 { get; set; }

    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    // Helper to generate consistent conversation ID for two users
    public static string GenerateId(string userId1, string userId2)
    {
        var ids = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
        return $"{ids[0]}_{ids[1]}";
    }
}
