using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// NERP 스타일이 자동으로 적용되는 커스텀 DataGrid
    /// </summary>
    public class NerpStyleDataGrid : DataGrid
    {
        public NerpStyleDataGrid()
        {
            // 기본 설정
            AutoGenerateColumns = true;
            IsReadOnly = true;
            AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(248, 249, 250));
            GridLinesVisibility = DataGridGridLinesVisibility.All;
            HeadersVisibility = DataGridHeadersVisibility.All;
            CanUserSortColumns = true;
            CanUserResizeColumns = true;
            CanUserReorderColumns = true;
            SelectionMode = DataGridSelectionMode.Extended;
            SelectionUnit = DataGridSelectionUnit.Cell;
            ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            BorderThickness = new Thickness(1);
            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            ColumnWidth = DataGridLength.Auto;
            MinColumnWidth = 80;
            
            // AutoGeneratingColumn 이벤트 등록
            AutoGeneratingColumn += NerpStyleDataGrid_AutoGeneratingColumn;
            LoadingRow += NerpStyleDataGrid_LoadingRow;
        }

        private void NerpStyleDataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // 언더스코어 이스케이프
            string header = e.Column.Header as string ?? e.PropertyName;
            if (!string.IsNullOrEmpty(header))
            {
                e.Column.Header = header.Replace("_", "__");
            }
            
            // 정렬 활성화
            e.Column.CanUserSort = true;
            
            // 숫자 타입 컬럼 자동 인식
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
                
                // 숫자 컬럼은 오른쪽 정렬 + 콤마 포맷
                if (isNumericColumn)
                {
                    displayStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                    displayStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
                    
                    // 숫자 3자리 콤마 포맷 적용
                    textColumn.Binding = new Binding(e.PropertyName)
                    {
                        StringFormat = "#,##0.######" // 소수점 있는 경우도 처리
                    };
                }
                
                textColumn.ElementStyle = displayStyle;
                
                // 자동 너비 + 최소 너비
                e.Column.MinWidth = 80;
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            }
            
            // 헤더 스타일 설정 (NERP 스타일)
            var headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(Color.FromRgb(44, 90, 160))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, new SolidColorBrush(Colors.White)));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.HeightProperty, 40.0));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(12, 8, 12, 8)));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(34, 70, 120))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 0)));
            
            // 헤더 템플릿 설정 (언더스코어 표시 및 텍스트 줄바꿈 지원)
            var headerTemplate = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            factory.SetValue(TextBlock.TextProperty, e.Column.Header.ToString());
            factory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            factory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            factory.SetValue(TextBlock.PaddingProperty, new Thickness(5));
            headerTemplate.VisualTree = factory;
            
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ContentTemplateProperty, headerTemplate));
            e.Column.HeaderStyle = headerStyle;
            
            // 셀 스타일 (선택 시 검정색 글자 유지)
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(224, 224, 224))));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0, 0, 1, 0)));
            cellStyle.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(5, 3, 5, 3)));
            
            var selectedTrigger = new Trigger
            {
                Property = DataGridCell.IsSelectedProperty,
                Value = true
            };
            selectedTrigger.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
            selectedTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(173, 216, 230))));
            cellStyle.Triggers.Add(selectedTrigger);
            
            e.Column.CellStyle = cellStyle;
        }

        private void NerpStyleDataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            // 행 번호 표시 (선택적)
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}
