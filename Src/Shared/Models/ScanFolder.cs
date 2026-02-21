using System.ComponentModel.DataAnnotations;

namespace ProjectDashboard.Shared.Models;

public class ScanFolder
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Path { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
