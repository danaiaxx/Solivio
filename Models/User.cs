using System.ComponentModel.DataAnnotations;

namespace Solivio.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Username { get; set; }
        public required string SecurityQuestion { get; set; }
        
        [MaxLength(100)]
        public required string SecurityAnswer { get; set; }

        public string? ProfileImage { get; set; }

        public byte[]? ProfileImageData { get; set; }
        public string? ProfileImageContentType { get; set; }

    }
}