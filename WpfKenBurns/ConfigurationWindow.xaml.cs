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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Ookii.Dialogs.Wpf;

namespace WpfKenBurns
{
    public partial class ConfigurationWindow : Window
    {
        private Configuration? configuration;
        private bool changed;

        /// <inheritdoc/>
        public ConfigurationWindow()
        {
            InitializeComponent();
        }

        private void UpdateConfiguration(Configuration config)
        {
            configuration?.PropertyChanged -= OnConfigurationPropertyChanged;
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
            if (configuration == null)
            {
                return;
            }

            try
            {
                VistaFolderBrowserDialog dialog = new()
                {
                    Multiselect = true,
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                foreach (string folder in dialog.SelectedPaths)
                {
                    if (configuration.Folders.Any(f => f.Path == folder))
                    {
                        continue;
                    }

                    configuration.Folders.Add(new ScreensaverImageFolder(folder, true));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("Failed to load folder browser");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (configuration == null)
            {
                return;
            }

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

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            Close();
        }

        private void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (configuration == null)
            {
                return;
            }

            foreach (ScreensaverImageFolder folder in imageSourcesListView.SelectedItems.Cast<ScreensaverImageFolder>().ToList())
            {
                configuration.Folders.Remove(folder);
            }
        }

        private void FoldersListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            removeSelectedFolderButton.IsEnabled = e.AddedItems.Count > 0;
        }
    }
}
