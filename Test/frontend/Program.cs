// DOC: Program - file del progetto; contiene logica specifica della feature/modulo.
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var apiBaseUrl =
    Environment.GetEnvironmentVariable("EXTERNAL_AUTH_BACKEND_BASE_URL")
    ?? "https://filmhub-api.delightfuldune-f7916078.francecentral.azurecontainerapps.io";
apiBaseUrl = apiBaseUrl.TrimEnd('/');

app.MapGet("/media/{*assetPath}", (string assetPath) =>
{
    if (string.IsNullOrWhiteSpace(assetPath))
    {
        return Results.NotFound();
    }

    var target = $"{apiBaseUrl}/media/{assetPath.TrimStart('/')}";
    return Results.Redirect(target, permanent: false);
});

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

