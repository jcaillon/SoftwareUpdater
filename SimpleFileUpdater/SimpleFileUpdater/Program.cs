using System;
using System.Diagnostics;
using System.IO;
using System.Text;

#if WINDOWSONLYBUILD
using System.Windows.Forms;
using System.Threading;
#else
using System.Threading.Tasks;
#endif

namespace SimpleFileUpdater {
    /// <summary>
    /// Main program.
    /// </summary>
    public static class Program {
        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="args"></param>
        [STAThread]
        public static int Main(string[] args) {
            if (args == null) {
#if WINDOWSONLYBUILD
                MessageBox.Show("This program requires arguments", "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
#else
                Console.WriteLine("This program requires arguments");
                Console.ReadKey();
#endif
                return 1;
            }

            string processName = null;
            int? processId = null;
            string actionFilePath = null;
            int? waitForCloseInMs = null;

            for (int i = 0; i < args.Length - 1; i++) {
                if (args[i] == "--name") {
                    processName = args[i + 1];
                } else if (args[i] == "--pid") {
                    processId = int.Parse(args[i + 1]);
                } else if (args[i] == "--action-file") {
                    actionFilePath = args[i + 1];
                } else if (args[i] == "--wait") {
                    waitForCloseInMs = int.Parse(args[i + 1]);
                }
            }

            if (string.IsNullOrEmpty(processName) && !processId.HasValue || string.IsNullOrEmpty(actionFilePath)) {
#if WINDOWSONLYBUILD
                MessageBox.Show("Wrong number of arguments passed to this program.\n\nExcepting:\n\n{--name PROCESSNAME | --pid PROCESSID} --action-file ACTIONFILE [--wait TIME_IN_MS]", "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
#else
                Console.WriteLine("Wrong number of arguments passed to this program.\n\nExcepting:\n\n{--name PROCESSNAME | --pid PROCESSID} --action-file ACTIONFILE [--wait TIME_IN_MS]");
                Console.ReadKey();
#endif
                return 1;
            }

            // wait for the process to stop
            if (!string.IsNullOrEmpty(processName)) {
                while (IsProcessOpen(processName)) {
#if WINDOWSONLYBUILD
                    Thread.Sleep(200);
#else
                    Task.Delay(200).Wait();
#endif
                }
            }

            if (processId.HasValue) {
                while (IsProcessOpen(processId.Value)) {
#if WINDOWSONLYBUILD
                    Thread.Sleep(200);
#else
                    Task.Delay(200).Wait();
#endif
                }
            }

            if (string.IsNullOrEmpty(actionFilePath) || !File.Exists(actionFilePath)) {
#if WINDOWSONLYBUILD 
                MessageBox.Show("The action file specified with argument --action-file ACTIONFILE must exist.", "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
#else
                Console.WriteLine("The action file specified with argument --action-file ACTIONFILE must exist.");
                Console.ReadKey();
#endif
                return 1;
            }

#if WINDOWSONLYBUILD
            Thread.Sleep(waitForCloseInMs ?? 500);
#else
            Task.Delay(waitForCloseInMs ?? 500).Wait();
#endif
            
            using (StringReader reader = new StringReader(File.ReadAllText(actionFilePath, Encoding.Default))) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    var splitLine = line.Split('\t');
                    try {
                        switch (splitLine.Length) {
                            case 2:
                                switch (splitLine[0]) {
                                    case "start":
                                        Process.Start(splitLine[1]);
                                        break;
                                }

                                break;
                            case 3:
                                if (File.Exists(splitLine[2])) {
                                    File.Delete(splitLine[2]);
                                }

                                if (!Directory.Exists(Path.GetDirectoryName(splitLine[2]))) {
                                    Directory.CreateDirectory(Path.GetDirectoryName(splitLine[2]) ?? "");
                                }

                                switch (splitLine[0]) {
                                    case "copy":
                                        File.Copy(splitLine[1], splitLine[2]);
                                        break;
                                    case "move":
                                        File.Move(splitLine[1], splitLine[2]);
                                        break;
                                    case "start":
                                        Process.Start(splitLine[1], splitLine[2]);
                                        break;
                                }

                                break;
                        }
                    } catch (Exception e) {
#if WINDOWSONLYBUILD
                        MessageBox.Show("The update failed:\n" + e.Message, "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
#else
                        Console.WriteLine("The update failed:\n" + e.Message);
                        Console.ReadKey();
#endif
                        return 1;
                    }
                }
            }

            File.Delete(actionFilePath);

            return 0;
        }

        private static bool IsProcessOpen(string name) {
            foreach (var process in Process.GetProcesses()) {
                if (process.ProcessName.Contains(name)) {
                    return true;
                }
            }

            return false;
        }

        private static bool IsProcessOpen(int pid) {
            foreach (var process in Process.GetProcesses()) {
                if (process.Id.Equals(pid)) {
                    return true;
                }
            }

            return false;
        }
    }
}