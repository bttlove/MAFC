using pviBase.Configurations;
using pviBase.Data;
using pviBase.Middlewares;
using pviBase.Services;
using pviBase.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Serilog;
using Serilog.Events;
using Newtonsoft.Json; // Đảm bảo dòng này có
using Newtonsoft.Json.Serialization;
using pviBase.BackgroundTasks; // For Hangfire tasks

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
.MinimumLevel.Debug()
.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
.Enrich.FromLogContext()
.WriteTo.Console()
.WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
.CreateLogger();

builder.Logging.ClearProviders();
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson() // If you prefer Newtonsoft.Json for deserialization
    .AddFluentValidation(fv =>
    {
        fv.DisableDataAnnotationsValidation = true;
        fv.RegisterValidatorsFromAssemblyContaining<InsuranceContractRequestDtoValidator>();
        fv.RegisterValidatorsFromAssemblyContaining<CreateContractRequestDtoValidator>();
    });

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    })
    .AddFluentValidation(fv =>
    {
        fv.DisableDataAnnotationsValidation = true;
        fv.RegisterValidatorsFromAssemblyContaining<InsuranceContractRequestDtoValidator>();
        fv.RegisterValidatorsFromAssemblyContaining<CreateContractRequestDtoValidator>();
        // THÊM DÒNG NÀY:
        fv.RegisterValidatorsFromAssemblyContaining<GetContractByLoanNoRequestDtoValidator>();
    });
// Entity Framework Core with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version"),
        new QueryStringApiVersionReader("api-version")
    );
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Swagger/OpenAPI
builder.Services.AddSwaggerGen(options =>
{
    var apiVersionDescriptionProvider = builder.Services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
    {
        options.SwaggerDoc(description.GroupName, new OpenApiInfo
        {
            Title = $"Insurance Hub API {description.ApiVersion}",
            Version = description.ApiVersion.ToString(),
            Description = "API for managing insurance contracts."
        });
    }
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policyBuilder => policyBuilder.WithOrigins("http://localhost:4200", "http://localhost:3000") // Add your allowed origins
                            .AllowAnyHeader()
                            .AllowAnyMethod());
});



// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisCacheSettings:ConnectionString");
    options.InstanceName = "InsuranceHub_";
});

// Hangfire for Task Scheduling
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();
builder.Services.AddTransient<SampleBackgroundTask>(); // Register your background task service

// Register Services
builder.Services.AddScoped<IInsuranceService, InsuranceService>();

var app = builder.Build();

// Apply migrations on startup (for development only, for production, handle migrations carefully)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.

// Exception handling middleware at the very top to catch all exceptions
app.UseExceptionHandlingMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        }
    });
}

app.UseHttpsRedirection(); // Recommended for production

app.UseRouting();

// CORS must be before Authorization and Authentication
app.UseCors("AllowSpecificOrigin");

// Rate Limiting
 // Apply after routing and CORS

// IP Whitelisting should be before endpoint execution
app.UseIpWhitelistMiddleware();

// Authentication and Authorization (if you add them later)
// app.UseAuthentication();
// app.UseAuthorization();

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire");

// Example of background job setup using your service
RecurringJob.AddOrUpdate<SampleBackgroundTask>(
    "DailyCleanupJob",
    x => x.PerformDailyDataCleanup(),
    Cron.Daily() // Runs once a day
);

// Response Wrapping should be after the main processing but before writing to client
app.UseResponseWrappingMiddleware(); // Place after other processing middlewares like routing, auth.

app.MapControllers();

app.Run();