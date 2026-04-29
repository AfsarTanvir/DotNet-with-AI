# 🚀 PHASE 7 — Caching Strategy (Performance Optimization)

---

## 📌 Objective

Add intelligent caching to the Notes application to reduce database queries and improve response times for frequently accessed data.

**Current State:** No caching implemented. All queries hit the database every time.  
**Goal:** Cache read operations while maintaining cache coherence during mutations.

---

## 🧠 SECTION 1: THEORETICAL FOUNDATION

### 1.1 What is Caching?

**Caching** is the practice of storing frequently accessed data in a faster/closer storage layer to avoid expensive operations.

```
Without Cache:
Request → Database Query → Data → Response (slow, high DB load)

With Cache:
Request → Cache Hit → Response (fast, low DB load)
Request → Cache Miss → Database Query → Cache Store → Response
```

### 1.2 Core Concepts

#### Cache Hit
- Data is found in cache → return immediately
- **Time:** ~1ms (memory) or ~10-50ms (Redis)
- **No database query needed**

#### Cache Miss
- Data not in cache → fetch from database
- **Time:** ~100-500ms (database query)
- **Store result in cache for future hits**

#### Cache Stale
- Data in cache is outdated (cache key still exists but value changed in DB)
- **Problem:** Stale data returned to users
- **Solution:** Cache invalidation (delete key when DB changes)

---

### 1.3 Cache Strategies (Patterns)

#### Strategy 1: Cache-Aside (Look-Aside)
**Most common in .NET**

```
Request
  ↓
Check Cache
  ├─ Hit → Return Data
  └─ Miss
      ↓
      Query Database
      ↓
      Store in Cache
      ↓
      Return Data
```

**Responsibility:** Application layer manages cache (get, set, invalidate)  
**Used in:** Most enterprise applications

**Pseudocode:**
```csharp
public async Task<List<Note>> GetNotesAsync(Guid userId)
{
    string key = $"notes:list:{userId}";
    
    if (cache.TryGetValue(key, out List<Note> cachedNotes))
        return cachedNotes;  // Hit
    
    // Miss: fetch from DB
    var notes = await _unitOfWork.Notes.GetAllAsync(ct);
    notes = notes.Where(n => n.CreatedBy == userId).ToList();
    
    // Store in cache (5 minute TTL)
    cache.Set(key, notes, TimeSpan.FromMinutes(5));
    
    return notes;
}
```

---

#### Strategy 2: Write-Through Cache

```
Write Request
  ↓
Write to Cache
  ↓
Write to Database
  ↓
Response
```

**Advantage:** Cache is always up-to-date  
**Disadvantage:** Writes are slow (dual write latency)  
**Use when:** Consistency is critical (e.g., financial transactions)

---

#### Strategy 3: Write-Behind Cache

```
Write Request
  ↓
Write to Cache Only
  ↓
Response Immediately
  ↓
[Background Job] Write Cache to Database (async)
```

**Advantage:** Fast writes, cache acts as queue  
**Disadvantage:** Data loss risk if cache crashes before DB write  
**Use when:** Acceptable loss (e.g., analytics, user activity logs)

---

#### Strategy 4: Read-Through Cache

```
Request
  ↓
Cache (with built-in loader function)
  ├─ Hit → Return
  └─ Miss → Call loader function → Get from DB → Cache → Return
```

**Advantage:** Transparent, no app-level cache logic  
**Disadvantage:** Less common in .NET, adds latency to first miss  
**Use when:** Distributed cache provider supports (Redis with custom modules)

---

### 1.4 Cache Eviction Policies

When cache memory is full, old entries are removed. Common strategies:

#### 1. Least Recently Used (LRU)
- Remove the entry not accessed for the longest time
- **Best for:** Access patterns that have temporal locality
- **Example:** User's recent notes (accessed today is more important than old)

#### 2. Least Frequently Used (LFU)
- Remove the entry with fewest accesses
- **Best for:** Popularity-based scenarios
- **Example:** Most-viewed notes should stay longer

