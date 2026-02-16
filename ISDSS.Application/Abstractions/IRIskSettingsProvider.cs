namespace ISDSS.Application.Abstractions;

public interface IRiskSettingsProvider
{
    int MaxRecencyDays { get; }
    double RecencyWeight { get; }       // 0..1
    decimal HighRiskThreshold { get; }  // 0..100
}
