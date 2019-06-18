using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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
                    MessageBox.Show("Not implemented yet!");
                    Application.Current.Shutdown();
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
            ScreensaverWindow window = new ScreensaverWindow(handle);
            window.Show();
        }
    }
}
