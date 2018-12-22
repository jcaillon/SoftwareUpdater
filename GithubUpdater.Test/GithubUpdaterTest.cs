#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (GithubUpdaterTest.cs) is part of GithubUpdater.Test.
// 
// GithubUpdater.Test is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GithubUpdater.Test is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GithubUpdater.Test. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using GithubUpdater.GitHub;
using GithubUpdater.Test.HttpUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GithubUpdater.Test {

    [TestClass]
    public class GithubUpdaterTest {

        private static string _testFolder;
        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(GithubUpdaterTest)));

        [ClassInitialize]
        public static void Init(TestContext context) {
            Cleanup();
            Directory.CreateDirectory(TestFolder);
        }

        [ClassCleanup]
        public static void Cleanup() {
            if (Directory.Exists(TestFolder)) {
                Directory.Delete(TestFolder, true);
            }
        }

        [TestMethod]
        public void Test() {
            // hostname to use
            // we need something different than 127.0.0.1 or localhost for the proxy!
            IPHostEntry hostEntry;
            try {
                hostEntry = Dns.GetHostEntry("mylocalhost");
            } catch (Exception) {
                hostEntry = null;
            }
            var host = hostEntry == null ? "127.0.0.1" : "mylocalhost";

            var baseDir = Path.Combine(TestFolder, "http");
            Directory.CreateDirectory(baseDir);
            
            var githubServer = new SimpleGithubServer(baseDir, "admin");
            var proxyServer = new SimpleHttpProxyServer("jucai69d", "julien caillon");
            
            githubServer.Releases = new List<GitHubRelease> {
                new GitHubRelease {
                    
                    CreatedAt = $"{DateTime.UtcNow:s}Z"
                }
            };
            
            var cts = new CancellationTokenSource();
            var task1 = HttpServer.ListenAsync(8084, cts.Token, githubServer.OnHttpRequest, true);
            var task2 = HttpServer.ListenAsync(8085, cts.Token, proxyServer.OnHttpRequest, true);
            
            // do
            githubServer.Releases = new List<GitHubRelease> {
                new GitHubRelease {
                    Name = "rel1",
                    TagName = "v1.0.1",
                    Prerelease = true,
                    ZipballUrl = "file.v1.0",
                    CreatedAt = $"{DateTime.UtcNow:s}Z",
                    Assets = new List<GitHubAsset> {
                        new GitHubAsset {
                            Name = "asset1"
                        },
                        new GitHubAsset {
                            Name = "asset2"
                        }
                    }
                },
                new GitHubRelease {
                    Name = "rel2",
                    TagName = "v1.1.0",
                    Prerelease = false
                },
                new GitHubRelease {
                    Name = "rel3",
                    TagName = "v1.2.1",
                    Prerelease = true
                },
                new GitHubRelease {
                    Name = "rel5",
                    TagName = "v3.0.0",
                    Prerelease = false
                },
                new GitHubRelease {
                    Name = "rel4",
                    TagName = "v2.0.0",
                    Prerelease = false
                }
            };
            
            var updater = new GitHubUpdater();
            updater.UseAuthorizationToken("admin");
            updater.UseAlternativeBaseUrl($"http://{host}:8084");
            updater.UseProxy($"http://{host}:8085/", "jucai69d", "julien caillon");
            updater.SetRepo("3pUser", "yolo");
            updater.UseMaxNumberOfReleasesToFetch(10);
            
            var releases = updater.FetchNewReleases(UpdaterHelper.StringToVersion("0"));
            Assert.AreEqual(5, releases.Count);
            Assert.AreEqual("rel5", releases[0].Name);
            
            releases = updater.FetchNewReleases(UpdaterHelper.StringToVersion("3"));
            Assert.AreEqual(0, releases.Count);
            
            releases = updater.FetchNewReleases(UpdaterHelper.StringToVersion("1.2"));
            Assert.AreEqual(3, releases.Count);
            Assert.AreEqual("rel5", releases[0].Name);
            
            File.WriteAllText(Path.Combine(baseDir, "testFile"), "cc");
            var countProgress = 0;
            var dlPath = updater.DownloadToTempFile("testFile", progress => countProgress++);
            
            Assert.IsTrue(countProgress > 0);
            Assert.IsTrue(File.Exists(dlPath));
            Assert.AreEqual(File.ReadAllText(Path.Combine(baseDir, "testFile")), File.ReadAllText(dlPath));
            
            File.Delete(dlPath);

            if (!host.Equals("127.0.0.1")) {
                Assert.IsTrue(proxyServer.NbRequestsHandledOk > 0);
            }

            HttpServer.Stop(cts, task1, task2);
        }
    }
}