#### 3. First In, First Out (FIFO)
- Remove oldest entry regardless of usage
- **Simple, predictable, rarely optimal**

#### 4. Random Eviction
- Remove random entry
- **Rarely used, unpredictable**

#### 5. TTL (Time-To-Live) Based
- Remove entry after X time regardless of usage
- **Most common in enterprise apps**
- **Example:** `Set(key, value, TTL: 5 minutes)` → auto-remove after 5 min

#### 6. Sliding Expiration
- Extend TTL on every access
- **Example:** Accessed → TTL resets to 5 minutes
- **Use when:** Active data should stay longer

#### 7. Absolute Expiration
- Remove entry at exact time regardless of access
- **Example:** Cache invalidates at 2025-04-29 15:00:00 UTC
- **Use when:** Data is time-sensitive (e.g., daily report cache)

---

### 1.5 Cache Stampede & Thundering Herd Problem

**Problem:** When cache expires, many concurrent requests miss cache and hit DB simultaneously.

```
Time ──────────────────────────────────────────
      Cache Valid
      ↓
      Requests served from cache (fast)
      ↓
      Cache Expires!
      ↓ (all waiting requests now miss)
      1000 requests hit DB at same time ← STAMPEDE!
      ↓
      Database overloaded
      ↓
      Slow response time for everyone
```

**Solutions:**

1. **Lock-based refresh:** First miss acquires lock, refreshes cache, others wait for lock
2. **Extended TTL on hit:** Sliding expiration (extends TTL each time accessed)
3. **Probabilistic TTL:** Refresh before exact expiration with small probability
4. **Stale-while-revalidate:** Serve stale data while refreshing in background

---

### 1.6 Cache Sizing: How Many Items?

**Rule of Thumb:** Cache 5-20% of your working set

```
Working Set = Data accessed 80% of the time
Example:
├─ Total notes in database: 1 million
├─ Notes accessed 80% of the time: ~50,000 (5% of DB)
└─ Cache size: 2,500 - 10,000 entries (5-20% of working set)
```

**Factors to consider:**

| Factor | Guidance |
|--------|----------|
| **Available memory** | In-process: 100MB-1GB per server. Distributed (Redis): 10GB+ easily |
| **Item size** | Small notes (1-5KB each): can cache more. Large blobs: cache fewer |
| **Access frequency** | Hot data: always cache. Cold data: skip caching |
| **TTL** | Short TTL (1 min): smaller cache needed. Long TTL (1 hour): larger cache needed |
| **User count** | 100 users: cache per-user entries. 1M users: cache global data only |

**For this app:**
- Users: ~1,000 (estimated)
- Notes per user: ~100 (estimated)
- Per-user list cache: 1K × 100 = 100K notes max
- Single note cache: subset of above
- **Memory estimate:** 100K notes × 3KB/note = 300MB (acceptable for in-process)

---

---

## 💾 SECTION 2: .NET CACHING OPTIONS

### 2.1 IMemoryCache (In-Process)

**NuGet:** `Microsoft.Extensions.Caching.Memory` (built-in with .NET)

```csharp
// Startup (Program.cs)
builder.Services.AddMemoryCache();

// Usage
public class GetNotesHandler : IRequestHandler<GetNotesQuery, List<Note>>
{
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    
    public GetNotesHandler(IMemoryCache cache, IUnitOfWork unitOfWork)
    {
        _cache = cache;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<List<Note>> Handle(GetNotesQuery request, CancellationToken ct)
    {
        string key = $"notes:list:{request.UserId}";
        
        if (_cache.TryGetValue(key, out List<Note> cached))
            return cached;
        
        var notes = await _unitOfWork.Notes.GetAllAsync(ct);
        notes = notes.Where(n => n.CreatedBy == request.UserId).ToList();
        
        _cache.Set(key, notes, TimeSpan.FromMinutes(5));
        return notes;
    }
}
```

