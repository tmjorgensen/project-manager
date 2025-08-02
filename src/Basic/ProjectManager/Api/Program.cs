using Api.Domain;
using Api.Endpoints;
using Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services
    .AddAuthentication()
    .AddJwtBearer(options =>
    {
        builder.Configuration.GetAuthSettings(out string secret, out string issuer, out string audience);

        options.TokenValidationParameters = new()
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Log to console.
builder.Services.AddLogging(logging => logging.AddConsole());

// Use string conversion for enums.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();

var anonymousEndpoints = app.MapGroup("/").AllowAnonymous();

anonymousEndpoints.MapPost("/sign-in", Security.SignIn);

var authenticatedEndpoints = app.MapGroup("/").RequireAuthorization();

authenticatedEndpoints.MapGet("/projects", Projects.SearchProjects);
authenticatedEndpoints.MapPost("/projects", Projects.CreateProject);
authenticatedEndpoints.MapGet("/projects/{id:guid}", Projects.GetProjectById).WithName(nameof(Projects.GetProjectById));
authenticatedEndpoints.MapPut("/projects/{id:guid}", Projects.UpdateProject);
authenticatedEndpoints.MapDelete("/projects/{id:guid}", Projects.DeleteProject);

authenticatedEndpoints.MapGet("/activities", Activities.SearchActivities);
authenticatedEndpoints.MapPost("/activities", Activities.CreateActivity);
authenticatedEndpoints.MapGet("/activities/{id:guid}", Activities.GetActivityById).WithName(nameof(Activities.GetActivityById));
authenticatedEndpoints.MapPut("/activities/{id:guid}", Activities.UpdateActivity);
authenticatedEndpoints.MapDelete("/activities/{id:guid}", Activities.DeleteActivity);

app.Run();
