// <copyright file="ScreensaverWindow.xaml.cs" company="PlaceholderCompany">
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfKenBurns.Native;

namespace WpfKenBurns
{
    public partial class ScreensaverWindow : Window
    {
        /// <summary>
        /// Message that is sent to all windows when the display resolution has changed. Equal to WM_DISPLAYCHANGE.
        /// </summary>
        private const int WindowMessageDisplayChange = 0x007e;

        private readonly IntPtr windowHandle;
        private readonly Configuration configuration;
        private readonly bool isPreviewWindow = false;

        private Point lastMousePosition = default;

        internal ScreensaverWindow(Configuration config, IntPtr previewHandle)
            : this(config)
        {
            // Set the preview window as the parent of this window
            NativeMethods.SetParent(this.windowHandle, previewHandle);

            // Make this a child window so it will close when the parent dialog closes
            // GWL_STYLE = -16, WS_CHILD = 0x40000000
            NativeMethods.SetWindowLong(this.windowHandle, -16, 0x40000000);

            // Place our window inside the parent
            NativeMethods.GetClientRect(previewHandle, out RECT parentRect);

            NativeMethods.SetWindowPos(this.windowHandle, IntPtr.Zero, 0, 0, parentRect.Width, parentRect.Height, SetWindowPosFlags.ShowWindow);

            this.isPreviewWindow = true;
        }

        internal ScreensaverWindow(Configuration config, RECT monitor)
            : this(config)
        {
            this.Topmost = true;
            this.Cursor = Cursors.None;

            NativeMethods.SetWindowPos(this.windowHandle, IntPtr.Zero, monitor.X, monitor.Y, monitor.Width, monitor.Height, SetWindowPosFlags.ShowWindow);
        }

        private ScreensaverWindow(Configuration config)
        {
            this.InitializeComponent();
            this.configuration = config;
            this.windowHandle = new WindowInteropHelper(this).EnsureHandle();

            HwndSource source = HwndSource.FromHwnd(this.windowHandle);

            source.AddHook(this.WndProc);
        }

        internal event Action? DisplayChanged;

        internal Image CreateImage(BitmapImage source, Size size)
        {
            Image image = new()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Stretch = Stretch.UniformToFill,
                Source = source,
                Opacity = 0,
                Width = size.Width,
                Height = size.Height,
            };

            RenderOptions.SetBitmapScalingMode(image, this.configuration.Quality);

            this.grid.Children.Add(image);

            return image;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WindowMessageDisplayChange)
            {
                this.DisplayChanged?.Invoke();
            }

            return IntPtr.Zero;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.isPreviewWindow)
            {
                return;
            }

            Application.Current.Shutdown();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isPreviewWindow)
            {
                return;
            }

            Point pos = e.GetPosition(this);

            if (this.lastMousePosition != default)
            {
                int mouseSensitivity = 10 - this.configuration.MouseSensitivity;

                if ((this.lastMousePosition - pos).Length > mouseSensitivity * 2)
                {
                    Application.Current.Shutdown();
                }
            }

            this.lastMousePosition = pos;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.isPreviewWindow)
            {
                return;
            }

            Application.Current.Shutdown();
        }
    }
}
