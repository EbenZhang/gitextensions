using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Git.hub;
using GitCommands;
using GitCommands.Config;
using GitUIPluginInterfaces;
using JetBrains.Annotations;
using ResourceManager;
using RestSharp;

namespace GitUI.CommandsDialogs.BrowseDialog
{
    public partial class FormUpdates : GitExtensionsForm
    {
        #region Translation
        private readonly TranslationString _newVersionAvailable =
            new TranslationString("There is a new version {0} of Git Extensions available");
        private readonly TranslationString _noUpdatesFound =
            new TranslationString("No updates found");
        private readonly TranslationString _downloading =
            new TranslationString("Trying to download {0}, please wait...");
        #endregion

        public IWin32Window OwnerWindow;
        public Version CurrentVersion { get; }
        public bool UpdateFound;
        public string InstallerPath = "";
        private string _releasePageUrl = "";
        public string NewVersion = "";

        public FormUpdates(Version currentVersion)
        {
            CurrentVersion = currentVersion;

            InitializeComponent();
            InitializeComplete();

            progressBar1.Visible = true;
            progressBar1.Style = ProgressBarStyle.Marquee;
        }

        private void CloseButtonClick(object sender, EventArgs e)
        {
            Close();
        }

        public void SearchForUpdatesAndShow(IWin32Window ownerWindow, bool alwaysShow)
        {
            OwnerWindow = ownerWindow;
            new Thread(SearchForUpdates).Start();
            if (alwaysShow)
            {
                ShowDialog(ownerWindow);
            }
        }

        private class GitHubReleaseInfo
        {
            public string html_url { get; set; }
            public string tag_name { get; set; }
        }

        private GitHubReleaseInfo GetLatestGitExtensionsRelease()
        {
            var client = new RestClient("https://api.github.com");
            client.UserAgent = "mabako/Git.hub";

            var request = new RestRequest("/repos/EbenZhang/gitextensions/releases/latest");

            return client.Get<GitHubReleaseInfo>(request).Data;
        }

        private void SearchForUpdates()
        {
            try
            {
                var github = new Client();
                Repository gitExtRepo = github.getRepository("EbenZhang", "gitextensions");
                if (gitExtRepo == null)
                {
                    return;
                }

                var configData = GetLatestGitExtensionsRelease();
                if (configData == null)
                {
                    return;
                }

                CheckForNewerVersion(configData);
            }
            catch (InvalidAsynchronousStateException)
            {
                // InvalidAsynchronousStateException (The destination thread no longer exists) is thrown
                // if a UI component gets disposed or the UI thread EXITs while a 'check for updates' thread
                // is in the middle of its run... Ignore it, likely the user has closed the app
            }
            catch (Exception ex)
            {
                this.InvokeSync(() =>
                    {
                        if (Visible)
                        {
                            ExceptionUtils.ShowException(this, ex, string.Empty, true);
                        }
                    });
                Done();
            }
        }

        private void CheckForNewerVersion(GitHubReleaseInfo release)
        {
            Version newVersion = null;
            try
            {
                newVersion = new Version(release.tag_name);
            }
            catch
            {
                // ignored
            }

            if (newVersion == null)
            {
                UpdateFound = false;
            }
            else
            {
                UpdateFound = CurrentVersion < new Version(release.tag_name);
            }

            if (UpdateFound)
            {
                var setupFileName = $"GitExtensions-{release.tag_name}.msi";
                try
                {
                    var downloadToFolder = Path.Combine(Path.GetTempPath(), "GitExtensionReleases");
                    DeleteOldSetups(downloadToFolder);
                    if (!Directory.Exists(downloadToFolder))
                    {
                        Directory.CreateDirectory(downloadToFolder);
                    }

                    _releasePageUrl = release.html_url;
                    InstallerPath = Path.Combine(downloadToFolder, setupFileName);
                    this.InvokeAsync(() =>
                    {
                        if (!Visible)
                        {
                            return;
                        }

                        UpdateLabel.Text = string.Format(_downloading.Text, release.tag_name);
                    }).FileAndForget();
                    DownloadNewRelease(release, setupFileName);
                    NewVersion = release.tag_name;
                    Done();
                }
                catch
                { // fall back to the download link
                    InstallerPath = GetDownloadUrl(release, setupFileName);
                    Done();
                }
            }
            else
            {
                _releasePageUrl = "";
                InstallerPath = "";
                Done();
            }
        }

