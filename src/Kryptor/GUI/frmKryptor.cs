﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

/*  
    Kryptor: Free and open source file encryption software.
    Copyright(C) 2020 Samuel Lucas

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program. If not, see https://www.gnu.org/licenses/.
*/

namespace Kryptor
{
    public partial class frmKryptor : Form
    {
        public frmKryptor()
        {
            InitializeComponent();
        }

        private void Kryptor_Load(object sender, EventArgs e)
        {
            this.DragEnter += new DragEventHandler(FileDragEnter);
            this.DragDrop += new DragEventHandler(FileDragDrop);
            // Use system render mode on Linux & macOS
            MenuStripRenderMode();
            // Fix label alignment on Linux & macOS
            MonoGUI.AlignLabels(lblDragDrop, lblPassword, lblEntropy, null);
            // Check whether Kryptor has been opened by clicking on an encrypted file
            CheckForSelectedFiles();
            LoadSettings();
            CheckForUpdatesAtStartup();
        }

        private void FileDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void FileDragDrop(object sender, DragEventArgs e)
        {
            string[] selectedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            Globals.SetSelectedFiles(selectedFiles.ToList());
            picFilesSelected.BackColor = Color.LawnGreen;
        }

        private void MenuStripRenderMode()
        {
            if (Constants.RunningOnMono == true)
            {
                msMenus.RenderMode = ToolStripRenderMode.System;
            }
        }

        private void CheckForSelectedFiles()
        {
            if (Globals.GetSelectedFiles() != null)
            {
                picFilesSelected.BackColor = Color.LawnGreen;
            }
        }

        private void LoadSettings()
        {
            Settings.ReadSettings();
            chkShowPassword.Checked = Globals.ShowPasswordByDefault;
            tmrClearClipboard.Interval = Globals.ClearClipboardInterval;
            if (Globals.DarkTheme == true)
            {
                ApplyDarkTheme();
            }
        }

        private static void CheckForUpdatesAtStartup()
        {
            if (Globals.CheckForUpdates == true)
            {
                bool displayUpToDate = false;
                Updates.UpdateKryptor(displayUpToDate);
            }
        }

        private void ShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            if (chkShowPassword.Checked == true)
            {
                txtPassword.UseSystemPasswordChar = false;
            }
            else if (chkShowPassword.Checked == false)
            {
                txtPassword.UseSystemPasswordChar = true;
            }
        }

