using ISDSS.Domain.Entities;

namespace ISDSS.Application.Abstractions;

public interface IExportService
{
    Task ExportStudentsAsync(string path);
    Task<List<Student>> ImportStudentsAsync(string path);
}
