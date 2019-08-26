#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (SimpleGithubServer.cs) is part of SoftwareUpdater.Test.
//
// SoftwareUpdater.Test is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoftwareUpdater.Test is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoftwareUpdater.Test. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using SoftwareUpdater.GitLab;

namespace SoftwareUpdater.Test.HttpUtils {

    public class SimpleGitLabServer {

        private const int BufferSize = 1024 * 32;
        private string _rootPath;
        private string _token;

        public SimpleGitLabServer(string rootPath, string token) {
            _rootPath = rootPath;
            _token = token;
        }

        public List<GitLabTag> Tags { get; set; }

        public void OnHttpRequest(HttpListenerRequest request, HttpListenerResponse response) {
            // handle basic authent
            var receivedAuthent = request.Headers.GetHeaderValues("PRIVATE-TOKEN")?.FirstOrDefault();
            if (!string.IsNullOrEmpty(_token)) {
                if (string.IsNullOrEmpty(receivedAuthent)) {
                    // or HttpListener.AuthenticationSchemes
                    response.WithHeader("WWW-Authenticate", "OAuth").WithCode(HttpStatusCode.Unauthorized).AsText("Authentication required.");
                    return;
                }
                if (!_token.Equals(receivedAuthent)) {
                    response.WithCode(HttpStatusCode.Forbidden).AsText("Incorrect PRIVATE-TOKEN.");
                    return;
                }
            }

            // check verb
            if (request.HttpMethod.ToUpper() != "GET" && request.HttpMethod.ToUpper() != "HEAD") {
                throw new Exception($"Unknown http verb : {request.HttpMethod}.");
            }

            // get url path
            string urlAbsolutePath = request.Url.AbsolutePath;
            if (!string.IsNullOrEmpty(urlAbsolutePath) && urlAbsolutePath.Length > 1) {
                // handle spaces.
                urlAbsolutePath = WebUtility.UrlDecode(urlAbsolutePath.Substring(1));
            }

            if (string.IsNullOrEmpty(urlAbsolutePath)) {
                response.WithCode(HttpStatusCode.NotFound).AsText("Path not specified.");
                return;
            }

            if (urlAbsolutePath.Contains("repository/tags")) {

                var inputSerializer = new DataContractJsonSerializer(typeof(List<GitLabTag>));

                inputSerializer.WriteObject(response.OutputStream, Tags);

                response.WithHeader("content-type", "application/json; charset=utf-8").WithCode();
                return;
            }

            if (urlAbsolutePath.Contains("repository/archive")) {
                response.ContentType = "application/octet-stream";
                response.ContentLength64 = 10;

                if (request.HttpMethod.ToUpper().Equals("GET")) {
                    byte[] buffer = new byte[BufferSize];
                    response.OutputStream.Write(new byte[10], 0, 10);
                    response.OutputStream.Flush();
                }
                var lastTimeWrite = File.GetLastWriteTimeUtc(urlAbsolutePath);
                response.WithHeader("ETag", lastTimeWrite.Ticks.ToString("x")).WithHeader("Last-Modified", lastTimeWrite.ToString("R")).WithCode();
                return;
            }

            urlAbsolutePath = Path.Combine(_rootPath, urlAbsolutePath);

            if (!File.Exists(urlAbsolutePath)) {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            } else {
                using (var stream = File.OpenRead(urlAbsolutePath)) {
                    response.ContentType = "application/octet-stream";
                    response.ContentLength64 = stream.Length;

                    if (request.HttpMethod.ToUpper().Equals("GET")) {
                        byte[] buffer = new byte[BufferSize];
                        int nbBytesRead;
                        while ((nbBytesRead = stream.Read(buffer, 0, buffer.Length)) > 0) {
                            response.OutputStream.Write(buffer, 0, nbBytesRead);
                        }

                        response.OutputStream.Flush();
                    }
                }
                var lastTimeWrite = File.GetLastWriteTimeUtc(urlAbsolutePath);
                response.WithHeader("ETag", lastTimeWrite.Ticks.ToString("x")).WithHeader("Last-Modified", lastTimeWrite.ToString("R")).WithCode();
            }
        }
    }
}
