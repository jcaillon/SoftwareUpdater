#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (UpdaterHelper.cs) is part of GithubUpdater.
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

namespace GithubUpdater {
    
    /// <summary>
    /// Helper class for the updater.
    /// </summary>
    public static class UpdaterHelper {
        
        /// <summary>
        /// Converts a valid version string to a version object. (vX.X.X.X-suffix)
        /// </summary>
        /// <param name="versionString"></param>
        /// <returns></returns>
        public static Version StringToVersion(string versionString) {
            var idx = versionString.IndexOf('-');
            versionString = idx > 0 ? versionString.Substring(0, idx) : versionString;
            versionString = versionString.TrimStart('v');
            var nbDots = versionString.Length - versionString.Replace(".", "").Length;
            for (int i = 0; i < 3 - nbDots; i++) {
                versionString += ".0";
            }
            return new Version(versionString);
        }
    }
}