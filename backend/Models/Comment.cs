namespace CommentApi.Models;

/// <summary>A blog comment. Designed to cover the whole feature, not just this slice.</summary>
public class Comment
{
    public int Id { get; set; }

    /// <summary>Browser-generated identifier; determines who may edit/delete the comment.</summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>Display name from the "enter your name" flow.</summary>
    public string AuthorName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>UTC. Drives newest-first sort and the 5-minute edit/delete window.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Null = top-level; set = reply. Only one level of nesting is allowed.</summary>
    public int? ParentId { get; set; }

    /// <summary>Null until the comment is edited.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Soft delete. True = shown as [deleted] but the row is kept so replies survive.</summary>
    public bool IsDeleted { get; set; }
}
