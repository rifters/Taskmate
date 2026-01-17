using System.Collections.Generic;
using System.Windows;

namespace Taskmate
{
    public partial class HistoryWindow : Window
    {
        public HistoryWindow(List<AssignmentHistoryEntry> history)
        {
            InitializeComponent();
            dgHistory.ItemsSource = history;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}