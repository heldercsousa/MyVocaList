# CLAUDE.md Updates

## 1. Update App Section (Line 4)

**Change from:**
```
Karaoke queue management with round-based progression. .NET MAUI 8.0 (net8.0-android).
```

**To:**
```
Karaoke queue management with round-based progression. .NET MAUI 9.0 (net9.0-android).
```

---

## 2. Update Stack Section (Lines 77-81)

**Change from:**
```
MediatR, FluentValidation, Serilog, EF Core 9, SQLite
UraniumUI (Material Design 3)
```

**To:**
```
MediatR, FluentValidation, Serilog, EF Core 9, SQLite
UraniumUI 2.14, HorusSoftware.Maui.MaterialDesignControls 10.0
```

---

## 3. New Section: UI Thread Safety (Insert after Error Handling, before Git Commits)

```markdown
## UI Thread Safety

**MANDATORY**: All UI operations must execute on the native UI thread.

### Rules
1. **NEVER** block UI thread - No `Task.Wait()`, `.Result`, or synchronous I/O
2. **ALWAYS** use `Application.Current.Dispatcher` for cross-thread UI updates
3. **NEVER** modify `ObservableCollection` from background threads
4. **ALWAYS** use `async Task` - Never `async void` (except event handlers)

### Required Pattern
- Use `Application.Current.Dispatcher.Dispatch()` for UI updates from background
- Use `Application.Current.Dispatcher.DispatchAsync()` for async UI work
- Heavy computation on `Task.Run()`, marshal results to UI via Dispatcher

### Why NOT MainThread
- `MainThread.BeginInvokeOnMainThread` has known Windows issues
- `Dispatcher` works consistently on Android, iOS, Windows

### Code Patterns
See `DesignSystem_Implementation_Guide.md` for:
- `ThreadSafeViewModelBase` implementation
- `ThreadSafeDialogService` wrapper
- Collection update patterns
```

---

## 4. Summary of All CLAUDE.md Changes

| Location | Change |
|----------|--------|
| Line 4 | MAUI 8.0 → MAUI 9.0 |
| Lines 77-81 | Add HorusSoftware MDC to stack |
| After line 51 | Insert UI Thread Safety section |

**Total addition**: ~20 lines (rules only, no code samples)
