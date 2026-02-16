using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using ISDSS.Domain.Entities;
using ISDSS.Application.Abstractions;
using ISDSS.Application.Services;
using ISDSS.Infrastructure.Persistence;
using ISDSS.Infrastructure.Security;
using ISDSS.Infrastructure.Serialization;
using ISDSS.Infrastructure.Configuration;
using System.Data.Common;

namespace ISDSS.Presentation.UI.ViewModels;

public class MainViewModel : BaseViewModel
{
    private static readonly IReadOnlyList<FilePickerFileType> IsdssFileTypes =
        new List<FilePickerFileType>
        {
            new("ISDSS") { Patterns = new List<string> { "*.isdss" } }
        };

    private static readonly IReadOnlyList<FilePickerFileType> CsvFileTypes =
        new List<FilePickerFileType>
        {
            new("CSV") { Patterns = new List<string> { "*.csv" } }
        };

    public ObservableCollection<StudentRow> Students { get; } = new();
    public ObservableCollection<StudentRow> StudentsView { get; } = new();
    public ObservableCollection<CourseRow> Courses { get; } = new();
    public ObservableCollection<AssessmentRow> Assessments { get; } = new();
    public ObservableCollection<UserAccountRow> Users { get; } = new();

    private StudentRow? _selectedRow;
    public StudentRow? SelectedRow
    {
        get => _selectedRow;
        set
        {
            _selectedRow = value;
            OnPropertyChanged();
            DeleteSelectedCommand.RaiseCanExecuteChanged();
        }
    }

    private CourseRow? _selectedCourse;
    public CourseRow? SelectedCourse
    {
        get => _selectedCourse;
        set
        {
            _selectedCourse = value;
            OnPropertyChanged();
            DeleteCourseCommand?.RaiseCanExecuteChanged();
        }
    }

    private AssessmentRow? _selectedAssessment;
    public AssessmentRow? SelectedAssessment
    {
        get => _selectedAssessment;
        set
        {
            _selectedAssessment = value;
            OnPropertyChanged();
            DeleteAssessmentCommand?.RaiseCanExecuteChanged();
        }
    }

    private string _name = string.Empty;
    private string? _email;

