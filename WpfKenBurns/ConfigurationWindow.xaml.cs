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

using Ookii.Dialogs.Wpf;
using System;
using System.Windows;

namespace WpfKenBurns
{
    /// <summary>
    /// Logique d'interaction pour ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        public ConfigurationWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigurationManager.Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load configuration.");
            }

            foldersListView.ItemsSource = ConfigurationManager.Folders;
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();

                if (dialog.ShowDialog() != true) return;

                ConfigurationManager.Folders.Add(new ScreensaverImageFolder
                {
                    Path = dialog.SelectedPath,
                    Recursive = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigurationManager.Save();
            Close();
        }

        private void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (foldersListView.SelectedIndex >= 0)
            {
                ConfigurationManager.Folders.RemoveAt(foldersListView.SelectedIndex);
            }
        }

        private void FoldersListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            removeSelectedFolderButton.IsEnabled = foldersListView.SelectedIndex != -1;
        }
    }
}
