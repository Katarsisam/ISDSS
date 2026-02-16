using System.ComponentModel.DataAnnotations;

namespace ISDSS.Domain.Entities;

public enum UserAccessLevel
{
    Auditor = 0,
    Instructor = 1,
    Admin = 2
}

public class UserAccount
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Login { get; set; } = "";

    [Required, MaxLength(200)]
    public string PasswordHash { get; set; } = "";

    [MaxLength(100)]
    public string RoleTitle { get; set; } = "";

    public UserAccessLevel AccessLevel { get; set; } = UserAccessLevel.Auditor;

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
