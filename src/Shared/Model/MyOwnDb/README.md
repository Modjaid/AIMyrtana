## MyOwnDb (Shared Model)

Общий `DbContext` для PostgreSQL на Entity Framework Core, чтобы разные сервисы могли хранить/изменять данные в одной базе.

### Подключение в сервисе

1) Добавьте ссылку на проект `src/Shared/Model/MyOwnDb/MyOwnDb.csproj`.

2) В `Program.cs` (или DI-конфигурации) зарегистрируйте:

```csharp
builder.Services.AddMyOwnDb(builder.Configuration);
```

3) В `appsettings.json` сервиса добавьте:

```json
{
  "MyOwnDb": {
    "ConnectionString": "Host=localhost;Port=5432;Database=myowndb;Username=postgres;Password=postgres"
  }
}
```

### Миграции

Проект содержит design-time фабрику. Для генерации миграций установите `dotnet-ef` и выполните из корня репозитория:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate -p "src/Shared/Model/MyOwnDb/MyOwnDb.csproj"
dotnet ef database update -p "src/Shared/Model/MyOwnDb/MyOwnDb.csproj"
```

Для CI/локали можно передать строку подключения через переменную окружения:

- `MYOWNDB_CONNECTIONSTRING`

