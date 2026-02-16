using System.ComponentModel.DataAnnotations;

namespace ISDSS.Domain.Entities;

public class Student
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = "";

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    public DateTime? LastTrainingDate { get; set; }

    [Range(0, 100)]
    public decimal CompliancePercent { get; set; } = 0m;
}
