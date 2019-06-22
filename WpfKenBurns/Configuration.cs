using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfKenBurns
{
    public class Configuration : INotifyPropertyChanged
    {
        public ObservableCollection<ScreensaverImageFolder> Folders
        {
            get => folders;
            set
            {
                if (folders == value) return;

                folders = value;
                NotifyPropertyChanged();
            }
        }

        public float Duration
        {
            get => duration;
            set
            {
                if (duration == value) return;

                duration = Clamp(value, 0, 30);
                NotifyPropertyChanged();
            }
        }

        public float FadeDuration
        {
            get => fadeDuration;
            set
            {
                if (fadeDuration == value) return;

                fadeDuration = Clamp(value, 0, 30);
                NotifyPropertyChanged();
            }
        }

        public float MovementFactor
        {
            get => movementFactor;
            set
            {
                if (movementFactor == value) return;

                movementFactor = Clamp(value, 0, 1);
                NotifyPropertyChanged();
            }
        }

        public float ScaleFactor
        {
            get => scaleFactor;
            set
            {
                if (scaleFactor == value) return;

                scaleFactor = Clamp(value, 0, 1);
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<ScreensaverImageFolder> folders = new ObservableCollection<ScreensaverImageFolder>();
        private float duration = 7;
        private float fadeDuration = 1.5f;
        private float movementFactor = 0.05f;
        private float scaleFactor = 0.05f;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value > max) return max;
            if (value < min) return min;

            return value;
        }
    }
}
