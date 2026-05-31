namespace MyVocaList.Tests.Unit.Services;

public class TransactionLogWriterTests
{
    private readonly string _logDir;

    public TransactionLogWriterTests()
    {
        _logDir = Path.Combine(Path.GetTempPath(), $"mvl_log_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_logDir);
    }

    [Fact]
    // [AC] AppendAsync writes a valid JSON line to the session log file
    public async Task AppendAsync_SingleEntry_WritesJsonLineToFile()
    {
        var writer = new TransactionLogWriter(_logDir);
        var entry = new LogEntry(DateTime.UtcNow, "Create", "Singer", "1", null, "{\"Name\":\"Maria\"}");

        await writer.AppendAsync(entry, CancellationToken.None);

        var lines = await File.ReadAllLinesAsync(writer.CurrentSessionLogPath);
        Assert.Single(lines);
        Assert.Contains("\"op\":\"Create\"", lines[0]);
        Assert.Contains("\"entity\":\"Singer\"", lines[0]);
    }

    [Fact]
    // [AC] PruneLogsOlderThanAsync deletes log files whose last entry is before snapshot timestamp
    public async Task PruneLogsOlderThanAsync_OldFile_Deleted()
    {
        var writer = new TransactionLogWriter(_logDir);
        var oldEntry = new LogEntry(DateTime.UtcNow.AddHours(-2), "Create", "Venue", "1", null, "{}");
        await writer.AppendAsync(oldEntry, CancellationToken.None);
        var logPath = writer.CurrentSessionLogPath;

        // new session = new file name (different timestamp)
        await Task.Delay(1100); // ensure different second for file name
        var freshWriter = new TransactionLogWriter(_logDir);
        await freshWriter.PruneLogsOlderThanAsync(DateTime.UtcNow.AddHours(-1), CancellationToken.None);

        Assert.False(File.Exists(logPath));
    }
}

public class BackupServiceTests
{
    private readonly Mock<IBackupRepository> _repoMock = new();
    private readonly Mock<ITransactionLogWriter> _logWriterMock = new();
    private readonly Mock<ILogger<BackupService>> _loggerMock = new();
    private readonly string _backupDir;
    private readonly string _dbPath;

    public BackupServiceTests()
    {
        _backupDir = Path.Combine(Path.GetTempPath(), $"mvl_bkp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_backupDir);
        _dbPath = Path.Combine(_backupDir, "MyVocaList.db");
        File.WriteAllText(_dbPath, "fake-db-content");
    }

    private BackupService CreateSut() =>
        new(_repoMock.Object, _logWriterMock.Object, _loggerMock.Object, _dbPath, _backupDir);

    [Fact]
    // [AC] CreateFullBackupAsync creates a snapshot file and records history
    public async Task CreateFullBackupAsync_ValidDb_CreatesFileAndRecordsHistory()
    {
        _repoMock.Setup(r => r.AddAsync(It.IsAny<BackupHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<BackupHistory>());
        _logWriterMock.Setup(l => l.PruneLogsOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var result = await sut.CreateFullBackupAsync(BackupTrigger.Manual, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.FilePath);
        _repoMock.Verify(r => r.AddAsync(It.Is<BackupHistory>(h =>
            h.TriggerType == BackupTrigger.Manual &&
            h.BackupType == BackupType.FullSnapshot), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] GetHistoryAsync delegates to repository with limit
    public async Task GetHistoryAsync_ReturnsRepositoryResult()
    {
        var expected = new List<BackupHistory> { new() { Id = 1, TriggerType = BackupTrigger.AppStop } };
        _repoMock.Setup(r => r.GetRecentAsync(5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);
        var sut = CreateSut();

        var result = await sut.GetHistoryAsync(5, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(BackupTrigger.AppStop, result[0].TriggerType);
    }

    [Fact]
    // [AC] HasRecentBackupAsync returns true when latest snapshot is within 24 hours
    public async Task HasRecentBackupAsync_RecentSnapshot_ReturnsTrue()
    {
        _repoMock.Setup(r => r.GetLatestSnapshotAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new BackupHistory { CreatedAt = DateTime.UtcNow.AddHours(-2) });
        var sut = CreateSut();

        var result = await sut.HasRecentBackupAsync(CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    // [AC] HasRecentBackupAsync returns false when no snapshot exists
    public async Task HasRecentBackupAsync_NoSnapshot_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetLatestSnapshotAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync((BackupHistory?)null);
        var sut = CreateSut();

        var result = await sut.HasRecentBackupAsync(CancellationToken.None);

        Assert.False(result);
    }
}
