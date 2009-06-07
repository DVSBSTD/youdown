using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Threading;
using Path=System.IO.Path;

namespace YouDown
{
    public partial class MainWindow : Window
    {
        List<string> addresses = new List<string>();
        int videoCount;
        List<Video> videos = new List<Video>();
        WebClient cl;

        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Process(object file)
        {
            UpdateStatus("Processing list...");
            UpdateProgress(0);

            string[] lines = File.ReadAllLines((string)file, Encoding.UTF8);
            foreach (var line in lines)
            {
                if (line.Contains("youtube.com"))
                {
                    addresses.Add(line.Trim());
                }
            }

            SetMaximum(addresses.Count*2);

            if (addresses.Count > 0)
            {
                int finished = 0;

                foreach (var add in addresses)
                {
                    finished++;

                    UpdateStatus(String.Format("Retrieving URL {0}/{1}", finished, addresses.Count));
                    AddProgress();
                    var target = Target.Fetch(add);

                    if (target.Id != null)
                    {
                        videos.Add(target);
                    }
                }

                SetMaximum(videos.Count*2);
                UpdateProgress(videos.Count);
            }
            else
            {
                Reset();
            }

            if (videos.Count > 0)
            {
                videoCount = videos.Count;

                if (cl == null) cl = new WebClient();
                cl.DownloadFileCompleted += DownloadFileCompleted;
                cl.DownloadProgressChanged += DownloadProgressChanged;

                DownloadFileCompleted(this, null);
            }
            else
            {
                Reset();
            }
        }

        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            UpdateDown(e.ProgressPercentage);
        }

        void DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (videos.Count > 0)
            {
                var vid = videos.First();

                UpdateStatus(String.Format("Downloading video {0}/{1} - {2}", videoCount - videos.Count + 1, videoCount,
                                           vid.Title));
                AddProgress();

                string fmt = "&fmt=22";
                string ext = ".hd.mp4";

                try
                {
                    WebRequest.Create(
                        String.Format("http://www.youtube.com/get_video?video_id={0}&l={1}&t={2}{3}", vid.Id,
                                      vid.Secret1, vid.Secret2, fmt)).GetResponse();
                } 
                catch (WebException)
                {
                    fmt = "&fmt=18";
                    ext = ".hq.mp4";

                    try
                    {
                        WebRequest.Create(
                            String.Format("http://www.youtube.com/get_video?video_id={0}&l={1}&t={2}{3}", vid.Id,
                                          vid.Secret1, vid.Secret2, fmt)).GetResponse();
                    }
                    catch (WebException)
                    {
                        fmt = "";
                        ext = ".flv";
                    }
                }

                var uri =
                        new Uri(String.Format("http://www.youtube.com/get_video?video_id={0}&l={1}&t={2}{3}",
                                              vid.Id,
                                              vid.Secret1, vid.Secret2, fmt));

                cl.DownloadFileAsync(uri, SanitizePath(vid.Title) + ext);

                videos.Remove(vid);
            }
            else
            {
                Reset();
            }
        }

        private string SanitizePath(string title)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < title.Length; i++)
            {
                char filenameChar = title[i];
                foreach (char c in Path.GetInvalidFileNameChars())
                    if (filenameChar.Equals(c))
                    {
                        filenameChar = ' ';
                        break;
                    }

                sb.Append(filenameChar);
            }

            return sb.ToString();
        }

        private void UpdateStatus(string status)
        {
            if (Thread.CurrentThread != Dispatcher.Thread)
                Dispatcher.Invoke(DispatcherPriority.Normal,
                    (ThreadStart)(() => UpdateStatus(status)));
            else
            {
                StatusLabel.Content = status;
            }
        }

        private void UpdateProgress(double status)
        {
            if (Thread.CurrentThread != Dispatcher.Thread)
                Dispatcher.Invoke(DispatcherPriority.Normal,
                                  (ThreadStart) (() => UpdateProgress(status)));
            else
            {
                Progress.Value = status;
            }
        }

        private void UpdateDown(double status)
        {
            if (Thread.CurrentThread != Dispatcher.Thread)
                Dispatcher.Invoke(DispatcherPriority.Normal,
                                  (ThreadStart)(() => UpdateDown(status)));
            else
            {
                ProgressDown.Value = status;
            }
        }

        private void AddProgress()
        {
            if (Thread.CurrentThread != Dispatcher.Thread)
                Dispatcher.Invoke(DispatcherPriority.Normal,
                                  (ThreadStart)(AddProgress));
            else
            {
                Progress.Value++;
            }
        }

        private void SetMaximum(double max)
        {
            if (Thread.CurrentThread != Dispatcher.Thread)
                Dispatcher.Invoke(DispatcherPriority.Normal,
                    (ThreadStart)(() => SetMaximum(max)));
            else
            {
                Progress.Maximum = max;
            }
        }

        private void Reset()
        {
            if (Thread.CurrentThread != Dispatcher.Thread)
                Dispatcher.Invoke(DispatcherPriority.Normal,
                    (ThreadStart)(Reset));
            else
            {
                addresses = new List<string>();
                videoCount = 0;
                videos = new List<Video>();

                UpdateStatus("Done");
                SetMaximum(100);
                UpdateProgress(100);

                FileText.Text = "Filename";
                RunButton.IsEnabled = true;
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.FileName = "List";
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text documents (.txt)|*.txt|All Files|*.*";

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                FileText.Text = dlg.FileName;
            }
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(FileText.Text))
            {
                ((Button) sender).IsEnabled = false;
                ParameterizedThreadStart process = Process;
                new Thread(process).Start(FileText.Text);
            }
            else
            {
                MessageBox.Show("No file at that location");
            }
        }     

        private void FileText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = (TextBox) sender;

            if (box.Text != "Filename")
            {
                box.BorderThickness = new Thickness(2);

                if (File.Exists(box.Text))
                {
                    box.BorderBrush = Brushes.Green;
                }
                else
                {
                    box.BorderBrush = Brushes.Red;
                }
            }
            else
            {
                box.BorderThickness = new Thickness(1);
                box.BorderBrush = Brushes.Black;
            }
        }
    }
}
