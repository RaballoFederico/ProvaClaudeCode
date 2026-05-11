var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        // Evita che browser mantenga versioni obsolete degli script durante sviluppo.
        context.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        context.Context.Response.Headers.Pragma = "no-cache";
        context.Context.Response.Headers.Expires = "0";
    }
});

app.MapGet("/", () => Results.Redirect("/home.html"));
app.MapFallback(() => Results.NotFound(new { message = "Pagina non trovata" }));

app.Run();
