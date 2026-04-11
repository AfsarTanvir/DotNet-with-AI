# Notes Application - Clean Architecture Documentation

## 📋 Project Overview

This is a **Clean Architecture** implementation for a Notes management system built with **.NET 10** and **C# 14**. The project demonstrates proper separation of concerns, dependency injection, domain-driven design, and SOLID principles.

**Key Technologies:**
- .NET 10
- Entity Framework Core 10
- PostgreSQL (Npgsql)
- Clean Architecture Pattern
- Domain-Driven Design (DDD)
- CQRS (Command Query Responsibility Segregation)
- Unit of Work & Repository Pattern

---

## 🏗️ Architecture Layers

### 1. **API Layer** (Presentation/Controllers)
**Location:** `API/Controllers/`

**Responsibility:** Handle HTTP requests and responses  
**Dependencies:** Application layer only  

**Key Files:**
- `NotesController.cs` - REST endpoints for note operations

**Principles:**
- ✅ Controllers only orchestrate handlers
- ✅ No business logic in controllers
- ✅ All I/O is async
- ✅ Delegates all work to Application layer handlers

**Endpoints:**
```
POST   /api/notes              → Create note
GET    /api/notes              → Get all notes
PUT    /api/notes/{id}         → Update note
DELETE /api/notes/{id}         → Delete note (soft delete)
```

---

### 2. **Application Layer** (Use Cases/Commands/Queries)
**Location:** `Notes.Application/`

**Responsibility:** Orchestrate business logic, enforce application rules

**Structure:**

#### Commands (Write Operations)
```
Commands/CreateNote/
  ├── CreateNoteCommand.cs      → Input/Request object
  └── CreateNoteHandler.cs      → Handle create logic

Commands/UpdateNote/
  ├── UpdateNoteCommand.cs      → Input/Request object
  └── UpdateNoteHandler.cs      → Handle update logic

Commands/DeleteNotes/
  ├── DeleteNoteCommand.cs      → Input/Request object
  └── DeleteNoteHandler.cs      → Handle delete logic
```

#### Queries (Read Operations)
```
Commands/GetNotes/
  ├── GetNotesQuery.cs          → Input/Request object
  └── GetNotesHandler.cs        → Handle query logic
```

#### Interfaces (Abstractions)
```
Interfaces/
  ├── INoteRepository.cs        → Data access contract
  └── IUnitOfWork.cs            → Transaction/Persistence contract
```

**Handler Responsibilities:**
1. Receive command/query
2. Validate input (if needed)
3. Coordinate with domain entities
4. Call repository through UnitOfWork
5. Return result or throw exception

**Example - CreateNoteHandler:**
```csharp
public async Task<Guid> Handle(CreateNoteCommand command, CancellationToken ct)
{
    try
    {
        var note = new Note(command.Title, command.Content, command.UserId);
        await _unitOfWork.Notes.AddAsync(note, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return note.Id;
    }
    catch (ArgumentException ex)
    {
        throw new ValidationException(ex.Message);
    }
}
```

**Key Principles:**
- ✅ Handlers depend on **abstractions** (IUnitOfWork), not concrete implementations
- ✅ All data operations go through repositories
- ✅ No direct database access
- ✅ Exception handling converts domain exceptions to application exceptions
- ✅ Commands/Queries are immutable records

---

### 3. **Domain Layer** (Business Rules/Entities)
**Location:** `Notes.Domain/`

**Responsibility:** Contain all business logic and rules. Never depend on other layers.

**Components:**

#### Base Entity Class
```csharp
Common/Entity.cs
├── Id (Guid)                  → Primary key
├── CreatedAt (DateTime)       → Creation timestamp
├── UpdatedAt (DateTime?)      → Last update timestamp
├── DeletedAt (DateTime?)      → Soft delete timestamp
├── IsDeleted (bool)           → Property to check if deleted
├── SoftDelete()               → Mark as deleted
└── EnsureNotDeleted()         → Validate not deleted
```

