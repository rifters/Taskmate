using System.Windows;
using System.Windows.Controls;

namespace Taskmate
{
    public partial class BulkEditDialog : Window
    {
        public bool ApplyCategory => chkCategory.IsChecked == true;
        public bool ApplyDifficulty => chkDifficulty.IsChecked == true;
        public bool ApplyTime => chkTime.IsChecked == true;

        public string? SelectedCategory => cmbCategory.SelectedItem?.ToString();
        public string? SelectedDifficulty => (cmbDifficulty.SelectedItem as ComboBoxItem)?.Content?.ToString();
        public int TimeValue => int.TryParse(txtTime.Text, out int val) && val > 0 ? val : 15;

        public BulkEditDialog(int taskCount)
        {
            InitializeComponent();
            txtCount.Text = taskCount.ToString();
            
            // Load categories
            cmbCategory.ItemsSource = CategoryManager.GetAllCategories();
            if (cmbCategory.Items.Count > 0)
                cmbCategory.SelectedIndex = 0;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (!ApplyCategory && !ApplyDifficulty && !ApplyTime)
            {
                MessageBox.Show("Please select at least one property to apply.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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