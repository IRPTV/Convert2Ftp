using MediaInfoNET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;



namespace ConvertToFtp
{
    public partial class Form1 : Form
    {
        string _EpisodeNum = "";
        string _ProgId = "";
        string _TempDir = "";
        string _Extention = "";
        int _RowIndx = 0;
        public Form1()
        {
            InitializeComponent();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "mp4 files (*.mp4)|*.mp4|Avi files (*.avi)|*.avi";
            openFileDialog1.Title = "Select Avi Or Mp4 Files";
            openFileDialog1.Multiselect = true;
            openFileDialog1.InitialDirectory = textBox1.Text;
            openFileDialog1.ShowDialog();
        }
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            textBox1.Text = openFileDialog1.FileName;
            MediaFile InMeldiaFile = new MediaFile(textBox1.Text);
            if (InMeldiaFile.Audio.Count == 1)
            {
                if (InMeldiaFile.Audio[0].Channels == 2)
                {
                    button3.Enabled = true;
                }
                else
                {
                    button3.Enabled = false;
                    MessageBox.Show("File Is MONO and it's not acceptable");
                }
            }
            else
            {
                button3.Enabled = false;
                MessageBox.Show("File has no audio channel");
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            progressBar1.Maximum = 100;
            _TempDir = System.Configuration.ConfigurationSettings.AppSettings["TempDirectory"].Trim();
        }
        public bool IsDirectory(string directory)
        {
            if (directory == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            else if (System.IO.Path.GetExtension(directory) == string.Empty)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        protected bool CheckDir()
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(System.Configuration.ConfigurationSettings.AppSettings["Server"].Trim()
                   + "/" + _ProgId);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Credentials = new NetworkCredential(System.Configuration.ConfigurationSettings.AppSettings["UserName"].Trim(),
                    System.Configuration.ConfigurationSettings.AppSettings["PassWord"].Trim());
                request.UsePassive = true;
                request.UseBinary = true;
                request.KeepAlive = false;
                using (var resp = (FtpWebResponse)request.GetResponse())
                {
                    if (resp.StatusCode == FtpStatusCode.PathnameCreated)
                    {
                        // MessageBox.Show("Directoy " + textBox2.Text + " created.");
                        //  Console.WriteLine(resp.StatusCode);
                        resp.Close();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }


        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Start")
            {
                button1.Text = "Started";
                button1.BackColor = Color.Red;
                richTextBox2.Text = "";
                richTextBox1.Text = "";
            }
            richTextBox2.Text = "";
            richTextBox1.Text = "";
            _RowIndx = QeueProcess();
            if (_RowIndx >= 0)
            {

                dataGridView1.Rows[_RowIndx].Cells[3].Value = "In Progress";
                if (dataGridView1.Rows.Count > 0)
                {
                    dataGridView1.Rows[_RowIndx].Selected = true;
                    dataGridView1.FirstDisplayedScrollingRowIndex = _RowIndx;
                }
                _ProgId = dataGridView1.Rows[_RowIndx].Cells[1].Value.ToString();
                _EpisodeNum = dataGridView1.Rows[_RowIndx].Cells[2].Value.ToString();
                _Extention = Path.GetExtension(dataGridView1.Rows[_RowIndx].Cells[0].Value.ToString());
                CheckDir();
                CopyLocal(dataGridView1.Rows[_RowIndx].Cells[0].Value.ToString(), _TempDir + _EpisodeNum + "-Orig" + _Extention, _TempDir);

            }
        }
        protected void CopyLocal(string SourceFile, string DestFile, string TemDir)
        {
            richTextBox2.Text += "\n===================\n";
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
            Application.DoEvents();


            DirectoryInfo Dir = new DirectoryInfo(TemDir);
            if (!Dir.Exists)
            {
                Dir.Create();
                richTextBox2.Text += "Temp Directory Created: " + Dir.ToString() + "\n";
                richTextBox2.SelectionStart = richTextBox2.Text.Length;
                richTextBox2.ScrollToCaret();
                Application.DoEvents();
            }
            richTextBox2.Text += "Start Copy To Local: " + SourceFile.ToString() + "\n";
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
            Application.DoEvents();

            List<String> TempFiles = new List<String>();
            TempFiles.Add(SourceFile);

            CopyFiles.CopyFiles Temp = new CopyFiles.CopyFiles(TempFiles, DestFile);
            Temp.EV_copyCanceled += Temp_EV_copyCanceled;
            Temp.EV_copyComplete += Temp_EV_copyComplete;

            CopyFiles.DIA_CopyFiles TempDiag = new CopyFiles.DIA_CopyFiles();
            TempDiag.SynchronizationObject = this;
            Temp.CopyAsync(TempDiag);

        }
        void Temp_EV_copyComplete()
        {
            this.Invoke(new MethodInvoker(delegate()
            {
                richTextBox2.Text += "End Copy To Local: \n";
                richTextBox2.SelectionStart = richTextBox2.Text.Length;
                richTextBox2.ScrollToCaret();
                Application.DoEvents();
                string FileNameSuffix = "";
                if (dataGridView1.Rows[_RowIndx].Cells[4].Value.ToString() == "Ar")
                {
                    FileNameSuffix = "";
                }
                else
                {
                    FileNameSuffix = "_F";
                }


                Convert(_TempDir + _EpisodeNum + "-Orig" + _Extention, _TempDir + _EpisodeNum + FileNameSuffix + ".mp4", _TempDir, "mp4");

            }));

        }
        void Temp_EV_copyCanceled(List<CopyFiles.CopyFiles.ST_CopyFileDetails> filescopied)
        {
            //throw new NotImplementedException();
            MessageBox.Show("عملیات کپی متوقف شد");


        }
        protected bool Convert(string InFile, string OutFile, string TemDir, string Ext)
        {
            // -i "concat:1.mpg|2.mpg"  -c copy    001.mpg

            string AudioScript = "";

            //Check Language:
            MediaFile InMediaFile = new MediaFile(InFile);
            if (InMediaFile.Audio.Count==1)
            {
                if(InMediaFile.Audio[0].Channels==2)
                {
                    if(dataGridView1.Rows[_RowIndx].Cells[4].Value.ToString() == "Ar")
                    {
                        AudioScript = " -map_channel 0.1.0 -map_channel 0.1.0 ";
                    }
                    else
                    {
                        AudioScript = " -map_channel 0.1.1 -map_channel 0.1.1 ";
                    }
                }
                if (InMediaFile.Audio[0].Channels == 1)
                {
                   // AudioScript = "-map_channel 0.1.0 -map_channel 0.1.0";
                }
            }         



            
            
            progressBar1.Value = 0;
            label1.Text = "0%";

            Process proc = new Process(); if (Environment.Is64BitOperatingSystem)
            {
                proc.StartInfo.FileName = Path.GetDirectoryName(Application.ExecutablePath) + "\\ffmpeg64";
            }
            else
            {
                proc.StartInfo.FileName = Path.GetDirectoryName(Application.ExecutablePath) + "\\ffmpeg32";
            }

            DirectoryInfo Dir = new DirectoryInfo(TemDir);
            if (!Dir.Exists)
            {
                Dir.Create();
            }


            //ffmpeg -i 001.mpg  -b 700k     -ar 11025 -y   113.flv
            // proc.StartInfo.Arguments = "-i " + "\"" + InFile + "\"" + "     -b 700k     -ar 48000 -y  " + "\"" + OutFile + "\"";
            proc.StartInfo.Arguments = "-i " + "\"" + InFile + "\"" + AudioScript+ " -vf \"movie=logo.png [watermark]; [in][watermark] overlay=50:50 [out]\"   -b 900k -s 720x576   -ar 48000 -ab 192k -async 1 " + "   -y  " + "\"" + OutFile + "\"";

            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.EnableRaisingEvents = true;
            proc.Exited += new EventHandler(myProcess_Exited);
            if (!proc.Start())
            {
                return false;
            }

            richTextBox2.Text += "Convert Started : " + InFile + "\n";
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
            Application.DoEvents();



            proc.PriorityClass = ProcessPriorityClass.Normal;
            StreamReader reader = proc.StandardError;
            string line;




            while ((line = reader.ReadLine()) != null)
            {
                if (richTextBox1.Lines.Length > 15)
                {
                    richTextBox1.Text = "";
                }

                FindDuration(line, "1");
                richTextBox1.Text += (line) + " \n";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
                Application.DoEvents();
            }
            proc.Close();
            richTextBox2.Text += "Convert Finished : " + InFile + "\n";
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
            Application.DoEvents();

            return true;
        }
        private void myProcess_Exited(object sender, System.EventArgs e)
        {
            this.Invoke(new MethodInvoker(delegate()
            {
                progressBar1.Value = 100;
                label1.Text = ((progressBar1.Value * 100) / progressBar1.Maximum).ToString() + "%";
                Application.DoEvents();
                //if (dataGridView1.Rows[_RowIndx].Cells[4].Value.ToString() == "Ar")
                //{

                //Random Rdm = new Random();
                //int indx = Rdm.Next(0, progressBar1.Maximum);

                string ImageFileNameSuffix = "";
                if (dataGridView1.Rows[_RowIndx].Cells[4].Value.ToString() == "Ar")
                {
                    ImageFileNameSuffix = "";
                }
                else
                {
                    ImageFileNameSuffix = "_F";
                }
                ThumbGenerate(50, _TempDir + _EpisodeNum + "-Orig" + _Extention, 640, 360, _TempDir + _EpisodeNum + ImageFileNameSuffix + ".png");
                ThumbGenerate(50, _TempDir + _EpisodeNum + "-Orig" + _Extention, 480, 270, _TempDir + _EpisodeNum + ImageFileNameSuffix + "-m.png");
                ThumbGenerate(50, _TempDir + _EpisodeNum + "-Orig" + _Extention, 96, 54, _TempDir + _EpisodeNum + ImageFileNameSuffix + "-l.png");
                ThumbGenerate(50, _TempDir + _EpisodeNum + "-Orig" + _Extention, 178, 110, _TempDir + _EpisodeNum + ImageFileNameSuffix + "-ml.png");

                if (System.IO.File.Exists(_TempDir + _EpisodeNum + ImageFileNameSuffix + ".png"))
                {
                    UploadPng(_EpisodeNum + ImageFileNameSuffix + ".png");
                }
                else
                {
                    richTextBox2.Text += "Could not create Image:" + _EpisodeNum + ImageFileNameSuffix + ".png" + "\n";
                    richTextBox2.SelectionStart = richTextBox2.Text.Length;
                    richTextBox2.ScrollToCaret();
                    Application.DoEvents();
                }




                if (System.IO.File.Exists(_TempDir + _EpisodeNum + ImageFileNameSuffix + "-m.png"))
                {
                    UploadPng(_EpisodeNum + ImageFileNameSuffix + "-m.png");
                }
                else
                {
                    richTextBox2.Text += "Could not create Image:" + _EpisodeNum + ImageFileNameSuffix + "-m.png" + "\n";
                    richTextBox2.SelectionStart = richTextBox2.Text.Length;
                    richTextBox2.ScrollToCaret();
                    Application.DoEvents();
                }
                if (System.IO.File.Exists(_TempDir + _EpisodeNum + ImageFileNameSuffix + "-l.png"))
                {
                    UploadPng(_EpisodeNum + ImageFileNameSuffix + "-l.png");
                }
                else
                {
                    richTextBox2.Text += "Could not create Image:" + _EpisodeNum + ImageFileNameSuffix + "-l.png" + "\n";
                    richTextBox2.SelectionStart = richTextBox2.Text.Length;
                    richTextBox2.ScrollToCaret();
                    Application.DoEvents();
                }



                if (System.IO.File.Exists(_TempDir + _EpisodeNum + ImageFileNameSuffix + "-ml.png"))
                {
                    UploadPng(_EpisodeNum + ImageFileNameSuffix + "-ml.png");
                }
                else
                {
                    richTextBox2.Text += "Could not create Image:" + _EpisodeNum + ImageFileNameSuffix + "-ml.png" + "\n";
                    richTextBox2.SelectionStart = richTextBox2.Text.Length;
                    richTextBox2.ScrollToCaret();
                    Application.DoEvents();
                }




                //    }


                //Start Create Low Q
                string FileNameSuffix = "";
                if (dataGridView1.Rows[_RowIndx].Cells[4].Value.ToString() == "Ar")
                {
                    FileNameSuffix = "";
                }
                else
                {
                    FileNameSuffix = "_F";
                }
                ConvertLow(_TempDir + _EpisodeNum + FileNameSuffix + ".mp4", _TempDir + _EpisodeNum + FileNameSuffix + "_LOW_200.mp4", "200");
                ConvertLow(_TempDir + _EpisodeNum + FileNameSuffix + ".mp4", _TempDir + _EpisodeNum + FileNameSuffix + "_LOW_300.mp4", "300");
                ConvertLow(_TempDir + _EpisodeNum + FileNameSuffix + ".mp4", _TempDir + _EpisodeNum + FileNameSuffix + "_LOW_500.mp4", "500");
                ConvertLow(_TempDir + _EpisodeNum + FileNameSuffix + ".mp4", _TempDir + _EpisodeNum + FileNameSuffix + "_LOW_700.mp4", "700");
                this.Invoke(new MethodInvoker(delegate()
                {
                    Thread thread2 = new Thread(() => UploadVideoLow(_TempDir + _EpisodeNum + FileNameSuffix + "_LOW_200.mp4", _EpisodeNum + FileNameSuffix + "_LOW_200.mp4", "3", false));
                    thread2.Start();
                }));
                this.Invoke(new MethodInvoker(delegate()
                {
                    Thread thread2 = new Thread(() => UploadVideoLow(_TempDir + _EpisodeNum + FileNameSuffix + "_LOW_300.mp4", _EpisodeNum + FileNameSuffix + "_LOW_300.mp4", "4", false));
                    thread2.Start();
                }));
                this.Invoke(new MethodInvoker(delegate()
                {
                    Thread thread2 = new Thread(() => UploadVideoLow(_TempDir + _EpisodeNum + FileNameSuffix + "_LOW_500.mp4", _EpisodeNum + FileNameSuffix + "_LOW_500.mp4", "5", false));
                    thread2.Start();
                }));
                this.Invoke(new MethodInvoker(delegate()
                {
                    Thread thread2 = new Thread(() => UploadVideoLow(_TempDir + _EpisodeNum + FileNameSuffix + "_LOW_700.mp4", _EpisodeNum + FileNameSuffix + "_LOW_700.mp4", "6", false));
                    thread2.Start();
                }));


                UploadVideoLow(_TempDir + _EpisodeNum + FileNameSuffix + ".mp4", _EpisodeNum + FileNameSuffix + ".mp4", "2", true);

                try
                {
                    System.IO.File.Delete(_TempDir + _EpisodeNum + "-Orig" + _Extention);
                }
                catch
                {

                }


            }));


        }
        protected bool ConvertLow(string InFile, string OutFile, string Bitrate)
        {
            progressBar1.Value = 0;
            label1.Text = "0%";

            Process proc = new Process(); if (Environment.Is64BitOperatingSystem)
            {
                proc.StartInfo.FileName = Path.GetDirectoryName(Application.ExecutablePath) + "\\ffmpeg64";
            }
            else
            {
                proc.StartInfo.FileName = Path.GetDirectoryName(Application.ExecutablePath) + "\\ffmpeg32";
            }

            proc.StartInfo.Arguments = "-i " + "\"" + InFile + "\"" + "   -b " + Bitrate + "k     -y  " + "\"" + OutFile + "\"";
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.EnableRaisingEvents = true;
            if (!proc.Start())
            {
                return false;
            }

            richTextBox2.Text += "Convert Started LOW : " + Bitrate + " >> " + InFile + "\n";
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
            Application.DoEvents();



            //  proc.PriorityClass = ProcessPriorityClass.Normal;
            StreamReader reader = proc.StandardError;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (richTextBox1.Lines.Length > 15)
                {
                    richTextBox1.Text = "";
                }
                FindDuration(line, "1");
                richTextBox1.Text += (line) + " \n";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
                Application.DoEvents();
            }
            proc.Close();
            richTextBox2.Text += "Convert Finished LOW : " + Bitrate + " >> " + InFile + "\n";
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
            Application.DoEvents();

            return true;
        }
        protected void FindDuration(string Str, string ProgressControl)
        {
            string TimeCode = "";
            if (Str.Contains("Duration:"))
            {
                TimeCode = Str.Substring(Str.IndexOf("Duration: "), 21).Replace("Duration: ", "").Trim();
                string[] Times = TimeCode.Split('.')[0].Split(':');
                double Frames = double.Parse(Times[0].ToString()) * (3600) * (25) +
                    double.Parse(Times[1].ToString()) * (60) * (25) +
                    double.Parse(Times[2].ToString()) * (25);

                switch (ProgressControl)
                {
                    case "1": progressBar1.Maximum = int.Parse(Frames.ToString());
                        break;
                    case "2": progressBar2.Maximum = int.Parse(Frames.ToString());
                        break;
                    case "3": progressBar3.Maximum = int.Parse(Frames.ToString());
                        break;
                    case "4": progressBar4.Maximum = int.Parse(Frames.ToString());
                        break;
                    case "5": progressBar5.Maximum = int.Parse(Frames.ToString());
                        break;
                    case "6": progressBar6.Maximum = int.Parse(Frames.ToString());
                        break;

                    default:
                        break;
                }

            }
            if (Str.Contains("time="))
            {
                try
                {
                    string CurTime = "";
                    CurTime = Str.Substring(Str.IndexOf("time="), 16).Replace("time=", "").Trim();
                    string[] CTimes = CurTime.Split('.')[0].Split(':');
                    double CurFrame = double.Parse(CTimes[0].ToString()) * (3600) * (25) +
                        double.Parse(CTimes[1].ToString()) * (60) * (25) +
                        double.Parse(CTimes[2].ToString()) * (25);


                    switch (ProgressControl)
                    {
                        case "1": progressBar1.Value = int.Parse(CurFrame.ToString());

                            label1.Text = ((progressBar1.Value * 100) / progressBar1.Maximum).ToString() + "%";
                            break;
                        case "2": progressBar2.Value = int.Parse(CurFrame.ToString());

                            label3.Text = ((progressBar2.Value * 100) / progressBar2.Maximum).ToString() + "%";
                            break;
                        case "3":
                            progressBar3.Value = int.Parse(CurFrame.ToString());
                            label7.Text = ((progressBar3.Value * 100) / progressBar3.Maximum).ToString() + "%";
                            break;

                        case "4":
                            progressBar4.Value = int.Parse(CurFrame.ToString());
                            label11.Text = ((progressBar4.Value * 100) / progressBar4.Maximum).ToString() + "%";
                            break;

                        case "5":
                            progressBar5.Value = int.Parse(CurFrame.ToString());
                            label13.Text = ((progressBar5.Value * 100) / progressBar5.Maximum).ToString() + "%";
                            break;


                        case "6":
                            progressBar6.Value = int.Parse(CurFrame.ToString());
                            label15.Text = ((progressBar6.Value * 100) / progressBar6.Maximum).ToString() + "%";
                            break;

                        default:
                            break;
                    }


                    //if (ProgressControl == "1")
                    //{
                    //    progressBar1.Value = int.Parse(CurFrame.ToString());

                    //    label1.Text = ((progressBar1.Value * 100) / progressBar1.Maximum).ToString() + "%";
                    //}
                    //else
                    //{

                    //}

                    //label3.Text = CurFrame.ToString();
                    Application.DoEvents();
                }
                catch
                {


                }
            }
            if (Str.Contains("fps="))
            {

                string Speed = "";

                Speed = Str.Substring(Str.IndexOf("fps="), 8).Replace("fps=", "").Trim();

                label4.Text = "Speed: " + (float.Parse(Speed) / 25).ToString() + " X ";
                Application.DoEvents();

            }
        }
        //protected void UpDateSite()
        //{

        //    try
        //    {
        //        string FileNameSuffix = "";
        //        if (dataGridView1.Rows[_RowIndx].Cells[4].Value.ToString() == "Ar")
        //        {
        //            FileNameSuffix = "";
        //        }
        //        else
        //        {
        //            FileNameSuffix = "_F";
        //        }
        //        var ftpWebRequest = (FtpWebRequest)WebRequest.Create(System.Configuration.ConfigurationSettings.AppSettings["Server"].Trim() +
        //       "/" + _ProgId.Trim() + "/" + _EpisodeNum.Trim()+FileNameSuffix + ".mp4");

        //        ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile;
        //        ftpWebRequest.Credentials = new NetworkCredential(System.Configuration.ConfigurationSettings.AppSettings["UserName"].Trim(),
        //            System.Configuration.ConfigurationSettings.AppSettings["PassWord"].Trim());
        //        ftpWebRequest.UsePassive = true;
        //        ftpWebRequest.UseBinary = true;
        //        ftpWebRequest.KeepAlive = false;
        //        //  ftpWebRequest. = 10000;
        //        using (var inputStream = File.OpenRead(_TempDir + _EpisodeNum + ".mp4"))
        //        using (var outputStream = ftpWebRequest.GetRequestStream())
        //        {
        //            var buffer = new byte[32 * 1024];
        //            int totalReadBytesCount = 0;
        //            int readBytesCount;
        //            //while ((readBytesCount = inputStream.Read(buffer, 0, buffer.Length)) > 0)
        //            //{
        //            //    outputStream.Write(buffer, 0, readBytesCount);
        //            //    totalReadBytesCount += readBytesCount;
        //            //    var progress = totalReadBytesCount * 100.0 / inputStream.Length;
        //            //    progressBar1.Value = (int)Math.Ceiling(double.Parse(progress.ToString()));
        //            //    label1.Text = progress.ToString() + "%";
        //            //    Application.DoEvents();
        //            //}
        //            long length = inputStream.Length;
        //            long bfr = 0;
        //            progressBar1.Maximum = 100;
        //            richTextBox2.Text += "Start Upload To Ftp: " + ftpWebRequest.RequestUri.ToString() + "\n";
        //            richTextBox2.SelectionStart = richTextBox2.Text.Length;
        //            richTextBox2.ScrollToCaret();
        //            Application.DoEvents();
        //            while ((readBytesCount = inputStream.Read(buffer, 0, buffer.Length)) > 0)
        //            {
        //                bfr += readBytesCount;
        //                int Pr = (int)Math.Ceiling(double.Parse(((bfr * 100) / length).ToString()));
        //                progressBar1.Value = Pr;
        //                outputStream.Write(buffer, 0, readBytesCount);
        //                label1.Text = Pr.ToString() + "%";
        //                Application.DoEvents();
        //            }
        //        }
        //        richTextBox2.Text += "End Upload To Ftp: " + ftpWebRequest.RequestUri.ToString() + "\n";
        //        richTextBox2.SelectionStart = richTextBox2.Text.Length;
        //        richTextBox2.ScrollToCaret();
        //        Application.DoEvents();
        //        richTextBox2.Text += "Delete File: " + _TempDir + _EpisodeNum + "-Orig" + _Extention + "\n";
        //        richTextBox2.SelectionStart = richTextBox2.Text.Length;
        //        richTextBox2.ScrollToCaret();
        //        Application.DoEvents();
        //        File.Delete(_TempDir + _EpisodeNum + "-Orig" + _Extention);
        //        File.Delete(_TempDir + _EpisodeNum + ".mp4");
        //        richTextBox2.Text += "Delete File: " + _TempDir + _EpisodeNum + ".mp4" + "\n";
        //        richTextBox2.SelectionStart = richTextBox2.Text.Length;
        //        richTextBox2.ScrollToCaret();
        //        Application.DoEvents();
        //        dataGridView1.Rows[_RowIndx].Cells[3].Value = "Done";
        //        richTextBox2.Text += "Task Finished: " + _TempDir + _EpisodeNum + ".mp4" + "\n";
        //        richTextBox2.SelectionStart = richTextBox2.Text.Length;
        //        richTextBox2.ScrollToCaret();
        //        Application.DoEvents();

        //        if (dataGridView1.Rows[_RowIndx].Cells[4].Value.ToString() == "Ar")
        //        {
        //            richTextBox2.Text += "Start Update  WebSite " + ftpWebRequest.RequestUri.ToString() + "\n";
        //            richTextBox2.SelectionStart = richTextBox2.Text.Length;
        //            richTextBox2.ScrollToCaret();
        //            Application.DoEvents();

        //            WebBrowser Wbr = new WebBrowser();
        //            Wbr.Navigate("shahid.ifilmtv.ir/query/updateepisodenumber/" + _ProgId + "/?episode=" + _EpisodeNum);
        //            richTextBox2.Text += "End Update  WebSite " + ftpWebRequest.RequestUri.ToString() + "\n";
        //            richTextBox2.SelectionStart = richTextBox2.Text.Length;
        //            richTextBox2.ScrollToCaret();
        //            Application.DoEvents();
        //        }

        //        button1_Click(new object(), new EventArgs());
        //    }
        //    catch (Exception Exp)
        //    {
        //        Application.DoEvents();
        //        richTextBox2.Text += "Error Upload To Ftp: " + _TempDir + _EpisodeNum + ".mp4" + "\n";
        //        richTextBox2.Text += Exp.Message + "\n";
        //        richTextBox2.SelectionStart = richTextBox2.Text.Length;
        //        richTextBox2.ScrollToCaret();
        //        Application.DoEvents();
        //        Thread.Sleep(5000);
        //        UploadVideo();
        //    }

        //}
        protected void ThumbGenerate(double TimeSec, string FileName, int Width, int Height, string ImageFileName)
        {
            //  System.Diagnostics.Process.Start(_DirPath);
            double SelectedTime = TimeSec;
            SelectedTime = Math.Round((SelectedTime * 25));
            Process proc = new Process();
            if (Environment.Is64BitOperatingSystem)
            {
                proc.StartInfo.FileName = Path.GetDirectoryName(Application.ExecutablePath) + "\\ffmpeg64";
            }
            else
            {
                proc.StartInfo.FileName = Path.GetDirectoryName(Application.ExecutablePath) + "\\ffmpeg32";
            }
            proc.StartInfo.Arguments = "-i " + "\"" + FileName + "\"" + " -filter:v select=\"eq(n\\," + SelectedTime.ToString() + ")\",scale=" + Width + ":" + Height + ",crop=iw:" + Height + " -vframes 1  -y    \"" + ImageFileName + "\"";
          
              proc.StartInfo.Arguments =" -filter:v select="eq(n\\,"20")",scale="850":"480",crop=iw:850 -vframes 1 ";
            proc.StartInfo.RedirectStandardError = true;
            
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.Exited += new EventHandler(Thumb_Exited);
            if (!proc.Start())
            {
                richTextBox1.Text += " \n" + "Error starting";
                return;
            }
            StreamReader reader = proc.StandardError;
            string line;
            richTextBox2.Text += "Start create Image: " + ImageFileName + "\n";
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
            Application.DoEvents();
            while ((line = reader.ReadLine()) != null)
            {
                if (richTextBox1.Lines.Length > 15)
                {
                    richTextBox1.Text = "";
                }

                richTextBox1.Text += (line) + " \n";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
                Application.DoEvents();
            }
            proc.Close();
            richTextBox2.Text += "End Create Image: " + ImageFileName + "\n";
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
            Application.DoEvents();
        }
        private void Thumb_Exited(object sender, System.EventArgs e)
        {
            this.Invoke(new MethodInvoker(delegate()
            {


            }));
        }
        protected void UploadPng(string FileName)
        {
            try
            {
                var ftpWebRequest = (FtpWebRequest)WebRequest.Create(System.Configuration.ConfigurationSettings.AppSettings["Server"].Trim() +
              "/" + dataGridView1.Rows[_RowIndx].Cells[1].Value.ToString().Trim() + "/" + FileName);

                ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile;
                ftpWebRequest.Credentials = new NetworkCredential(System.Configuration.ConfigurationSettings.AppSettings["UserName"].Trim(),
                    System.Configuration.ConfigurationSettings.AppSettings["PassWord"].Trim());
                ftpWebRequest.UsePassive = false;
                ftpWebRequest.UseBinary = true;
                ftpWebRequest.KeepAlive = false;
                //  ftpWebRequest. = 10000;
                using (var inputStream = System.IO.File.OpenRead(_TempDir + FileName))
                using (var outputStream = ftpWebRequest.GetRequestStream())
                {
                    var buffer = new byte[32 * 1024];
                    int totalReadBytesCount = 0;
                    int readBytesCount;
                    //while ((readBytesCount = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                    //{
                    //    outputStream.Write(buffer, 0, readBytesCount);
                    //    totalReadBytesCount += readBytesCount;
                    //    var progress = totalReadBytesCount * 100.0 / inputStream.Length;
                    //    progressBar1.Value = (int)Math.Round( double.Parse(progress.ToString()));
                    //    label1.Text = ((progressBar1.Value * 100) / progressBar1.Maximum).ToString() + "%";
                    //    Application.DoEvents();
                    //}
                    long length = inputStream.Length;
                    long bfr = 0;
                    progressBar1.Maximum = 100;
                    richTextBox2.Text += "Start Upload Image: " + ftpWebRequest.RequestUri.ToString() + "\n";
                    richTextBox2.SelectionStart = richTextBox2.Text.Length;
                    richTextBox2.ScrollToCaret();
                    Application.DoEvents();
                    while ((readBytesCount = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bfr += readBytesCount;
                        int Pr = (int)Math.Ceiling(double.Parse(((bfr * 100) / length).ToString()));
                        progressBar1.Value = Pr;
                        outputStream.Write(buffer, 0, readBytesCount);
                        label1.Text = Pr.ToString() + "%";
                        Application.DoEvents();
                    }
                    richTextBox2.Text += "End Upload Image: " + ftpWebRequest.RequestUri.ToString() + "\n";
                    richTextBox2.SelectionStart = richTextBox2.Text.Length;
                    richTextBox2.ScrollToCaret();
                    Application.DoEvents();
                }

                try
                {
                    System.IO.File.Delete(_TempDir + FileName);
                    richTextBox2.Text += "Delete Local Image: " + _TempDir + FileName + "\n";
                    richTextBox2.SelectionStart = richTextBox2.Text.Length;
                    richTextBox2.ScrollToCaret();
                    Application.DoEvents();
                }
                catch
                {

                }

            }
            catch (Exception Exp)
            {
                Application.DoEvents();
                richTextBox2.Text += "Error Upload To Ftp: " + _TempDir + FileName + "\n";
                richTextBox2.Text += Exp.Message + "\n";
                richTextBox2.SelectionStart = richTextBox2.Text.Length;
                richTextBox2.ScrollToCaret();
                Application.DoEvents();
                Thread.Sleep(5000);
                UploadPng(FileName);
            }

        }
        private void UploadVideoLow(string InFile, string FileName, string ProgressControl, bool UpdateSite)
        {
            try
            {

                var ftpWebRequest = (FtpWebRequest)WebRequest.Create(System.Configuration.ConfigurationSettings.AppSettings["Server"].Trim() +
              "/" + dataGridView1.Rows[_RowIndx].Cells[1].Value.ToString().Trim() + "/" + FileName);

                ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile;
                ftpWebRequest.Credentials = new NetworkCredential(System.Configuration.ConfigurationSettings.AppSettings["UserName"].Trim(),
                    System.Configuration.ConfigurationSettings.AppSettings["PassWord"].Trim());
                ftpWebRequest.UsePassive = false;
                ftpWebRequest.UseBinary = true;
                ftpWebRequest.KeepAlive = false;
                //  ftpWebRequest. = 10000;
                using (var inputStream = System.IO.File.OpenRead(InFile))
                using (var outputStream = ftpWebRequest.GetRequestStream())
                {
                    var buffer = new byte[32 * 1024];
                    int totalReadBytesCount = 0;
                    int readBytesCount;
                    //while ((readBytesCount = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                    //{
                    //    outputStream.Write(buffer, 0, readBytesCount);
                    //    totalReadBytesCount += readBytesCount;
                    //    var progress = totalReadBytesCount * 100.0 / inputStream.Length;
                    //    progressBar1.Value = (int)Math.Round(double.Parse(progress.ToString()));
                    //    label1.Text = ((progressBar1.Value * 100) / progressBar1.Maximum).ToString() + "%";
                    //    Application.DoEvents();
                    //}
                    long length = inputStream.Length;
                    long bfr = 0;
                    switch (ProgressControl)
                    {
                        case "1": progressBar1.Maximum = 100;
                            break;
                        case "2": progressBar2.Maximum = 100;
                            break;
                        case "3": progressBar3.Maximum = 100;
                            break;
                        case "4": progressBar4.Maximum = 100;
                            break;
                        case "5": progressBar5.Maximum = 100;
                            break;
                        case "6": progressBar6.Maximum = 100;
                            break;
                        default:
                            break;
                    }
                    //richTextBox2.Text += "Start Upload : " + ftpWebRequest.RequestUri.ToString() + "\n";
                    //richTextBox2.SelectionStart = richTextBox2.Text.Length;
                    //richTextBox2.ScrollToCaret();
                    //Application.DoEvents();
                    while ((readBytesCount = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bfr += readBytesCount;
                        int Pr = (int)Math.Ceiling(double.Parse(((bfr * 100) / length).ToString()));

                        switch (ProgressControl)
                        {
                            case "1":
                                //  progressBar1.Value = Pr;
                                // label1.Text = Pr.ToString() + "%";
                                UpdateText(label1, Pr.ToString() + "%");
                                UpdateValue(progressBar1, Pr);
                                break;
                            case "2":
                                // progressBar2.Value = Pr;
                                // label3.Text = Pr.ToString() + "%";
                                UpdateText(label3, Pr.ToString() + "%");
                                UpdateValue(progressBar2, Pr);
                                break;
                            case "3":
                                //   progressBar3.Value = Pr;
                                //label7.Text = Pr.ToString() + "%";
                                UpdateText(label7, Pr.ToString() + "%");
                                UpdateValue(progressBar3, Pr);
                                break;

                            case "4":
                                UpdateText(label11, Pr.ToString() + "%");
                                UpdateValue(progressBar4, Pr);
                                break;
                            case "5":
                                UpdateText(label13, Pr.ToString() + "%");
                                UpdateValue(progressBar5, Pr);
                                break;
                            case "6":
                                UpdateText(label15, Pr.ToString() + "%");
                                UpdateValue(progressBar6, Pr);
                                break;
                            default:
                                break;
                        }
                        outputStream.Write(buffer, 0, readBytesCount);

                        Application.DoEvents();
                    }
                    //richTextBox2.Text += "End Upload : " + ftpWebRequest.RequestUri.ToString() + "\n";
                    //richTextBox2.SelectionStart = richTextBox2.Text.Length;
                    //richTextBox2.ScrollToCaret();
                    //Application.DoEvents();
                }
                try
                {
                    System.IO.File.Delete(_TempDir + FileName);
                }
                catch
                {

                }

                //richTextBox2.Text += "Delete Local : " + _TempDir + FileName + "\n";
                //richTextBox2.SelectionStart = richTextBox2.Text.Length;
                //richTextBox2.ScrollToCaret();
                //Application.DoEvents();



                if (UpdateSite)
                {

                }
            }
            catch (Exception Exp)
            {
                this.Invoke(new MethodInvoker(delegate()
                     {

                         //Application.DoEvents();
                         richTextBox2.Text += "Error Upload To Ftp: " + _TempDir + FileName + "\n";
                         richTextBox2.Text += Exp.Message + "\n";
                         richTextBox2.SelectionStart = richTextBox2.Text.Length;
                         richTextBox2.ScrollToCaret();
                         UploadVideoLow(InFile, FileName, ProgressControl, UpdateSite);
                         //Application.DoEvents();
                         //Thread.Sleep(5000);
                         // UploadVideoLow(InFile, FileName, ProgressControl, UpdateSite);
                     }));
            }



        }
        private void UpdateText(Label label, string text)
        {
            // If the current thread is not the UI thread, InvokeRequired will be true
            if (label.InvokeRequired)
            {
                // If so, call Invoke, passing it a lambda expression which calls
                // UpdateText with the same label and text, but on the UI thread instead.
                label.Invoke((Action)(() => UpdateText(label, text)));
                return;
            }
            // If we're running on the UI thread, we'll get here, and can safely update 
            // the label's text.
            label.Text = text;
        }
        private void UpdateValue(ProgressBar ProgBar, int Value)
        {
            // If the current thread is not the UI thread, InvokeRequired will be true
            if (ProgBar.InvokeRequired)
            {
                // If so, call Invoke, passing it a lambda expression which calls
                // UpdateText with the same label and text, but on the UI thread instead.
                ProgBar.Invoke((Action)(() => UpdateValue(ProgBar, Value)));
                return;
            }
            // If we're running on the UI thread, we'll get here, and can safely update 
            // the label's text.
            ProgBar.Value = Value;
        }
        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 0)
            {
                if (textBox2.Text.Length > 0)
                {
                    if (textBox4.Text.Length > 0)
                    {
                        string Lang = "Ar";
                        if (radioButton2.Checked)
                        {
                            Lang = "Fa";
                        }
                        this.dataGridView1.Rows.Add(textBox1.Text, textBox2.Text.Trim(), textBox4.Text.Trim(), "Waiting", Lang);
                    }
                }
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                if (dataGridView1.SelectedRows[0].Cells[3].Value.ToString() == "Waiting")
                {
                    this.dataGridView1.Rows.RemoveAt(dataGridView1.SelectedRows[0].Index);
                }
            }
        }
        protected int QeueProcess()
        {
            int Index = -1;
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                if (dataGridView1.Rows[i].Cells[3].Value.ToString() == "Waiting")
                {
                    Index = i;
                    return Index;
                }
            }
            return Index;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (progressBar2.Value == progressBar2.Maximum &&
                progressBar3.Value == progressBar3.Maximum &&
                progressBar4.Value == progressBar4.Maximum &&
                progressBar5.Value == progressBar5.Maximum &&
                progressBar6.Value == progressBar6.Maximum)
            {
                progressBar2.Value = 0;
                progressBar3.Value = 0;
                progressBar4.Value = 0;
                progressBar5.Value = 0;
                progressBar6.Value = 0;
                this.Invoke(new MethodInvoker(delegate()
                {
                    try
                    {
                        System.IO.File.Delete(_TempDir + _EpisodeNum + "-Orig" + _Extention);
                        System.IO.File.Delete(_TempDir + _EpisodeNum + ".mp4");
                    }
                    catch
                    {


                    }

                    richTextBox2.Text += "Delete File: " + _TempDir + _EpisodeNum + ".mp4" + "\n";
                    richTextBox2.SelectionStart = richTextBox2.Text.Length;
                    richTextBox2.ScrollToCaret();
                    Application.DoEvents();
                    dataGridView1.Rows[_RowIndx].Cells[3].Value = "Done";
                    if (dataGridView1.Rows[_RowIndx].Cells[4].Value.ToString() == "Ar")
                    {
                        richTextBox2.Text += "Start Update  WebSite Arbic " + _TempDir + _EpisodeNum + ".mp4" + "\n";
                        richTextBox2.SelectionStart = richTextBox2.Text.Length;
                        richTextBox2.ScrollToCaret();
                        Application.DoEvents();

                        WebBrowser Wbr = new WebBrowser();
                        Wbr.Navigate("shahid.ifilmtv.ir/query/updateepisodenumber/" + _ProgId + "/?episode=" + _EpisodeNum);
                        richTextBox2.Text += "End Update  WebSite Arbic" + _TempDir + _EpisodeNum + ".mp4" + "\n";
                        richTextBox2.SelectionStart = richTextBox2.Text.Length;
                        richTextBox2.ScrollToCaret();
                        Application.DoEvents();
                    }
                    else
                    {
                        if (dataGridView1.Rows[_RowIndx].Cells[4].Value.ToString() == "Fa")
                        {
                            richTextBox2.Text += "Start Update  WebSite Farsi " + _TempDir + _EpisodeNum + ".mp4" + "\n";
                            richTextBox2.SelectionStart = richTextBox2.Text.Length;
                            richTextBox2.ScrollToCaret();
                            Application.DoEvents();

                            WebBrowser Wbr = new WebBrowser();
                            Wbr.Navigate("shahid.ifilmtv.ir/query/updateepisodenumberfa/" + _ProgId + "/?episode=" + _EpisodeNum);
                            richTextBox2.Text += "End Update  WebSite Farsi" + _TempDir + _EpisodeNum + ".mp4" + "\n";
                            richTextBox2.SelectionStart = richTextBox2.Text.Length;
                            richTextBox2.ScrollToCaret();
                            Application.DoEvents();
                        }
                    }

                    button1_Click(new object(), new EventArgs());
                }));
            }
        }
    }
}
