namespace GitCommands.Git
{
    public interface IGitRevisionProvider
    {
        GitRevision GetRevision(string commit, bool shortFormat = false, bool loadRefs = false);
        bool IsExistingCommitHash(string sha1Fragment, out string fullSha1);
    }
}