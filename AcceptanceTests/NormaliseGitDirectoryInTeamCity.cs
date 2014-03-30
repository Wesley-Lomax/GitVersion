﻿namespace AcceptanceTests
{
    using System;
    using GitHubFlowVersion.AcceptanceTests;
    using GitHubFlowVersion.AcceptanceTests.Helpers;
    using GitVersion;
    using LibGit2Sharp;
    using Shouldly;
    using Xunit.Extensions;

    public class NormaliseGitDirectoryInTeamCity
    {
        private const string TaggedVersion = "1.0.3";

        [Theory]
        //TODO Stash support [InlineData("refs/pull-requests/5/merge-clean")]
        [InlineData("refs/pull/5/merge")]
        public void GivenARemoteWithATagOnMaster_AndAPullRequestWithTwoCommits_AndBuildIsRunningInTeamCity_VersionIsCalclatedProperly(string pullRequestRef)
        {
            using (var fixture = new RepositoryFixture())
            {
                var remoteRepositoryPath = PathHelper.GetTempPath();
                Repository.Init(remoteRepositoryPath);
                var remoteRepository = new Repository(remoteRepositoryPath);
                remoteRepository.Config.Set("user.name", "Test");
                remoteRepository.Config.Set("user.email", "test@email.com");
                fixture.Repository.Network.Remotes.Add("origin", remoteRepositoryPath);
                Console.WriteLine("Created git repository at {0}", remoteRepositoryPath);
                remoteRepository.MakeATaggedCommit(TaggedVersion);

                var branch = remoteRepository.CreateBranch("FeatureBranch");
                remoteRepository.Checkout(branch);
                remoteRepository.MakeCommits(2);
                remoteRepository.Checkout(remoteRepository.Head.Tip.Sha);
                //Emulate merge commit
                var mergeCommitSha = remoteRepository.MakeACommit().Sha;
                remoteRepository.Checkout("master"); // HEAD cannot be pointing at the merge commit
                remoteRepository.Refs.Add(pullRequestRef, new ObjectId(mergeCommitSha));

                // Checkout PR commit
                fixture.Repository.Fetch("origin");
                fixture.Repository.Checkout(mergeCommitSha);

                var result = GitVersionHelper.ExecuteIn(fixture.RepositoryPath, isTeamCity: true);

                result.ExitCode.ShouldBe(0);
                result.Output[VariableProvider.FullSemVer].ShouldBe("1.0.4-PullRequest.5+3");
            }
        }
    }
}
