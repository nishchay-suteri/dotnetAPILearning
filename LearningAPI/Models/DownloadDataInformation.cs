using System.ComponentModel.DataAnnotations;

namespace LearningAPI.Models;

public class DownloadDataInformation
{
    public int Id { get; set; }
    [Required]
    public string? DownloadUrl { get; set; }
    [Required]
    public TaskStatusValue TaskStatus { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
}