#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (HttpAuthorizationType.cs) is part of SoftwareUpdater.
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
namespace SoftwareUpdater.Http {
    
    /// <summary>
    /// Authorization header type.
    /// </summary>
    internal enum HttpAuthorizationType {
        
        /// <summary>
        /// Basic auth.
        /// </summary>
        Basic,
        
        /// <summary>
        /// Bearer token.
        /// </summary>
        Bearer,
        
        /// <summary>
        /// Token, used for the github api.
        /// </summary>
        Token
        
    }
}