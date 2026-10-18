using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Data.Repositories;
using schoolBE.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; // Use camelCase for JSON
    });

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

    // Read token from cookie or Authorization header
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // First try to get token from cookie
            var token = context.Request.Cookies["authToken"];
            
            // If no cookie, try Authorization header (for cross-origin requests)
            if (string.IsNullOrEmpty(token))
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    token = authHeader.Substring("Bearer ".Length).Trim();
                }
            }
            
            context.Token = token;
            return Task.CompletedTask;
        }
    };
});

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("ConStr") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Add HttpContextAccessor As AddSingleton 
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();


// Register Repositories
builder.Services.AddScoped<IAttendance, AttendanceRepository>();
builder.Services.AddScoped<IClasses, ClassesRepository>();
builder.Services.AddScoped<IClassSubjects, ClassSubjectsRepository>();
builder.Services.AddScoped<IContacts, ContactsRepository>();
builder.Services.AddScoped<IExamResults, ExamResultsRepository>();
builder.Services.AddScoped<IExamSchedule, ExamScheduleRepository>();
builder.Services.AddScoped<IExamSubject, ExamSubjectRepository>();
builder.Services.AddScoped<IExams, ExamsRepository>();
builder.Services.AddScoped<IExamParticipants, ExamParticipantsRepository>();
builder.Services.AddScoped<IExamRooms, ExamRoomsRepository>();
builder.Services.AddScoped<IExpense, ExpenseRepository>();
builder.Services.AddScoped<ILevels, LevelsRepository>();
builder.Services.AddScoped<ILevelSubjects, LevelSubjectsRepository>();
builder.Services.AddScoped<IMenus, MenusRepository>();
builder.Services.AddScoped<IPayment, PaymentRepository>();
builder.Services.AddScoped<IPaymentMethods, PaymentMethodsRepository>();
builder.Services.AddScoped<IFeeCategories, FeeCategoriesRepository>();
builder.Services.AddScoped<IPayRoll, PayRollRepository>();
builder.Services.AddScoped<IPermissions, PermissionsRepository>();
builder.Services.AddScoped<IPersons, PersonsRepository>();
builder.Services.AddScoped<IRoles, RolesRepository>();
builder.Services.AddScoped<ISchools, SchoolsRepository>();
builder.Services.AddScoped<ISchoolUsers, SchoolUsersRepository>();
builder.Services.AddScoped<IStages, StagesRepository>();
builder.Services.AddScoped<IStudentEnrollments, StudentEnrollmentsRepository>();
builder.Services.AddScoped<IStudentExit, StudentExitRepository>();
builder.Services.AddScoped<IStudentGraduation, StudentGraduationRepository>();
builder.Services.AddScoped<IStudentTransfer, StudentTransferRepository>();
builder.Services.AddScoped<IApp, AppRepository>();
builder.Services.AddScoped<ISubjects, SubjectsRepository>();
builder.Services.AddScoped<ISubmenus, SubmenusRepository>();
builder.Services.AddScoped<IUsers, UsersRepository>();
builder.Services.AddScoped<IStudentChargePostingService, StudentChargePostingService>();
builder.Services.AddScoped<AutoChargeRunner>();
builder.Services.AddHostedService<AutoChargeBackgroundService>();

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
    options.AddPolicy("CorePolicy",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:5173", "http://127.0.0.1:5174", "http://10.111.29.20:5174") // Allow frontend origins
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // Required when using credentials: 'include' in the frontend
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

// CORS must be before UseHttpsRedirection to avoid redirect issues with preflight requests
app.UseCors("CorePolicy");

// Enable static files to serve images from the Images directory
var imagesPath = Path.Combine(builder.Environment.ContentRootPath, "Images");
if (!Directory.Exists(imagesPath))
{
    Directory.CreateDirectory(imagesPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(imagesPath),
    RequestPath = "/Images",
    OnPrepareResponse = ctx =>
    {
        // Allow the SPA (different origin/port) to fetch photos for PDF generation.
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600");
    }
});

// Disable HTTPS redirection in development to avoid CORS preflight issues
// Only redirect to HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Configure to listen on all interfaces (0.0.0.0) so mobile devices can connect
var urls = new[] { "http://0.0.0.0:5028" };
app.Urls.Clear();
foreach (var url in urls)
{
    app.Urls.Add(url);
}

app.Run();