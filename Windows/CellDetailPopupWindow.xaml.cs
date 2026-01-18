using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FACTOVA_QueryHelper.Windows
{
    /// <summary>
    /// 셀 상세 보기 팝업 - 선택된 행의 데이터를 피벗 형태(컬럼명/값)로 표시
    /// </summary>
    public partial class CellDetailPopupWindow : Window
    {
        private ObservableCollection<PivotItem> _allItems = new();
        private ObservableCollection<PivotItem> _filteredItems = new();

        public CellDetailPopupWindow()
        {
            InitializeComponent();
            
            PivotDataGrid.ItemsSource = _filteredItems;
            
            // 키보드 단축키
            this.PreviewKeyDown += Window_PreviewKeyDown;
        }

        /// <summary>
        /// DataRowView에서 데이터를 피벗 형태로 변환하여 설정
        /// </summary>
        public void SetDataFromDataRowView(DataRowView rowView, int rowIndex = -1)
        {
            if (rowView == null) return;

            _allItems.Clear();

            var row = rowView.Row;
            var table = row.Table;

            for (int i = 0; i < table.Columns.Count; i++)
            {
                var column = table.Columns[i];
                var value = row[i];

                _allItems.Add(new PivotItem
                {
                    ColumnName = column.ColumnName,
                    Value = FormatValue(value, column.DataType),
                    OriginalValue = value
                });
            }

            // 정보 텍스트 업데이트
            if (rowIndex >= 0)
            {
                InfoTextBlock.Text = $"행 번호: {rowIndex + 1} | 총 {_allItems.Count}개 컬럼";
            }
            else
            {
                InfoTextBlock.Text = $"총 {_allItems.Count}개 컬럼";
            }

            ApplyFilter();
        }

        /// <summary>
        /// 딕셔너리에서 데이터를 피벗 형태로 변환하여 설정
        /// </summary>
        public void SetDataFromDictionary(IDictionary<string, object?> data, int rowIndex = -1)
        {
            if (data == null) return;

            _allItems.Clear();

            foreach (var kvp in data)
            {
                _allItems.Add(new PivotItem
                {
                    ColumnName = kvp.Key,
                    Value = FormatValue(kvp.Value, kvp.Value?.GetType()),
                    OriginalValue = kvp.Value
                });
            }

            // 정보 텍스트 업데이트
            if (rowIndex >= 0)
            {
                InfoTextBlock.Text = $"행 번호: {rowIndex + 1} | 총 {_allItems.Count}개 컬럼";
            }
            else
            {
                InfoTextBlock.Text = $"총 {_allItems.Count}개 컬럼";
            }

            ApplyFilter();
        }

        /// <summary>
        /// 객체에서 데이터를 피벗 형태로 변환하여 설정 (리플렉션 사용)
        /// </summary>
        public void SetDataFromObject(object obj, int rowIndex = -1)
        {
            if (obj == null) return;

            _allItems.Clear();

            var type = obj.GetType();
            var properties = type.GetProperties();

            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    _allItems.Add(new PivotItem
                    {
                        ColumnName = prop.Name,
                        Value = FormatValue(value, prop.PropertyType),
                        OriginalValue = value
                    });
                }
                catch
                {
                    // 읽기 불가능한 속성 무시
                }
            }

            // 정보 텍스트 업데이트
            if (rowIndex >= 0)
            {
                InfoTextBlock.Text = $"행 번호: {rowIndex + 1} | 총 {_allItems.Count}개 컬럼";
            }
            else
            {
                InfoTextBlock.Text = $"총 {_allItems.Count}개 컬럼";
            }

            ApplyFilter();
        }

        /// <summary>
        /// 값을 표시용 문자열로 포맷
        /// </summary>
        private string FormatValue(object? value, Type? type)
        {
            if (value == null || value == DBNull.Value)
                return "(null)";

            if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                if (value is DateTime dt)
                    return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            {
                if (value is IFormattable formattable)
                    return formattable.ToString("#,##0.######", null) ?? value.ToString() ?? "";
            }

            if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            {
                if (value is IFormattable formattable)
                    return formattable.ToString("#,##0", null) ?? value.ToString() ?? "";
            }

            return value.ToString() ?? "";
        }

        /// <summary>
        /// 검색 필터 적용
        /// </summary>
        private void ApplyFilter()
        {
            var searchText = SearchTextBox.Text?.Trim().ToLower() ?? "";

            _filteredItems.Clear();

            var filtered = string.IsNullOrEmpty(searchText)
                ? _allItems
                : _allItems.Where(item =>
                    item.ColumnName.ToLower().Contains(searchText) ||
                    item.Value.ToLower().Contains(searchText));

            foreach (var item in filtered)
            {
                _filteredItems.Add(item);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CopyAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("컬럼명\t값");

                foreach (var item in _filteredItems)
                {
                    sb.AppendLine($"{item.ColumnName}\t{item.Value}");
                }

                Clipboard.SetText(sb.ToString());
                MessageBox.Show($"{_filteredItems.Count}개 항목이 클립보드에 복사되었습니다.", 
                    "복사 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"복사 중 오류가 발생했습니다: {ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopySelectedButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItem = PivotDataGrid.SelectedItem as PivotItem;
                if (selectedItem == null)
                {
                    MessageBox.Show("복사할 항목을 선택해주세요.", "알림", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Clipboard.SetText($"{selectedItem.ColumnName}\t{selectedItem.Value}");
                MessageBox.Show("선택한 항목이 클립보드에 복사되었습니다.", 
                    "복사 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"복사 중 오류가 발생했습니다: {ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // ESC 키로 닫기
            if (e.Key == Key.Escape)
            {
                this.Close();
                e.Handled = true;
            }

            // Ctrl+C로 선택 복사
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CopySelectedButton_Click(sender, e);
                e.Handled = true;
            }

            // Ctrl+A로 전체 복사
            if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CopyAllButton_Click(sender, e);
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// 피벗 데이터 아이템
    /// </summary>
    public class PivotItem
    {
        public string ColumnName { get; set; } = "";
        public string Value { get; set; } = "";
        public object? OriginalValue { get; set; }
    }
}
