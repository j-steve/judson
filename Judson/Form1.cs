using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using SharpCompress.Archive;

namespace Judson
{
    public partial class JudsonMain : Form
    {
        const string Z_PATH = @"C:\Program Files\7-Zip\7zG.exe"; 

        public JudsonMain()
        {
            InitializeComponent();
        }
         
        private void button1_Click(object sender, EventArgs e)
        {

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                ExtractFile(openFileDialog1.FileName);
            }
        }

        private void ExtractFile(string inputFile)
        {
            if (!File.Exists(inputFile)) {throw new Exception("File \"" + inputFile + "\"does not exist.");}

            string inputFolder = Path.GetDirectoryName(inputFile);
            string outputPath = FileIO.GetUniqueFilepath(Path.Combine(inputFolder, Path.GetFileNameWithoutExtension(inputFile)));
            string extension = Path.GetExtension(inputFile).ToLowerInvariant();

            try
            {
                // Choose extraction methodology:
                if (extension == ".zip")  // If it's a basic zip file, use built-in extractor.
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(inputFile, outputPath);
                }
                else if (extension == ".7z" && File.Exists(Z_PATH)) // If 7zip is installed, use it.
                {
                    Extract7z(inputFile, outputPath);
                }
                else // If all else fails use SharpCompress.  Theoretically works on all files, but prone to errors when other methods might succeed.
                {
                    SharpCompressExtract(inputFile, outputPath);
                }

                // If archive contained single file or folder, make IT the top-level item. No need for container directory.
                string[] files = Directory.GetFileSystemEntries(outputPath, "*", SearchOption.TopDirectoryOnly);
                if (files.Count() == 1)
                {
                    string onlyFile = files.Single();
                    string newOutputPath = FileIO.GetUniqueFilepath(Path.Combine(inputFolder, Path.GetFileName(onlyFile)));
                    FileIO.MoveFileOrFolder(onlyFile, newOutputPath);
                    Directory.Delete(outputPath);
                    outputPath = Directory.Exists(newOutputPath) ? newOutputPath : inputFolder;
                }
                Process.Start(outputPath);
            }
            catch (Exception ex)
            {
                // On failure, delete output folder (UNLESS output path was input folder because extracted archive was one single file)
                if (Directory.Exists(outputPath) && outputPath != inputFolder) { Directory.Delete(outputPath, true); }
                
                // Display error message.
                lblStatus.Text = ex.Message;
                lblStatus.Update();
            }


        }
         
        private void SharpCompressExtract(string inputFile, string outputPath)
        {

            using (IArchive zipfile = ArchiveFactory.Open(inputFile))
            {
                progressBar1.Maximum = zipfile.Entries.Count();
                foreach (IArchiveEntry entry in zipfile.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        Console.WriteLine("     {0}", entry.FilePath);

                        lblStatus.Text = entry.FilePath;
                        lblStatus.Update();
                        lblSize.Text = String.Format("{0:#,0.00} MB", entry.Size / 1024.0 / 1024.0);
                        lblSize.Update();

                        entry.WriteToDirectory(outputPath, SharpCompress.Common.ExtractOptions.ExtractFullPath);
                    }
                    progressBar1.Value++;
                }
            } 
        }


        
        private void Extract7z(string inputFile, string outputPath)
        {   
            ProcessStartInfo pro = new ProcessStartInfo();
            pro.WindowStyle = ProcessWindowStyle.Hidden;
            pro.FileName = Z_PATH;
            pro.Arguments = String.Format("x \"{0}\" -o\"{1}\"", inputFile, outputPath);
            Process x = Process.Start(pro);
            x.WaitForExit(); 
        } 
         


    }
}
