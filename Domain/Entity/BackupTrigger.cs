namespace MyVocaList.Domain.Entity;

public enum BackupTrigger
{
    AppStop,
    QueueCreated,
    RoundCompleted,
    QueueClosed,
    Manual
}
