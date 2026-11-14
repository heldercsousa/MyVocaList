namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>Housekeeping of the images folder (REQ-PIC-11).</summary>
public interface IImageMaintenanceService
{
    /// <summary>Deletes image files no song or artist references and that are older than 24 hours; returns the count.</summary>
    Task<int> SweepOrphansAsync(CancellationToken ct = default);
}
