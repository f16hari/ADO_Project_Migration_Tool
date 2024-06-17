namespace ADOMigration;

public static class StringExtension
{
    public static string ReplaceFirst(this string value, string stringToReplace, string replaceWith)
    {
        var index = value.IndexOf(stringToReplace);

        if(index < 0) return value;

        return $"{value[..index]}{value[(index + stringToReplace.Length)..]}";
    }
}
