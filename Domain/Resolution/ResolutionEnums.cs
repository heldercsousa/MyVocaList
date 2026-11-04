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
