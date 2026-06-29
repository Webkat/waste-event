using WasteEventApi.Endpoints;
using WasteEventApi.Streaming;

// WasteEventService.Listen() resolves the CSV files relative to the current
// working directory. Pin it to the binary location so the linked CSVs (copied
// next to the DLL) are always found, no matter how the app is launched.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddSingleton<WasteEventBroadcaster>();
builder.Services.AddHostedService<WasteEventPublisher>();

var app = builder.Build();

app.UseCors(CorsPolicy);

app.MapGet("/", () => Results.Ok(new { status = "ok", stream = "/api/events/stream" }));
app.MapEventStream();

app.Run();