    public string NewStudentName
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
            AddStudentCommand.RaiseCanExecuteChanged();
        }
    }

    public string? NewStudentEmail
    {
        get => _email;
        set
        {
            _email = value;
            OnPropertyChanged();
        }
    }

    private string _filterText = string.Empty;
    public string FilterText
    {
        get => _filterText;
        set
        {
            _filterText = value ?? string.Empty;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    private string _login = "";
    public string LoginName
    {
        get => _login;
        set { _login = value; OnPropertyChanged(); }
    }

    private string _loginPassword = "";
    public string LoginPassword
    {
        get => _loginPassword;
        set { _loginPassword = value; OnPropertyChanged(); }
    }

    private string _newUserLogin = "";
    public string NewUserLogin
    {
        get => _newUserLogin;
        set { _newUserLogin = value; OnPropertyChanged(); RegisterUserCommand?.RaiseCanExecuteChanged(); }
    }

    private string _newUserPassword = "";
    public string NewUserPassword
    {
        get => _newUserPassword;
        set { _newUserPassword = value; OnPropertyChanged(); RegisterUserCommand?.RaiseCanExecuteChanged(); }
    }

    private string _newUserRole = "";
    public string NewUserRole
    {
        get => _newUserRole;
        set { _newUserRole = value; OnPropertyChanged(); }
    }

    private UserAccessLevel _newUserAccessLevel = UserAccessLevel.Auditor;
    public UserAccessLevel NewUserAccessLevel
    {
        get => _newUserAccessLevel;
        set { _newUserAccessLevel = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedAccessLevelOption)); }
    }

    public IReadOnlyList<AccessLevelOption> AccessLevelOptions { get; } =
        Enum.GetValues<UserAccessLevel>()
            .Select(level => new AccessLevelOption(level, level switch
            {
                UserAccessLevel.Admin => "Администратор",
                UserAccessLevel.Instructor => "Преподаватель",
                _ => "Аудитор"
            }))
            .ToList();

    public AccessLevelOption? SelectedAccessLevelOption
    {
        get => AccessLevelOptions.FirstOrDefault(o => o.Level == NewUserAccessLevel);
        set
        {
            if (value != null && value.Level != NewUserAccessLevel)
                NewUserAccessLevel = value.Level;
        }
    }

    private bool _isAdmin;
    public bool IsAdmin
    {
        get => _isAdmin;
        private set { _isAdmin = value; OnPropertyChanged(); OnPropertyChanged(nameof(LoginButtonText)); }
    }

    public string LoginButtonText => _currentUser != null ? "Выйти" : "Войти";

    private UserAccountRow? _currentUser;
    public string CurrentUserDisplay => _currentUser?.DisplayName ?? "Гость";
    public bool IsAuthenticated => _currentUser != null;
    public bool CanEditCourses => _currentUser is { AccessLevel: UserAccessLevel.Admin or UserAccessLevel.Instructor };
    public bool IsCourseEditorLocked => !CanEditCourses;
    public bool CanManageUsers => _currentUser?.AccessLevel == UserAccessLevel.Admin;
    public bool CanEditStudents => _currentUser is { AccessLevel: UserAccessLevel.Admin or UserAccessLevel.Instructor };
    public bool IsStudentGridReadOnly => !CanEditStudents;
    public bool CanGrade => _currentUser is { AccessLevel: UserAccessLevel.Admin or UserAccessLevel.Instructor };
    public bool CanEditCompliance => _currentUser?.AccessLevel == UserAccessLevel.Admin;
    public bool CanSeedDemo => _currentUser?.AccessLevel == UserAccessLevel.Admin;

    public StudentRow? AssessmentStudent
    {
        get => _assessmentStudent;
        set { _assessmentStudent = value; OnPropertyChanged(); AddAssessmentCommand?.RaiseCanExecuteChanged(); }
    }

    private StudentRow? _assessmentStudent;

    public CourseRow? AssessmentCourse
    {
        get => _assessmentCourse;
        set { _assessmentCourse = value; OnPropertyChanged(); AddAssessmentCommand?.RaiseCanExecuteChanged(); }
    }

    private CourseRow? _assessmentCourse;

    private decimal _newAssessmentScore = 100;
    public decimal NewAssessmentScore
    {
        get => _newAssessmentScore;
        set { _newAssessmentScore = Math.Clamp(value, 0, 100); OnPropertyChanged(); }
    }

    private bool _newAssessmentPassed = true;
    public bool NewAssessmentPassed
    {
        get => _newAssessmentPassed;
        set { _newAssessmentPassed = value; OnPropertyChanged(); }
    }

    private DateTimeOffset _newAssessmentDate = DateTimeOffset.UtcNow;
    public DateTimeOffset NewAssessmentDate
    {
        get => _newAssessmentDate;
        set { _newAssessmentDate = value; OnPropertyChanged(); }
    }

    public RelayCommand AddStudentCommand { get; }
    public RelayCommand ExportCommand { get; }
    public RelayCommand ImportCommand { get; }
    public RelayCommand ExportCsvCommand { get; }
    public RelayCommand SaveChangesCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand DeleteSelectedCommand { get; }
    public RelayCommand ClearFilterCommand { get; }
    public RelayCommand SeedDemoCommand { get; }
    public RelayCommand AddCourseCommand { get; }
    public RelayCommand DeleteCourseCommand { get; }
    public RelayCommand SaveCoursesCommand { get; }
    public RelayCommand AddAssessmentCommand { get; }
    public RelayCommand DeleteAssessmentCommand { get; }
    public RelayCommand LoginCommand { get; }
    public RelayCommand RegisterUserCommand { get; }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        private set { _totalCount = value; OnPropertyChanged(); }
    }

    private double _avgCompliance;
    public double AvgCompliance
    {
        get => _avgCompliance;
        private set { _avgCompliance = value; OnPropertyChanged(); }
    }

    private int _highRiskCount;
    public int HighRiskCount
    {
        get => _highRiskCount;
        private set { _highRiskCount = value; OnPropertyChanged(); }
    }

    public decimal HighRiskThreshold => _riskProvider.HighRiskThreshold;

    private readonly IRiskSettingsProvider _riskProvider = new RiskSettingsProvider();
    private readonly IReportService _reportSvc;
    private readonly AppDbContext? _ctx;
    private readonly IStudentService? _studentSvc;
    private readonly IExportService? _exportSvc;
    private bool _dbReady;

    private IStorageProvider? StorageProvider =>
        (global::Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?
            .MainWindow?.StorageProvider;

    public MainViewModel()
    {
        _reportSvc = new ReportService(_riskProvider);

        AddStudentCommand = new RelayCommand(async () => await AddStudentAsync(), () => _dbReady && CanEditStudents && !string.IsNullOrWhiteSpace(NewStudentName));
        ExportCommand     = new RelayCommand(async () => await ExportAsync(), () => _dbReady && _exportSvc != null);
        ImportCommand     = new RelayCommand(async () => await ImportAsync(), () => _dbReady && _exportSvc != null);
        ExportCsvCommand  = new RelayCommand(async () => await ExportCsvAsync());
        SaveChangesCommand= new RelayCommand(async () => await SaveChangesAsync(), () => _dbReady && (CanEditStudents || CanEditCompliance));
        RefreshCommand    = new RelayCommand(async () => await LoadAsync(), () => _dbReady);
        DeleteSelectedCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => _dbReady && SelectedRow != null && CanEditStudents);
        ClearFilterCommand= new RelayCommand(ResetFilter);
        SeedDemoCommand   = new RelayCommand(async () => await SeedDemoAsync(), () => _dbReady && CanSeedDemo);
        AddCourseCommand  = new RelayCommand(async () => await AddCourseAsync(), () => _dbReady && CanEditCourses);
        DeleteCourseCommand = new RelayCommand(async () => await DeleteCourseAsync(), () => _dbReady && CanEditCourses && SelectedCourse != null);
        SaveCoursesCommand  = new RelayCommand(async () => await SaveCoursesAsync(), () => _dbReady && CanEditCourses);
        AddAssessmentCommand = new RelayCommand(async () => await AddAssessmentAsync(), () => _dbReady && CanGrade && AssessmentStudent != null && AssessmentCourse != null);
        DeleteAssessmentCommand = new RelayCommand(async () => await DeleteAssessmentAsync(), () => _dbReady && SelectedAssessment != null && CanGrade);
        LoginCommand = new RelayCommand(async () => await LoginAsync());
        RegisterUserCommand = new RelayCommand(async () => await RegisterUserAsync(),
            () => _dbReady && CanManageUsers && !string.IsNullOrWhiteSpace(NewUserLogin) && !string.IsNullOrWhiteSpace(NewUserPassword));

        try
        {
            _ctx = new AppDbContext();

            try
            {
                _ctx.Database.Migrate();
            }
            catch (MySqlException mex) when (mex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"[ISDSS] Миграции уже применены: {mex.Message}");
                // Таблицы существуют, продолжаем работать без применения миграции.
            }
            EnsureAccessControlSchema(_ctx);

            var repo = new StudentRepository(_ctx);
            var crypto = new CryptoService();

            _studentSvc = new StudentService(repo, _riskProvider);
            _exportSvc  = new ExportService(crypto, repo);

            SetDbReady(true);
            _ = LoadAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ISDSS] Ошибка инициализации БД: {ex}");
            SetDbReady(false);
            SeedDemoFallback();
            _ = ShowMessageAsync("Не удалось подключиться к базе данных. Открыт демо-режим.");
        }
    }

    private void SetDbReady(bool ready)
    {
        _dbReady = ready;
        AddStudentCommand.RaiseCanExecuteChanged();
        ExportCommand.RaiseCanExecuteChanged();
        ImportCommand.RaiseCanExecuteChanged();
        SaveChangesCommand.RaiseCanExecuteChanged();
        RefreshCommand.RaiseCanExecuteChanged();
        DeleteSelectedCommand.RaiseCanExecuteChanged();
        AddCourseCommand.RaiseCanExecuteChanged();
        DeleteCourseCommand.RaiseCanExecuteChanged();
        SaveCoursesCommand.RaiseCanExecuteChanged();
        AddAssessmentCommand.RaiseCanExecuteChanged();
        DeleteAssessmentCommand.RaiseCanExecuteChanged();
        SeedDemoCommand.RaiseCanExecuteChanged();
        RegisterUserCommand.RaiseCanExecuteChanged();
    }

    private async Task LoadAsync()
    {
        var service = _studentSvc;
        if (!_dbReady || _ctx == null || service == null)
            return;

        try
        {
            _ctx.ChangeTracker.Clear();
            Students.Clear();

            var prevStudentId = AssessmentStudent?.Id;
            var prevCourseId = AssessmentCourse?.Id;

            var list = await service.GetAllAsync();
            foreach (var s in list)
                Students.Add(new StudentRow(s, () => service.ComputeRisk(s)));

            await LoadUsersAsync();
            await LoadCoursesAsync();
            await LoadAssessmentsAsync();

            AssessmentStudent = Students.FirstOrDefault(s => s.Id == prevStudentId) ?? Students.FirstOrDefault();
            AssessmentCourse = Courses.FirstOrDefault(c => c.Id == prevCourseId) ?? Courses.FirstOrDefault();

            ApplyFilter();
            RefreshStats();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync($"Ошибка загрузки: {ex.Message}");
        }
    }

    private async Task LoadCoursesAsync()
    {
        if (_ctx == null) return;

        Courses.Clear();
        var list = await _ctx.Courses
            .Include(c => c.AssignedUser)
            .OrderBy(c => c.Title)
            .ToListAsync();

        foreach (var c in list)
        {
            var row = new CourseRow(c);
            row.SetUserLookup(Users);
            Courses.Add(row);
        }

        AddAssessmentCommand?.RaiseCanExecuteChanged();
    }

    private async Task LoadUsersAsync()
    {
        if (_ctx == null) return;

        Users.Clear();
        var list = await _ctx.UserAccounts
            .OrderBy(u => u.Login)
            .ToListAsync();

        foreach (var u in list)
            Users.Add(new UserAccountRow(u));

        UpdateCourseUserBindings();
        RegisterUserCommand?.RaiseCanExecuteChanged();
    }

    private async Task RegisterUserAsync()
    {
        if (!_dbReady || _ctx == null || !CanManageUsers)
            return;

        var login = (NewUserLogin ?? string.Empty).Trim();
        var password = NewUserPassword ?? string.Empty;

        if (login.Length < 3)
        {
            await ShowMessageAsync("Логин должен содержать минимум 3 символа.");
            return;
        }

        if (password.Length < 6)
        {
            await ShowMessageAsync("Пароль должен содержать минимум 6 символов.");
            return;
        }

        if (await _ctx.UserAccounts.AnyAsync(u => u.Login == login))
        {
            await ShowMessageAsync("Такой логин уже зарегистрирован.");
            return;
        }

        var entity = new UserAccount
        {
            Login = login,
            PasswordHash = PasswordHasher.Hash(password),
            RoleTitle = (NewUserRole ?? string.Empty).Trim(),
            AccessLevel = NewUserAccessLevel
        };

        await _ctx.UserAccounts.AddAsync(entity);
        await _ctx.SaveChangesAsync();
        await LoadUsersAsync();

        NewUserLogin = string.Empty;
        NewUserPassword = string.Empty;
        NewUserRole = string.Empty;
        await ShowMessageAsync("Пользователь зарегистрирован.");
    }

    private void UpdateCourseUserBindings()
    {
        foreach (var course in Courses)
            course.SetUserLookup(Users);
    }

    private async Task LoadAssessmentsAsync()
    {
        if (_ctx == null) return;

        Assessments.Clear();
        var list = await _ctx.Assessments
            .OrderByDescending(a => a.IssuedAt)
            .ToListAsync();

        var studentNames = Students.ToDictionary(s => s.Id, s => s.FullName);
        var courseNames = Courses.ToDictionary(c => c.Id, c => c.Title);

        foreach (var a in list)
        {
            var row = new AssessmentRow(a,
                studentNames.TryGetValue(a.StudentId, out var sn) ? sn : $"#{a.StudentId}",
                courseNames.TryGetValue(a.CourseId, out var cn) ? cn : $"Курс #{a.CourseId}");
            Assessments.Add(row);
        }

        await UpdateComplianceFromAssessmentsAsync();
        SelectedAssessment = null;
        DeleteAssessmentCommand?.RaiseCanExecuteChanged();
    }

    private void ApplyFilter()
    {
        var term = FilterText?.Trim();
        StudentsView.Clear();

        foreach (var r in Students)
        {
            if (string.IsNullOrWhiteSpace(term) ||
                (r.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                StudentsView.Add(r);
            }
        }
    }

    private async Task UpdateComplianceFromAssessmentsAsync()
    {
        if (!_dbReady || _ctx == null)
        {
            RefreshStats();
            return;
        }

        var stats = await _ctx.Assessments
            .GroupBy(a => a.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                AvgScore = g.Average(x => x.Score),
                LastDate = g.Max(x => x.IssuedAt)
            })
            .ToListAsync();

        foreach (var s in Students)
        {
            s.CompliancePercent = 0;
            s.LastTrainingDate = null;
        }

        foreach (var stat in stats)
        {
            var student = Students.FirstOrDefault(s => s.Id == stat.StudentId);
            if (student != null)
            {
                student.CompliancePercent = Math.Clamp(Math.Round((decimal)stat.AvgScore, 2), 0, 100);
                student.LastTrainingDate = stat.LastDate;
            }
        }

        await _ctx.SaveChangesAsync();
        RefreshStats();
    }

    private async Task AddStudentAsync()
    {
        var service = _studentSvc;
        if (!_dbReady || service == null)
            return;

        try
        {
            await service.AddAsync(NewStudentName, NewStudentEmail);
            NewStudentName = string.Empty;
            NewStudentEmail = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync($"Ошибка добавления: {ex.Message}");
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (!_dbReady || _ctx == null || SelectedRow == null)
            return;

        try
        {
            _ctx.Students.Remove(SelectedRow.S);
            await _ctx.SaveChangesAsync();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync($"Ошибка удаления: {ex.Message}");
        }
    }

    private async Task AddCourseAsync()
    {
        if (!_dbReady || _ctx == null || !CanEditCourses)
            return;

        var course = new Course
        {
            Title = "Новый курс",
            Difficulty = 1,
            IsMandatory = true,
            AssignedUserId = _currentUser?.Id
        };

        await _ctx.Courses.AddAsync(course);
        await _ctx.SaveChangesAsync();
        await LoadCoursesAsync();
        AddAssessmentCommand?.RaiseCanExecuteChanged();
    }

    private async Task DeleteCourseAsync()
    {
        if (!_dbReady || _ctx == null || SelectedCourse == null || !CanEditCourses)
            return;

        _ctx.Courses.Remove(SelectedCourse.C);
        await _ctx.SaveChangesAsync();
        await LoadCoursesAsync();
        AddAssessmentCommand?.RaiseCanExecuteChanged();
    }

    private async Task SaveCoursesAsync()
    {
        if (!_dbReady || _ctx == null || !CanEditCourses)
            return;

        await _ctx.SaveChangesAsync();
        await LoadCoursesAsync();
        AddAssessmentCommand?.RaiseCanExecuteChanged();
    }

    private async Task AddAssessmentAsync()
    {
        if (!_dbReady || _ctx == null || AssessmentStudent == null || AssessmentCourse == null)
            return;

        var issuedAt = NewAssessmentDate == default
            ? DateTime.UtcNow
            : NewAssessmentDate.UtcDateTime;

        var entity = new Assessment
        {
            StudentId = AssessmentStudent.Id,
            CourseId = AssessmentCourse.Id,
            Score = NewAssessmentScore,
            Passed = NewAssessmentPassed,
            IssuedAt = issuedAt
        };

        await _ctx.Assessments.AddAsync(entity);
        await _ctx.SaveChangesAsync();
        await LoadAssessmentsAsync();
        NewAssessmentDate = DateTimeOffset.UtcNow;
        NewAssessmentScore = 100;
        NewAssessmentPassed = true;
    }

    private async Task DeleteAssessmentAsync()
    {
        if (!_dbReady || _ctx == null || SelectedAssessment == null)
            return;

        var entity = await _ctx.Assessments.FindAsync(SelectedAssessment.Id);
        if (entity != null)
        {
            _ctx.Assessments.Remove(entity);
            await _ctx.SaveChangesAsync();
            await LoadAssessmentsAsync();
        }
    }

    private async Task SaveChangesAsync()
    {
        if (!_dbReady || _ctx == null)
            return;

        try
        {
            foreach (var r in Students)
                r.CompliancePercent = Math.Clamp(r.CompliancePercent, 0, 100);

            await _ctx.SaveChangesAsync();
            await LoadAsync();
            await ShowMessageAsync("Изменения сохранены.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync($"Ошибка сохранения: {ex.Message}");
        }
    }

    private async Task ExportAsync()
    {
        if (!_dbReady || _exportSvc == null)
            return;

        var provider = StorageProvider;
        if (provider == null)
            return;

        var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = "students",
            DefaultExtension = "isdss",
            FileTypeChoices = IsdssFileTypes
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            await _exportSvc.ExportStudentsAsync(path);
            await ShowMessageAsync("Экспорт выполнен (файл зашифрован).");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync($"Ошибка экспорта: {ex.Message}");
        }
    }

    private async Task ExportCsvAsync()
    {
        var provider = StorageProvider;
        if (provider == null)
            return;

        var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = "students_report",
            DefaultExtension = "csv",
            FileTypeChoices = CsvFileTypes
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var entities = Students.Select(sr => sr.S).ToList();
            await _reportSvc.ExportCsvAsync(path, entities);
            await ShowMessageAsync("CSV-отчёт сохранён.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync($"Ошибка экспорта CSV: {ex.Message}");
        }
    }

    private async Task ImportAsync()
    {
        if (!_dbReady || _ctx == null || _exportSvc == null)
            return;

        var provider = StorageProvider;
        if (provider == null)
            return;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = IsdssFileTypes
        });

        var path = files?.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var imported = await _exportSvc.ImportStudentsAsync(path);
            imported = imported.Where(s => !string.IsNullOrWhiteSpace(s.FullName)).ToList();

            foreach (var s in imported)
                s.CompliancePercent = Math.Clamp(s.CompliancePercent, 0, 100);

            int inserted = 0, updated = 0;
            foreach (var s in imported)
            {
                Student? existing = null;
                if (!string.IsNullOrWhiteSpace(s.Email))
                    existing = await _ctx.Students.FirstOrDefaultAsync(x => x.Email == s.Email);
                if (existing == null)
                    existing = await _ctx.Students.FirstOrDefaultAsync(x => x.FullName == s.FullName);

                if (existing == null)
                {
                    await _ctx.Students.AddAsync(s);
                    inserted++;
                }
                else
                {
                    existing.FullName = s.FullName;
                    existing.Email = s.Email;
                    existing.LastTrainingDate = s.LastTrainingDate;
                    existing.CompliancePercent = s.CompliancePercent;
                    updated++;
                }
            }

            await _ctx.SaveChangesAsync();
            await LoadAsync();
            await ShowMessageAsync($"Импортировано: добавлено {inserted}, обновлено {updated}.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync($"Ошибка импорта: {ex.Message}");
        }
    }

    private async Task SeedDemoAsync()
    {
        if (!_dbReady || _ctx == null)
        {
            SeedDemoFallback();
            return;
        }

        var samples = CreateSampleStudents();
        PopulateStudents(samples);

        if (_dbReady && _ctx != null)
        {
            try
            {
                foreach (var sample in samples)
                {
                    var existing = await _ctx.Students.FirstOrDefaultAsync(s =>
                        (!string.IsNullOrEmpty(sample.Email) && s.Email == sample.Email) ||
                        s.FullName == sample.FullName);

                    if (existing == null)
                    {
                        await _ctx.Students.AddAsync(sample);
                    }
                    else
                    {
                        existing.CompliancePercent = sample.CompliancePercent;
                        existing.LastTrainingDate = sample.LastTrainingDate;
                        existing.Email = sample.Email;
                        existing.FullName = sample.FullName;
                    }
                }

                await _ctx.SaveChangesAsync();
                await LoadAsync();
                await ShowMessageAsync("Демо-данные записаны в базу.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync($"Ошибка загрузки демо-данных: {ex.GetBaseException().Message}");
            }
        }
        else
        {
            await ShowMessageAsync("База недоступна, показаны локальные демо-данные.");
        }
    }

    private void SeedDemoFallback()
    {
        PopulateStudents(CreateSampleStudents());
        PopulateCourses(CreateSampleCourses());
        PopulateAssessments(CreateSampleAssessments());
        Users.Clear();
        UpdateCourseUserBindings();
        AssessmentStudent = Students.FirstOrDefault();
        AssessmentCourse = Courses.FirstOrDefault();
        RefreshStats();
    }

    private static void EnsureAccessControlSchema(AppDbContext ctx)
    {
        try
        {
            var connectionString = ctx.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
                return;

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            static int ExecuteScalarInt(DbConnection cn, string sql)
            {
                using var cmd = cn.CreateCommand();
                cmd.CommandText = sql;
                var result = cmd.ExecuteScalar();
                return Convert.ToInt32(result ?? 0);
            }

            static void ExecuteNonQuery(DbConnection cn, string sql)
            {
                using var cmd = cn.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }

            bool TableExists(string table) =>
                ExecuteScalarInt(connection,
                    $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{table}';") > 0;

            bool ColumnExists(string table, string column) =>
                ExecuteScalarInt(connection,
                    $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = '{table}' AND column_name = '{column}';") > 0;

            bool IndexExists(string table, string index) =>
                ExecuteScalarInt(connection,
                    $"SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = '{table}' AND index_name = '{index}';") > 0;

            bool ConstraintExists(string constraint) =>
                ExecuteScalarInt(connection,
                    $"SELECT COUNT(*) FROM information_schema.referential_constraints WHERE constraint_schema = DATABASE() AND constraint_name = '{constraint}';") > 0;

            if (!TableExists("UserAccounts"))
            {
                ExecuteNonQuery(connection, """
                    CREATE TABLE `UserAccounts` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `Login` varchar(50) NOT NULL,
                        `PasswordHash` varchar(200) NOT NULL,
                        `RoleTitle` varchar(100) NULL,
                        `AccessLevel` int NOT NULL,
                        PRIMARY KEY (`Id`),
                        UNIQUE KEY `IX_UserAccounts_Login` (`Login`)
                    ) CHARACTER SET utf8mb4;
                    """);
            }

            if (!TableExists("SchemaVersions"))
            {
                ExecuteNonQuery(connection, """
                    CREATE TABLE `SchemaVersions` (
                        `MigrationId` varchar(150) NOT NULL,
                        `ProductVersion` varchar(32) NOT NULL,
                        PRIMARY KEY (`MigrationId`)
                    ) CHARACTER SET utf8mb4;
                    """);
            }

            if (ExecuteScalarInt(connection, "SELECT COUNT(*) FROM `SchemaVersions` WHERE `MigrationId` = '20250911192315_InitialCreate';") == 0)
            {
                ExecuteNonQuery(connection, "INSERT INTO `SchemaVersions` (`MigrationId`, `ProductVersion`) VALUES ('20250911192315_InitialCreate', '9.0.0');");
            }

            if (!ColumnExists("Courses", "AssignedUserId"))
            {
                ExecuteNonQuery(connection, "ALTER TABLE `Courses` ADD COLUMN `AssignedUserId` int NULL;");
            }

            if (!IndexExists("Courses", "IX_Courses_AssignedUserId"))
            {
                ExecuteNonQuery(connection, "CREATE INDEX `IX_Courses_AssignedUserId` ON `Courses` (`AssignedUserId`);");
            }

            if (!ConstraintExists("FK_Courses_UserAccounts_AssignedUserId"))
            {
                ExecuteNonQuery(connection, """
                    ALTER TABLE `Courses`
                    ADD CONSTRAINT `FK_Courses_UserAccounts_AssignedUserId`
                    FOREIGN KEY (`AssignedUserId`) REFERENCES `UserAccounts`(`Id`)
                    ON DELETE SET NULL;
                    """);
            }

            ExecuteNonQuery(connection, $"""
                INSERT INTO `UserAccounts` (`Login`, `PasswordHash`, `RoleTitle`, `AccessLevel`)
                SELECT 'admin', '{PasswordHasher.Hash("Admin!123")}', 'Администратор', 2
                WHERE NOT EXISTS (SELECT 1 FROM `UserAccounts` WHERE `Login` = 'admin');
                """);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ISDSS] Не удалось привести схему доступа в актуальное состояние: {ex.Message}");
        }
    }

    private void ResetFilter()
    {
        if (!string.IsNullOrEmpty(_filterText))
        {
            _filterText = string.Empty;
            OnPropertyChanged(nameof(FilterText));
        }

        StudentsView.Clear();
        foreach (var r in Students)
            StudentsView.Add(r);
    }

    private void SetCurrentUser(UserAccountRow? user)
    {
        _currentUser = user;
        IsAdmin = user?.AccessLevel == UserAccessLevel.Admin;
        OnPropertyChanged(nameof(CurrentUserDisplay));
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(CanEditCourses));
        OnPropertyChanged(nameof(IsCourseEditorLocked));
        OnPropertyChanged(nameof(CanManageUsers));
        OnPropertyChanged(nameof(CanEditStudents));
        OnPropertyChanged(nameof(IsStudentGridReadOnly));
        OnPropertyChanged(nameof(CanGrade));
        OnPropertyChanged(nameof(CanEditCompliance));
        OnPropertyChanged(nameof(CanSeedDemo));
        OnPropertyChanged(nameof(LoginButtonText));
        LoginCommand.RaiseCanExecuteChanged();
        AddCourseCommand.RaiseCanExecuteChanged();
        DeleteCourseCommand.RaiseCanExecuteChanged();
        SaveCoursesCommand.RaiseCanExecuteChanged();
        AddStudentCommand.RaiseCanExecuteChanged();
        DeleteSelectedCommand.RaiseCanExecuteChanged();
        SaveChangesCommand.RaiseCanExecuteChanged();
        AddAssessmentCommand.RaiseCanExecuteChanged();
        DeleteAssessmentCommand.RaiseCanExecuteChanged();
        SeedDemoCommand.RaiseCanExecuteChanged();
        RegisterUserCommand.RaiseCanExecuteChanged();
    }

    private async Task LoginAsync()
    {
        if (!_dbReady || _ctx == null)
        {
            await ShowMessageAsync("База данных недоступна.");
            return;
        }

        if (_currentUser != null)
        {
            SetCurrentUser(null);
            LoginName = "";
            LoginPassword = "";
            await ShowMessageAsync("Режим доступа отключён.");
            return;
        }

        var login = (LoginName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(login))
        {
            await ShowMessageAsync("Введите логин.");
            return;
        }

        var user = await _ctx.UserAccounts.FirstOrDefaultAsync(u => u.Login == login);
        if (user != null && PasswordHasher.Verify(LoginPassword, user.PasswordHash))
        {
            SetCurrentUser(new UserAccountRow(user));
            await ShowMessageAsync($"Здравствуйте, {user.Login}.");
        }
        else
        {
            await ShowMessageAsync("Неверный логин или пароль.");
        }

        LoginPassword = string.Empty;
    }

    private void PopulateStudents(IEnumerable<Student> samples)
    {
        FilterText = string.Empty;
        Students.Clear();

        var calc = new StudentService(null, _riskProvider);
        foreach (var s in samples)
        {
            // create copies so demo mode doesn't reuse entities tracked by DbContext
            var clone = new Student
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                CompliancePercent = s.CompliancePercent,
                LastTrainingDate = s.LastTrainingDate
            };
            Students.Add(new StudentRow(clone, () => calc.ComputeRisk(clone)));
        }

        ApplyFilter();
        RefreshStats();
    }

    private void PopulateCourses(IEnumerable<Course> samples)
    {
        Courses.Clear();
        foreach (var c in samples)
        {
            var clone = new Course
            {
                Id = c.Id,
                Title = c.Title,
                Difficulty = c.Difficulty,
                IsMandatory = c.IsMandatory
            };
            var row = new CourseRow(clone);
            row.SetUserLookup(Users);
            Courses.Add(row);
        }
    }

    private void PopulateAssessments(IEnumerable<Assessment> samples)
    {
        Assessments.Clear();
        var studentNames = Students.ToDictionary(s => s.Id, s => s.FullName);
        var courseNames = Courses.ToDictionary(c => c.Id, c => c.Title);

        foreach (var a in samples)
        {
            var clone = new Assessment
            {
                Id = a.Id,
                StudentId = a.StudentId,
                CourseId = a.CourseId,
                Score = a.Score,
                Passed = a.Passed,
                IssuedAt = a.IssuedAt
            };

            Assessments.Add(new AssessmentRow(clone,
                studentNames.TryGetValue(clone.StudentId, out var sn) ? sn : $"#{clone.StudentId}",
                courseNames.TryGetValue(clone.CourseId, out var cn) ? cn : $"#{clone.CourseId}"));
        }
    }

    private static IEnumerable<Student> CreateSampleStudents() => new[]
    {
        new Student{ Id=1, FullName="Иван Петров",      Email="ivan.petrov@example.com",  CompliancePercent=82, LastTrainingDate=DateTime.UtcNow.AddDays(-40)},
        new Student{ Id=2, FullName="Мария Сидорова",   Email="m.sidorova@example.com",   CompliancePercent=55, LastTrainingDate=DateTime.UtcNow.AddDays(-220)},
        new Student{ Id=3, FullName="Алишер Каримов",   Email="a.karimov@example.com",    CompliancePercent=30, LastTrainingDate=DateTime.UtcNow.AddDays(-400)},
        new Student{ Id=4, FullName="Жанна Орлова",     Email="zh.orlova@example.com",    CompliancePercent=96, LastTrainingDate=DateTime.UtcNow.AddDays(-10)},
    };

    private static IEnumerable<Course> CreateSampleCourses() => new[]
    {
        new Course{ Id=1, Title="Основы ИБ", IsMandatory=true, Difficulty=1 },
        new Course{ Id=2, Title="GDPR и комплаенс", IsMandatory=true, Difficulty=2 },
        new Course{ Id=3, Title="Инцидент-менеджмент", IsMandatory=false, Difficulty=3 },
    };

    private static IEnumerable<Assessment> CreateSampleAssessments() => new[]
    {
        new Assessment{ Id=1, StudentId=1, CourseId=1, Score=88, Passed=true, IssuedAt=DateTime.UtcNow.AddDays(-40)},
        new Assessment{ Id=2, StudentId=2, CourseId=2, Score=62, Passed=true, IssuedAt=DateTime.UtcNow.AddDays(-120)},
        new Assessment{ Id=3, StudentId=3, CourseId=3, Score=30, Passed=false, IssuedAt=DateTime.UtcNow.AddDays(-200)},
        new Assessment{ Id=4, StudentId=4, CourseId=1, Score=96, Passed=true, IssuedAt=DateTime.UtcNow.AddDays(-10)},
    };

    private void RefreshStats()
    {
        var list = Students.Select(s => s.S).ToList();
        TotalCount = list.Count;
        AvgCompliance = list.Count == 0 ? 0 : (double)list.Average(s => s.CompliancePercent);
        HighRiskCount = list.Count(s => new StudentService(null, _riskProvider).ComputeRisk(s) >= _riskProvider.HighRiskThreshold);
        OnPropertyChanged(nameof(HighRiskThreshold));
    }

    private static Task ShowMessageAsync(string message, string title = "ISDSS")
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is null)
        {
            Console.WriteLine($"{title}: {message}");
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<bool>();
        Dispatcher.UIThread.Post(() =>
        {
            var text = new TextBlock
            {
                Text = message,
                Margin = new Thickness(16, 16, 16, 8),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 420
            };

            var button = new Button
            {
                Content = "ОК",
                Width = 90,
                Margin = new Thickness(0, 0, 16, 16),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var panel = new StackPanel();
            panel.Children.Add(text);
            panel.Children.Add(button);

            var dialog = new Window
            {
                Title = title,
                Content = panel,
                SizeToContent = SizeToContent.WidthAndHeight,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            button.Click += (_, _) => dialog.Close();
            dialog.Closed += (_, _) => tcs.TrySetResult(true);
            dialog.ShowDialog(desktop.MainWindow);
        });

        return tcs.Task;
    }

    public class StudentRow : BaseViewModel
    {
        public Student S { get; }
        private readonly Func<decimal> _recalc;
        private decimal _risk;

        public StudentRow(Student s, Func<decimal> recalc)
        {
            S = s;
            _recalc = recalc;
            _risk = _recalc();
        }

        public int Id => S.Id;

        public string FullName
        {
            get => S.FullName;
            set { S.FullName = value; OnPropertyChanged(); Risk = _recalc(); }
        }

        public string? Email
        {
            get => S.Email;
            set { S.Email = value; OnPropertyChanged(); }
        }

        public decimal CompliancePercent
        {
            get => S.CompliancePercent;
            set { S.CompliancePercent = Math.Clamp(value, 0, 100); OnPropertyChanged(); Risk = _recalc(); }
        }

        public DateTime? LastTrainingDate
        {
            get => S.LastTrainingDate;
            set { S.LastTrainingDate = value; OnPropertyChanged(); Risk = _recalc(); }
        }

        public DateTimeOffset? LastTrainingDateOffset
        {
            get => S.LastTrainingDate.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(S.LastTrainingDate.Value, DateTimeKind.Utc))
                : null;
            set
            {
                LastTrainingDate = value?.UtcDateTime;
            }
        }

        public decimal Risk
        {
            get => _risk;
            private set { _risk = value; OnPropertyChanged(); }
        }
    }

    public class CourseRow : BaseViewModel
    {
        public Course C { get; }
        private IReadOnlyDictionary<int, UserAccountRow> _userLookup = new Dictionary<int, UserAccountRow>();
        private string _assignedUserName;

        public CourseRow(Course c)
        {
            C = c;
            _assignedUserName = c.AssignedUser?.Login ?? string.Empty;
        }

        public int Id => C.Id;

        public string Title
        {
            get => C.Title;
            set { if (C.Title != value) { C.Title = value; OnPropertyChanged(); } }
        }

        public bool IsMandatory
        {
            get => C.IsMandatory;
            set { if (C.IsMandatory != value) { C.IsMandatory = value; OnPropertyChanged(); } }
        }

        public byte Difficulty
        {
            get => C.Difficulty;
            set { if (C.Difficulty != value) { C.Difficulty = value; OnPropertyChanged(); } }
        }

        public int? AssignedUserId
        {
            get => C.AssignedUserId;
            set
            {
                if (C.AssignedUserId != value)
                {
                    C.AssignedUserId = value;
                    UpdateAssignedUserName();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AssignedUserRow));
                }
            }
        }

        public string AssignedUserName
        {
            get => _assignedUserName;
            private set
            {
                if (_assignedUserName != value)
                {
                    _assignedUserName = value;
                    OnPropertyChanged();
                }
            }
        }

        public UserAccountRow? AssignedUserRow
        {
            get => C.AssignedUserId.HasValue && _userLookup.TryGetValue(C.AssignedUserId.Value, out var row)
                ? row
                : null;
            set
            {
                var newId = value?.Id;
                if (C.AssignedUserId != newId)
                {
                    C.AssignedUserId = newId;
                    UpdateAssignedUserName();
                    OnPropertyChanged(nameof(AssignedUserId));
                    OnPropertyChanged();
                }
            }
        }

        public void SetUserLookup(IEnumerable<UserAccountRow> users)
        {
            _userLookup = users.ToDictionary(u => u.Id);
            UpdateAssignedUserName();
            OnPropertyChanged(nameof(AssignedUserRow));
        }

        private void UpdateAssignedUserName()
        {
            AssignedUserName = AssignedUserRow?.DisplayName ?? string.Empty;
        }
    }

    public class AssessmentRow : BaseViewModel
    {
        public Assessment A { get; }
        public string StudentName { get; }
        public string CourseTitle { get; }

        public AssessmentRow(Assessment a, string studentName, string courseTitle)
        {
            A = a;
            StudentName = studentName;
            CourseTitle = courseTitle;
        }

        public int Id => A.Id;
        public int StudentId => A.StudentId;
        public int CourseId => A.CourseId;

        public decimal Score
        {
            get => A.Score;
            set { if (A.Score != value) { A.Score = value; OnPropertyChanged(); } }
        }

        public bool Passed
        {
            get => A.Passed;
            set { if (A.Passed != value) { A.Passed = value; OnPropertyChanged(); } }
        }

        public DateTime IssuedAt
        {
            get => A.IssuedAt;
            set { if (A.IssuedAt != value) { A.IssuedAt = value; OnPropertyChanged(); } }
        }
    }

    public class UserAccountRow : BaseViewModel
    {
        public UserAccountRow(UserAccount account)
        {
            Account = account;
        }

        public UserAccount Account { get; }
        public int Id => Account.Id;
        public string Login => Account.Login;
        public string? RoleTitle => Account.RoleTitle;
        public UserAccessLevel AccessLevel => Account.AccessLevel;

        public string AccessLevelLabel => AccessLevel switch
        {
            UserAccessLevel.Admin => "Администратор",
            UserAccessLevel.Instructor => "Преподаватель",
            _ => "Аудитор"
        };

        public string DisplayName =>
            string.IsNullOrWhiteSpace(RoleTitle)
                ? $"{Login} ({AccessLevelLabel})"
                : $"{Login} – {RoleTitle} ({AccessLevelLabel})";
    }

    public record AccessLevelOption(UserAccessLevel Level, string Label);
}
