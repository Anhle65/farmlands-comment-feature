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

    // `now` is injected so tests pin a single instant rather than racing two
    // DateTimeOffset.UtcNow calls across the window boundary. Callers in
    // production pass DateTimeOffset.UtcNow (or a TimeProvider reading).
    public static bool CheckCanModify(Comment comment, string requesterId, string requesterName, DateTimeOffset now)
    {
        var isOwner = comment.AuthorId == requesterId;
        var isAuthor = comment.AuthorName == requesterName;
        var withinWindow = now - comment.CreatedAt <= EditWindow;
        return isOwner && isAuthor && withinWindow;
    }
}
