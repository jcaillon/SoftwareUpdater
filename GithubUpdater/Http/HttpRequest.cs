#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (HttpRequest.cs) is part of GithubUpdater.
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace GithubUpdater.Http {

    /// <summary>
    /// Handles http requests.
    /// </summary>
    internal class HttpRequest {

        private const int DefaultBufferSize = 64 * 1024;
        private const string AuthorizationHeader = "Authorization";
        private const string ProxyAuthorizationHeader = "Proxy-Authorization";

        private string _baseUrl;

        private int _bufferSize = DefaultBufferSize;

        private int _timeOut = Timeout.Infinite;
        private int _readWriteTimeOut = Timeout.Infinite;

        private IWebProxy _proxy = WebRequest.DefaultWebProxy;
        private NetworkCredential _basicCredential;
        private Dictionary<string, string> _headersKeyValue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string _proxyAuthorizationHeader;
        private string _userAgent = $"{nameof(HttpRequest)}/{typeof(HttpRequest).Assembly.GetName().Version}";

        private CancellationToken? _cancelToken;

        private bool _expectContinue = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="baseUrl"></param>
        public HttpRequest(string baseUrl) {
            UseBaseUrl(baseUrl);
        }

        /// <summary>
        /// Use an http proxy for your requests. (would be <see cref="WebRequest.DefaultWebProxy"/> by default).
        /// </summary>
        /// <remarks>
        /// If you intend to use a locally served proxy, don't use 127.0.0.1 or localhost but use your machine name instead.
        /// That is because .net framework is hardcoded to not sent req for localhost through the proxy.
        /// </remarks>
        /// <param name="address">Can be null for a null proxy (direct connection).</param>
        /// <param name="userName">domain\user. Can be null if no credentials are needed.</param>
        /// <param name="userPassword"></param>
        /// <param name="bypassProxyOnLocal"></param>
        /// <param name="sendProxyAuthorizationBeforeServerChallenge">Send the proxy-authorization header before the 407 challenge from the proxy server.</param>
        public HttpRequest UseProxy(string address, string userName = null, string userPassword = null, bool bypassProxyOnLocal = false, bool sendProxyAuthorizationBeforeServerChallenge = true) {
            _proxyAuthorizationHeader = !sendProxyAuthorizationBeforeServerChallenge ? null : $"{HttpAuthorizationType.Basic} {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{userPassword}"))}";
            _proxy = string.IsNullOrEmpty(address) ? null : new WebProxy(address) {
                UseDefaultCredentials = false,
                BypassProxyOnLocal = bypassProxyOnLocal,
                Credentials = string.IsNullOrEmpty(userName) ? null : new NetworkCredential(userName, userPassword)
            };
            return this;
        }

        /// <summary>
        /// Sets an authorization header.
        /// </summary>
        /// <param name="authorizationType"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        public HttpRequest UseAuthorizationHeader(HttpAuthorizationType authorizationType, string credentials) {
            UseHeader(AuthorizationHeader, string.IsNullOrEmpty(credentials) ? "" : $"{authorizationType} {credentials}");
            return this;
        }

        /// <summary>
        /// Sets a timeout for the http request. Defaults to infinite. You can also use <see cref="UseCancellationToken"/> for that purpose.
        /// </summary>
        /// <param name="timeOut"></param>
        /// <param name="readWriteTimeOut"></param>
        /// <returns></returns>
        public HttpRequest UseTimeout(int timeOut, int readWriteTimeOut) {
            _timeOut = timeOut;
            _readWriteTimeOut = readWriteTimeOut;
            return this;
        }

        /// <summary>
        /// Sets the buffer size to use for the request.
        /// </summary>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public HttpRequest UseBufferSize(int bufferSize) {
            _bufferSize = bufferSize;
            return this;
        }

        /// <summary>
        /// Sets the base url for the request.
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <returns></returns>
        public HttpRequest UseBaseUrl(string baseUrl) {
            _baseUrl = baseUrl?.Trim().TrimEnd('/');
            if (!string.IsNullOrEmpty(_baseUrl)) {
                _baseUrl += '/';
            }
            return this;
        }

        /// <summary>
        /// Sets a cancellation token for the request.
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public HttpRequest UseCancellationToken(CancellationToken? cancelToken) {
            _cancelToken = cancelToken;
            return this;
        }

        /// <summary>
        /// Sets a Basic authorization for the request.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="userPassword"></param>
        /// <param name="sendAuthorizationBeforeServerChallenge">Send the authorization header before receiving the 401 challenge from the server.</param>
        /// <returns></returns>
        public HttpRequest UseBasicAuthorizationHeader(string userName, string userPassword, bool sendAuthorizationBeforeServerChallenge = true) {
            _basicCredential = string.IsNullOrEmpty(userName) ? null : new NetworkCredential(userName, userPassword);
            if (sendAuthorizationBeforeServerChallenge) {
                UseHeader(AuthorizationHeader, $"{HttpAuthorizationType.Basic} {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{userPassword}"))}");
            }
            return this;
        }

        /// <summary>
        /// Sets extra headers to use for the request
        /// </summary>
        /// <param name="headersKeyValue"></param>
        /// <returns></returns>
        public HttpRequest UseHeaders(Dictionary<string, string> headersKeyValue) {
            foreach (var kpv in headersKeyValue) {
                UseHeader(kpv.Key, kpv.Value);
            }
            return this;
        }

        /// <summary>
        /// Set header to use for the request.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public HttpRequest UseHeader(string key, string value) {
            if (_headersKeyValue.ContainsKey(key)) {
                _headersKeyValue[key] = value;
            } else {
                _headersKeyValue.Add(key, value);
            }
            return this;
        }

        /// <summary>
        /// Clear all the headers defined.
        /// </summary>
        /// <returns></returns>
        public HttpRequest ClearAllHeaders() {
            _headersKeyValue.Clear();
            return this;
        }

        /// <summary>
        /// Get the response from a webservice that sends back json.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="output"></param>
        /// <typeparam name="TOutput"></typeparam>
        /// <returns></returns>
        public HttpResponse GetJson<TOutput>(string relativePath, out TOutput output) where TOutput : class {
            return RequestJson(HttpRequestMethod.Get, relativePath, (object) null, out output);
        }

        /// <summary>
        /// Make a request to a webservice that accepts and sends back json.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="relativePath"></param>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public HttpResponse RequestJson<TInput, TOutput>(HttpRequestMethod method, string relativePath, TInput input, out TOutput output) where TOutput : class {
            DataContractJsonSerializer inputSerializer;
            DataContractJsonSerializer outputSerializer;
            try {
                inputSerializer = new DataContractJsonSerializer(typeof(TInput));
                outputSerializer = new DataContractJsonSerializer(typeof(TOutput));
            } catch (Exception e) {
                throw new Exception("The input and/or output objects are not serializable. Use the attributes [DataContract] and [DataMember].", e);
            }

            void ModifyRequest(HttpWebRequest request) {
                request.ContentType = "application/json; charset=utf-8";
            }

            void WriteToUpStream(Stream upStream) {
                inputSerializer.WriteObject(upStream, input);
            }

            Encoding responseEncoding = Encoding.UTF8;
            void HandleResponse(HttpWebResponse response) {
                try {
                    string[] contentType;
                    try {
                        contentType = response.Headers.GetValues("Content-Type");
                    } catch (Exception) {
                        contentType = null;
                    }
                    if (contentType != null) {
                        var chatSet = string.Join("; ", contentType).Split(';').Select(s => s.Trim()).FirstOrDefault(s => s.StartsWith("charset="));
                        if (!string.IsNullOrEmpty(chatSet)) {
                            responseEncoding = Encoding.GetEncoding(chatSet.Replace("charset=", ""));
                        }
                    } else {
                        responseEncoding = Encoding.UTF8;
                    }
                } catch (Exception) {
                    responseEncoding = Encoding.UTF8;
                }
            }

            var outputObject = default(TOutput);
            void ReadDownStream(Stream downStream) {
                if (Equals(responseEncoding, Encoding.UTF8)) {
                    outputObject = outputSerializer.ReadObject(downStream) as TOutput;
                } else {
                    using (var downStreamEncoded = new StreamReader(downStream, responseEncoding)) {
                        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(downStreamEncoded.ReadToEnd()))) {
                            outputObject = outputSerializer.ReadObject(stream) as TOutput;
                        }
                    }
                }
            }

            var outputResponse = Execute(method, relativePath, ModifyRequest, input != null ? WriteToUpStream : (Action<Stream>) null, HandleResponse, ReadDownStream);

            output = outputObject;

            return outputResponse;
        }

        /// <summary>
        /// Download a file from the server, handles progression.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="downloadFilePath"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public HttpResponse DownloadFile(string relativePath, string downloadFilePath, Action<DownloadProgress> progress = null) {

            var dir = Path.GetDirectoryName(downloadFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            long totalLength = 0;
            void HandleResponse(HttpWebResponse response) {
                totalLength = response.ContentLength;
            }

            void ReadDownStream(Stream downStream) {
                using (var fileStream = File.OpenWrite(downloadFilePath)) {
                    byte[] buffer = new byte[_bufferSize];
                    int nbBytesRead;
                    long totalDone = 0;
                    while ((nbBytesRead = downStream.Read(buffer, 0, buffer.Length)) > 0) {
                        fileStream.Write(buffer, 0, nbBytesRead);
                        totalDone += nbBytesRead;
                        progress?.Invoke(new DownloadProgress(totalLength, totalDone));
                        _cancelToken?.ThrowIfCancellationRequested();
                    }

                    if (totalLength > 0 && totalDone != totalLength) {
                        throw new Exception($"File download failed, {totalDone} bytes read but {totalLength} bytes were expected.");
                    }
                }
            }

            return Execute(HttpRequestMethod.Get, relativePath, null, null, HandleResponse, ReadDownStream);
        }

        protected HttpWebRequest CreateRequest(HttpRequestMethod method, string relativePath) {
            string url;
            if (relativePath.StartsWith("http://") || relativePath.StartsWith("https://")) {
                url = relativePath;
            } else {
                url = $"{_baseUrl ?? ""}{relativePath.TrimStart('/')}";
            }
            if (string.IsNullOrEmpty(url)) {
                throw new NullReferenceException("Url can't be null or empty.");
            }
            var httpRequest = WebRequest.CreateHttp(url);

            httpRequest.ReadWriteTimeout = _readWriteTimeOut;
            httpRequest.Timeout = _timeOut;
            httpRequest.PreAuthenticate = true;

            if (_basicCredential != null) {
                httpRequest.Credentials = _basicCredential;
            }

            if (_proxy != null) {
                httpRequest.Proxy = _proxy;
                if (!string.IsNullOrEmpty(_proxyAuthorizationHeader)) {
                    httpRequest.Headers.Add(ProxyAuthorizationHeader, _proxyAuthorizationHeader);
                }
            }

            httpRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            httpRequest.UserAgent = _userAgent;
            httpRequest.Accept = "*/*";
            httpRequest.KeepAlive = true;

            if (!_expectContinue) {
                httpRequest.Expect = null;

            }

            foreach (var kpv in _headersKeyValue) {
                switch (kpv.Key.ToLower()) {
                    case "content-type":
                        httpRequest.ContentType = kpv.Value;
                        break;
                    case "keep-alive":
                        bool.TryParse(kpv.Value, out bool vBool);
                        httpRequest.KeepAlive = vBool;
                        break;
                    case "accept":
                        httpRequest.Accept = kpv.Value;
                        break;
                    case "user-agent":
                        httpRequest.UserAgent = kpv.Value;
                        break;
                    default:
                        httpRequest.Headers[kpv.Key] = kpv.Value;
                        break;
                }
            }

            httpRequest.Method = method.ToString().ToUpper(CultureInfo.InvariantCulture);

            return httpRequest;
        }

        protected HttpResponse Execute(HttpRequestMethod method, string relativePath, Action<HttpWebRequest> modifyRequest = null, Action<Stream> writeToUpStream = null, Action<HttpWebResponse> handleResponse = null, Action<Stream> readDownStream = null) {

            var output = new HttpResponse();

            try {
                var httpRequest = CreateRequest(method, relativePath);

                using (_cancelToken?.Register(() => {
                    httpRequest.Abort();
                })) {

                    modifyRequest?.Invoke(httpRequest);

                    // write to upstream
                    if (writeToUpStream != null) {
                        using (var upStream = httpRequest.GetRequestStream()) {
                            writeToUpStream(upStream);
                        }
                    }

                    // get response
                    using (var httpWebResponse = (HttpWebResponse) httpRequest.GetResponse()) {
                        output.StatusCode = httpWebResponse.StatusCode;
                        output.StatusDescription = httpWebResponse.StatusDescription;
                        handleResponse?.Invoke(httpWebResponse);

                        // read downstream
                        if (readDownStream != null) {
                            using (var downStream = httpWebResponse.GetResponseStream()) {
                                readDownStream(downStream);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                output.Exception = e;
                if (e is WebException we &&  we.Response is HttpWebResponse hwr) {
                    output.StatusCode = hwr.StatusCode;
                    output.StatusDescription = hwr.StatusDescription;
                }
            }

            if (!(output.Exception is OperationCanceledException) && (_cancelToken?.IsCancellationRequested ?? false)) {
                output.Exception = new OperationCanceledException();
            }

            return output;
        }

    }
}
