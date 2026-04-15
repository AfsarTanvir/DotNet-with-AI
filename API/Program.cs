using API.Middleware;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notes.Application.Behaviors;
using Notes.Application.Commands.CreateNote;
using Notes.Application.Interfaces;
using Notes.Infrastructure;
using Notes.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(CreateNoteHandler).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(CreateNoteCommand).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Add services to the container.
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Notes API v1");
    });
}

// Log ALL HTTP requests FIRST
app.UseSerilogRequestLogging();

// Then exception middleware
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
