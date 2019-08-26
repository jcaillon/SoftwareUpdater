#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (SoftwareUpdaterTest.cs) is part of SoftwareUpdater.Test.
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
using System.Net;
using System.Threading;
using SoftwareUpdater.GitHub;
using SoftwareUpdater.GitLab;
using SoftwareUpdater.Test.HttpUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SoftwareUpdater.Test {

    [TestClass]
    public class GitLabUpdaterTest {

        private static string _testFolder;
        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(GitLabUpdaterTest)));

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

            var gitLabServer = new SimpleGitLabServer(baseDir, "admin");
            var proxyServer = new SimpleHttpProxyServer("jucai69d", "julien caillon");

            var cts = new CancellationTokenSource();
            var task1 = HttpServer.ListenAsync(8086, cts.Token, gitLabServer.OnHttpRequest, true);
            var task2 = HttpServer.ListenAsync(8087, cts.Token, proxyServer.OnHttpRequest, true);

            // do
            gitLabServer.Tags = new List<GitLabTag> {
                new GitLabTag {
                    TagName = "v1.0.1-beta",
                    TagMessage = "rel1",
                    TagSha1 = "1",
                    Release = new GitLabRelease {
                        TagName = "",
                        Description = "Description"
                    }
                },
                new GitLabTag {
                    TagName = "v1.1.0",
                    TagMessage = "rel2",
                    TagSha1 = "2"
                },
                new GitLabTag {
                    TagName = "v1.2.1-beta",
                    TagMessage = "rel3",
                    TagSha1 = "3"
                },
                new GitLabTag {
                    TagName = "v3.0.0",
                    TagMessage = "rel5",
                    TagSha1 = "5"
                },
                new GitLabTag {
                    TagName = "v2.0.0",
                    TagMessage = "rel4",
                    TagSha1 = "4"
                }
            };

            var updater = new GitLabUpdater($"http://{host}:8086");
            updater.UsePrivateToken("admin");
            updater.UseProxy($"http://{host}:8087/", "jucai69d", "julien caillon");
            updater.SetProjectId("test/truc");
            updater.UseMaxNumberOfTagsToFetch(10);

            var tags = updater.FetchNewReleases(UpdaterHelper.StringToVersion("0"));
            Assert.AreEqual(5, tags.Count);
            Assert.AreEqual("rel5", tags[0].TagMessage);

            tags = updater.FetchNewReleases(UpdaterHelper.StringToVersion("3"));
            Assert.AreEqual(0, tags.Count);

            tags = updater.FetchNewReleases(UpdaterHelper.StringToVersion("1.2"));
            Assert.AreEqual(3, tags.Count);
            Assert.AreEqual("rel5", tags[0].TagMessage);

            File.WriteAllText(Path.Combine(baseDir, "testFile"), "cc");
            var countProgress = 0;
            var dlPath = updater.DownloadToTempFile("testFile", progress => countProgress++);

            Assert.IsTrue(countProgress > 0);
            Assert.IsTrue(File.Exists(dlPath));
            Assert.AreEqual(File.ReadAllText(Path.Combine(baseDir, "testFile")), File.ReadAllText(dlPath));

            File.Delete(dlPath);

            countProgress = 0;
            dlPath = updater.DownloadRepositoryArchive("mysha1", progress => countProgress++);
            Assert.IsTrue(countProgress > 0);
            Assert.IsTrue(File.Exists(dlPath));
            Assert.AreEqual(new FileInfo(dlPath).Length, 10);

            File.Delete(dlPath);

            if (!host.Equals("127.0.0.1")) {
                Assert.IsTrue(proxyServer.NbRequestsHandledOk > 0);
            }

            HttpServer.Stop(cts, task1, task2);
        }
    }
}