#### Pros ✅
- **Built-in:** No external dependencies
- **Fast:** Sub-millisecond access (in-process memory)
- **Simple:** Minimal configuration
- **No network latency:** Data in same process
- **Good for:** Single-server deployments

#### Cons ❌
- **Not distributed:** Cache not shared across servers
- **Lost on restart:** Cache cleared when app restarts
- **Memory limit:** Limited to available RAM on single machine
- **No clustering:** Can't share between load-balanced instances
- **Not suitable for:** Multi-server deployments, long-term persistence

#### Eviction Strategy
```csharp
var cacheOptions = new MemoryCacheEntryOptions()
    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))     // Max 5 min
    .SetSlidingExpiration(TimeSpan.FromMinutes(2));      // Reset on access
_cache.Set(key, notes, cacheOptions);
```

---

### 2.2 IDistributedCache (Redis)

**NuGet:** `Microsoft.Extensions.Caching.StackExchangeRedis`

```csharp
// Startup
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";  // Redis connection
});

// Usage
public class GetNotesHandler : IRequestHandler<GetNotesQuery, List<Note>>
{
    private readonly IDistributedCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    
    public GetNotesHandler(IDistributedCache cache, IUnitOfWork unitOfWork)
    {
        _cache = cache;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<List<Note>> Handle(GetNotesQuery request, CancellationToken ct)
    {
        string key = $"notes:list:{request.UserId}";
        
        // Try get from Redis
        var cached = await _cache.GetStringAsync(key, ct);
        if (cached != null)
            return JsonSerializer.Deserialize<List<Note>>(cached);
        
        // Cache miss: query DB
        var notes = await _unitOfWork.Notes.GetAllAsync(ct);
        notes = notes.Where(n => n.CreatedBy == request.UserId).ToList();
        
        // Store in Redis
        var json = JsonSerializer.Serialize(notes);
        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, ct);
        
        return notes;
    }
}
```

#### What is Redis?
- **In-memory data store** with key-value structure
- **Supports:** Strings, Lists, Sets, Sorted Sets, Hashes, Streams
- **Persistent:** Can save to disk (AOF, RDB)
- **Replication:** Master-slave setup available
- **Cluster:** Distributed Redis with sharding
- **Used by:** Netflix, Twitter, GitHub, Stack Overflow

#### Pros ✅
- **Distributed:** Shared across all servers
- **Persistent:** Can save to disk (survives restarts)
- **Survives restarts:** Data persisted to disk
- **Cluster support:** Scale horizontally
- **Rich data types:** Lists, sets, sorted sets, streams
- **Good for:** Multi-server deployments, high availability
- **Can expire keys automatically**
- **Supports Pub/Sub for cache invalidation**

#### Cons ❌
- **External dependency:** Requires Redis server (operational overhead)
- **Network latency:** ~10-50ms per operation vs 1ms for in-memory
- **Serialization overhead:** Must serialize/deserialize to JSON
- **Cost:** Hosting costs (Redis cloud, managed services)
- **Complexity:** Distributed cache issues (clock skew, network partitions)
- **Single point of failure:** If Redis down, cache unavailable (mitigate with cluster)

#### Redis Data Structures for Caching
```
String (most common):
  KEYS: notes:list:{userId}
  VALUE: JSON-serialized list of notes
  
Hash (grouping related data):
  KEYS: notes:
  FIELDS: {userId} → JSON list
  
Set (fast membership checks):
  KEYS: invalidated_keys
  MEMBERS: {keysThatNeedRefresh}
  
Sorted Set (leaderboards, rankings):
  KEYS: user:activity
  SCORE: timestamp
  MEMBER: userId
```

---

### 2.3 HybridCache (.NET 9+)

**NuGet:** `Microsoft.Extensions.Caching.Hybrid` (new in .NET 9)

