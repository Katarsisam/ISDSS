using ISDSS.Domain.Entities;

namespace ISDSS.Application.Abstractions;

public interface IReportService
{
    Task ExportCsvAsync(string path, IEnumerable<Student> students);
}
