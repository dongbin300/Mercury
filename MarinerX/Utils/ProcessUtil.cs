using System.Diagnostics;

namespace MarinerX.Utils
{
    public class ProcessUtil
    {
        public static Process? Start(string path)
        {
            ProcessStartInfo info = new()
            {
                FileName = path,
                UseShellExecute = true
            };

            return Process.Start(info);
        }

        public static Process? Start(string path, string argument)
        {
            ProcessStartInfo info = new()
            {
                FileName = path,
                UseShellExecute = true,
                Arguments = argument
            };

            return Process.Start(info);
        }
    }
}
