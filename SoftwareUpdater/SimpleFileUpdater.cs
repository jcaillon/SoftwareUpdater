#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (SimpleFileUpdater.cs) is part of SoftwareUpdater.
//
// SoftwareUpdater is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoftwareUpdater is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoftwareUpdater. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using SoftwareUpdater.Utilities;

#if WINDOWSONLYBUILD
using System.Security.Principal;
using System.Security.AccessControl;
#else
using System.Runtime.InteropServices;
#endif

[assembly: InternalsVisibleTo("SoftwareUpdater.Test")]

namespace SoftwareUpdater {

    /// <summary>
    /// A simple file updater that allows to move/copy files or start a process after the current program has been executed.
    /// </summary>
    public class SimpleFileUpdater {

        private static SimpleFileUpdater _instance;

        /// <summary>
        /// Get the singleton instance of the updater.
        /// </summary>
        public static SimpleFileUpdater Instance => _instance ?? (_instance = new SimpleFileUpdater());

        private StringBuilder _output;
        private Process _process;
        private static HashSet<string> _writableDirectories;
        private string _actionFilePath;

        private string ExeDirectoryPath => Path.Combine(Path.GetTempPath(), UpdaterExecutableSubDirectory ?? "", "SimpleFileUpdater");

        /// <summary>
        /// new instance.
        /// </summary>
        private SimpleFileUpdater() { }

        /// <summary>
        /// Overload the sub directory in the temporary directory where to create the SimpleFileUpdater executable.
        /// </summary>
        public string UpdaterExecutableSubDirectory { get; set; }

        /// <summary>
        /// Will the updater need admin rights to do its job?
        /// When using <see cref="AddFileToMove"/> or <see cref="AddFileToCopy"/>, the rights needed to write the files
        /// are checked and this boolean is set to true if needed.
        /// </summary>
        public bool IsAdminRightsNeeded { get; private set; }

        /// <summary>
        /// Try to clean up the exe used in a previous update.
        /// </summary>
        /// <returns></returns>
        public bool TryToCleanLastExe() {
            var exeDirectoryPath = ExeDirectoryPath;
            if (Directory.Exists(exeDirectoryPath)) {
                try {
                    Directory.Delete(exeDirectoryPath, true);
                    return true;
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
        /// Allows to copy a file during the update.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public bool AddFileToCopy(string from, string to) {
            return AddFileToMoveOrCopy(from, to, true);
        }

        /// <summary>
        /// Allows to move a file during the update.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public bool AddFileToMove(string from, string to) {
            return AddFileToMoveOrCopy(from, to, false);
        }

        /// <summary>
        /// Allows to remove a directory (recursively) during the update.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool AddDirectoryToRemove(string path) {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) {
                return false;
            }
            return AddPathToRemove(path);
        }

