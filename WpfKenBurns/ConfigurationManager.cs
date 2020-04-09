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
        public static Configuration Configuration { get; set; } = new Configuration();

        private static readonly byte[] MAGIC = new byte[] { 0x54, 0x7d, 0x1d, 0x74 };
        private static readonly byte REVISION = 0;
        private static readonly string CONFIGURATION_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WpfKenBurns");
        private static readonly string CONFIGURATION_FILE = Path.Combine(CONFIGURATION_FOLDER, "config");

        public static void Save()
        {
            if (!Directory.Exists(CONFIGURATION_FOLDER)) Directory.CreateDirectory(CONFIGURATION_FOLDER);

            FileStream fileStream = new FileStream(CONFIGURATION_FILE, FileMode.Create, FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(fileStream);

            writer.Write(MAGIC);
            writer.Write(REVISION);

            writer.Write(Configuration.Duration);
            writer.Write(Configuration.FadeDuration);
            writer.Write(Configuration.MovementFactor);
            writer.Write(Configuration.ScaleFactor);
            writer.Write(Configuration.MouseSensitivity);
            writer.Write((byte)Configuration.Quality);

            writer.Write(Configuration.Folders.Count);

            foreach (ScreensaverImageFolder folder in Configuration.Folders)
            {
                writer.Write(folder.Path);
                writer.Write(folder.Recursive);
            }

            writer.Close();
            fileStream.Close();
        }

        public static void Load()
        {
            Configuration configuration = new Configuration();

            if (!Directory.Exists(CONFIGURATION_FOLDER)) Directory.CreateDirectory(CONFIGURATION_FOLDER);

            FileStream fileStream = new FileStream(CONFIGURATION_FILE, FileMode.OpenOrCreate, FileAccess.Read);

            if (fileStream.Length == 0) return;

            BinaryReader reader = new BinaryReader(fileStream);

            if (!reader.ReadBytes(MAGIC.Length).SequenceEqual(MAGIC))
            {
                throw new InvalidDataException("Unknown file format");
            }

            byte revision = reader.ReadByte();

            if (revision != REVISION)
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

            Configuration.CopyFrom(configuration);
        }

        public static void Reset()
        {
            Configuration.CopyFrom(new Configuration());
        }
    }
}
