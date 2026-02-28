namespace UniversalTasker.UI.ViewModels;

public class LogEntryViewModel
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = "";
    public string TimestampText => Timestamp.ToString("HH:mm:ss.fff");
}
