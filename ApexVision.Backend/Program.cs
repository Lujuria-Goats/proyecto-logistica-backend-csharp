using System.Text;
using System.Security.Claims;
using ApexVision.Backend.Data;
using ApexVision.Backend.Models;
using ApexVision.Backend.Services;
using ApexVision.Backend.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CloudinaryDotNet;
using Serilog;
using System.IdentityModel.Tokens.Jwt; // Necesario para JwtSecurityTokenHandler
using Microsoft.Extensions.DependencyInjection; // Necesario para GetRequiredService
using Microsoft.AspNetCore.Authorization; // Añadido para AuthorizationPolicyBuilder

// Limpiar el mapa de claims predeterminado para evitar remapeos automáticos.
// JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/apex-vision-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    
    var configuration = builder.Configuration;

    // Add services to the container.

    // 1. Configure DbContext for PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

// 2. Configure JWT Authentication BEFORE AddIdentity
// Se configura JWT como esquema de autenticación predeterminado ANTES de AddIdentity
// para que Identity respete esta configuración.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado"))),
        ClockSkew = TimeSpan.FromMinutes(30),
        // Usar los claim types completos de Microsoft
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.NameIdentifier
    };
    // Mapear claims de entrada para convertir nombres cortos a URIs
    options.MapInboundClaims = true;

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var accessToken = context.Request.Headers["Authorization"].ToString();
            // No loguear el token completo por seguridad. Solo indicar si se recibió.
            logger.LogDebug("OnMessageReceived: Token de autorización recibido: {Status}", string.IsNullOrEmpty(accessToken) ? "[No token]" : "[Token presente]");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("OnTokenValidated: Token validado. Claims en el principal:");
            if (context.Principal != null && context.Principal.Claims != null)
            {
                foreach (var claim in context.Principal.Claims)
                {
                    logger.LogDebug("- Tipo: {ClaimType}, Valor: {ClaimValue}", claim.Type, claim.Value);
                }
                // Asegurarse de que context.Principal no sea nulo antes de llamar a IsInRole
                bool isAdminInPrincipal = context.Principal.IsInRole("Admin");
                logger.LogDebug("OnTokenValidated: context.Principal.IsInRole(\"Admin\"): {IsAdmin}", isAdminInPrincipal);
            }
            else
            {
                logger.LogDebug("OnTokenValidated: No hay principal o claims para loguear.");
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "OnAuthenticationFailed: Fallo de autenticación.");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("OnChallenge: Desafío de autenticación. Motivo: {Error}, Descripción: {ErrorDescription}", context.Error ?? "[No Error]", context.ErrorDescription ?? "[No Description]");
            return Task.CompletedTask;
        }
    };
});

// Add Identity services
builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
    // Usar ClaimTypes.Role para consistencia con JWT
    options.ClaimsIdentity.RoleClaimType = ClaimTypes.Role;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Evitar que las cookies de Identity redirijan en APIs: devolver 401/403 en lugar de 302 a /Account/Login
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    };
});


// 3. Register JWT Service
builder.Services.AddScoped<JwtService>();

// 4. Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .Build();

    // Política específica para roles que también requiere JWT
    options.AddPolicy("AdminOnly", new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("Admin")
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .Build());

    options.AddPolicy("DriverOnly", new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("Driver")
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .Build());

    // Política para Admin O Driver (para endpoints compartidos como Routes/save)
    options.AddPolicy("AdminOrDriver", new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("Admin", "Driver")
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .Build());
});

// --- CONFIGURACIÓN DE CLOUDINARY ROBUSTA ---
var cloudName = builder.Configuration["Cloudinary:CloudName"];
var apiKey = builder.Configuration["Cloudinary:ApiKey"];
var apiSecret = builder.Configuration["Cloudinary:ApiSecret"];

if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
{
    Log.Warning("⚠️ Credenciales de Cloudinary incompletas en la sección 'Cloudinary'. Intentando con 'CloudinarySettings'.");
    // Intentar leer con la otra estructura común por si acaso (CloudinarySettings)
    cloudName = builder.Configuration["CloudinarySettings:CloudName"];
    apiKey = builder.Configuration["CloudinarySettings:ApiKey"];
    apiSecret = builder.Configuration["CloudinarySettings:ApiSecret"];
}

