using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeLapseCreator
{
    public class TimelapseProject : INotifyPropertyChanged
    {
        private string _sourcePath;
        public string SourcePath
        {
            get { return _sourcePath; }
            set
            {
                if (this._sourcePath != value)
                {
                    this._sourcePath = value;
                    this.FirePropertyChanged("SourcePath");
                    this.FirePropertyChanged("SourceImages");
                    this.FirePropertyChanged("SourceImagesCount");
                    this.FirePropertyChanged("UsedImagesCount");
                    this.FirePropertyChanged("EstimatedTimelapseDuration");
                }
            }
        }

        private int _framerate = 25;
        public int Framerate// { get; set; } = 25;
        {
            get { return _framerate; }
            set
            {
                if (this._framerate != value)
                {
                    this._framerate = value;
                    this.FirePropertyChanged("Framerate");
                    this.FirePropertyChanged("EstimatedTimelapseDuration");
                }
            }
        }

        private int _imageUsageRate = 1;
        public int ImagesUsageRate// { get; set; } = 1;
        {
            get { return _imageUsageRate; }
            set
            {
                if (this._imageUsageRate != value)
                {
                    this._imageUsageRate = value;
                    this.FirePropertyChanged("ImagesUsageRate");
                    this.FirePropertyChanged("UsedImagesCount");
                    this.FirePropertyChanged("EstimatedTimelapseDuration");
                }
            }
        }

        // La sortie est inscrite dans un cadre Full HD
        public const int MaxOutputWidth = 1920;
        public const int MaxOutputHeight = 1080;

        public static CropRatio[] AvailableCropRatios { get; } =
        {
            new CropRatio("16/9", 16, 9),
            new CropRatio("3/2", 3, 2),
            new CropRatio("1/1", 1, 1),
            new CropRatio("2/3", 2, 3),
        };

        private CropRatio _cropRatio = AvailableCropRatios[0];
        public CropRatio CropRatio
        {
            get { return _cropRatio; }
            set
            {
                if (this._cropRatio != value)
                {
                    this._cropRatio = value;
                    this.FirePropertyChanged("CropRatio");
                    this.FirePropertyChanged("OutputWidth");
                    this.FirePropertyChanged("OutputHeight");
                    this.FirePropertyChanged("OutputResolution");
                }
            }
        }

        public int OutputWidth
        {
            get
            {
                if (CropRatio.Horizontal * MaxOutputHeight >= CropRatio.Vertical * MaxOutputWidth) return MaxOutputWidth;

                return RoundToEven((double)MaxOutputHeight * CropRatio.Horizontal / CropRatio.Vertical);
            }
        }

        public int OutputHeight
        {
            get
            {
                if (CropRatio.Horizontal * MaxOutputHeight < CropRatio.Vertical * MaxOutputWidth) return MaxOutputHeight;

                return RoundToEven((double)MaxOutputWidth * CropRatio.Vertical / CropRatio.Horizontal);
            }
        }

        public string OutputResolution
        {
            get { return $"{OutputWidth} x {OutputHeight}"; }
        }

        // H.264 en yuv420p impose des dimensions paires
        private static int RoundToEven(double value)
        {
            return (int)Math.Round(value / 2) * 2;
        }

        public FileInfo[] SourceImages
        {
            get
            {
                if (string.IsNullOrEmpty(SourcePath)) return null;

                var exts = new[] { ".jpg", ".jpeg" };
                DirectoryInfo sourcePathDirectory = new DirectoryInfo(SourcePath);
                return sourcePathDirectory
                        .EnumerateFiles("*.*", SearchOption.TopDirectoryOnly)
                        .Where(file => exts.Any(x => file.Extension.Equals(x, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
            }
        }

        public int SourceImagesCount
        {
            get
            {
                if (SourceImages == null) return 0;

                return SourceImages.Count();
            }
        }

        public int UsedImagesCount
        {
            get
            {
                if (ImagesUsageRate == 0) return 0;

                return (int)Math.Truncate((double)SourceImagesCount / ImagesUsageRate);
            }
        }

        public double EstimatedTimelapseDuration
        {
            get
            {
                if (Framerate == 0) return 0;

                return Math.Round((double)UsedImagesCount / Framerate, 1);
            }
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void FirePropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion
    }
}
