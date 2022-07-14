﻿// WpfKenBurns - A simple Ken Burns-style screensaver
// Copyright © 2019-2022 Nicolas Gnyra

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

namespace WpfKenBurns
{
    public class ScreensaverImageFolder
    {
        public ScreensaverImageFolder(string path, bool recursive)
        {
            Path = path;
            Recursive = recursive;
        }

        public string Path { get; set; }

        public bool Recursive { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is not ScreensaverImageFolder other) return false;

            return (Path, Recursive) == (other.Path, other.Recursive);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Path, Recursive);
        }
    }
}
