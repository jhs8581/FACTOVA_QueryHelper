using System.Windows;
using System.Windows.Input;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class TabRenameDialog : Window
    {
        public string NewTabName { get; private set; }

        public TabRenameDialog(string currentName)
        {
            InitializeComponent();
            
            // 현재 이름 설정
            TabNameTextBox.Text = currentName;
            TabNameTextBox.SelectAll();
            
            // 로드 후 포커스
            Loaded += (s, e) => TabNameTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TabNameTextBox.Text))
            {
                MessageBox.Show("탭 이름을 입력해주세요.", "입력 오류", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TabNameTextBox.Focus();
                return;
            }

            NewTabName = TabNameTextBox.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TabNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
            }
        }
    }
}
