using Microsoft.EntityFrameworkCore;
using Notes.Application.Commands.CreateNote;
using Notes.Application.Commands.DeleteNotes;
using Notes.Application.Commands.GetNotes;
using Notes.Application.Commands.UpdateNote;
using Notes.Application.Interfaces;
using Notes.Infrastructure;
using Notes.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();

// Notes Handlers
builder.Services.AddScoped<CreateNoteHandler>();
builder.Services.AddScoped<GetNotesHandler>();
builder.Services.AddScoped<DeleteNoteHandler>();
builder.Services.AddScoped<UpdateNoteHandler>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
