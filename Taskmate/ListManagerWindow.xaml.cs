using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace Taskmate
{
    public partial class ListManagerWindow : Window
    {
        public List<string> Items { get; private set; }
        public string ListType { get; private set; }

        public ListManagerWindow(List<string> existingItems, string listType)
        {
            InitializeComponent();
            Items = new List<string>(existingItems);
            ListType = listType;
            Title = $"Manage {listType}";
            RefreshList();
        }

        private void RefreshList()
        {
            lstItems.ItemsSource = null;
            lstItems.ItemsSource = Items;
            txtCount.Text = $"Total: {Items.Count}";
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string item = txtNewItem.Text.Trim();
            if (!string.IsNullOrEmpty(item))
            {
                Items.Add(item);
                txtNewItem.Clear();
                RefreshList();
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lstItems.SelectedItem != null)
            {
                string? itemToRemove = lstItems.SelectedItem.ToString();
                if (itemToRemove != null)
                {
                    Items.Remove(itemToRemove);
                    RefreshList();
                }
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Clear all items?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Items.Clear();
                RefreshList();
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = $"Export {ListType}",
                Filter = "Text Files (*.txt)|*.txt|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                FileName = $"{ListType.ToLower()}_{DateTime.Now:yyyyMMdd}.txt"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    if (sfd.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        string json = JsonSerializer.Serialize(Items, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(sfd.FileName, json);
                    }
                    else
                    {
                        File.WriteAllLines(sfd.FileName, Items);
                    }
                    MessageBox.Show($"Exported {Items.Count} items to {Path.GetFileName(sfd.FileName)}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = $"Import {ListType}",
                Filter = "Text Files (*.txt)|*.txt|JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    List<string> importedItems;

                    if (ofd.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        string json = File.ReadAllText(ofd.FileName);
                        importedItems = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    }
                    else
                    {
                        importedItems = File.ReadAllLines(ofd.FileName)
                            .Select(line => line.Trim())
                            .Where(line => !string.IsNullOrEmpty(line))
                            .ToList();
                    }

                    Items.AddRange(importedItems);
                    RefreshList();
                    MessageBox.Show($"Imported {importedItems.Count} items", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}