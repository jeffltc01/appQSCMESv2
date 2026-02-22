using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MESv2.Api.Data;
using MESv2.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<MesDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWorkCenterService, WorkCenterService>();
builder.Services.AddScoped<IProductionRecordService, ProductionRecordService>();
builder.Services.AddScoped<IInspectionRecordService, InspectionRecordService>();
builder.Services.AddScoped<IAssemblyService, AssemblyService>();
builder.Services.AddScoped<ISerialNumberService, SerialNumberService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IRoundSeamService, RoundSeamService>();
builder.Services.AddScoped<INameplateService, NameplateService>();
builder.Services.AddScoped<IHydroService, HydroService>();
builder.Services.AddScoped<IXrayQueueService, XrayQueueService>();
builder.Services.AddScoped<ISellableTankStatusService, SellableTankStatusService>();

builder.Services.Configure<NiceLabelOptions>(builder.Configuration.GetSection("NiceLabel"));
builder.Services.AddHttpClient<INiceLabelService, NiceLabelService>();

builder.Services.Configure<LimbleOptions>(builder.Configuration.GetSection("Limble"));
builder.Services.AddHttpClient<ILimbleService, LimbleService>();

builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection("GitHub"));
builder.Services.AddHttpClient<IGitHubService, GitHubService>();
builder.Services.AddScoped<IIssueRequestService, IssueRequestService>();
builder.Services.AddScoped<IAIReviewService, AIReviewService>();
builder.Services.AddScoped<ILogViewerService, LogViewerService>();
builder.Services.AddScoped<ISupervisorDashboardService, SupervisorDashboardService>();
builder.Services.AddScoped<IDigitalTwinService, DigitalTwinService>();
builder.Services.AddScoped<IDowntimeService, DowntimeService>();
builder.Services.AddScoped<IOeeService, OeeService>();
builder.Services.AddScoped<IRealTimeXrayService, RealTimeXrayService>();

// --- JWT configuration ---
string jwtKey;
if (builder.Environment.IsDevelopment())
{
    jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-secret-key-min-32-chars-long-for-hs256";
}
else
{
    jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException(
            "Jwt:Key must be configured in non-Development environments. " +
            "Set it via Azure App Settings or Key Vault.");
}

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MESv2",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MESv2",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// --- CORS configuration ---
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

if (!builder.Environment.IsDevelopment() && (configuredOrigins == null || configuredOrigins.Length == 0))
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins must be configured in non-Development environments. " +
        "Set Cors__AllowedOrigins__0 in Azure App Settings.");
}

var allowedOrigins = configuredOrigins ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

// --- Health checks ---
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MesDbContext>("database");

// --- Application Insights ---
if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var app = builder.Build();

// --- Database initialization ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
    context.Database.Migrate();

    if (app.Environment.IsDevelopment())
        DbInitializer.Seed(context);
    else
        DbInitializer.SeedReferenceData(context);

    DbInitializer.SyncJoinTables(context);
    DbInitializer.EnsureAssembledProducts(context);
    DbInitializer.BackfillInspectionProductionRecords(context);
}

// --- Global exception handler (non-Development) ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async httpContext =>
        {
            var logger = httpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var exceptionFeature = httpContext.Features.Get<IExceptionHandlerFeature>();

            if (exceptionFeature?.Error is not null)
            {
                logger.LogError(exceptionFeature.Error,
                    "Unhandled exception on {Method} {Path}",
                    httpContext.Request.Method,
                    httpContext.Request.Path);
            }

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new { message = "An internal server error occurred." });
            await httpContext.Response.WriteAsync(payload);
        });
    });
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
