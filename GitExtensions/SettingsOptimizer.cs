using GitCommands;
using System;
using System.Collections.Generic;
using System.IO;

namespace GitExtensions
{
    internal static class SettingsOptimizer
    {
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GitExtensions");

        private static readonly string ConfigFilePath = Path.Combine(AppDataDir, "OptimizedSettings.txt");

        private static List<string> _optimizedItems;

        private static bool hasChange = false;

        public static void Optimize()
        {
            if (!Directory.Exists(AppDataDir))
            {
                Directory.CreateDirectory(AppDataDir);
            }
            if (File.Exists(ConfigFilePath))
            {
                _optimizedItems = new List<string>(File.ReadAllLines(ConfigFilePath));
            }
            else
            {
                _optimizedItems = new List<string>();
            }

            Optimize(nameof(AppSettings.UseConsoleEmulatorForCommands), false, x => AppSettings.UseConsoleEmulatorForCommands = x);
            Optimize(nameof(AppSettings.AlwaysShowAdvOpt), true, x => AppSettings.AlwaysShowAdvOpt = x);
            Optimize(nameof(AppSettings.CloseCommitDialogAfterCommit), true, x => AppSettings.CloseCommitDialogAfterCommit = x);
            Optimize(nameof(AppSettings.DontConfirmPushNewBranch), true, x => AppSettings.DontConfirmPushNewBranch = x);
            Optimize(nameof(AppSettings.CloseProcessDialog), true, x => AppSettings.CloseProcessDialog = x);
            Optimize(nameof(AppSettings.RememberIgnoreWhiteSpacePreference), true, x => AppSettings.RememberIgnoreWhiteSpacePreference = x);
            Optimize(nameof(AppSettings.DontConfirmCommitAfterConflictsResolved), true, x => AppSettings.DontConfirmCommitAfterConflictsResolved = x);
            Optimize(nameof(AppSettings.DontConfirmResolveConflicts), true, x => AppSettings.DontConfirmResolveConflicts = x);
            Optimize(nameof(AppSettings.DontConfirmSecondAbortConfirmation), true, x => AppSettings.DontConfirmSecondAbortConfirmation = x);
            Optimize(nameof(AppSettings.ConEmuStyle), "<Ubuntu>", x => AppSettings.ConEmuStyle.Value = x);

            if (hasChange)
            {
                AppSettings.SaveSettings();
                File.WriteAllLines(ConfigFilePath, _optimizedItems);
            }
        }

        private static void Optimize<T>(string name, T value, Action<T> action)
        {
            if (!_optimizedItems.Contains(name))
            {
                action(value);
                _optimizedItems.Add(name);
                hasChange = true;
            }
        }
    }
}
