# Quick Reference Guide

## 🎯 Project Structure at a Glance

```
Notes Application (Clean Architecture)
│
├── 🔵 API Layer (Presentation)
│   └── Controllers/ → REST endpoints
│
├── 🟢 Application Layer (Use Cases)
│   ├── Commands/ → Write operations
│   ├── Queries/ → Read operations
│   └── Handlers/ → Business orchestration
│
├── 🟡 Domain Layer (Business Rules)
│   ├── Entities/ → Note, User
│   ├── Exceptions/ → Custom errors
│   └── Common/ → Base Entity class
│
└── 🔴 Infrastructure Layer (Data Access)
    ├── AppDbContext → Database configuration
    ├── Repositories/ → Data access
    └── UnitOfWork → Transaction management
```

---

## 📡 API Endpoints

| Method | Endpoint | Handler | Purpose |
|--------|----------|---------|---------|
| POST | `/api/notes` | CreateNoteHandler | Create a new note |
| GET | `/api/notes` | GetNotesHandler | Get all notes |
| PUT | `/api/notes/{id}` | UpdateNoteHandler | Update a note |
| DELETE | `/api/notes/{id}` | DeleteNoteHandler | Delete a note (soft) |

---

## 🔄 Request/Response Flow

```
HTTP Request
    ↓
Controller (API Layer)
    ↓
Handler (Application Layer)
    ↓
Entity Validation (Domain Layer)
    ↓
Repository (Infrastructure Layer)
    ↓
Database (PostgreSQL)
    ↓
UnitOfWork.SaveChangesAsync()
    ↓
HTTP Response
```

---

## 💾 Database Models

### User Entity
```csharp
public class User : Entity
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public bool IsActive { get; set; }
}
```

### Note Entity
```csharp
public class Note : Entity
{
    public string Title { get; set; }
    public string? Content { get; set; }
    public Guid CreatedBy { get; set; } // User ID
}
```

### Base Entity
```csharp
public abstract class Entity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; } // Soft delete
    public bool IsDeleted { get; set; }
}
```

---

## 🛠️ How to Add a New Feature

### Example: Add "Publish Note" Feature

**Step 1: Create Domain Method** (Domain Layer)
```csharp
// In Note.cs
public void Publish()
{
    if (IsPublished)
        throw new ArgumentException("Already published");
    
    IsPublished = true;
    PublishedAt = DateTime.UtcNow;
}
```

**Step 2: Create Command** (Application Layer)
```csharp
// Commands/PublishNote/PublishNoteCommand.cs
public record PublishNoteCommand(Guid NoteId);
```

**Step 3: Create Handler** (Application Layer)
```csharp
// Commands/PublishNote/PublishNoteHandler.cs
public class PublishNoteHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public PublishNoteHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PublishNoteCommand command, CancellationToken ct)
    {
        var note = await _unitOfWork.Notes.GetByIdAsync(command.NoteId, ct);
        
        if (note == null)
            throw new NoteNotFoundException(command.NoteId);

        note.Publish(); // Domain method called

        _unitOfWork.Notes.Update(note);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

**Step 4: Register Handler** (API Layer)
```csharp
// In Program.cs
builder.Services.AddScoped<PublishNoteHandler>();
```

**Step 5: Add Endpoint** (API Layer)
```csharp
// In NotesController.cs
[HttpPost("{id}/publish")]
public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
{
    await _publishHandler.Handle(new PublishNoteCommand(id), ct);
    return NoContent();
}
```

---

## ⚠️ Common Mistakes to Avoid

### ❌ Wrong: Business Logic in Handler
```csharp
public async Task Handle(CreateNoteCommand cmd, CT ct)
{
    if (string.IsNullOrEmpty(cmd.Title))
        throw new Exception("Title required"); // Wrong place
}
```

### ✅ Correct: Business Logic in Entity
```csharp
// In Note.cs
public Note(string title, ...)
{
    SetTitle(title); // Entity validates
}

public void SetTitle(string title)
{
    if (string.IsNullOrWhiteSpace(title))
        throw new ArgumentException("Title required");
    
    Title = title;
}
```

---

### ❌ Wrong: Handler Uses AppDbContext
```csharp
public CreateNoteHandler(AppDbContext context) // Direct dependency
{
    _context = context;
}
```

### ✅ Correct: Handler Uses IUnitOfWork
```csharp
public CreateNoteHandler(IUnitOfWork unitOfWork) // Abstraction
{
    _unitOfWork = unitOfWork;
}
```

---

### ❌ Wrong: Generic Exceptions
```csharp
if (note == null)
    throw new Exception("Not found"); // Too generic
```

### ✅ Correct: Domain-Specific Exceptions
```csharp
if (note == null)
    throw new NoteNotFoundException(id); // Semantic
```

---

## 🧪 Testing Pattern

### Testing a Handler
```csharp
[TestFixture]
public class CreateNoteHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private CreateNoteHandler _handler;

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateNoteHandler(_unitOfWorkMock.Object);
    }

    [Test]
    public async Task Handle_ValidCommand_CreateNote()
    {
        // Arrange
        var command = new CreateNoteCommand("Title", "Content", Guid.NewGuid());

        // Act
        var noteId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(noteId, Is.Not.EqualTo(Guid.Empty));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_EmptyTitle_ThrowsValidationException()
    {
        // Arrange
        var command = new CreateNoteCommand("", "Content", Guid.NewGuid());

        // Act & Assert
        Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None)
        );
    }
}
```

---

## 📋 File Checklist for New Handlers

When adding a new command handler, create:

- [ ] `Commands/OperationName/OperationNameCommand.cs` - Record with parameters
- [ ] `Commands/OperationName/OperationNameHandler.cs` - Handler class with Handle method
- [ ] Update `Program.cs` - Register handler: `AddScoped<OperationNameHandler>()`
- [ ] Update `NotesController.cs` - Add endpoint and dependency
- [ ] Add tests - Unit tests for handler
- [ ] Update `ARCHITECTURE.md` - Document new feature

---

## 🔑 Key Principles to Remember

1. **Domain First** - Business logic in entities, not handlers
2. **Abstraction Over Implementation** - Use interfaces, inject dependencies
3. **Single Responsibility** - One handler, one operation
4. **Async All The Way** - All I/O is async with CancellationToken
5. **Soft Delete Always** - Never permanently delete (use SoftDelete())
6. **Custom Exceptions** - Semantic, meaningful error types
7. **Immutable Records** - Commands and queries are records
8. **No Leaky Abstractions** - Don't expose database concerns in API

---

## 🚀 Useful Commands

### Build
```powershell
dotnet build
```

### Run
```powershell
dotnet run --project API/API.csproj
```

### Run Tests
```powershell
dotnet test
```

### Add Migration
```powershell
dotnet ef migrations add MigrationName -p Notes.Infrastructure
```

### Update Database
```powershell
dotnet ef database update -p Notes.Infrastructure
```

### View SQL (Debug)
```powershell
# In AppDbContext.OnConfiguring:
optionsBuilder.LogTo(Console.WriteLine);
```

---

## 📞 Getting Help

1. Read `ARCHITECTURE.md` for detailed explanations
2. Check `COMMIT_CHECKLIST.md` for code quality standards
3. Review existing handlers for patterns
4. Look at entity validation in Domain layer
5. Check test examples for usage patterns

---

**Version:** 1.0  
**Last Updated:** 2025  
**Status:** ✅ Production Ready
