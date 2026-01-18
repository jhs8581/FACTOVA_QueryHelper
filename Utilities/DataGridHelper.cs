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

            // 🎯 셀 스타일: 세로 중앙 정렬 + 가로 꽉 채우기
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(
                DataGridCell.VerticalContentAlignmentProperty,
                VerticalAlignment.Center)); // 🔥 세로 중앙 정렬
            cellStyle.Setters.Add(new Setter(
                DataGridCell.HorizontalContentAlignmentProperty,
                HorizontalAlignment.Stretch)); // 🔥 가로 꽉 채우기
            dataGrid.CellStyle = cellStyle;

            // 🔥 모든 DataGridTextColumn에 TextWrapping 적용
            dataGrid.AutoGeneratingColumn += (s, e) =>
            {
                if (e.Column is DataGridTextColumn textColumn)
                {
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                    style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Top));
                    textColumn.ElementStyle = style;
                }
            };

            // 🔥 이미 존재하는 컬럼에도 TextWrapping 적용
            foreach (var column in dataGrid.Columns)
            {
                if (column is DataGridTextColumn textColumn && textColumn.ElementStyle == null)
                {
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                    style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Top));
                    textColumn.ElementStyle = style;
                }
            }

            // 🎨 스타일이 적용된 RowHeader 사용
            if (useStyledRowHeaders)
            {
                ApplyRowHeaderStyle(dataGrid);
            }

            // 🔥 우클릭 메뉴 추가 (행 높이 선택)
            AddRowHeightContextMenu(dataGrid);
        }

        /// <summary>
        /// 🔥 DataGrid에 행 높이 선택 우클릭 메뉴 추가
        /// </summary>
        private static void AddRowHeightContextMenu(DataGrid dataGrid)
        {
            var contextMenu = dataGrid.ContextMenu ?? new ContextMenu();

            // 구분선 추가 (기존 메뉴가 있으면)
            if (contextMenu.Items.Count > 0)
            {
                contextMenu.Items.Add(new Separator());
            }

            // 행 높이 서브메뉴
            var rowHeightMenu = new MenuItem
            {
                Header = "📏 행 높이",
                Icon = new System.Windows.Controls.TextBlock { Text = "📏", FontSize = 14 }
            };

            // 행 높이 옵션들
            var heights = new (string Name, double Value)[]
            {
                ("기본 (자동)", double.NaN),
                ("작게 (25)", 25),
                ("중간 (50)", 50),
                ("크게 (100)", 100),
                ("아주 크게 (200)", 200)
            };

            foreach (var (name, value) in heights)
            {
                var menuItem = new MenuItem { Header = name };
                var heightValue = value; // 클로저를 위한 복사
                menuItem.Click += (s, e) =>
                {
                    if (double.IsNaN(heightValue))
                    {
                        dataGrid.RowHeight = double.NaN;
                    }
                    else
                    {
                        dataGrid.RowHeight = heightValue;
                    }
                };
                rowHeightMenu.Items.Add(menuItem);
            }

            contextMenu.Items.Add(rowHeightMenu);
            dataGrid.ContextMenu = contextMenu;
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
    }
}
