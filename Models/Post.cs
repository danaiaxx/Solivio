using System.ComponentModel.DataAnnotations;

namespace Solivio.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public string Caption { get; set; }

        public DateTime DatePosted { get; set; } = DateTime.UtcNow;

        public List<PostImage>? Images { get; set; }
    }
}
