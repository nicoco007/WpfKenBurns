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

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int count = windows.Count;

                for (int i = 0; i < count; i++)
                {
                    ScreensaverWindow window = windows[i];
                    BitmapImage source = GetImage();
                    await uiDispatcher.InvokeAsync(() => window.PrepareNextImage(source));
                }

                await Task.WhenAll(windows.Select(w => w.WaitForNext())).WaitAsync(cancellationToken);

                await uiDispatcher.InvokeAsync(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        windows[i].StartNext();
                    }
                });

                await Task.WhenAll(windows.Select(w => w.WaitForFaded())).WaitAsync(cancellationToken);
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
