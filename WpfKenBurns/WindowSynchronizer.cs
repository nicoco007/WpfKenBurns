// WpfKenBurns - A simple Ken Burns-style screensaver
// Copyright © 2019-2026 Nicolas Gnyra
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.If not, see https://www.gnu.org/licenses/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfKenBurns.Native;

namespace WpfKenBurns
{
    internal partial class WindowSynchronizer
    {
        private static readonly string[] ValidImageExtensions;

        static WindowSynchronizer()
        {
            ValidImageExtensions = [.. ImageCodecInfo.GetImageDecoders().SelectMany(codec => codec.FilenameExtension?.Split(';') ?? []).Select(ext => ext.TrimStart('*'))];
        }

        private readonly List<ScreensaverWindow> windows = [];
        private readonly List<string> files = [];
        private readonly Random random = new();

        private readonly IntPtr handle;

        private readonly long startTime;
        private readonly Timer? timer;

        private Configuration? configuration;
        private CancellationTokenSource? cancellationTokenSource;
        private Task? task;
        private bool resetting = false;

        private readonly IEnumerator<string> randomFileEnumerator;

        public WindowSynchronizer()
        {
            startTime = Environment.TickCount64;
            timer = new Timer(OnTimerTick, null, 0, 100);
            randomFileEnumerator = RandomFileEnumerator();

            Application.Current.Exit += OnApplicationExit;
        }

        public WindowSynchronizer(IntPtr handle)
        {
            this.handle = handle;
            randomFileEnumerator = RandomFileEnumerator();
        }

        public void Start()
        {
            try
            {
                configuration = Configuration.Load();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("Failed to load configuration: " + ex.Message);
                Application.Current.Shutdown();
                return;
            }

            foreach (ScreensaverImageFolder folder in configuration.Folders)
            {
                try
                {
                    files.AddRange(
                        Directory.EnumerateFiles(folder.Path, "*.*", folder.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                            .Where(path => ValidImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)));
                }
                catch (IOException ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            if (handle != IntPtr.Zero)
            {
                ScreensaverWindow window = new(configuration, handle);
                window.Show();
                windows.Add(window);
                StartWorker();
            }
            else
            {
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                EnumerateMonitors();
            }

            Application.Current.Exit += (sender, e) =>
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
            };
        }

        private static uint GetLastInputTime()
        {
            LASTINPUTINFO info = new();

            if (!NativeMethods.GetLastInputInfo(ref info))
            {
                int error = Marshal.GetLastPInvokeError();
                Debug.WriteLine($"GetLastInputInfo failed: Error {error}");
                return 0;
            }

            return info.Time;
        }

        private static Point GetPointOnRectangleFromAngle(double angle, double width, double height)
        {
            double max = Math.Sqrt(Math.Pow(width / 2, 2) + Math.Pow(height / 2, 2));

            double x = Math.Clamp(Math.Cos(angle) * max, -width / 2, width / 2);
            double y = Math.Clamp(Math.Sin(angle) * max, -height / 2, height / 2);

            return new Point(x - width / 2, y - height / 2);
        }

        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            timer?.Dispose();
            Application.Current.Exit -= OnApplicationExit;
        }

        private void EnumerateMonitors()
        {
            if (resetting)
            {
                return;
            }

            if (configuration == null)
            {
                return;
            }

            resetting = true;
            StopWorker();

            foreach (Window window in windows)
            {
                window.Close();
            }

            windows.Clear();

            NativeMethods.EnumDisplayMonitors(
                IntPtr.Zero,
                IntPtr.Zero,
                (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
                {
                    ScreensaverWindow window = new(configuration, lprcMonitor);
                    window.Show();
                    windows.Add(window);
                    window.DisplayChanged += OnDisplayChanged;
                    return true;
                },
                IntPtr.Zero);

            StartWorker();

            resetting = false;
        }

        private void OnDisplayChanged()
        {
            EnumerateMonitors();
        }

        private void StopWorker()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            task?.Wait();
        }

        private void StartWorker()
        {
            cancellationTokenSource = new CancellationTokenSource();
            task = Task.Run(() => Worker(cancellationTokenSource.Token), cancellationTokenSource.Token).ContinueWith(OnWorkerTaskCompleted);
        }

