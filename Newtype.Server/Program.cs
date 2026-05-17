using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtype.Server.Data;
using Newtype.Server.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder. Configuration. GetConnectionString("DefaultConnection");

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddSignalR();
builder.Services.AddSingleton<ExecutionService>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapHub<CompilerHub>("/compilerHub");
app.MapControllers();

app.MapFallbackToFile("index.html");
app.Run();

public class CompilerHub : Hub {}
