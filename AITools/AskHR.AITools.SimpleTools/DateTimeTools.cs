using System.ComponentModel;

namespace AskHR.AITools.SimpleTools;

public static class DateTimeTools
{
    [Description("Get current datetime")]
    public static DateTime GetCurrentDateTime()
    {
        return DateTime.Now;
    }
}
