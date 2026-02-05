using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FACTOVA_QueryHelper.Database;
using FACTOVA_QueryHelper.Models;
using FACTOVA_QueryHelper.Services; // 🔥 추가
using FACTOVA_QueryHelper.Utilities; // 🔥 DataGridHelper 추가
using OfficeOpenXml;
using System.IO;
using Microsoft.Win32;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// GmesInfoControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GmesInfoControl : UserControl
    {
        private SharedDataContext? _sharedData;
        private QueryDatabase? _database;
        private List<QueryItem> _infoQueries = new List<QueryItem>();
        private bool _isInitializing = false;
        private List<DynamicGridInfo> _dynamicGrids = new List<DynamicGridInfo>();
        
        // 🔥 OracleDbService 추가
        private OracleDbService? _dbService;

        private class DynamicGridInfo
        {
            public int Index { get; set; }
            public ComboBox QueryComboBox { get; set; } = null!;
            public DataGrid DataGrid { get; set; } = null!;
            public Button ClearButton { get; set; } = null!;
            public TextBlock ResultInfoTextBlock { get; set; } = null!; // 조회 결과 정보
        }

        public GmesInfoControl()
        {
            InitializeComponent();
            
            // 🔥 OracleDbService 초기화
            _dbService = new OracleDbService();
            
            WorkOrderTextBox.LostFocus += InputField_LostFocus;
            WorkOrderNameTextBox.LostFocus += InputField_LostFocus;
            ModelSuffixTextBox.LostFocus += InputField_LostFocus;
            LotIdTextBox.LostFocus += InputField_LostFocus;
            EquipmentIdTextBox.LostFocus += InputField_LostFocus;
            
            // 🔥 PARAM1~PARAM4 이벤트 핸들러 추가
            Param1TextBox.LostFocus += InputField_LostFocus;
            Param2TextBox.LostFocus += InputField_LostFocus;
            Param3TextBox.LostFocus += InputField_LostFocus;
            Param4TextBox.LostFocus += InputField_LostFocus;
            
            QuerySelectComboBox.SelectionChanged += QueryComboBox_SelectionChanged;
            PlanInfoDataGrid.AutoGeneratingColumn += DataGrid_AutoGeneratingColumn;
            PlanInfoDataGrid.LoadingRow += DataGrid_LoadingRow; // CHK 컬럼 체크
            PlanInfoDataGrid.SelectionChanged += PlanInfoDataGrid_SelectionChanged; // 선택 변경 이벤트
            
            // 🔥 Ctrl+C 키보드 이벤트 핸들러 추가
            PlanInfoDataGrid.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    try
                    {
                        if (PlanInfoDataGrid.SelectedItem is DataRowView selectedRow)
                        {
                            // 선택된 행의 모든 데이터를 탭으로 구분하여 복사
                            var row = selectedRow.Row;
                            var values = new List<string>();
                            
                            foreach (DataColumn column in row.Table.Columns)
                            {
                                values.Add(row[column]?.ToString() ?? "");
                            }
                            
                            var textToCopy = string.Join("\t", values);
                            Clipboard.SetText(textToCopy);
                            e.Handled = true;
}
                    }
                    catch (Exception ex)
                    {
}
                }
            };
            
            // 🔥 조회 버튼 클릭 이벤트 연결
            ExecuteQueryButton.Click += ExecuteQueryButton_Click;
        }

        /// <summary>
        /// 조회 조건 접기/펼치기
        /// </summary>
        private void ToggleSearchConditionButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchConditionGrid.Visibility == Visibility.Visible)
            {
                // 접기
                SearchConditionGrid.Visibility = Visibility.Collapsed;
                SearchConditionRow.Height = new GridLength(0);
                ToggleIconTextBlock.Text = "▼";
            }
            else
            {
                // 펼치기
                SearchConditionGrid.Visibility = Visibility.Visible;
                SearchConditionRow.Height = GridLength.Auto;
                ToggleIconTextBlock.Text = "▲";
            }
        }

        /// <summary>
        /// 계획정보 DataGrid의 편집 시작을 막음 (복사만 허용)
        /// </summary>
        private void PlanInfoDataGrid_BeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;  // 편집 취소 (복사만 가능)
        }

        /// <summary>
        /// 계획정보 DataGrid 더블클릭 - 선택 행 기준 전체 조회 실행
        /// </summary>
        private async void PlanInfoDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 헤더나 빈 영역 클릭 방지
            var dataGrid = sender as DataGrid;
            if (dataGrid == null) return;

            // 실제 행을 클릭했는지 확인
            var row = ItemsControl.ContainerFromElement(dataGrid, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null || row.Item == null) return;

            // 선택된 행이 있는지 확인
            if (PlanInfoDataGrid.SelectedItem is not DataRowView selectedRow)
            {
                MessageBox.Show("선택된 행이 없습니다.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 🔥 더블클릭 시 선택된 정보 업데이트
            try
            {
                var rowData = selectedRow.Row;
                var table = rowData.Table;

                // 🔥 컬럼명이 이스케이프될 수 있으므로 (_를 __로) 두 가지 형식 모두 확인
                string GetColumnValue(string columnName)
                {
                    // 원본 컬럼명으로 먼저 시도
                    if (table.Columns.Contains(columnName))
                    {
                        return rowData[columnName]?.ToString() ?? "-";
                    }
                    // 이스케이프된 컬럼명으로 시도 (_를 __로)
                    var escapedName = columnName.Replace("_", "__");
                    if (table.Columns.Contains(escapedName))
                    {
                        return rowData[escapedName]?.ToString() ?? "-";
                    }
                    return "-";
                }

                // WORK_ORDER_ID
                SelectedWorkOrderIdTextBlock.Text = GetColumnValue("WORK_ORDER_ID");

                // WORK_ORDER_NAME
                SelectedWorkOrderNameTextBlock.Text = GetColumnValue("WORK_ORDER_NAME");

                // PRODUCT_SPECIFICATION_ID
                SelectedProductSpecIdTextBlock.Text = GetColumnValue("PRODUCT_SPECIFICATION_ID");
            }
            catch (Exception ex)
            {
}

            // 전체 조회 버튼 클릭과 동일한 로직 실행
            try
            {
                ExecuteAllGridsButton.IsEnabled = false;
                ExecuteAllGridsButton.Content = "조회 중...";

                // 각 그리드별로 시간 측정과 함께 실행
                var tasks = new List<System.Threading.Tasks.Task>();
                var stopwatches = new Dictionary<int, System.Diagnostics.Stopwatch>();

                foreach (var gridInfo in _dynamicGrids)
                {
                    if (gridInfo.QueryComboBox.SelectedItem is QueryItem query)
                    {
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        stopwatches[gridInfo.Index] = stopwatch;

                        tasks.Add(ExecuteQueryToGridWithRowDataAndMeasure(query, gridInfo, selectedRow, stopwatch));
                    }
                }

                if (tasks.Count == 0)
                {
                    MessageBox.Show("조회할 쿼리가 선택되지 않았습니다.", "알림",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await System.Threading.Tasks.Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"전체 조회 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ExecuteAllGridsButton.IsEnabled = true;
                ExecuteAllGridsButton.Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = "⚡", FontSize = 16, Margin = new Thickness(0, 0, 5, 0) },
                        new TextBlock { Text = "선택 행 기준 전체 조회" }
                    }
                };
            }
        }

        /// <summary>
        /// 계획정보 DataGrid의 선택 변경 이벤트
        /// </summary>
        private void PlanInfoDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 🔥 더블클릭할 때만 정보가 표시되도록 SelectionChanged 이벤트에서는 아무 작업도 하지 않음
            // 선택 변경 시 정보 표시 제거
        }

        /// <summary>
        /// W/O ID 복사
        /// </summary>
        private void CopyWorkOrderId_Click(object sender, RoutedEventArgs e)
        {
            var text = SelectedWorkOrderIdTextBlock.Text;
            if (text != "-" && !string.IsNullOrEmpty(text))
            {
                Clipboard.SetText(text);
            }
        }

        /// <summary>
        /// W/O 명 복사
        /// </summary>
        private void CopyWorkOrderName_Click(object sender, RoutedEventArgs e)
        {
            var text = SelectedWorkOrderNameTextBlock.Text;
            if (text != "-" && !string.IsNullOrEmpty(text))
            {
                Clipboard.SetText(text);
            }
        }

        /// <summary>
        /// Model.Suffix 복사
        /// </summary>
        private void CopyProductSpecId_Click(object sender, RoutedEventArgs e)
        {
            var text = SelectedProductSpecIdTextBlock.Text;
            if (text != "-" && !string.IsNullOrEmpty(text))
            {
                Clipboard.SetText(text);
            }
        }

        public async void Initialize(SharedDataContext sharedData)
        {
            _isInitializing = true;
            
            _sharedData = sharedData;
            _database = new QueryDatabase(sharedData.Settings.DatabasePath);
            
            try
            {
                // 🔥 비동기 작업은 백그라운드에서 실행
                await System.Threading.Tasks.Task.Run(() =>
                {
                    // 데이터 로드는 백그라운드에서
});
                
                // UI 업데이트는 UI 스레드에서
                LoadSiteInfos();
                LoadInputValues();
                LoadInfoQueries();
var sw = System.Diagnostics.Stopwatch.StartNew();
                
                // 그리드를 항상 20개로 고정 생성
                CreateDynamicGrids(20);
                
                sw.Stop();
// 폰트 크기 적용
                ApplyFontSize();
                UpdateFontSizeDisplay();
            }
            finally
            {
                _isInitializing = false;
            }
            
            // 🔥 초기화 완료 후 사업장 정보를 다시 한번 명시적으로 적용
            if (SiteComboBox.SelectedItem is SiteInfo selectedSite)
            {
                _sharedData.Settings.GmesFactory = selectedSite.RepresentativeFactory;
                _sharedData.Settings.GmesOrg = selectedSite.Organization;
                _sharedData.Settings.GmesFacility = selectedSite.Facility;
                _sharedData.Settings.GmesWipLineId = selectedSite.WipLineId;
                _sharedData.Settings.GmesEquipLineId = selectedSite.EquipLineId;
                _sharedData.SaveSettingsCallback?.Invoke();




            }
        }

        /// <summary>
        /// 사업장 정보를 로드합니다.
        /// </summary>
        private void LoadSiteInfos()
        {
            if (_database == null) return;

            try
            {
                // 🔥 IsDefault(표시순번) 순서로 정렬된 사업장 목록 가져오기
                var sites = _database.GetAllSites();
                
                SiteComboBox.ItemsSource = sites;

                // 🔥 폼 로드 시 첫 번째 항목을 객체로 직접 선택 (인덱스가 아닌 실제 객체)
                if (sites.Count > 0)
                {
                    SiteComboBox.SelectedItem = sites[0]; // 🔥 SelectedIndex 대신 SelectedItem 사용




}
            }
            catch (Exception ex)
            {
}
        }

        /// <summary>
        /// 사업장 ComboBox 선택 변경 이벤트
        /// </summary>
        private void SiteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || SiteComboBox.SelectedItem is not SiteInfo selectedSite)
                return;

            // 선택된 사업장 정보를 숨겨진 필드에 적용
            if (_sharedData != null)
            {
                // 🔥 디버깅: 변경 전 값 로깅
                


_sharedData.Settings.GmesFactory = selectedSite.RepresentativeFactory;
                _sharedData.Settings.GmesOrg = selectedSite.Organization;
                _sharedData.Settings.GmesFacility = selectedSite.Facility;
                _sharedData.Settings.GmesWipLineId = selectedSite.WipLineId;
                _sharedData.Settings.GmesEquipLineId = selectedSite.EquipLineId;
                _sharedData.SaveSettingsCallback?.Invoke();
                
                // 🔥 디버깅: 변경 후 선택된 사업장 정보 로깅
                



}
        }

        private void LoadInputValues()
        {
            if (_sharedData == null) return;

            // 일자는 항상 오늘 날짜로 설정 (저장하지 않음)
            DateFromPicker.SelectedDate = DateTime.Today;
            DateToPicker.SelectedDate = DateTime.Today;
            
            WorkOrderTextBox.Text = _sharedData.Settings.GmesWorkOrder;
            WorkOrderNameTextBox.Text = _sharedData.Settings.GmesWorkOrderName;
            ModelSuffixTextBox.Text = _sharedData.Settings.GmesModelSuffix;
            LotIdTextBox.Text = _sharedData.Settings.GmesLotId;
            EquipmentIdTextBox.Text = _sharedData.Settings.GmesEquipmentId;
            
            // 🔥 PARAM1~PARAM4 로드
            Param1TextBox.Text = _sharedData.Settings.GmesParam1 ?? "";
            Param2TextBox.Text = _sharedData.Settings.GmesParam2 ?? "";
            Param3TextBox.Text = _sharedData.Settings.GmesParam3 ?? "";
            Param4TextBox.Text = _sharedData.Settings.GmesParam4 ?? "";
        }

        private void SaveInputValues()
        {
            if (_sharedData == null || _isInitializing) return;

            // 🔥 사업장 정보는 SiteComboBox_SelectionChanged에서 처리
            
            // 일자는 저장하지 않음 (항상 현재 날짜 사용)
            
            _sharedData.Settings.GmesWorkOrder = WorkOrderTextBox.Text;
            _sharedData.Settings.GmesWorkOrderName = WorkOrderNameTextBox.Text;
            _sharedData.Settings.GmesModelSuffix = ModelSuffixTextBox.Text;
            _sharedData.Settings.GmesLotId = LotIdTextBox.Text;
            _sharedData.Settings.GmesEquipmentId = EquipmentIdTextBox.Text;
            
            // 🔥 PARAM1~PARAM4 저장
            _sharedData.Settings.GmesParam1 = Param1TextBox.Text;
            _sharedData.Settings.GmesParam2 = Param2TextBox.Text;
            _sharedData.Settings.GmesParam3 = Param3TextBox.Text;
            _sharedData.Settings.GmesParam4 = Param4TextBox.Text;

            _sharedData.SaveSettingsCallback?.Invoke();
        }

        private void InputField_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveInputValues();
        }

        private void QueryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            // 🔥 기준정보 그리드 초기화 (ItemsSource와 Columns 모두 초기화)
            PlanInfoDataGrid.ItemsSource = null;
            PlanInfoDataGrid.Columns.Clear();
            PlanInfoResultTextBlock.Text = "";
            
            // 🔥 선택된 정보 초기화
            SelectedWorkOrderIdTextBlock.Text = "-";
            SelectedWorkOrderNameTextBlock.Text = "-";
            SelectedProductSpecIdTextBlock.Text = "-";
            
            // 🔥 모든 동적 그리드 초기화 (ItemsSource와 Columns 모두 초기화)
            foreach (var gridInfo in _dynamicGrids)
            {
                gridInfo.DataGrid.ItemsSource = null;
                gridInfo.DataGrid.Columns.Clear();
                gridInfo.ResultInfoTextBlock.Text = "";
            }

            // 선택된 계획정보 쿼리의 그룹명으로 상세 쿼리 자동 로드
            if (QuerySelectComboBox.SelectedItem is QueryItem selectedPlanQuery &&
                !string.IsNullOrWhiteSpace(selectedPlanQuery.QueryName) &&
                selectedPlanQuery.OrderNumber >= 0) // 플레이스홀더가 아닌 경우만
            {
                LoadDetailQueriesByQueryName(selectedPlanQuery.QueryName);
            }
            else
            {
                // 플레이스홀더 선택 시 모든 콤보박스 활성화 및 전체 쿼리 바인딩
                UpdateAllGridComboBoxes();
            }
        }

        /// <summary>
        /// 그룹명(QueryName)으로 상세 쿼리(순번 1 이상)를 로드하여 동적 그리드에 자동 바인딩
        /// </summary>
        private void LoadDetailQueriesByQueryName(string queryName)
        {
            if (_database == null) return;

            try
            {
                var allQueries = _database.GetAllQueries();

                // 선택된 그룹명과 일치하고 순번이 1 이상인 쿼리만 필터링
                var detailQueries = allQueries
                    .Where(q => q.QueryType == "정보 조회" && 
                                q.QueryName == queryName && 
                                q.OrderNumber >= 1)
                    .OrderBy(q => q.OrderNumber)
                    .ToList();

                if (detailQueries.Count > 0)
                {
                    // 동적 그리드 생성 및 쿼리 자동 바인딩 (최대 20개)
                    GenerateDynamicGridsWithQueries(detailQueries);
}
                else
                {
                    // 상세 쿼리가 없으면 동적 그리드를 20개 빈 상태로 재생성
                    CreateDynamicGrids(20);
                    
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"상세 쿼리 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 동적 그리드를 생성하고 쿼리 콤보박스를 바인딩합니다.
        /// </summary>
        private void CreateDynamicGrids(int count)
        {
            CreateDynamicGridsCore(count, null);
        }

        /// <summary>
        /// 쿼리 목록과 함께 동적 그리드 생성 및 자동 바인딩
        /// </summary>
        private void GenerateDynamicGridsWithQueries(List<QueryItem> queries)
        {
            CreateDynamicGridsCore(20, queries);
        }

        /// <summary>
        /// 동적 그리드 생성의 핵심 로직
        /// </summary>
        private void CreateDynamicGridsCore(int count, List<QueryItem>? queriesToBind)
        {
            _isInitializing = true;

            DynamicGridsContainer.Children.Clear();
            DynamicGridsContainer.RowDefinitions.Clear();
            _dynamicGrids.Clear();

            // 항상 20개 그리드 생성
            const int gridCount = 20;

            // 행 정의 추가
            for (int i = 0; i < gridCount; i++)
            {
                DynamicGridsContainer.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(350, GridUnitType.Pixel)
                });
            }

            // 그리드 생성
            for (int i = 0; i < gridCount; i++)
            {
                int gridIndex = i + 1;
                var gridInfo = CreateDynamicGrid(gridIndex);
                _dynamicGrids.Add(gridInfo);

                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10),
                    Margin = new Thickness(5),
                    Child = CreateGridContainer(gridInfo)
                };

                Grid.SetRow(border, i);
                Grid.SetColumn(border, 0);

                DynamicGridsContainer.Children.Add(border);
            }

            // 콤보박스 바인딩
            BindGridComboBoxes(queriesToBind);
            
            _isInitializing = false;
        }

        /// <summary>
        /// 동적 그리드의 콤보박스에 쿼리 목록을 바인딩합니다.
        /// </summary>
        private void BindGridComboBoxes(List<QueryItem>? queriesToBind)
        {
            // 🔥 비즈명이 있고 사용여부가 체크된 쿼리만 필터링
            // ExcludeFlag가 'Y'이면 콤보박스에 바인딩하지 않음
            var queriesWithBizName = _infoQueries
                .Where(q => !string.IsNullOrWhiteSpace(q.BizName) && 
                           !string.Equals(q.ExcludeFlag, "Y", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            foreach (var gridInfo in _dynamicGrids)
            {
                gridInfo.QueryComboBox.ItemsSource = queriesWithBizName;
                gridInfo.QueryComboBox.IsEnabled = true;
                gridInfo.ClearButton.IsEnabled = true;
            }

            // 자동 바인딩할 쿼리가 있으면 선택
            if (queriesToBind != null && queriesToBind.Count > 0)
            {
                int count = Math.Min(queriesToBind.Count, _dynamicGrids.Count);
                
                for (int i = 0; i < count; i++)
                {
                    var gridInfo = _dynamicGrids[i];
                    var query = queriesToBind[i];

                    // QueryName과 BizName, OrderNumber로 매칭
                    var matchingQuery = queriesWithBizName.FirstOrDefault(q => 
                        q.QueryName == query.QueryName && 
                        q.BizName == query.BizName && 
                        q.OrderNumber == query.OrderNumber);

                    if (matchingQuery != null)
                    {
                        gridInfo.QueryComboBox.SelectedItem = matchingQuery;
                        
                    }
                    else
                    {
                        
                    }
                }
            }
        }

        private void LoadInfoQueries()
        {
            if (_database == null) return;

            try
            {
                _isInitializing = true;
                
                var allQueries = _database.GetAllQueries();
                
                // 모든 "정보 조회" 타입의 쿼리 필터링 (순번 제한 없이)
                var infoQueries = allQueries
                    .Where(q => q.QueryType == "정보 조회")
                    .OrderBy(q => q.BizName)
                    .ThenBy(q => q.OrderNumber)
                    .ToList();

                // 순번 0인 쿼리만 계획정보 쿼리 콤보박스용
                var planQueries = infoQueries.Where(q => q.OrderNumber == 0).ToList();

                // 플레이스홀더 아이템 생성
                var placeholderItem = new QueryItem 
                { 
                    QueryName = "-- 쿼리를 선택하세요 --",
                    Query = "",
                    TnsName = "",
                    UserId = "",
                    Password = "",
                    QueryType = "",
                    BizName = "",
                    OrderNumber = -1
                };

                // 계획정보 쿼리 콜박스: 플레이스홀더 + 순번 0인 쿼리
                var planQueryList = new List<QueryItem> { placeholderItem };
                planQueryList.AddRange(planQueries);
                
                QuerySelectComboBox.ItemsSource = planQueryList;
                
                // _infoQueries에는 모든 정보 조회 쿼리 저장 (동적 그리드용)
                _infoQueries = infoQueries;

                LoadSelectedQueries();
                
                _isInitializing = false;
            }
            catch (Exception ex)
            {
                _isInitializing = false;
                MessageBox.Show($"쿼리 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSelectedQueries()
        {
            if (_sharedData == null || _infoQueries.Count == 0) return;

            // 항상 플레이스홀더(인덱스 0)로 시작
            QuerySelectComboBox.SelectedIndex = 0;
        }

        private void LoadQueriesButton_Click(object sender, RoutedEventArgs e)
        {
            LoadInfoQueries();
            UpdateAllGridComboBoxes();
        }

        private void ClearQuerySelectButton_Click(object sender, RoutedEventArgs e)
        {
            QuerySelectComboBox.SelectedItem = null;
        }

        /// <summary>
        /// 기준정보 쿼리 보기 버튼 클릭
        /// </summary>
        private void ViewPlanQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (QuerySelectComboBox.SelectedItem is QueryItem query && query.OrderNumber >= 0)
            {
                // 🔥 쿼리 RowNumber와 DB 경로를 포함하여 팝업 열기 (편집 및 저장 가능)
                var window = new QueryTextEditWindow(
                    query.Query, 
                    isReadOnly: true, 
                    queryId: query.RowNumber, 
                    databasePath: _sharedData?.Settings.DatabasePath ?? "")
                {
                    Title = $"쿼리 보기 - {query.QueryName}",
                    Owner = Window.GetWindow(this),
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                
                // 🔥 저장 후 쿼리 목록 다시 로드
                window.QuerySaved += (s, args) =>
                {
                    LoadInfoQueries();
                    UpdateAllGridComboBoxes();
                    
                    // 현재 선택된 쿼리 유지
                    var selectedQueryId = query.RowNumber;
                    var planQueryList = QuerySelectComboBox.ItemsSource as List<QueryItem>;
                    if (planQueryList != null)
                    {
                        var updatedQuery = planQueryList.FirstOrDefault(q => q.RowNumber == selectedQueryId);
                        if (updatedQuery != null)
                        {
                            QuerySelectComboBox.SelectedItem = updatedQuery;
                        }
                    }
                };
                
                window.ShowDialog();
            }
            else
            {
                MessageBox.Show("먼저 기준정보 쿼리를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 동적 그리드 생성 (그리드 인덱스 기준으로)
        /// </summary>
        private DynamicGridInfo CreateDynamicGrid(int index)
        {
            var queryComboBox = new ComboBox
            {
                Width = 180,
                Height = 28,
                DisplayMemberPath = "BizName", // 🔥 QueryName → BizName으로 변경
                Margin = new Thickness(10, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            // SelectionChanged 이벤트 제거 - 저장 불필요

            var clearButton = new Button
            {
                Content = "✖",
                Width = 28,
                Height = 28,
                Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 5, 0),
                ToolTip = "선택 취소"
            };
            clearButton.Click += (s, e) => queryComboBox.SelectedItem = null;

            var resultInfoTextBlock = new TextBlock
            {
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Text = ""
            };

            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = true,
                IsReadOnly = true,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserReorderColumns = true,
                CanUserResizeColumns = true
                // 🔥 나머지 스타일은 App.xaml의 암묵적 스타일 자동 적용
            };
            
            // 🔥 App.xaml 스타일 자동 적용 (헤더, 셀, 행 스타일 코드 삭제)
            
            dataGrid.AutoGeneratingColumn += DataGrid_AutoGeneratingColumn;
            dataGrid.LoadingRow += DataGrid_LoadingRow;
            
            // 🔥 행 번호 표시 활성화 (FACTOVA Grid 스타일)
            DataGridHelper.EnableRowNumbers(dataGrid);
            
            // 🔥 복사 이벤트 핸들러 추가
            dataGrid.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    try
                    {
                        var dg = s as DataGrid;
                        if (dg != null && dg.ItemsSource is DataView dataView && dataView.Count > 0)
                        {
                            var selectedCells = dg.SelectedCells;
                            if (selectedCells.Count == 0) return;

                            // 선택된 셀들을 정렬하여 행/열 순서대로 정리
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

                            // 행과 열로 그룹화
                            var rows = cellInfos.GroupBy(x => x.RowIndex)
                                .OrderBy(g => g.Key)
                                .Select(g => string.Join("\t", g.OrderBy(x => x.ColumnIndex).Select(x => x.Value)))
                                .ToList();

                            var textToCopy = string.Join(Environment.NewLine, rows);
                            Clipboard.SetText(textToCopy);
                            e.Handled = true;
}
                    }
                    catch (Exception ex)
                    {
}
                }
            };

            return new DynamicGridInfo
            {
                Index = index,
                QueryComboBox = queryComboBox,
                DataGrid = dataGrid,
                ClearButton = clearButton,
                ResultInfoTextBlock = resultInfoTextBlock
            };
        }

        private Grid CreateGridContainer(DynamicGridInfo gridInfo)
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var titleBlock = new TextBlock
            {
                Text = $"그리드 {gridInfo.Index}",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 쿼리 실행 버튼
            var executeButton = new Button
            {
                Content = "▶",
                Width = 30,
                Height = 28,
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 5, 0)
            };
            executeButton.Click += async (s, e) => await ExecuteDynamicGridQuery(gridInfo);

            // 🔥 팝업 보기 버튼
            var popupButton = new Button
            {
                Content = "🔍",
                Width = 30,
                Height = 28,
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 5, 0),
                ToolTip = "팝업으로 보기"
            };
            popupButton.Click += (s, e) => ShowGridInPopup(gridInfo);

            // 🔥 셀 상세 보기 버튼 (피벗)
            var cellDetailButton = new Button
            {
                Content = "📊",
                Width = 30,
                Height = 28,
                Background = new SolidColorBrush(Color.FromRgb(111, 66, 193)), // #6F42C1 보라색
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 5, 0),
                ToolTip = "선택 행 상세 보기 (피벗)"
            };
            cellDetailButton.Click += (s, e) => ShowCellDetailPopup(gridInfo);

            // 쿼리 보기 버튼
            var viewQueryButton = new Button
            {
                Content = "📝 쿼리 보기",
                Width = 90,
                Height = 28,
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)), // #FF28A745
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 10, 0),
                ToolTip = "쿼리 보기"
            };
            
            // 마우스 오버 스타일 추가
            var viewButtonStyle = new Style(typeof(Button));
            viewButtonStyle.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(40, 167, 69))));
            var viewTrigger = new System.Windows.Trigger { Property = Button.IsMouseOverProperty, Value = true };
            viewTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(33, 136, 56)))); // #FF218838
            viewButtonStyle.Triggers.Add(viewTrigger);
            viewQueryButton.Style = viewButtonStyle;
            
            viewQueryButton.Click += (s, e) =>
            {
                if (gridInfo.QueryComboBox.SelectedItem is QueryItem query)
                {
                    // 🔥 쿼리 RowNumber와 DB 경로를 포함하여 팝업 열기 (편집 및 저장 가능)
                    var window = new QueryTextEditWindow(
                        query.Query, 
                        isReadOnly: true, 
                        queryId: query.RowNumber, 
                        databasePath: _sharedData?.Settings.DatabasePath ?? "")
                    {
                        Title = $"쿼리 보기 - {query.QueryName}",
                        Owner = Window.GetWindow(this),
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    
                    // 🔥 저장 후 쿼리 목록 다시 로드
                    window.QuerySaved += (sender, args) =>
                    {
                        LoadInfoQueries();
                        UpdateAllGridComboBoxes();
                        
                        // 현재 선택된 쿼리 유지
                        var selectedQueryId = query.RowNumber;
                        var queriesWithBizName = _infoQueries
                            .Where(q => !string.IsNullOrWhiteSpace(q.BizName) && 
                                       !string.Equals(q.ExcludeFlag, "Y", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        
                        var updatedQuery = queriesWithBizName.FirstOrDefault(q => q.RowNumber == selectedQueryId);
                        if (updatedQuery != null)
                        {
                            gridInfo.QueryComboBox.SelectedItem = updatedQuery;
                        }
                    };
                    
                    window.ShowDialog();
                }
                else
                {
                    MessageBox.Show("먼저 쿼리를 선택하세요.", "알림",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };

            headerPanel.Children.Add(titleBlock);
            headerPanel.Children.Add(gridInfo.QueryComboBox);  // 쿼리 선택 콤보박스
            headerPanel.Children.Add(gridInfo.ClearButton);     // 취소 버튼
            headerPanel.Children.Add(executeButton);            // 실행 버튼
            headerPanel.Children.Add(popupButton);              // 🔥 팝업 보기 버튼
            headerPanel.Children.Add(cellDetailButton);         // 🔥 셀 상세 보기 버튼 (피벗)
            headerPanel.Children.Add(viewQueryButton);          // 쿼리 보기 버튼
            headerPanel.Children.Add(gridInfo.ResultInfoTextBlock); // 결과 정보

            Grid.SetRow(headerPanel, 0);
            Grid.SetRow(gridInfo.DataGrid, 1);

            grid.Children.Add(headerPanel);
            grid.Children.Add(gridInfo.DataGrid);

            return grid;
        }

        private void UpdateAllGridComboBoxes()
        {
            BindGridComboBoxes(null);
        }

        // 🔥 그리드 팝업으로 표시하는 메서드
        private void ShowGridInPopup(DynamicGridInfo gridInfo)
        {
            if (gridInfo.DataGrid.ItemsSource == null)
            {
                MessageBox.Show("조회된 데이터가 없습니다.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dataView = gridInfo.DataGrid.ItemsSource as DataView;
            if (dataView == null || dataView.Count == 0)
            {
                MessageBox.Show("조회된 데이터가 없습니다.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var popupWindow = new Windows.GridPopupWindow
                {
                    Owner = Window.GetWindow(this),
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                // 제목 설정
                var queryName = (gridInfo.QueryComboBox.SelectedItem as QueryItem)?.BizName ?? $"그리드 {gridInfo.Index}";
                popupWindow.SetTitle(queryName);

                // 정보 설정
                var info = gridInfo.ResultInfoTextBlock.Text;
                popupWindow.SetInfo(info);

                // 데이터 설정
                popupWindow.SetDataSource(dataView);

                popupWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"팝업 표시 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔥 선택된 행의 데이터를 피벗 형태로 표시하는 팝업
        private void ShowCellDetailPopup(DynamicGridInfo gridInfo)
        {
            if (gridInfo.DataGrid.SelectedItem == null)
            {
                MessageBox.Show("행을 선택해주세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var popup = new Windows.CellDetailPopupWindow
                {
                    Owner = Window.GetWindow(this),
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                // DataRowView인 경우 (DataTable 바인딩)
                if (gridInfo.DataGrid.SelectedItem is DataRowView rowView)
                {
                    int rowIndex = gridInfo.DataGrid.Items.IndexOf(rowView);
                    popup.SetDataFromDataRowView(rowView, rowIndex);
                }
                // 일반 객체인 경우
                else
                {
                    int rowIndex = gridInfo.DataGrid.SelectedIndex;
                    popup.SetDataFromObject(gridInfo.DataGrid.SelectedItem, rowIndex);
                }

                popup.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"셀 상세 보기 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task ExecuteDynamicGridQuery(DynamicGridInfo gridInfo)
        {
            if (gridInfo.QueryComboBox.SelectedItem is not QueryItem query)
            {
                MessageBox.Show("조회할 쿼리를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 계획정보 DataGrid에서 선택된 항목 확인
            if (PlanInfoDataGrid.ItemsSource == null)
            {
                MessageBox.Show("계획정보를 먼저 조회하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // SelectedItem으로 선택된 행 가져오기
            DataRowView? selectedRow = PlanInfoDataGrid.SelectedItem as DataRowView;
            
            if (selectedRow == null)
            {
                MessageBox.Show("계획정보에서 행을 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // 시간 측정 시작
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                await ExecuteQueryToGridWithRowData(query, gridInfo.DataGrid, selectedRow);
                
                stopwatch.Stop();
                
                // 조회 결과 정보 표시
                int rowCount = gridInfo.DataGrid.Items.Count;
                double seconds = stopwatch.Elapsed.TotalSeconds;
                gridInfo.ResultInfoTextBlock.Text = $"📊 {rowCount}건 | ⏱️ {seconds:F2}초";
                gridInfo.ResultInfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // 초록색
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                gridInfo.ResultInfoTextBlock.Text = $"❌ 오류";
                gridInfo.ResultInfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // 빨간색
                throw;
            }
        }

        private async System.Threading.Tasks.Task ExecuteQueryToGridWithRowDataAndMeasure(
            QueryItem queryItem,
            DynamicGridInfo gridInfo,
            DataRowView selectedRow,
            System.Diagnostics.Stopwatch stopwatch)
        {
            try
            {
                await ExecuteQueryToGridWithRowData(queryItem, gridInfo.DataGrid, selectedRow);
                
                stopwatch.Stop();
                
                // 조회 결과 정보 표시
                int rowCount = gridInfo.DataGrid.Items.Count;
                double seconds = stopwatch.Elapsed.TotalSeconds;
                
                // UI 업데이트는 Dispatcher를 통해 수행
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    gridInfo.ResultInfoTextBlock.Text = $"📊 {rowCount}건 | ⏱️ {seconds:F2}초";
                    gridInfo.ResultInfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // 초록색
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    gridInfo.ResultInfoTextBlock.Text = $"❌ 오류";
                    gridInfo.ResultInfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // 빨강색
                });
                
                throw new Exception($"[{queryItem.QueryName}] {ex.Message}", ex);
            }
        }

        private async void ExecuteQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (QuerySelectComboBox.SelectedItem is not QueryItem selectedQuery ||
                selectedQuery.OrderNumber < 0) // 플레이스홀더 체크
            {
                MessageBox.Show("조회할 쿼리를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 시간 측정 시작
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                await ExecuteQueryToGrid(selectedQuery, PlanInfoDataGrid);
                
                stopwatch.Stop();
                
                // 조회 결과 정보 표시
                int rowCount = PlanInfoDataGrid.Items.Count;
                double seconds = stopwatch.Elapsed.TotalSeconds;
                PlanInfoResultTextBlock.Text = $"📊 {rowCount}건 | ⏱️ {seconds:F2}초";
                PlanInfoResultTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // 초록색
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                PlanInfoResultTextBlock.Text = $"❌ 오류";
                PlanInfoResultTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // 빨간색
            }
        }

        private async System.Threading.Tasks.Task ExecuteQueryToGrid(QueryItem queryItem, DataGrid targetGrid)
        {
            if (_sharedData == null) return;
            
            // 🔥 ROWNUM 제한 설정 적용
            _dbService?.SetRowLimit(_sharedData.Settings.EnableRowLimit, _sharedData.Settings.RowLimitCount);

            try
            {
                string connectionString;

                // 🔥 1순위: Version 정보가 있으면 사업장 정보의 TNS 사용
                if (!string.IsNullOrWhiteSpace(queryItem.Version) && SiteComboBox.SelectedItem is SiteInfo selectedSite)
                {
                    var tnsName = selectedSite.GetTnsForVersion(queryItem.Version);

                    if (string.IsNullOrEmpty(tnsName))
                    {
                        MessageBox.Show($"사업장 '{selectedSite.SiteName}'에 버전 {queryItem.Version}에 대한 TNS 설정이 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var connectionInfoService = new Services.ConnectionInfoService(_sharedData.Settings.DatabasePath);
                    var allConnections = connectionInfoService.GetAll();
                    var connectionInfo = allConnections.FirstOrDefault(c => c.Name == tnsName);

                    if (connectionInfo == null)
                    {
                        MessageBox.Show($"접속 정보 '{tnsName}'를 찾을 수 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(connectionInfo.TNS, StringComparison.OrdinalIgnoreCase));

                    if (selectedTns == null)
                    {
                        MessageBox.Show($"TNS '{connectionInfo.TNS}'를 찾을 수 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 🔥 OracleDbService 연결 설정
                    await _dbService!.ConfigureAsync(selectedTns, connectionInfo.UserId, connectionInfo.Password);
                }
                else if (queryItem.ConnectionInfoId.HasValue)
                {
                    var connectionInfoService = new Services.ConnectionInfoService(_sharedData.Settings.DatabasePath);
                    var allConnections = connectionInfoService.GetAll();
                    var connectionInfo = allConnections.FirstOrDefault(c => c.Id == queryItem.ConnectionInfoId.Value);
                    
                    if (connectionInfo == null)
                    {
                        MessageBox.Show($"접속 정보 ID {queryItem.ConnectionInfoId.Value}를 찾을 수 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(connectionInfo.TNS, StringComparison.OrdinalIgnoreCase));
                    
                    if (selectedTns == null)
                    {
                        MessageBox.Show($"TNS '{connectionInfo.TNS}'를 찾을 수 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // 🔥 OracleDbService 연결 설정
                    await _dbService!.ConfigureAsync(selectedTns, connectionInfo.UserId, connectionInfo.Password);
                }
                else if (!string.IsNullOrWhiteSpace(queryItem.Host) &&
                    !string.IsNullOrWhiteSpace(queryItem.Port) &&
                    !string.IsNullOrWhiteSpace(queryItem.ServiceName))
                {
                    // 🔥 직접 연결 정보 사용
                    var tnsString = $"(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={queryItem.Host})(PORT={queryItem.Port}))(CONNECT_DATA=(SERVICE_NAME={queryItem.ServiceName})))";
                    await _dbService!.ConfigureAsync(tnsString, queryItem.UserId, queryItem.Password);
                }
                else if (!string.IsNullOrWhiteSpace(queryItem.TnsName))
                {
                    var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(queryItem.TnsName, StringComparison.OrdinalIgnoreCase));

                    if (selectedTns == null)
                    {
                        MessageBox.Show($"TNS '{queryItem.TnsName}'를 찾을 수 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 🔥 OracleDbService 연결 설정
                    await _dbService!.ConfigureAsync(selectedTns, queryItem.UserId, queryItem.Password);
                }
                else
                {
                    MessageBox.Show("연결 정보가 없습니다.\n쿼리에 TNS 또는 접속 정보를 설정해주세요.", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string processedQuery = ReplaceQueryParameters(queryItem.Query);

                // 🔥 OracleDbService로 쿼리 실행
                var result = await _dbService!.ExecuteQueryAsync(processedQuery);

                // 🔥 ItemsSource와 Columns을 모두 초기화 후 바인딩
                targetGrid.ItemsSource = null;
                targetGrid.Columns.Clear();
                
                // 🔥 데이터가 있을 때만 바인딩
                if (result != null && result.Rows.Count > 0)
                {
                    targetGrid.ItemsSource = result.DefaultView;
                    ApplyFontSizeToGrid(targetGrid);
                }
                else
                {
}
            }
            catch (Exception ex)
            {
                // 🔥 오류 발생 시에도 그리드 초기화
                targetGrid.ItemsSource = null;
                targetGrid.Columns.Clear();
                
                MessageBox.Show($"쿼리 실행 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 🔥 연결 해제
                _dbService?.Disconnect();
            }
        }

        private async System.Threading.Tasks.Task ExecuteQueryToGridWithRowData(
            QueryItem queryItem, 
            DataGrid targetGrid, 
            DataRowView selectedRow)
        {
            if (_sharedData == null) return;

            try
            {
                // 🔥 1순위: Version 정보가 있으면 사업장 정보의 TNS 사용
                if (!string.IsNullOrWhiteSpace(queryItem.Version) && SiteComboBox.SelectedItem is SiteInfo selectedSite)
                {
                    var tnsName = selectedSite.GetTnsForVersion(queryItem.Version);

                    if (string.IsNullOrEmpty(tnsName))
                    {
                        throw new Exception($"사업장 '{selectedSite.SiteName}'에 버전 {queryItem.Version}에 대한 TNS 설정이 없습니다.");
                    }

                    var connectionInfoService = new Services.ConnectionInfoService(_sharedData.Settings.DatabasePath);
                    var allConnections = connectionInfoService.GetAll();
                    var connectionInfo = allConnections.FirstOrDefault(c => c.Name == tnsName);

                    if (connectionInfo == null)
                    {
                        throw new Exception($"접속 정보 '{tnsName}'를 찾을 수 없습니다.");
                    }

                    var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(connectionInfo.TNS, StringComparison.OrdinalIgnoreCase));

                    if (selectedTns == null)
                    {
                        throw new Exception($"TNS '{connectionInfo.TNS}'를 찾을 수 없습니다.");
                    }

                    await _dbService!.ConfigureAsync(selectedTns, connectionInfo.UserId, connectionInfo.Password);
                }
                else if (queryItem.ConnectionInfoId.HasValue)
                {
                    var connectionInfoService = new Services.ConnectionInfoService(_sharedData.Settings.DatabasePath);
                    var allConnections = connectionInfoService.GetAll();
                    var connectionInfo = allConnections.FirstOrDefault(c => c.Id == queryItem.ConnectionInfoId.Value);
                    
                    if (connectionInfo == null)
                    {
                        throw new Exception($"접속 정보 ID {queryItem.ConnectionInfoId.Value}를 찾을 수 없습니다.");
                    }
                    
                    var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(connectionInfo.TNS, StringComparison.OrdinalIgnoreCase));
                    
                    if (selectedTns == null)
                    {
                        throw new Exception($"TNS '{connectionInfo.TNS}'를 찾을 수 없습니다.");
                    }
                    
                    await _dbService!.ConfigureAsync(selectedTns, connectionInfo.UserId, connectionInfo.Password);
                }
                else if (!string.IsNullOrWhiteSpace(queryItem.Host) &&
                    !string.IsNullOrWhiteSpace(queryItem.Port) &&
                    !string.IsNullOrWhiteSpace(queryItem.ServiceName))
                {
                    var tnsString = $"(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={queryItem.Host})(PORT={queryItem.Port}))(CONNECT_DATA=(SERVICE_NAME={queryItem.ServiceName})))";
                    await _dbService!.ConfigureAsync(tnsString, queryItem.UserId, queryItem.Password);
                }
                else if (!string.IsNullOrWhiteSpace(queryItem.TnsName))
                {
                    var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(queryItem.TnsName, StringComparison.OrdinalIgnoreCase));

                    if (selectedTns == null)
                    {
                        throw new Exception($"TNS '{queryItem.TnsName}'를 찾을 수 없습니다.");
                    }

                    await _dbService!.ConfigureAsync(selectedTns, queryItem.UserId, queryItem.Password);
                }
                else
                {
                    throw new Exception("연결 정보가 없습니다. 쿼리에 TNS 또는 접속 정보를 설정해주세요.");
                }

                string processedQuery = ReplaceQueryParametersWithRowData(queryItem.Query, selectedRow);

                // 🔥 OracleDbService로 쿼리 실행
                var result = await _dbService!.ExecuteQueryAsync(processedQuery);

                // 🔥 ItemsSource와 Columns을 모두 초기화 후 바인딩
                targetGrid.ItemsSource = null;
                targetGrid.Columns.Clear();
                
                // 🔥 데이터가 있을 때만 바인딩
                if (result != null && result.Rows.Count > 0)
                {
                    targetGrid.ItemsSource = result.DefaultView;
                    ApplyFontSizeToGrid(targetGrid);
                }
                else
                {
}
            }
            catch (Exception ex)
            {
                // 🔥 오류 발생 시에도 그리드 초기화
                targetGrid.ItemsSource = null;
                targetGrid.Columns.Clear();
                
                throw new Exception($"[{queryItem.QueryName}] {ex.Message}", ex);
            }
            // 🔥 Disconnect() 제거 - ExecuteQueryAsync 내부에서 연결 관리하므로 불필요
            // 병렬 실행 시 다른 쿼리의 연결을 끊는 문제 해결
        }

        private string ReplaceQueryParametersWithRowData(string query, DataRowView selectedRow)
        {
            string result = query;

            if (selectedRow != null)
            {
                var row = selectedRow.Row;
                var table = row.Table;

                foreach (DataColumn column in table.Columns)
                {
                    string columnName = column.ColumnName;
                    string columnValue = row[column]?.ToString() ?? "";
                    
                    // 🔥 이스케이프된 컬럼명(__) → 원본 컬럼명(_)으로 변환하여 파라미터 매칭
                    string originalColumnName = columnName.Replace("__", "_");

                    // 🔥 DB Link 보호: 파라미터만 치환 (TABLE@DBLINK 형식은 치환하지 않음)
                    string parameterName = $"@{originalColumnName}";
                    result = SafeReplaceParameter(result, parameterName, $"'{columnValue}'");

                    // 언더스코어 제거한 버전으로도 치환
                    string parameterNameNoUnderscore = $"@{originalColumnName.Replace("_", "")}";
                    result = SafeReplaceParameter(result, parameterNameNoUnderscore, $"'{columnValue}'");
                }
            }

            return result;
        }

        private string ReplaceQueryParameters(string query)
        {
            string result = query;

            // 🔥 사업장 정보가 선택되어 있으면 해당 정보 사용
            string factory = _sharedData?.Settings.GmesFactory ?? "";
            string org = _sharedData?.Settings.GmesOrg ?? "";
            string facility = _sharedData?.Settings.GmesFacility ?? "";
            string wipLineId = _sharedData?.Settings.GmesWipLineId ?? "";
            string equipLineId = _sharedData?.Settings.GmesEquipLineId ?? "";

            // 🔥 디버깅: 쿼리 파라미터 치환 전 값 확인



// 🔥 DB Link 보호: 파라미터만 치환 (TABLE@DBLINK 형식은 치환하지 않음)
            result = SafeReplaceParameter(result, "@REPRESENTATIVE_FACTORY_CODE", $"'{factory}'");
            result = SafeReplaceParameter(result, "@ORGANIZATION_ID", $"'{org}'");
            result = SafeReplaceParameter(result, "@PRODUCTION_YMD_START", $"'{DateFromPicker.SelectedDate?.ToString("yyyyMMdd") ?? ""}'");
            result = SafeReplaceParameter(result, "@PRODUCTION_YMD_END", $"'{DateToPicker.SelectedDate?.ToString("yyyyMMdd") ?? ""}'");
            result = SafeReplaceParameter(result, "@WIP_LINE_ID", $"'{wipLineId}'");
            result = SafeReplaceParameter(result, "@LINE_ID", $"'{equipLineId}'");
            result = SafeReplaceParameter(result, "@FACILITY_CODE", $"'{facility}'");
            result = SafeReplaceParameter(result, "@WORK_ORDER_ID", $"'{WorkOrderTextBox.Text}'");
            result = SafeReplaceParameter(result, "@WORK_ORDER_NAME", $"'{WorkOrderNameTextBox.Text}'");
            result = SafeReplaceParameter(result, "@PRODUCT_SPECIFICATION_ID", $"'{ModelSuffixTextBox.Text}'");
            result = SafeReplaceParameter(result, "@LOT_ID", $"'{LotIdTextBox.Text}'");
            result = SafeReplaceParameter(result, "@EQUIPMENT_ID", $"'{EquipmentIdTextBox.Text}'");
            
            // 🔥 PARAM1~PARAM4 치환
            result = SafeReplaceParameter(result, "@PARAM1", $"'{Param1TextBox.Text}'");
            result = SafeReplaceParameter(result, "@PARAM2", $"'{Param2TextBox.Text}'");
            result = SafeReplaceParameter(result, "@PARAM3", $"'{Param3TextBox.Text}'");
            result = SafeReplaceParameter(result, "@PARAM4", $"'{Param4TextBox.Text}'");

            return result;
        }

        /// <summary>
        /// 🔥 DB Link를 보호하면서 파라미터만 안전하게 치환
        /// DB Link: TABLE@DBLINK (@ 앞에 영문자/숫자가 붙음)
        /// 파라미터: @PARAM_NAME (@ 앞에 공백, 연산자, 괄호 등이 있음)
        /// </summary>
        private string SafeReplaceParameter(string query, string paramName, string value)
        {
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(paramName))
                return query;

            // 파라미터가 존재하는지 먼저 확인
            int index = 0;
            while ((index = query.IndexOf(paramName, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                // @ 바로 앞 문자 확인
                if (index > 0)
                {
                    char prevChar = query[index - 1];
                    
                    // @ 앞에 영문자/숫자가 있으면 DB Link이므로 건너뜀
                    if (char.IsLetterOrDigit(prevChar) || prevChar == '_')
                    {
                        index += paramName.Length;
                        continue;
                    }
                }

                // @ 바로 뒤 문자 확인 (파라미터명 뒤에 영문자/숫자가 더 있으면 건너뜀)
                int endIndex = index + paramName.Length;
                if (endIndex < query.Length)
                {
                    char nextChar = query[endIndex];
                    if (char.IsLetterOrDigit(nextChar) || nextChar == '_')
                    {
                        index += paramName.Length;
                        continue;
                    }
                }

                // 파라미터 치환
                query = query.Substring(0, index) + value + query.Substring(endIndex);
                index += value.Length;
            }

            return query;
        }

        private void DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string header = e.Column.Header as string ?? e.PropertyName;
            
            if (!string.IsNullOrEmpty(header))
            {
                e.Column.Header = header.Replace("_", "__");
            }
            
            // 🔥 NERP 스타일: 정렬 활성화
            e.Column.CanUserSort = true;
            
            // 🔥 NERP 스타일: 숫자 타입 컬umn 자동 인식
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
                    textColumn.Binding = new System.Windows.Data.Binding(e.PropertyName)
                    {
                        StringFormat = "#,##0.######" // 소수점 있는 경우도 처리
                    };
                }
                
                textColumn.ElementStyle = displayStyle;
                
                // 🔥 그냥 Auto - 헤더/데이터 중 더 긴 쪽에 맞춤
                e.Column.Width = DataGridLength.Auto;
            }
            
            // 🔥 일반 컬럼 선택 시 글자색 검정 유지
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
            
            var selectedTrigger = new System.Windows.Trigger
            {
                Property = DataGridCell.IsSelectedProperty,
                Value = true
            };
            selectedTrigger.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
            selectedTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(173, 216, 230)))); // 연한 파란색
            cellStyle.Triggers.Add(selectedTrigger);
            
            e.Column.CellStyle = cellStyle;
            
            // 🔥 CLOB 컬럼 처리 (기존 로직 유지)
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
        
        /// <summary>
        /// CLOB 컬럼용 TextBox 셀 템플릿 생성
        /// </summary>
        private DataTemplate CreateClobCellTemplate(string columnName)
        {
            var factory = new FrameworkElementFactory(typeof(TextBox));
            
            // 바인딩 설정
            factory.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding(columnName)
            {
                Mode = System.Windows.Data.BindingMode.OneWay
            });
            
            // TextBox 속성 설정
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

        /// <summary>
        /// DataGrid 행 로드 시 색상 컬럼 값에 따라 배경색/글자색 설정
        /// BACKGROUND_COLOR, FOREGROUND_COLOR 컬럼 또는 CHK 컬럼 지원
        /// </summary>
        private void DataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is DataRowView rowView)
            {
                var row = rowView.Row;
                var table = row.Table;
                
                // 🔥 기본값 초기화
                e.Row.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
                e.Row.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
                
                // 🔥 BACKGROUND_COLOR 컬럼 확인 (원본 또는 이스케이프된 이름)
                string? bgColor = GetColumnValue(row, table, "BACKGROUND_COLOR");
                if (!string.IsNullOrWhiteSpace(bgColor))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(bgColor);
                        e.Row.Background = new SolidColorBrush(color);
                    }
                    catch
                    {
}
                }
                
                // 🔥 FOREGROUND_COLOR 컬럼 확인 (원본 또는 이스케이프된 이름)
                string? fgColor = GetColumnValue(row, table, "FOREGROUND_COLOR");
                if (!string.IsNullOrWhiteSpace(fgColor))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(fgColor);
                        e.Row.Foreground = new SolidColorBrush(color);
                    }
                    catch
                    {
}
                }
                
                // 🔥 CHK 컬럼 처리 (기존 로직 - BACKGROUND/FOREGROUND가 없을 때만)
                if (string.IsNullOrWhiteSpace(bgColor) && string.IsNullOrWhiteSpace(fgColor))
                {
                    string? chkValue = GetColumnValue(row, table, "CHK");
                    if (chkValue == "E")
                    {
                        e.Row.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
                        e.Row.Foreground = new SolidColorBrush(Color.FromRgb(139, 0, 0));
                    }
                }
            }
        }

        /// <summary>
        /// 🔥 컬럼 값 가져오기 (원본 이름 또는 이스케이프된 이름 지원)
        /// </summary>
        private string? GetColumnValue(DataRow row, DataTable table, string columnName)
        {
            // 원본 컬럼명으로 먼저 시도
            if (table.Columns.Contains(columnName))
            {
                return row[columnName]?.ToString()?.Trim();
            }
            // 이스케이프된 컬럼명으로 시도 (_를 __로)
            var escapedName = columnName.Replace("_", "__");
            if (table.Columns.Contains(escapedName))
            {
                return row[escapedName]?.ToString()?.Trim();
            }
            return null;
        }

        private void DecreaseFontButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            if (_sharedData.Settings.FontSize > 8)
            {
                _sharedData.Settings.FontSize--;
                _sharedData.SaveSettingsCallback?.Invoke();
                ApplyFontSize();
                UpdateFontSizeDisplay();
            }
        }

        private void IncreaseFontButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            if (_sharedData.Settings.FontSize < 30)
            {
                _sharedData.Settings.FontSize++;
                _sharedData.SaveSettingsCallback?.Invoke();
                ApplyFontSize();
                UpdateFontSizeDisplay();
            }
        }

        private void UpdateFontSizeDisplay()
        {
            if (_sharedData == null) return;
            FontSizeTextBlock.Text = _sharedData.Settings.FontSize.ToString();
        }

        private void ExportToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 파일 저장 대화상자
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = $"GMES정보조회_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    DefaultExt = ".xlsx"
                };

                if (saveFileDialog.ShowDialog() != true)
                    return;

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    // 1. 계획정보 시트 추가
                    if (PlanInfoDataGrid.ItemsSource != null)
                    {
                        var planDataView = PlanInfoDataGrid.ItemsSource as DataView;
                        if (planDataView != null && planDataView.Count > 0)
                        {
                            AddDataGridToExcel(package, "계획정보", planDataView.ToTable());
                        }
                    }

                    // 2. 각 동적 그리드를 개별 시트로 추가
                    foreach (var gridInfo in _dynamicGrids)
                    {
                        if (gridInfo.DataGrid.ItemsSource != null)
                        {
                            var dataView = gridInfo.DataGrid.ItemsSource as DataView;
                            if (dataView != null && dataView.Count > 0)
                            {
                                var queryName = (gridInfo.QueryComboBox.SelectedItem as QueryItem)?.QueryName ?? $"그리드{gridInfo.Index}";
                                
                                // 시트 이름은 최대 31자로 제한하고 특수문자 제거
                                string sheetName = SanitizeSheetName(queryName, gridInfo.Index);
                                
                                AddDataGridToExcel(package, sheetName, dataView.ToTable());
                            }
                        }
                    }

                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        MessageBox.Show("다운로드할 데이터가 없습니다.\n먼저 쿼리를 조회하세요.", "알림",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Excel 파일 저장
                    var fileInfo = new FileInfo(saveFileDialog.FileName);
                    package.SaveAs(fileInfo);

                    MessageBox.Show($"Excel 파일이 성공적으로 저장되었습니다.\n\n파일: {fileInfo.Name}\n시트 수: {package.Workbook.Worksheets.Count}개",
                        "완료", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 파일 열기 여부 확인
                    var result = MessageBox.Show("저장된 Excel 파일을 여시겠습니까?", "확인",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = fileInfo.FullName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel 파일 생성 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// DataTable을 Excel 시트로 추가합니다.
        /// </summary>
        private void AddDataGridToExcel(ExcelPackage package, string sheetName, DataTable dataTable)
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // 헤더 작성
            for (int col = 0; col < dataTable.Columns.Count; col++)
            {
                var columnName = dataTable.Columns[col].ColumnName;
                // 언더스코어를 공백으로 변경하여 가독성 향상
                worksheet.Cells[1, col + 1].Value = columnName.Replace("_", " ");
            }

            // 헤더 스타일
            using (var range = worksheet.Cells[1, 1, 1, dataTable.Columns.Count])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 120, 215)); // #FF0078D7
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // 데이터 작성
            for (int row = 0; row < dataTable.Rows.Count; row++)
            {
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    var cellValue = dataTable.Rows[row][col];
                    var cell = worksheet.Cells[row + 2, col + 1];

                    // 값 설정
                    if (cellValue != null && cellValue != DBNull.Value)
                    {
                        // 숫자 타입 처리
                        if (cellValue is decimal || cellValue is double || cellValue is float || 
                            cellValue is int || cellValue is long)
                        {
                            cell.Value = cellValue;
                        }
                        // DateTime 타입 처리
                        else if (cellValue is DateTime dateTime)
                        {
                            cell.Value = dateTime;
                            cell.Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                        }
                        else
                        {
                            cell.Value = cellValue.ToString();
                        }
                    }

                    // 테두리 추가
                    cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                // CHK 컬럼이 'E'인 경우 행 배경색을 빨간색으로 설정
                if (dataTable.Columns.Contains("CHK"))
                {
                    var chkValue = dataTable.Rows[row]["CHK"]?.ToString()?.Trim();
                    if (chkValue == "E")
                    {
                        using (var rowRange = worksheet.Cells[row + 2, 1, row + 2, dataTable.Columns.Count])
                        {
                            rowRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            rowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 200, 200));
                            rowRange.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(139, 0, 0));
                        }
                    }
                }
            }

            // 열 너비 자동 조정
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // 최소/최대 열 너비 설정
            for (int col = 1; col <= dataTable.Columns.Count; col++)
            {
                var column = worksheet.Column(col);
                if (column.Width < 10)
                    column.Width = 10;
                else if (column.Width > 50)
                    column.Width = 50;
            }

            // 틀 고정 (헤더 행)
            worksheet.View.FreezePanes(2, 1);
        }

        /// <summary>
        /// Excel 시트 이름에서 사용할 수 없는 문자를 제거하고 길이를 제한합니다.
        /// </summary>
        private string SanitizeSheetName(string name, int index)
        {
            // Excel 시트 이름에서 사용할 수 없는 문자: \ / * ? : [ ]
            var invalidChars = new char[] { '\\', '/', '*', '?', ':', '[', ']' };
            string sanitized = name;

            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c.ToString(), "");
            }

            // 최대 31자로 제한 (Excel 시트 이름 제한)
            if (sanitized.Length > 25)
            {
                sanitized = sanitized.Substring(0, 25);
            }

            // 그리드 번호 추가
            sanitized = $"{index}_{sanitized}";

            return sanitized;
        }

        /// <summary>
        /// 폰트 크기를 적용합니다.
        /// </summary>
        public void ApplyFontSize()
        {
            if (_sharedData == null) return;

            int fontSize = _sharedData.Settings.FontSize;

            // 계획정보 DataGrid에 폰트 크기 적용
            ApplyFontSizeToGrid(PlanInfoDataGrid);

            // 모든 동적 그리드에 폰트 크기 적용
            foreach (var gridInfo in _dynamicGrids)
            {
                ApplyFontSizeToGrid(gridInfo.DataGrid);
            }
        }

        /// <summary>
        /// 특정 DataGrid에 폰트 크기를 적용합니다.
        /// </summary>
        private void ApplyFontSizeToGrid(DataGrid dataGrid)
        {
            if (_sharedData == null) return;

            int fontSize = _sharedData.Settings.FontSize;

            // DataGrid 본문 폰트 크기 적용
            dataGrid.FontSize = fontSize;

            // 🔥 NERP 스타일 헤더 (연한 하늘색 배경 + 진한 파란색 글자)
            var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BackgroundProperty, 
                new SolidColorBrush(Color.FromRgb(240, 248, 255)))); // #F0F8FF
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.ForegroundProperty, 
                new SolidColorBrush(Color.FromRgb(44, 90, 160)))); // #2C5AA0
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, 
                FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.PaddingProperty, 
                new Thickness(8, 5, 8, 5)));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BorderBrushProperty, 
                new SolidColorBrush(Color.FromRgb(176, 196, 222)))); // #B0C4DE
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BorderThicknessProperty, 
                new Thickness(0, 0, 1, 1)));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HorizontalContentAlignmentProperty, 
                HorizontalAlignment.Left));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty, 
                (double)fontSize));
            
            dataGrid.ColumnHeaderStyle = headerStyle;
        }

        /// <summary>
        /// 사업장 새로고침 버튼 클릭
        /// </summary>
        private void RefreshSiteButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSiteInfos();
            MessageBox.Show("사업장 정보가 새로고침되었습니다.", "완료",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// DataGrid 셀의 값을 가져옵니다.
        /// </summary>
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
                
                // Fallback: DataGridTemplateColumn이나 다른 타입의 컬럼
                if (cellInfo.Item is DataRowView rv && cellInfo.Column != null)
                {
                    var colIndex = cellInfo.Column.DisplayIndex;
                    if (colIndex >= 0 && colIndex < rv.Row.Table.Columns.Count)
                    {
                        return rv.Row[colIndex]?.ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
}
            
            return "";
        }
    }
}
