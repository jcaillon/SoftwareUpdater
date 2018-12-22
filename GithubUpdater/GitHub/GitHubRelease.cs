#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (GitHubRelease.cs) is part of GithubUpdater.
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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GithubUpdater.GitHub {

    /// <summary>
    /// Represents a github release.
    /// </summary>
    [DataContract]
    public class GitHubRelease {

        /// <summary>
        /// The url to check this release on a web browser.
        /// </summary>
        [DataMember(Name = "html_url")]
        public string HtmlUrl { get; set; }

        /// <summary>
        /// Url of the zip containing the source code
        /// </summary>
        [DataMember(Name = "zipball_url")]
        public string ZipballUrl { get; set; }

        /// <summary>
        /// Release version
        /// </summary>
        [DataMember(Name = "tag_name")]
        public string TagName { get; set; }

        /// <summary>
        /// Targeted branch
        /// </summary>
        [DataMember(Name = "target_commitish")]
        public string TargetCommitish { get; set; }

        /// <summary>
        /// Release name
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// content of the release text
        /// </summary>
        [DataMember(Name = "body")]
        public string Body { get; set; }

        /// <summary>
        /// Is this a draft of a release?
        /// </summary>
        [DataMember(Name = "draft")]
        public bool Draft { get; set; }
        
        /// <summary>
        /// Is this a pre-release?
        /// </summary>
        [DataMember(Name = "prerelease")]
        public bool Prerelease { get; set; }

        /// <summary>
        /// Date of the creation of this release.
        /// </summary>
        [DataMember(Name = "created_at")]
        public string CreatedAt { get; set; }

        /// <summary>
        /// Dat of the publication of this release.
        /// </summary>
        [DataMember(Name = "published_at")]
        public string PublishedAt { get; set; }

        /// <summary>
        /// List of assets (i.e. artifacts) for this release.
        /// </summary>
        [DataMember(Name = "assets")]
        public List<GitHubAsset> Assets { get; set; }
    }
}