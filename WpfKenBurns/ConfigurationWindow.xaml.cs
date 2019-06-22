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
using System.ComponentModel;
using System.Diagnostics;
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
            DataContext = ConfigurationManager.Configuration;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(object sender, string propertyName)
        {
            PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigurationManager.Load();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("Failed to load configuration: " + ex.Message);
            }

            foldersListView.ItemsSource = ConfigurationManager.Configuration.Folders;
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();

                if (dialog.ShowDialog() != true) return;

                ConfigurationManager.Configuration.Folders.Add(new ScreensaverImageFolder
                {
                    Path = dialog.SelectedPath,
                    Recursive = false
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("Failed to load folder browser");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigurationManager.Save();
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("Failed to save configuration: " + ex.Message);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to reset all settings?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                ConfigurationManager.Reset();
                DataContext = ConfigurationManager.Configuration;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to quit? Any changes will be lost.", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                Close();
            }
        }

        private void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (foldersListView.SelectedIndex >= 0)
            {
                ConfigurationManager.Configuration.Folders.RemoveAt(foldersListView.SelectedIndex);
            }
        }

        private void FoldersListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            removeSelectedFolderButton.IsEnabled = foldersListView.SelectedIndex != -1;
        }
    }
}
