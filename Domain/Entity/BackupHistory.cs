namespace MyVocaList.Domain.Entity;

public class BackupHistory
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public BackupTrigger TriggerType { get; set; }
    public BackupType BackupType { get; set; }
    public string FilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public MirrorStatus MirrorStatus { get; set; }
}
