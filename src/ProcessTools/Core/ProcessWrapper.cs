#region

using System;
using System.Diagnostics;
using System.IO;
using ProcessTools.Core.Extensions;

#endregion

namespace ProcessTools.Core
{
    public class ProcessWrapper
    {
        private const bool _createNoWindow = true;
        private const bool _useShellExecute = false;
        private const ProcessWindowStyle _windowStyle = ProcessWindowStyle.Hidden;

        private static readonly object _processLock = new Object();
        public bool RedirectStandardError = true;
        public bool RedirectStandardOutput = false;

        public ProcessWrapper(string fileName,
                              string arguments,
                              string workingDirectory = null,
                              bool redirectStandardError = true,
                              bool redirectStandardOutput = false)
        {
            if (workingDirectory.HasValue())
                WorkingDirectory = workingDirectory;
            else
                WorkingDirectory = Directory.GetCurrentDirectory();
            FileName = fileName;
            Arguments = arguments;
            RedirectStandardError = redirectStandardError;
            RedirectStandardOutput = redirectStandardOutput;
        }

        public string WorkingDirectory { get; set; }

        public string FileName { get; set; }

        public string Arguments { get; set; }

        public string Execute()
        {
            lock (_processLock)
            {
                string result = null;
                var process = new Process
                    {
                        StartInfo =
                            {
                                WorkingDirectory = WorkingDirectory,
                                FileName = FileName,
                                Arguments = Arguments,
                                CreateNoWindow = _createNoWindow,
                                UseShellExecute = _useShellExecute,
                                RedirectStandardError = RedirectStandardError,
                                RedirectStandardOutput = RedirectStandardOutput,
                                WindowStyle = _windowStyle
                            }
                    };

                process.Start();
                process.WaitForExit();
                if (RedirectStandardOutput)
                    result = process.StandardOutput.ReadToEnd();
                else if (RedirectStandardError)
                    result = process.StandardError.ReadToEnd();

                process.Close();
                return result;
            }
        }
    }
}