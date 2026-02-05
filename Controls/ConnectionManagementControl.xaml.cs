using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using FACTOVA_QueryHelper.Models;
using FACTOVA_QueryHelper.Services;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class ConnectionManagementControl : UserControl
    {
        private ConnectionInfoService _connectionService;
        private ObservableCollection<ConnectionInfo> _connections;
        private bool _hasUnsavedChanges = false;
        
        // 🔥 변경된 항목 추적 (신규 + 수정)
        private HashSet<ConnectionInfo> _modifiedConnections = new HashSet<ConnectionInfo>();

        // 저장 완료 시 발생하는 이벤트
        public event EventHandler? ConnectionInfosSaved;

        public ConnectionManagementControl()
        {
            InitializeComponent();
            // 🔥 초기화는 Initialize에서 수행
            _connectionService = null!;
            _connections = new ObservableCollection<ConnectionInfo>();
            ConnectionsDataGrid.ItemsSource = _connections;
            
            // 🔥 Version ComboBox 컬럼에 ItemsSource 설정
            SetupVersionColumn();
        }
        
        /// <summary>
        /// Version 컬럼의 ComboBox에 ItemsSource를 설정합니다.
        /// </summary>
        private void SetupVersionColumn()
        {
            // Version 컬럼 찾기 (DataGridComboBoxColumn)
            foreach (var column in ConnectionsDataGrid.Columns)
            {
                if (column is DataGridComboBoxColumn comboColumn && 
                    comboColumn.Header?.ToString() == "Version")
                {
                    comboColumn.ItemsSource = new[] { "", "1.0", "2.0" };
                    break;
                }
            }
        }

        /// <summary>
        /// DB 경로를 지정하여 초기화합니다.
        /// </summary>
        public void Initialize(string? databasePath = null)
        {
            _connectionService = new ConnectionInfoService(databasePath);
            LoadConnections();
        }

        private void LoadConnections()
        {
            _connections.Clear();
            _modifiedConnections.Clear();
            
            var connections = _connectionService.GetAllConnections();
            foreach (var conn in connections)
            {
                _connections.Add(conn);
            }

            TotalCountText.Text = $"{_connections.Count}개";
            _hasUnsavedChanges = false;
            
            // 🔥 편집 모드 Border 숨김
            EditModeBorder.Visibility = Visibility.Collapsed;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "저장하지 않은 변경사항이 있습니다. 새로고침하면 변경사항이 사라집니다.\n계속하시겠습니까?",
                    "경고",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            LoadConnections();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // 🔥 신규 항목 생성 (ID = 0)
            var newConnection = new ConnectionInfo
            {
                Id = 0, // 신규 항목 표시
                Name = "새 접속 정보",
                TNS = "",
                UserId = "",
                Password = "",
                IsActive = false,
                IsFavorite = false
            };

            // 컬렉션에 추가
            _connections.Add(newConnection);
            
            // 🔥 수정 목록에 추가 (신규 항목)
            _modifiedConnections.Add(newConnection);
            
            TotalCountText.Text = $"{_connections.Count}개";
            _hasUnsavedChanges = true;
            
            // 🔥 편집 모드 Border 표시
            EditModeBorder.Visibility = Visibility.Visible;

            // 새로 추가된 행으로 스크롤 & 선택
            ConnectionsDataGrid.SelectedItem = newConnection;
            ConnectionsDataGrid.ScrollIntoView(newConnection);
            
            
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionsDataGrid.SelectedItem is ConnectionInfo selectedConnection)
            {
                var result = MessageBox.Show(
                    $"'{selectedConnection.Name}' 접속 정보를 삭제하시겠습니까?",
                    "삭제 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // ID가 있으면 DB에서도 삭제
                    if (selectedConnection.Id > 0)
                    {
                        _connectionService.DeleteConnection(selectedConnection.Id);
                        
                        // 🔥 삭제 시에도 이벤트 발생
                        ConnectionInfosSaved?.Invoke(this, EventArgs.Empty);
                        
                        
                    }
                    else
                    {
                        
                    }

                    // 컬렉션 및 수정 목록에서 제거
                    _connections.Remove(selectedConnection);
                    _modifiedConnections.Remove(selectedConnection);
                    
                    TotalCountText.Text = $"{_connections.Count}개";

                    MessageBox.Show("접속 정보가 삭제되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("삭제할 항목을 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 🔥 수정된 항목이 없으면 종료
                if (_modifiedConnections.Count == 0)
                {
                    MessageBox.Show("변경된 항목이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int newCount = 0;
                int updateCount = 0;

                // 🔥 변경된 항목만 처리
                foreach (var connection in _modifiedConnections.ToList())
                {
                    // 필수 필드 검증
                    if (string.IsNullOrWhiteSpace(connection.Name))
                    {
                        MessageBox.Show($"접속 정보 이름을 입력해주세요.\n(ID: {connection.Id})", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(connection.UserId))
                    {
                        MessageBox.Show($"'{connection.Name}'의 User ID를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(connection.Password))
                    {
                        MessageBox.Show($"'{connection.Name}'의 Password를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 🔥 ID가 0이면 신규 추가, 아니면 업데이트
                    if (connection.Id == 0)
                    {
                        var newId = _connectionService.AddConnection(connection);
                        connection.Id = newId;
                        newCount++;
                        
                    }
                    else
                    {
                        _connectionService.UpdateConnection(connection);
                        updateCount++;
                        
                    }
                }

                // 🔥 수정 목록 초기화
                _modifiedConnections.Clear();
                _hasUnsavedChanges = false;
                
                // 🔥 편집 모드 Border 숨김
                EditModeBorder.Visibility = Visibility.Collapsed;
                
                // 🔥 저장 완료 이벤트 발생 (SettingsControl이 구독)
                ConnectionInfosSaved?.Invoke(this, EventArgs.Empty);
                
                // 성공 메시지
                string message = $"저장 완료!\n\n신규: {newCount}개\n수정: {updateCount}개\n총: {newCount + updateCount}개";
                MessageBox.Show(message, "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                
                
                
                // 🔥 DataGrid 새로고침
                ConnectionsDataGrid.Items.Refresh();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
}
        }

        private void CancelChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_modifiedConnections.Count == 0)
            {
                MessageBox.Show("변경된 항목이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"변경된 {_modifiedConnections.Count}개 항목을 취소하고 다시 로드하시겠습니까?",
                "취소 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoadConnections();
                
                // 🔥 편집 모드 Border 숨김
                EditModeBorder.Visibility = Visibility.Collapsed;
}
        }

        private void ConnectionsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                // 🔥 편집된 행을 수정 목록에 추가
                if (e.Row.Item is ConnectionInfo connection)
                {
                    _modifiedConnections.Add(connection);
                    _hasUnsavedChanges = true;
                    
                    // 🔥 편집 모드 Border 표시
                    EditModeBorder.Visibility = Visibility.Visible;
                    
                    
}
            }
        }

        private void ConnectionsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Selection changed logic if needed
        }
    }
}
