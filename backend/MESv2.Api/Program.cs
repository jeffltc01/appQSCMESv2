using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MESv2.Api.Data;
using MESv2.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<MesDbContext>(options =>
{
    if (connectionString != null && connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
        options.UseSqlServer(connectionString);
    else
        options.UseSqlite(connectionString ?? "Data Source=mes.db");
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

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-secret-key-min-32-chars-long-for-hs256";
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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MesDbContext>();

    if (context.Database.IsSqlServer())
    {
        context.Database.Migrate();
    }
    else
    {
        context.Database.EnsureCreated();
        DbInitializer.Seed(context);
    }
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

app.Run();
