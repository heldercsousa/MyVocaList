# Database Indexing Rules

## The Rule

Every field used in a `WHERE` clause, `ORDER BY`, `JOIN`, or search filter **must** have an explicit index declared in the entity's EF Core `IEntityTypeConfiguration<T>` class. No exceptions.

A missing index on a searched field is a silent performance bug — SQLite does a full table scan and gives no warning.

---

## Index Types and When to Use Each

### Standard index — for searched/sorted non-unique fields

```csharp
builder.HasIndex(p => p.FullNameNormalized)
       .HasDatabaseName("IX_Persons_FullNameNormalized");
```

Use when: field is filtered or sorted but values can repeat.

### Unique index — for unique non-nullable fields

```csharp
builder.HasIndex(p => p.Name)
       .IsUnique()
       .HasDatabaseName("IX_Venues_Name");
```

Use when: field must be unique across all rows and is always populated.

### Nullable unique index — for optional unique fields

```csharp
builder.HasIndex(p => p.Email)
       .IsUnique()
       .HasDatabaseName("IX_Persons_Email");
```

Use when: field is optional (nullable) but must be unique when present.
**SQLite behavior:** `NULL` values are never considered equal — multiple rows with `NULL` are allowed even with a `UNIQUE` index. This is the correct behavior for optional unique fields.
**MSSQL behavior:** Same — `NULL` is not equal to `NULL` in unique filtered indexes.

### Filtered (partial) unique index — for conditional uniqueness

```csharp
builder.HasIndex(p => new { p.FullNameNormalized, p.BirthdayDayMonth })
       .IsUnique()
       .HasFilter("[BirthdayDayMonth] IS NOT NULL")
       .HasDatabaseName("IX_Persons_Name_Birthday");
```

Use when: uniqueness only applies when a field is populated. Here: same name + same birthday = duplicate; same name + no birthday = allowed.

### Composite index — for multi-field searches or composite keys

```csharp
builder.HasIndex(p => new { p.EventId, p.PersonId })
       .HasDatabaseName("IX_Participations_EventId_PersonId");
```

Use when: queries regularly filter on two fields together, or a two-field unique constraint is needed.

---

## Naming Convention

```
IX_{TableName}_{FieldName(s)}
```

Examples:
- `IX_Venues_Name`
- `IX_Persons_FullNameNormalized`
- `IX_Persons_Email`
- `IX_Persons_Name_Birthday`
- `IX_Participations_EventId_PersonId`

Always set `HasDatabaseName(...)` explicitly — do not rely on EF Core's generated name.

---

## Collation and Search

Indexes on text fields are only as useful as the queries that use them. For SQLite:

- **Always** use `EF.Functions.Like` + `EF.Functions.Collate` on both operands for text searches (see `search-collation` memory)
- Use the **normalized** column (`FullNameNormalized`) for name searches — not `FullName`
- The service normalizes the search term; the repository receives an already-normalized string

```csharp
// Correct — uses index on FullNameNormalized, collation-aware
.Where(p => EF.Functions.Like(
    EF.Functions.Collate(p.FullNameNormalized, "NOCASE"),
    EF.Functions.Collate(normalizedTerm + "%", "NOCASE")))

// Wrong — case-sensitive, may not use index
.Where(p => p.FullName.StartsWith(term))
```

---

## Required vs Optional Fields in EF Configuration

With nullable reference types **disabled** (project-wide), EF cannot infer nullability from C# types. Always declare explicitly:

```csharp
// Required (NOT NULL in DB)
builder.Property(p => p.FullName).IsRequired();

// Optional (NULL allowed in DB)
builder.Property(p => p.Email).IsRequired(false);
```

Never rely on EF defaults when nullable reference types are disabled — the defaults are unpredictable.

---

## Checklist for New Entities

When adding a new entity or field, verify:

- [ ] Every field used in a `WHERE` or `ORDER BY` has an `HasIndex(...)` entry
- [ ] Unique constraints use `.IsUnique()`
- [ ] Nullable unique fields use `.IsRequired(false)` + `.IsUnique()`
- [ ] Conditional uniqueness uses `.HasFilter(...)`
- [ ] All index names are explicit via `.HasDatabaseName(...)`
- [ ] Search queries use `EF.Functions.Like` + `EF.Functions.Collate` (not `.StartsWith()`, `.Contains()`, or `==`)
- [ ] Required fields have `.IsRequired()`; optional fields have `.IsRequired(false)` — never implicit
