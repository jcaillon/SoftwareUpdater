#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (GitHubAsset.cs) is part of SoftwareUpdater.
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
using System.Runtime.Serialization;

namespace SoftwareUpdater.GitHub {

    /// <summary>
    /// Represents an asset (i.e. artifact) of a github release.
    /// </summary>
    [DataContract]
    public class GitHubAsset {

        /// <summary>
        /// The url at which to download the asset.
        /// </summary>
        [DataMember(Name = "browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        /// <summary>
        /// File name of the asset.
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Short description of the asset.
        /// </summary>
        [DataMember(Name = "label")]
        public object Label { get; set; }

        /// <summary>
        /// State of the asset.
        /// </summary>
        [DataMember(Name = "state")]
        public string State { get; set; }

        /// <summary>
        /// Html content type of the asset (for instance application/zip).
        /// </summary>
        [DataMember(Name = "content_type")]
        public string ContentType { get; set; }

        /// <summary>
        /// Size in bytes.
        /// </summary>
        [DataMember(Name = "size")]
        public int Size { get; set; }

        /// <summary>
        /// Number of downloads.
        /// </summary>
        [DataMember(Name = "download_count")]
        public int DownloadCount { get; set; }

        /// <summary>
        /// Date of the creation.
        /// </summary>
        [DataMember(Name = "created_at")]
        public string CreatedAt { get; set; }

        /// <summary>
        /// Date of the last update.
        /// </summary>
        [DataMember(Name = "updated_at")]
        public string UpdatedAt { get; set; }
    }
}