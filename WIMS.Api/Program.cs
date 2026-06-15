using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using WIMS.Api.Middlewares;
using WIMS.Application.Common.Services;
using WIMS.Application.CommonServices;
using WIMS.Application.DTOs;
using WIMS.Application.Interfaces.Common;
using WIMS.Application.Interfaces.Repositories;
using WIMS.Application.Interfaces.Services.Admin;
using WIMS.Application.Interfaces.Services.Audit;
using WIMS.Application.Interfaces.Services.Auth;
using WIMS.Application.Interfaces.Services.ProductCategory;
using WIMS.Application.Interfaces.Services.Profile;
using WIMS.Application.Interfaces.Services.UnitOfMeasure;
using WIMS.Application.Interfaces.Services.WarehouseManagement;
using WIMS.Application.Mappings;
using WIMS.Application.Service.Admin;
using WIMS.Application.Service.Audit;
using WIMS.Application.Service.Auth;
using WIMS.Application.Service;
using WIMS.Application.Service.Profile;
using WIMS.Application.Service.UnitOfMeasure;
using WIMS.Application.Service.WarehouseManagement;
using WIMS.Application.Validators.Admin;
using WIMS.Infrastructure.Data;
using WIMS.Infrastructure.Data.Seeder.Implementation;
using WIMS.Infrastructure.Data.Seeder.Interface;
using WIMS.Infrastructure.Repository;
using WIMS.Application.Interfaces.Services.Products;
using WIMS.Application.Service.Products;
using WIMS.Application.Interfaces.Services.PurchaseOrder;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WIMS API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",

        Type = SecuritySchemeType.Http,

        Scheme = "bearer",

        BearerFormat = "JWT",

        In = ParameterLocation.Header,

        Description =
            "Enter JWT Token like this: Bearer your_token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

            Array.Empty<string>()
        }
    });
});


builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
    )
);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();

            var response = ApiResponse<object>.Failure(
                message: "Validation failed.",
                errors: errors,
                statusCode: 400
            );

            return new BadRequestObjectResult(response);
        };
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });


//fluent validation
builder.Services.AddFluentValidationAutoValidation();
ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

//DI
//Helper
builder.Services.AddScoped<ISeeder, Seeder>();
builder.Services.AddSingleton<IInputNormalizer, InputNormalizer>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<ICodeGeneratorService, CodeGeneratorService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();


//Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IAdminUserManagementService, AdminUserManagementService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IZoneService, ZoneService>();
builder.Services.AddScoped<ZoneService>();
builder.Services.AddScoped<IBinService, BinService>();
builder.Services.AddScoped<IUnitOfMeasureService, UnitOfMeasureService>();
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPoService,PoService>();


//repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IZoneRepository, ZoneRepository>();
builder.Services.AddScoped<IBinRepository, BinRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IUnitOfMeasureRepository,UnitOfMeasureRepository>();
builder.Services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IPoRepository , PoRepository>();
builder.Services.AddScoped<IPoItemRepository,PoItemRepository>();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsql => npgsql.MigrationsAssembly("WIMS.Infrastructure")));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(UserMappingProfile).Assembly);
});

//jwt authenticationcd
builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        ),

        ClockSkew = TimeSpan.Zero
    };
});

//Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("ManagerOrAbove", policy =>
        policy.RequireRole("Administrator", "WarehouseManager"));
    options.AddPolicy("StockKeeperOrAbove", policy =>
        policy.RequireRole("Administrator", "WarehouseManager", "StockKeeper"));
});


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.Logger(lc => lc
        .Filter.ByExcluding(
            logEvent => logEvent.Level >= LogEventLevel.Error)
        .WriteTo.File(
            "logs/requests-.txt",
            rollingInterval: RollingInterval.Day))

    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(
            logEvent => logEvent.Level >= LogEventLevel.Error)
        .WriteTo.File(
            "logs/errors-.txt",
            rollingInterval: RollingInterval.Day))

    .CreateLogger();


builder.Host.UseSerilog();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
   {
       options.ConfigObject.AdditionalItems["persistAuthorization"] = true;
   });

}

app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var seeders = scope.ServiceProvider.GetServices<ISeeder>();
    foreach (var seeder in seeders)
        await seeder.SeedAsync();
}

app.Run();
