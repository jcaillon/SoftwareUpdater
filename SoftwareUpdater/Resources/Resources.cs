#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (Resources.cs) is part of SoftwareUpdater.
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
using System.Linq;
using System.Reflection;

namespace SoftwareUpdater.Resources {

    /// <summary>
    /// Resources.
    /// </summary>
    internal static class Resources {

        /// <summary>
        /// Write the embedded files.
        /// </summary>
        /// <param name="coreVersion"></param>
        /// <param name="adminVersion"></param>
        /// <param name="directoryPath"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static string WriteSimpleFileUpdateFile(bool coreVersion, bool adminVersion, string directoryPath) {

            if (string.IsNullOrEmpty(directoryPath)) {
                throw new ArgumentNullException(nameof(directoryPath));
            }
            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            var resourceNamespace = $"{nameof(SoftwareUpdater)}.{nameof(Resources)}.SimpleFileUpdater{(coreVersion ? "_core" : adminVersion ? "_admin" : "")}.";
            var resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(n => n.StartsWith(resourceNamespace));

            string mainFileName = null;

            foreach (var resourceName in resourceNames) {
                var fileName = Path.GetFileName(resourceName);
                if (string.IsNullOrEmpty(fileName)) {
                    continue;
                }

                fileName = fileName.Replace(resourceNamespace, "");
                if (fileName.EndsWith(".exe") || fileName.EndsWith(".dll")) {
                    mainFileName = fileName;
                }

                var targetFilePath = Path.Combine(directoryPath, fileName);
                using (var writer = new StreamWriter(File.OpenWrite(targetFilePath))) {
                    using (Stream resFileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
                        resFileStream?.CopyTo(writer.BaseStream);
                    }
                }
            }

            return Path.Combine(directoryPath, mainFileName ?? "");
        }
    }
}