        private void tsmiCopyPassword_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPassword.Text))
            {
                EditClipboard.SetClipboard(txtPassword.Text);
                EditClipboard.AutoClearClipboard();
            }
        }

        private void tsmiClearPassword_Click(object sender, EventArgs e)
        {
            txtPassword.Clear();
        }

        private void tsmiClearClipboard_Click(object sender, EventArgs e)
        {
            EditClipboard.ClearClipboard();
        }

        private void tsmiClearSelectedFiles_Click(object sender, EventArgs e)
        {
            ClearSelectedFiles();
        }

        private void tmrClearClipboard_Tick(object sender, EventArgs e)
        {
            EditClipboard.ClearClipboard();
            tmrClearClipboard.Stop();
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            PasswordEvaluation.DisplayPasswordEntropy(txtPassword.Text, lblEntropy);
        }

        private void btnEncrypt_Click(object sender, EventArgs e)
        {
            lblPassword.Focus();
            bool encryption = true;
            GetPasswordInput(encryption);
        }

        private void GetPasswordInput(bool encryption)
        {
            char[] password = txtPassword.Text.ToCharArray();
            ValidateUserInput(encryption, password);
        }

        private void ValidateUserInput(bool encryption, char[] password)
        {
            if (!bgwEncryption.IsBusy & !bgwDecryption.IsBusy & !bgwShredFiles.IsBusy)
            {
                if (Globals.GetSelectedFiles() != null)
                {
                    if (password.Length >= 8 | !string.IsNullOrEmpty(Globals.KeyfilePath))
                    {
                        GetPasswordBytes(encryption, password);
                    }
                    else
                    {
                        DisplayMessage.InformationMessageBox("Please enter a password (8+ characters long) or select/generate a keyfile.");
                    }
                }
                else
                {
                    DisplayMessage.InformationMessageBox("Please select a file or folder to encrypt.");
                }
            }
            else
            {
                DisplayMessage.InformationMessageBox("Please wait for the current operation to finish.");
            }
        }

        private void GetPasswordBytes(bool encryption, char[] password)
        {
            byte[] passwordBytes = FileEncryption.GetPasswordBytes(password);
            Utilities.ZeroArray(password);
            if (passwordBytes != null)
            {
                ClearPasswordTextbox();
                StartBackgroundWorker(encryption, passwordBytes);
            }
            else
            {
                DisplayMessage.ErrorMessageBox("Failed to get password bytes. The encryption/decryption operation has been cancelled.");
            }
        }

        private void ClearPasswordTextbox()
        {
            if (Globals.AutoClearPassword == true)
            {
                txtPassword.Clear();
            }
        }

        private void StartBackgroundWorker(bool encryption, byte[] passwordBytes)
        {
            BeforeBackgroundWorkerStarts();
            if (encryption == true)
            {
                bgwEncryption.RunWorkerAsync(passwordBytes);
            }
            else
            {
                bgwDecryption.RunWorkerAsync(passwordBytes);
            }
        }

        private void BeforeBackgroundWorkerStarts()
        {
            prgProgress.Visible = true;
            DisableMenus();
        }

        private void DisableMenus()
        {
            // Prevent the user altering settings, etc whilst encryption/decryption is running
            tsmiFile.Enabled = false;
            tsmiTools.Enabled = false;
            cmsClearFilesMenu.Enabled = false;
        }

        private void bgwEncryption_DoWork(object sender, DoWorkEventArgs e)
        {
            bool encryption = true;
            byte[] passwordBytes = (byte[])e.Argument;
            CallFileEncryption(encryption, passwordBytes, bgwEncryption);
        }

        private static void CallFileEncryption(bool encryption, byte[] passwordBytes, BackgroundWorker backgroundWorker)
        {
            // Don't use keyfile bytes when only a keyfile is selected
            byte[] keyfileBytes = null;
            if (passwordBytes.Length != Constants.HMACKeySize)
            {
                keyfileBytes = FileEncryption.GetKeyfileBytes(ref passwordBytes);
            }
            FileEncryption.GetFilePaths(encryption, passwordBytes, keyfileBytes, backgroundWorker);
            Utilities.ZeroArray(passwordBytes);
            Utilities.ZeroArray(keyfileBytes);
        }

        private void bgwEncryption_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            UpdateProgressBar(e.ProgressPercentage);
        }

        private void UpdateProgressBar(int progressPercentage)
        {
            try
            {
                // This solution fixes some update lag
                prgProgress.Value = progressPercentage;
                prgProgress.Value = progressPercentage - 1;
                prgProgress.Value = progressPercentage;
            }
            catch (ArgumentException ex)
            {
                Logging.LogException(ex.ToString(), Logging.Severity.Bug);
                DisplayMessage.ErrorResultsText(string.Empty, ex.GetType().Name, "Invalid progress bar value. This is a bug - please report it.");
            }
        }

        private void bgwEncryption_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            const string outputMessage = "encrypted";
            FileEncryptionCompleted(outputMessage);
        }

        private void FileEncryptionCompleted(string outputMessage)
        {
            ClearSelectedFiles();
            // Deallocate Argon2 memory
            GC.Collect();
            BackgroundWorkerCompleted(outputMessage);
        }

        private void ClearSelectedFiles()
        {
            if (Globals.SuccessfulCount == Globals.TotalCount)
            {
                Globals.SetSelectedFiles(null);
                tsmiSelectFiles.Checked = false;
                tsmiSelectFolder.Checked = false;
                picFilesSelected.BackColor = Color.Red;
                Globals.KeyfilePath = null;
                tsmiSelectKeyfile.Checked = false;
            }
        }

        private void BackgroundWorkerCompleted(string outputMessage)
        {
            EnableMenus();
            prgProgress.Visible = false;
            prgProgress.Value = 0;
            Globals.ResultsText += Environment.NewLine + $"Successfully {outputMessage}: {Invariant.ToString(Globals.SuccessfulCount)}/{Invariant.ToString(Globals.TotalCount)}";
            using (var results = new frmResults())
            {
                results.ShowDialog();
            }
        }

        private void EnableMenus()
        {
            // Allow the user to access all features after encryption/decryption has finished
            tsmiFile.Enabled = true;
            tsmiTools.Enabled = true;
            cmsClearFilesMenu.Enabled = true;
        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            lblPassword.Focus();
            bool encryption = false;
            GetPasswordInput(encryption);
        }

        private void bgwDecryption_DoWork(object sender, DoWorkEventArgs e)
        {
            bool encryption = false;
            byte[] passwordBytes = (byte[])e.Argument;
            CallFileEncryption(encryption, passwordBytes, bgwDecryption);
        }

        private void bgwDecryption_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            UpdateProgressBar(e.ProgressPercentage);
        }

        private void bgwDecryption_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            const string outputMessage = "decrypted";
            FileEncryptionCompleted(outputMessage);
        }

        private void tsmiSelectFiles_Click(object sender, EventArgs e)
        {
            bool filesSelected = SelectFiles.SelectFilesDialog();
            FilesSelected(filesSelected);
        }

        private void FilesSelected(bool filesSelected)
        {
            if (filesSelected == true)
            {
                picFilesSelected.BackColor = Color.LawnGreen;
                tsmiSelectFiles.Checked = true;
                tsmiSelectFolder.Checked = false;
            }
        }

        private void tsmiSelectFolder_Click(object sender, EventArgs e)
        {
            bool folderSelected = SelectFiles.SelectFolderDialog();
            FolderSelected(folderSelected);
        }

        private void FolderSelected(bool folderSelected)
        {
            if (folderSelected == true)
            {
                picFilesSelected.BackColor = Color.LawnGreen;
                tsmiSelectFolder.Checked = true;
                tsmiSelectFiles.Checked = false;
            }
        }

        private void tsmiCreateKeyFile_Click(object sender, EventArgs e)
        {
            bool keyfileGenerated = Keyfiles.CreateKeyfile();
            KeyfileSelected(keyfileGenerated);
        }

        private void tsmiSelectKeyfile_Click(object sender, EventArgs e)
        {
            bool keyfileSelected = Keyfiles.SelectKeyfile();
            KeyfileSelected(keyfileSelected);
        }

        private void KeyfileSelected(bool keyfileSelected)
        {
            if (keyfileSelected == true)
            {
                tsmiSelectKeyfile.Checked = keyfileSelected;
            }
        }

        private void tsmiSettings_Click(object sender, EventArgs e)
        {
            using (var settings = new frmSettings())
            {
                settings.ShowDialog();
            }
        }

        private void tsmiQuit_Click(object sender, EventArgs e)
        {
            if (!bgwEncryption.IsBusy & !bgwDecryption.IsBusy & !bgwShredFiles.IsBusy)
            {
                Application.Exit();
            }
            else
            {
                DisplayMessage.InformationMessageBox("Please wait for the current operation to finish.");
            }
        }

        private void tsmiPasswordGenerator_Click(object sender, EventArgs e)
        {
            using (var passwordGenerator = new frmPasswordGenerator())
            {
                passwordGenerator.ShowDialog();
            }
        }

        private void tsmiPasswordSharing_Click(object sender, EventArgs e)
        {
            using (var passwordSharing = new frmPasswordSharing())
            {
                passwordSharing.ShowDialog();
            }
        }

        private void tsmiShredFiles_Click(object sender, EventArgs e)
        {
            bool filesSelected = SelectFiles.SelectFilesDialog();
            CallShredFiles(filesSelected);
        }

        private void tsmiShredFolder_Click(object sender, EventArgs e)
        {
            bool folderSelected = SelectFiles.SelectFolderDialog();
            CallShredFiles(folderSelected);
        }

        private void CallShredFiles(bool filesSelected)
        {
            if (filesSelected == true)
            {
                BeforeBackgroundWorkerStarts();
                bgwShredFiles.RunWorkerAsync();
            }
        }

        private void bgwShredFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            ShredFiles.ShredSelectedFiles(bgwShredFiles);
        }

        private void bgwShredFiles_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            UpdateProgressBar(e.ProgressPercentage);
        }

        private void bgwShredFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            const string outputMessage = "shredded";
            BackgroundWorkerCompleted(outputMessage);
        }

        private void tsmiBackupSettings_Click(object sender, EventArgs e)
        {
            Settings.BackupSettings();
        }

        private void tsmiRestoreSettings_Click(object sender, EventArgs e)
        {
            Settings.RestoreSettings();
        }

        private void tsmiDocumentation_Click(object sender, EventArgs e)
        {
            const string documentationLink = "https://kryptor.co.uk/Documentation.html";
            OpenURL.OpenLink(documentationLink);
        }

        private void tsmiSourceCode_Click(object sender, EventArgs e)
        {
            const string sourceCodeLink = "https://github.com/Kryptor-Software/Kryptor";
            OpenURL.OpenLink(sourceCodeLink);
        }

        private void tsmiDonate_Click(object sender, EventArgs e)
        {
            const string donateLink = "https://kryptor.co.uk/Donate.html";
            OpenURL.OpenLink(donateLink);
        }

        private void tsmiUpdateCheck_Click(object sender, EventArgs e)
        {
            bool displayUpToDate = true;
            Updates.UpdateKryptor(displayUpToDate);
        }

        private void tsmiAbout_Click(object sender, EventArgs e)
        {
            using (var about = new frmAboutKryptor())
            {
                about.ShowDialog();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (bgwEncryption.IsBusy | bgwDecryption.IsBusy | bgwShredFiles.IsBusy)
            {
                if (e != null)
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnFormClosing(e);
        }

        private void ApplyDarkTheme()
        {
            if (Constants.RunningOnMono == false)
            {
                msMenus.Renderer = new ToolStripProfessionalRenderer(new DarkMenu());
            }
            this.BackColor = Color.FromArgb(Constants.Red, Constants.Green, Constants.Blue);
            msMenus.BackColor = Color.FromArgb(Constants.Red, Constants.Green, Constants.Blue);
            tsmiFile.ForeColor = Color.White;
            tsmiTools.ForeColor = Color.White;
            tsmiHelp.ForeColor = Color.White;
            txtPassword.BackColor = Color.DimGray;
            txtPassword.ForeColor = Color.White;
            btnEncrypt.BackColor = Color.FromArgb(Constants.Red, Constants.Green, Constants.Blue);
            btnEncrypt.ForeColor = Color.White;
            btnEncrypt.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnDecrypt.BackColor = Color.FromArgb(Constants.Red, Constants.Green, Constants.Blue);
            btnDecrypt.ForeColor = Color.White;
            btnDecrypt.FlatAppearance.MouseDownBackColor = Color.Transparent;
            lblFilesSelected.ForeColor = Color.White;
            lblShowPassword.ForeColor = Color.White;
            lblDragDrop.ForeColor = Color.White;
            lblPassword.ForeColor = Color.White;
            SharedContextMenu.DarkContextMenu(cmsClearFilesMenu);
            SharedContextMenu.DarkContextMenu(cmsPasswordMenu);
            ChangeMenuTheme(tsmiFile);
            ChangeMenuTheme(tsmiTools);
            ChangeMenuTheme(tsmiHelp);
        }

        private static void ChangeMenuTheme(ToolStripMenuItem menu)
        {
            foreach (ToolStripMenuItem toolStripItem in menu.DropDownItems)
            {
                toolStripItem.ForeColor = Color.White;
                toolStripItem.BackColor = Color.FromArgb(Constants.Red, Constants.Green, Constants.Blue);
            }
        }
    }
}
