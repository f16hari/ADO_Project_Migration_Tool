namespace ADOMigration;

public class FileLogger(string filePath) : ILogger
{
    public string FilePath { get; } = filePath;

    public void Log(string message)
    {
        try
        {
            using StreamWriter writer = File.AppendText(FilePath);
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to log file: {ex.Message}");
        }
    }
}