        /// <summary>
        /// Allows to remove a file during the update.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool AddFileToRemove(string path) {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
                return false;
            }
            if (!FilePathHasWritePermission(path)) {
                IsAdminRightsNeeded = true;
            }
            return AddPathToRemove(path);
        }

        private bool AddPathToRemove(string path) {
            if (_output == null) {
                _output = new StringBuilder();
            }
            _output.Append("remove").Append('\t').Append(path).AppendLine();
            return true;
        }

        private bool AddFileToMoveOrCopy(string from, string to, bool isCopy) {
            if (string.IsNullOrEmpty(from) || !File.Exists(from) || string.IsNullOrEmpty(to)) {
                return false;
            }
            if (!FilePathHasWritePermission(to)) {
                IsAdminRightsNeeded = true;
            }
            if (_output == null) {
                _output = new StringBuilder();
            }
            _output.Append(isCopy ? "copy" : "move").Append('\t').Append(from).Append('\t').Append(to).AppendLine();
            return true;
        }

        /// <summary>
        /// Starts the updater, it will wait for the given process (defaults to current process) to end before starting its business.
        /// </summary>
        /// <param name="pidToWait"></param>
        /// <param name="delayBeforeActionInMilliseconds"></param>
        public void Start(int? pidToWait = null, int? delayBeforeActionInMilliseconds = null) {
            if (_process != null) {
                try {
                    _process?.Kill();
                    _process?.WaitForExit();
                    _process?.Dispose();
                    if (File.Exists(_actionFilePath)) {
                        File.Delete(_actionFilePath);
                    }
                } catch (Exception) {
                    // ignored
                }
                _process = null;
            }
            var exeDirectoryPath = ExeDirectoryPath;
            if (!Directory.Exists(exeDirectoryPath)) {
                Directory.CreateDirectory(exeDirectoryPath);
            }
            var executablePath = Resources.Resources.WriteSimpleFileUpdateFile(DotNet.IsNetStandardBuild, IsAdminRightsNeeded, exeDirectoryPath);
            _actionFilePath = Path.Combine(exeDirectoryPath, Path.GetRandomFileName());
            File.WriteAllText(_actionFilePath, _output.ToString(), Encoding.Default);

            _process = new Process {
                StartInfo = {
                    FileName = DotNet.IsNetStandardBuild ? DotNet.FullPathOrDefault() : executablePath,
                    Arguments = $"{(DotNet.IsNetStandardBuild ? $"{Path.GetFileName(executablePath)} " : "")}--pid {pidToWait ?? Process.GetCurrentProcess().Id} --action-file \"{_actionFilePath}\"{(delayBeforeActionInMilliseconds != null ? $" --wait {delayBeforeActionInMilliseconds}" : "")}",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = !DotNet.IsNetStandardBuild,
                    WorkingDirectory = exeDirectoryPath,
                    Verb = IsAdminRightsNeeded ? "runas" : ""
                }
            };
            _process.Start();
        }

        internal void WaitForProcessExit() {
            try {
                _process.WaitForExit();
            } catch (InvalidOperationException) {
                // ignored
            }
        }

        /// <summary>
        /// Test if the current user has the permission to write files to a directory.
        /// </summary>
        /// <param name="directoryPath">Full path to directory </param>
        /// <returns>true if write permission</returns>
        public static bool DirectoryPathHasFileWritePermission(string directoryPath) {
            if (string.IsNullOrEmpty(directoryPath)) {
                return false;
            }
            try {
                using (File.Create(Path.Combine(directoryPath, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose)) { }
                return true;
            } catch {
                // ignored
            }
            return false;
        }

	    /// <summary>
        /// Returns true if a file path is writable with the current user.
        /// </summary>
        /// <param name="filePath">Full path to file to test.</param>
        /// <returns>true if write permission</returns>
        public static bool FilePathHasWritePermission(string filePath) {
            try {
                if (string.IsNullOrEmpty(filePath)) {
                    return false;
                }
#if NETFRAMEWORK
                if (File.Exists(filePath)) {
                    return IsCurrentUserMatchingRule(File.GetAccessControl(filePath).GetAccessRules(true, true, typeof(SecurityIdentifier)), FileSystemRights.Write);
                }
#endif
                var needSubDirCreation = false;
				var directoryPath = Path.GetDirectoryName(filePath);
	            while (!Directory.Exists(directoryPath)) {
                    needSubDirCreation = true;
	                directoryPath = Path.GetDirectoryName(directoryPath);
	            }
                if (_writableDirectories == null) {
#if WINDOWSONLYBUILD
                    _writableDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
#else
                    _writableDirectories = new HashSet<string>(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
#endif
                }
                if (_writableDirectories.Contains(directoryPath)) {
                    return true;
                }
                using (File.Create(Path.Combine(directoryPath, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose)) { }

                if (needSubDirCreation) {
                    var subDir = Path.Combine(directoryPath, Path.GetRandomFileName());
                    Directory.CreateDirectory(subDir);
                    Directory.Delete(subDir);
                }

                _writableDirectories.Add(directoryPath);
                return true;
            } catch (Exception) {
                return false;
            }
        }

#if NETFRAMEWORK
        private static bool IsCurrentUserMatchingRule(AuthorizationRuleCollection rules, FileSystemRights accessRight) {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            foreach (FileSystemAccessRule rule in rules) {
                if (identity.User != null && identity.User.Equals(rule.IdentityReference) || identity.Groups != null && identity.Groups.Contains(rule.IdentityReference)) {
                    if ((accessRight & rule.FileSystemRights) == accessRight) {
                        if (rule.AccessControlType == AccessControlType.Allow)
                            return true;
                    }
                }
            }
            return false;
        }
#endif
    }
}
