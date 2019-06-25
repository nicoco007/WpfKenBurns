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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
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
        private BitmapImage currentImage = null;
        private Random random = new Random();
        private Point lastMousePosition = default;
        private bool isPreviewWindow = false;
        private RandomizedEnumerator<string> fileEnumerator;

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigurationManager.Load();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("Failed to load configuration: " + ex.Message);
                Application.Current.Shutdown();
            }

            List<string> files = new List<string>();

            foreach (ScreensaverImageFolder folder in ConfigurationManager.Configuration.Folders)
            {
                files.AddRange(Directory.GetFiles(folder.Path, "*", folder.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
            }

            fileEnumerator = new RandomizedEnumerator<string>(files);

            UpdateCurrentImage();
            KenBurns(grid);
        }

        private void KenBurns(Panel container)
        {
            double duration       = ConfigurationManager.Configuration.Duration;
            double movementFactor = ConfigurationManager.Configuration.MovementFactor + 1;
            double scaleFactor    = ConfigurationManager.Configuration.ScaleFactor + 1;
            double fadeDuration   = ConfigurationManager.Configuration.FadeDuration;
            double totalDuration  = duration + fadeDuration * 2;

            double width = container.ActualWidth;
            double height = container.ActualHeight;

            Image image = new Image()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Stretch = Stretch.UniformToFill,
                Source = currentImage
            };

            RenderOptions.SetBitmapScalingMode(image, ConfigurationManager.Configuration.Quality);

            grid.Children.Add(image);

            ThicknessAnimation marginAnimation = new ThicknessAnimation();
            DoubleAnimation widthAnimation = new DoubleAnimation();
            DoubleAnimation heightAnimation = new DoubleAnimation();
            DoubleAnimation opacityAnimation = new DoubleAnimation();

            bool zoomDirection = random.Next(2) == 1;
            double fromScale = (zoomDirection ? scaleFactor : 1);
            double toScale = (zoomDirection ? 1 : scaleFactor);

            double angle = random.NextDouble() * Math.PI * 2;

            Point p1 = GetPointOnRectangleFromAngle(angle,           width * (movementFactor * fromScale - 1), height * (movementFactor * fromScale - 1));
            Point p2 = GetPointOnRectangleFromAngle(angle + Math.PI, width * (movementFactor * toScale - 1),   height * (movementFactor * toScale - 1));

            marginAnimation.From = new Thickness(p1.X, p1.Y, 0, 0);
            marginAnimation.To = new Thickness(p2.X, p2.Y, 0, 0);

            widthAnimation.From = width * movementFactor * fromScale;
            widthAnimation.To = width * movementFactor * toScale;

            heightAnimation.From = height * movementFactor * fromScale;
            heightAnimation.To = height * movementFactor * toScale;

            opacityAnimation.From = 0;
            opacityAnimation.To = 1;

            marginAnimation.Duration = TimeSpan.FromSeconds(totalDuration);
            widthAnimation.Duration = TimeSpan.FromSeconds(totalDuration);
            heightAnimation.Duration = TimeSpan.FromSeconds(totalDuration);
            opacityAnimation.Duration = TimeSpan.FromSeconds(fadeDuration);

            Storyboard.SetTarget(marginAnimation, image);
            Storyboard.SetTarget(widthAnimation, image);
            Storyboard.SetTarget(heightAnimation, image);
            Storyboard.SetTarget(opacityAnimation, image);

            Storyboard.SetTargetProperty(marginAnimation, new PropertyPath(Image.MarginProperty));
            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(Image.WidthProperty));
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(Image.HeightProperty));
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(Image.OpacityProperty));

            Storyboard storyboard = new Storyboard();

            storyboard.Children.Add(marginAnimation);
            storyboard.Children.Add(widthAnimation);
            storyboard.Children.Add(heightAnimation);
            storyboard.Children.Add(opacityAnimation);

            storyboard.Duration = new Duration(TimeSpan.FromSeconds(totalDuration));

            bool nextStarted = false;

            storyboard.CurrentTimeInvalidated += (sender, args) =>
            {
                if (!nextStarted && storyboard.GetCurrentTime() >= TimeSpan.FromSeconds(duration + fadeDuration))
                {
                    nextStarted = true;
                    KenBurns(container);
                }
            };

            storyboard.Completed += (sender, args) =>
            {
                grid.Children.Remove(image);
            };

            storyboard.Begin();

            new Thread(UpdateCurrentImage).Start();
        }

        public void UpdateCurrentImage()
        {
            if (!fileEnumerator.MoveNext())
            {
                fileEnumerator.Reset();
                fileEnumerator.MoveNext();
            }

            string fileName = fileEnumerator.Current;

            if (fileName == default) return;

            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            BitmapImage image = new BitmapImage();

            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.None;
            image.StreamSource = fileStream;
            image.EndInit();

            fileStream.Dispose();
            
            image.Freeze();

            currentImage = image;
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

        private static double Clamp(double value, double min, double max)
        {
            if (value > max) return max;
            if (value < min) return min;

            return value;
        }

        private static Point GetPointOnRectangleFromAngle(double angle, double width, double height)
        {
            double max = Math.Sqrt(Math.Pow(width / 2, 2) + Math.Pow(height / 2, 2));

            double x = Clamp(Math.Cos(angle) * max, -width / 2, width / 2);
            double y = Clamp(Math.Sin(angle) * max, -height / 2, height / 2);

            return new Point(x - width / 2, y - height / 2);
        }
    }
}