```csharp
// Startup
builder.Services.AddHybridCache();

// Usage
public class GetNotesHandler : IRequestHandler<GetNotesQuery, List<Note>>
{
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    
    public GetNotesHandler(HybridCache cache, IUnitOfWork unitOfWork)
    {
        _cache = cache;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<List<Note>> Handle(GetNotesQuery request, CancellationToken ct)
    {
        string key = $"notes:list:{request.UserId}";
        
        var notes = await _cache.GetOrCreateAsync(
            key,
            async cancel => 
            {
                var result = await _unitOfWork.Notes.GetAllAsync(cancel);
                return result.Where(n => n.CreatedBy == request.UserId).ToList();
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            },
            cancel: ct
        );
        
        return notes;
    }
}
```

#### Pros ✅
- **Two-tier caching:** L1 (in-process) + L2 (distributed Redis)
- **Fast and reliable:** In-process for speed, Redis for distribution
- **Automatic invalidation:** Built-in coherence between L1 and L2
- **Best of both:** Memory speed + distributed fallback
- **Requires .NET 9+**

#### Cons ❌
- **Requires .NET 9+:** Not available in .NET 10 (yet, or needs new APIs)
- **Newer, less proven:** Limited production usage vs IMemoryCache/Redis
- **Complexity:** Managing two cache layers

---

### 2.4 IOutputCache (HTTP-Level)

**Built-in HTTP caching** for entire responses

```csharp
// Startup
builder.Services.AddOutputCache();

// Endpoint
[HttpGet]
[OutputCache(Duration = 300)]  // Cache entire HTTP response for 5 min
public async Task<IActionResult> GetNotes(CancellationToken ct)
{
    var notes = await _mediator.Send(new GetNotesQuery(), ct);
    return Ok(notes);
}
```

#### Pros ✅
- **Transparent:** Cache entire response
- **HTTP standards compliant:** Works with browsers, proxies, CDNs
- **No serialization:** Response already in HTTP format
- **Simple:** One-line decorator

#### Cons ❌
- **User-agnostic:** Can't cache per-user (security risk in auth apps)
- **All-or-nothing:** Can't cache partial responses
- **Not suitable for:** User-specific or personalized data
- **For this app:** NOT RECOMMENDED (would cache user A's notes in user B's response)

---

---

## 🎯 SECTION 3: WHAT TO CACHE IN THIS APPLICATION

### 3.1 Cacheable Queries

Based on codebase analysis, these queries should be cached:

| Query | Cacheable? | Reason | Suggested TTL |
|-------|-----------|--------|---------------|
| `GetNotesQuery` | ✅ **Yes** | Most frequently accessed, 100% read-only | 5 min |
| `GetNoteQuery(id)` | ✅ **Yes** | Frequently accessed, 100% read-only | 10 min |
| `GetUsersQuery` | ⚠️ **Selective** | Only admin endpoint, less frequent | 30 min |
| `GetUserQuery(id)` | ⚠️ **Selective** | Profile lookup, less frequent | 30 min |

### 3.2 Cache Key Strategy

**Format:** `{entity}:{operation}:{qualifier}`

```
notes:list:{userId}           // All notes for a specific user
notes:single:{noteId}         // Single note by ID
users:list                    // All users (admin)
users:single:{userId}         // Single user by ID
```

**Why user-scoped?** `GetNotesHandler` calls `GetAllAsync()` (all notes for all users) then filters in-memory. Shared cache key would leak users' data to each other.

### 3.3 Cache Invalidation Points

#### Invalidate `notes:list:{userId}`
- When: `CreateNoteHandler` creates a note for user
- When: `UpdateNoteHandler` updates a note by user
- When: `DeleteNoteHandler` soft-deletes a note by user

#### Invalidate `notes:single:{noteId}`
- When: `UpdateNoteHandler` updates the note
- When: `DeleteNoteHandler` soft-deletes the note

#### Invalidate `users:list`
- When: `CreateUserHandler` creates a user
- When: `RegisterHandler` registers new user

#### Invalidate `users:single:{userId}`
- When: User updates their profile (future feature)

### 3.4 Cache Invalidation Implementation

