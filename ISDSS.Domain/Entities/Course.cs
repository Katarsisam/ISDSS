namespace ISDSS.Domain.Entities;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsMandatory { get; set; } = true;
    public byte Difficulty { get; set; } = 1;
    public int? AssignedUserId { get; set; }
    public UserAccount? AssignedUser { get; set; }
}
