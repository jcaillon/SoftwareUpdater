using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

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

            if (args == null || args.Length < 4) {
                MessageBox.Show("Wrong number of arguments passed to this program.\n\nExcepting:\n\n{--name PROCESSNAME | --pid PROCESSID} --action-file ACTIONFILE", "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }

            string processName = null;
            int? processId = null;
            string actionFilePath = null;

            for (int i = 0; i < args.Length - 1; i++) {
                if (args[i] == "--name") {
                    processName = args[i + 1];
                } else if (args[i] == "--pid") {
                    processId = int.Parse(args[i + 1]);
                } else if (args[i] == "--action-file") {
                    actionFilePath = args[i + 1];
                }
            }

            if (string.IsNullOrEmpty(processName) && !processId.HasValue || string.IsNullOrEmpty(actionFilePath)) {
                MessageBox.Show("Wrong number of arguments passed to this program.\n\nExcepting:\n\n{--name PROCESSNAME | --pid PROCESSID} --action-file ACTIONFILE", "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }

            // wait for the process to stop
            if (!string.IsNullOrEmpty(processName)) {
                while (IsProcessOpen(processName)) {
                    Thread.Sleep(200);
                }
            }
            if (processId.HasValue) {
                while (IsProcessOpen(processId.Value)) {
                    Thread.Sleep(200);
                }
            }

            if (string.IsNullOrEmpty(actionFilePath) || !File.Exists(actionFilePath)) {
                MessageBox.Show("The action file specified with argument --action-file ACTIONFILE must exist.", "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
            
            
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
                        MessageBox.Show("The update failed:\n" + e.Message, "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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