#### Entities
```
Entities/
├── Note.cs
│   ├── Title (string)         → Required
│   ├── Content (string?)      → Optional
│   ├── CreatedBy (Guid)       → User who created
│   ├── SetTitle()             → Validate & set title
│   └── UpdateContent()        → Update content
│
└── User.cs
    ├── FirstName (string)
    ├── LastName (string)
    ├── Email (string)
    ├── PasswordHash (string)
    ├── IsActive (bool)
    └── UpdateName()           → Validate & update
```

#### Domain Exceptions
```
Exceptions/
├── NoteNotFoundException      → Note with ID not found
├── ValidationException        → Input validation failed
└── (Add more as needed)
```

**Example - Note Entity:**
```csharp
public class Note : Entity
{
    public string Title { get; private set; }
    public string? Content { get; private set; }
    public Guid CreatedBy { get; private set; }

    public Note(string title, string? content, Guid userId)
    {
        SetTitle(title);  // Entity validates itself
        Content = content;
        CreatedBy = userId;
    }

    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty");

        Title = title;
    }
}
```

**Key Principles:**
- ✅ **Business logic lives in entities**, not in handlers
- ✅ **Entities validate themselves** - prevents invalid state
- ✅ **Private setters** - prevent unauthorized changes
- ✅ **Factory methods or constructors** - control entity creation
- ✅ **Domain exceptions** are specific and meaningful
- ✅ **Soft delete support** - records aren't permanently deleted
- ✅ **Immutable by default** - properties have only getters

---

### 4. **Infrastructure Layer** (Data Persistence)
**Location:** `Notes.Infrastructure/`

**Responsibility:** Handle all I/O operations (database, external services)

**Components:**

#### Database Context
```
AppDbContext.cs
├── DbSet<User>
├── DbSet<Note>
└── OnModelCreating()
    ├── Default timestamps (CURRENT_TIMESTAMP)
    ├── Soft delete query filters
    └── Relationships & constraints
```

**Query Filters:**
- Users: Exclude deleted users AND inactive users
- Notes: Exclude deleted notes

#### Repository Implementation
```
Persistence/
├── NoteRepository.cs
│   ├── GetByIdAsync()
│   ├── GetAllAsync()
│   ├── AddAsync()
│   └── Update()
│
└── UnitOfWork.cs
    ├── Notes property (lazy-loaded)
    └── SaveChangesAsync()
```

**Key Principles:**
- ✅ **Unit of Work pattern** - manages all repositories and transactions
- ✅ **Repository pattern** - abstracts data access
- ✅ **All DB calls are async** - supports scalability
- ✅ **EF Core integration** - leverages ORM for data mapping
- ✅ **Query filters** - soft-deleted records automatically excluded
- ✅ **Centralized SaveChanges** - all changes committed together

---

## 📊 Data Flow (Request → Response)

### Scenario: Create a Note

```
1. CLIENT MAKES REQUEST
   POST /api/notes
   {
     "title": "My First Note",
     "content": "Some content here",
     "userId": "550e8400-e29b-41d4-a716-446655440000"
   }

2. API LAYER (NotesController)
   ├─→ Receives HTTP request
   ├─→ Deserializes to CreateNoteCommand
   └─→ Calls _createHandler.Handle(command, ct)

3. APPLICATION LAYER (CreateNoteHandler)
   ├─→ Receives CreateNoteCommand
   ├─→ Creates Note entity: new Note(title, content, userId)
   │   └─→ Entity constructor validates (throws if invalid)
   ├─→ Calls _unitOfWork.Notes.AddAsync(note, ct)
   └─→ Calls _unitOfWork.SaveChangesAsync(ct)

4. INFRASTRUCTURE LAYER (UnitOfWork + NoteRepository)
   ├─→ UnitOfWork.SaveChangesAsync() triggers
   ├─→ EF Core DbContext.SaveChangesAsync()
   ├─→ SQL INSERT executed on PostgreSQL
   └─→ Transaction committed

5. CLIENT RECEIVES RESPONSE
   200 OK
   {
     "id": "550e8400-e29b-41d4-a716-446655440000"
   }
```

