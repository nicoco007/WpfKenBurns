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
        private BitmapImage currentImage = null;
        private Random random = new Random();
        private Point lastMousePosition = default;
        private bool IsPreviewWindow { get; }
        private List<string> Files { get; set; }
        private RECT Monitor { get; set; }

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
            
            IsPreviewWindow = true;

            Monitor = parentRect;
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

            Monitor = monitor;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigurationManager.Load();

            Files = new List<string>();

            foreach (ScreensaverImageFolder folder in ConfigurationManager.Folders)
            {
                Files.AddRange(Directory.GetFiles(folder.Path, "*", folder.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
            }

            UpdateCurrentImage();
            KenBurns();
        }

        private void KenBurns()
        {
            double duration       = ConfigurationManager.Duration;
            double movementFactor = ConfigurationManager.MovementFactor;
            double scaleFactor    = ConfigurationManager.ScaleFactor;
            double fadeDuration   = ConfigurationManager.FadeDuration;
            double totalDuration  = duration + fadeDuration * 2;

            double maxDistanceX = Width * (movementFactor - 1);
            double maxDistanceY = Height * (movementFactor - 1);

            Image image = new Image()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Stretch = Stretch.UniformToFill,
                Source = currentImage
            };

            grid.Children.Add(image);

            ThicknessAnimation marginAnimation = new ThicknessAnimation();
            DoubleAnimation widthAnimation = new DoubleAnimation();
            DoubleAnimation heightAnimation = new DoubleAnimation();
            DoubleAnimation opacityAnimation = new DoubleAnimation();

            double angle = random.NextDouble() * Math.PI * 2;

            Point p1 = new Point((Math.Cos(angle) - 1) * maxDistanceX / 2, (Math.Sin(angle) - 1) * maxDistanceY / 2);
            Point p2 = new Point((-Math.Cos(angle) - 1) * maxDistanceX / 2, (-Math.Sin(angle) - 1) * maxDistanceY / 2);
            Point offset = new Point((maxDistanceX - Math.Abs(p2.X - p1.X)) * (random.NextDouble() * 2 - 1), (maxDistanceY - Math.Abs(p2.Y - p1.Y)) * (random.NextDouble() * 2 - 1));

            marginAnimation.From = new Thickness(p1.X + offset.X, p1.Y + offset.Y, 0, 0);
            marginAnimation.To = new Thickness(p2.X + offset.X, p2.Y + offset.Y, 0, 0);

            double fromScale = 1 + (scaleFactor - 1) * random.NextDouble();
            double toScale = 1 + (scaleFactor - 1) * random.NextDouble();

            widthAnimation.From = Width * movementFactor * fromScale;
            widthAnimation.To = Width * movementFactor * toScale;

            heightAnimation.From = Height * movementFactor * fromScale;
            heightAnimation.To = Height * movementFactor * toScale;

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

            storyboard.CurrentTimeInvalidated += (e, args) =>
            {
                if (!nextStarted && storyboard.GetCurrentTime() >= TimeSpan.FromSeconds(duration - fadeDuration))
                {
                    KenBurns();
                    nextStarted = true;
                }
            };

            storyboard.Completed += (e, args) =>
            {
                grid.Children.Remove(image);
            };

            storyboard.Begin();

            UpdateCurrentImage();
        }

        public void UpdateCurrentImage()
        {
            if (Files.Count == 0) return;

            string file;

            do
            {
                file = Files[random.Next(Files.Count)];
            } while (false);

            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);

            currentImage = new BitmapImage();

            currentImage.BeginInit();
            currentImage.CacheOption = BitmapCacheOption.OnLoad;
            currentImage.CreateOptions = BitmapCreateOptions.None;
            currentImage.DecodePixelWidth = Monitor.Width;
            currentImage.StreamSource = fileStream;
            currentImage.EndInit();

            fileStream.Dispose();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (IsPreviewWindow) return;

            Application.Current.Shutdown();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsPreviewWindow) return;

            Point pos = e.GetPosition(this);

            if (lastMousePosition != default && (Math.Abs(lastMousePosition.X - pos.X) > 5 || Math.Abs(lastMousePosition.Y - pos.Y) > 5))
            {
                Application.Current.Shutdown();
            }

            lastMousePosition = pos;
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsPreviewWindow) return;

            Application.Current.Shutdown();
        }
    }
}
