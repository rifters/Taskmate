using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Taskmate
{
    public partial class SwapDialog : Window
    {
        public string Person1 { get; private set; } = string.Empty;
        public string Person2 { get; private set; } = string.Empty;

        public SwapDialog(List<string> people, string selectedPerson = "")
        {
            InitializeComponent();

            cmbPerson1.ItemsSource = people;
            cmbPerson2.ItemsSource = people;

            if (!string.IsNullOrEmpty(selectedPerson) && people.Contains(selectedPerson))
            {
                cmbPerson1.SelectedItem = selectedPerson;
            }
            else if (people.Count > 0)
            {
                cmbPerson1.SelectedIndex = 0;
            }

            if (people.Count > 1)
            {
                cmbPerson2.SelectedIndex = 1;
            }
        }

        private void btnSwap_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPerson1.SelectedItem == null || cmbPerson2.SelectedItem == null)
            {
                MessageBox.Show("Please select both people to swap.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Person1 = cmbPerson1.SelectedItem?.ToString() ?? string.Empty;
            Person2 = cmbPerson2.SelectedItem?.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(Person1) || string.IsNullOrEmpty(Person2))
            {
                MessageBox.Show("Invalid selection.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Person1 == Person2)
            {
                MessageBox.Show("Please select two different people.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
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