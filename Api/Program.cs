using Api.Controllers;
using Api.Middleware;
using Api.Services;
using Application;
using Domain.Repositories;
using Infrastructure;
using Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System;
using System.Text;

// Método principal asíncrono
return await Main(args);

async Task<int> Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);


    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.Limits.MaxRequestBodySize = 8589934592; // 8 GB
        serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromHours(10);
        serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromHours(10);
        serverOptions.Limits.MaxResponseBufferSize = 8589934592; // 8 GB

        // Configuraciones para buffering de requests
        serverOptions.Limits.MaxRequestBufferSize = 1048576; // 1MB buffer
        serverOptions.Limits.MaxRequestHeaderCount = 100;
        serverOptions.Limits.MaxRequestHeadersTotalSize = 32768; // 32KB headers
        serverOptions.Limits.MaxRequestLineSize = 8192; // 8KB request line

        // HTTP/2 settings
        serverOptions.Limits.Http2.MaxStreamsPerConnection = 100;
        serverOptions.Limits.Http2.HeaderTableSize = 4096;
        serverOptions.Limits.Http2.MaxFrameSize = 16384;
        serverOptions.Limits.Http2.MaxRequestHeaderFieldSize = 8192;
    });

    // Configurar FormOptions para manejar grandes archivos de formulario
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 8589934592; // 8 GB
        options.ValueCountLimit = int.MaxValue; // Sin límite específico
    });
    
	// Serilog
    builder.Host.UseSerilog((ctx, lc) =>
{
	lc.ReadFrom.Configuration(ctx.Configuration)
	  .Enrich.FromLogContext()
	  .WriteTo.Console();
});

// Registrar servicios de aplicación
builder.Services.AddApplicationServices();

// Registrar servicios de infraestructura
builder.Services.AddInfrastructureServices(builder.Configuration);

// Controllers + Validation
builder.Services.AddControllers();

// Servicios de la API
builder.Services.AddHttpClient();
builder.Services.AddScoped<Api.Services.DownloadMultipleCV>();
builder.Services.AddScoped<Api.Services.PDFDownloadZipService>();
builder.Services.AddScoped<IPlaywrightService, PlaywrightService>();
builder.Services.AddScoped<IPandapeApiService, PandapeApiService>();
builder.Services.AddScoped<Api.Services.LdapService>();
builder.Services.AddHostedService<Api.Services.PeriodicRequestService>();
builder.Services.AddScoped<IUnitOfWork,UnitOfWork>();
builder.Services.AddScoped<Api.Services.UserService>();
builder.Services.AddScoped<Api.Services.ResponseBDService>();
builder.Services.AddScoped<Api.Services.WebScrapingParserService>();
builder.Services.AddScoped<Api.Services.RoleService>();
builder.Services.AddScoped<Api.Services.PermissionService>();

// CORS (permitir cualquier origen)
builder.Services.AddCors(opt =>
{
	opt.AddPolicy("Default", p => 
		p.SetIsOriginAllowed(origin =>
		{
			var host = new Uri(origin).Host;
			return host == "localhost" || host == "http://localhost:3000" || host == "10.128.50.17" || host == "10.128.50.16";
		})
		.AllowAnyOrigin()
		.AllowAnyHeader()
		.AllowAnyMethod());
});

// Auth (JWT con validación de permisos)
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "dev-secret-key-secure-default-2024"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(o =>
	{
		o.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = false,
			ValidateAudience = false,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = key,
			RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
			NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
		};
		
		// Configurar para aceptar tokens sin el prefijo "Bearer"
		o.Events = new JwtBearerEvents
		{
			OnMessageReceived = context =>
			{
				var token = context.Request.Headers["Authorization"].FirstOrDefault();
				if (!string.IsNullOrEmpty(token))
				{
					// Si el token ya tiene el prefijo "Bearer ", lo usamos tal como está
					if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
					{
						context.Token = token.Substring("Bearer ".Length).Trim();
					}
					// Si no tiene el prefijo, asumimos que es solo el token
					else
					{
						context.Token = token;
					}
				}
				return Task.CompletedTask;
			}
		};
	});

// Autorización basada en políticas para permisos
builder.Services.AddAuthorization(options =>
{
	// Políticas basadas en permisos para usuarios
	options.AddPolicy("CanReadUsers", policy => policy.RequireClaim("permission", "users.read"));
	options.AddPolicy("CanWriteUsers", policy => policy.RequireClaim("permission", "users.write"));
	options.AddPolicy("CanDeleteUsers", policy => policy.RequireClaim("permission", "users.delete"));
	
	// Políticas basadas en permisos para contenido
	options.AddPolicy("CanReadContent", policy => policy.RequireClaim("permission", "content.read"));
	options.AddPolicy("CanWriteContent", policy => policy.RequireClaim("permission", "content.write"));
	options.AddPolicy("CanDeleteContent", policy => policy.RequireClaim("permission", "content.delete"));
	
	// Políticas basadas en permisos para roles
	options.AddPolicy("CanReadRoles", policy => policy.RequireClaim("permission", "roles.read"));
	options.AddPolicy("CanWriteRoles", policy => policy.RequireClaim("permission", "roles.write"));
	options.AddPolicy("CanDeleteRoles", policy => policy.RequireClaim("permission", "roles.delete"));
	
	// Políticas basadas en permisos para permisos
	options.AddPolicy("CanReadPermissions", policy => policy.RequireClaim("permission", "permissions.read"));
	options.AddPolicy("CanWritePermissions", policy => policy.RequireClaim("permission", "permissions.write"));
	options.AddPolicy("CanDeletePermissions", policy => policy.RequireClaim("permission", "permissions.delete"));
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { 
		Title = "SG_Semilla API", 
		Version = "v1",
		Description = "API de ejemplo con Clean Architecture + CQRS"
	});
	
	// Configuración para JWT en Swagger
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
			new string[] { }
		}
	});
});

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging();

// Registrar middleware de manejo global de excepciones
app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Aplicar migraciones y sembrar datos iniciales al iniciar
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Aplicar migraciones
        var dbContext = services.GetRequiredService<Infrastructure.Persistence.AppDbContext>();
        dbContext.Database.Migrate();
        logger.LogInformation("Migraciones aplicadas correctamente");
        
        // Sembrar datos iniciales
        var dataSeeder = services.GetRequiredService<Infrastructure.Persistence.DataSeeder>();
        await dataSeeder.SeedAsync();
        logger.LogInformation("Datos iniciales sembrados correctamente");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error durante la inicialización de la base de datos");
    }
}

app.Run();
    return 0;
}
