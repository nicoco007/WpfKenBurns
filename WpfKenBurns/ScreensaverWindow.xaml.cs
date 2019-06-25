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

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WpfKenBurns
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class ScreensaverWindow : Window
    {
        private Point lastMousePosition = default;
        private bool isPreviewWindow = false;

        public ScreensaverWindow(IntPtr previewHandle)
        {
            InitializeComponent();

            IntPtr windowHandle = new WindowInteropHelper(GetWindow(this)).EnsureHandle();

            // Set the preview window as the parent of this window
            NativeMethods.SetParent(windowHandle, previewHandle);

            // Make this a child window so it will close when the parent dialog closes
            // GWL_STYLE = -16, WS_CHILD = 0x40000000
            NativeMethods.SetWindowLong(windowHandle, -16, 0x40000000);

            // Place our window inside the parent
            RECT parentRect;
            NativeMethods.GetClientRect(previewHandle, out parentRect);

            Width = parentRect.Width;
            Height = parentRect.Height;

            isPreviewWindow = true;
        }

        public ScreensaverWindow(RECT monitor)
        {
            InitializeComponent();

            Topmost = true;
            Cursor = null;

            Top = monitor.Top;
            Left = monitor.Left;
            Width = monitor.Width;
            Height = monitor.Height;
        }

        public Image CreateImage(BitmapImage source)
        {
            Image image = new Image()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Stretch = Stretch.UniformToFill,
                Source = source,
                Opacity = 0
            };

            RenderOptions.SetBitmapScalingMode(image, ConfigurationManager.Configuration.Quality);

            grid.Children.Add(image);

            return image;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (isPreviewWindow) return;

            Application.Current.Shutdown();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isPreviewWindow) return;

            Point pos = e.GetPosition(this);

            if (lastMousePosition != default)
            {
                int mouseSensitivity = 10 - ConfigurationManager.Configuration.MouseSensitivity;

                if ((lastMousePosition - pos).Length > mouseSensitivity * 2)
                {
                    Application.Current.Shutdown();
                }
            }

            lastMousePosition = pos;
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (isPreviewWindow) return;

            Application.Current.Shutdown();
        }
    }
}
