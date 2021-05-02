﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using GitCommands;
using GitCommands.Config;
using GitUI.CommandsDialogs.FormatPatchDialog;
using GitUIPluginInterfaces;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using ResourceManager;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace GitUI.CommandsDialogs
{
    public partial class FormFormatPatch : GitModuleForm
    {
        private readonly TranslationString _currentBranchText = new("Current branch:");
        private readonly TranslationString _noOutputPathEnteredText =
            new("You need to enter an output path.");
        private readonly TranslationString _noEmailEnteredText =
            new("You need to enter an email address.");
        private readonly TranslationString _noSubjectEnteredText =
            new("You need to enter a mail subject.");
        private readonly TranslationString _wrongSmtpSettingsText =
            new("You need to enter a valid smtp in the settings dialog.");
        private readonly TranslationString _revisionsNeededText =
            new("You need to select at least one revision");
        private readonly TranslationString _revisionsNeededCaption =
            new("Patch error");
        private readonly TranslationString _sendMailResult =
            new("Send to:");
        private readonly TranslationString _sendMailResultFailed =
            new("Failed to send mail.");
        private readonly TranslationString _patchResultCaption =
            new("Patch result");
        private readonly TranslationString _noGitMailConfigured =
            new("There is no email address configured in the settings dialog.");
        private readonly TranslationString _failCreatePatch =
            new("Unable to create patch file(s)");

        [Obsolete("For VS designer and translation test only. Do not remove.")]
        private FormFormatPatch()
        {
            InitializeComponent();
        }

        public FormFormatPatch(GitUICommands commands)
            : base(commands)
        {
            InitializeComponent();
            RevisionGrid.ShowUncommittedChangesIfPossible = false;
            InitializeComplete();

            MailFrom.Text = Module.GetEffectiveSetting(SettingKeyString.UserEmail);
        }

        private void Browse_Click(object sender, EventArgs e)
        {
            var userSelectedPath = OsShellUtil.PickFolder(this);

            if (userSelectedPath is not null)
            {
                OutputPath.Text = userSelectedPath;
            }
        }

        private void FormFormatPath_Load(object sender, EventArgs e)
        {
            OutputPath.Text = AppSettings.LastFormatPatchDir;
            string selectedHead = Module.GetSelectedBranch();
            SelectedBranch.Text = _currentBranchText.Text + " " + selectedHead;

            SaveToDir_CheckedChanged(this, EventArgs.Empty);
            OutputPath.TextChanged += OutputPath_TextChanged;
            RevisionGrid.Load();
        }

        private void OutputPath_TextChanged(object sender, EventArgs e)
        {
            if (Directory.Exists(OutputPath.Text))
            {
                AppSettings.LastFormatPatchDir = OutputPath.Text;
            }
        }

        private void FormatPatch_Click(object sender, EventArgs e)
        {
            if (SaveToDir.Checked && string.IsNullOrEmpty(OutputPath.Text))
            {
                MessageBox.Show(this, _noOutputPathEnteredText.Text, TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!SaveToDir.Checked && string.IsNullOrEmpty(MailTo.Text))
            {
                MessageBox.Show(this, _noEmailEnteredText.Text, TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!SaveToDir.Checked && string.IsNullOrEmpty(MailSubject.Text))
            {
                MessageBox.Show(this, _noSubjectEnteredText.Text, TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!SaveToDir.Checked && string.IsNullOrEmpty(AppSettings.SmtpServer))
            {
                MessageBox.Show(this, _wrongSmtpSettingsText.Text, TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string savePatchesToDir = OutputPath.Text;

            if (!SaveToDir.Checked)
            {
                savePatchesToDir = Path.Combine(Module.WorkingDirGitDir, "PatchesToMail");
                if (Directory.Exists(savePatchesToDir))
                {
                    foreach (string file in Directory.GetFiles(savePatchesToDir, "*.patch"))
                    {
                        File.Delete(file);
                    }
                }
                else
                {
                    Directory.CreateDirectory(savePatchesToDir);
                }
            }

            string rev1 = "";
            string rev2 = "";
            string result = "";

            var revisions = RevisionGrid.GetSelectedRevisions(SortDirection.Descending);
            if (revisions.Count > 0)
            {
                if (revisions.Count == 1)
                {
                    var parents = revisions[0].ParentIds;
                    rev1 = parents?.Count > 0 ? parents[0].ToString() : "";
                    rev2 = revisions[0].Guid;
                    result = Module.FormatPatch(rev1, rev2, savePatchesToDir);
                }
                else if (revisions.Count == 2)
                {
                    var parents = revisions[0].ParentIds;
                    rev1 = parents?.Count > 0 ? parents[0].ToString() : "";
                    rev2 = revisions[1].Guid;
                    result = Module.FormatPatch(rev1, rev2, savePatchesToDir);
                }
                else
                {
                    int n = 0;
                    foreach (GitRevision revision in revisions)
                    {
                        n++;
                        var parents = revision.ParentIds;
                        rev1 = parents?.Count > 0 ? parents[0].ToString() : "";
                        rev2 = revision.Guid;
                        result += Module.FormatPatch(rev1, rev2, savePatchesToDir, n);
                    }
                }
            }
            else
            {
                MessageBox.Show(this, _revisionsNeededText.Text, _revisionsNeededCaption.Text,  MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!SaveToDir.Checked)
            {
                result += Environment.NewLine + Environment.NewLine;
                if (SendMail(savePatchesToDir))
                {
                    result += _sendMailResult.Text + " " + MailTo.Text;
                }
                else
                {
                    result += _sendMailResultFailed.Text;
                }

                // Clean up
                if (Directory.Exists(savePatchesToDir))
                {
                    foreach (string file in Directory.GetFiles(savePatchesToDir, "*.patch"))
                    {
                        File.Delete(file);
                    }
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                MessageBox.Show(this, _failCreatePatch.Text, _revisionsNeededCaption.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(this, result, _patchResultCaption.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
        }

        private bool SendMail(string dir)
        {
            var attachmentStreams = new List<Stream>();
            try
            {
                string from = MailFrom.Text;

                if (string.IsNullOrEmpty(from))
                {
                    MessageBox.Show(this, _noGitMailConfigured.Text, TranslatedStrings.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                string to = MailTo.Text;

                var mail = new MimeMessage();
                mail.From.Add(new MailboxAddress(from, from));
                mail.To.Add(new MailboxAddress(to, to));
                mail.Subject = MailSubject.Text;
                var bodyPart = new TextPart(TextFormat.Plain)
                {
                    Text = MailBody.Text
                };
                var body = new Multipart
                {
                    bodyPart
                };

                foreach (string file in Directory.GetFiles(dir, "*.patch"))
                {
                    var stream = File.OpenRead(file);
                    attachmentStreams.Add(stream);
                    var attachment = new MimePart
                    {
                        Content = new MimeContent(stream),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = Path.GetFileName(file)
                    };
                    body.Add(attachment);
                }

                mail.Body = body;
                using var smtpClient = new SmtpClient();
                smtpClient.Connect(AppSettings.SmtpServer, AppSettings.SmtpPort,
                    AppSettings.SmtpUseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None);

                using (var credentials = new SmtpCredentials())
                {
                    credentials.login.Text = from;

                    var cred = credentials.ShowDialog(this) == DialogResult.OK
                        ? new NetworkCredential(credentials.login.Text, credentials.password.Text)
                        : CredentialCache.DefaultNetworkCredentials;
                    smtpClient.Authenticate(cred);
                }

                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                smtpClient.Send(mail);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                attachmentStreams.ForEach(x => x.Dispose());
            }

            return true;
        }

        private void SaveToDir_CheckedChanged(object sender, EventArgs e)
        {
            OutputPath.Enabled = SaveToDir.Checked;
            MailFrom.Enabled = !SaveToDir.Checked;
            MailTo.Enabled = !SaveToDir.Checked;
            MailSubject.Enabled = !SaveToDir.Checked;
            MailBody.Enabled = !SaveToDir.Checked;
        }
    }
}
