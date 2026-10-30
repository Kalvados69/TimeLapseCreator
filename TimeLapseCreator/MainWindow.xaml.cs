using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TimeLapseCreator
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Stopwatch sw = new Stopwatch();
        public TimelapseProject timelapse;

        public MainWindow()
        {
            InitializeComponent();

            timelapse = new TimelapseProject();
            DataContext = timelapse;
        }

        private void btnGetSourcePath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Rechercher le dossier";
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = tbSourcePath.Text;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = tbSourcePath.Text;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                //tbSourcePath.Text = dlg.FileName;   // A RECOMMENTER
                timelapse.SourcePath = dlg.FileName;
            }
            RefreshPreview(true);
        }

        public void RefreshPreview(bool reloadImage)
        {
            if (timelapse?.SourcePath != null)
            {
                lblRenderingInfo.Content = string.Empty;
                pbRendering.Value = 0;

                FileInfo[] images = timelapse.SourceImages;

                int sourceImagesCount = images.Count();

                if (reloadImage)
                {
                    if (sourceImagesCount > 0)
                    {
                        imgPreview.Source = new BitmapImage(new Uri(images[0].FullName));
                    }
                    else
                    {
                        imgPreview.Source = null;
                    }
                }

                int imageUsageRate;
                int framerate;
                int usedImagesCount = 0;
                double estimatedTimelapseDuration = 0;

                if (int.TryParse(tbImageUsageRate.Text, out imageUsageRate))
                {
                    if (int.TryParse(tbFramerate.Text, out framerate))
                    {
                        usedImagesCount = (int)Math.Truncate((double)sourceImagesCount / imageUsageRate);
                        estimatedTimelapseDuration = Math.Round((double)usedImagesCount / framerate, 1);
                    }
                }

                //lblDetectedImagesNumber.Content = sourceImagesCount;                        // A RECOMMENTER
                //lblUsedImagesNumber.Content = usedImagesCount;                              // A RECOMMENTER
                //lblOutputVideoEstimatedDurationNumber.Content = estimatedTimelapseDuration; // A RECOMMENTER
            }
        }

        private void TimelapseParametersChanged(object sender, TextChangedEventArgs e)
        {
            RefreshPreview(false);
        }

        private void btnRenderTimelapse_Click(object sender, RoutedEventArgs e)
        {
            //DirectoryInfo sourcePathDirectory = new DirectoryInfo(timelapse.SourcePath);
            //FileInfo[] images = GetImageFiles(sourcePathDirectory).ToArray();
            //images.OrderBy(fi => fi.CreationTime);

            //int sourceImagesCount = images.Count();

            //int imageUsageRate;
            //int framerate;
            //int usedImagesCount = 0;
            //double estimatedTimelapseDuration = 0;

            //if (int.TryParse(tbImageUsageRate.Text, out imageUsageRate))
            //{
            //    if (int.TryParse(tbFramerate.Text, out framerate))
            //    {
            //        //sw.Restart();   // A RECOMMENTER

            //        pbRendering.Minimum = 0;
            //        pbRendering.Maximum = usedImagesCount;

            //        usedImagesCount = (int)Math.Truncate((double)sourceImagesCount / imageUsageRate);
            //        estimatedTimelapseDuration = Math.Round((double)usedImagesCount / framerate, 1);

            //        // create instance of video writer
            //        VideoFileWriter writer = new VideoFileWriter();

            //        for (int i = 0; i < sourceImagesCount; i += imageUsageRate)
            //        {
            //            // create a bitmap to save into the video file
            //            using (Bitmap image = new Bitmap(images[i].FullName))
            //            {
            //                // create new video file
            //                if (!writer.IsOpen) writer.Open(System.IO.Path.Combine(sourcePathDirectory.FullName, "EasyTimelapse.mp4"), image.Width, image.Height, framerate, VideoCodec.MPEG4, 50000000);

            //                writer.WriteVideoFrame(image);
            //            }
            //            pbRendering.Value++;
            //        }
            //        writer.Close();

            //        //sw.Stop();  // A RECOMMENTER
            //        //lblRenderingInfo.Content = String.Format($"Rendu terminé en {Math.Truncate((double)sw.ElapsedMilliseconds / 1000)} secondes");  // A RECOMMENTER
            //    }
            //}

            BackgroundRenderTimelapse();  // A DECOMMENTER
        }

        private void BackgroundRenderTimelapse()
        {
            if (string.IsNullOrEmpty(timelapse.SourcePath))
            {
                lblRenderingInfo.Content = "Aucun dossier sélectionné";
                return;
            }
            if (timelapse.Framerate < 1 || timelapse.ImagesUsageRate < 1)
            {
                lblRenderingInfo.Content = "Framerate et taux doivent être ≥ 1";
                return;
            }

            btnRenderTimelapse.IsEnabled = false;

            BackgroundWorker bw = new BackgroundWorker();

            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = false;

            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);

            sw.Restart();

            bw.RunWorkerAsync(timelapse);
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            TimelapseProject bw_timelapse = e.Argument as TimelapseProject;

            DirectoryInfo sourcePathDirectory = new DirectoryInfo(bw_timelapse.SourcePath);
            FileInfo[] images = bw_timelapse.SourceImages;

            int sourceImagesCount = images.Length;
            if (sourceImagesCount == 0) throw new InvalidOperationException("Aucune image JPEG dans le dossier.");

            string outputPath = System.IO.Path.Combine(sourcePathDirectory.FullName, $"{sourcePathDirectory.Name} - Timelapse.mp4");

            if (worker != null && worker.WorkerReportsProgress)
            {
                worker.ReportProgress(-1, sourceImagesCount);
            }

            StringBuilder ffmpegErrors = new StringBuilder();

            using (Process ffmpeg = StartFfmpeg(bw_timelapse, outputPath, ffmpegErrors))
            {
                try
                {
                    // Les JPEG sont envoyés tels quels : ffmpeg les décode, recadre, redimensionne et encode
                    using (Stream stdin = ffmpeg.StandardInput.BaseStream)
                    {
                        for (int i = 0; i < sourceImagesCount; i += bw_timelapse.ImagesUsageRate)
                        {
                            if (worker.CancellationPending == true)
                            {
                                e.Cancel = true;
                                break;
                            }

                            byte[] jpeg = File.ReadAllBytes(images[i].FullName);
                            stdin.Write(jpeg, 0, jpeg.Length);

                            if (worker != null && worker.WorkerReportsProgress)
                            {
                                worker.ReportProgress(i + 1);
                            }
                        }
                    }
                }
                catch (IOException)
                {
                    // ffmpeg a fermé le pipe : l'erreur est remontée via le code de sortie ci-dessous
                }

                ffmpeg.WaitForExit();

                if (ffmpeg.ExitCode != 0)
                {
                    lock (ffmpegErrors)
                    {
                        throw new InvalidOperationException($"ffmpeg a échoué (code {ffmpeg.ExitCode}) : {ffmpegErrors.ToString().Trim()}");
                    }
                }
            }
        }

        private const int H264Crf = 18;         // 18 = quasi transparent ; monter vers 20-23 pour alléger le fichier
        private const string H264Preset = "slow";

        private static Process StartFfmpeg(TimelapseProject project, string outputPath, StringBuilder errors)
        {
            int width = project.OutputWidth;
            int height = project.OutputHeight;

            // Mise à l'échelle pour couvrir le cadre de sortie, puis recadrage centré au ratio choisi.
            // Conversion de la plage pleine JPEG vers la plage vidéo BT.709, mieux gérée par les lecteurs.
            string filters = $"scale={width}:{height}:force_original_aspect_ratio=increase:flags=lanczos:out_range=tv:out_color_matrix=bt709,crop={width}:{height},setsar=1,format=yuv420p";

            string arguments = string.Join(" ",
                "-hide_banner -loglevel error -y",
                $"-f image2pipe -framerate {project.Framerate} -c:v mjpeg -i -",
                $"-vf \"{filters}\"",
                $"-c:v libx264 -preset {H264Preset} -crf {H264Crf} -pix_fmt yuv420p",
                "-color_range tv -colorspace bt709 -color_primaries bt709 -color_trc bt709",
                "-movflags +faststart",
                $"\"{outputPath}\"");

            ProcessStartInfo startInfo = new ProcessStartInfo(FindFfmpeg(), arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
            };

            Process ffmpeg;
            try
            {
                ffmpeg = Process.Start(startInfo);
            }
            catch (Win32Exception)
            {
                throw new FileNotFoundException("ffmpeg.exe introuvable. Placez-le à côté de TimeLapseCreator.exe ou ajoutez-le au PATH.");
            }

            // stderr doit être lu en continu, sinon ffmpeg se bloque quand le tampon est plein
            ffmpeg.ErrorDataReceived += (s, args) =>
            {
                if (args.Data != null) lock (errors) errors.AppendLine(args.Data);
            };
            ffmpeg.BeginErrorReadLine();

            return ffmpeg;
        }

        private static string FindFfmpeg()
        {
            string local = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            return File.Exists(local) ? local : "ffmpeg.exe";
        }

        // This event handler updates the progress.
        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == -1)
                pbRendering.Maximum = Convert.ToInt32(e.UserState);
            else
                pbRendering.Value = e.ProgressPercentage;
        }

        // This event handler deals with the results of the background operation.
        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            sw.Stop();

            if (e.Cancelled == true)
            {
                lblRenderingInfo.Content = "Canceled!";
            }
            else if (e.Error != null)
            {
                lblRenderingInfo.Content = "Error: " + e.Error.Message;
            }
            else
            {
                lblRenderingInfo.Content = String.Format($"Rendu terminé en {Math.Truncate((double)sw.ElapsedMilliseconds / 1000)} secondes");
            }

            btnRenderTimelapse.IsEnabled = true;
        }
    }
}
