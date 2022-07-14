// WpfKenBurns - A simple Ken Burns-style screensaver
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WpfKenBurns
{
    public class WindowSynchronizer
    {
        private static readonly string[] ValidImageExtensions = { ".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".wmp" };

        private readonly List<ScreensaverWindow> windows = new();
        private readonly Random random = new();

        private readonly IntPtr handle;

        private Configuration? configuration;
        private CancellationTokenSource? cancellationTokenSource;
        private Task? task;
        private bool resetting = false;

        private RandomizedEnumerator<string>? fileEnumerator;

        public WindowSynchronizer() { }

        public WindowSynchronizer(IntPtr handle) : this()
        {
            this.handle = handle;
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

            foreach (string filePath in configuration.ProgramDenylist)
            {
                string fullPath = Path.GetFullPath(filePath);
                Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(filePath));

                if (processes.Any(p => !string.IsNullOrEmpty(p.MainModule?.FileName) && Path.GetFullPath(p.MainModule.FileName) == fullPath))
                {
                    Application.Current.Shutdown();
                    return;
                }
            }

            List<string> files = new();

            foreach (ScreensaverImageFolder folder in configuration.Folders)
            {
                try
                {
                    files.AddRange(
                        Directory.GetFiles(folder.Path, "*.*", folder.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                            .Where(path => ValidImageExtensions.Contains(Path.GetExtension(path))));
                }
                catch (IOException ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            fileEnumerator = new RandomizedEnumerator<string>(files);

            if (handle != IntPtr.Zero)
            {
                ScreensaverWindow window = new(configuration, handle);
                window.Show();
                windows.Add(window);
                RestartWorker();
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

        private void EnumerateMonitors()
        {
            if (resetting) return;
            if (configuration == null) return;

            resetting = true;

            foreach (Window window in windows)
            {
                window.Close();
            }

            windows.Clear();

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                ScreensaverWindow window = new(configuration, lprcMonitor);
                window.Show();
                windows.Add(window);
                window.DisplayChanged += OnDisplayChanged;
                return true;
            }, IntPtr.Zero);

            RestartWorker();

            resetting = false;
        }

        private void OnDisplayChanged()
        {
            EnumerateMonitors();
        }

        private void RestartWorker()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            task?.Wait();

            task = Task.Run(Worker).ContinueWith(OnWorkerTaskFaulted, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OnWorkerTaskFaulted(Task task)
        {
            Debug.WriteLine(task.Exception);

            foreach (ScreensaverWindow window in windows)
            {
                window.errorTextBlock.Text = string.Join("\n", task.Exception!.InnerExceptions.Select(ex => ex.Message));
            }
        }

        private async Task Worker()
        {
            Dispatcher uiDispatcher = Application.Current.Dispatcher;

            await uiDispatcher.InvokeAsync(() =>
            {
                foreach (ScreensaverWindow window in windows)
                {
                    window.errorTextBlock.Text = null;
                }
            });

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            TaskCompletionSource? taskCompletionSource = null;
            cancellationToken.Register(() => taskCompletionSource?.TrySetCanceled());

            while (!cancellationToken.IsCancellationRequested)
            {
                Storyboard?[] storyboards = new Storyboard?[windows.Count];
                CountdownEvent countdownEvent = new(windows.Count);

                for (int i = 0; i < windows.Count; i++)
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
            while (fileEnumerator!.Count > 0)
            {
                if (!fileEnumerator.MoveNext())
                {
                    fileEnumerator.Reset();
                    fileEnumerator.MoveNext();
                }

                string filePath = fileEnumerator.Current;

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
                    fileEnumerator.Remove(fileEnumerator.Current);
                }
            }

            throw new Exception("No images could be loaded");
        }

        private Storyboard CreateStoryboardAnimation(Panel container, Image image, Size imageSize, CountdownEvent countdownEvent)
        {
            if (configuration == null) return new Storyboard();

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

            Storyboard.SetTargetProperty(marginAnimation, new PropertyPath(Image.MarginProperty));
            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(Image.WidthProperty));
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(Image.HeightProperty));
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(Image.OpacityProperty));

            Storyboard storyboard = new();

            storyboard.Children.Add(marginAnimation);
            storyboard.Children.Add(widthAnimation);
            storyboard.Children.Add(heightAnimation);
            storyboard.Children.Add(opacityAnimation);

            storyboard.Duration = new(TimeSpan.FromSeconds(totalDuration));

            bool nextStarted = false;
            
            storyboard.CurrentTimeInvalidated += (sender, args) =>
            {
                if (!nextStarted && storyboard.GetCurrentTime() >= TimeSpan.FromSeconds(duration + fadeDuration))
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
