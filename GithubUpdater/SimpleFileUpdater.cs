#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (SimpleFileUpdater.cs) is part of GithubUpdater.
// 
// GithubUpdater is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GithubUpdater is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GithubUpdater. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("GithubUpdater.Test")]

namespace GithubUpdater {

    /// <summary>
    /// A simple file updater that allows to move files or start a process after the current program has been executed.
    /// </summary>
    public class SimpleFileUpdater {

        private static SimpleFileUpdater _instance;

        /// <summary>
        /// Get the singleton instance of the updater.
        /// </summary>
        public static SimpleFileUpdater Instance => _instance ?? (_instance = new SimpleFileUpdater());

        private readonly string _exeFilePath = Path.Combine(Path.GetTempPath(), "SimpleFileUpdater.exe");
        private bool _requireAdminExe;
        private StringBuilder _output;
        private Process _process;

        /// <summary>
        /// new instance.
        /// </summary>
        private SimpleFileUpdater() { }

        /// <summary>
        /// Will the updater need admin rights to do its job?
        /// </summary>
        public bool IsAdminRightsNeeded => _requireAdminExe;

        /// <summary>
        /// Try to clean up the exe used in a previous update.
        /// </summary>
        /// <returns></returns>
        public bool TryToCleanLastExe() {
            if (File.Exists(_exeFilePath)) {
                try {
                    File.Delete(_exeFilePath);
                } catch (Exception) {
                    // ignore
                }
            }
            return false;
        }

        /// <summary>
        /// Allows to execute a program during the update.
        /// </summary>
        /// <param name="pathToExe"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool AddProgramExecution(string pathToExe, string parameters = null) {
            if (string.IsNullOrEmpty(pathToExe)) {
                return false;
            }
            if (_output == null) {
                _output = new StringBuilder();
            }

            if (string.IsNullOrEmpty(parameters)) {
                _output.Append("start").Append('\t').Append(pathToExe).AppendLine();
            } else {
                _output.Append("start").Append('\t').Append(pathToExe).Append('\t').Append(parameters).AppendLine();
            }
            return true;
        }

        /// <summary>
        /// Allows to move a file during the update.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public bool AddFileToMove(string from, string to) {
            if (string.IsNullOrEmpty(from) || !File.Exists(from) || string.IsNullOrEmpty(to)) {
                return false;
            }
            var destinationDirectory = Path.GetDirectoryName(to);
            if (Directory.Exists(destinationDirectory) && !IsDirectoryWritable(destinationDirectory)) {
                _requireAdminExe = true;
            }
            if (_output == null) {
                _output = new StringBuilder();
            }
            _output.Append("move").Append('\t').Append(from).Append('\t').Append(to).AppendLine();
            return true;
        }

        /// <summary>
        /// Starts the updater, it will wait for the given process (defaults to current process) to end before starting its business.
        /// </summary>
        /// <param name="pidToWait"></param>
        /// <param name="delayBeforeActionInMilliseconds"></param>
        public void Start(int? pidToWait = null, int? delayBeforeActionInMilliseconds = null) {
            Resources.Resources.WriteSimpleFileUpdateFile(_requireAdminExe, _exeFilePath);
            var actionFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.WriteAllText(actionFilePath, _output.ToString(), Encoding.Default);
            _process = new Process {
                StartInfo = {
                    FileName = _exeFilePath,
                    Arguments = $"--pid {pidToWait ?? Process.GetCurrentProcess().Id} --action-file \"{actionFilePath}\"{(delayBeforeActionInMilliseconds != null ? $" --wait {delayBeforeActionInMilliseconds}" : "")}",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                }
            };
            if (_requireAdminExe) {
                _process.StartInfo.Verb = "runas";
            }
            _process.Start();
        }

        internal void WaitForProcessExit() {
            _process.WaitForExit();
        }

        private static bool IsDirectoryWritable(string dirPath) {
            try {
                var tempPath = Path.Combine(dirPath, Path.GetRandomFileName());
                File.WriteAllText(tempPath, "");
                File.Delete(tempPath);
                return true;
            } catch (Exception) {
                return false;
            }
        }
        
    }
}