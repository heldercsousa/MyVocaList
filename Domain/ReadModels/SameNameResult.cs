using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.ReadModels;

/// <summary>
/// BUG-088 (REQ-DUP-09): the same-name sheet data. <paramref name="Matches"/> is capped at the list limit,
/// oldest first (lowest Id). <paramref name="TotalCount"/> is the full number of same-name singers.
/// </summary>
/// <param name="Matches">The listed same-name singers, at most the list limit.</param>
/// <param name="TotalCount">The number of same-name singers, listed or not.</param>
public record SameNameResult(IReadOnlyList<Person> Matches, int TotalCount);
