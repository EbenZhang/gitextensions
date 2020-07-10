using GitUIPluginInterfaces;
using JetBrains.Annotations;

namespace GitCommands.Git
{
    public interface IGitRevisionProvider
    {
        GitRevision GetRevision([CanBeNull] ObjectId commit = null,
            bool shortFormat = false, bool loadRefs = false);
    }
}
