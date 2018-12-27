#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (GitHubUpdater.cs) is part of GithubUpdater.
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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using GithubUpdater.GitHub.Exceptions;
using GithubUpdater.Http;

namespace GithubUpdater.GitHub {

    /// <summary>
    /// A github updater.
    /// </summary>
    public class GitHubUpdater {

        private HttpRequest _httpRequest = new HttpRequest(@"https://api.github.com");
        private int _maxNumberOfReleasesToFetch;
        private string _repoOwner;
        private string _repoName;

        /// <summary>
        /// Sets the repo owner/name to use for this updater.
        /// </summary>
        /// <param name="repoOwner"></param>
        /// <param name="repoName"></param>
        /// <returns></returns>
        public GitHubUpdater SetRepo(string repoOwner, string repoName) {
            _repoOwner = repoOwner;
            _repoName = repoName;
            return this;
        }

        /// <summary>
        /// Only fetch a max number of releases when checking updates instead of getting everything. Allows lighter network traffic.
        /// </summary>
        /// <param name="maxNumberOfReleasesToFetch"></param>
        /// <returns></returns>
        public GitHubUpdater UseMaxNumberOfReleasesToFetch(int maxNumberOfReleasesToFetch) {
            _maxNumberOfReleasesToFetch = maxNumberOfReleasesToFetch;
            return this;
        }
        
        /// <summary>
        /// Use an http proxy for the update.
        /// </summary>
        /// <remarks>
        /// If you intend to use a locally served proxy, don't use 127.0.0.1 or localhost but use your machine name instead.
        /// That is because .net framework is hardcoded to not sent req for localhost through the proxy.
        /// </remarks>
        /// <param name="address">Can be null for a null proxy.</param>
        /// <param name="userName">domain\user. Can be null if no credentials are needed.</param>
        /// <param name="userPassword"></param>
        /// <param name="bypassProxyOnLocal"></param>
        /// <param name="sendProxyAuthorizationBeforeServerChallenge">Send the proxy-authorization header before the 407 challenge from the proxy server.</param>
        public GitHubUpdater UseProxy(string address, string userName = null, string userPassword = null, bool bypassProxyOnLocal = false, bool sendProxyAuthorizationBeforeServerChallenge = true) {
            _httpRequest.UseProxy(address, userName, userPassword, bypassProxyOnLocal, sendProxyAuthorizationBeforeServerChallenge);
            return this;
        }

        /// <summary>
        /// Use an OAuth token for authentication to the github api (user:token). Generate a token at https://github.com/settings/tokens.
        /// See https://developer.github.com/v3/ for more help.
        /// If you are using anonymous requests, mind the limits of the api.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public GitHubUpdater UseAuthorizationToken(string token) {
            _httpRequest.UseAuthorizationHeader(HttpAuthorizationType.Token, token);
            return this;
        }

        /// <summary>
        /// Sets a timeout for the http request. Defaults to infinite. You can also use <see cref="UseCancellationToken"/> for that purpose.
        /// </summary>
        /// <param name="timeOut"></param>
        /// <param name="readWriteTimeOut"></param>
        /// <returns></returns>
        public GitHubUpdater UseTimeout(int timeOut, int readWriteTimeOut) {
            _httpRequest.UseTimeout(timeOut, readWriteTimeOut);
            return this;
        }

        /// <summary>
        /// Sets the buffer size to use for requests/downloads.
        /// </summary>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public GitHubUpdater UseBufferSize(int bufferSize) {
            _httpRequest.UseBufferSize(bufferSize);
            return this;
        }
        
        /// <summary>
        /// Sets the base url for the request. Defaults to github api, change only if you host your own service.
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <returns></returns>
        public GitHubUpdater UseAlternativeBaseUrl(string baseUrl) {
            _httpRequest.UseBaseUrl(baseUrl);
            return this;
        }

        /// <summary>
        /// Sets a cancellation token for the request.
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public GitHubUpdater UseCancellationToken(CancellationToken? cancelToken) {
            _httpRequest.UseCancellationToken(cancelToken);
            return this;
        }

        /// <summary>
        /// Get the max number of releases to fetch for each update check.
        /// </summary>
        protected int MaxNumberOfReleasesToFetch => _maxNumberOfReleasesToFetch;
        
        /// <summary>
        /// The relative url path to use to request the releases list.
        /// </summary>
        protected virtual string GetReleaseApiRelativeUrl() => $"repos/{_repoOwner}/{_repoName}/releases{(_maxNumberOfReleasesToFetch > 0 ? $"?page=1&per_page={_maxNumberOfReleasesToFetch}" : "")}";

        /// <summary>
        /// Returns a list of new releases based on the given predicate. Can also sort the releases.
        /// </summary>
        /// <param name="isNewReleasePredicate"></param>
        /// <param name="orderBySelector"></param>
        /// <returns></returns>
        /// <exception cref="GithubFailedRequestException"></exception>
        public List<GitHubRelease> FetchNewReleases(Func<GitHubRelease, bool> isNewReleasePredicate, Func<GitHubRelease, object> orderBySelector = null) {
            _httpRequest.UseHeader("Accept", "application/vnd.github.v3+json");
            
            var res = _httpRequest.GetJson(GetReleaseApiRelativeUrl(), out List<GitHubRelease> releases);
            if (res.StatusCode == HttpStatusCode.Forbidden || res.StatusCode == HttpStatusCode.Unauthorized) {
                _httpRequest.UseAuthorizationHeader(HttpAuthorizationType.Token, "");
                res = _httpRequest.GetJson(GetReleaseApiRelativeUrl(), out releases);
            }
            if (!res.Success || res.StatusCode != HttpStatusCode.OK) {
                throw new GithubFailedRequestException($"Failed to get the expected response from github releases API: {res.StatusDescription}", res.Exception);
            }

            releases.RemoveAll(release => !isNewReleasePredicate(release));

            if (releases.Count > 0 && orderBySelector != null) {
                releases = releases.OrderByDescending(orderBySelector).ToList();
            }

            return releases;
        }
        
        /// <summary>
        /// Returns a list of new releases based on the local version versus the tag version of distant releases.
        /// The returned list is sorted by descending order, the first release being the most up-to-date.
        /// </summary>
        /// <param name="localVersion">See <see cref="UpdaterHelper.StringToVersion"/> to get the version from a string.</param>
        /// <returns></returns>
        public List<GitHubRelease> FetchNewReleases(Version localVersion) {
            return FetchNewReleases(release => UpdaterHelper.StringToVersion(release.TagName).CompareTo(localVersion) > 0, release => UpdaterHelper.StringToVersion(release.TagName));
        }

        /// <summary>
        /// Downloads a file to a temporary file and return its location.
        /// </summary>
        /// <param name="urlToDownload"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        /// <exception cref="GithubFailedRequestException"></exception>
        public string DownloadToTempFile(string urlToDownload, Action<DownloadProgress> progress = null) {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var res = _httpRequest.DownloadFile(urlToDownload, path, progress);
            if (!res.Success) {
                throw new GithubFailedRequestException($"Failed to download {urlToDownload} in {path}: {res.StatusDescription}", res.Exception);
            }
            return path;
        }

    }
}
