using ISDSS.Application.Abstractions;
using ISDSS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISDSS.Infrastructure.Persistence;

public class StudentRepository : IStudentRepository
{
    private readonly AppDbContext _ctx;

    public StudentRepository(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<List<Student>> GetAllAsync()
    {
       return await _ctx.Students.ToListAsync();
    }

    public async Task AddAsync(Student student)
    {
        await _ctx.Students.AddAsync(student);
        await _ctx.SaveChangesAsync();
    }
}
