using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Classic;
using Microsoft.Win32;

namespace DesLevelObj
{
    public partial class MainForm : Form
    {
        public GameFiles gameFiles;
        private bool updating;

        public MainForm()
        {
            InitializeComponent();
            RestoreAll();
        }

        private void btnLevelFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Level Files (*.hog;*.rdl;*.rl2)|*.hog;*.rdl;*.rl2|All files (*.*)|*.*";
                var cur = txtLevelFile.Text;
                if (cur != "")
                {
                    if (!Directory.Exists(cur))
                        cur = Path.GetDirectoryName(cur);
                    openFileDialog.InitialDirectory = cur;
                }
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtLevelFile.Text = openFileDialog.FileName;
                    //LevelFileUpdate();
                }
            }
        }

        private bool IsHogFile(string filename)
        {
            try
            {
                using (var f = File.OpenRead(filename))
                {
                    var buf = new byte[3];
                    f.Read(buf, 0, buf.Length);
                    return buf[0] == 'D' && buf[1] == 'H' && buf[2] == 'F';
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        private IEnumerable<string> HogLevelFilenames(Hog hog)
        {
            return hog.items.Select(item => item.name).Where(name =>
                name.EndsWith(".rdl", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".rl2", StringComparison.OrdinalIgnoreCase));
        }

        private void LevelFileUpdate()
        {
            var sel = cmbLevel.SelectedItem?.ToString();
            cmbLevel.Items.Clear();
            if (IsHogFile(txtLevelFile.Text))
            {
                var hog = new Hog(txtLevelFile.Text);
                foreach (var name in HogLevelFilenames(hog))
                    cmbLevel.Items.Add(name); //.Substring(0, entry.name.Length - 4));
            }
            if (sel != null)
                cmbLevel.SelectedItem = sel;
            //cmbLevel.Text = cmbLevel.SelectedValue != null ? cmbLevel.SelectedValue.ToString() : "";
        }

        private void PigFileUpdate()
        {
            try
            {
                gameFiles = txtPigFile.Text == "" ? null : new GameFiles(txtPigFile.Text);
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }

        private void Convert(string filename, string level, bool dumpTex)
        {
            var lvl = new ClassicLevel();
            Stream s;
            if (IsHogFile(filename))
            {
                var hog = new Hog(txtLevelFile.Text);
                if (level == null)
                    level = HogLevelFilenames(hog).First();
                s = new MemoryStream(hog.ItemData(level));
            }
            else
            {
                s = File.OpenRead(filename);
                level = Path.GetFileName(filename);
            }
            lvl.Read(new BinaryReader(s));
            s.Close();
            var outDir = txtOutDir.Text;
            var dest = Path.Combine(
                    outDir != "" ? outDir : Path.GetDirectoryName(filename),
                    Path.GetFileNameWithoutExtension(level) + ".obj");
            LevelToObj.Convert(gameFiles, lvl, dest, dumpTex);
            Log("Converted level to " + dest);
        }

        public void Log(string message)
        {
            txtLog.AppendText(message + "\r\n");
        }

        private void btnPigFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Pig Files (*.pig)|*.pig|All files (*.*)|*.*";
                var cur = txtPigFile.Text;
                if (cur != "")
                {
                    if (!Directory.Exists(cur))
                        cur = Path.GetDirectoryName(cur);
                    openFileDialog.InitialDirectory = cur;
                }
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtPigFile.Text = openFileDialog.FileName;
                    //PigFileUpdate();
                }
            }
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            if (gameFiles == null || gameFiles.version == Classic.Version.UNKNOWN)
            {
                Log("Cannot convert, missing pig file");
                return;
            }
            Convert(txtLevelFile.Text, cmbLevel.SelectedItem?.ToString(), chkTexPng.Checked);
        }

        private void btnOutDir_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.FileName = "ignored.obj";
                var cur = txtOutDir.Text;
                if (cur != "")
                    saveFileDialog.InitialDirectory = cur;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutDir.Text = Path.GetDirectoryName(saveFileDialog.FileName);
                }
            }
        }
 
        private void RestoreAll()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DesLevelObj"))
            {
                if (key == null)
                    return;
                updating = true;
                txtPigFile.Text = (string)key.GetValue("PigFile");
                txtLevelFile.Text = (string)key.GetValue("LevelFile");
                cmbLevel.SelectedItem = (string)key.GetValue("Level");
                txtOutDir.Text = (string)key.GetValue("OutDir");
                chkTexPng.Checked = (int)key.GetValue("TexPng", 0) != 0;
                updating = false;
            }
        }

        private void SaveAll()
        {
            if (updating)
                return;
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\DesLevelObj");
            key.SetValue("PigFile", txtPigFile.Text);
            key.SetValue("LevelFile", txtLevelFile.Text);
            key.SetValue("Level", cmbLevel.SelectedItem?.ToString() ?? "");
            key.SetValue("OutDir", txtOutDir.Text);
            key.SetValue("TexPng", chkTexPng.Checked ? 1 : 0);
        }

        private void chkTexPng_CheckedChanged(object sender, EventArgs e)
        {
            SaveAll();
        }

        private void txtOutDir_TextChanged(object sender, EventArgs e)
        {
            SaveAll();
        }

        private void txtLevelFile_TextChanged(object sender, EventArgs e)
        {
            SaveAll();
            LevelFileUpdate();
        }

        private void txtPigFile_TextChanged(object sender, EventArgs e)
        {
            SaveAll();
            PigFileUpdate();
        }

        private void cmbLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            SaveAll();
        }
    }
}