**Option 1: Direct cache injection in handlers (simple)**
```csharp
public class CreateNoteHandler : IRequestHandler<CreateNoteCommand, Note>
{
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public async Task<Note> Handle(CreateNoteCommand request, CancellationToken ct)
    {
        var userId = GetUserIdFromClaims();
        
        var note = new Note(request.Title, request.Content, userId);
        await _unitOfWork.Notes.AddAsync(note, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        
        // Invalidate cache
        _cache.Remove($"notes:list:{userId}");
        
        return note;
    }
}
```

**Option 2: Domain events (clean, decoupled)**
```csharp
// In NoteCreatedEventHandler (already exists)
public class NoteCreatedEventHandler : INotificationHandler<NoteCreatedEvent>
{
    private readonly IMemoryCache _cache;
    
    public async Task Handle(NoteCreatedEvent @event, CancellationToken ct)
    {
        _cache.Remove($"notes:list:{@event.UserId}");
        // Existing logging...
    }
}

// Add similar handlers for UpdateNote and DeleteNote
```

---

---

## 🛠️ SECTION 4: RECOMMENDED IMPLEMENTATION PLAN

### 4.1 Approach: MediatR Pipeline Behavior (Clean Architecture)

**Why this approach?**
1. ✅ Reuses existing `LoggingBehavior` / `ValidationBehavior` pattern
2. ✅ Keeps caching logic OUT of handlers
3. ✅ Centralizes cache management
4. ✅ Easy to enable/disable caching per query with marker interface
5. ✅ Can swap cache implementation (Memory → Redis) with 1 line change

### 4.2 Implementation Steps

#### Step 1: Create ICacheableQuery Marker Interface
```csharp
// Notes.Application/Interfaces/ICacheableQuery.cs
namespace Notes.Application.Interfaces;

public interface ICacheableQuery
{
    string CacheKey { get; }
}
```

#### Step 2: Mark Queries as Cacheable
```csharp
// GetNotesQuery.cs
public record GetNotesQuery(Guid UserId) : IRequest<List<Note>>, ICacheableQuery
{
    public string CacheKey => $"notes:list:{UserId}";
}

// GetNoteQuery.cs
public record GetNoteQuery(Guid NoteId) : IRequest<Note>, ICacheableQuery
{
    public string CacheKey => $"notes:single:{NoteId}";
}
```

#### Step 3: Create CachingBehavior
```csharp
// Notes.Application/Behaviors/CachingBehavior.cs
using System.Reflection;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace Notes.Application.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    
    public CachingBehavior(IDistributedCache cache)
    {
        _cache = cache;
    }
    
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken ct)
    {
        // Check if this request is cacheable
        if (request is not ICacheableQuery cacheableQuery)
            return await next();  // Not cacheable, just execute
        
        string key = cacheableQuery.CacheKey;
        
        // Try get from cache
        var cached = await _cache.GetStringAsync(key, ct);
        if (cached != null)
        {
            try
            {
                return JsonSerializer.Deserialize<TResponse>(cached);
            }
            catch
            {
                _cache.Remove(key);  // Corrupted cache, remove
            }
        }
        
        // Cache miss: execute handler
        var response = await next();
        
        // Store in cache
        var json = JsonSerializer.Serialize(response);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(key, json, options, ct);
        
        return response;
    }
}
```

#### Step 4: Register in Program.cs
```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") 
        ?? "localhost:6379";
});

// Register behavior (after existing behaviors)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
```

#### Step 5: Add Cache Invalidation Event Handlers
```csharp
// In existing NoteCreatedEventHandler
public class NoteCreatedEventHandler : INotificationHandler<NoteCreatedEvent>
{
    private readonly IDistributedCache _cache;
    
    public async Task Handle(NoteCreatedEvent @event, CancellationToken ct)
    {
        // Existing logging...
        
        // Invalidate user's notes list cache
        await _cache.RemoveAsync($"notes:list:{@event.CreatedBy}", ct);
    }
}

// Similar for UpdateNoteEventHandler and DeleteNoteEventHandler
```

