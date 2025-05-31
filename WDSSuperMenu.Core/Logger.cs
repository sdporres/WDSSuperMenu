namespace WDSSuperMenu.Core
{
    public static class Logger
    {
        public static void LogToFile(string message)
        {
#if DEBUG
            try
            {
                File.AppendAllText("debug.log", $"{DateTime.Now}: {message}\n");
            }
            catch
            {
                // Suppress logging errors
            }
#endif
        }
    }
}
