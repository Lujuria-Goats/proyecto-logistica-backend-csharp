using System.Text;
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

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
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

// Add Identity services
builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 2. Register JWT Service
builder.Services.AddScoped<JwtService>();

// Configure Cloudinary
var cloudinaryAccount = new Account(
    builder.Configuration["Cloudinary:CloudName"],
    builder.Configuration["Cloudinary:ApiKey"],
    builder.Configuration["Cloudinary:ApiSecret"]
);
builder.Services.AddSingleton(new Cloudinary(cloudinaryAccount));
builder.Services.AddScoped<IPhotoService, CloudinaryService>();

// Configure Azure AI Vision (Análisis de fotos con IA)
builder.Services.AddScoped<IImageAnalysisService, AzureImageAnalysisService>();
builder.Services.Configure<ApexVision.Backend.DTOs.AzureVisionSettings>(builder.Configuration.GetSection("AzureVisionSettings"));
builder.Services.AddScoped<IAiValidationService, AiValidationService>();

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

builder.Services.AddControllers();

// 3. Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado")))
    };
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
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new User
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
                Log.Information("Usuario Admin creado: admin@apexvision.com / Admin123!");
            }
            else
            {
                Log.Error("Error al crear usuario Admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            Log.Information("Usuario Admin ya existe.");
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

app.UseCors("AllowAll"); // Apply the CORS policy

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