        private void DownloadNewRelease(GitHubReleaseInfo release, string setupFileName)
        {
            string url = GetDownloadUrl(release, setupFileName);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AllowAutoRedirect = true;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream dataStream = response.GetResponseStream();
                using (var fileStream = new FileStream(InstallerPath, FileMode.OpenOrCreate))
                {
                    dataStream.CopyTo(fileStream);
                }

                response.Close();
            }
        }

        private static string GetDownloadUrl(GitHubReleaseInfo release, string setupFileName)
        {
            const string downloadUrlFormat = "https://github.com/EbenZhang/gitextensions/releases/download/{0}/{1}";
            var url = string.Format(downloadUrlFormat, release.tag_name, setupFileName);
            return url;
        }

        private void DeleteOldSetups(string downloadToFolder)
        {
            try
            {
                if (Directory.Exists(downloadToFolder))
                {
                    Directory.Delete(downloadToFolder, recursive: true);
                }
            }
            catch
            {
                // don't care
            }
        }

        private void Done()
        {
            this.InvokeSync(() =>
            {
                progressBar1.Visible = false;

                if (UpdateFound)
                {
                    btnInstallNow.Enabled = true;
                    UpdateLabel.Text = string.Format(_newVersionAvailable.Text, NewVersion);
                    linkChangeLog.Visible = true;

                    if (!Visible)
                    {
                        ShowDialog(OwnerWindow);
                    }
                }
                else
                {
                    UpdateLabel.Text = _noUpdatesFound.Text;
                }
            });
        }

        private void linkChangeLog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(_releasePageUrl);
        }

        private void btnInstallNow_Click(object sender, EventArgs e)
        {
            if (InstallerPath.IsNotNullOrWhitespace())
            {
                using (Process.Start(InstallerPath))
                {
                    Application.Exit();
                }
            }
        }
    }

    public enum ReleaseType
    {
        Major,
        HotFix,
        ReleaseCandidate
    }

    public class ReleaseVersion
    {
        public Version Version;
        public ReleaseType ReleaseType;
        public string DownloadPage;

        [CanBeNull]
        public static ReleaseVersion FromSection(IConfigSection section)
        {
            Version ver;
            try
            {
                ver = new Version(section.SubSection);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }

            var version = new ReleaseVersion
            {
                Version = ver,
                ReleaseType = ReleaseType.Major,
                DownloadPage = section.GetValue("DownloadPage")
            };

            Enum.TryParse(section.GetValue("ReleaseType"), true, out version.ReleaseType);

            return version;
        }

        public static IEnumerable<ReleaseVersion> Parse(string versionsStr)
        {
            var cfg = new ConfigFile("", true);
            cfg.LoadFromString(versionsStr);
            var sections = cfg.GetConfigSections("Version");
            sections = sections.Concat(cfg.GetConfigSections("RCVersion"));

            return sections.Select(FromSection).Where(version => version != null);
        }

        public static IEnumerable<ReleaseVersion> GetNewerVersions(
            Version currentVersion,
            bool checkForReleaseCandidates,
            IEnumerable<ReleaseVersion> availableVersions)
        {
            var versions = availableVersions.Where(version =>
                    version.ReleaseType == ReleaseType.Major ||
                    version.ReleaseType == ReleaseType.HotFix ||
                    (checkForReleaseCandidates && version.ReleaseType == ReleaseType.ReleaseCandidate));

            return versions.Where(version => version.Version.CompareTo(currentVersion) > 0);
        }
    }
}