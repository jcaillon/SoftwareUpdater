#region header
// ========================================================================
// Copyright (c) 2019 - Julien Caillon (julien.caillon@gmail.com)
// This file (GitLabRelease.cs) is part of GithubUpdater.
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

using System.Runtime.Serialization;

namespace GithubUpdater.GitLab {

    /// <summary>
    /// Represents a gitlab release (consists only in a message as a meta data on a tag).
    /// </summary>
    [DataContract]
    public class GitLabRelease {

        /// <summary>
        /// The tag on which this release is based.
        /// </summary>
        [DataMember(Name = "tag_name")]
        public string TagName { get; set; }

        /// <summary>
        /// The release description.
        /// </summary>
        [DataMember(Name = "description")]
        public string Description { get; set; }
    }
}
