namespace DbMetaTool.Services;

/// <summary>
/// Represents the outcome of a database update operation, including aggregated statistics and errors.
/// </summary>
public sealed class UpdateReport
{
    public UpdateReport()
    {
    }

    /// <summary>
    /// Total number of script files discovered for execution.
    /// </summary>
    public int FilesTotal { get; set; }

    /// <summary>
    /// Number of script files executed successfully.
    /// </summary>
    public int FilesSucceeded { get; set; }

    /// <summary>
    /// Number of script files that failed and were rolled back.
    /// </summary>
    public int FilesFailed { get; set; }

    /// <summary>
    /// Total number of SQL statements executed.
    /// </summary>
    public int StatementsExecuted { get; set; }

    public List<BuildError> Errors { get; } = new();
}
