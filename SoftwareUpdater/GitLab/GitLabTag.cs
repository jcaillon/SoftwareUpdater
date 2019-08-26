#region header
// ========================================================================
// Copyright (c) 2019 - Julien Caillon (julien.caillon@gmail.com)
// This file (GitLabTag.cs) is part of SoftwareUpdater.
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

using System.Collections.Generic;
using System.Runtime.Serialization;
using SoftwareUpdater.GitHub;

namespace SoftwareUpdater.GitLab {

    /// <summary>
    /// Represents the meta-data stored by gitlab on a tag.
    /// </summary>
    [DataContract]
    public class GitLabTag {

        /// <summary>
        /// Release version.
        /// </summary>
        [DataMember(Name = "name")]
        public string TagName { get; set; }

        /// <summary>
        /// Message of the git tag.
        /// </summary>
        [DataMember(Name = "message")]
        public string TagMessage { get; set; }

        /// <summary>
        /// SHA1 of the commit tagged.
        /// </summary>
        [DataMember(Name = "target")]
        public string TagSha1 { get; set; }

        /// <summary>
        /// Targeted branch
        /// </summary>
        [DataMember(Name = "release")]
        public GitLabRelease Release { get; set; }
    }
}
