# MediatR Patterns — Reference (not active)

> On-demand reference. **MediatR is NOT registered in `MauiProgram.cs`** — the current architecture uses direct service interfaces (`IVenueService`, etc.) injected via constructor DI. These are reference patterns for *when* MediatR is introduced. Loaded via the `myvocalist-coding` skill map or a pointer from `CLAUDE.md § Rules Files` — it does not load unconditionally.
> Mark patterns as "confirmed" once the first handler is registered.

## Command Pattern

```csharp
// Command (mutates state, no return value)
public record AddSongToQueueCommand(Guid SongId, Guid VenueId) : IRequest;

// Handler
public sealed class AddSongToQueueCommandHandler : IRequestHandler<AddSongToQueueCommand>
{
    public async Task Handle(AddSongToQueueCommand request, CancellationToken ct)
    {
        // business logic here
    }
}
```

## Query Pattern

```csharp
// Query (reads state, returns DTO)
public record GetQueueQuery(Guid VenueId) : IRequest<IReadOnlyList<QueueEntryDto>>;

// Handler
public sealed class GetQueueQueryHandler : IRequestHandler<GetQueueQuery, IReadOnlyList<QueueEntryDto>>
{
    public async Task<IReadOnlyList<QueueEntryDto>> Handle(GetQueueQuery request, CancellationToken ct)
    {
        // read-only query here
    }
}
```

## Domain Events (Notifications)

```csharp
// Event
public record SongAddedToQueueEvent(Guid SongId, Guid VenueId) : INotification;

// Handler
public sealed class SongAddedToQueueEventHandler : INotificationHandler<SongAddedToQueueEvent>
{
    public async Task Handle(SongAddedToQueueEvent notification, CancellationToken ct)
    {
        // side-effects here (e.g. push notification, audit log)
    }
}
```

## Pipeline Behaviors

<!-- TODO: document registered behaviors (validation, logging, etc.) -->

### Validation Behavior (FluentValidation)

```csharp
// Runs AbstractValidator<TRequest> before every command/query handler
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // ...
}
```

## Registration (MauiProgram.cs / DI)

```csharp
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<AddSongToQueueCommand>());

// Pipeline behaviors registered in order:
// builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

## Naming Conventions

| Type | Suffix | Example |
|------|--------|---------|
| Command | `Command` | `AddSongToQueueCommand` |
| Command handler | `CommandHandler` | `AddSongToQueueCommandHandler` |
| Query | `Query` | `GetQueueQuery` |
| Query handler | `QueryHandler` | `GetQueueQueryHandler` |
| Domain event | `Event` | `SongAddedToQueueEvent` |
| Event handler | `EventHandler` | `SongAddedToQueueEventHandler` |

## Current Architecture (Pre-MediatR)

Services currently use direct interface injection. Pattern confirmed in VenueService:

```csharp
// Service interface (in Services project)
public interface IVenueService
{
    (bool isValid, string message) ValidateNameInput(string name);
    Task<(bool success, string message, Venue? venue)> CreateVenueAsync(string name);
    Task<(bool success, string message)> UpdateVenueAsync(int id, string name);
    Task<(bool success, string message)> DeleteVenuesAsync(IEnumerable<int> ids);
    Task<(IEnumerable<VenueListItemDto> items, int totalCount)> GetPagedVenuesForListAsync(
        int pageNumber, int pageSize, string? query = null);
}

// ViewModel injects service directly
public VenuesViewModel(IVenueService venueService, ISnackbarService snackbarService, ...)
```

When MediatR is introduced, the ViewModel will `Send()` commands/queries instead of calling the service directly.

## Known Gotchas

<!-- TODO: populate as issues are discovered -->

---

> **Authorship note:** Content moved verbatim from `.claude/rules/mediatr-patterns.md` (Rules File Refactoring, Task 02, 2026-07-04). The rules file was deleted because it documents code that does not exist yet — 1.1k tokens of pure unconditional overhead. Container change only, zero content change. Human-review before relied upon (CLAUDE.md § Continuous Enhancement — Authorship).
