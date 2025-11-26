using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FACTOVA_QueryHelper.Models;
using FACTOVA_QueryHelper.Services;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class TableShortcutManagementControl : UserControl
    {
        private ObservableCollection<TableShortcut> _shortcuts;
        private TableShortcutService? _service;
        private bool _isEditing = false;
        
        /// <summary>
        /// 단축어가 저장되었을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler? ShortcutsSaved;

        public TableShortcutManagementControl()
        {
            InitializeComponent();
            _shortcuts = new ObservableCollection<TableShortcut>();
            ShortcutsDataGrid.ItemsSource = _shortcuts;
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public void Initialize(string databasePath)
        {
            _service = new TableShortcutService(databasePath);
            LoadShortcuts();
        }

        /// <summary>
        /// 단축어 목록 로드
        /// </summary>
        private void LoadShortcuts()
        {
            try
            {
                if (_service == null)
                    return;

                _shortcuts.Clear();
                var shortcuts = _service.GetAll();
                
                foreach (var shortcut in shortcuts)
                {
                    _shortcuts.Add(shortcut);
                }

                // 총 개수 업데이트
                TotalCountTextBlock.Text = $"{_shortcuts.Count}개";

                System.Diagnostics.Debug.WriteLine($"✅ Loaded {_shortcuts.Count} table shortcuts");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"단축어 목록 로드 실패:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 새로고침 버튼 클릭
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditing)
            {
                var result = MessageBox.Show(
                    "저장하지 않은 변경사항이 손실됩니다.\n새로고침하시겠습니까?",
                    "확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                _isEditing = false;
                EditModeBorder.Visibility = Visibility.Collapsed;
            }

            LoadShortcuts();
        }

        /// <summary>
        /// 추가 버튼 클릭 (새 빈 행 추가)
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var newShortcut = new TableShortcut
            {
                Shortcut = "",
                FullTableName = "",
                Description = ""
            };

            _shortcuts.Add(newShortcut);
            ShortcutsDataGrid.SelectedItem = newShortcut;
            ShortcutsDataGrid.ScrollIntoView(newShortcut);
            
            // 단축어 셀로 포커스 이동
            ShortcutsDataGrid.CurrentCell = new DataGridCellInfo(newShortcut, ShortcutsDataGrid.Columns[0]);
            ShortcutsDataGrid.BeginEdit();

            _isEditing = true;
            EditModeBorder.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 삭제 버튼 클릭
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ShortcutsDataGrid.SelectedItem is not TableShortcut selected)
                {
                    MessageBox.Show("삭제할 항목을 선택해주세요.", 
                        "선택 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 새로 추가된 항목 (DB에 없음)
                if (string.IsNullOrWhiteSpace(selected.Shortcut))
                {
                    _shortcuts.Remove(selected);
                    return;
                }

                var result = MessageBox.Show(
                    $"다음 단축어를 삭제하시겠습니까?\n\n{selected.Shortcut} → {selected.FullTableName}",
                    "삭제 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                if (_service == null)
                {
                    MessageBox.Show("서비스가 초기화되지 않았습니다.", 
                        "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _service.Delete(selected.Shortcut);
                _shortcuts.Remove(selected);
                
                TotalCountTextBlock.Text = $"{_shortcuts.Count}개";
                
                // 🔥 단축어 저장 이벤트 발생
                ShortcutsSaved?.Invoke(this, EventArgs.Empty);

                MessageBox.Show("단축어가 삭제되었습니다.", 
                    "삭제 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"단축어 삭제 실패:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 셀 편집 종료 시
        /// </summary>
        private void ShortcutsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            _isEditing = true;
            EditModeBorder.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 변경사항 저장 버튼 클릭
        /// </summary>
        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_service == null)
                {
                    MessageBox.Show("서비스가 초기화되지 않았습니다.", 
                        "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 빈 항목 제거
                var emptyItems = _shortcuts.Where(s => 
                    string.IsNullOrWhiteSpace(s.Shortcut) && 
                    string.IsNullOrWhiteSpace(s.FullTableName)).ToList();
                
                foreach (var item in emptyItems)
                {
                    _shortcuts.Remove(item);
                }

                // 유효성 검사
                foreach (var shortcut in _shortcuts)
                {
                    if (string.IsNullOrWhiteSpace(shortcut.Shortcut))
                    {
                        MessageBox.Show("단축어를 입력해주세요.", "입력 오류",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(shortcut.FullTableName))
                    {
                        MessageBox.Show("테이블명을 입력해주세요.", "입력 오류",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // DB에서 기존 데이터 가져오기
                var existingShortcuts = _service.GetAll();
                var existingDict = existingShortcuts.ToDictionary(s => s.Shortcut, StringComparer.OrdinalIgnoreCase);

                int addedCount = 0;
                int updatedCount = 0;

                // 추가 또는 수정
                foreach (var shortcut in _shortcuts)
                {
                    if (existingDict.ContainsKey(shortcut.Shortcut))
                    {
                        var existing = existingDict[shortcut.Shortcut];
                        if (existing.FullTableName != shortcut.FullTableName || existing.Description != shortcut.Description)
                        {
                            _service.Update(shortcut);
                            updatedCount++;
                        }
                        existingDict.Remove(shortcut.Shortcut);
                    }
                    else
                    {
                        _service.Add(shortcut);
                        addedCount++;
                    }
                }

                // 결과 메시지
                var resultMessage = $"저장 완료!\n\n";
                if (addedCount > 0) resultMessage += $"추가: {addedCount}개\n";
                if (updatedCount > 0) resultMessage += $"수정: {updatedCount}개\n";

                if (addedCount + updatedCount > 0)
                {
                    MessageBox.Show(resultMessage.TrimEnd(), "저장 완료",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // 🔥 단축어 저장 이벤트 발생
                    ShortcutsSaved?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    MessageBox.Show("변경된 내용이 없습니다.", "정보",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                _isEditing = false;
                EditModeBorder.Visibility = Visibility.Collapsed;
                
                LoadShortcuts();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "입력 오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 실패:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void CancelChangesButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "변경사항을 취소하시겠습니까?",
                "확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _isEditing = false;
                EditModeBorder.Visibility = Visibility.Collapsed;
                LoadShortcuts();
            }
        }
    }
}
