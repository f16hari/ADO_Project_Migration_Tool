using System.ComponentModel;

namespace ADOMigration.Extensions;

public static class EnumExtensions
{
    public static string Description(this Enum value)
    {
        var fi = value.GetType().GetField(value.ToString()) ?? throw new InvalidEnumArgumentException("Not a valid enum type");

        if (fi.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes && attributes.Length != 0)
        {
            return attributes.First().Description;
        }

        return value.ToString();
    }
}