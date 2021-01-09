// WpfKenBurns - A simple Ken Burns-style screensaver
// Copyright © 2019-2021 Nicolas Gnyra

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

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WpfKenBurns
{
    public class Configuration : INotifyPropertyChanged
    {

        private static readonly byte[] Magic = { 0x54, 0x7d, 0x1d, 0x74 };
        private static readonly byte Revision = 1;
        private static readonly string ConfigurationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WpfKenBurns");
        private static readonly string ConfigurationFile = Path.Combine(ConfigurationFolder, "config");

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

        public byte MouseSensitivity
        {
            get => mouseSensitivity;
            set
            {
                if (mouseSensitivity == value) return;

                mouseSensitivity = Clamp(value, 0, 10);
                NotifyPropertyChanged();
            }
        }

        public BitmapScalingMode Quality
        {
            get => quality;
            set
            {
                if (quality == value) return;

                quality = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<string> ProgramDenylist
        {
            get => programDenylist;
            set
            {
                if (programDenylist == value) return;

                programDenylist = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private ObservableCollection<ScreensaverImageFolder> folders = new ObservableCollection<ScreensaverImageFolder>();
        private float duration = 7;
        private float fadeDuration = 1.5f;
        private float movementFactor = 0.05f;
        private float scaleFactor = 0.05f;
        private byte mouseSensitivity = 8;
        private BitmapScalingMode quality = BitmapScalingMode.HighQuality;
        private ObservableCollection<string> programDenylist = new ObservableCollection<string>();

        public static void Save(Configuration configuration)
        {
            if (!Directory.Exists(ConfigurationFolder)) Directory.CreateDirectory(ConfigurationFolder);

            FileStream fileStream = new FileStream(ConfigurationFile, FileMode.Create, FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(fileStream);

            writer.Write(Magic);
            writer.Write(Revision);

            writer.Write(configuration.Duration);
            writer.Write(configuration.FadeDuration);
            writer.Write(configuration.MovementFactor);
            writer.Write(configuration.ScaleFactor);
            writer.Write(configuration.MouseSensitivity);
            writer.Write((byte)configuration.Quality);

            writer.Write(configuration.Folders.Count);

            foreach (ScreensaverImageFolder folder in configuration.Folders)
            {
                writer.Write(folder.Path);
                writer.Write(folder.Recursive);
            }

            writer.Write(configuration.ProgramDenylist.Count);

            foreach (string filePath in configuration.ProgramDenylist)
            {
                writer.Write(filePath);
            }

            writer.Close();
            fileStream.Close();
        }

        public static Configuration Load()
        {
            Configuration configuration = new Configuration();

            if (!Directory.Exists(ConfigurationFolder)) Directory.CreateDirectory(ConfigurationFolder);

            FileStream fileStream = new FileStream(ConfigurationFile, FileMode.OpenOrCreate, FileAccess.Read);

            if (fileStream.Length == 0) return configuration;

            BinaryReader reader = new BinaryReader(fileStream);

            if (!reader.ReadBytes(Magic.Length).SequenceEqual(Magic))
            {
                throw new InvalidDataException("Unknown file format");
            }

            byte revision = reader.ReadByte();

            if (revision != Revision)
            {
                throw new InvalidDataException("Unexpected file version " + revision);
            }

            configuration.Duration = reader.ReadSingle();
            configuration.FadeDuration = reader.ReadSingle();
            configuration.MovementFactor = reader.ReadSingle();
            configuration.ScaleFactor = reader.ReadSingle();
            configuration.MouseSensitivity = reader.ReadByte();
            configuration.Quality = (BitmapScalingMode)reader.ReadByte();

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                configuration.Folders.Add(new ScreensaverImageFolder(reader.ReadString(), reader.ReadBoolean()));
            }

            count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                configuration.ProgramDenylist.Add(reader.ReadString());
            }

            reader.Close();
            fileStream.Close();

            return configuration;
        }

        public void CopyFrom(Configuration other)
        {
            Folders          = other.Folders;
            Duration         = other.Duration;
            FadeDuration     = other.FadeDuration;
            MovementFactor   = other.MovementFactor;
            ScaleFactor      = other.ScaleFactor;
            MouseSensitivity = other.MouseSensitivity;
            Quality          = other.Quality;
            ProgramDenylist = other.ProgramDenylist;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static byte Clamp(byte value, byte min, byte max)
        {
            if (value > max) return max;
            if (value < min) return min;

            return value;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value > max) return max;
            if (value < min) return min;

            return value;
        }
    }
}
