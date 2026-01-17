using System.Windows;

namespace Taskmate
{
    public partial class FirstRunDialog : Window
    {
        public bool CopySamples { get; private set; }

        public FirstRunDialog()
        {
            InitializeComponent();
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            CopySamples = true;
            
            if (chkDontShowAgain.IsChecked == true)
            {
                SampleManager.MarkFirstRunComplete();
            }
            
            DialogResult = true;
            Close();
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            CopySamples = false;
            
            if (chkDontShowAgain.IsChecked == true)
            {
                SampleManager.MarkFirstRunComplete();
            }
            
            DialogResult = true;
            Close();
        }
    }
}