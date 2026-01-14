using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FACTOVA_QueryHelper.Windows
{
    public partial class GridPopupWindow : Window
    {
        public GridPopupWindow()
        {
            InitializeComponent();
            
            PopupDataGrid.AutoGeneratingColumn += DataGrid_AutoGeneratingColumn;
            PopupDataGrid.LoadingRow += DataGrid_LoadingRow;
            PopupDataGrid.PreviewKeyDown += DataGrid_PreviewKeyDown;
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
        }

        private void DataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
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
