using Microsoft.EntityFrameworkCore;
using Solivio.Models;

namespace Solivio.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Post> Posts { get; set; }

        public DbSet<PostImage> PostImages { get; set; }
    }
}