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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfKenBurns
{
    public partial class ScreensaverWindow : Window
    {
        public event Action DisplayChanged;

        private IntPtr windowHandle;
        private Configuration configuration;
        private Point lastMousePosition = default;
        private bool isPreviewWindow = false;

        private ScreensaverWindow(Configuration config)
        {
            InitializeComponent();
            configuration = config;
            windowHandle = new WindowInteropHelper(this).EnsureHandle();

            HwndSource source = HwndSource.FromHwnd(windowHandle);

            source.AddHook(WndProc);
        }

        public ScreensaverWindow(Configuration config, IntPtr previewHandle) : this(config)
        {
            // Set the preview window as the parent of this window
            NativeMethods.SetParent(windowHandle, previewHandle);

            // Make this a child window so it will close when the parent dialog closes
            // GWL_STYLE = -16, WS_CHILD = 0x40000000
            NativeMethods.SetWindowLong(windowHandle, -16, 0x40000000);

            // Place our window inside the parent
            RECT parentRect;
            NativeMethods.GetClientRect(previewHandle, out parentRect);

            NativeMethods.SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, parentRect.Width, parentRect.Height, SetWindowPosFlags.ShowWindow);

            isPreviewWindow = true;
        }

        public ScreensaverWindow(Configuration config, RECT monitor) : this(config)
        {
            Topmost = true;
            Cursor = Cursors.None;

            NativeMethods.SetWindowPos(windowHandle, IntPtr.Zero, monitor.X, monitor.Y, monitor.Width, monitor.Height, SetWindowPosFlags.ShowWindow);
        }

        public Image CreateImage(BitmapImage source, Size size)
        {
            Image image = new Image
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Stretch = Stretch.UniformToFill,
                Source = source,
                Opacity = 0,
                Width = size.Width,
                Height = size.Height
            };

            RenderOptions.SetBitmapScalingMode(image, configuration.Quality);

            grid.Children.Add(image);

            return image;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x007e) // WM_DISPLAYCHANGE
            {
                DisplayChanged?.Invoke();
            }

            return IntPtr.Zero;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (isPreviewWindow) return;

            Application.Current.Shutdown();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPreviewWindow) return;

            Point pos = e.GetPosition(this);

            if (lastMousePosition != default)
            {
                int mouseSensitivity = 10 - configuration.MouseSensitivity;

                if ((lastMousePosition - pos).Length > mouseSensitivity * 2)
                {
                    Application.Current.Shutdown();
                }
            }

            lastMousePosition = pos;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isPreviewWindow) return;

            Application.Current.Shutdown();
        }
    }
}