### 4.3 Pipeline Order (Important!)

```
Request
  ↓ LoggingBehavior (logs request name)
  ↓ ValidationBehavior (validates command/query)
  ↓ CachingBehavior (checks cache, returns if hit, stores result)
  ↓ Handler (executes only on cache miss)
```

---

---

## 🔄 SECTION 5: ALTERNATIVES

### Alternative 1: Repository-Level Caching Decorator

**Pattern:** Wrap repository in caching decorator

```csharp
// Notes.Application/Repositories/CachedNoteRepository.cs
public class CachedNoteRepository : INoteRepository
{
    private readonly INoteRepository _innerRepository;
    private readonly IDistributedCache _cache;
    
    public CachedNoteRepository(INoteRepository inner, IDistributedCache cache)
    {
        _innerRepository = inner;
        _cache = cache;
    }
    
    public async Task<Note?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        string key = $"notes:single:{id}";
        var cached = await _cache.GetStringAsync(key, ct);
        if (cached != null)
            return JsonSerializer.Deserialize<Note>(cached);
        
        var note = await _innerRepository.GetByIdAsync(id, ct);
        if (note != null)
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(note), 
                new DistributedCacheEntryOptions 
                { 
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                }, ct);
        
        return note;
    }
    
    // Similar for other methods...
}

// Register in Program.cs
builder.Services.AddScoped<INoteRepository>(sp =>
    new CachedNoteRepository(
        new NoteRepository(sp.GetRequiredService<AppDbContext>()),
        sp.GetRequiredService<IDistributedCache>()
    )
);
```

#### Pros ✅
- Repository-level concern (data access owns caching)
- Works with any handler automatically
- Transparent to handlers

#### Cons ❌
- Boilerplate code (decorator for each repository)
- Harder to test (need to mock both inner repo + cache)
- Cache invalidation still scattered across handlers
- Not as clean as pipeline behavior

---

### Alternative 2: Direct Handler Injection

**Pattern:** Inject `IDistributedCache` directly into each query handler

```csharp
public class GetNotesHandler : IRequestHandler<GetNotesQuery, List<Note>>
{
    private readonly IDistributedCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public GetNotesHandler(IDistributedCache cache, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _cache = cache;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<List<Note>> Handle(GetNotesQuery request, CancellationToken ct)
    {
        var userId = ExtractUserId();
        string key = $"notes:list:{userId}";
        
        // Try cache
        var cached = await _cache.GetStringAsync(key, ct);
        if (cached != null)
            return JsonSerializer.Deserialize<List<Note>>(cached);
        
        // Cache miss
        var notes = await _unitOfWork.Notes.GetAllAsync(ct);
        notes = notes.Where(n => n.CreatedBy == userId).ToList();
        
        // Store
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(notes),
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            }, ct);
        
        return notes;
    }
}
```

#### Pros ✅
- Simple and explicit (cache logic visible in handler)
- Full control per handler
- Easy to debug (see cache logic right there)

#### Cons ❌
- **Repetitive code:** Every query handler repeats cache logic
- **Violates SRP:** Handler now does caching + business logic
- **Hard to maintain:** Change cache TTL = edit 10 handlers
- **Not scalable:** Doesn't work well for large number of queries
- **Mixes concerns:** Handler coupled to cache implementation

---

### Alternative 3: HTTP Output Cache

**Pattern:** Cache entire HTTP response

```csharp
[HttpGet]
[OutputCache(Duration = 300)]  // Cache for 5 minutes
public async Task<IActionResult> GetNotes(CancellationToken ct)
{
    var result = await _mediator.Send(new GetNotesQuery(), ct);
    return Ok(result);
}
```

#### Pros ✅
- **Transparent:** No code changes needed
- **HTTP standards compliant**
- **Works with CDNs**

