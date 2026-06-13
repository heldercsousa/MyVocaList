namespace MyVocaList.Domain.Resolution;

/// <summary>Describes the outcome kind produced by a resolution lookup.</summary>
public enum ResolutionKind
{
    /// <summary>No match found — safe to create a new record.</summary>
    NoMatch,

    /// <summary>An exact match was found via external provider + id.</summary>
    ExactExternalMatch,

    /// <summary>An exact match was found in the local DB by title/version/artist under collation.</summary>
    ExactLocalMatch,

    /// <summary>No exact match, but one or more fuzzy candidates scored above threshold.</summary>
    FuzzyCandidates
}

/// <summary>The action the user (or system) has chosen after inspecting a resolution result.</summary>
public enum ResolutionChoice
{
    /// <summary>Create a brand-new record with a canonical or new title.</summary>
    CreateNew,

    /// <summary>Create a new record explicitly marked as a variant version (non-empty Version required).</summary>
    CreateNewVersion,

    /// <summary>Merge API fields into the existing matched record.</summary>
    UpdateExisting,

    /// <summary>Attach the external provider/id to the matched record without changing other fields.</summary>
    AttachExternalId
}
