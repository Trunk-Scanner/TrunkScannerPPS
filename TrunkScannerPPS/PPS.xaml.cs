using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace TrunkScannerPPS
{
    public partial class PPS : Window
    {
        public Codeplug CurrentCodeplug { get; set; }

        public PPS()
        {
            InitializeComponent();
            Title = $"TrunkScanner PPS Version {App.appVersion}";
            PopulateModeComboBox();
        }

        private void PopulateModeComboBox()
        {
            ModeComboBox.ItemsSource = Enum.GetValues(typeof(ChannelMode));
        }

        private void ResetVisibility()
        {
            ScanManagementPanel.Visibility = Visibility.Collapsed;
            ChannelDetailsPanel.Visibility = Visibility.Collapsed;
            AddChannelBtn.Visibility = Visibility.Collapsed;
            DeleteChannelBtn.Visibility = Visibility.Collapsed;
            AddZoneBtn.Visibility = Visibility.Collapsed;
            DeleteZoneBtn.Visibility= Visibility.Collapsed;
            NameLabel.Visibility = Visibility.Collapsed;
            NameTextBox.Visibility = Visibility.Collapsed;
            NameTextBox.IsEnabled = true;
            TgidLabel.Visibility = Visibility.Collapsed;
            TgidTextBox.Visibility = Visibility.Collapsed;
            TgidTextBox.IsEnabled = true;
            FrequencyLabel.Visibility = Visibility.Collapsed;
            FrequencyTextBox.Visibility = Visibility.Collapsed;
            FrequencyTextBox.IsEnabled = true;
            ModeLabel.Visibility = Visibility.Collapsed;
            ModeComboBox.Visibility = Visibility.Collapsed;
            ModeComboBox.IsEnabled = true;
            PopulateModeComboBox();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Codeplug JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Codeplug File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string fileContent = File.ReadAllText(openFileDialog.FileName);

                if (CurrentCodeplug != null)
                    ClearTreeView();

                CurrentCodeplug = JsonConvert.DeserializeObject<Codeplug>(fileContent);

                if (!CurrentCodeplug.IsValid())
                {
                    MessageBox.Show("Invalid codeplug!");
                    return;
                }

                if (CurrentCodeplug.IsTrunkingInhibited())
                {
                    MessageBox.Show("Pager is INHIBITED!");
                        return;
                }

                PopulateTreeView();

                MessageBox.Show("Codeplug loaded successfully.");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            CurrentCodeplug.CodeplugVersion = App.appVersion;
            CurrentCodeplug.LastProgramSource = 0;
            CurrentCodeplug.LastProgrammedDate = DateTime.Now;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Codeplug JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Save Codeplug File"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string json = JsonConvert.SerializeObject(CurrentCodeplug, Formatting.Indented);
                File.WriteAllText(saveFileDialog.FileName, json);
                MessageBox.Show("Codeplug saved successfully.");
            }
        }

        private void ExpandAllItems(TreeViewItem item)
        {
            if (item == null) return;

            item.IsExpanded = true;
            foreach (TreeViewItem subitem in item.Items)
            {
                ExpandAllItems(subitem);
            }
        }

        private void PopulateTreeView()
        {
            MainTreeView.Items.Clear();

            var rootItem = new TreeViewItem { Header = $"{CurrentCodeplug.SerialNumber}" };
            var zonesNode = new TreeViewItem { Header = "Zones" };
            var scanListsNode = new TreeViewItem { Header = "Scan Lists" };
            var radioInfoNode = new TreeViewItem { Header = "Radio Info" };
            var radioWideNode = new TreeViewItem { Header = "Radio Wide" };

            foreach (var zone in CurrentCodeplug.Zones)
            {
                var zoneNode = new TreeViewItem { Header = zone.Name, Tag = zone };
                foreach (var channel in zone.Channels)
                {
                    string displayText = $"{channel.Alias}";
                    if (channel.Mode == ChannelMode.P25Conventional || channel.Mode == ChannelMode.AnalogConventional)
                    {
                        displayText += $" (Freq: {channel.Frequency})";
                    }
                    else
                    {
                        displayText += $" (TGID: {channel.Tgid})";
                    }
                    var channelNode = new TreeViewItem { Header = displayText, Tag = channel };
                    zoneNode.Items.Add(channelNode);
                }
                zonesNode.Items.Add(zoneNode);
            }

            foreach (var scanList in CurrentCodeplug.ScanLists)
            {
                var scanListNode = new TreeViewItem { Header = scanList.Name, Tag = scanList };
                foreach (var item in scanList.Items)
                {
                    string displayText = $"{item.Alias}";
                    if (item.Mode == ChannelMode.P25Conventional || item.Mode == ChannelMode.AnalogConventional)
                    {
                        displayText += $" (Freq: {item.Frequency})";
                    }
                    else
                    {
                        displayText += $" (TGID: {item.Tgid})";
                    }
                    var channelNode = new TreeViewItem { Header = displayText, Tag = item };
                    scanListNode.Items.Add(channelNode);
                }
                scanListsNode.Items.Add(scanListNode);
            }

            rootItem.Items.Add(radioInfoNode);
            rootItem.Items.Add(radioWideNode);
            rootItem.Items.Add(zonesNode);
            rootItem.Items.Add(scanListsNode);
            MainTreeView.Items.Add(rootItem);
            ExpandAllItems(rootItem); // TODO: This whole odreal is buggy. Maybe look at again in the future
        }



        private void ClearTreeView()
        {
            MainTreeView.Items.Clear();
        }


        private void MainTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ResetVisibility();

            if (MainTreeView.SelectedItem is TreeViewItem selectedItem)
            {
                if (selectedItem.Header.ToString() == "Radio Info")
                {
                    SetupRadioInfoControls();
                }
                else if (selectedItem.Header.ToString() == "Radio Wide")
                {
                    SetupRadioWideControls();
                }
                else if (selectedItem.Tag is Zone zone)
                {
                    NameLabel.Visibility = Visibility.Visible;
                    NameLabel.Text = "Zone: ";
                    NameTextBox.Text = zone.Name;
                    NameTextBox.Visibility = Visibility.Visible;
                    AddChannelBtn.Visibility = Visibility.Visible;
                    DeleteChannelBtn.Visibility = Visibility.Visible;
                    AddZoneBtn.Visibility = Visibility.Visible;
                    DeleteZoneBtn.Visibility = Visibility.Visible;
                }
                else if (selectedItem.Tag is Channel channel)
                {
                    PopulateModeComboBox();

                    NameTextBox.Text = channel.Alias;
                    NameLabel.Visibility = Visibility.Visible;
                    NameTextBox.Visibility = Visibility.Visible;
                    ChannelDetailsPanel.Visibility = Visibility.Visible;

                    if (channel.Mode == ChannelMode.P25Conventional || channel.Mode == ChannelMode.AnalogConventional)
                    {
                        FrequencyLabel.Text = "Frequency: ";
                        FrequencyLabel.Visibility = Visibility.Visible;
                        FrequencyTextBox.Visibility = Visibility.Visible;
                        FrequencyTextBox.Text = channel.Frequency;
                        TgidLabel.Visibility = Visibility.Collapsed;
                        TgidTextBox.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        TgidLabel.Text = "TGID: ";
                        TgidLabel.Visibility = Visibility.Visible;
                        TgidTextBox.Visibility = Visibility.Visible;
                        TgidTextBox.Text = channel.Tgid;
                        FrequencyLabel.Visibility = Visibility.Collapsed;
                        FrequencyTextBox.Visibility = Visibility.Collapsed;
                    }
                    ModeLabel.Text = "Mode: ";
                    ModeLabel.Visibility = Visibility.Visible;
                    ModeComboBox.Visibility = Visibility.Visible;
                    ModeComboBox.SelectedItem = channel.Mode;
                }
                else if (selectedItem.Tag is ScanList scanList)
                {
                    NameTextBox.Text = scanList.Name;
                    NameLabel.Text = "Name: ";
                    NameLabel.Visibility = Visibility.Visible;
                    NameTextBox.Visibility = Visibility.Visible;
                    ScanManagementPanel.Visibility = Visibility.Visible;

                    PopulateChannelComboBox(scanList);
                }
                else if (selectedItem.Tag is ScanListItem scanListItem)
                {
                    NameLabel.Visibility = Visibility.Collapsed;
                    NameTextBox.Visibility = Visibility.Collapsed;
                    
                    ScanManagementPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void UpdateTreeViewItemHeader(TreeViewItem item, string newHeader)
        {
            if (item != null)
            {
                item.Header = newHeader;
            }
        }

        private TreeViewItem FindTreeViewItemByTag(ItemsControl container, object tag)
        {
            if (container == null)
                return null;

            foreach (object item in container.Items)
            {
                var treeViewItem = container.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem != null && treeViewItem.Tag == tag)
                    return treeViewItem;

                var result = FindTreeViewItemByTag(treeViewItem, tag);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void SetupRadioInfoControls()
        {
            ResetVisibility();

            ChannelDetailsPanel.Visibility = Visibility.Visible;

            NameLabel.Visibility = Visibility.Visible;
            NameLabel.Text = "Last Program Date/Time: ";
            NameTextBox.Visibility = Visibility.Visible;
            NameTextBox.Text = CurrentCodeplug.LastProgrammedDate.ToString();
            NameTextBox.IsEnabled = false;

            FrequencyLabel.Visibility = Visibility.Visible;
            FrequencyLabel.Text = "Model: ";
            FrequencyTextBox.Visibility = Visibility.Visible;
            FrequencyTextBox.Text = CurrentCodeplug.ModelNumber;
            FrequencyTextBox.IsEnabled = false;

            TgidLabel.Visibility = Visibility.Visible;
            TgidLabel.Text = "Serial: ";
            TgidTextBox.Visibility = Visibility.Visible;
            TgidTextBox.Text = CurrentCodeplug.SerialNumber;
            TgidTextBox.IsEnabled = false;

            ModeLabel.Visibility = Visibility.Visible;
            ModeLabel.Text = "Last Program Source: ";
            ModeComboBox.Visibility = Visibility.Visible;
            ModeComboBox.ItemsSource = Enum.GetValues(typeof(CodeplugSource));
            ModeComboBox.SelectedIndex = CurrentCodeplug.LastProgramSource;
            ModeComboBox.IsEnabled = false;
        }

        private void SetupRadioWideControls()
        {
            ResetVisibility();

            ChannelDetailsPanel.Visibility = Visibility.Visible;

            FrequencyLabel.Visibility = Visibility.Visible;
            FrequencyLabel.Text = "Home Sys ID: ";
            FrequencyTextBox.Visibility = Visibility.Visible;
            FrequencyTextBox.Text = CurrentCodeplug.HomeSystemId;

            TgidLabel.Visibility = Visibility.Visible;
            TgidLabel.Text = "Born Sys ID: ";
            TgidTextBox.Visibility = Visibility.Visible;
            TgidTextBox.Text = CurrentCodeplug.BornSystemId;
            TgidTextBox.IsEnabled = false;

            ModeLabel.Visibility = Visibility.Visible;
            ModeLabel.Text = "Enforce Sys ID: ";
            ModeComboBox.Visibility = Visibility.Visible;
            ModeComboBox.ItemsSource = new List<string> { "Yes", "No" };
            ModeComboBox.SelectedItem = CurrentCodeplug.EnforceSystemId ? "Yes" : "No";
            ModeComboBox.IsEnabled = true;
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MainTreeView.SelectedItem is TreeViewItem selectedItem)
            {
                if (selectedItem.Tag is Zone zone)
                {
                    zone.Name = NameTextBox.Text;
                    UpdateTreeViewItemHeader(selectedItem, zone.Name);
                }
                else if (selectedItem.Tag is Channel channel)
                {
                    channel.Alias = NameTextBox.Text;
                    string displayText = $"{channel.Alias}";
                    if (channel.Mode == ChannelMode.P25Conventional || channel.Mode == ChannelMode.AnalogConventional)
                    {
                        displayText += $" (Freq: {channel.Frequency})";
                    }
                    else
                    {
                        displayText += $" (TGID: {channel.Tgid})";
                    }
                    UpdateTreeViewItemHeader(selectedItem, displayText);
                }
                else if (selectedItem.Tag is ScanList scanList)
                {
                    scanList.Name = NameTextBox.Text;
                    UpdateTreeViewItemHeader(selectedItem, scanList.Name);
                }
            }
        }


        private void AddChannel_Click(object sender, RoutedEventArgs e)
        {
            if (MainTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is Zone zone)
            {
                Channel newChannel = new Channel { Alias = "New Channel", Tgid = "1000", Frequency = "000.000", Mode = ChannelMode.P25Trunking };
                zone.Channels.Add(newChannel);
                TreeViewItem newChannelNode = new TreeViewItem { Header = newChannel.Alias, Tag = newChannel };
                selectedItem.Items.Add(newChannelNode);
                selectedItem.IsExpanded = true;
            }
            else
            {
                MessageBox.Show("Please select a zone to add a channel.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddZone_Click(object sender, RoutedEventArgs e)
        {
            string newZoneName = "New Zone";
            if (!CurrentCodeplug.Zones.Any(z => z.Name.Equals(newZoneName, StringComparison.OrdinalIgnoreCase)))
            {
                Zone newZone = new Zone { Name = newZoneName };
                CurrentCodeplug.Zones.Add(newZone);

                TreeViewItem zonesNode = FindTreeViewItem(MainTreeView, "Zones");
                if (zonesNode != null)
                {
                    TreeViewItem newZoneNode = new TreeViewItem { Header = newZone.Name, Tag = newZone };
                    zonesNode.Items.Add(newZoneNode);
                    zonesNode.IsExpanded = true;
                }
            }
            else
            {
                MessageBox.Show("A zone with this name already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TreeViewItem FindTreeViewItem(ItemsControl container, string name)
        {
            foreach (object item in container.Items)
            {
                TreeViewItem treeViewItem = container.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem != null)
                {
                    if (treeViewItem.Header.ToString() == name)
                    {
                        return treeViewItem;
                    }

                    TreeViewItem result = FindTreeViewItem(treeViewItem, name);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        private void DeleteZone_Click(object sender, RoutedEventArgs e)
        {
            if (MainTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is Zone zone)
            {
                if (MessageBox.Show($"Are you sure you want to delete the zone '{zone.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    CurrentCodeplug.Zones.Remove(zone);

                    TreeViewItem parentItem = selectedItem.Parent as TreeViewItem;
                    if (parentItem != null)
                    {
                        parentItem.Items.Remove(selectedItem);
                    }
                    else
                    {
                        MainTreeView.Items.Remove(selectedItem);
                    }

                    MessageBox.Show("Zone deleted successfully.", "Zone Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Please select a zone to delete.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void PopulateChannelComboBox(ScanList selectedScanList)
        {
            var allChannels = CurrentCodeplug.Zones.SelectMany(z => z.Channels).ToList();
            var filteredChannels = allChannels.Where(c => !selectedScanList.Items.Any(i => i.Tgid == c.Tgid)).ToList();
            ChannelComboBox.ItemsSource = filteredChannels;
            ChannelComboBox.DisplayMemberPath = "Alias";
        }

        private void DeleteChannel_Click(object sender, RoutedEventArgs e)
        {
            if (MainTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is Channel channel && selectedItem.Parent is TreeViewItem parentItem)
            {
                Zone parentZone = parentItem.Tag as Zone;
                parentZone.Channels.Remove(channel);
                parentItem.Items.Remove(selectedItem);
            }
            else
            {
                MessageBox.Show("Please select a channel to delete.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TgidTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MainTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is Channel channel)
            {
                channel.Tgid = TgidTextBox.Text;
                PopulateTreeView();
            }
        }

        private void FrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MainTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is Channel channel)
            {
                channel.Frequency = FrequencyTextBox.Text;
                PopulateTreeView();
            }
        }

        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is Channel channel)
            {
                if (ModeComboBox.SelectedItem != null)
                {
                    channel.Mode = (ChannelMode)ModeComboBox.SelectedItem;

                    if (channel.Mode == ChannelMode.P25Conventional || channel.Mode == ChannelMode.AnalogConventional)
                    {
                        FrequencyLabel.Text = "Frequency: ";
                        FrequencyLabel.Visibility = Visibility.Visible;
                        FrequencyTextBox.Visibility = Visibility.Visible;
                        TgidTextBox.Text = string.Empty;
                        TgidLabel.Visibility = Visibility.Collapsed;
                        TgidTextBox.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        TgidLabel.Text = "TGID: ";
                        TgidLabel.Visibility = Visibility.Visible;
                        TgidTextBox.Visibility = Visibility.Visible;
                        FrequencyTextBox.Text = string.Empty;
                        FrequencyLabel.Visibility = Visibility.Collapsed;
                        FrequencyTextBox.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }


        private void AddScanList_Click(object sender, RoutedEventArgs e)
        {
            string scanListName = NewScanListNameTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(scanListName) && !CurrentCodeplug.ScanLists.Any(sl => sl.Name.Equals(scanListName)))
            {
                ScanList newScanList = new ScanList { Name = scanListName };
                CurrentCodeplug.ScanLists.Add(newScanList);
                PopulateTreeView();
                MessageBox.Show("Scan list added successfully.");
            }
            else
            {
                MessageBox.Show("Scan list name cannot be empty or already exists.");
            }
        }

        private void DeleteScanList_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = MainTreeView.SelectedItem as TreeViewItem;
            if (selectedItem != null && selectedItem.Tag is ScanList scanList)
            {
                CurrentCodeplug.ScanLists.Remove(scanList);
                PopulateTreeView();
                MessageBox.Show("Scan list deleted successfully.");
            }
            else
            {
                MessageBox.Show("Please select a scan list to delete.");
            }
        }

        private void AddChannelToScanList_Click(object sender, RoutedEventArgs e)
        {
            if (MainTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is ScanList scanList &&
                ChannelComboBox.SelectedItem is Channel selectedChannel)
            {
                if (!scanList.Items.Any(i => i.Alias == selectedChannel.Alias && i.Tgid == selectedChannel.Tgid))
                {
                    scanList.Items.Add(new ScanListItem { Alias = selectedChannel.Alias, Tgid = selectedChannel.Tgid });
                    PopulateTreeView();
                    MessageBox.Show("Channel added to scan list.");
                }
                else
                {
                    MessageBox.Show("This channel is already in the scan list.");
                }
            }
            else
            {
                MessageBox.Show("Please select a scan list and a channel.");
            }
        }

        private void RemoveChannelFromScanList_Click(object sender, RoutedEventArgs e)
        {
            if (MainTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is ScanListItem selectedScanListItem)
            {
                TreeViewItem parentScanListTreeViewItem = selectedItem.Parent as TreeViewItem;
                if (parentScanListTreeViewItem?.Tag is ScanList parentScanList)
                {
                    parentScanList.Items.Remove(selectedScanListItem);
                    PopulateTreeView();
                    MessageBox.Show("Channel removed from scan list.");
                }
                else
                {
                    MessageBox.Show("Selected item is not part of a scan list.");
                }
            }
            else
            {
                MessageBox.Show("Please select a channel from a scan list to remove.");
            }
        }

        private void CloseCodeplug_Click(object sender, RoutedEventArgs e)
        {
            ResetVisibility();
            ClearTreeView();
            CurrentCodeplug = null;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Trunk Scan PPS\n" +
                $"Version: {App.appVersion}\n" +
                "Written by PhpSplutions\n"
                );
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            HelpDialog helpDialog = new HelpDialog();
            helpDialog.ShowDialog();
        }

        private void ChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
