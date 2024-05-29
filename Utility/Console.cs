namespace ADOMigration;

public static class Console
{
    public static void WriteLine(string message, bool clearPreviousLine = false)
    {
        if (clearPreviousLine) do { System.Console.Write("\b \b"); } while (System.Console.CursorLeft > 0);
        
        System.Console.WriteLine(message);
    }

    public static void ClearLine()
    {
        do { System.Console.Write("\b \b"); } while (System.Console.CursorLeft > 0);
    }
}
