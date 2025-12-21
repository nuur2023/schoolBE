using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Data.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not found"));
var issuer = jwtSettings["Issuer"] ?? "SchoolMS";
var audience = jwtSettings["Audience"] ?? "SchoolMS";

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
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero // Remove delay of token when expire
    };

    // Read token from cookie
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["authToken"];
            return Task.CompletedTask;
        }
    };
});

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Register Repositories
builder.Services.AddScoped<IAccounts, AccountsRepository>();
builder.Services.AddScoped<IAttendance, AttendanceRepository>();
builder.Services.AddScoped<IChartOfAccount, ChartOfAccountRepository>();
builder.Services.AddScoped<IClasses, ClassesRepository>();
builder.Services.AddScoped<IClassSubjects, ClassSubjectsRepository>();
builder.Services.AddScoped<IContacts, ContactsRepository>();
builder.Services.AddScoped<IExamResults, ExamResultsRepository>();
builder.Services.AddScoped<IExamSchedule, ExamScheduleRepository>();
builder.Services.AddScoped<IExams, ExamsRepository>();
builder.Services.AddScoped<IExpense, ExpenseRepository>();
builder.Services.AddScoped<IFiscalYear, FiscalYearRepository>();
builder.Services.AddScoped<IJournalDetails, JournalDetailsRepository>();
builder.Services.AddScoped<IJournalEntry, JournalEntryRepository>();
builder.Services.AddScoped<ILevels, LevelsRepository>();
builder.Services.AddScoped<ILevelSubjects, LevelSubjectsRepository>();
builder.Services.AddScoped<IMenus, MenusRepository>();
builder.Services.AddScoped<IPayment, PaymentRepository>();
builder.Services.AddScoped<IPaymentMethods, PaymentMethodsRepository>();
builder.Services.AddScoped<IPayRoll, PayRollRepository>();
builder.Services.AddScoped<IPermissions, PermissionsRepository>();
builder.Services.AddScoped<IPersons, PersonsRepository>();
builder.Services.AddScoped<IRoles, RolesRepository>();
builder.Services.AddScoped<ISchoolAccounts, SchoolAccountsRepository>();
builder.Services.AddScoped<ISchools, SchoolsRepository>();
builder.Services.AddScoped<ISchoolUsers, SchoolUsersRepository>();
builder.Services.AddScoped<IStages, StagesRepository>();
builder.Services.AddScoped<IStudentEnrollments, StudentEnrollmentsRepository>();
builder.Services.AddScoped<ISubjects, SubjectsRepository>();
builder.Services.AddScoped<ISubmenus, SubmenusRepository>();
builder.Services.AddScoped<IUsers, UsersRepository>();
builder.Services.AddScoped<IYearEndClosingEntry, YearEndClosingEntryRepository>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "School Management API", Version = "v1" });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
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
            Array.Empty<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:4200") // Add your frontend URLs
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Allow cookies
    });
});

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SchoolContext>();
        SeedData.Seed(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

