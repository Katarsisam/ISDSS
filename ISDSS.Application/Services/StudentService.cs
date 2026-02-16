using System.Text.RegularExpressions;
using ISDSS.Application.Abstractions;
using ISDSS.Domain.Entities;

namespace ISDSS.Application.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _repo;
    private readonly IRiskSettingsProvider _risk;

    public StudentService(IStudentRepository? repo, IRiskSettingsProvider? risk = null)
    {
        _repo = repo!;
        _risk = risk ?? new DefaultRisk();
    }

    public async Task<List<Student>> GetAllAsync() => await _repo.GetAllAsync();

    public async Task<Student> AddAsync(string fullName, string? email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("ФИО не должно быть пустым.", nameof(fullName));

        if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
            throw new ArgumentException("Некорректный формат Email.", nameof(email));

        var s = new Student
        {
            FullName = fullName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            CompliancePercent = 0,
            LastTrainingDate = DateTime.UtcNow
        };
        await _repo.AddAsync(s);
        return s;
    }

    public decimal ComputeRisk(Student s)
    {
        var days = s.LastTrainingDate.HasValue ? (DateTime.UtcNow - s.LastTrainingDate.Value).TotalDays : 999.0;
        var recencyScore = (decimal)Math.Min(days / Math.Max(_risk.MaxRecencyDays, 1), 1.0) * 100m;  // 0..100
        var compliancePenalty = 100m - Math.Clamp(s.CompliancePercent, 0, 100);                      // 0..100
        var w = (decimal)Math.Clamp(_risk.RecencyWeight, 0, 1);
        var risk = w * recencyScore + (1 - w) * compliancePenalty;
        return Math.Round(Math.Clamp(risk, 0, 100), 2);
    }

    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private sealed class DefaultRisk : IRiskSettingsProvider
    {
        public int MaxRecencyDays => 365;
        public double RecencyWeight => 0.5;
        public decimal HighRiskThreshold => 75m;
    }
}
