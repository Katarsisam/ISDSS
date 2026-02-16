# ISDSS — Information Security Decision Support System

## 📘 Описание проекта
**ISDSS (Information Security Decision Support System)** — настольное приложение, предназначенное для учёта обучающихся по информационной безопасности и расчёта индивидуального показателя риска.  
Система помогает администраторам курсов быстро выявлять сотрудников с высоким уровнем риска и формировать отчёты для руководства.

Приложение реализовано на **.NET 9 (Avalonia UI)** с использованием **Entity Framework Core** и **MySQL**.  
Архитектура построена по принципам **модульности** и **слоистого проектирования (Layered Architecture)**.

---

## 🧩 Структура репозитория

1. **ISDSS.Application** — сервисный слой приложения. Содержит бизнес-логику и связь между доменной моделью и инфраструктурой.
2. **ISDSS.Domain** — доменные сущности (`Student`, `Assessment`, `Course`, `UserAccount`), алгоритм расчёта индивидуального риска.
3. **ISDSS.Infrastructure** — взаимодействие с базой данных MySQL (контекст `AppDbContext`, миграции EF Core, подключение), шифрование данных, сериализация десериализация.
4. **ISDSS.Presentation.UI** — графический интерфейс пользователя, построенный по паттерну **MVVM** (окна, таблицы, команды).
5. **ISDSS.sln** — файл решения Visual Studio.
6. **appsettings.json (настроить самостоятельно)** — конфигурационный файл подключения и параметров расчёта риска.

---

## ⚙️ Установка и запуск

### 1. Клонирование проекта
```bash
git clone https://github.com/Katarsisam/ISDSS.git
cd ISDSS
```

### 2. Настройка базы данных
Убедитесь, что установлен и запущен **MySQL Server**.  
Отредактируйте файл `appsettings.json`, указав корректные параметры подключения:
```json
"ConnectionStrings": {
  "DefaultConnection": "server=localhost;database=isdss;user=root;password=yourpassword;"
}
```
Или отредактируйте `AppDbContext.cs` и `DesignTimeDbContextFactory.cs`, указав базовые параметры подключения, если json конфигурация отсутсвует.
```bash
"Server=localhost;Port=3306;Database=isdss;User Id=root;Password=yourpassword;TreatTinyAsBoolean=true;"
```

### 3. Применение миграций
```bash
dotnet ef database update -p ISDSS.Infrastructure -s ISDSS.Presentation.UI
```

### 4. Запуск приложения
Через Visual Studio откройте `ISDSS.sln` и запустите проект **ISDSS.Presentation.UI**  
или выполните:
```bash
dotnet run --project ISDSS.Presentation.UI
```
### 4.1. Запуск приложения (версия для docker)
```bash
docker compose build
docker compose up -d db ## Контейнер с базой данных
docker compose up ui ## Контейнер с ПО
```
---

## 🧮 Алгоритм расчёта риска

Формулы вычисления показателя риска основаны на двух параметрах — **давности обучения** и **проценте соответствия**:

```
riskDays = min((DaysSinceLastTraining / MaxRecencyDays), 1) × 100
riskCompliance = 100 - CompliancePercent
risk = RecencyWeight × riskDays + (1 - RecencyWeight) × riskCompliance
```

Значение риска всегда находится в диапазоне **0–100**.  
Цветовая подсветка в интерфейсе отображает уровень риска (низкий, средний, высокий).

---

## 📊 Основные возможности
- учёт обучающихся, пользователей, их уровня доступа и хранение информации в БД MySQL;  
- автоматический пересчёт показателя риска;  
- фильтрация и поиск по таблице;  
- экспорт отчётов в формате CSV;  
- резервное копирование и восстановление данных (*.isdss*);  
- локальная работа без постоянного сетевого подключения.

---

## 💾 Пример конфигурации (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;database=isdss;user=root;password=yourpassword;"
  },
  "RiskSettings": {
    "MaxRecencyDays": 365,
    "RecencyWeight": 0.6,
    "HighRiskThreshold": 75
  }
}
```

---

## 🧰 Технологии
- **.NET 9 / Avalonia UI**
- **Entity Framework Core 6**
- **MySQL 8.x**
- **MVVM**
- **AES-GCM Encryption** (для резервных копий)

---

## 👨‍💻 Автор
**Комаров А. Г.**  
[https://github.com/Katarsisam/ISDSS.git](https://github.com/Katarsisam/ISDSS.git)

---
