#region using
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using CCWin;
using Newtonsoft.Json;
using SharpCompress.Archives.Zip;
#endregion

namespace NugetUpdateV4
{
    public partial class Form1 : CCSkinMain
    {
        #region field
        public ConcurrentBag<Metadata> Metadatas =new ConcurrentBag<Metadata>();
        #endregion


        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            skinTextBox1.Text = AppDomain.CurrentDomain.BaseDirectory;
#if DEBUG
            skinTextBox1.Text = @"E:\\dll\\Dropbox\\nugetpkg";
#endif

        }
        private void skinButton1_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog
                         {
                             Description = @"请选择 NUPKG 所在文件夹"
                         };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            if (string.IsNullOrEmpty(dialog.SelectedPath))
            {
                MessageBox.Show(this, @"文件夹路径不能为空", @"提示");
                return;
            }
            skinTextBox1.Text = dialog.SelectedPath;
        }
        private async void skinButton2_Click(object sender, EventArgs e)
        {
            while (Metadatas.TryTake(out _))
            {
            }
            toolStripStatusLabel1.Text = @"正在处理请稍后...";
            await AnalyseZips();
            toolStripStatusLabel1.Text = @"处理完毕";
        }
        private Task AnalyseZips()
        {
            return Task.Run(() =>
                            {
                                var files = Directory.GetFiles(skinTextBox1.Text, "*.nupkg");
                                Invoke(new Action(() => { toolStripStatusLabel1.Text = $@"共发现 NugetPkg {files.Length} 个"; }));
                                Parallel.ForEach(files, f =>
                                                        {
                                                            try
                                                            {
                                                                var archive = SharpCompress.Archives.ArchiveFactory.Open(f);
                                                                var nuspec = archive.Entries.First(s => s.Key.EndsWith(".nuspec"));
                                                                if (nuspec == null) throw new Exception("未发现 nuspec 文件");
                                                                XmlDocument xml=new XmlDocument();
                                                                xml.Load(nuspec.OpenEntryStream());
                                                                string xmlstr=JsonConvert.SerializeXmlNode(xml);
                                                                var pkg= JsonConvert.DeserializeObject<ClassNuspec>(xmlstr);
                                                                Metadatas.Add(pkg.Package.Metadata);
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                var res  = MessageBox.Show($"File: {f} \r\n{e.Message}\r\n文件长度：{GetFileSize(f)}\r\n是否删除错误数据？", "Warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                                                                if (res == DialogResult.Yes)
                                                                {
                                                                    File.Delete(f);
                                                                }


                                                            }


                                                        });
                            });
        }
        public string GetFileSize(string sFileFullName)
        {
            FileInfo fiInput = new FileInfo(sFileFullName);
            double   len     = fiInput.Length;

            string[] sizes =
            {
                "B",
                "KB",
                "MB",
                "GB"
            };
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len = len / 1024;
            }

            string filesize = $"{len:0.##} {sizes[order]}";
            return filesize;

        }
    }
}