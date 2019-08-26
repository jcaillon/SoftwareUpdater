#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (HttpResponse.cs) is part of SoftwareUpdater.
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
using System.Net;

namespace SoftwareUpdater.Http {
    
    /// <summary>
    /// A response from an http web request.
    /// </summary>
    internal class HttpResponse {

        /// <summary>
        /// Was the request successful?
        /// </summary>
        public bool Success => Exception == null && (int) StatusCode >= 200 && (int) StatusCode < 300;

        /// <summary>
        /// Status code of the request, use this to know if it went ok
        /// </summary>
        public HttpStatusCode StatusCode { get; internal set; } = HttpStatusCode.BadRequest;

        /// <summary>
        /// Status description of the request
        /// </summary>
        public string StatusDescription { get; internal set; }
        
        /// <summary>
        /// Exception caught during the request, will be null if all went ok
        /// </summary>
        public Exception Exception { get; internal set; }
    }
}