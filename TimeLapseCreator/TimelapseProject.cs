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

        public FileInfo[] SourceImages
        {
            get
            {
                if (string.IsNullOrEmpty(SourcePath)) return null;

                DirectoryInfo sourcePathDirectory = new DirectoryInfo(SourcePath);
                FileInfo[] images = sourcePathDirectory.GetFiles("*.JPG", SearchOption.TopDirectoryOnly); //*.jpg,*.jpeg,*.JPG,*.JPEG
                images.OrderBy(fi => fi.CreationTime);
                return images;
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
