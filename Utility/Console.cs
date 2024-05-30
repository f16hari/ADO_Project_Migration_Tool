namespace ADOMigration;

public static class Console
{
    public static void WriteLine(string message, bool clearPreviousLine = false)
    {
        if(clearPreviousLine) System.Console.Write("\b \b\b\b\b\b \b\b\b\b \b\b \b\b\b\b");
        
        System.Console.WriteLine(message);
    }

    public static void ClearLine()
    {
        System.Console.Write("\b \b\b\b\b\b \b\b\b\b \b\b \b\b\b\b");
    }
}
