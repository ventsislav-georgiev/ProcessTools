namespace ProcessInfo
{
    internal static class Utilities
    {
        public static bool IsUserAvailableProcess(string processName)
        {
            switch (processName)
            {
                case "Idle":
                case "System":
                    {
                        return false;
                    }
                default:
                    {
                        return true;
                    }
            }
        }
    }
}