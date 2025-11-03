using System.Windows;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class QueryTextEditWindow : Window
    {
        public string QueryText { get; private set; } = string.Empty;

        public QueryTextEditWindow(string initialQuery = "")
        {
            InitializeComponent();
            QueryTextBox.Text = initialQuery;
            QueryTextBox.SelectAll();
            QueryTextBox.Focus();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            QueryText = QueryTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
