using System.Text.Json;
using ISDSS.Application.Abstractions;

namespace ISDSS.Infrastructure.Configuration;

public class RiskSettingsProvider : IRiskSettingsProvider
{
    private sealed class Root
    {
        public Conn? ConnectionStrings { get; set; }
        public Risk? Risk { get; set; }
    }
    private sealed class Conn { public string? Default { get; set; } }
    private sealed class Risk
    {
        public int MaxRecencyDays { get; set; } = 365;
        public double RecencyWeight { get; set; } = 0.5;
        public decimal HighRiskThreshold { get; set; } = 75m;
    }

    private readonly Risk _risk;

    public RiskSettingsProvider()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var root = JsonSerializer.Deserialize<Root>(json);
            _risk = root?.Risk ?? new Risk();
        }
        else
        {
            _risk = new Risk();
        }
    }

    public int MaxRecencyDays => _risk.MaxRecencyDays;
    public double RecencyWeight => Math.Clamp(_risk.RecencyWeight, 0, 1);
    public decimal HighRiskThreshold => Math.Clamp(_risk.HighRiskThreshold, 0, 100);
}
