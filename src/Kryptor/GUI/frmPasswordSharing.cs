﻿using System;
using System.Drawing;
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
    public partial class frmPasswordSharing : Form
    {
        public frmPasswordSharing()
        {
            InitializeComponent();
        }

        private void frmPasswordSharing_Load(object sender, EventArgs e)
        {
            lblAsymmetricKey.Focus();
            if (Globals.DarkTheme == true)
            {
                ApplyDarkTheme();
            }
            // Fix label alignment on Linux & macOS
            MonoGUI.AlignLabels(lblAsymmetricKey, lblPassword, null, llbGenerateKeyPair);
        }

        private void ApplyDarkTheme() 
        {
            this.BackColor = Color.FromArgb(Constants.Red, Constants.Green, Constants.Blue);
            lblAsymmetricKey.ForeColor = Color.White;
            lblPassword.ForeColor = Color.White;
            llbGenerateKeyPair.LinkColor = Color.White;
            llbGenerateKeyPair.ActiveLinkColor = Color.White;
            txtKey.BackColor = Color.DimGray;
            txtKey.ForeColor = Color.White;
            txtPassword.BackColor = Color.DimGray;
            txtPassword.ForeColor = Color.White;
            btnEncryptPassword.BackColor = Color.FromArgb(Constants.Red, Constants.Green, Constants.Blue);
            btnEncryptPassword.ForeColor = Color.White;
            btnEncryptPassword.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnDecryptPassword.BackColor = Color.FromArgb(Constants.Red, Constants.Green, Constants.Blue);
            btnDecryptPassword.ForeColor = Color.White;
            btnDecryptPassword.FlatAppearance.MouseDownBackColor = Color.Transparent;
            SharedContextMenu.DarkContextMenu(cmsTextboxMenu);
        }

        private void tsmiCopyTextbox_Click(object sender, EventArgs e)
        {
            SharedContextMenu.CopyTextbox(sender);
        }

        private void tsmiClearTextbox_Click(object sender, EventArgs e)
        {
            SharedContextMenu.ClearTextbox(sender);
        }

        private void tsmiClearClipboard_Click(object sender, EventArgs e)
        {
            EditClipboard.ClearClipboard();
        }

        private void picHelp_Click(object sender, EventArgs e)
        {
            const string passwordSharingLink = "https://kryptor.co.uk/Password%20Sharing.html";
            OpenURL.OpenLink(passwordSharingLink);
        }

        private void llbGenerateKeyPair_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lblAsymmetricKey.Focus();
            using (var generateKeyPair = new frmGenerateKeyPair())
            {
                generateKeyPair.ShowDialog();
            }
        }

        private void btnEncryptPassword_Click(object sender, EventArgs e)
        {
            lblAsymmetricKey.Focus();
            bool encryption = true;
            GetUserInput(encryption);
        }

        private void GetUserInput(bool encryption)
        {
            char[] key = txtKey.Text.ToCharArray();
            char[] password = txtPassword.Text.ToCharArray();
            if (key.Length > 0 & password.Length > 0)
            {
                char[] message = PasswordSharing.ConvertUserInput(encryption, key, password);
                if (message.Length > 0)
                {
                    txtPassword.Text = new string(message);
                    ClearArrays(key, password, message);
                }
            }
            else
            {
                DisplayUserInputError(encryption);
            }
        }

        private static void ClearArrays(char[] key, char[] password, char[] message)
        {
            Utilities.ZeroArray(key);
            Utilities.ZeroArray(password);
            Utilities.ZeroArray(message);
        }

        private void btnDecryptPassword_Click(object sender, EventArgs e)
        {
            lblPassword.Focus();
            bool encryption = false;
            GetUserInput(encryption);
        }

        private static void DisplayUserInputError(bool encryption)
        {
            string errorMessage;
            if (encryption == true)
            {
                errorMessage = "Sender: Please enter the recipient's public key and a plaintext password to encrypt.";
            }
            else
            {
                errorMessage = "Recipient: Please enter your private key and a ciphertext password to decrypt.";
            }
            DisplayMessage.InformationMessageBox(errorMessage);
        }
    }
}
