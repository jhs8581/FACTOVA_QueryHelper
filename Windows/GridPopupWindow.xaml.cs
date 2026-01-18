using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using FACTOVA_QueryHelper.Utilities; // 🔥 DataGridHelper 추가

namespace FACTOVA_QueryHelper.Windows
{
    public partial class GridPopupWindow : Window
    {
        public GridPopupWindow()
        {
            InitializeComponent();
            
            PopupDataGrid.AutoGeneratingColumn += DataGrid_AutoGeneratingColumn;
            PopupDataGrid.PreviewKeyDown += DataGrid_PreviewKeyDown;
            
            // 🔥 LoadingRow 이벤트 등록 (CHK 컬럼 처리용)
            PopupDataGrid.LoadingRow += DataGrid_LoadingRow;
            
            // 🔥 행 번호는 SetDataSource에서 데이터 바인딩 후 설정
        }

        public void SetTitle(string title)
        {
            TitleTextBlock.Text = title;
            this.Title = title;
        }

        public void SetInfo(string info)
        {
            InfoTextBlock.Text = info;
        }

        public void SetDataSource(DataView dataView)
        {
            PopupDataGrid.ItemsSource = dataView;
            
            // 🔥 데이터 바인딩 후 행 번호 활성화 (FACTOVA Grid 스타일)
            DataGridHelper.EnableRowNumbers(PopupDataGrid);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string header = e.Column.Header as string ?? e.PropertyName;
            
            if (!string.IsNullOrEmpty(header))
            {
                e.Column.Header = header.Replace("_", "__");
            }
            
            // 🔥 정렬 활성화
            e.Column.CanUserSort = true;
            
            // 🔥 숫자 타입 컬럼 자동 인식
            bool isNumericColumn = e.PropertyType == typeof(int) || 
                                   e.PropertyType == typeof(long) || 
                                   e.PropertyType == typeof(decimal) || 
                                   e.PropertyType == typeof(double) || 
                                   e.PropertyType == typeof(float) ||
                                   e.PropertyType == typeof(short) ||
                                   e.PropertyType == typeof(Int16) ||
                                   e.PropertyType == typeof(Int32) ||
                                   e.PropertyType == typeof(Int64);
            
            if (e.Column is DataGridTextColumn textColumn)
            {
                var displayStyle = new Style(typeof(TextBlock));
                displayStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                displayStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                displayStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(5, 3, 5, 3)));
                
                // 🔥 숫자 컬럼은 오른쪽 정렬 + 콤마 포맷
                if (isNumericColumn)
                {
                    displayStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                    displayStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
                    
                    // 🔥 숫자 3자리 콤마 포맷 적용
                    textColumn.Binding = new Binding(e.PropertyName)
                    {
                        StringFormat = "#,##0.######" // 소수점 있는 경우도 처리
                    };
                }
                
                textColumn.ElementStyle = displayStyle;
                
                // 🔥 Auto 너비 (헤더/데이터 중 더 긴 쪽에 맞춤)
                e.Column.Width = DataGridLength.Auto;
            }
            
            // 🔥 일반 컬럼 선택 시 글자색 검정 유지
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
            
