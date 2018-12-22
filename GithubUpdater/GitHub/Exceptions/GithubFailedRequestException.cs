#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (GithubFailedRequestException.cs) is part of GithubUpdater.
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

namespace GithubUpdater.GitHub.Exceptions {
    
    /// <summary>
    /// Exception thrown on response problems from github.
    /// </summary>
    public class GithubFailedRequestException : Exception {
        
        internal GithubFailedRequestException(string message, Exception innerException) : base(message, innerException) { }
    }
}