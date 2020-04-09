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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WpfKenBurns
{
    public class WindowSynchronizer
    {
        private Configuration configuration;
        private List<ScreensaverWindow> windows = new List<ScreensaverWindow>();
        private Random random = new Random();
        private bool running = false;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private IntPtr handle;

        private RandomizedEnumerator<string> fileEnumerator;

        public WindowSynchronizer()
        {
            configuration = ConfigurationManager.Load();;
        }

        public WindowSynchronizer(IntPtr handle) : this()
        {
            this.handle = handle;
        }

        public void Start()
        {
            if (running) throw new InvalidOperationException();

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

            foreach (ScreensaverImageFolder folder in configuration.Folders)
            {
                files.AddRange(Directory.GetFiles(folder.Path, "*", folder.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
            }

            fileEnumerator = new RandomizedEnumerator<string>(files);

            if (handle != IntPtr.Zero)
            {
                ScreensaverWindow window = new ScreensaverWindow(configuration, handle);
                window.Show();
                windows.Add(window);
            }
            else
            {
                NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
                {
                    ScreensaverWindow window = new ScreensaverWindow(configuration, lprcMonitor);
                    window.Show();
                    windows.Add(window);
                    return true;
                }, IntPtr.Zero);
            }

            Application.Current.Exit += (sender, e) =>
            {
                cancellationTokenSource.Cancel(false);
                running = false;
            };

            running = true;

            new Thread(Worker).Start();
        }

        private void Worker()
        {
            ManualResetEventSlim[] previousResetEvents = new ManualResetEventSlim[windows.Count];
            Dispatcher uiDispatcher = Application.Current.Dispatcher;

            if (uiDispatcher == null) return;

            try
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    Storyboard[] storyboards = new Storyboard[windows.Count];
                    ManualResetEventSlim[] resetEvents = new ManualResetEventSlim[windows.Count];

                    for (int i = 0; i < windows.Count; i++)
                    {
                        BitmapImage source = GetImage();
                        Image image = uiDispatcher.Invoke(() => windows[i].CreateImage(source));

                        Panel container = (Panel)image.Parent;

                        ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);
                        storyboards[i] = SetupAnimation(container, image, resetEvent);
                        resetEvents[i] = resetEvent;

                        if (cancellationTokenSource.IsCancellationRequested) return;
                    }

                    for (int i = 0; i < previousResetEvents.Length; i++)
                    {
                        previousResetEvents[i]?.Wait(cancellationTokenSource.Token);
                    }

                    for (int i = 0; i < storyboards.Length; i++)
                    {
                        previousResetEvents[i] = resetEvents[i];
                        Application.Current.Dispatcher.Invoke(() => storyboards[i].Begin());
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private BitmapImage GetImage()
        {
            if (!fileEnumerator.MoveNext())
            {
                fileEnumerator.Reset();
                fileEnumerator.MoveNext();
            }

            string fileName = fileEnumerator.Current;

            if (fileName == default) return null;

            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            BitmapImage image = new BitmapImage();

            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.None;
            image.StreamSource = fileStream;
            image.EndInit();

            fileStream.Dispose();

            image.Freeze();

            return image;
        }

        private Storyboard SetupAnimation(Panel container, Image image, ManualResetEventSlim resetEvent)
        {
            double duration = configuration.Duration;
            double movementFactor = configuration.MovementFactor + 1;
            double scaleFactor = configuration.ScaleFactor + 1;
            double fadeDuration = configuration.FadeDuration;
            double totalDuration = duration + fadeDuration * 2;

            double width = container.ActualWidth;
            double height = container.ActualHeight;

            ThicknessAnimation marginAnimation = new ThicknessAnimation();
            DoubleAnimation widthAnimation = new DoubleAnimation();
            DoubleAnimation heightAnimation = new DoubleAnimation();
            DoubleAnimation opacityAnimation = new DoubleAnimation();

            bool zoomDirection = random.Next(2) == 1;
            double fromScale = (zoomDirection ? scaleFactor : 1);
            double toScale = (zoomDirection ? 1 : scaleFactor);

            double angle = random.NextDouble() * Math.PI * 2;

            Point p1 = GetPointOnRectangleFromAngle(angle, width * (movementFactor * fromScale - 1), height * (movementFactor * fromScale - 1));
            Point p2 = GetPointOnRectangleFromAngle(angle + Math.PI, width * (movementFactor * toScale - 1), height * (movementFactor * toScale - 1));

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
                    resetEvent.Set();
                }
            };

            storyboard.Completed += (sender, args) =>
            {
                container.Children.Remove(image);
            };

            storyboard.Freeze();

            return storyboard;
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
