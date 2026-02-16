using System.Text;
using System.Text.Json;
using ISDSS.Application.Abstractions;
using ISDSS.Domain.Entities;

namespace ISDSS.Infrastructure.Serialization;

/// <summary>
/// Экспорт/импорт списка студентов с шифрованием (AES).
/// Работает с IStudentRepository и ICryptoService (Encrypt/Decrypt byte[]).
/// </summary>
public class ExportService : IExportService
{
    private readonly ICryptoService _crypto;
    private readonly IStudentRepository _repo;

    public ExportService(ICryptoService crypto, IStudentRepository repo)
    {
        _crypto = crypto;
        _repo = repo;
    }

    public async Task ExportStudentsAsync(string path)
    {
        // 1) из БД
        var students = await _repo.GetAllAsync();

        // 2) сериализация в JSON -> bytes (UTF-8)
        var json = JsonSerializer.Serialize(students, new JsonSerializerOptions { WriteIndented = false });
        var plainBytes = Encoding.UTF8.GetBytes(json);

        // 3) шифрование и запись
        var cipherBytes = _crypto.Encrypt(plainBytes);
        await File.WriteAllBytesAsync(path, cipherBytes);
    }

    public async Task<List<Student>> ImportStudentsAsync(string path)
    {
        // 1) читаем зашифрованный файл
        var cipherBytes = await File.ReadAllBytesAsync(path);

        // 2) расшифровка -> строка JSON
        var plainBytes = _crypto.Decrypt(cipherBytes);
        var json = Encoding.UTF8.GetString(plainBytes);

        // 3) десериализация
        return JsonSerializer.Deserialize<List<Student>>(json) ?? new List<Student>();
    }
}
