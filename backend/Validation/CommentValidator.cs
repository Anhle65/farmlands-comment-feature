using CommentApi.Models;

namespace CommentApi.Validation;

/// Pure structural validation for a <see cref="Comment"/>. Knows nothing about the store
/// or HTTP — it only checks the shape of the input. Referential checks (e.g. "does the
/// parent exist") belong in the store, not here.
public static class CommentValidator
{
    public const int MaxContentLength = 1000;

    /// <summary>Returns null when the comment is valid; otherwise an error message.</summary>
    public static string? Validate(Comment comment)
    {
        if (string.IsNullOrWhiteSpace(comment.AuthorId))   return "AuthorId is required and rendered by browser";
        if (string.IsNullOrWhiteSpace(comment.AuthorName)) return "AuthorName is required";
        if (string.IsNullOrWhiteSpace(comment.Content))    return "Content is required";
        if (comment.Content.Length > MaxContentLength)     return $"Content must be {MaxContentLength} characters or fewer";
        return null;
    }
}
