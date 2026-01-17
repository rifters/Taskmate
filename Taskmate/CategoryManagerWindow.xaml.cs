using System.Windows;

namespace Taskmate
{
    public partial class CategoryManagerWindow : Window
    {
        public CategoryManagerWindow()
        {
            InitializeComponent();
            LoadCategories();
            lstCategories.SelectionChanged += (s, e) => btnDelete.IsEnabled = lstCategories.SelectedItem != null;
        }

        private void LoadCategories()
        {
            lstCategories.ItemsSource = CategoryManager.GetAllCategories();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string newCategory = txtNewCategory.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(newCategory))
            {
                MessageBox.Show("Please enter a category name.", "Category Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CategoryManager.AddCategory(newCategory);
            LoadCategories();
            txtNewCategory.Clear();
            lstCategories.SelectedItem = newCategory;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is string selectedCategory)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the category '{selectedCategory}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    CategoryManager.RemoveCategory(selectedCategory);
                    LoadCategories();
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}