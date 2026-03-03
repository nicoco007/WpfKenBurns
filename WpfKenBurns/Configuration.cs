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
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ProtoBuf;
using ProtoBuf.Meta;

namespace WpfKenBurns
{
    [ProtoContract(UseProtoMembersOnly = true)]
    internal class Configuration : INotifyPropertyChanged
    {
        private static readonly string ConfigurationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WpfKenBurns");
        private static readonly string ConfigurationFile = Path.Combine(ConfigurationFolder, "config");

        private static readonly RuntimeTypeModel RuntimeTypeModel = RuntimeTypeModel.Default;

        public event PropertyChangedEventHandler? PropertyChanged;

#pragma warning disable SA1500, SA1513
        [ProtoMember(1)]
        public ObservableCollection<ScreensaverImageFolder> Folders
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                this.NotifyPropertyChanged();
            }
        } = [];

        [ProtoMember(2)]
        public float Duration
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = Math.Clamp(value, 1, 30);
                this.NotifyPropertyChanged();
            }
        } = 7;

        [ProtoMember(3)]
        public float FadeDuration
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = Math.Clamp(value, 0.5f, 30);
                this.NotifyPropertyChanged();
            }
        } = 1.5f;

        [ProtoMember(4)]
        public float MovementFactor
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = Math.Clamp(value, 0, 1);
                this.NotifyPropertyChanged();
            }
        } = 0.05f;

        [ProtoMember(5)]
        public float ScaleFactor
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = Math.Clamp(value, 0, 1);
                this.NotifyPropertyChanged();
            }
        } = 0.05f;

        [ProtoMember(6)]
        public BitmapScalingMode Quality
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                this.NotifyPropertyChanged();
            }
        } = BitmapScalingMode.HighQuality;
#pragma warning restore SA1500, SA1513

        public static void Save(Configuration configuration)
        {
            if (!Directory.Exists(ConfigurationFolder))
            {
                Directory.CreateDirectory(ConfigurationFolder);
            }

            using FileStream fileStream = File.Create(ConfigurationFile);

            RuntimeTypeModel.Serialize(fileStream, configuration);
        }

        public static Configuration Load()
        {
            if (!Directory.Exists(ConfigurationFolder))
            {
                Directory.CreateDirectory(ConfigurationFolder);
            }

            if (!File.Exists(ConfigurationFile))
            {
                return new Configuration();
            }

            using FileStream fileStream = File.OpenRead(ConfigurationFile);

            return RuntimeTypeModel.Deserialize<Configuration>(fileStream);
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
