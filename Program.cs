using ApiBiblioteca.Models;
using ApiBiblioteca.Services;
using ApiBiblioteca.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using Quartz;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;



// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()  // Permite tu frontend en localhost:7053
              .AllowAnyMethod()  // Permite GET, POST, PUT, etc.
              .AllowAnyHeader(); // Permite cualquier header
    });
});

// Configurar Entity Framework
builder.Services.AddDbContext<DbContextBiblioteca>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("StringConnection")));
//Configurar Quartz
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("RecordarPrestamosProntosVencerJob");
    q.AddJob<RecordarPrestamosServices>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(t => t
        .ForJob(jobKey)
        .WithIdentity("RecordarPrestamosProntosVencerJob-trigger")
        .WithCronSchedule("0 0 8 * * ?")); // Ejecutar cada dia 08:00 AM


    var jobKey2 = new JobKey("ProcesarReservasVencidasJob");
    q.AddJob<ProcesarReservasVencidasJob>(opts => opts.WithIdentity(jobKey2));
    q.AddTrigger(t => t
        .ForJob(jobKey2)
        .WithIdentity("ProcesarReservasVencidasJob-trigger")
        .WithCronSchedule("0 0/10 * * * ?")); // Ejecutar cada 3 horas
});




builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Configurar ImportDefaults

builder.Services.Configure<ImportDefaults>(
    builder.Configuration.GetSection("ImportDefaults"));


// Configurar JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// Registrar el servicio de autorización
builder.Services.AddScoped<IAutorizacionService, AutorizacionService>();
builder.Services.AddScoped<ILibroImportService, LibroImportService>();

builder.Configuration.AddJsonFile("emailsettings.json", optional: true, reloadOnChange: true);

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordResetService, PasswordService>();
builder.Services.AddScoped<INotificacionesServices, NotificacionesServices>();
builder.Services.AddScoped<IEstadisticasService, EstadisticasService>();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll"); 

// IMPORTANTE: UseAuthentication debe ir antes de UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();