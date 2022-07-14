// <copyright file="RECT.cs" company="PlaceholderCompany">
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

using System.Runtime.InteropServices;

namespace WpfKenBurns.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }

        public RECT(System.Drawing.Rectangle r)
            : this(r.Left, r.Top, r.Right, r.Bottom)
        {
        }

        public int X
        {
            get => this.Left;
            set
            {
                this.Right -= this.Left - value;
                this.Left = value;
            }
        }

        public int Y
        {
            get => this.Top;
            set
            {
                this.Bottom -= this.Top - value;
                this.Top = value;
            }
        }

        public int Height
        {
            get => this.Bottom - this.Top;
            set => this.Bottom = value + this.Top;
        }

        public int Width
        {
            get => this.Right - this.Left;
            set => this.Right = value + this.Left;
        }

        public System.Drawing.Point Location
        {
            get => new System.Drawing.Point(this.Left, this.Top);
            set
            {
                this.X = value.X;
                this.Y = value.Y;
            }
        }

        public System.Drawing.Size Size
        {
            get => new System.Drawing.Size(this.Width, this.Height);
            set
            {
                this.Width = value.Width;
                this.Height = value.Height;
            }
        }

        public static implicit operator System.Drawing.Rectangle(RECT r)
        {
            return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
        }

        public static implicit operator RECT(System.Drawing.Rectangle r)
        {
            return new RECT(r);
        }

        public static bool operator ==(RECT r1, RECT r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(RECT r1, RECT r2)
        {
            return !r1.Equals(r2);
        }

        public bool Equals(RECT r)
        {
            return r.Left == this.Left && r.Top == this.Top && r.Right == this.Right && r.Bottom == this.Bottom;
        }

        public override bool Equals(object? obj)
        {
            if (obj is RECT rect)
            {
                return this.Equals(rect);
            }
            else if (obj is System.Drawing.Rectangle rectangle)
            {
                return this.Equals(new RECT(rectangle));
            }

            return false;
        }

        public override int GetHashCode()
        {
            return ((System.Drawing.Rectangle)this).GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", this.Left, this.Top, this.Right, this.Bottom);
        }
    }
}
