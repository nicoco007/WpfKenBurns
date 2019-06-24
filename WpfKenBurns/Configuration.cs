// WpfKenBurns - A simple Ken Burns-style screensaver
// Copyright © 2019 Nicolas Gnyra

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU Affero General Public License for more details.

// You should have received a copy of the GNU Affero General Public License
// along with this program.If not, see<https://www.gnu.org/licenses/>.

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

                duration = Clamp(value, 1, 30);
                NotifyPropertyChanged();
            }
        }

        public float FadeDuration
        {
            get => fadeDuration;
            set
            {
                if (fadeDuration == value) return;

                fadeDuration = Clamp(value, 0.5f, 30);
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

        public void CopyFrom(Configuration other)
        {
            Folders = other.Folders;
            Duration = other.Duration;
            FadeDuration = other.FadeDuration;
            MovementFactor = other.MovementFactor;
            ScaleFactor = other.ScaleFactor;
        }

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