if (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
{
    var cloudinaryAccount = new Account(cloudName, apiKey, apiSecret);
    builder.Services.AddSingleton(new Cloudinary(cloudinaryAccount));
    builder.Services.AddScoped<IPhotoService, CloudinaryService>();
    Log.Information("✅ Cloudinary configurado correctamente.");
}
else
{
    Log.Error("❌ ERROR CRÍTICO: Cloudinary NO se pudo configurar. La subida de fotos fallará.");
    // No registramos el servicio para que la app arranque al menos
}
// ------------------------------------------------

// Configure Azure AI Vision (Análisis de fotos con IA)
builder.Services.AddScoped<IImageAnalysisService, AzureImageAnalysisService>();

// Configuración manual para soportar variables de entorno personalizadas (AZURE_VISION_ENDPOINT)
// y mantener compatibilidad con appsettings.json
builder.Services.Configure<ApexVision.Backend.DTOs.AzureVisionSettings>(settings =>
{
    // 1. Cargar desde appsettings (AzureVision)
    // Nota: Cambié el nombre de sección recomendada a "AzureVision" en pasos anteriores, pero el código original usaba "AzureVisionSettings".
    // Voy a soportar ambos para seguridad o usar el nuevo.
    // El paso anterior añadió "AzureVision" a appsettings.json.
    configuration.GetSection("AzureVision").Bind(settings);
    
    // 2. Sobrescribir con variables de entorno específicas si existen (estilo Docker del usuario)
    var envEndpoint = configuration["AZURE_VISION_ENDPOINT"];
    var envKey = configuration["AZURE_VISION_KEY"];
    
    if (!string.IsNullOrEmpty(envEndpoint)) settings.Endpoint = envEndpoint;
    if (!string.IsNullOrEmpty(envKey)) settings.Key = envKey;
});

builder.Services.AddScoped<IAiValidationService, AiValidationService>();

// Configure Optimization Service (Optimización de rutas con Java backend)
builder.Services.AddScoped<IOptimizationService, OptimizationService>();

// Configure Route Service (Guardar y cargar rutas)
builder.Services.AddScoped<IRouteService, RouteService>();

// Register HttpClientFactory for services that need it
builder.Services.AddHttpClient();

builder.Services.AddHttpClient("JavaOptimizationApi", client =>
{
    client.BaseAddress = new Uri("http://apex-java:8081/");
    // Configure other HttpClient settings like headers if needed
});


builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Configurar serialización JSON: aceptar camelCase o PascalCase, y devolver camelCase
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ApexVision API", Version = "v1" });

    // Configure Swagger to use JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Handle IFormFile properly in Swagger
    c.OperationFilter<ApexVision.Backend.Filters.FileUploadOperationFilter>();
});

var app = builder.Build();

// --- ZONA DE DESPLIEGUE AUTOMÁTICO (Migraciones y Seed) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<Role>>();

        // 1. Aplicar Migraciones pendientes (Crea las tablas si no existen)
        Log.Information("Aplicando migraciones de base de datos...");
        context.Database.Migrate();

        // 2. Crear Roles si no existen
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new Role { Name = "Admin" });
            Log.Information("Rol 'Admin' creado.");
        }
        
        if (!await roleManager.RoleExistsAsync("Driver"))
        {
            await roleManager.CreateAsync(new Role { Name = "Driver" });
            Log.Information("Rol 'Driver' creado.");
        }

        // 3. Crear Usuario Admin por defecto (Para que puedas entrar)
        var adminEmail = "admin@apexvision.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = "admin",
                Email = adminEmail,
                FullName = "Admin Principal",
                PhoneNumber = "+573001234567"
            };
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Log.Information("Usuario Admin creado y asignado al rol 'Admin'.");
            }
            else
            {
                Log.Error("Error al crear usuario Admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Si el usuario ya existe, nos aseguramos de que tenga el rol Admin
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Log.Information("Usuario Admin existente fue asignado al rol 'Admin'.");
            }
            else
            {
                Log.Information("Usuario Admin ya existe y tiene el rol correcto.");
            }
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Ocurrió un error durante la migración o el seeding.");
    }
}
// -----------------------------------------------------------

// Configure the HTTP request pipeline.
// Add exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// SWAGGER SIEMPRE HABILITADO (necesario para Docker/VPS y demostración)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ApexVision API v1");
    c.RoutePrefix = "swagger";  // Disponible en /swagger en lugar de /swagger/index.html
});

// COMENTADO PARA EVITAR BUCLES CON NGINX
// Nginx Proxy Manager ya maneja HTTPS→HTTP internamente
// Si dejamos esta línea activa, crea un bucle infinito de redirecciones
// app.UseHttpsRedirection();

app.UseRouting(); // Mover UseRouting aquí

app.UseCors("AllowAll"); // Mover UseCors aquí, después de UseRouting

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
