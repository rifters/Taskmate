using System.Linq;
using System.Windows;

namespace Taskmate
{
    public partial class TemplateBrowserWindow : Window
    {
        public AssignmentTemplate? SelectedTemplate { get; private set; }

        public TemplateBrowserWindow()
        {
            InitializeComponent();
            LoadTemplates();
        }

        private void LoadTemplates()
        {
            dgTemplates.ItemsSource = TemplateManager.GetAllTemplates();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (dgTemplates.SelectedItem is AssignmentTemplate template)
            {
                SelectedTemplate = template;
                DialogResult = true;
                Close();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgTemplates.SelectedItem is AssignmentTemplate template)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the template '{template.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    TemplateManager.DeleteTemplate(template.Id);
                    LoadTemplates();
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}