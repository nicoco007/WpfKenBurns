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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfKenBurns.Native;

namespace WpfKenBurns
{
    public partial class ScreensaverWindow : Window
    {
        private record Img(
            Image Image,
            Storyboard Storyboard,
            ThicknessAnimation MarginAnimation,
            DoubleAnimation WidthAnimation,
            DoubleAnimation HeightAnimation,
            DoubleAnimation OpacityAnimation);

        /// <summary>
        /// Message that is sent to all windows when the display resolution has changed. Equal to WM_DISPLAYCHANGE.
        /// </summary>
        private const int WindowMessageDisplayChange = 0x007e;

        private readonly IntPtr windowHandle;
        private readonly Configuration configuration;
        private readonly Random random = new();

        private readonly Img[] images = new Img[2];

        private int currentSlot = 0;

        private TaskCompletionSource? faded;
        private TaskCompletionSource? startNext;

        internal ScreensaverWindow(Configuration config, IntPtr previewHandle)
            : this(config)
        {
            // Set the preview window as the parent of this window
            NativeMethods.SetParent(windowHandle, previewHandle);

            // Make this a child window so it will close when the parent dialog closes
            // GWL_STYLE = -16, WS_CHILD = 0x40000000
            NativeMethods.SetWindowLong(windowHandle, -16, 0x40000000);

            // Place our window inside the parent
            NativeMethods.GetClientRect(previewHandle, out RECT parentRect);

            NativeMethods.SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, parentRect.Width, parentRect.Height, SetWindowPosFlags.ShowWindow);
        }

        internal ScreensaverWindow(Configuration config, RECT monitor)
            : this(config)
        {
            Topmost = true;
            Cursor = Cursors.None;

            NativeMethods.SetWindowPos(windowHandle, IntPtr.Zero, monitor.X, monitor.Y, monitor.Width, monitor.Height, SetWindowPosFlags.ShowWindow);
        }

        private ScreensaverWindow(Configuration config)
        {
            InitializeComponent();
            configuration = config;
            windowHandle = new WindowInteropHelper(this).EnsureHandle();

            for (int i = 0; i < 2; i++)
            {
                Image image = new()
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Stretch = Stretch.UniformToFill,
                    Opacity = 0,
                };

                RenderOptions.SetBitmapScalingMode(image, configuration.Quality);
                grid.Children.Add(image);

                TimeSpan duration = TimeSpan.FromSeconds(configuration.Duration);
                TimeSpan fadeDuration = TimeSpan.FromSeconds(configuration.FadeDuration);
                TimeSpan totalDuration = duration + fadeDuration * 2;

                ThicknessAnimation marginAnimation = new(default, default, totalDuration);
                DoubleAnimation widthAnimation = new(default, default, totalDuration);
                DoubleAnimation heightAnimation = new(default, default, totalDuration);
                DoubleAnimation opacityAnimation = new(default, default, fadeDuration);

                Storyboard.SetTarget(marginAnimation, image);
                Storyboard.SetTarget(widthAnimation, image);
                Storyboard.SetTarget(heightAnimation, image);
                Storyboard.SetTarget(opacityAnimation, image);

                Storyboard.SetTargetProperty(marginAnimation, new PropertyPath(MarginProperty));
                Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(WidthProperty));
                Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(HeightProperty));
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));

                Storyboard storyboard = new()
                {
                    Duration = new(totalDuration),
                };

                storyboard.Children.Add(marginAnimation);
                storyboard.Children.Add(widthAnimation);
                storyboard.Children.Add(heightAnimation);
                storyboard.Children.Add(opacityAnimation);

                images[i] = new(image, storyboard, marginAnimation, widthAnimation, heightAnimation, opacityAnimation);
            }

            HwndSource source = HwndSource.FromHwnd(windowHandle);

            source.AddHook(WndProc);
        }

        internal event Action? DisplayChanged;

        internal Task WaitForFaded() => faded?.Task ?? Task.CompletedTask;

        internal Task WaitForNext() => startNext?.Task ?? Task.CompletedTask;

        internal void PrepareNextImage(BitmapImage source)
        {
            Img img = images[currentSlot];

            Storyboard storyboard = img.Storyboard;
            storyboard.Remove();

            double scale = Math.Max(ActualWidth / source.PixelWidth, ActualHeight / source.PixelHeight);
            Size imageSize = new(source.PixelWidth * scale, source.PixelHeight * scale);

            Image image = img.Image;
            image.Source = source;
            image.Width = imageSize.Width;
            image.Height = imageSize.Height;
            image.Opacity = 0;
            Panel.SetZIndex(image, 1);
            Panel.SetZIndex(images[1 - currentSlot].Image, 0);

            double width = grid.ActualWidth;
            double height = grid.ActualHeight;

            double movementFactor = configuration.MovementFactor + 1;
            double scaleFactor = configuration.ScaleFactor + 1;
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

            ThicknessAnimation marginAnimation = img.MarginAnimation;
            marginAnimation.From = new Thickness(p1.X, p1.Y, 0, 0);
            marginAnimation.To = new Thickness(p2.X, p2.Y, 0, 0);

            DoubleAnimation widthAnimation = img.WidthAnimation;
            widthAnimation.From = imageSize.Width * movementFactor * fromScale;
            widthAnimation.To = imageSize.Width * movementFactor * toScale;

            DoubleAnimation heightAnimation = img.HeightAnimation;
            heightAnimation.From = imageSize.Height * movementFactor * fromScale;
            heightAnimation.To = imageSize.Height * movementFactor * toScale;

            DoubleAnimation opacityAnimation = img.OpacityAnimation;
            opacityAnimation.From = 0;
            opacityAnimation.To = 1;

            TimeSpan durationToNext = TimeSpan.FromSeconds(configuration.Duration + configuration.FadeDuration);
            TimeSpan fadeSpan = TimeSpan.FromSeconds(configuration.FadeDuration);

            void TriggerFaded(object? sender, EventArgs e)
            {
                TimeSpan current = ((Clock)sender!).CurrentTime!.Value;

                if (current >= fadeSpan)
                {
                    faded?.TrySetResult();
                    storyboard.CurrentTimeInvalidated -= TriggerFaded;
                }
            }

            void TriggerStartNext(object? sender, EventArgs e)
            {
                TimeSpan current = ((Clock)sender!).CurrentTime!.Value;

                if (current >= durationToNext)
                {
                    startNext?.TrySetResult();
                    storyboard.CurrentTimeInvalidated -= TriggerStartNext;
                }
            }

            storyboard.CurrentTimeInvalidated += TriggerFaded;
            storyboard.CurrentTimeInvalidated += TriggerStartNext;
        }

        internal void StartNext()
        {
            faded = new TaskCompletionSource();
            startNext = new TaskCompletionSource();
            images[currentSlot].Storyboard.Begin();
            currentSlot = 1 - currentSlot;
        }

        private static Point GetPointOnRectangleFromAngle(double angle, double width, double height)
        {
            double max = Math.Sqrt(Math.Pow(width / 2, 2) + Math.Pow(height / 2, 2));

            double x = Math.Clamp(Math.Cos(angle) * max, -width / 2, width / 2);
            double y = Math.Clamp(Math.Sin(angle) * max, -height / 2, height / 2);

            return new Point(x - width / 2, y - height / 2);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WindowMessageDisplayChange)
            {
                DisplayChanged?.Invoke();
            }

            return IntPtr.Zero;
        }
    }
}
