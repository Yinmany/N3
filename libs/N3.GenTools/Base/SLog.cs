namespace N3.GenTools;

public static class SLog
{
    public static void Info(object msg)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(msg);
    }

    public static void Error(object msg)
    {
        ConsoleColor old = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
        }
        finally
        {
            Console.ForegroundColor = old;
        }
    }
}