            var selectedTrigger = new Trigger
            {
                Property = DataGridCell.IsSelectedProperty,
                Value = true
            };
            selectedTrigger.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
            selectedTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(173, 216, 230)))); // 연한 파란색
            cellStyle.Triggers.Add(selectedTrigger);
            
            e.Column.CellStyle = cellStyle;
            
            // CLOB 컬럼 처리 (기존 로직 유지)
            if (e.PropertyType == typeof(string) && e.PropertyDescriptor != null)
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid?.ItemsSource is DataView dataView)
                {
                    var columnName = e.PropertyName;
                    if (dataView.Table.Columns.Contains(columnName))
                    {
                        var dataColumn = dataView.Table.Columns[columnName];
                        
                        bool isLongText = dataColumn.DataType == typeof(string) && 
                                         (dataColumn.MaxLength == -1 || dataColumn.MaxLength > 500);
                        
                        if (!isLongText && dataView.Table.Rows.Count > 0)
                        {
                            var sampleValue = dataView.Table.Rows[0][columnName]?.ToString() ?? "";
                            isLongText = sampleValue.Length > 100 || sampleValue.Contains('\n');
                        }
                        
                        if (isLongText)
                        {
                            e.Cancel = true;
                            
                            var templateColumn = new DataGridTemplateColumn
                            {
                                Header = header.Replace("_", "__"),
                                Width = new DataGridLength(200),
                                CellTemplate = CreateClobCellTemplate(columnName)
                            };
                            
                            dataGrid.Columns.Add(templateColumn);
                        }
                    }
                }
            }
        }
        
        private DataTemplate CreateClobCellTemplate(string columnName)
        {
            var factory = new FrameworkElementFactory(typeof(TextBox));
            
            factory.SetBinding(TextBox.TextProperty, new Binding(columnName)
            {
                Mode = BindingMode.OneWay
            });
            
            factory.SetValue(TextBox.IsReadOnlyProperty, true);
            factory.SetValue(TextBox.TextWrappingProperty, TextWrapping.Wrap);
            factory.SetValue(TextBox.AcceptsReturnProperty, true);
            factory.SetValue(TextBox.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            factory.SetValue(TextBox.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            factory.SetValue(TextBox.BorderThicknessProperty, new Thickness(0));
            factory.SetValue(TextBox.BackgroundProperty, Brushes.Transparent);
            factory.SetValue(TextBox.PaddingProperty, new Thickness(5));
            
            // 🔥 셀 전체를 꽉 채우도록 설정
            factory.SetValue(TextBox.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            factory.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            factory.SetValue(TextBox.MinHeightProperty, 25.0);  // 최소 높이
            factory.SetValue(TextBox.MaxHeightProperty, 200.0); // 최대 높이 (너무 길면 스크롤)
            
            var dataTemplate = new DataTemplate
            {
                VisualTree = factory
            };
            
            return dataTemplate;
        }

        private void DataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            // 🔥 행 번호는 DataGridHelper가 이미 설정했으므로 유지
            // CHK 컬럼의 값에 따라 배경색만 변경
            
            if (e.Row.Item is DataRowView rowView)
            {
                var row = rowView.Row;
                
                if (row.Table.Columns.Contains("CHK"))
                {
                    var chkValue = row["CHK"]?.ToString()?.Trim();
                    
                    if (chkValue == "E")
                    {
                        e.Row.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
                        e.Row.Foreground = new SolidColorBrush(Color.FromRgb(139, 0, 0));
                    }
                    else
                    {
                        e.Row.ClearValue(Control.BackgroundProperty);
                        e.Row.ClearValue(Control.ForegroundProperty);
                    }
                }
                else
                {
                    e.Row.ClearValue(Control.BackgroundProperty);
                    e.Row.ClearValue(Control.ForegroundProperty);
                }
            }
            
            // 🔥 행 번호는 Header에 이미 설정되어 있으므로 건드리지 않음!
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                try
                {
                    var dg = sender as DataGrid;
                    if (dg != null && dg.ItemsSource is DataView dataView && dataView.Count > 0)
                    {
                        var selectedCells = dg.SelectedCells;
                        if (selectedCells.Count == 0) return;

                        var cellInfos = selectedCells
                            .Select(cellInfo => new
                            {
                                RowIndex = dg.Items.IndexOf(cellInfo.Item),
                                ColumnIndex = cellInfo.Column.DisplayIndex,
                                Value = GetCellValue(cellInfo)
                            })
                            .OrderBy(x => x.RowIndex)
                            .ThenBy(x => x.ColumnIndex)
                            .ToList();

                        if (cellInfos.Count == 0) return;

                        var rows = cellInfos.GroupBy(x => x.RowIndex)
                            .OrderBy(g => g.Key)
                            .Select(g => string.Join("\t", g.OrderBy(x => x.ColumnIndex).Select(x => x.Value)))
                            .ToList();

                        var textToCopy = string.Join(System.Environment.NewLine, rows);
                        Clipboard.SetText(textToCopy);
                        e.Handled = true;
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"복사 오류: {ex.Message}");
                }
            }
        }

        private static string GetCellValue(DataGridCellInfo cellInfo)
        {
            try
            {
                if (cellInfo.Item is DataRowView rowView && cellInfo.Column is DataGridBoundColumn boundColumn)
                {
                    var binding = (boundColumn as DataGridTextColumn)?.Binding as System.Windows.Data.Binding;
                    if (binding != null && !string.IsNullOrEmpty(binding.Path.Path))
                    {
                        var columnName = binding.Path.Path;
                        if (rowView.Row.Table.Columns.Contains(columnName))
                        {
                            return rowView.Row[columnName]?.ToString() ?? "";
                        }
                    }
                }
                
                if (cellInfo.Item is DataRowView rv && cellInfo.Column != null)
                {
                    var colIndex = cellInfo.Column.DisplayIndex;
                    if (colIndex >= 0 && colIndex < rv.Row.Table.Columns.Count)
                    {
                        return rv.Row[colIndex]?.ToString() ?? "";
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCellValue 오류: {ex.Message}");
            }
            
            return "";
        }
    }
}
