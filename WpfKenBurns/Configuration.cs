// <copyright file="Configuration.cs" company="PlaceholderCompany">
// WpfKenBurns - A simple Ken Burns-style screensaver
// Copyright © 2019-2022 Nicolas Gnyra
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.If not, see&lt;https://www.gnu.org/licenses/&gt;.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WpfKenBurns
{
    internal class Configuration : INotifyPropertyChanged
    {
        private static readonly byte[] Magic = { 0x54, 0x7d, 0x1d, 0x74 };
        private static readonly byte Revision = 1;
        private static readonly string ConfigurationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WpfKenBurns");
        private static readonly string ConfigurationFile = Path.Combine(ConfigurationFolder, "config");

        private ObservableCollection<ScreensaverImageFolder> folders = new();
        private float duration = 7;
        private float fadeDuration = 1.5f;
        private float movementFactor = 0.05f;
        private float scaleFactor = 0.05f;
        private byte mouseSensitivity = 8;
        private BitmapScalingMode quality = BitmapScalingMode.HighQuality;
        private ObservableCollection<string> programDenylist = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ScreensaverImageFolder> Folders
        {
            get => this.folders;
            set
            {
                if (this.folders == value)
                {
                    return;
                }

                this.folders = value;
                this.NotifyPropertyChanged();
            }
        }

        public float Duration
        {
            get => this.duration;
            set
            {
                if (this.duration == value)
                {
                    return;
                }

                this.duration = Math.Clamp(value, 1, 30);
                this.NotifyPropertyChanged();
            }
        }

        public float FadeDuration
        {
            get => this.fadeDuration;
            set
            {
                if (this.fadeDuration == value)
                {
                    return;
                }

                this.fadeDuration = Math.Clamp(value, 0.5f, 30);
                this.NotifyPropertyChanged();
            }
        }

        public float MovementFactor
        {
            get => this.movementFactor;
            set
            {
                if (this.movementFactor == value)
                {
                    return;
                }

                this.movementFactor = Math.Clamp(value, 0, 1);
                this.NotifyPropertyChanged();
            }
        }

        public float ScaleFactor
        {
            get => this.scaleFactor;
            set
            {
                if (this.scaleFactor == value)
                {
                    return;
                }

                this.scaleFactor = Math.Clamp(value, 0, 1);
                this.NotifyPropertyChanged();
            }
        }

        public byte MouseSensitivity
        {
            get => this.mouseSensitivity;
            set
            {
                if (this.mouseSensitivity == value)
                {
                    return;
                }

                this.mouseSensitivity = Math.Clamp(value, (byte)0, (byte)10);
                this.NotifyPropertyChanged();
            }
        }

        public BitmapScalingMode Quality
        {
            get => this.quality;
            set
            {
                if (this.quality == value)
                {
                    return;
                }

                this.quality = value;
                this.NotifyPropertyChanged();
            }
        }

        public ObservableCollection<string> ProgramDenylist
        {
            get => this.programDenylist;
            set
            {
                if (this.programDenylist == value)
                {
                    return;
                }

                this.programDenylist = value;
                this.NotifyPropertyChanged();
            }
        }

        public static void Save(Configuration configuration)
        {
            if (!Directory.Exists(ConfigurationFolder))
            {
                Directory.CreateDirectory(ConfigurationFolder);
            }

            using FileStream fileStream = new(ConfigurationFile, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new(fileStream);

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
        }

        public static Configuration Load()
        {
            Configuration configuration = new();

            if (!Directory.Exists(ConfigurationFolder))
            {
                Directory.CreateDirectory(ConfigurationFolder);
            }

            using FileStream fileStream = new(ConfigurationFile, FileMode.OpenOrCreate, FileAccess.Read);

            if (fileStream.Length == 0)
            {
                return configuration;
            }

            using BinaryReader reader = new(fileStream);

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

            return configuration;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
