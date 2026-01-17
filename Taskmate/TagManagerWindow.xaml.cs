using System.Windows;

namespace Taskmate
{
    public partial class TagManagerWindow : Window
    {
        public TagManagerWindow()
        {
            InitializeComponent();
            LoadTags();
        }

        private void LoadTags()
        {
            lstTags.ItemsSource = TagManager.GetAllTags();
        }

        private void lstTags_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            bool hasSelection = lstTags.SelectedItem != null;
            btnRename.IsEnabled = hasSelection;
            btnDelete.IsEnabled = hasSelection;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string newTag = txtNewTag.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(newTag))
            {
                MessageBox.Show("Please enter a tag name.", "Tag Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            TagManager.AddTag(newTag);
            LoadTags();
            txtNewTag.Clear();
            lstTags.SelectedItem = newTag;
        }

        private void btnRename_Click(object sender, RoutedEventArgs e)
        {
            if (lstTags.SelectedItem is string selectedTag)
            {
                var inputDialog = new InputDialog("Rename Tag", $"Rename '{selectedTag}' to:");
                if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.ResponseText))
                {
                    string newTag = inputDialog.ResponseText.Trim();
                    
                    if (newTag.Equals(selectedTag, System.StringComparison.OrdinalIgnoreCase))
                        return;

                    TagManager.RenameTag(selectedTag, newTag);
                    LoadTags();
                    lstTags.SelectedItem = newTag;
                    
                    MessageBox.Show($"Tag renamed successfully!\n\nAll assignments with '{selectedTag}' have been updated to '{newTag}'.", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lstTags.SelectedItem is string selectedTag)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the tag '{selectedTag}'?\n\nNote: This will not delete assignments with this tag, but the tag will no longer appear in the list.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    TagManager.RemoveTag(selectedTag);
                    LoadTags();
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}