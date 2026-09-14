using BingoBoard.Data;
using BingoBoard.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddNpgsqlDbContext<ApplicationDbContext>("db");
builder.Services.AddDefaultIdentity();

var host = builder.Build();
host.Run();