### Scenario: Update a Note

```
1. CLIENT MAKES REQUEST
   PUT /api/notes/550e8400-e29b-41d4-a716-446655440000
   {
     "title": "Updated Title",
     "content": "Updated content"
   }

2. API LAYER (NotesController)
   ├─→ Deserializes to UpdateNoteCommand
   └─→ Calls _updateHandler.Handle(command, ct)

3. APPLICATION LAYER (UpdateNoteHandler)
   ├─→ Calls _unitOfWork.Notes.GetByIdAsync(id, ct)
   ├─→ Validates note exists (throws NoteNotFoundException if not)
   ├─→ Calls note.SetTitle(title) → Entity validates
   ├─→ Calls note.UpdateContent(content)
   ├─→ Calls _unitOfWork.Notes.Update(note)
   └─→ Calls _unitOfWork.SaveChangesAsync(ct)

4. INFRASTRUCTURE LAYER
   ├─→ EF Core tracks note changes
   ├─→ SQL UPDATE executed
   └─→ Transaction committed

5. CLIENT RECEIVES RESPONSE
   204 No Content
```

---

## 🎯 Design Patterns Implemented

### 1. **Unit of Work Pattern**
**Purpose:** Manage transactions and coordinate multiple repositories

```csharp
// ❌ Without UoW (wrong - manual transaction management)
_noteRepository.Update(note);
_userRepository.Update(user);
_context.SaveChangesAsync(); // Manual save

// ✅ With UoW (correct - centralized transaction)
_unitOfWork.Notes.Update(note);
_unitOfWork.Users.Update(user);
_unitOfWork.SaveChangesAsync(); // Single transaction, coordinated
```

**Benefits:**
- Single point for transaction management
- Automatic rollback on exception
- Consistent state across all repositories

### 2. **Repository Pattern**
**Purpose:** Abstract data access, making code testable and implementation-agnostic

```csharp
public interface INoteRepository
{
    Task<Note?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Note>> GetAllAsync(CancellationToken ct);
    Task AddAsync(Note note, CancellationToken ct);
    void Update(Note note);
}
```

**Benefits:**
- Swap database implementations without changing handlers
- Easy to mock in unit tests
- Centralized query logic

### 3. **CQRS (Command Query Responsibility Segregation)**
**Purpose:** Separate read and write operations

```csharp
// Commands (Write)
public record CreateNoteCommand(string Title, string? Content, Guid UserId);
public record UpdateNoteCommand(Guid Id, string Title, string? Content);
public record DeleteNoteCommand(Guid Id);

// Queries (Read)
public record GetNotesQuery();
```

**Benefits:**
- Clear separation of concerns
- Can optimize reads and writes independently
- Easier to understand intent

### 4. **Domain-Driven Design (DDD)**
**Purpose:** Business logic lives in domain entities, not in handlers

```csharp
// ❌ Wrong - Handler validates (anemic domain)
public async Task Handle(UpdateNoteCommand cmd, CT ct)
{
    if (string.IsNullOrEmpty(cmd.Title))
        throw new Exception("Title required");
    note.Title = cmd.Title;
}

// ✅ Correct - Entity validates (rich domain)
public async Task Handle(UpdateNoteCommand cmd, CT ct)
{
    note.SetTitle(cmd.Title); // Entity enforces business rules
}
```

**Benefits:**
- Impossible to create invalid entities
- Business logic is reusable
- Clear ownership of rules

### 5. **Dependency Injection / Inversion of Control**
**Purpose:** Loose coupling through abstractions

```csharp
public CreateNoteHandler(IUnitOfWork unitOfWork)
{
    _unitOfWork = unitOfWork; // Depends on abstraction
}
```

**Benefits:**
- Easy to test (mock dependencies)
- Easy to swap implementations
- Framework manages lifetime

---

## ⚠️ Exception Hierarchy

