using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// SettingsControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private SharedDataContext? _sharedData;
        
        // TNS 경로 변경 이벤트
        public event EventHandler? TnsPathChanged;

        public SettingsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 공유 데이터 컨텍스트를 초기화합니다.
        /// </summary>
        public void Initialize(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            LoadSettings();
        }

        /// <summary>
        /// 설정을 로드합니다.
        /// </summary>
        private void LoadSettings()
        {
            if (_sharedData == null) return;

            // 기본 경로 표시
            DefaultPathTextBlock.Text = SettingsManager.GetDefaultTnsPath();
            TnsPathTextBox.Text = _sharedData.Settings.TnsPath;
            
            // DB 기본 경로 표시
            DefaultDatabasePathTextBlock.Text = QueryDatabase.GetDefaultDatabasePath();
            DatabasePathTextBox.Text = string.IsNullOrWhiteSpace(_sharedData.Settings.DatabasePath) 
                ? QueryDatabase.GetDefaultDatabasePath() 
                : _sharedData.Settings.DatabasePath;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            string? filePath = FileDialogManager.OpenTnsFileDialog(
                Path.GetDirectoryName(_sharedData.Settings.TnsPath) ?? "");

            if (filePath != null)
            {
                TnsPathTextBox.Text = filePath;
            }
        }

        private void BrowseDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                Title = "데이터베이스 파일 위치 선택",
                FileName = "queries.db",
                DefaultExt = ".db"
            };

            if (!string.IsNullOrWhiteSpace(DatabasePathTextBox.Text))
            {
                var directory = Path.GetDirectoryName(DatabasePathTextBox.Text);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    saveFileDialog.InitialDirectory = directory;
                }
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                DatabasePathTextBox.Text = saveFileDialog.FileName;
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            TnsPathTextBox.Text = SettingsManager.GetDefaultTnsPath();
            DatabasePathTextBox.Text = QueryDatabase.GetDefaultDatabasePath();
            UpdateStatus("설정이 기본값으로 복원되었습니다.", Colors.Green);
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            if (!ValidationHelper.ValidateNotEmpty(TnsPathTextBox.Text, "TNS 파일 경로"))
                return;

            if (!FileDialogManager.FileExists(TnsPathTextBox.Text))
            {
                var result = MessageBox.Show(
                    "지정한 파일이 존재하지 않습니다.\n그래도 저장하시겠습니까?",
                    "확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            bool databasePathChanged = _sharedData.Settings.DatabasePath != DatabasePathTextBox.Text;

            _sharedData.Settings.TnsPath = TnsPathTextBox.Text;
            _sharedData.Settings.DatabasePath = DatabasePathTextBox.Text;
            _sharedData.SaveSettingsCallback?.Invoke();
            
            // QueryExecutionManager 설정 업데이트
            _sharedData.QueryExecutionManager?.UpdateSettings(_sharedData.Settings);
            
            // TNS 경로 변경 이벤트 발생
            TnsPathChanged?.Invoke(this, EventArgs.Empty);

            UpdateStatus("설정이 저장되었습니다.", Colors.Green);
            
            if (databasePathChanged)
            {
                MessageBox.Show(
                    "설정이 저장되었습니다.\n\n" +
                    "?? 데이터베이스 파일 경로가 변경되었습니다.\n" +
                    "변경사항을 적용하려면 프로그램을 재시작하세요.",
                    "완료",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("설정이 저장되었습니다.", "완료",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            // 메인 윈도우 상태바 업데이트
            _sharedData?.UpdateStatusCallback?.Invoke(message, color);
        }
    }
}
