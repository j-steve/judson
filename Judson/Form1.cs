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
         
        private string ExtractFile(string inputFile)
        {
            string inputFolder = Path.GetDirectoryName(inputFile);
            string outputPath = GetUniqueFilepath(Path.Combine(inputFolder, Path.GetFileNameWithoutExtension(inputFile)));

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
            return outputPath;
            /*
            string[] files = Directory.GetFileSystemEntries(tempFolder, "*", SearchOption.TopDirectoryOnly);
            if (files.Count() == 1)
            {
                string onlyFile = files.Single();
                outputPath = GetUniqueFilepath(Path.Combine(inputFolder, Path.GetFileName(onlyFile)));
                MoveFileOrFolder(onlyFile, outputPath);
                Directory.Delete(tempFolder);
            }
            else
            {
                outputPath = GetUniqueFilepath(Path.Combine(inputFolder, Path.GetFileNameWithoutExtension(inputFile)));
                Directory.Move(tempFolder, outputPath);
            }
        }
        return outputPath;*/
        }


        public static void MoveFileOrFolder(string oldPath, string newPath)
        {
            if (File.Exists(oldPath)) { File.Move(oldPath, newPath); }
            else if (Directory.Exists(oldPath)) { Directory.Move(oldPath, newPath); }
            else { throw new FileNotFoundException("Cannot move file: file or folder does not exist.", oldPath); }
        }

        /// <summary>
        ///  Returns TRUE if the specified file path exists as either a file or a directory.
        /// </summary>
        public static bool FilepathExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        public static string CreateTempFolder()
        {
            string path;
            do { path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()); }
            while (Directory.Exists(path));

            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Returns an unused filepath by appending a unique number to the end of the given path (if neccesary).
        /// </summary>
        public static string GetUniqueFilepath(string path)
        {
            return UniqueFilepath.GetNext(path);
        }

        /// <summary>
        /// The private static class which produces the unique filename.
        /// </summary>
        private static class UniqueFilepath
        {
            private static string NumberPattern = " ({0})";

            public static string GetNext(string path)
            {
                if (File.Exists(path))
                { // If path has extension then insert the number pattern just before the extension and return next filename.
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Path.GetDirectoryName(path));
                    sb.Append(Path.DirectorySeparatorChar);
                    sb.Append(Path.GetFileNameWithoutExtension(path));
                    sb.Append(NumberPattern);
                    sb.Append(Path.GetExtension(path));
                    return IncrementUntilUnique(sb.ToString());
                }
                else if (Directory.Exists(path))
                { // If it is a directory, juust append the pattern to the end.
                    return IncrementUntilUnique(path + NumberPattern);
                }
                else
                { // Short-cut if already available.
                    return path;
                }

            }

            private static string IncrementUntilUnique(string pattern)
            {
                string tmp = string.Format(pattern, 1);
                if (tmp == pattern) { throw new ArgumentException("The pattern must include an index place-holder", "pattern"); }

                if (!FilepathExists(tmp))
                    return tmp; // short-circuit if no matches

                int min = 1, max = 2; // min is inclusive, max is exclusive/untested

                while (FilepathExists(string.Format(pattern, max)))
                {
                    min = max;
                    max *= 2;
                }

                while (max != min + 1)
                {
                    int pivot = (max + min) / 2;
                    if (FilepathExists(string.Format(pattern, pivot)))
                        min = pivot;
                    else
                        max = pivot;
                }

                return string.Format(pattern, max);
            }
        }

        private void Unzip(FileInfo inputFile)
        {
            if (!inputFile.Exists)
            {
                throw new Exception("File \"" + inputFile.FullName + "\"does not exist.");
            }

            string inputFileName = Path.GetFileNameWithoutExtension(inputFile.FullName);
            string outputPath = Path.Combine(inputFile.DirectoryName, inputFileName);


            switch (inputFile.Extension.ToLowerInvariant())
            {
                case ".7z":
                    const string Z_PATH = @"C:\Program Files\7-Zip\7zG.exe"; 
                    if (!File.Exists(Z_PATH)) {throw new InvalidOperationException("Required unzipper not found at " + Z_PATH + ".");}
                    ProcessStartInfo pro = new ProcessStartInfo();
                    pro.WindowStyle = ProcessWindowStyle.Hidden;
                    pro.FileName = Z_PATH;
                    pro.Arguments = String.Format("x \"{0}\" -o\"{1}\"", inputFile, outputPath);
                    Process x = Process.Start(pro);
                    x.WaitForExit(); 
                    break;
                case ".rar": 
                    throw new NotImplementedException();
                    break;
                default:
                    System.IO.Compression.ZipFile.ExtractToDirectory(inputFile.FullName, outputPath);
                    break;
            } 
        }

         


    }
}
