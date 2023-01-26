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
using Newtonsoft.Json;

namespace DesLevelObj
{
    public partial class MainForm : Form
    {
        public GameFiles gameFiles;
        public TextureRemapRoot textureRemap;
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
                openFileDialog.Filter = "Level Files (*.hog;*.rdl;*.rl2;*.mn3;*.d3l)|*.hog;*.rdl;*.rl2;*.mn3;*.d3l|All files (*.*)|*.*";
                var cur = txtLevelFile.Text;
                if (cur != "")
                {
                    if (!Directory.Exists(cur))
                        cur = Path.GetDirectoryName(cur);
                    openFileDialog.InitialDirectory = cur;
                }
                else if (gameFiles != null)
                {
                    openFileDialog.InitialDirectory = gameFiles.dir;
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
                    var buf = new byte[4];
                    f.Read(buf, 0, buf.Length);
                    return (buf[0] == 'D' && buf[1] == 'H' && buf[2] == 'F') ||
                        (buf[0] == 'H' && buf[1] == 'O' && buf[2] =='G' && buf[3] == '2');
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
                name.EndsWith(".rl2", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".d3l", StringComparison.OrdinalIgnoreCase));
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

        private void GameDirUpdate()
        {
            if (gameFiles != null && gameFiles.dir == txtGameDir.Text)
                return;
            try
            {
                gameFiles = txtGameDir.Text == "" ? null : new GameFiles(txtGameDir.Text);
                Log("Loaded game data for Descent " + (int)gameFiles.version);
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }

        private void ConvertD3(Stream s, string dest, bool dumpTex)
        {
            var lvl = D3Level.Level.Read(new BinaryReader(s));
            s.Close();
            D3LevelToObj.Convert(this, gameFiles, textureRemap, lvl, dest, dumpTex);
        }

        private void Convert(string filename, string level, bool dumpTex)
        {
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

            var outDir = txtOutDir.Text;
            var dest = Path.Combine(
                    outDir != "" ? outDir : Path.GetDirectoryName(filename),
                    Path.GetFileNameWithoutExtension(level) + ".obj");

            if (filename.EndsWith(".d3l", StringComparison.OrdinalIgnoreCase) ||
                level.EndsWith(".d3l", StringComparison.OrdinalIgnoreCase))
            {
                ConvertD3(s, dest, dumpTex);
            }
            else
            {
                var lvl = new ClassicLevel();
                lvl.Read(new BinaryReader(s));
                s.Close();
                gameFiles.SelectPalette(lvl.palette);
                LevelToObj.Convert(this, gameFiles, textureRemap, lvl, dest, dumpTex);
            }
            Log("Converted level to " + dest);
        }

        public void Log(string message)
        {
            txtLog.AppendText(message + "\r\n");
        }

        private void btnGameDir_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Main hog (descent.hog;descent2.hog;descent3.hog)|descent.hog;descent2.hog;d3.hog|All files (*.*)|*.*";
                var cur = txtGameDir.Text;
                if (cur != "")
                {
                    openFileDialog.InitialDirectory = cur;
                }
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtGameDir.Text = Path.GetDirectoryName(openFileDialog.FileName);
                }
            }
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            if (gameFiles == null || gameFiles.version == Classic.Version.UNKNOWN)
            {
                Log("Cannot convert, missing game folder");
                return;
            }
            
            try
            {
                Convert(txtLevelFile.Text, cmbLevel.SelectedItem?.ToString(), chkTexPng.Checked);
            }
            catch (Exception ex)
            {
                Log($"Error!!! {ex}");
            }
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
                txtGameDir.Text = (string)key.GetValue("GameDir");
                txtLevelFile.Text = (string)key.GetValue("LevelFile");
                cmbLevel.SelectedItem = (string)key.GetValue("Level");
                txtOutDir.Text = (string)key.GetValue("OutDir");
                chkTexRemap.Checked = (int)key.GetValue("TexRemap", 0) != 0;
                txtTextureRemapFile.Text = (string)key.GetValue("TexureRemapFile");
                chkTexPng.Checked = (int)key.GetValue("TexPng", 0) != 0;
                
                txtTextureRemapFile.Enabled = chkTexRemap.Checked;
                btnTextureRemap.Enabled = chkTexRemap.Checked;
                
                LoadTextureRemapJson();
                
                updating = false;
            }
        }

        private void SaveAll()
        {
            if (updating)
                return;
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\DesLevelObj");
            key.SetValue("GameDir", txtGameDir.Text);
            key.SetValue("LevelFile", txtLevelFile.Text);
            key.SetValue("Level", cmbLevel.SelectedItem?.ToString() ?? "");
            key.SetValue("OutDir", txtOutDir.Text);
            key.SetValue("TexRemap", chkTexRemap.Checked ? 1 : 0);
            key.SetValue("TexureRemapFile", txtTextureRemapFile.Text);
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

        private void txtGameDir_TextChanged(object sender, EventArgs e)
        {
            SaveAll();
            GameDirUpdate();
        }

        private void cmbLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            SaveAll();
        }

        private void chkTexRemap_CheckedChanged(object sender, EventArgs e)
        {
            txtTextureRemapFile.Enabled = chkTexRemap.Checked;
            btnTextureRemap.Enabled = chkTexRemap.Checked;
            LoadTextureRemapJson();
            SaveAll();
        }

        private void txtTextureRemapFile_TextChanged(object sender, EventArgs e)
        {
            SaveAll();
        }
        
        private void btnTextureRemap_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Json Files (*.json)|*.json";
                var cur = txtTextureRemapFile.Text;
                if (cur != "")
                {
                    if (!Directory.Exists(cur))
                        cur = Path.GetDirectoryName(cur);
                    openFileDialog.InitialDirectory = cur;
                }
                else if (gameFiles != null)
                {
                    openFileDialog.InitialDirectory = gameFiles.dir;
                }
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtTextureRemapFile.Text = openFileDialog.FileName;
                    LoadTextureRemapJson();
                }
            }
        }
        
        private void LoadTextureRemapJson()
        {
            textureRemap = null;
            
            Log("Texture Remap reset");
            
            var textureRemapFile = txtTextureRemapFile.Text;

            if (!chkTexRemap.Checked || string.IsNullOrEmpty(textureRemapFile))
                return;
            
            if (!File.Exists(textureRemapFile))
            {
                Log($"Warning: file does not exist! {textureRemapFile}");
                return;
            }
            
            if (!textureRemapFile.ToLower().EndsWith(".json"))
            {
                Log($"Warning: file must be .json! {textureRemapFile}");
                return;
            }
            
            try
            {
                var textureRemapFileContents = File.ReadAllText(textureRemapFile);
                
                if (!string.IsNullOrEmpty(textureRemapFileContents))
                    textureRemap = JsonConvert.DeserializeObject<TextureRemapRoot>(textureRemapFileContents);
            }
            catch (Exception e)
            {
                Log($"Error!!! {e}");
            }

            if (textureRemap != null)
            {
                Log("Texture remap json loaded successfully!");
                Log($"Texture Remap size = {textureRemap.TextureRemap.Count} - READY!");
            }
            else
                Log("Texture remap json could not be parsed!");
        }
    }
}
