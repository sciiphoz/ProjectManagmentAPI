using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.DataBaseContext;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ContextDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBConnection")), ServiceLifetime.Scoped);
//builder.Services.AddScoped<IUsersLoginsService, UserLoginService>();
//builder.Services.AddScoped<IRecordService, RecordService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();