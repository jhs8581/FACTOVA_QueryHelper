using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using FACTOVA_QueryHelper.Models;
using FACTOVA_QueryHelper.Services;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class ConnectionManagementControl : UserControl
    {
        private readonly ConnectionInfoService _connectionService;
        private ObservableCollection<ConnectionInfo> _connections;
        private bool _hasUnsavedChanges = false;

        // 저장 완료 시 발생하는 이벤트
        public event EventHandler? ConnectionInfosSaved;

        public ConnectionManagementControl()
        {
            InitializeComponent();
            _connectionService = new ConnectionInfoService();
            _connections = new ObservableCollection<ConnectionInfo>();
            ConnectionsDataGrid.ItemsSource = _connections;

            LoadConnections();
        }

        private void LoadConnections()
        {
            _connections.Clear();
            var connections = _connectionService.GetAllConnections();
            foreach (var conn in connections)
            {
                _connections.Add(conn);
            }

            TotalCountText.Text = $"{_connections.Count}개";
            _hasUnsavedChanges = false;
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
            // 빈 행 추가
            var newConnection = new ConnectionInfo
            {
                Name = "새 접속 정보",
                TNS = "",
                UserId = "",
                Password = "",
                IsActive = false,
                IsFavorite = false
            };

            // 일단 메모리에만 추가 (저장 버튼 누를 때 DB에 저장)
            _connections.Add(newConnection);
            TotalCountText.Text = $"{_connections.Count}개";
            _hasUnsavedChanges = true;

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

                    _connections.Remove(selectedConnection);
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
                foreach (var connection in _connections)
                {
                    // 필수 필드 검증
                    if (string.IsNullOrWhiteSpace(connection.Name))
                    {
                        MessageBox.Show("접속 정보 이름을 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(connection.UserId))
                    {
                        MessageBox.Show("User ID를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(connection.Password))
                    {
                        MessageBox.Show("Password를 입력해주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // ID가 0이면 새로 추가, 아니면 업데이트
                    if (connection.Id == 0)
                    {
                        var newId = _connectionService.AddConnection(connection);
                        connection.Id = newId;
                    }
                    else
                    {
                        _connectionService.UpdateConnection(connection);
                    }
                }

                _hasUnsavedChanges = false;
                
                // 🔥 저장 완료 이벤트 발생
                ConnectionInfosSaved?.Invoke(this, EventArgs.Empty);
                
                MessageBox.Show("모든 변경사항이 저장되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                
                System.Diagnostics.Debug.WriteLine("🔔 ConnectionInfosSaved event raised");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelChangesButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "변경사항을 취소하고 다시 로드하시겠습니까?",
                "취소 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoadConnections();
            }
        }

        private void ConnectionsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                _hasUnsavedChanges = true;
            }
        }

        private void ConnectionsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Selection changed logic if needed
        }
    }
}
