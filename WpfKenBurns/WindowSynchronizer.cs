// <copyright file="WindowSynchronizer.cs" company="PlaceholderCompany">
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
using WpfKenBurns.Native;

namespace WpfKenBurns
{
    internal class WindowSynchronizer
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

        public WindowSynchronizer()
        {
        }

        public WindowSynchronizer(IntPtr handle)
        {
            this.handle = handle;
        }

        public void Start()
        {
            try
            {
                this.configuration = Configuration.Load();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("Failed to load configuration: " + ex.Message);
                Application.Current.Shutdown();
                return;
            }

            foreach (string filePath in this.configuration.ProgramDenylist)
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

            foreach (ScreensaverImageFolder folder in this.configuration.Folders)
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

            this.fileEnumerator = new RandomizedEnumerator<string>(files);

            if (this.handle != IntPtr.Zero)
            {
                ScreensaverWindow window = new(this.configuration, this.handle);
                window.Show();
                this.windows.Add(window);
                this.StartWorker();
            }
            else
            {
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                this.EnumerateMonitors();
            }

            Application.Current.Exit += (sender, e) =>
            {
                this.cancellationTokenSource?.Cancel();
                this.cancellationTokenSource?.Dispose();
            };
        }

        private static Point GetPointOnRectangleFromAngle(double angle, double width, double height)
        {
            double max = Math.Sqrt(Math.Pow(width / 2, 2) + Math.Pow(height / 2, 2));

            double x = Math.Clamp(Math.Cos(angle) * max, -width / 2, width / 2);
            double y = Math.Clamp(Math.Sin(angle) * max, -height / 2, height / 2);

            return new Point(x - width / 2, y - height / 2);
        }

        private void EnumerateMonitors()
        {
            if (this.resetting)
            {
                return;
            }

            if (this.configuration == null)
            {
                return;
            }

            this.resetting = true;
            this.StopWorker();

            foreach (Window window in this.windows)
            {
                window.Close();
            }

            this.windows.Clear();

            NativeMethods.EnumDisplayMonitors(
                IntPtr.Zero,
                IntPtr.Zero,
                (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
                {
                    ScreensaverWindow window = new(this.configuration, lprcMonitor);
                    window.Show();
                    this.windows.Add(window);
                    window.DisplayChanged += this.OnDisplayChanged;
                    return true;
                },
                IntPtr.Zero);

            this.StartWorker();

            this.resetting = false;
        }

        private void OnDisplayChanged()
        {
            this.EnumerateMonitors();
        }

        private void StopWorker()
        {
            this.cancellationTokenSource?.Cancel();
            this.cancellationTokenSource?.Dispose();
            this.cancellationTokenSource = null;

            this.task?.Wait();
        }

        private void StartWorker()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.task = Task.Run(() => this.Worker(this.cancellationTokenSource.Token), this.cancellationTokenSource.Token).ContinueWith(this.OnWorkerTaskCompleted);
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
                foreach (ScreensaverWindow window in this.windows)
                {
                    window.errorTextBlock.Text = string.Join("\n", task.Exception!.InnerExceptions.Select(ex => ex.Message));
                }
            });
        }

        private async Task Worker(CancellationToken cancellationToken)
        {
            Dispatcher uiDispatcher = Application.Current.Dispatcher;

            await uiDispatcher.InvokeAsync(() =>
            {
                foreach (ScreensaverWindow window in this.windows)
                {
                    window.errorTextBlock.Text = null;
                }
            });

            TaskCompletionSource? taskCompletionSource = null;
            cancellationToken.Register(() => taskCompletionSource?.TrySetCanceled());

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Storyboard?[] storyboards = new Storyboard?[this.windows.Count];
                CountdownEvent countdownEvent = new(this.windows.Count);

                for (int i = 0; i < this.windows.Count; i++)
                {
                    ScreensaverWindow window = this.windows[i];
                    BitmapImage source = this.GetImage();

                    double scale = Math.Max(window.ActualWidth / source.PixelWidth, window.ActualHeight / source.PixelHeight);
                    Size size = new(source.PixelWidth * scale, source.PixelHeight * scale);

                    Image image = await uiDispatcher.InvokeAsync(() => window.CreateImage(source, size));

                    Panel container = (Panel)image.Parent;

                    ManualResetEventSlim resetEvent = new(false);
                    storyboards[i] = this.CreateStoryboardAnimation(container, image, size, countdownEvent);
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
            while (this.fileEnumerator!.Count > 0)
            {
                if (!this.fileEnumerator.MoveNext())
                {
                    this.fileEnumerator.Reset();
                    this.fileEnumerator.MoveNext();
                }

                string filePath = this.fileEnumerator.Current;

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
                    this.fileEnumerator.Remove(this.fileEnumerator.Current);
                }
            }

            throw new Exception("No images could be loaded");
        }

        private Storyboard CreateStoryboardAnimation(Panel container, Image image, Size imageSize, CountdownEvent countdownEvent)
        {
            if (this.configuration == null)
            {
                return new Storyboard();
            }

            double duration = this.configuration.Duration;
            double movementFactor = this.configuration.MovementFactor + 1;
            double scaleFactor = this.configuration.ScaleFactor + 1;
            double fadeDuration = this.configuration.FadeDuration;
            double totalDuration = duration + fadeDuration * 2;

            double width = container.ActualWidth;
            double height = container.ActualHeight;

            ThicknessAnimation marginAnimation = new();
            DoubleAnimation widthAnimation = new();
            DoubleAnimation heightAnimation = new();
            DoubleAnimation opacityAnimation = new();

            bool zoomDirection = this.random.Next(2) == 1;
            double fromScale = zoomDirection ? scaleFactor : 1;
            double toScale = zoomDirection ? 1 : scaleFactor;

            double angle = this.random.NextDouble() * Math.PI * 2;

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
    }
}
