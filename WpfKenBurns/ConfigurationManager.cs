// WpfKenBurns - A simple Ken Burns-style screensaver
// Copyright © 2019-2020 Nicolas Gnyra

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
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace WpfKenBurns
{
    public static class ConfigurationManager
    {
        private static readonly byte[] Magic = { 0x54, 0x7d, 0x1d, 0x74 };
        private static readonly byte Revision = 0;
        private static readonly string ConfigurationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WpfKenBurns");
        private static readonly string ConfigurationFile = Path.Combine(ConfigurationFolder, "config");

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
                configuration.Folders.Add(new ScreensaverImageFolder
                {
                    Path = reader.ReadString(),
                    Recursive = reader.ReadBoolean()
                });
            }

            reader.Close();
            fileStream.Close();

            return configuration;
        }
    }
}