```
Exception (System)
├── ArgumentException (Domain Entity Validation)
│   └── Thrown by: Note.SetTitle(), User.UpdateName()
│
├── ValidationException (Application Layer)
│   └── Thrown by: Handlers catching ArgumentException
│   └── Used for: Input validation
│
├── NoteNotFoundException (Application Layer)
│   └── Thrown by: Handlers
│   └── Used for: Resource not found (404)
│
└── (Framework exceptions: DbUpdateException, etc.)
```

**Exception Flow:**
```
1. Entity throws ArgumentException (business rule violation)
2. Handler catches ArgumentException
3. Handler throws ValidationException (app-level exception)
4. API returns 400 Bad Request
```

---

## 📁 Complete File Structure

```
SaaSApp/
│
├── 📂 API/                          (Presentation Layer)
│   ├── Controllers/
│   │   ├── NotesController.cs       → REST endpoints
│   │   └── WeatherForecastController.cs
│   ├── Program.cs                   → DI configuration
│   ├── appsettings.json
│   └── API.csproj
│
├── 📂 Notes.Application/            (Application Layer)
│   ├── Commands/
│   │   ├── CreateNote/
│   │   │   ├── CreateNoteCommand.cs
│   │   │   └── CreateNoteHandler.cs
│   │   ├── UpdateNote/
│   │   │   ├── UpdateNoteCommand.cs
│   │   │   └── UpdateNoteHandler.cs
│   │   ├── DeleteNotes/
│   │   │   ├── DeleteNoteCommand.cs
│   │   │   └── DeleteNoteHandler.cs
│   │   └── GetNotes/
│   │       ├── GetNotesQuery.cs
│   │       └── GetNotesHandler.cs
│   │
│   ├── Interfaces/
│   │   ├── INoteRepository.cs
│   │   └── IUnitOfWork.cs
│   │
│   └── Notes.Application.csproj
│
├── 📂 Notes.Domain/                 (Domain Layer)
│   ├── Common/
│   │   └── Entity.cs                → Base class
│   │
│   ├── Entities/
│   │   ├── Note.cs
│   │   └── User.cs
│   │
│   ├── Exceptions/
│   │   ├── NoteNotFoundException.cs
│   │   └── ValidationException.cs
│   │
│   └── Notes.Domain.csproj
│
├── 📂 Notes.Infrastructure/         (Infrastructure Layer)
│   ├── AppDbContext.cs              → EF Core DbContext
│   │
│   ├── Persistence/
│   │   ├── NoteRepository.cs        → Data access
│   │   ├── UnitOfWork.cs            → Transaction manager
│   │   └── Migrations/              → EF Core migrations
│   │
│   └── Notes.Infrastructure.csproj
│
├── 📂 BuildingBlocks/               (Shared Utilities)
│   └── BuildingBlocks.csproj
│
├── 📂 Infrastructure/               (Other Utilities)
│   └── Infrastructure.csproj
│
├── ARCHITECTURE.md                  → This file
└── SaaSApp.sln
```

---

## ✅ Pre-Commit Checklist

Before committing code, verify:

### Code Quality
- [ ] No compilation errors
- [ ] No warnings (treat as errors)
- [ ] All async methods have CancellationToken parameter
- [ ] No unused using statements
- [ ] Consistent naming conventions

### Architecture
- [ ] Domain layer has NO external dependencies
- [ ] Application layer only depends on Domain + Interfaces
- [ ] Infrastructure implements interfaces from Application
- [ ] API only calls handlers from Application
- [ ] No circular dependencies

### Database
- [ ] All DbSets properly configured in AppDbContext
- [ ] Query filters applied (soft delete filters)
- [ ] Default values configured for timestamps
- [ ] Migrations created and applied

### Handlers
- [ ] All handlers use IUnitOfWork (not AppDbContext)
- [ ] All handlers validate input (or entity does)
- [ ] All handlers catch domain exceptions appropriately
- [ ] All public methods are async with CancellationToken
- [ ] No direct repository access (go through UnitOfWork)

