# Github Updater

Updating software through github releases.

[![logo](logo.png)](https://github.com/jcaillon/GithubUpdater)

## About

Basically, this library provides a simple way to:

- get new releases info posted on github
- download an asset or a zipball from the latest release
- update the currently running .exe/.dll when it exits

### Usage example

Check the project `DemoSelfUpdateApp` in the repository to see a working example.

```csharp
updater = new GitHubUpdater();
            
updater.SetRepo("jcaillon", "GithubUpdater");
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
```
