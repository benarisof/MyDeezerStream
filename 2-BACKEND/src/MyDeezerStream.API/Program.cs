using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyDeezer.Application;
using MyDeezerStream.API.Services;
using MyDeezerStream.Application.Interfaces;
using MyDeezerStream.Infrastructure;
using OfficeOpenXml;
using System.Diagnostics;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Configuration Excel
ExcelPackage.License.SetNonCommercialPersonal("MyDeezerStream");

// --- CONFIGURATION DYNAMIQUE POUR DOCKER ---

// Récupération des URLs frontend depuis la configuration ou variables d'env
var frontendUrls = (builder.Configuration["FrontendUrls"] ?? "http://localhost:4200")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

// Ajouter l'URL du service Docker frontend si défini
var dockerFrontendUrl = builder.Configuration["DOCKER_FRONTEND_URL"];
if (!string.IsNullOrWhiteSpace(dockerFrontendUrl))
{
    frontendUrls = frontendUrls.Append(dockerFrontendUrl.Trim()).ToArray();
}

// En mode développement, ajouter les URLs courantes
if (builder.Environment.IsDevelopment())
{
    var devUrls = new[]
    {
        "http://localhost:4200",
        "http://localhost:80",
        "http://frontend",
        "http://host.docker.internal:4200"
    };

    frontendUrls = frontendUrls.Union(devUrls).ToArray();
}

// Log les URLs autorisées pour debug
Console.WriteLine($"[CORS] Frontends autorisés : {string.Join(", ", frontendUrls)}");

// --- SERVICES ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- AUTHENTIFICATION AUTH0 ---
var auth0Domain = builder.Configuration["Auth0:Domain"] ?? builder.Configuration["AUTH0_DOMAIN"];
var auth0Audience = builder.Configuration["Auth0:Audience"] ?? builder.Configuration["AUTH0_AUDIENCE"];

// Normalisation de l'Authority pour éviter les soucis de schéma / slash final
if (!string.IsNullOrWhiteSpace(auth0Domain))
{
    auth0Domain = auth0Domain.Trim().TrimEnd('/');

    if (!auth0Domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
        !auth0Domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        auth0Domain = $"https://{auth0Domain}";
    }

    auth0Domain += "/";
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = auth0Domain;
        options.Audience = auth0Audience;

        // Validation des configurations
        if (string.IsNullOrEmpty(options.Authority))
            throw new InvalidOperationException("Auth0:Domain non configuré");
        if (string.IsNullOrEmpty(options.Audience))
            throw new InvalidOperationException("Auth0:Audience non configuré");

        // Sécurité : Timeout pour éviter que l'API ne freeze si Auth0 est lent à répondre
        options.BackchannelTimeout = TimeSpan.FromSeconds(10);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Authority,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.NameIdentifier
        };

        // Événements de debug pour le token
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[AUTH] Échec: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"[AUTH] Token validé pour: {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// --- CORS DYNAMIQUE ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy.WithOrigins(frontendUrls)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- CONFIGURATION DU LOGGING POUR DOCKER ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ==================== TRY-CATCH AUTOUR DE LA CONSTRUCTION ET DU DÉMARRAGE ====================
try
{
    Console.WriteLine("[INFO] Construction de l'application...");
    var app = builder.Build();
    Console.WriteLine("[OK] Application construite avec succès");

    // ========== MIDDLEWARE DE DEBUG ULTRA-SIMPLE ==========
    app.Use(async (context, next) =>
    {
        Console.WriteLine($"⚡ [DEBUG] REQUÊTE: {context.Request.Method} {context.Request.Path}");

        var originalBody = context.Response.Body;
        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        try
        {
            await next();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 [EXCEPTION] Type: {ex.GetType().Name}");
            Console.WriteLine($"💥 [EXCEPTION] Message: {ex.Message}");
            Console.WriteLine($"💥 [STACK] {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"💥 [INNER] {ex.InnerException.Message}");
            }
            await Console.Out.FlushAsync();

            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Exception: {ex.Message}");
        }

        memStream.Position = 0;
        var responseBody = await new StreamReader(memStream).ReadToEndAsync();
        memStream.Position = 0;
        await memStream.CopyToAsync(originalBody);
        context.Response.Body = originalBody;

        Console.WriteLine($"⚡ [DEBUG] RÉPONSE: {context.Response.StatusCode}");
        if (!string.IsNullOrEmpty(responseBody))
        {
            Console.WriteLine($"⚡ [DEBUG] BODY: {responseBody}");
        }
        await Console.Out.FlushAsync();
    });
    // ======================================================

    // --- MIDDLEWARE DE MESURE DE PERFORMANCE ---
    app.Use(async (context, next) =>
    {
        var sw = Stopwatch.StartNew();
        await next();
        sw.Stop();
        Console.WriteLine($"[PERF] {context.Request.Method} {context.Request.Path} -> {sw.ElapsedMilliseconds}ms - {context.Response.StatusCode}");
    });

    // Swagger uniquement en développement
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseRouting();
    app.UseCors("AngularPolicy");
    app.UseAuthentication();
    app.UseAuthorization();

    // Mapper les contrôleurs existants
    app.MapControllers();

    // --- ENDPOINTS DE TEST ULTRA-SIMPLES (SANS DÉPENDANCES) ---
    app.MapGet("/", () => "Hello World!");
    app.MapGet("/ping", () => "pong");
    app.MapGet("/simple", () => Results.Ok(new { message = "OK", time = DateTime.Now }));

    Console.WriteLine("[OK] Démarrage de l'application...");
    Console.WriteLine("Endpoints disponibles:");
    Console.WriteLine("  - GET /");
    Console.WriteLine("  - GET /ping");
    Console.WriteLine("  - GET /simple");
    Console.WriteLine("  - GET /api/health (via contrôleur)");
    await Console.Out.FlushAsync();

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("!!!!!!!!!! ERREUR FATALE AU DÉMARRAGE !!!!!!!!!!");
    Console.WriteLine($"[ERREUR FATALE] Type: {ex.GetType().Name}");
    Console.WriteLine($"[ERREUR FATALE] Message: {ex.Message}");
    Console.WriteLine($"[ERREUR FATALE] Stack: {ex.StackTrace}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"[ERREUR FATALE] Inner: {ex.InnerException.Message}");
    }
    await Console.Out.FlushAsync();
    throw;
}