#### Cons ❌
- **User-agnostic:** Cache not scoped per user → data leak risk
- **All-or-nothing:** Can't cache partial data
- **Security risk:** User A gets User B's cached response
- **Not recommended:** For this app with JWT authentication

---

---

## 📊 SECTION 6: PROS & CONS COMPARISON

### 6.1 Cache Implementation Comparison

| Aspect | IMemoryCache | IDistributedCache (Redis) | HybridCache |
|--------|--------------|---------------------------|-------------|
| **Setup Complexity** | ⭐ Trivial | ⭐⭐ Moderate | ⭐⭐ Moderate |
| **Access Speed** | ⭐⭐⭐⭐⭐ ~1ms | ⭐⭐⭐ ~20ms | ⭐⭐⭐⭐⭐ ~1ms (L1) |
| **Distribution** | ❌ No | ✅ Yes | ✅ Yes |
| **Persistence** | ❌ No | ✅ Yes | ✅ Yes (L2) |
| **Multi-server** | ❌ No | ✅ Yes | ✅ Yes |
| **Cost** | $ Free | $$ Redis hosting | $$ Redis hosting |
| **Operational** | ⭐ None | ⭐⭐⭐ Redis ops | ⭐⭐⭐ Redis ops |
| **Production Ready** | ✅ Yes | ✅ Yes | ⚠️ .NET 9+ |
| **Data Loss Risk** | High | Low | Low |
| **Best for** | Single-server | Multi-server | Multi-server + speed |

---

### 6.2 Architecture Strategy Comparison

| Aspect | Pipeline Behavior | Repository Decorator | Direct Injection |
|--------|------------------|----------------------|-----------------|
| **Code Duplication** | ✅ Zero | ❌ High | ❌ Very High |
| **Maintainability** | ✅ Easy | ⚠️ Moderate | ❌ Hard |
| **Testability** | ✅ Easy | ⚠️ Moderate | ❌ Hard |
| **Scalability** | ✅ 100s queries | ⚠️ 10-20 repos | ❌ Not scalable |
| **Separation of Concerns** | ✅ Clean | ⚠️ Mixed | ❌ Mixed |
| **Enable/Disable** | ✅ One line | ⚠️ Per repo | ❌ Per handler |
| **Reuses existing pattern** | ✅ Yes | ❌ No | ❌ No |
| **Complexity** | ⭐⭐ Moderate | ⭐⭐⭐ High | ⭐ Simple |
| **Recommended** | **✅ YES** | ❌ No | ❌ No |

---

### 6.3 Redis vs In-Memory: Decision Matrix

| Scenario | Recommendation |
|----------|-----------------|
| **Single-server development** | Use IMemoryCache (simple, free) |
| **Small team, predictable load** | Use IMemoryCache |
| **Multi-server load balanced** | Use Redis (IDistributedCache) |
| **High availability required** | Use Redis Cluster |
| **Cache must survive restarts** | Use Redis (persistence) |
| **Cost is critical** | Use IMemoryCache |
| **Need pub/sub for invalidation** | Use Redis |
| **Need rich data structures** | Use Redis |
| **Enterprise production** | Use Redis + cluster |
| **This app (Notes)** | **Start with IMemoryCache, migrate to Redis when scaling** |

---

---

## 🚀 SECTION 7: IMPLEMENTATION ROADMAP

### Phase 7a: Foundation (Start Here)
1. ✅ Create `ICacheableQuery` marker interface
2. ✅ Create `CachingBehavior<TRequest, TResponse>` using IMemoryCache
3. ✅ Mark `GetNotesQuery` and `GetNoteQuery` as cacheable
4. ✅ Register behavior in Program.cs with IMemoryCache
5. ✅ Test: Verify cache hits on repeated requests

### Phase 7b: Invalidation
1. ✅ Add cache removal to `NoteCreatedEventHandler`
2. ✅ Create `NoteUpdatedEvent` and event handler
3. ✅ Create `NoteDeletedEvent` and event handler
4. ✅ Test: Verify cache invalidation on mutations

