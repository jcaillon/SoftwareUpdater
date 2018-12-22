#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (Resources.cs) is part of GithubUpdater.
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
using System.IO;
using System.Reflection;

namespace GithubUpdater.Resources {

    /// <summary>
    /// Resources.
    /// </summary>
    internal static class Resources {

        /// <summary>
        /// Write the embedded files.
        /// </summary>
        /// <param name="adminVersion"></param>
        /// <param name="filePath"></param>
        public static void WriteSimpleFileUpdateFile(bool adminVersion, string filePath) {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
            using (var writer = new StreamWriter(File.OpenWrite(filePath))) {
                using (Stream resFilestream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(GithubUpdater)}.{nameof(Resources)}.SimpleUpdater.SimpleFileUpdater{(adminVersion ? "_admin" : "")}.exe")) {
                    if (resFilestream != null) {
                        resFilestream.CopyTo(writer.BaseStream);
                    }
                }
            }
        }
    }
}