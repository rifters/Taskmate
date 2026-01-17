using System.Windows;

namespace Taskmate
{
    public partial class AddNotesDialog : Window
    {
        // Make sure this is a PUBLIC property
        public string Notes => txtNotes.Text?.Trim() ?? string.Empty;

        public AddNotesDialog(string existingNotes = "")
        {
            InitializeComponent();
            
            // Ensure txtNotes is set before accessing
            if (!string.IsNullOrEmpty(existingNotes))
            {
                txtNotes.Text = existingNotes;
            }
            
            txtNotes.Focus();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}