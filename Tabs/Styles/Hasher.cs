﻿using System;
using System.IO;
using System.Windows.Forms;
using DataCore.Functions;
using Grimoire.Utilities;
using Grimoire.Logs.Enums;
using Grimoire.Configuration;

namespace Grimoire.Tabs.Styles
{
    public partial class Hasher : UserControl
    {
        #region Properties
        Logs.Manager lManager;
        XmlManager xMan = XmlManager.Instance;
        ConfigMan configMan = GUI.Main.Instance.ConfigMan;
        #endregion

        #region Constructors
        public Hasher()
        {
            InitializeComponent();
            lManager = Logs.Manager.Instance;
            lManager.Enter(Sender.HASHER, Level.NOTICE, "Hasher Utility Started.");
            set_checks();
            localize();
        }
        #endregion

        #region Events

        private void input_TextChanged(object sender, EventArgs e)
        {
            if (input.Text.Length > 3)
                output.Text = (StringCipher.IsEncoded(input.Text)) ? StringCipher.Decode(input.Text) : StringCipher.Encode(input.Text);
            else if (input.Text.Length == 0)
                output.ResetText();
        }

        private void cMenu_clear_Click(object sender, EventArgs e) => fileGrid.Rows.Clear();

        private void flipBtn_Click(object sender, EventArgs e)
        {
            if (output.Text.Length > 3)
                input.Text = output.Text;

            lManager.Enter(Sender.HASHER, Level.NOTICE, "User flipped input/output");
        }

        private void cMenu_add_file_Click(object sender, EventArgs e)
        {
            string path = Paths.FilePath;

            if (Paths.FileResult != DialogResult.OK)
                return;

            add_file_to_grid(path);
        }

        private void cMenu_convert_Click(object sender, EventArgs e)
        {
            if (fileGrid.SelectedRows.Count > 0)
                convertEntry(fileGrid.SelectedRows[0].Index);

            if (autoClear_chk.Checked)
                fileGrid.Rows.Remove(fileGrid.SelectedRows[0]);
        }

        private void cMenu_convert_all_Click(object sender, EventArgs e) => convertAllEntries();

        private void autoClear_chk_CheckStateChanged(object sender, EventArgs e)
        {
            configMan["AutoConvert", "Hash"] = autoClear_chk.Checked;
            //TODO: implement save bruh
        }

        private void opt_auto_convert_CheckStateChanged(object sender, EventArgs e)
        {
            configMan["AutoClear", "Hash"] = autoClear_chk.Checked;
            //TODO: implement save bruh
        }

        private void cMenu_add_folder_Click(object sender, EventArgs e)
        {
            string directory = Grimoire.Utilities.Paths.FolderPath;

            if (Grimoire.Utilities.Paths.FolderResult != DialogResult.OK)
                return;

            foreach (string path in Directory.GetFiles(directory))
                add_file_to_grid(path);
        }

        #endregion

        #region Methods (Public)

        public void Add_Dropped_Files(string[] paths)
        {
            if (paths.Length == 1)
            {
                if (File.GetAttributes(paths[0]).HasFlag(FileAttributes.Directory))
                    paths = Directory.GetFiles(paths[0]);
            }

            foreach (string path in paths)
            {
                add_file_to_grid(path);
            }

            if (autoConvert_chk.Checked)
                convertAllEntries();
        }

        public void Localize() { localize(); }

        #endregion

        #region Methods (private)

        private void set_checks()
        {
            autoClear_chk.Checked = configMan["AutoClear", "Hash"];
            autoConvert_chk.Checked = configMan["AutoConvert", "Hash"];

            switch (configMan["Type", "Hash"])
            {
                case 1:
                    optAppend_ascii_rBtn.Checked = true;
                    break;

                case 2:
                    optRemove_ascii_rBtn.Checked = true;
                    break;

                default:
                    optNone_rBtn.Checked = true;
                    break;
            }
        }

        private void convertAllEntries()
        {
            foreach (DataGridViewRow row in fileGrid.Rows)
                convertEntry(row.Index);

            if (autoClear_chk.Checked)
                cMenu_clear_Click(null, EventArgs.Empty);
        }

        private void convertEntry(int rowIndex)
        {
            DataGridViewRow row = fileGrid.Rows[rowIndex];
            // [0] - Original Name
            // [1] - Converted Name
            // [2] - File Directory
            // [3] - Conversion Status

            string originalPath = string.Format(@"{0}\{1}", (string)row.Cells[2].Value, (string)row.Cells[0].Value);
            string convertedPath = string.Format(@"{0}\{1}", (string)row.Cells[2].Value, (string)row.Cells[1].Value);

            if (File.Exists(convertedPath))
            {
                if (MessageBox.Show("The conversion of this file: {0} will result in overwriting a file with the same converted name.\nDo you wish to continue?", "File Already Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Hand) == DialogResult.No)
                {
                    lManager.Enter(Sender.HASHER, Level.NOTICE, "User opted to cancel conversion of {0}", (string)row.Cells[0].Value);
                    return;
                }

                File.Delete(convertedPath);
                lManager.Enter(Sender.HASHER, Level.NOTICE, "Converted Path already exists, deleting file at path: {0}", convertedPath);
            }

            if (File.Exists(originalPath))
            {
                File.Move(originalPath, convertedPath);
                fileGrid.Rows[rowIndex].Cells[3].Value = "Complete";
                lManager.Enter(Sender.HASHER, Level.NOTICE, "File at path: {0} converted to path: {1}", originalPath, convertedPath);
            }
        }

        private void add_file_to_grid(string path)
        {
            string originalName = Path.GetFileName(path);
            string outName = string.Empty;
            bool IsEncoded = StringCipher.IsEncoded(originalName);

            switch (IsEncoded)
            {
                case true:
                    string decodedName = StringCipher.Decode(originalName);

                    if (optRemove_ascii_rBtn.Checked)
                        if (decodedName.Contains("(ascii)"))
                            outName = decodedName.Replace("(ascii)", string.Empty);
                    break;

                case false:
                    if (optAppend_ascii_rBtn.Checked)
                        if (!originalName.Contains("(ascii)"))
                            outName = originalName.Insert(originalName.Length - 4, "(ascii)");

                    if (optRemove_ascii_rBtn.Checked)
                        outName = originalName.Replace("(ascii)", string.Empty);
                    break;
            }

            if (string.IsNullOrEmpty(outName))
                outName = originalName;


            switch (IsEncoded)
            {
                case true:
                    {
                        originalName = StringCipher.Decode(outName);
                        fileGrid.Rows.Add(outName, originalName, Path.GetDirectoryName(path), "Pending");
                    }
                    break;

                case false:
                    {
                        string convertedName = StringCipher.IsEncoded(outName) ? StringCipher.Decode(outName) : StringCipher.Encode(outName);
                        fileGrid.Rows.Add(outName, convertedName, Path.GetDirectoryName(path), "Pending");
                    }
                    break;
            }           
        }

        private void localize()
        {
            xMan.Localize(this, Localization.Enums.SenderType.Tab);
        }

        #endregion
    }
}
