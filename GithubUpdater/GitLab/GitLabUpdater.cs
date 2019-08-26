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
using GithubUpdater.GitLab.Exceptions;
using GithubUpdater.Http;

namespace GithubUpdater.GitLab {

    /// <summary>
    /// A gitlab updater.
    /// </summary>
    public class GitLabUpdater {

        private readonly HttpRequest _httpRequest;
        private int _maxNumberOfTagsToFetch;
        private string _projectId;

        /// <summary>
        /// Get new instance.
        /// </summary>
        /// <param name="gitLabBaseUrl">The base url to the gitlab API (e.g. http://gitlab.example.com/api/v4)</param>
        public GitLabUpdater(string gitLabBaseUrl) {
            _httpRequest = new HttpRequest(gitLabBaseUrl);
        }

        /// <summary>
        /// Sets the projectId to use for this updater.
        /// </summary>
        /// <param name="projectId">e.g. myself/myproject</param>
        /// <returns></returns>
        public GitLabUpdater SetProjectId(string projectId) {
            _projectId = WebUtility.UrlEncode(projectId);
            return this;
        }

        /// <summary>
        /// Only fetch a max number of tags when checking updates instead of getting everything. Allows lighter network traffic.
        /// </summary>
        /// <param name="maxNumberOfTagsToFetch"></param>
        /// <returns></returns>
        public GitLabUpdater UseMaxNumberOfTagsToFetch(int maxNumberOfTagsToFetch) {
            _maxNumberOfTagsToFetch = maxNumberOfTagsToFetch;
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
        public GitLabUpdater UseProxy(string address, string userName = null, string userPassword = null, bool bypassProxyOnLocal = false, bool sendProxyAuthorizationBeforeServerChallenge = true) {
            _httpRequest.UseProxy(address, userName, userPassword, bypassProxyOnLocal, sendProxyAuthorizationBeforeServerChallenge);
            return this;
        }

        /// <summary>
        /// Use a private token for authentication to the gitlab api. Generate a token at /profile/personal_access_tokens.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public GitLabUpdater UsePrivateToken(string token) {
            _httpRequest.UseHeader("PRIVATE-TOKEN", token);
            return this;
        }

        /// <summary>
        /// Sets a timeout for the http request. Defaults to infinite. You can also use <see cref="UseCancellationToken"/> for that purpose.
        /// </summary>
        /// <param name="timeOut"></param>
        /// <param name="readWriteTimeOut"></param>
        /// <returns></returns>
        public GitLabUpdater UseTimeout(int timeOut, int readWriteTimeOut) {
            _httpRequest.UseTimeout(timeOut, readWriteTimeOut);
            return this;
        }

        /// <summary>
        /// Sets the buffer size to use for requests/downloads.
        /// </summary>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public GitLabUpdater UseBufferSize(int bufferSize) {
            _httpRequest.UseBufferSize(bufferSize);
            return this;
        }

        /// <summary>
        /// Sets a cancellation token for the request.
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public GitLabUpdater UseCancellationToken(CancellationToken? cancelToken) {
            _httpRequest.UseCancellationToken(cancelToken);
            return this;
        }

        /// <summary>
        /// Get the max number of releases to fetch for each update check.
        /// </summary>
        protected int MaxNumberOfTagsToFetch => _maxNumberOfTagsToFetch;

        /// <summary>
        /// The relative url path to use to request the releases list.
        /// </summary>
        protected virtual string GetTagsApiRelativeUrl() => $"projects/{_projectId}/repository/tags?order_by=updated&sort=desc{(_maxNumberOfTagsToFetch > 0 ? $"&page=1&per_page={_maxNumberOfTagsToFetch}" : "")}";

        /// <summary>
        /// The relative url path to use to download a particular sha1 of the repository as an archive.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetRepositoryArchiveApiRelativeUrl(string referenceOrSha1) => $"projects/{_projectId}/repository/archive.zip?sha={referenceOrSha1}";

        /// <summary>
        /// Returns a list of new releases based on the given predicate. Can also sort the releases.
        /// </summary>
        /// <param name="isNewReleasePredicate"></param>
        /// <param name="orderBySelector"></param>
        /// <returns></returns>
        /// <exception cref="GitLabFailedRequestException"></exception>
        public List<GitLabTag> FetchNewReleases(Func<GitLabTag, bool> isNewReleasePredicate, Func<GitLabTag, object> orderBySelector = null) {
            var res = _httpRequest.GetJson(GetTagsApiRelativeUrl(), out List<GitLabTag> releases);
            if (!res.Success || res.StatusCode != HttpStatusCode.OK) {
                throw new GitLabFailedRequestException($"Failed to get the expected response from gitlab tags API: {res.StatusDescription}", res.Exception);
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
        public List<GitLabTag> FetchNewReleases(Version localVersion) {
            return FetchNewReleases(release => UpdaterHelper.StringToVersion(release.TagName).CompareTo(localVersion) > 0, release => UpdaterHelper.StringToVersion(release.TagName));
        }

        /// <summary>
        /// Downloads a file to a temporary file and return its location.
        /// </summary>
        /// <param name="urlToDownload"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        /// <exception cref="GitLabFailedRequestException"></exception>
        public string DownloadToTempFile(string urlToDownload, Action<DownloadProgress> progress = null) {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var res = _httpRequest.DownloadFile(urlToDownload, path, progress);
            if (!res.Success) {
                throw new GitLabFailedRequestException($"Failed to download {urlToDownload} in {path}: {res.StatusDescription}", res.Exception);
            }
            return path;
        }

        /// <summary>
        /// Downloads the zipped repository for a particular sha1/reference.
        /// </summary>
        /// <param name="referenceOrSha1"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        /// <exception cref="GitLabFailedRequestException"></exception>
        public string DownloadRepositoryArchive(string referenceOrSha1, Action<DownloadProgress> progress = null) {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var res = _httpRequest.DownloadFile(GetRepositoryArchiveApiRelativeUrl(referenceOrSha1), path, progress);
            if (!res.Success) {
                throw new GitLabFailedRequestException($"Failed to download the zipped repository for sha {referenceOrSha1} in {path}: {res.StatusDescription}", res.Exception);
            }
            return path;
        }

    }
}
