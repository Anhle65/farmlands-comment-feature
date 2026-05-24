using CommentApi.Models;
namespace CommentApi.Tests.Unit;
using CommentApi.Authorization; 
public class CommentAuthorizationTests
{
    private const string ValidAuthorId = "55555555-5555-5555-5555-555555555555";
    private const string InvalidAuthorId = "00000000-0000-0000-0000-000000000000";
    private const string ValidAuthorName = "Anh";
    
    [Fact]
    public void CheckCanModify_OwnerWithinWindow_ReturnsTrue()
    {
        var comment = new Comment
        {
            Id = 1,
            AuthorId = ValidAuthorId,
            AuthorName = ValidAuthorName,
            Content = "Anything.",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),   // 1 minute ago — inside window
        };
        var canModify = CommentAuthorization.CheckCanModify(comment, ValidAuthorId);
        Assert.True(canModify);
    }

    [Fact]
    public void CheckCanModify_OwnerExactlyAtWindowBoundary_ReturnsTrue()
    {
        var comment = new Comment
        {
            Id = 1,
            AuthorId = ValidAuthorId,       
            AuthorName = ValidAuthorName,
            Content = "Anything.",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),   // exactly at boundary
        };
        var canModify = CommentAuthorization.CheckCanModify(comment, ValidAuthorId);
        Assert.True(canModify);
    }

    [Fact]
    public void CheckCanModify_OwnerPastWindow_ReturnsFalse()
    {
        var comment = new Comment
        {
            Id = 1,
            AuthorId = ValidAuthorId,
            AuthorName = ValidAuthorName,
            Content = "Anything.",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-6),   // 6 minutes ago — outside window
        };
        var canModify = CommentAuthorization.CheckCanModify(comment, ValidAuthorId);
        Assert.False(canModify);
    }

    [Fact]
    public void CheckCanModify_NotOwner_ReturnsFalse()
    {
        var comment = new Comment
        {
            Id = 1,
            AuthorId = InvalidAuthorId,
            AuthorName = ValidAuthorName,
            Content = "Anything.",
            CreatedAt = DateTimeOffset.UtcNow,   // right now — inside window, but requester isn't owner
        };
        var canModify = CommentAuthorization.CheckCanModify(comment, ValidAuthorId);
        Assert.False(canModify);
    }
}