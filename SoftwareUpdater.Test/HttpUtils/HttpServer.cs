#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (HttpServer.cs) is part of SoftwareUpdater.Test.
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
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SoftwareUpdater.Test.HttpUtils {
    
    /// <summary>
    /// HTTP server listener class.
    /// </summary>
    /// <remarks>
    /// Originally from https://github.com/dajuric/simple-http.
    /// </remarks>
    public static class HttpServer {
        
        /// <summary>
        /// Creates and starts a new instance of the http(s) server.
        /// </summary>
        /// <param name="port">The http/https URI listening port.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="onHttpRequest">Action executed on HTTP request.</param>
        /// <param name="localhostOnly"></param>
        /// <param name="useHttps">True to add 'https://' prefix instead of 'http://'. https://github.com/dajuric/simple-http/tree/master/Source/SSL-scripts.</param>
        /// <param name="maxHttpConnectionCount">Maximum HTTP connection count, after which the incoming requests will wait.</param>
        /// <returns>Server listening task.</returns>
        public static async Task ListenAsync(int port, CancellationToken token, Action<HttpListenerRequest, HttpListenerResponse> onHttpRequest, bool localhostOnly = false, bool useHttps = false, byte maxHttpConnectionCount = 32) {
            if (port < 0 || port > ushort.MaxValue)
                throw new NotSupportedException($"The provided port value must in the range: [0..{ushort.MaxValue}");

            var s = useHttps ? "s" : String.Empty;
            var host = localhostOnly ? "127.0.0.1" : "+";
            await ListenAsync($"http{s}://{host}:{port}/", token, onHttpRequest, maxHttpConnectionCount);
        }

        /// <summary>
        /// Creates and starts a new instance of the http(s) server.
        /// </summary>
        /// <param name="httpListenerPrefix">The http/https URI listening prefix.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="onHttpRequest">Action executed on HTTP request.</param>
        /// <param name="maxHttpConnectionCount">Maximum HTTP connection count, after which the incoming requests will wait (sockets are not included).</param>
        /// <returns>Server listening task.</returns>
        public static async Task ListenAsync(string httpListenerPrefix, CancellationToken token, Action<HttpListenerRequest, HttpListenerResponse> onHttpRequest, byte maxHttpConnectionCount = 32) {
            if (token == null) {
                throw new ArgumentNullException(nameof(token), "The provided token must not be null.");
            }

            if (onHttpRequest == null) {
                throw new ArgumentNullException(nameof(onHttpRequest), "The provided HTTP request/response action must not be null.");
            }

            if (maxHttpConnectionCount < 1) {
                throw new ArgumentException("The value must be greater or equal than 1.", nameof(maxHttpConnectionCount));
            }

            var listener = new HttpListener();
            try {
                listener.Prefixes.Add(httpListenerPrefix);
            } catch (Exception ex) {
                throw new ArgumentException("The provided prefix is not supported. Prefixes have the format: 'http(s)://+:(port)/'", ex);
            }

            try {
                listener.Start();
            } catch (Exception ex) when ((ex as HttpListenerException)?.ErrorCode == 5) {
                var msg = GetNamespaceReservationExceptionMessage(httpListenerPrefix);
                throw new UnauthorizedAccessException(msg, ex);
            }

            using (var s = new SemaphoreSlim(maxHttpConnectionCount)) {
                using (token.Register(() => listener.Close())) {
                    bool shouldStop = false;
                    while (!shouldStop) {
                        HttpListenerContext ctx = null;
                        try {
                            ctx = await listener.GetContextAsync();
                            await s.WaitAsync(token);
                            await Task.Factory.StartNew(() => onHttpRequest(ctx.Request, ctx.Response), token);
                            s.Release();
                        } catch (HttpListenerException e) {
                            if (e.ErrorCode != 64 && e.ErrorCode != 1229) {
                                Console.WriteLine(e);
                            }
                        } catch (Exception e) {
                            if (token.IsCancellationRequested) {
                                break;
                            }
                            Console.WriteLine(e);
                            try {
                                ctx?.Response.WithCode(HttpStatusCode.InternalServerError).AsText(e.Message);
                            } catch (Exception) {
                                // do nothing
                            }
                        } finally {
                            ctx?.Response.Close();
                            if (token.IsCancellationRequested) {
                                shouldStop = true;
                            }
                        }
                    }
                }
            }
        }

        private static string GetNamespaceReservationExceptionMessage(string httpListenerPrefix) {
            string msg;
            var m = Regex.Match(httpListenerPrefix, @"(?<protocol>\w+)://localhost:?(?<port>\d*)");
            if (m.Success) {
                var protocol = m.Groups["protocol"].Value;
                var port = m.Groups["port"].Value;
                if (String.IsNullOrEmpty(port)) {
                    port = 80.ToString();
                }

                msg = $"The HTTP server can not be started, as the namespace reservation already exists.\nPlease run (elevated): 'netsh http delete urlacl url={protocol}://+:{port}/'.";
            } else {
                msg = $"The HTTP server can not be started, as the namespace reservation does not exist.\nPlease run (elevated): 'netsh http add urlacl url={httpListenerPrefix} user=\"Everyone\"'.";
            }

            return msg;
        }

        public static void Stop(CancellationTokenSource cts, params Task[] tasks) {
            try {
                cts.Cancel();
                Task.WaitAll(tasks);
            } catch (Exception ex) {
                if (ex is AggregateException)
                    ex = ex.InnerException;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + ex);
                Console.ResetColor();
            }
        }

        public static string[] GetHeaderValues(this NameValueCollection headers, string headerPropertyName) {
            foreach (var t in headers.AllKeys) {
                if (t.Equals(headerPropertyName, StringComparison.OrdinalIgnoreCase)) {
                    return headers.GetValues(t);
                }
            }

            return null;
        }

        /// <summary>
        /// Writes the specified data to the response.
        /// <para>Response is closed and can not be longer modified.</para>
        /// </summary>
        /// <param name="response">HTTP response.</param>
        /// <param name="txt">Text data to write.</param>
        /// <param name="mime">Mime type.</param>
        public static void AsText(this HttpListenerResponse response, string txt, string mime = "text/html") {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            if (txt == null)
                throw new ArgumentNullException(nameof(txt));

            if (mime == null)
                throw new ArgumentNullException(nameof(mime));

            var data = Encoding.UTF8.GetBytes(txt);

            response.ContentLength64 = data.Length;
            response.ContentType = $"{mime}; charset=utf-8";
            response.OutputStream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Sets the status code for the response.
        /// </summary>
        /// <param name="response">HTTP response.</param>
        /// <param name="statusCode">HTTP status code.</param>
        /// <returns>Modified HTTP response.</returns>
        public static HttpListenerResponse WithCode(this HttpListenerResponse response, HttpStatusCode statusCode = HttpStatusCode.OK) {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            response.StatusCode = (int) statusCode;
            return response;
        }

        /// <summary>
        /// Sets the specified header for the response.
        /// </summary>
        /// <param name="response">HTTP response.</param>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        /// <returns>Modified HTTP response.</returns>
        public static HttpListenerResponse WithHeader(this HttpListenerResponse response, string name, string value) {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (String.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(name));

            switch (name.ToLower()) {
                case "content-length":
                    int.TryParse(value, out int vInt);
                    response.ContentLength64 = vInt;
                    break;
                case "content-type":
                    response.ContentType = value;
                    break;
                case "keep-alive":
                    bool.TryParse(value, out bool vBool);
                    response.KeepAlive = vBool;
                    break;
                case "transfer-encoding":
                    if (value.Contains("chunked"))
                        response.SendChunked = true;
                    else
                        response.Headers[name] = value;
                    break;
                case "www-authenticate":
                    response.AddHeader(name, value);
                    break;
                default:
                    response.Headers[name] = value;
                    break;
            }

            return response;
        }

    }
}