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
using System.Linq;
using System.Threading;
using System.Windows;

namespace WpfKenBurns
{
    public partial class App : Application
    {
        private Mutex? mutex;

        /// <inheritdoc/>
        protected override void OnStartup(StartupEventArgs e)
        {
            mutex = new(true, "WpfKenBurns-a5e92dd9-5f91-4005-9667-0d6f8dc1bf4b", out bool createdNew);

            if (!createdNew)
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);

            if (e.Args.Length >= 1)
            {
                string mode = e.Args[0].Trim();

                if (mode.StartsWith("/s", StringComparison.OrdinalIgnoreCase))
                {
                    WindowSynchronizer sync = new();
                    sync.Start();
                    return;
                }
                else if (mode.StartsWith("/p", StringComparison.OrdinalIgnoreCase))
                {
                    string? strHandle = e.Args.Length >= 2 ? e.Args[1] : mode.Split(':').ElementAtOrDefault(1);

                    if (IntPtr.TryParse(strHandle, out IntPtr handle))
                    {
                        WindowSynchronizer sync = new(handle);
                        sync.Start();
                        return;
                    }
                }
            }

            ConfigurationWindow window = new();
            window.Show();
        }

        /// <inheritdoc/>
        protected override void OnExit(ExitEventArgs e)
        {
            mutex?.ReleaseMutex();
            mutex?.Dispose();

            base.OnExit(e);
        }
    }
}
