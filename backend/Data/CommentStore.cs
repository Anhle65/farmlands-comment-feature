using CommentApi.Models;

namespace CommentApi.Data;

/// In-memory comment store, seeded with mock data at construction.
/// Registered as a singleton so the seed is created once per process.

public class CommentStore
{
    private const string AuthorBob = "11111111-1111-1111-1111-111111111111";
    private const string AuthorEve = "22222222-2222-2222-2222-222222222222";
    private const string AuthorSam = "33333333-3333-3333-3333-333333333333";
    private const string AuthorAlice = "44444444-4444-4444-4444-444444444444";
    private readonly List<Comment> _comments;

    public CommentStore()
    {
        // Timestamps are relative to startup so the 5-minute window stays testable later.
        var now = DateTimeOffset.UtcNow;
        _comments =
        [
            new Comment
            {
                Id = 1, AuthorId = AuthorBob, AuthorName = "Bob",
                Content = "What a nice weather for our community picnic!",
                CreatedAt = now.AddDays(-3), ParentId = null, UpdatedAt = null, IsDeleted = false,
            },
            new Comment
            {
                Id = 2, AuthorId = AuthorEve, AuthorName = "Eve",
                Content = "Agreed, it's a beautiful day for a walk in the park.",
                CreatedAt = now.AddDays(-2), ParentId = 1, UpdatedAt = null, IsDeleted = false,
            },
            new Comment
            {
                Id = 3, AuthorId = AuthorAlice, AuthorName = "Alice",
                Content = "Any other activities planned for the weekend?",
                CreatedAt = now.AddDays(-1), ParentId = null, UpdatedAt = now.AddHours(-23), IsDeleted = false,
            },
            new Comment
            {
                Id = 4, AuthorId = AuthorSam, AuthorName = "Sam",
                Content = "Weather forecast says it might rain on the weekend.",
                CreatedAt = now.AddHours(-5), ParentId = null, UpdatedAt = null, IsDeleted = true,
            }
        ];
    }

    public IReadOnlyList<Comment> GetAll() => _comments;
}
