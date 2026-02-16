using ISDSS.Domain.Entities;

namespace ISDSS.Application.Abstractions;

public interface IStudentRepository
{
    Task<List<Student>> GetAllAsync();
    Task AddAsync(Student student);
}
