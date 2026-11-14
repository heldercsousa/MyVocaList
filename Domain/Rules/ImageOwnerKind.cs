namespace MyVocaList.Domain.Rules;

/// <summary>Which kind of record owns an uploaded picture; decides its images sub-folder.</summary>
public enum ImageOwnerKind
{
    Song,
    Artist,
}
