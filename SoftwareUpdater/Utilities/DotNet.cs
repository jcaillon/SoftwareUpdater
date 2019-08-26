#region header

// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (DotNetExe.cs) is part of SoftwareUpdater.
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
using System.IO;

#if !WINDOWSONLYBUILD
using System.Diagnostics;
using System.Runtime.InteropServices;
#endif

namespace SoftwareUpdater.Utilities {

    /// <summary>
    /// Utilities for finding the "dotnet.exe" file from the currently running .NET Core application.
    /// Credits go to: https://github.com/natemcmaster/CommandLineUtils.
    /// </summary>
    internal static class DotNet {
        private const string FileName = "dotnet";

        static DotNet() {
            FullPath = TryFindDotNetExePath();
        }

        /// <summary>
        /// The full filepath to the .NET Core CLI executable.
        /// <para>
        /// May be <c>null</c> if the CLI cannot be found. <seealso cref="FullPathOrDefault" />
        /// </para>
        /// </summary>
        /// <returns>The path or null</returns>
        public static string FullPath { get; }

        /// <summary>
        /// Finds the full filepath to the .NET Core CLI executable,
        /// or returns a string containing the default name of the .NET Core muxer ('dotnet').
        /// <returns>The path or a string named 'dotnet'</returns>
        /// </summary>
        public static string FullPathOrDefault() => FullPath ?? FileName;

        private static string TryFindDotNetExePath() {
            var fileName = FileName;
#if WINDOWSONLYBUILD
            fileName += ".exe";
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                fileName += ".exe";
            }

            var mainModule = Process.GetCurrentProcess().MainModule;
            if (!string.IsNullOrEmpty(mainModule.FileName) && Path.GetFileName(mainModule.FileName).Equals(fileName, StringComparison.OrdinalIgnoreCase)) {
                return mainModule.FileName;
            }
#endif
            var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (!string.IsNullOrEmpty(dotnetRoot)) {
                return Path.Combine(dotnetRoot, fileName);
            }

            return null;
        }

        /// <summary>
        /// Returns true if the current executable targets .net core.
        /// </summary>
        public static bool IsNetStandardBuild {
            get {
#if WINDOWSONLYBUILD
                return false;
#else
                return true;
#endif
            }
        }
    }
}