        private async Task OnWorkerTaskCompleted(Task task)
        {
            if (!task.IsFaulted)
            {
                return;
            }

            Debug.WriteLine(task.Exception);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (ScreensaverWindow window in windows)
                {
                    window.errorTextBlock.Text = string.Join("\n", task.Exception.InnerExceptions.Select(ex => ex.Message));
                }
            });
        }

        private async Task Worker(CancellationToken cancellationToken)
        {
            Dispatcher uiDispatcher = Application.Current.Dispatcher;

            await uiDispatcher.InvokeAsync(() =>
            {
                foreach (ScreensaverWindow window in windows)
                {
                    window.errorTextBlock.Text = null;
                }
            });

            TaskCompletionSource? taskCompletionSource = null;
            cancellationToken.Register(() => taskCompletionSource?.TrySetCanceled());

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int count = windows.Count;
                Storyboard?[] storyboards = new Storyboard?[count];
                CountdownEvent countdownEvent = new(count);

                for (int i = 0; i < count; i++)
                {
                    ScreensaverWindow window = windows[i];
                    BitmapImage source = GetImage();

                    double scale = Math.Max(window.ActualWidth / source.PixelWidth, window.ActualHeight / source.PixelHeight);
                    Size size = new(source.PixelWidth * scale, source.PixelHeight * scale);

                    Image image = await uiDispatcher.InvokeAsync(() => window.CreateImage(source, size));

                    Panel container = (Panel)image.Parent;

                    ManualResetEventSlim resetEvent = new(false);
                    storyboards[i] = CreateStoryboardAnimation(container, image, size, countdownEvent);
                }

                if (taskCompletionSource != null)
                {
                    await taskCompletionSource.Task;
                }

                await uiDispatcher.InvokeAsync(() =>
                {
                    foreach (Storyboard? storyboard in storyboards)
                    {
                        storyboard?.Begin();
                    }
                });

                taskCompletionSource = new TaskCompletionSource();
                ThreadPool.RegisterWaitForSingleObject(countdownEvent.WaitHandle, (state, timeout) => taskCompletionSource?.TrySetResult(), null, -1, true);
            }
        }

        private BitmapImage GetImage()
        {
            while (randomFileEnumerator.MoveNext())
            {
                string filePath = randomFileEnumerator.Current;

                try
                {
                    using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);

                    BitmapImage image = new();

                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.None;
                    image.StreamSource = fileStream;
                    image.EndInit();

                    image.Freeze();

                    return image;
                }
                catch (Exception ex) when (ex is NotSupportedException or IOException)
                {
                    Debug.WriteLine($"Failed to load '{filePath}'; removing from list\n{ex}");
                    files.Remove(randomFileEnumerator.Current);
                }
            }

            throw new Exception("No images could be loaded");
        }

        private IEnumerator<string> RandomFileEnumerator()
        {
            while (files.Count > 0)
            {
                foreach (string file in files.Shuffle())
                {
                    yield return file;
                }
            }
        }

        private Storyboard CreateStoryboardAnimation(Panel container, Image image, Size imageSize, CountdownEvent countdownEvent)
        {
            if (configuration == null)
            {
                return new Storyboard();
            }

            double duration = configuration.Duration;
            double movementFactor = configuration.MovementFactor + 1;
            double scaleFactor = configuration.ScaleFactor + 1;
            double fadeDuration = configuration.FadeDuration;
            double totalDuration = duration + fadeDuration * 2;

            double width = container.ActualWidth;
            double height = container.ActualHeight;

            ThicknessAnimation marginAnimation = new();
            DoubleAnimation widthAnimation = new();
            DoubleAnimation heightAnimation = new();
            DoubleAnimation opacityAnimation = new();

            bool zoomDirection = random.Next(2) == 1;
            double fromScale = zoomDirection ? scaleFactor : 1;
            double toScale = zoomDirection ? 1 : scaleFactor;

            double angle = random.NextDouble() * Math.PI * 2;

            Point p1 = GetPointOnRectangleFromAngle(angle, width * (movementFactor * fromScale - 1), height * (movementFactor * fromScale - 1));
            Point p2 = GetPointOnRectangleFromAngle(angle + Math.PI, width * (movementFactor * toScale - 1), height * (movementFactor * toScale - 1));

            // center image if its proportions aren't identical to the screen's
            p1.X -= (imageSize.Width - width) / 2;
            p1.Y -= (imageSize.Height - height) / 2;
            p2.X -= (imageSize.Width - width) / 2;
            p2.Y -= (imageSize.Height - height) / 2;

            marginAnimation.From = new Thickness(p1.X, p1.Y, 0, 0);
            marginAnimation.To = new Thickness(p2.X, p2.Y, 0, 0);

            widthAnimation.From = imageSize.Width * movementFactor * fromScale;
            widthAnimation.To = imageSize.Width * movementFactor * toScale;

            heightAnimation.From = imageSize.Height * movementFactor * fromScale;
            heightAnimation.To = imageSize.Height * movementFactor * toScale;

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

            Storyboard.SetTargetProperty(marginAnimation, new PropertyPath(FrameworkElement.MarginProperty));
            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(FrameworkElement.WidthProperty));
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(FrameworkElement.HeightProperty));
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));

            Storyboard storyboard = new();

            storyboard.Children.Add(marginAnimation);
            storyboard.Children.Add(widthAnimation);
            storyboard.Children.Add(heightAnimation);
            storyboard.Children.Add(opacityAnimation);

            storyboard.Duration = new(TimeSpan.FromSeconds(totalDuration));

            bool nextStarted = false;
            TimeSpan durationToNext = TimeSpan.FromSeconds(duration + fadeDuration);

            storyboard.CurrentTimeInvalidated += (sender, args) =>
            {
                if (!nextStarted && storyboard.GetCurrentTime() >= durationToNext)
                {
                    nextStarted = true;
                    countdownEvent.Signal();
                }
            };

            storyboard.Completed += (sender, args) =>
            {
                container.Children.Remove(image);
            };

            storyboard.Freeze();

            return storyboard;
        }

        private void OnTimerTick(object? state)
        {
            uint time = GetLastInputTime();
            long now = Environment.TickCount64;

            if ((now - time) < (now - startTime))
            {
                Application.Current.Dispatcher.BeginInvoke(Application.Current.Shutdown);
            }
        }
    }
}
