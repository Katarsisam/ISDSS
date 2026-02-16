using ISDSS.Domain.Entities;

namespace ISDSS.Application.Abstractions;

public interface IStudentService
{
    Task<List<Student>> GetAllAsync();
    Task<Student> AddAsync(string fullName, string? email);
    decimal ComputeRisk(Student s);
}
