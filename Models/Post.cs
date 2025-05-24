using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Solivio.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public string Location { get; set; } = null!;

        [Required]
        public string Caption { get; set; } = null!;

        public DateTime DatePosted { get; set; } = DateTime.UtcNow;

        public List<PostImage>? Images { get; set; }
    }
}
