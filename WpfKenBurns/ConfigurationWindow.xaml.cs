// WpfKenBurns - A simple Ken Burns-style screensaver
// Copyright © 2019-2021 Nicolas Gnyra

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
using System.Diagnostics;
using System.Windows;

namespace WpfKenBurns
{
    /// <summary>
    /// Logique d'interaction pour ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        private Configuration? configuration;
        private bool changed;

        public ConfigurationWindow()
        {
            InitializeComponent();
        }

        private void UpdateConfiguration(Configuration config)
        {
            if (configuration != null)
            {
                configuration.PropertyChanged -= OnConfigurationPropertyChanged;
            }

            configuration = config;
            DataContext = config;
            config.PropertyChanged += OnConfigurationPropertyChanged;
        }

        private void OnConfigurationPropertyChanged(object? sender, EventArgs e)
        {
            changed = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateConfiguration(Configuration.Load());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("Failed to load configuration: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateConfiguration(new Configuration());
            }
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (configuration == null) return;

            try
            {
                VistaFolderBrowserDialog dialog = new();

                if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.SelectedPath)) return;

                configuration.Folders.Add(new ScreensaverImageFolder(dialog.SelectedPath, false));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("Failed to load folder browser");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (configuration == null) return;

            try
            {
                Configuration.Save(configuration);
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("Failed to save configuration: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to reset all settings?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                UpdateConfiguration(new Configuration());
                changed = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (changed)
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to quit? Any changes will be lost.", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

                if (result != MessageBoxResult.Yes) return;
            }

            Close();
        }

        private void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (configuration == null) return;

            if (imageSourcesListView.SelectedIndex >= 0)
            {
                configuration.Folders.RemoveAt(imageSourcesListView.SelectedIndex);
            }
        }

        private void FoldersListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            removeSelectedFolderButton.IsEnabled = e.AddedItems.Count > 0;
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (configuration == null) return;

            try
            {
                VistaOpenFileDialog dialog = new();

                dialog.Filter = "Executable Files (*.exe)|*.exe";

                if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FileName)) return;

                configuration.ProgramDenylist.Add(dialog.FileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("Failed to load folder browser");
            }
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (configuration == null) return;

            if (programDenylistListView.SelectedIndex >= 0)
            {
                configuration.ProgramDenylist.RemoveAt(programDenylistListView.SelectedIndex);
            }
        }

        private void ProgramDenylistListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            removeSelectedFileButton.IsEnabled = e.AddedItems.Count > 0;
        }
    }
}
