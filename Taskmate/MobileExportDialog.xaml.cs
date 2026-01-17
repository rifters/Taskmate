using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Taskmate
{
    public partial class MobileExportDialog : Window
    {
        private List<AssignmentResult> assignments;
        private string title;

        public MobileExportDialog(List<AssignmentResult> assignments, string title)
        {
            InitializeComponent();
            this.assignments = assignments;
            this.title = title;
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Title = "Save Mobile Export",
                    Filter = "HTML Files (*.html)|*.html",
                    FileName = $"mobile_assignments_{DateTime.Now:yyyyMMdd_HHmmss}.html"
                };

                if (sfd.ShowDialog() == true)
                {
                    string folderPath = Path.GetDirectoryName(sfd.FileName)!;
                    string htmlPath = MobileExporter.SaveMobileExport(assignments, title, folderPath);
                    
                    var result = MessageBox.Show(
                        $"Mobile export created successfully!\n\n" +
                        $"Files saved to: {folderPath}\n\n" +
                        $"✓ HTML page: {Path.GetFileName(htmlPath)}\n" +
                        $"✓ QR code: qr_*.png\n\n" +
                        $"Open the HTML file now?",
                        "Export Successful",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = htmlPath,
                            UseShellExecute = true
                        });
                    }

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}