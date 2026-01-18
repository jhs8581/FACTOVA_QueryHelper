using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FACTOVA_QueryHelper.Utilities
{
    /// <summary>
    /// DataGrid 관련 공통 유틸리티 클래스
    /// </summary>
    public static class DataGridHelper
    {
        /// <summary>
        /// DataGrid에 행 번호를 표시합니다 (기본 스타일).
        /// </summary>
        /// <param name="dataGrid">대상 DataGrid</param>
        public static void EnableRowNumbers(DataGrid dataGrid)
        {
            EnableRowNumbers(dataGrid, useStyledRowHeaders: true);
        }

        /// <summary>
        /// DataGrid에 행 번호를 표시합니다.
        /// </summary>
        /// <param name="dataGrid">대상 DataGrid</param>
        /// <param name="useStyledRowHeaders">스타일이 적용된 RowHeader 사용 여부</param>
        public static void EnableRowNumbers(DataGrid dataGrid, bool useStyledRowHeaders)
        {
            if (dataGrid == null) return;

            // 기존 이벤트 핸들러 제거 (중복 방지)
            dataGrid.LoadingRow -= DataGrid_LoadingRow;
            dataGrid.UnloadingRow -= DataGrid_UnloadingRow;

            // 이벤트 핸들러 추가
            dataGrid.LoadingRow += DataGrid_LoadingRow;
            dataGrid.UnloadingRow += DataGrid_UnloadingRow;

            // RowHeader가 보이도록 설정
            if (dataGrid.HeadersVisibility != DataGridHeadersVisibility.All && 
                dataGrid.HeadersVisibility != DataGridHeadersVisibility.Row)
            {
                dataGrid.HeadersVisibility = DataGridHeadersVisibility.All;
            }

            // 🔥 행 높이 기본값: 자동 (내용에 맞춤)
            dataGrid.RowHeight = double.NaN;

            // 📋 읽기 전용 모드 해제 (셀 복사를 위해 필요)
            dataGrid.IsReadOnly = false;

            // 🎯 폰트 설정 (모든 DataGrid 통일)
            dataGrid.FontFamily = new FontFamily("Malgun Gothic, Segoe UI, sans-serif");
            dataGrid.FontSize = 11;

            // 🎨 ColumnHeader 스타일 설정 (모든 DataGrid 통일)
            ApplyColumnHeaderStyle(dataGrid);

            // 🎯 CellStyle은 App.xaml의 전역 스타일 사용 (ControlTemplate으로 세로 중앙 정렬)
            // dataGrid.CellStyle을 명시적으로 설정하지 않음 → App.xaml 암묵적 스타일 적용

            // 🔥 모든 DataGridTextColumn에 TextWrapping + DateTime 포맷 + 숫자 우측정렬 적용
            dataGrid.AutoGeneratingColumn += (s, e) =>
            {
                if (e.Column is DataGridTextColumn textColumn)
                {
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                    style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                    
                    // 🔥 숫자 타입 컬럼 우측 정렬 + 천단위 구분자
                    if (IsNumericType(e.PropertyType))
                    {
                        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                        style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(0, 0, 8, 0)));
                        
                        // 정수형은 천단위 구분자, 실수형은 소수점 2자리
                        if (IsIntegerType(e.PropertyType))
                        {
                            textColumn.Binding = new System.Windows.Data.Binding(e.PropertyName)
                            {
                                StringFormat = "#,##0"
                            };
                        }
                        else
                        {
                            textColumn.Binding = new System.Windows.Data.Binding(e.PropertyName)
                            {
                                StringFormat = "#,##0.##"
                            };
                        }
                    }
                    // 🔥 DateTime 타입 컬럼 자동 포맷 (yyyy-MM-dd HH:mm:ss)
                    else if (e.PropertyType == typeof(DateTime) || e.PropertyType == typeof(DateTime?))
                    {
                        textColumn.Binding = new System.Windows.Data.Binding(e.PropertyName)
                        {
                            StringFormat = "yyyy-MM-dd HH:mm:ss"
                        };
                    }
                    
                    textColumn.ElementStyle = style;
                }
            };

            // 🔥 이미 존재하는 컬럼에도 TextWrapping + 중앙 정렬 적용
            ApplyTextWrappingToColumns(dataGrid);

            // 🔥 Loaded 이벤트에서도 컬럼 스타일 적용 (XAML 바인딩 후 적용되도록)
            dataGrid.Loaded += (s, e) =>
            {
                ApplyTextWrappingToColumns(dataGrid);
            };

            // 🎨 스타일이 적용된 RowHeader 사용
            if (useStyledRowHeaders)
            {
                ApplyRowHeaderStyle(dataGrid);
            }
        }

        /// <summary>
        /// 🔥 DataGrid의 모든 TextColumn에 TextWrapping + VerticalAlignment 적용
        /// </summary>
        private static void ApplyTextWrappingToColumns(DataGrid dataGrid)
        {
            foreach (var column in dataGrid.Columns)
            {
                if (column is DataGridTextColumn textColumn)
                {
                    // 기존 스타일 확인
                    var existingStyle = textColumn.ElementStyle;
                    
                    // 이미 TextWrapping과 VerticalAlignment가 모두 설정되어 있는지 확인
                    bool hasTextWrapping = false;
                    bool hasVerticalAlignment = false;
                    
                    if (existingStyle != null)
                    {
                        foreach (var setter in existingStyle.Setters.OfType<Setter>())
                        {
                            if (setter.Property == TextBlock.TextWrappingProperty)
                                hasTextWrapping = true;
                            if (setter.Property == TextBlock.VerticalAlignmentProperty)
                                hasVerticalAlignment = true;
                        }
                    }
                    
                    // 이미 모두 설정되어 있으면 건너뛰기
                    if (hasTextWrapping && hasVerticalAlignment)
                        continue;
                    
                    // 새로운 스타일 생성 (기존 스타일 확장)
                    var newStyle = new Style(typeof(TextBlock));
                    if (existingStyle != null)
                    {
                        newStyle.BasedOn = existingStyle;
                    }
                    
                    // 부족한 속성만 추가
                    if (!hasTextWrapping)
                    {
                        newStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                    }
                    if (!hasVerticalAlignment)
                    {
                        newStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                    }
                    
                    textColumn.ElementStyle = newStyle;
                }
            }
        }

        /// <summary>
        /// 🎨 ColumnHeader 스타일 적용 (모든 DataGrid 통일)
        /// </summary>
        private static void ApplyColumnHeaderStyle(DataGrid dataGrid)
        {
            var headerStyle = new Style(typeof(DataGridColumnHeader));
            
            // 배경색: 연한 파란색
            headerStyle.Setters.Add(new Setter(
                DataGridColumnHeader.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(240, 248, 255)))); // #F0F8FF
            
            // 글자색: 진한 파란색
            headerStyle.Setters.Add(new Setter(
                DataGridColumnHeader.ForegroundProperty,
                new SolidColorBrush(Color.FromRgb(44, 90, 160)))); // #2C5AA0
            
            // 폰트 굵기: Bold
            headerStyle.Setters.Add(new Setter(
                DataGridColumnHeader.FontWeightProperty,
                FontWeights.Bold));
            
            // 패딩
            headerStyle.Setters.Add(new Setter(
                DataGridColumnHeader.PaddingProperty,
                new Thickness(8, 5, 8, 5)));
            
            // 테두리
            headerStyle.Setters.Add(new Setter(
                DataGridColumnHeader.BorderBrushProperty,
                new SolidColorBrush(Color.FromRgb(176, 196, 222)))); // #B0C4DE
            
            headerStyle.Setters.Add(new Setter(
                DataGridColumnHeader.BorderThicknessProperty,
                new Thickness(0, 0, 1, 1)));
            
            // 정렬
            headerStyle.Setters.Add(new Setter(
                DataGridColumnHeader.HorizontalContentAlignmentProperty,
                HorizontalAlignment.Left));
            
            headerStyle.Setters.Add(new Setter(
                DataGridColumnHeader.VerticalContentAlignmentProperty,
                VerticalAlignment.Center));
            
            // DataGrid에 스타일 적용
            dataGrid.ColumnHeaderStyle = headerStyle;
        }

        /// <summary>
        /// 🎨 RowHeader 스타일 적용 (보기 좋게!)
        /// </summary>
        private static void ApplyRowHeaderStyle(DataGrid dataGrid)
        {
            var rowHeaderStyle = new Style(typeof(DataGridRowHeader));

            // 배경색: 연한 파란색
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(240, 248, 255)))); // #F0F8FF (Alice Blue)

            // 글자색: 진한 파란색
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.ForegroundProperty,
                new SolidColorBrush(Color.FromRgb(0, 102, 204)))); // #0066CC

            // 폰트 굵기: 세미볼드
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.FontWeightProperty,
                FontWeights.SemiBold));

            // 폰트 크기: 12
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.FontSizeProperty,
                12.0));

            // 너비: 50픽셀
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.WidthProperty,
                50.0));

            // 가운데 정렬
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.HorizontalContentAlignmentProperty,
                HorizontalAlignment.Center));

            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.VerticalContentAlignmentProperty,
                VerticalAlignment.Center));

            // 테두리
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.BorderBrushProperty,
                new SolidColorBrush(Color.FromRgb(176, 196, 222)))); // #B0C4DE (Light Steel Blue)

            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.BorderThicknessProperty,
                new Thickness(0, 0, 1, 1)));

            // 패딩
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.PaddingProperty,
                new Thickness(5, 3, 5, 3)));

            // 🎨 마우스 오버 효과
            var mouseOverTrigger = new Trigger
            {
                Property = DataGridRowHeader.IsMouseOverProperty,
                Value = true
            };
            mouseOverTrigger.Setters.Add(new Setter(
                DataGridRowHeader.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(220, 235, 255)))); // 더 진한 파란색
            mouseOverTrigger.Setters.Add(new Setter(
                DataGridRowHeader.CursorProperty,
                Cursors.Hand));
            rowHeaderStyle.Triggers.Add(mouseOverTrigger);

            // DataGrid에 스타일 적용
            dataGrid.RowHeaderStyle = rowHeaderStyle;
        }

        /// <summary>
        /// 행이 로드될 때 행 번호를 설정합니다.
        /// </summary>
        private static void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // 행 번호를 RowHeader에 표시 (1부터 시작)
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
            
            // 📏 행 높이는 우클릭 메뉴에서 사용자가 선택 (DataGrid.RowHeight)
        }

        /// <summary>
        /// 행이 언로드될 때 정리 작업을 수행합니다.
        /// </summary>
        private static void DataGrid_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            // 필요시 정리 작업 수행
        }

        /// <summary>
        /// ItemsSource 변경 시 행 번호를 새로고침합니다.
        /// </summary>
        public static void RefreshRowNumbers(DataGrid dataGrid)
        {
            if (dataGrid == null) return;

            // ItemsSource를 임시로 null로 설정 후 다시 바인딩하여 LoadingRow 이벤트 재발생
            var itemsSource = dataGrid.ItemsSource;
            dataGrid.ItemsSource = null;
            dataGrid.ItemsSource = itemsSource;
        }

        /// <summary>
        /// 🎨 NERP 스타일 RowHeader 적용 (GMES 정보 조회 화면용)
        /// </summary>
        public static void EnableRowNumbersWithNerpStyle(DataGrid dataGrid)
        {
            if (dataGrid == null) return;

            // 기존 이벤트 핸들러 제거 (중복 방지)
            dataGrid.LoadingRow -= DataGrid_LoadingRow;
            dataGrid.UnloadingRow -= DataGrid_UnloadingRow;

            // 이벤트 핸들러 추가
            dataGrid.LoadingRow += DataGrid_LoadingRow;
            dataGrid.UnloadingRow += DataGrid_UnloadingRow;

            // RowHeader가 보이도록 설정
            if (dataGrid.HeadersVisibility != DataGridHeadersVisibility.All && 
                dataGrid.HeadersVisibility != DataGridHeadersVisibility.Row)
            {
                dataGrid.HeadersVisibility = DataGridHeadersVisibility.All;
            }

            // 📋 읽기 전용 모드 해제 (셀 복사를 위해 필요)
            dataGrid.IsReadOnly = false;

            // 🎯 셀 스타일: 세로 중앙 정렬 + 가로 꽉 채우기
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(
                DataGridCell.VerticalContentAlignmentProperty,
                VerticalAlignment.Center)); // 🔥 세로 중앙 정렬
            cellStyle.Setters.Add(new Setter(
                DataGridCell.HorizontalContentAlignmentProperty,
                HorizontalAlignment.Stretch)); // 🔥 가로 꽉 채우기
            dataGrid.CellStyle = cellStyle;

            // 🎨 NERP 스타일 적용
            ApplyNerpRowHeaderStyle(dataGrid);
        }

        /// <summary>
        /// 🎨 NERP 스타일 RowHeader (더 밝은 파란색 계열)
        /// </summary>
        private static void ApplyNerpRowHeaderStyle(DataGrid dataGrid)
        {
            var rowHeaderStyle = new Style(typeof(DataGridRowHeader));

            // 배경색: NERP 스타일과 동일한 연한 파란색
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(240, 248, 255)))); // #F0F8FF

            // 글자색: NERP 스타일과 동일한 진한 파란색
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.ForegroundProperty,
                new SolidColorBrush(Color.FromRgb(44, 90, 160)))); // #2C5AA0

            // 폰트 굵기: 볼드
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.FontWeightProperty,
                FontWeights.Bold));

            // 폰트 크기: 11
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.FontSizeProperty,
                11.0));

            // 너비: 45픽셀
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.WidthProperty,
                45.0));

            // 가운데 정렬
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.HorizontalContentAlignmentProperty,
                HorizontalAlignment.Center));

            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.VerticalContentAlignmentProperty,
                VerticalAlignment.Center));

            // 테두리: NERP 스타일과 동일
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.BorderBrushProperty,
                new SolidColorBrush(Color.FromRgb(176, 196, 222)))); // #B0C4DE

            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.BorderThicknessProperty,
                new Thickness(0, 0, 1, 1)));

            // 패딩
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.PaddingProperty,
                new Thickness(5, 3, 5, 3)));

            // DataGrid에 스타일 적용
            dataGrid.RowHeaderStyle = rowHeaderStyle;
        }

        /// <summary>
        /// 🎨 다크 스타일 RowHeader 적용 (쿼리 관리 화면용)
        /// </summary>
        public static void EnableRowNumbersWithDarkStyle(DataGrid dataGrid)
        {
            if (dataGrid == null) return;

            // 기존 이벤트 핸들러 제거 (중복 방지)
            dataGrid.LoadingRow -= DataGrid_LoadingRow;
            dataGrid.UnloadingRow -= DataGrid_UnloadingRow;

            // 이벤트 핸들러 추가
            dataGrid.LoadingRow += DataGrid_LoadingRow;
            dataGrid.UnloadingRow += DataGrid_UnloadingRow;

            // RowHeader가 보이도록 설정
            if (dataGrid.HeadersVisibility != DataGridHeadersVisibility.All && 
                dataGrid.HeadersVisibility != DataGridHeadersVisibility.Row)
            {
                dataGrid.HeadersVisibility = DataGridHeadersVisibility.All;
            }

            // 📋 읽기 전용 모드 해제 (셀 복사를 위해 필요)
            dataGrid.IsReadOnly = false;

            // 🎯 셀 스타일: 세로 중앙 정렬 + 가로 꽉 채우기
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(
                DataGridCell.VerticalContentAlignmentProperty,
                VerticalAlignment.Center)); // 🔥 세로 중앙 정렬
            cellStyle.Setters.Add(new Setter(
                DataGridCell.HorizontalContentAlignmentProperty,
                HorizontalAlignment.Stretch)); // 🔥 가로 꽉 채우기
            dataGrid.CellStyle = cellStyle;

            // 🎨 다크 스타일 적용
            ApplyDarkRowHeaderStyle(dataGrid);
        }

        /// <summary>
        /// 🎨 다크 스타일 RowHeader (진한 파란색 계열)
        /// </summary>
        private static void ApplyDarkRowHeaderStyle(DataGrid dataGrid)
        {
            var rowHeaderStyle = new Style(typeof(DataGridRowHeader));

            // 배경색: 진한 파란색 (DataGrid 헤더와 비슷)
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(0, 120, 215)))); // #0078D7

            // 글자색: 흰색
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.ForegroundProperty,
                Brushes.White));

            // 폰트 굵기: 볼드
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.FontWeightProperty,
                FontWeights.Bold));

            // 폰트 크기: 11
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.FontSizeProperty,
                11.0));

            // 너비: 50픽셀
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.WidthProperty,
                50.0));

            // 가운데 정렬
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.HorizontalContentAlignmentProperty,
                HorizontalAlignment.Center));

            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.VerticalContentAlignmentProperty,
                VerticalAlignment.Center));

            // 테두리: 더 진한 파란색
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.BorderBrushProperty,
                new SolidColorBrush(Color.FromRgb(0, 86, 160)))); // #0056A0

            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.BorderThicknessProperty,
                new Thickness(0, 0, 1, 0)));

            // 패딩
            rowHeaderStyle.Setters.Add(new Setter(
                DataGridRowHeader.PaddingProperty,
                new Thickness(5, 3, 5, 3)));

            // 🎨 마우스 오버 효과
            var mouseOverTrigger = new Trigger
            {
                Property = DataGridRowHeader.IsMouseOverProperty,
                Value = true
            };
            mouseOverTrigger.Setters.Add(new Setter(
                DataGridRowHeader.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(0, 102, 204)))); // 더 밝은 파란색
            mouseOverTrigger.Setters.Add(new Setter(
                DataGridRowHeader.CursorProperty,
                Cursors.Hand));
            rowHeaderStyle.Triggers.Add(mouseOverTrigger);

            // DataGrid에 스타일 적용
            dataGrid.RowHeaderStyle = rowHeaderStyle;
        }

        /// <summary>
        /// 🔥 숫자 타입 여부 확인 (정수 + 실수)
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            // Nullable 타입인 경우 내부 타입 확인
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            
            return underlyingType == typeof(byte) ||
                   underlyingType == typeof(sbyte) ||
                   underlyingType == typeof(short) ||
                   underlyingType == typeof(ushort) ||
                   underlyingType == typeof(int) ||
                   underlyingType == typeof(uint) ||
                   underlyingType == typeof(long) ||
                   underlyingType == typeof(ulong) ||
                   underlyingType == typeof(float) ||
                   underlyingType == typeof(double) ||
                   underlyingType == typeof(decimal);
        }

        /// <summary>
        /// 🔥 정수 타입 여부 확인
        /// </summary>
        private static bool IsIntegerType(Type type)
        {
            // Nullable 타입인 경우 내부 타입 확인
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            
            return underlyingType == typeof(byte) ||
                   underlyingType == typeof(sbyte) ||
                   underlyingType == typeof(short) ||
                   underlyingType == typeof(ushort) ||
                   underlyingType == typeof(int) ||
                   underlyingType == typeof(uint) ||
                   underlyingType == typeof(long) ||
                   underlyingType == typeof(ulong);
        }
    }
}
