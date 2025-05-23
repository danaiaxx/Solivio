using Solivio.Models;
using System.ComponentModel.DataAnnotations;

public class PostImage
{
    [Key]
    public int Id { get; set; }

    public byte[]? ImageData { get; set; }

    public string? ContentType { get; set; }

    // Foreign key to Post
    public int PostId { get; set; }
    public Post? Post { get; set; }
}