### Entities
- [ ] All properties have private setters
- [ ] All setters validate data
- [ ] ToString() overridden if needed
- [ ] Equality comparison implemented if needed

### Exceptions
- [ ] Use custom domain exceptions (not generic Exception)
- [ ] Exception messages are descriptive
- [ ] Exceptions inherit from appropriate base class

### Testing
- [ ] Unit tests exist for handlers
- [ ] Unit tests exist for entities
- [ ] Mock IUnitOfWork in handler tests
- [ ] Test both success and failure paths

### Documentation
- [ ] Public methods have XML documentation
- [ ] Complex logic has comments
- [ ] ARCHITECTURE.md is updated

---

## 🔍 Best Practices Reference

| Practice | Why | Example |
|----------|-----|---------|
| **Immutable Records** | Thread-safe, predictable | `public record CreateNoteCommand(...)` |
| **Async/Await** | Scalable, responsive | `async Task<Guid> Handle(...)` |
| **CancellationToken** | Graceful shutdown | `async Task Handle(..., CancellationToken ct)` |
| **Unit of Work** | Transaction management | `IUnitOfWork _unitOfWork` |
| **Repository Pattern** | Data access abstraction | `INoteRepository` |
| **Soft Delete** | Data recovery, audit trail | `DeletedAt` property |
| **Domain Exceptions** | Semantic errors | `NoteNotFoundException` |
| **DI Pattern** | Loose coupling | Constructor injection |
| **Query Filters** | Exclude soft-deleted | `HasQueryFilter()` |

---

## ❓ FAQ

### Q: Why validate in the entity instead of the handler?
**A:** Entities are responsible for maintaining their invariants (business rules). If validation lives in handlers, the same entity could be created with invalid state elsewhere in the code.

### Q: Why use UnitOfWork if we only have one repository?
**A:** Future-proofing. When you need multiple repositories in a single transaction, UnitOfWork scales seamlessly without handler refactoring. It also centralizes transaction management.

### Q: Can handlers throw domain exceptions?
**A:** Yes. Handlers catch domain exceptions (like `ArgumentException`) and convert them to application exceptions (`ValidationException`) for consistent API responses.

### Q: How does soft delete work?
**A:** The `DeletedAt` property stores the deletion timestamp. EF Core's query filter automatically excludes these records from all queries. To permanently delete, implement a separate method.

### Q: What's the difference between commands and queries?
**A:** Commands modify state (Create, Update, Delete). Queries read state (Get). This separation improves clarity and allows different optimization strategies.

### Q: How do I test handlers?
**A:** Mock `IUnitOfWork` in unit tests. Create fake repositories that implement `INoteRepository`. Verify handler calls correct methods on mocks.

---

## 🚀 Next Steps to Enhance Architecture

1. **Logging** - Add structured logging (Serilog)
2. **Validation** - Add FluentValidation for commands
3. **Authorization** - Add role-based access control
4. **Caching** - Add distributed caching for queries
5. **Events** - Add domain events for cross-aggregate communication
6. **Specifications** - Add query specifications for complex queries
7. **Mediator** - Add MediatR for command/query dispatch
8. **Result Pattern** - Use Result<T> instead of exceptions for flow control
9. **API Documentation** - Add Swagger/OpenAPI documentation
10. **Unit Tests** - Add comprehensive unit test coverage

---

## 📚 Resources

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)

---

## 📝 Summary

This Clean Architecture ensures:
- ✅ **Testability** - Each layer independently testable
- ✅ **Maintainability** - Clear separation of concerns
- ✅ **Scalability** - Easy to add features without breaking existing code
- ✅ **Flexibility** - Easy to swap implementations (e.g., databases)
- ✅ **Domain Focus** - Business logic is the star, not the framework
- ✅ **SOLID Principles** - Applied throughout
- ✅ **Future-Proof** - Easy to extend and maintain

**Last Updated:** 2025
**Version:** 1.0
