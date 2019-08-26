using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SoftwareUpdater;
using SoftwareUpdater.GitHub;
using SoftwareUpdater.Test.HttpUtils;

namespace DemoSelfUpdateApp {

    public static class Program {

        public static void Main(string[] args) {

            var host = "127.0.0.1";
            var port = 8084;

            var cts = new CancellationTokenSource();
            var task = StartFakeGithubServer(host, port, cts.Token);

            var updater = new GitHubUpdater();
            updater.UseAlternativeBaseUrl($"http://{host}:{port}");

            SelfUpdate(updater);

            HttpServer.Stop(cts, task);
        }

        private static void SelfUpdate(GitHubUpdater updater) {

            // var updater = new SoftwareUpdater();

            updater.SetRepo("jcaillon", "SoftwareUpdater");
            updater.UseCancellationToken(new CancellationTokenSource(3000).Token);
            updater.UseMaxNumberOfReleasesToFetch(10);

            var currentVersion = UpdaterHelper.StringToVersion(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);
            Console.WriteLine($"Our current version is: {currentVersion}.");

            var releases = updater.FetchNewReleases(currentVersion);
            Console.WriteLine($"We found {releases.Count} new releases on github.");
            Console.WriteLine($"The latest release if {releases[0].Name}.");

            Console.WriteLine($"Downloading the latest release asset: {releases[0].Assets[0].BrowserDownloadUrl}.");
            var tempFilePath = updater.DownloadToTempFile(releases[0].Assets[0].BrowserDownloadUrl, progress => {
                Console.WriteLine($"Downloading... {progress.NumberOfBytesDoneTotal} / {progress.NumberOfBytesTotal} bytes.");
            });

            var fileUpdater = SimpleFileUpdater.Instance;
            Console.WriteLine("We will replace this .exe with the one on the github release after this program has exited.");
            fileUpdater.AddFileToMove(tempFilePath, Assembly.GetExecutingAssembly().Location);
            fileUpdater.Start();

        }

        private static Task StartFakeGithubServer(string host, int port, CancellationToken token) {
            var baseDir = Path.Combine(AppContext.BaseDirectory, "github_server");
            Directory.CreateDirectory(baseDir);

            File.WriteAllText(Path.Combine(baseDir, "DemoSelfUpdateApp.exe"), "fake exe content!");

            var githubServer = new SimpleGithubServer(baseDir, null) {
                Releases = new List<GitHubRelease> {
                    new GitHubRelease {
                        Name = "rel1",
                        TagName = "v1.0.1",
                        Prerelease = true,
                        Assets = new List<GitHubAsset> {
                            new GitHubAsset {
                                Name = "DemoSelfUpdateApp.exe",
                                BrowserDownloadUrl = $"http://{host}:{port}/DemoSelfUpdateApp.exe"
                            }
                        }
                    },
                    new GitHubRelease {
                        Name = "rel2",
                        TagName = "v1.1.0",
                        Prerelease = false,
                        Assets = new List<GitHubAsset> {
                            new GitHubAsset {
                                Name = "DemoSelfUpdateApp.exe",
                                BrowserDownloadUrl = $"http://{host}:{port}/DemoSelfUpdateApp.exe"
                            }
                        }
                    },
                    new GitHubRelease {
                        Name = "rel3",
                        TagName = "v1.2.1",
                        Prerelease = true,
                        Assets = new List<GitHubAsset> {
                            new GitHubAsset {
                                Name = "DemoSelfUpdateApp.exe",
                                BrowserDownloadUrl = $"http://{host}:{port}/DemoSelfUpdateApp.exe"
                            }
                        }
                    },
                    new GitHubRelease {
                        Name = "rel4",
                        TagName = "v2.0.0",
                        Prerelease = false,
                        Assets = new List<GitHubAsset> {
                            new GitHubAsset {
                                Name = "DemoSelfUpdateApp.exe",
                                BrowserDownloadUrl = $"http://{host}:{port}/DemoSelfUpdateApp.exe"
                            }
                        }
                    },
                    new GitHubRelease {
                        Name = "rel5",
                        TagName = "v3.0.0",
                        Prerelease = false,
                        Assets = new List<GitHubAsset> {
                            new GitHubAsset {
                                Name = "DemoSelfUpdateApp.exe",
                                BrowserDownloadUrl = $"http://{host}:{port}/DemoSelfUpdateApp.exe"
                            }
                        }
                    }
                }
            };

            return HttpServer.ListenAsync(port, token, githubServer.OnHttpRequest, true);
        }
    }
}
