using System.Linq;
using System.Windows;

namespace Taskmate
{
    public partial class TagSelectionDialog : Window
    {
        public string SelectedTag { get; private set; } = "General";

        public TagSelectionDialog()
        {
            InitializeComponent();
            LoadTags();
        }

        private void LoadTags()
        {
            cmbTag.Items.Clear();
            
            // Load tags from TagManager
            var tags = TagManager.GetAllTags();
            
            foreach (var tag in tags)
            {
                cmbTag.Items.Add(tag);
            }
            
            // Also add tags from history that might not be in the manager yet
            var historyTags = AssignmentHistoryManager.GetAllTags();
            foreach (var tag in historyTags)
            {
                if (!tags.Contains(tag))
                {
                    cmbTag.Items.Add(tag);
                }
            }
            
            if (cmbTag.Items.Count > 0)
                cmbTag.SelectedIndex = 0;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            SelectedTag = cmbTag.Text?.Trim() ?? "General";
            if (string.IsNullOrEmpty(SelectedTag))
                SelectedTag = "General";
            
            // Add to tag manager if it's new
            TagManager.AddTag(SelectedTag);
            
            DialogResult = true;
            Close();
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            SelectedTag = "General";
            DialogResult = true;
            Close();
        }

        private void btnManageTags_Click(object sender, RoutedEventArgs e)
        {
            var tagManager = new TagManagerWindow
            {
                Owner = this
            };
            
            tagManager.ShowDialog();
            
            // Reload tags after managing
            string currentSelection = cmbTag.Text ?? string.Empty;
            LoadTags();
            
            // Try to restore selection
            if (!string.IsNullOrEmpty(currentSelection))
            {
                cmbTag.Text = currentSelection;
            }
        }
    }
}