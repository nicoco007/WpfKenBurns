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
using System.Linq;
using System.Windows;

namespace WpfKenBurns
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length >= 1)
            {
                string mode = e.Args[0].Trim().ToLower();

                if (mode.StartsWith("/c"))
                {
                    ConfigurationWindow window = new ConfigurationWindow();
                    window.Show();
                }
                else if (mode.StartsWith("/p"))
                {
                    string strHandle;

                    if (e.Args.Length >= 2)
                    {
                        strHandle = e.Args[1];
                    }
                    else
                    {
                        strHandle = e.Args[0].Split(':').LastOrDefault();
                    }

                    if (int.TryParse(strHandle, out var intHandle))
                    {
                        ShowScreensaver(new IntPtr(intHandle));
                    }
                }
                else
                {
                    ShowScreensaver(IntPtr.Zero);
                }
            }
            else
            {
                ShowScreensaver(IntPtr.Zero);
            }
        }

        private void ShowScreensaver(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                ScreensaverWindow window = new ScreensaverWindow(handle);
                window.Show();
            }
            else
            {
                NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
                {
                    ScreensaverWindow window = new ScreensaverWindow(lprcMonitor);
                    window.Show();
                    return true;
                }, IntPtr.Zero);
            }
        }
    }
}
