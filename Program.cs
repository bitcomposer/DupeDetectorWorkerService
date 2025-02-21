using DupeDetectorWorkerService;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DupeDetector Service";
});

#pragma warning disable CA1416 // Validate platform compatibility
LoggerProviderOptions.RegisterProviderOptions<
    EventLogSettings, EventLogLoggerProvider>(builder.Services);
#pragma warning restore CA1416 // Validate platform compatibility

builder.Services.AddSingleton<FileHasherService>();
builder.Services.AddHostedService<WindowsBackgroundService>();

var host = builder.Build();
host.Run();