### Phase 7c: Redis Migration (Optional)
1. ⏳ Add Redis to docker-compose or infrastructure
2. ⏳ Change IMemoryCache → IDistributedCache in Program.cs
3. ⏳ Add Redis connection string to appsettings.json
4. ⏳ Update DistributedCacheEntryOptions for extended TTL
5. ⏳ Test: Multi-server cache sharing

### Phase 7d: Monitoring & Tuning
1. ⏳ Add cache hit/miss metrics (Serilog structured logging)
2. ⏳ Monitor cache memory usage
3. ⏳ Adjust TTLs based on data access patterns
4. ⏳ Load test and benchmark

---

---

## ❓ COMMON QUESTIONS

### Q: How many items should I cache?
**A:** Start conservative: cache 5-20% of your "hot" data. For this app: ~2,500-10,000 notes (5-20% of working set). Monitor memory and cache hit rates.

### Q: What if cache gets out of sync with database?
**A:** That's why we have TTL. Cache entry expires after 5 minutes, forcing a refresh. For critical data, use shorter TTL (1-2 min). Use cache invalidation events to force refresh immediately on mutations.

### Q: IMemoryCache vs Redis: which should I use?
**A:** 
- **Single server:** IMemoryCache
- **Multiple servers / high availability:** Redis
- **This app now:** IMemoryCache (simplicity), migrate to Redis later when scaling

### Q: Will caching break user authorization?
**A:** **Potentially, if you're not careful!** Cache keys MUST be user-scoped. Wrong: `notes:list` (shared cache, users see each other's data). Right: `notes:list:{userId}` (per-user).

### Q: What about stale reads?
**A:** That's acceptable for most UIs. Users expect eventual consistency, not real-time consistency. If user creates a note, they see it immediately (new note). Other users see it within cache TTL (5-10 min) — acceptable.

### Q: Should I cache delete operations?
**A:** No, it's a write operation. The `DeleteNoteHandler` invalidates the cache and writes to DB. Queries hit the fresh DB on next access.

### Q: Can I use cache for writes?
**A:** Not recommended. The `CreateNoteCommand` and `UpdateNoteCommand` should write to DB first (source of truth), then invalidate cache. Never cache writes; caching is for reads only.

---

---

## 📚 NEXT STEPS

1. **Now:** Review this document, understand the concepts
2. **Phase 7a:** Implement IMemoryCache + CachingBehavior + ICacheableQuery (recommended start)
3. **Phase 7b:** Implement cache invalidation via domain events
4. **Phase 7c:** (Later) Migrate to Redis when scaling to multiple servers
5. **Phase 7d:** (Later) Add monitoring, metrics, fine-tune TTLs

---

---

## 🔗 REFERENCES

### Caching Concepts
- [Martin Fowler: Cache-Aside Pattern](https://martinfowler.com/bliki/CacheAsidePattern.html)
- [Caching Strategies](https://docs.microsoft.com/en-us/azure/architecture/patterns/cache-aside)
- [Cache Stampede (Thundering Herd)](https://en.wikipedia.org/wiki/Cache_stampede)

### .NET Caching
- [Microsoft.Extensions.Caching.Memory](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory)
- [Microsoft.Extensions.Caching.StackExchangeRedis](https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Extensions.Caching.StackExchangeRedis)
- [HybridCache (.NET 9+)](https://github.com/dotnet/runtime/issues/29788)

### Redis
- [Redis Documentation](https://redis.io/documentation)
- [Redis vs Memcached](https://aws.amazon.com/elasticache/redis-vs-memcached/)
- [Redis Cluster Tutorial](https://redis.io/topics/cluster-tutorial)

### Performance
- [Web Performance Working Group](https://www.w3.org/webperf/)
- [Chrome DevTools Performance](https://developer.chrome.com/docs/devtools/performance/)

---

**Document Version:** 1.0  
**Last Updated:** 2025-04-29  
**Status:** ✅ Ready for Implementation (Phase 7a)  
**Next Review:** After Phase 7b completion
