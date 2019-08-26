#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (DownloadProgress.cs) is part of SoftwareUpdater.
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
namespace SoftwareUpdater {
    
    /// <summary>
    /// The progression of a download.
    /// </summary>
    public class DownloadProgress {
        
        /// <summary>
        /// Total nb of bytes that needs to be exchanged.
        /// </summary>
        public long NumberOfBytesTotal { get; }
        
        /// <summary>
        /// The nb of bytes already exchanged.
        /// </summary>
        public long NumberOfBytesDoneTotal { get; }
        
        internal DownloadProgress(long numberOfBytesTotal, long numberOfBytesDoneTotal) {
            NumberOfBytesTotal = numberOfBytesTotal;
            NumberOfBytesDoneTotal = numberOfBytesDoneTotal;
        }
    }
}