using System.Text;
using ISDSS.Application.Abstractions;
using ISDSS.Domain.Entities;

namespace ISDSS.Infrastructure.Serialization;

public class ReportService : IReportService
{
    private readonly IRiskSettingsProvider _risk;

    public ReportService(IRiskSettingsProvider risk)
    {
        _risk = risk;
    }

    private decimal ComputeRisk(Student s)
    {
        var days = s.LastTrainingDate.HasValue
            ? (DateTime.UtcNow - s.LastTrainingDate.Value).TotalDays
            : 999.0;

        var recencyScore = (decimal)Math.Min(days / Math.Max(_risk.MaxRecencyDays, 1), 1.0) * 100m;
        var compliancePenalty = 100m - Math.Clamp(s.CompliancePercent, 0, 100);
        var w = (decimal)Math.Clamp(_risk.RecencyWeight, 0, 1);

        var risk = w * recencyScore + (1 - w) * compliancePenalty;
        return Math.Round(Math.Clamp(risk, 0, 100), 2);
    }

    public async Task ExportCsvAsync(string path, IEnumerable<Student> students)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id;ФИО;Email;Соответствие;ДатаОбучения;Риск");

        foreach (var s in students)
        {
            var date = s.LastTrainingDate?.ToString("yyyy-MM-dd") ?? "";
            var risk = ComputeRisk(s);
            var line = $"{s.Id};\"{s.FullName.Replace("\"","\"\"")}\";{s.Email};{s.CompliancePercent};{date};{risk}";
            sb.AppendLine(line);
        }
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        await File.WriteAllBytesAsync(path, bytes);
    }
}
