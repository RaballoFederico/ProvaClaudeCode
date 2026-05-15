var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBackendApi", policy =>
    {
        policy.WithOrigins("https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowBackendApi");

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/home.html"));
app.MapFallbackToFile("home.html");

app.Run();
