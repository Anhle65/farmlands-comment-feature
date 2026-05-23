using CommentApi.Models;
using CommentApi.Validation;

namespace CommentApi.Tests.Unit;

public class CommentValidatorTests
{
    // Shared defaults so each test's "interesting" value stands out (e.g. Content = "" in
    // the EmptyContent test) instead of being buried in noise.
    private const string ValidAuthorId = "55555555-5555-5555-5555-555555555555";
    private const string ValidAuthorName = "Anh";
    private const string ValidContent = "A valid comment.";

    // ───────────────────────────────────────────────────────────────────
    // Happy paths
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidTopLevelComment_ReturnsNull()
    {
        var error = CommentValidator.Validate(new Comment
        {
            AuthorId = ValidAuthorId,
            AuthorName = ValidAuthorName,
            Content = ValidContent,
        });

        Assert.Null(error);   // null = the validator said "OK"
    }

    [Fact]
    public void Validate_ValidReply_ReturnsNull()
    {
        // ParentId existence is the store's concern; the validator only checks the
        // shape of the input. Any non-null int is fine here.
        var error = CommentValidator.Validate(new Comment
        {
            AuthorId = ValidAuthorId,
            AuthorName = ValidAuthorName,
            Content = ValidContent,
            ParentId = 1,
        });

        Assert.Null(error);
    }

    // ───────────────────────────────────────────────────────────────────
    // Content rules
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyContent_ReturnsError()
    {
        var error = CommentValidator.Validate(new Comment
        {
            AuthorId = ValidAuthorId,
            AuthorName = ValidAuthorName,
            Content = "",
        });

        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_WhitespaceContent_ReturnsError()
    {
        var error = CommentValidator.Validate(new Comment
        {
            AuthorId = ValidAuthorId,
            AuthorName = ValidAuthorName,
            Content = "   ",
        });

        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_ContentExactly1000Chars_ReturnsNull()
    {
        // Boundary, inclusive — exactly the max length is VALID.
        var error = CommentValidator.Validate(new Comment
        {
            AuthorId = ValidAuthorId,
            AuthorName = ValidAuthorName,
            Content = new string('a', 1000),
        });

        Assert.Null(error);
    }

    [Fact]
    public void Validate_Content1001Chars_ReturnsError()
    {
        // One char over the limit — INVALID.
        var error = CommentValidator.Validate(new Comment
        {
            AuthorId = ValidAuthorId,
            AuthorName = ValidAuthorName,
            Content = new string('a', 1001),
        });

        Assert.NotNull(error);
    }

    // ───────────────────────────────────────────────────────────────────
    // AuthorName rules
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyAuthorName_ReturnsError()
    {
        var error = CommentValidator.Validate(new Comment
        {
            AuthorId = ValidAuthorId,
            AuthorName = "",
            Content = ValidContent,
        });

        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_WhitespaceAuthorName_ReturnsError()
    {
        var error = CommentValidator.Validate(new Comment
        {
            AuthorId = ValidAuthorId,
            AuthorName = "   ",
            Content = ValidContent,
        });

        Assert.NotNull(error);
    }

    // ───────────────────────────────────────────────────────────────────
    // AuthorId rules
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyAuthorId_ReturnsError()
    {
        var error = CommentValidator.Validate(new Comment
        {
            AuthorId = "",
            AuthorName = ValidAuthorName,
            Content = ValidContent,
        });

        Assert.NotNull(error);
    }
}
