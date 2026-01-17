using System.Windows;

namespace Taskmate
{
    public partial class RoleManagerWindow : Window
    {
        public RoleManagerWindow()
        {
            InitializeComponent();
            LoadRoles();
        }

        private void LoadRoles()
        {
            lstRoles.ItemsSource = RoleManager.GetAllRoles();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string newRole = txtNewRole.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(newRole))
            {
                MessageBox.Show("Please enter a role name.", "Role Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RoleManager.AddRole(newRole);
            LoadRoles();
            txtNewRole.Clear();
            lstRoles.SelectedItem = newRole;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}