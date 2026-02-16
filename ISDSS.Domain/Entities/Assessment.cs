namespace ISDSS.Domain.Entities;

public class Assessment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public decimal Score { get; set; }
    public bool Passed { get; set; }
    public DateTime IssuedAt { get; set; }
}
