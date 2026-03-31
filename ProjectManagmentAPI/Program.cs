using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default-key-12345678901234567890"))
        };
    });

builder.Services.AddAuthorization();

// Database Context
builder.Services.AddDbContext<ContextDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBConnection")), ServiceLifetime.Scoped);

// Register Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ISprintService, SprintService>();
builder.Services.AddScoped<IBacklogService, BacklogService>();
builder.Services.AddScoped<ISubTaskService, SubTaskService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IRetrospectiveService, RetrospectiveService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// CORS Configuration - объединенная политика
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins(
                "https://localhost:5000",
                "https://localhost:5001",
                "http://localhost:5000",
                "https://localhost:5259",
                "http://localhost:5259",
                "https://localhost:7064",
                "http://localhost:5146"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS - должен быть между UseHttpsRedirection и UseAuthentication
app.UseCors("AllowBlazor");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Optional: Auto-migrate database on startup (uncomment if needed)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ContextDb>();
    // dbContext.Database.Migrate(); // Раскомментировать для автоматического применения миграций
}

app.Run();