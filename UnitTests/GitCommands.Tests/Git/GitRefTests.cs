﻿using GitCommands;
using GitUIPluginInterfaces;
using NSubstitute;
using NUnit.Framework;

namespace GitCommandsTests.Git
{
    public sealed class GitRefTests
    {
        [Test]
        public void IsTrackingRemote_should_return_true_when_tracking_remote()
        {
            string remoteBranchShortName = "remote_branch";
            string remoteName = "origin";
            GitRef localBranchRef = SetupLocalBranchWithATrackingReference(remoteBranchShortName, remoteName);

            GitRef remoteBranchRef = SetupRemoteRef(remoteBranchShortName, remoteName);

            Assert.IsTrue(localBranchRef.IsTrackingRemote(remoteBranchRef));
        }

        [Test]
        public void IsTrackingRemote_should_return_false_when_remote_is_null()
        {
            GitRef localBranchRef = SetupLocalBranchWithATrackingReference("remote_branch", "origin");

            Assert.IsFalse(localBranchRef.IsTrackingRemote(null));
        }

        [Test]
        public void IsTrackingRemote_should_return_false_when_tracking_another_remote()
        {
            string remoteBranchShortName = "remote_branch";
            GitRef localBranchRef = SetupLocalBranchWithATrackingReference(remoteBranchShortName, "origin");

            GitRef remoteBranchRef = SetupRemoteRef(remoteBranchShortName, "upstream");

            Assert.IsFalse(localBranchRef.IsTrackingRemote(remoteBranchRef));
        }

        [Test]
        public void IsTrackingRemote_should_return_false_when_tracking_another_remote_branch()
        {
            GitRef localBranchRef = SetupLocalBranchWithATrackingReference("one_remote_branch", "origin");

            GitRef remoteBranchRef = SetupRemoteRef("another_remote_branch", "origin");

            Assert.IsFalse(localBranchRef.IsTrackingRemote(remoteBranchRef));
        }

        [Test]
        public void IsTrackingRemote_should_return_false_when_supposedly_local_branch_is_a_remote_ref()
        {
            GitRef localBranchRef = SetupRemoteRef("a_remote_branch", "origin");

            GitRef remoteBranchRef = SetupRemoteRef("a_remote_branch", "origin");

            Assert.IsFalse(localBranchRef.IsTrackingRemote(remoteBranchRef));
        }

        [Test]
        public void IsTrackingRemote_should_return_false_when_supposedly_remote_branch_is_a_local_ref()
        {
            GitRef localBranchRef = SetupLocalBranchWithATrackingReference("a_remote_branch", "origin");

            GitRef remoteBranchRef = SetupLocalBranchWithATrackingReference("a_remote_branch", "origin");

            Assert.IsFalse(localBranchRef.IsTrackingRemote(remoteBranchRef));
        }

        [Test]
        public void IsTrackingRemote_should_return_false_when_local_branch_is_tracking_nothing()
        {
            var localGitModule = Substitute.For<IGitModule>();
            var localConfigFileSettings = Substitute.For<IConfigFileSettings>();
            localConfigFileSettings.GetValue($"branch.local_branch.merge").Returns(string.Empty);
            localConfigFileSettings.GetValue($"branch.local_branch.remote").Returns(string.Empty);
            localGitModule.LocalConfigFile.Returns(localConfigFileSettings);
            var localBranchRef = new GitRef(localGitModule, ObjectId.Random(), "refs/heads/local_branch");

            GitRef remoteBranchRef = SetupLocalBranchWithATrackingReference("a_remote_branch", "origin");

            Assert.IsFalse(localBranchRef.IsTrackingRemote(remoteBranchRef));
        }

        private static GitRef SetupRemoteRef(string remoteBranchShortName, string remoteName)
        {
            var remoteGitModule = Substitute.For<IGitModule>();
            var remoteConfigFileSettings = Substitute.For<IConfigFileSettings>();
            remoteGitModule.LocalConfigFile.Returns(remoteConfigFileSettings);
            var remoteBranchRef = new GitRef(remoteGitModule, ObjectId.Random(), $"refs/remotes/{remoteName}/{remoteBranchShortName}", remoteName);
            return remoteBranchRef;
        }

        private static GitRef SetupLocalBranchWithATrackingReference(string remoteShortName, string remoteName)
        {
            var localGitModule = Substitute.For<IGitModule>();
            var localConfigFileSettings = Substitute.For<IConfigFileSettings>();
            localConfigFileSettings.GetValue($"branch.local_branch.merge").Returns($"refs/heads/{remoteShortName}");
            localConfigFileSettings.GetValue($"branch.local_branch.remote").Returns(remoteName);
            localGitModule.LocalConfigFile.Returns(localConfigFileSettings);
            var localBranchRef = new GitRef(localGitModule, ObjectId.Random(), "refs/heads/local_branch");
            return localBranchRef;
        }
    }
}
