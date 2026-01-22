using BingoBoard.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddApplicationDbContext();
builder.Services.AddDefaultIdentity();

var host = builder.Build();
host.Run();
