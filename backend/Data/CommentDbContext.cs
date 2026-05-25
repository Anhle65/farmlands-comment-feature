using CommentApi.Models;
using Microsoft.EntityFrameworkCore;
namespace CommentApi.Data;
public class CommentDbContext: DbContext
{
    public DbSet<Comment> Comments { get; set; }

    public CommentDbContext(DbContextOptions<CommentDbContext> options) : base(options) {}

}