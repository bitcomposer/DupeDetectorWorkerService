using DupeDetectorWorkerService;
using DupeDetectorWorkerService.database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DupeDetector Service";
});

IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

var connectionString =
    builder.Configuration.GetConnectionString("DupeConn")
        ?? throw new InvalidOperationException("Connection string"
        + "'DefaultConnection' not found.");

#pragma warning disable CA1416 // Validate platform compatibility
LoggerProviderOptions.RegisterProviderOptions<
    EventLogSettings, EventLogLoggerProvider>(builder.Services);
#pragma warning restore CA1416 // Validate platform compatibility
builder.Services.AddDbContextFactory<DupeDBContext>(options =>
{
    options.UseSqlite(connectionString);
});
builder.Services.AddSingleton<FileHasherService>();
builder.Services.AddHostedService<WindowsBackgroundService>();

var host = builder.Build();
host.Run();
