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
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WpfKenBurns.Native
{
    internal partial class NativeMethods
    {
        internal delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [LibraryImport("user32.dll")]
        [SupportedOSPlatform("windows")]
        internal static partial IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [SupportedOSPlatform("windows")]
        internal static nint SetWindowLong(IntPtr hWnd, int nIndex, nint dwNewLong) => Environment.Is64BitProcess ? SetWindowLong64(hWnd, nIndex, dwNewLong) : SetWindowLong32(hWnd, nIndex, dwNewLong);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongA", SetLastError = true)]
        [SupportedOSPlatform("windows")]
        internal static partial nint SetWindowLong32(IntPtr hWnd, int nIndex, nint dwNewLong);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrA", SetLastError = true)]
        [SupportedOSPlatform("windows")]
        internal static partial nint SetWindowLong64(IntPtr hWnd, int nIndex, nint dwNewLong);

        [LibraryImport("user32.dll")]
        [SupportedOSPlatform("windows")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [LibraryImport("user32.dll")]
        [SupportedOSPlatform("windows")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [LibraryImport("user32.dll", SetLastError = true)]
        [SupportedOSPlatform("windows")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [LibraryImport("user32.dll", SetLastError = true)]
        [SupportedOSPlatform("windows")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetLastInputInfo(ref LASTINPUTINFO plii);
    }
}
