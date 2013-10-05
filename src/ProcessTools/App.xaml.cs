using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using ProcessTools.Core;

namespace ProcessTools
{
    /// <summary>
    ///   Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(int dwProcessId);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private Mutex _applicationMutex;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"ProcessTools.Executables.handle.exe"))
            {
                if (stream != null)
                {
                    var assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    using (var fileStream = File.Create("handle.exe"))
                    {
                        fileStream.Write(assemblyData, 0, assemblyData.Length);
                    }
                }
            }

            //Dynamic Assembly Loading
            AppDomain.CurrentDomain.AssemblyResolve += (s, args) =>
            {
                String resourceName = string.Concat("ProcessTools.EmbeddedLibraries.", new AssemblyName(args.Name).Name, ".dll");

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        return null;
                    var assemblyData = new Byte[stream.Length];

                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };

            try
            {
                bool createdNew = false;
                _applicationMutex = new Mutex(true, "ProcessTools", out createdNew);
                if (!createdNew)
                {
                    Process process = Process.GetCurrentProcess();
                    var applicationProcesses = Process.GetProcessesByName(process.ProcessName);

                    var firstInstance = applicationProcesses.SingleOrDefault(item => item.Id != process.Id);
                    if (firstInstance != null)
                    {
                        AllowSetForegroundWindow(firstInstance.Id);
                        SetForegroundWindow(firstInstance.MainWindowHandle);
                    }
                    App.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Utilities.Message(ex.Message);
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (File.Exists("handle.exe"))
                File.Delete("handle.exe");
        }
    }
}