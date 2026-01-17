using System.Windows;

namespace Taskmate
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; private set; } = string.Empty;

        public InputDialog(string title, string prompt)
        {
            InitializeComponent();
            Title = title;
            txtPrompt.Text = prompt;
            txtInput.Focus();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = txtInput.Text;
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