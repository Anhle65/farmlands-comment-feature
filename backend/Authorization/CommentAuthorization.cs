using CommentApi.Models;

namespace CommentApi.Authorization;

/// <summary>
/// Authorization rules for modifying a comment. Knows nothing about the store, HTTP, or
/// the action being attempted — only whether a given requester is allowed to modify a
/// given comment given the 5-minute window. Shared between the edit and delete endpoints.
/// </summary>
public static class CommentAuthorization
{
    public static readonly TimeSpan EditWindow = TimeSpan.FromMinutes(5);

    // CheckCanModify(Comment comment, string requesterId) → bool
    // Implementation comes after CommentAuthorizationTests goes red on it.
    public static bool CheckCanModify(Comment comment, string requesterId)
    {
        var isOwner = comment.AuthorId == requesterId;
        var withinWindow = DateTimeOffset.UtcNow - comment.CreatedAt <= EditWindow;
        return isOwner && withinWindow;
    }
}
