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
            
            // 🔥 사업장 정보 관련 필드 제거 (Factory, Org, Facility, WipLineId, EquipLineId)
            DateFromPicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            DateToPicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
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

                // WORK_ORDER_ID
                if (table.Columns.Contains("WORK_ORDER_ID"))
                {
                    var workOrderId = rowData["WORK_ORDER_ID"]?.ToString() ?? "-";
                    SelectedWorkOrderIdTextBlock.Text = workOrderId;
                }
                else
                {
                    SelectedWorkOrderIdTextBlock.Text = "-";
                }

                // WORK_ORDER_NAME
                if (table.Columns.Contains("WORK_ORDER_NAME"))
                {
                    var workOrderName = rowData["WORK_ORDER_NAME"]?.ToString() ?? "-";
                    SelectedWorkOrderNameTextBlock.Text = workOrderName;
                }
                else
                {
                    SelectedWorkOrderNameTextBlock.Text = "-";
                }

                // PRODUCT_SPECIFICATION_ID
                if (table.Columns.Contains("PRODUCT_SPECIFICATION_ID"))
                {
                    var productSpecId = rowData["PRODUCT_SPECIFICATION_ID"]?.ToString() ?? "-";
                    SelectedProductSpecIdTextBlock.Text = productSpecId;
                }
                else
                {
                    SelectedProductSpecIdTextBlock.Text = "-";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 더블클릭 시 선택된 행 정보 표시 오류: {ex.Message}");
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

        public void Initialize(SharedDataContext sharedData)
        {
            _isInitializing = true;
            
            _sharedData = sharedData;
            _database = new QueryDatabase(sharedData.Settings.DatabasePath);
            
            // 🔥 사업장 정보 로드
            LoadSiteInfos();
            
            LoadInputValues();
            LoadInfoQueries();
            
            // 그리드를 항상 20개로 고정 생성
            GenerateDynamicGridsWithoutBinding(20);
            
            // 폰트 크기 적용
            ApplyFontSize();
            UpdateFontSizeDisplay();
            
            _isInitializing = false;
            
            // 🔥 초기화 완료 후 사업장 정보를 다시 한번 명시적으로 적용
            if (SiteComboBox.SelectedItem is SiteInfo selectedSite)
            {
                _sharedData.Settings.GmesFactory = selectedSite.RepresentativeFactory;
                _sharedData.Settings.GmesOrg = selectedSite.Organization;
                _sharedData.Settings.GmesFacility = selectedSite.Facility;
                _sharedData.Settings.GmesWipLineId = selectedSite.WipLineId;
                _sharedData.Settings.GmesEquipLineId = selectedSite.EquipLineId;
                _sharedData.SaveSettingsCallback?.Invoke();
                
                System.Diagnostics.Debug.WriteLine("=== Initialize 완료 후 사업장 정보 재확인 ===");
                System.Diagnostics.Debug.WriteLine($"선택된 사업장: {selectedSite.SiteName}");
                System.Diagnostics.Debug.WriteLine($"Factory: {_sharedData.Settings.GmesFactory}");
                System.Diagnostics.Debug.WriteLine($"Org: {_sharedData.Settings.GmesOrg}");
                System.Diagnostics.Debug.WriteLine($"Facility: {_sharedData.Settings.GmesFacility}");
                System.Diagnostics.Debug.WriteLine($"WipLineId: {_sharedData.Settings.GmesWipLineId}");
                System.Diagnostics.Debug.WriteLine($"EquipLineId: {_sharedData.Settings.GmesEquipLineId}");
                System.Diagnostics.Debug.WriteLine("============================================");
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
                    
                    System.Diagnostics.Debug.WriteLine($"=== 사업장 로드 완료 ===");
                    System.Diagnostics.Debug.WriteLine($"총 {sites.Count}개 사업장");
                    System.Diagnostics.Debug.WriteLine($"선택된 사업장: {sites[0].SiteName}");
                    System.Diagnostics.Debug.WriteLine($"  - Factory: {sites[0].RepresentativeFactory}");
                    System.Diagnostics.Debug.WriteLine($"  - Org: {sites[0].Organization}");
                    System.Diagnostics.Debug.WriteLine($"  - Facility: {sites[0].Facility}");
                    System.Diagnostics.Debug.WriteLine($"  - WipLineId: {sites[0].WipLineId}");
                    System.Diagnostics.Debug.WriteLine($"  - EquipLineId: {sites[0].EquipLineId}");
                    System.Diagnostics.Debug.WriteLine("========================");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 사업장 정보 로드 오류: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("=== 사업장 선택 변경 (변경 전) ===");
                System.Diagnostics.Debug.WriteLine($"이전 Factory: {_sharedData.Settings.GmesFactory}");
                System.Diagnostics.Debug.WriteLine($"이전 Org: {_sharedData.Settings.GmesOrg}");
                System.Diagnostics.Debug.WriteLine($"이전 Facility: {_sharedData.Settings.GmesFacility}");
                System.Diagnostics.Debug.WriteLine($"이전 WipLineId: {_sharedData.Settings.GmesWipLineId}");
                System.Diagnostics.Debug.WriteLine($"이전 EquipLineId: {_sharedData.Settings.GmesEquipLineId}");
                
                _sharedData.Settings.GmesFactory = selectedSite.RepresentativeFactory;
                _sharedData.Settings.GmesOrg = selectedSite.Organization;
                _sharedData.Settings.GmesFacility = selectedSite.Facility;
                _sharedData.Settings.GmesWipLineId = selectedSite.WipLineId;
                _sharedData.Settings.GmesEquipLineId = selectedSite.EquipLineId;
                _sharedData.SaveSettingsCallback?.Invoke();
                
                // 🔥 디버깅: 변경 후 선택된 사업장 정보 로깅
                System.Diagnostics.Debug.WriteLine("=== 사업장 선택 변경 (변경 후) ===");
                System.Diagnostics.Debug.WriteLine($"사업장명: {selectedSite.SiteName}");
                System.Diagnostics.Debug.WriteLine($"신규 Factory: {_sharedData.Settings.GmesFactory}");
                System.Diagnostics.Debug.WriteLine($"신규 Org: {_sharedData.Settings.GmesOrg}");
                System.Diagnostics.Debug.WriteLine($"신규 Facility: {_sharedData.Settings.GmesFacility}");
                System.Diagnostics.Debug.WriteLine($"신규 WipLineId: {_sharedData.Settings.GmesWipLineId}");
                System.Diagnostics.Debug.WriteLine($"신규 EquipLineId: {_sharedData.Settings.GmesEquipLineId}");
                System.Diagnostics.Debug.WriteLine("===================================");
            }
        }

        /// <summary>
        /// 동적 그리드를 생성하되 쿼리 바인딩은 하지 않음 (최초 로드용)
        /// </summary>
        private void GenerateDynamicGridsWithoutBinding(int count)
        {
            _isInitializing = true;
            
            DynamicGridsContainer.Children.Clear();
            DynamicGridsContainer.RowDefinitions.Clear();
            _dynamicGrids.Clear();

            // 🔥 1열 레이아웃으로 변경 - 행 개수 = 그리드 개수
            int rowCount = count;
            
            // 행 정의 추가
            for (int i = 0; i < rowCount; i++)
            {
                DynamicGridsContainer.RowDefinitions.Add(new RowDefinition 
                { 
                    Height = new GridLength(350, GridUnitType.Pixel) 
                });
            }

            for (int i = 0; i < count; i++)
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

                // 🔥 1열 레이아웃: Grid.Row만 설정, Grid.Column은 항상 0
                Grid.SetRow(border, i);
                Grid.SetColumn(border, 0);

                DynamicGridsContainer.Children.Add(border);
            }

            // 모든 "정보 조회" 쿼리를 콤보박스에 바인딩
            UpdateAllGridComboBoxes();
            
            _isInitializing = false;
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

        private void DatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
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
                    
                    System.Diagnostics.Debug.WriteLine($"✅ 그룹명 '~{queryName}~'에 대한 {detailQueries.Count}개의 상세 쿼리가 자동 바인딩되었습니다.");
                }
                else
                {
                    // 상세 쿼리가 없으면 동적 그리드를 20개 빈 상태로 재생성
                    GenerateDynamicGridsWithoutBinding(20);
                    
                    System.Diagnostics.Debug.WriteLine($"⚠️ 그룹명 '~{queryName}~'에 대한 상세 쿼리(순번 1 이상)가 없습니다. 빈 그리드 20개를 생성했습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"상세 쿼리 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 비즈명으로 상세 쿼리(순번 1 이상)를 로드하여 동적 그리드에 자동 바인딩
        /// </summary>
        private void LoadDetailQueriesByBizName(string bizName)
        {
            if (_database == null) return;

            try
            {
                var allQueries = _database.GetAllQueries();

                // 선택된 비즈명과 일치하고 순번이 1 이상인 쿼리만 필터링
                var detailQueries = allQueries
                    .Where(q => q.QueryType == "정보 조회" && 
                                q.BizName == bizName && 
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
                    GenerateDynamicGridsWithoutBinding(20);
                    
                    System.Diagnostics.Debug.WriteLine($"비즈명 '~{bizName}~'에 대한 상세 쿼리(순번 1 이상)가 없습니다. 빈 그리드 20개를 생성했습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"상세 쿼리 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 쿼리 목록과 함께 동적 그리드 생성 및 자동 바인딩
        /// </summary>
        private void GenerateDynamicGridsWithQueries(List<QueryItem> queries)
        {
            _isInitializing = true;

            DynamicGridsContainer.Children.Clear();
            DynamicGridsContainer.RowDefinitions.Clear();
            _dynamicGrids.Clear();

            // 최대 20개까지만 처리
            int count = Math.Min(queries.Count, 20);

            // 🔥 1열 레이아웃으로 변경 - 항상 20개 그리드용 행 생성
            int rowCount = 20;

            // 행 정의 추가
            for (int i = 0; i < rowCount; i++)
            {
                DynamicGridsContainer.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(350, GridUnitType.Pixel)
                });
            }

            // 20개 그리드 모두 생성
            for (int i = 0; i < 20; i++)
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

                // 🔥 1열 레이아웃: Grid.Row만 설정, Grid.Column은 항상 0
                Grid.SetRow(border, i);
                Grid.SetColumn(border, 0);

                DynamicGridsContainer.Children.Add(border);
            }

            // 모든 정보 조회 쿼리를 콜박스에 바인딩 (항상 활성화 - 사용자가 변경 가능)
            // 🔥 비즈명이 있는 쿼리만 필터링
            var queriesWithBizName = _infoQueries.Where(q => !string.IsNullOrWhiteSpace(q.BizName)).ToList();
            
            foreach (var gridInfo in _dynamicGrids)
            {
                gridInfo.QueryComboBox.ItemsSource = queriesWithBizName;
                gridInfo.QueryComboBox.IsEnabled = true;  // 항상 활성화
                gridInfo.ClearButton.IsEnabled = true;     // 항상 활성화
            }

            // 매칭되는 쿼리를 순서대로 자동 선택 (최대 20개까지)
            // 사용자는 언제든지 변경 가능
            for (int i = 0; i < count && i < _dynamicGrids.Count; i++)
            {
                var gridInfo = _dynamicGrids[i];
                var query = queries[i];

                // 🔥 queriesWithBizName에서 같은 쿼리를 찾아서 선택
                // QueryName과 BizName, OrderNumber로 매칭
                var matchingQuery = queriesWithBizName.FirstOrDefault(q => 
                    q.QueryName == query.QueryName && 
                    q.BizName == query.BizName && 
                    q.OrderNumber == query.OrderNumber);

                if (matchingQuery != null)
                {
                    gridInfo.QueryComboBox.SelectedItem = matchingQuery;
                    System.Diagnostics.Debug.WriteLine($"✅ 그리드 {gridInfo.Index}: '{query.QueryBizName}' (순번 {query.OrderNumber}) 자동 선택됨");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ 그리드 {gridInfo.Index}: '{query.QueryName}' (비즈명: {query.BizName}, 순번: {query.OrderNumber}) 쿼리를 찾을 수 없습니다.");
                }
            }
            
            _isInitializing = false;
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

                // 계획정보 쿼리 콤보박스: 플레이스홀더 + 순번 0인 쿼리
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

        private void SaveSelectedQueries()
        {
            if (_sharedData == null || _isInitializing) return;

            _sharedData.Settings.GmesPlanQueryName = (QuerySelectComboBox.SelectedItem as QueryItem)?.QueryName ?? "";
            SaveDynamicGridQueries();
        }

        private void LoadSelectedQueries()
        {
            if (_sharedData == null || _infoQueries.Count == 0) return;

            // 항상 플레이스홀더(인덱스 0)로 시작
            QuerySelectComboBox.SelectedIndex = 0;

            // 동적 그리드 쿼리 복원 제거 - 사용자가 직접 선택
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
                var window = new QueryTextEditWindow(query.Query, isReadOnly: true)
                {
                    Title = $"쿼리 보기 - {query.QueryName}",
                    Owner = Window.GetWindow(this),
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                window.ShowDialog();
            }
            else
            {
                MessageBox.Show("먼저 기준정보 쿼리를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void GenerateDynamicGrids(int count)
        {
            _isInitializing = true;
            
            DynamicGridsContainer.Children.Clear();
            DynamicGridsContainer.RowDefinitions.Clear();
            _dynamicGrids.Clear();

            // 🔥 1열 레이아웃으로 변경 - 행 개수 = 그리드 개수
            int rowCount = count;
            
            // 행 정의 추가
            for (int i = 0; i < rowCount; i++)
            {
                DynamicGridsContainer.RowDefinitions.Add(new RowDefinition 
                { 
                    Height = new GridLength(350, GridUnitType.Pixel) 
                });
            }

            for (int i = 0; i < count; i++)
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

                // 🔥 1열 레이아웃: Grid.Row만 설정, Grid.Column은 항상 0
                Grid.SetRow(border, i);
                Grid.SetColumn(border, 0);

                DynamicGridsContainer.Children.Add(border);
            }

            UpdateAllGridComboBoxes();
            
            _isInitializing = false;
        }

        private ScrollViewer? FindParentScrollViewer(DependencyObject child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            
            while (parent != null)
            {
                if (parent is ScrollViewer scrollViewer)
                    return scrollViewer;
                
                parent = VisualTreeHelper.GetParent(parent);
            }
            
            return null;
        }

        private ScrollViewer? FindScrollViewer(DependencyObject child)
        {
            return FindParentScrollViewer(child);
        }

        private DynamicGridInfo CreateDynamicGrid(int index)
        {
            var queryComboBox = new ComboBox
            {
                Width = 180,
                Height = 28,
                DisplayMemberPath = "BizName", // 🔥 QueryName → BizName으로 변경
                Margin = new Thickness(10, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
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
                IsReadOnly = false,  // 셀 복사를 위해 편집 가능하도록 변경
                CanUserAddRows = false,  // 빈 행 생성 방지
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                HeadersVisibility = DataGridHeadersVisibility.All,
                FontSize = 10,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,  // 🔥 가로 스크롤 활성화
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                ClipboardCopyMode = DataGridClipboardCopyMode.ExcludeHeader,  // 헤더 제외하고 복사
                SelectionMode = DataGridSelectionMode.Extended,  // 다중 선택 가능
                SelectionUnit = DataGridSelectionUnit.Cell  // 셀 단위 선택
            };
            dataGrid.AutoGeneratingColumn += DataGrid_AutoGeneratingColumn;
            dataGrid.LoadingRow += DataGrid_LoadingRow; // CHK 컬럼 체크를 위한 이벤트
            
            // BeginningEdit 이벤트 추가 - 실제 편집은 막고 복사만 허용
            dataGrid.BeginningEdit += (s, e) => e.Cancel = true;

            // 헤더 스타일을 명시적으로 생성 (파란색 계열로 통일)
            var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BackgroundProperty, 
                new SolidColorBrush(Color.FromRgb(0, 120, 215)))); // #FF0078D7
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.ForegroundProperty, 
                Brushes.White));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, 
                FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HorizontalContentAlignmentProperty, 
                HorizontalAlignment.Center));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty, 
                10.0));
            
            dataGrid.ColumnHeaderStyle = headerStyle;

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
                    var window = new QueryTextEditWindow(query.Query, isReadOnly: true)
                    {
                        Title = $"쿼리 보기 - {query.QueryName}",
                        Owner = Window.GetWindow(this),
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
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
            // 🔥 비즈명이 있는 쿼리만 필터링
            var queriesWithBizName = _infoQueries.Where(q => !string.IsNullOrWhiteSpace(q.BizName)).ToList();
            
            foreach (var gridInfo in _dynamicGrids)
            {
                // 비즈명이 있는 정보 조회 쿼리를 콜박스에 바인딩
                gridInfo.QueryComboBox.ItemsSource = queriesWithBizName;
                
                // 콤보박스 활성화 및 취소 버튼 활성화
                gridInfo.QueryComboBox.IsEnabled = true;
                gridInfo.ClearButton.IsEnabled = true;
            }

            // 복원 기능 제거 - 사용자가 직접 선택
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

        private void SaveDynamicGridQueries()
        {
            if (_sharedData == null || _isInitializing) return;

            var selectedQueries = new List<string>();
            foreach (var gridInfo in _dynamicGrids)
            {
                var queryName = (gridInfo.QueryComboBox.SelectedItem as QueryItem)?.QueryName ?? "";
                selectedQueries.Add(queryName);
            }

            // 최대 20개까지 저장
            for (int i = 0; i < selectedQueries.Count && i < 20; i++)
            {
                switch (i)
                {
                    case 0: _sharedData.Settings.GmesGrid1QueryName = selectedQueries[i]; break;
                    case 1: _sharedData.Settings.GmesGrid2QueryName = selectedQueries[i]; break;
                    case 2: _sharedData.Settings.GmesGrid3QueryName = selectedQueries[i]; break;
                    case 3: _sharedData.Settings.GmesGrid4QueryName = selectedQueries[i]; break;
                    case 4: _sharedData.Settings.GmesGrid5QueryName = selectedQueries[i]; break;
                    case 5: _sharedData.Settings.GmesGrid6QueryName = selectedQueries[i]; break;
                    case 6: _sharedData.Settings.GmesGrid7QueryName = selectedQueries[i]; break;
                    case 7: _sharedData.Settings.GmesGrid8QueryName = selectedQueries[i]; break;
                    case 8: _sharedData.Settings.GmesGrid9QueryName = selectedQueries[i]; break;
                    case 9: _sharedData.Settings.GmesGrid10QueryName = selectedQueries[i]; break;
                    case 10: _sharedData.Settings.GmesGrid11QueryName = selectedQueries[i]; break;
                    case 11: _sharedData.Settings.GmesGrid12QueryName = selectedQueries[i]; break;
                    case 12: _sharedData.Settings.GmesGrid13QueryName = selectedQueries[i]; break;
                    case 13: _sharedData.Settings.GmesGrid14QueryName = selectedQueries[i]; break;
                    case 14: _sharedData.Settings.GmesGrid15QueryName = selectedQueries[i]; break;
                    case 15: _sharedData.Settings.GmesGrid16QueryName = selectedQueries[i]; break;
                    case 16: _sharedData.Settings.GmesGrid17QueryName = selectedQueries[i]; break;
                    case 17: _sharedData.Settings.GmesGrid18QueryName = selectedQueries[i]; break;
                    case 18: _sharedData.Settings.GmesGrid19QueryName = selectedQueries[i]; break;
                    case 19: _sharedData.Settings.GmesGrid20QueryName = selectedQueries[i]; break;
                }
            }

            _sharedData.SaveSettingsCallback?.Invoke();
        }

        private void LoadDynamicGridQueries()
        {
            if (_sharedData == null || _infoQueries.Count == 0) return;

            var savedQueries = new List<string>
            {
                _sharedData.Settings.GmesGrid1QueryName,
                _sharedData.Settings.GmesGrid2QueryName,
                _sharedData.Settings.GmesGrid3QueryName,
                _sharedData.Settings.GmesGrid4QueryName,
                _sharedData.Settings.GmesGrid5QueryName,
                _sharedData.Settings.GmesGrid6QueryName,
                _sharedData.Settings.GmesGrid7QueryName,
                _sharedData.Settings.GmesGrid8QueryName,
                _sharedData.Settings.GmesGrid9QueryName,
                _sharedData.Settings.GmesGrid10QueryName,
                _sharedData.Settings.GmesGrid11QueryName,
                _sharedData.Settings.GmesGrid12QueryName,
                _sharedData.Settings.GmesGrid13QueryName,
                _sharedData.Settings.GmesGrid14QueryName,
                _sharedData.Settings.GmesGrid15QueryName,
                _sharedData.Settings.GmesGrid16QueryName,
                _sharedData.Settings.GmesGrid17QueryName,
                _sharedData.Settings.GmesGrid18QueryName,
                _sharedData.Settings.GmesGrid19QueryName,
                _sharedData.Settings.GmesGrid20QueryName
            };

            for (int i = 0; i < _dynamicGrids.Count && i < savedQueries.Count; i++)
            {
                if (!string.IsNullOrEmpty(savedQueries[i]))
                {
                    var query = _infoQueries.FirstOrDefault(q => q.QueryName == savedQueries[i]);
                    if (query != null)
                    {
                        _dynamicGrids[i].QueryComboBox.SelectedItem = query;
                    }
                }
            }
        }

        private async void ExecuteAllGridsButton_Click(object sender, RoutedEventArgs e)
        {
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

                //MessageBox.Show($"전체 조회 완료: {tasks.Count}개 그리드", "완료",
                //    MessageBoxButton.OK, MessageBoxImage.Information);
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
        /// 전체 조회 시 시간 측정과 함께 쿼리를 실행합니다.
        /// </summary>
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

            try
            {
                string connectionString;

                // 🔥 1순위: ConnectionInfoId가 있는 경우 - 접속 정보 사용
                if (queryItem.ConnectionInfoId.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"=== ConnectionInfo 사용 ===");
                    System.Diagnostics.Debug.WriteLine($"ConnectionInfoId: {queryItem.ConnectionInfoId.Value}");
                    
                    // ConnectionInfo 조회
                    var connectionInfoService = new Services.ConnectionInfoService(_sharedData.Settings.DatabasePath);
                    var allConnections = connectionInfoService.GetAll();
                    var connectionInfo = allConnections.FirstOrDefault(c => c.Id == queryItem.ConnectionInfoId.Value);
                    
                    if (connectionInfo == null)
                    {
                        MessageBox.Show($"접속 정보 ID {queryItem.ConnectionInfoId.Value}를 찾을 수 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // TNS Entry 찾기
                    var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(connectionInfo.TNS, StringComparison.OrdinalIgnoreCase));
                    
                    if (selectedTns == null)
                    {
                        MessageBox.Show($"TNS '{connectionInfo.TNS}'를 찾을 수 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    connectionString = selectedTns.GetConnectionString();
                    
                    // ConnectionInfo의 UserId, Password 사용
                    queryItem.UserId = connectionInfo.UserId;
                    queryItem.Password = connectionInfo.Password;
                    
                    System.Diagnostics.Debug.WriteLine($"✅ ConnectionInfo 사용: {connectionInfo.Name} (TNS: {connectionInfo.TNS})");
                }
                // 🔥 2순위: Host/Port/ServiceName이 직접 입력된 경우
                else if (!string.IsNullOrWhiteSpace(queryItem.Host) &&
                    !string.IsNullOrWhiteSpace(queryItem.Port) &&
                    !string.IsNullOrWhiteSpace(queryItem.ServiceName))
                {
                    connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={queryItem.Host})(PORT={queryItem.Port}))(CONNECT_DATA=(SERVICE_NAME={queryItem.ServiceName})));";
                    System.Diagnostics.Debug.WriteLine($"✅ 직접 입력 정보 사용: {queryItem.Host}:{queryItem.Port}/{queryItem.ServiceName}");
                }
                // 🔥 3순위: TNS 이름으로 검색
                else if (!string.IsNullOrWhiteSpace(queryItem.TnsName))
                {
                    System.Diagnostics.Debug.WriteLine("=== TNS 연결 시도 ===");
                    System.Diagnostics.Debug.WriteLine($"쿼리의 TNS 이름: '{queryItem.TnsName}'");
                    
                    var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(queryItem.TnsName, StringComparison.OrdinalIgnoreCase));

                    if (selectedTns == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ TNS '{queryItem.TnsName}'를 찾을 수 없습니다!");
                        MessageBox.Show($"TNS '{queryItem.TnsName}'를 찾을 수 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"✅ TNS '{selectedTns.Name}' 찾음");
                    connectionString = selectedTns.GetConnectionString();
                }
                else
                {
                    MessageBox.Show("연결 정보가 없습니다.\n쿼리에 TNS 또는 접속 정보를 설정해주세요.", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string processedQuery = ReplaceQueryParameters(queryItem.Query);

                var result = await OracleDatabase.ExecuteQueryAsync(
                    connectionString,
                    queryItem.UserId,
                    queryItem.Password,
                    processedQuery);

                // 🔥 ItemsSource와 Columns를 모두 초기화 후 바인딩
                targetGrid.ItemsSource = null;
                targetGrid.Columns.Clear();
                targetGrid.ItemsSource = result.DefaultView;
                
                // 데이터 바인딩 후 폰트 크기 적용
                ApplyFontSizeToGrid(targetGrid);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 실행 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                string connectionString;

                // 🔥 1순위: ConnectionInfoId가 있는 경우 - 접속 정보 사용
                if (queryItem.ConnectionInfoId.HasValue)
                {
                    // ConnectionInfo 조회
                    var connectionInfoService = new Services.ConnectionInfoService(_sharedData.Settings.DatabasePath);
                    var allConnections = connectionInfoService.GetAll();
                    var connectionInfo = allConnections.FirstOrDefault(c => c.Id == queryItem.ConnectionInfoId.Value);
                    
                    if (connectionInfo == null)
                    {
                        throw new Exception($"접속 정보 ID {queryItem.ConnectionInfoId.Value}를 찾을 수 없습니다.");
                    }
                    
                    // TNS Entry 찾기
                    var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(connectionInfo.TNS, StringComparison.OrdinalIgnoreCase));
                    
                    if (selectedTns == null)
                    {
                        throw new Exception($"TNS '{connectionInfo.TNS}'를 찾을 수 없습니다.");
                    }
                    
                    connectionString = selectedTns.GetConnectionString();
                    
                    // ConnectionInfo의 UserId, Password 사용
                    queryItem.UserId = connectionInfo.UserId;
                    queryItem.Password = connectionInfo.Password;
                }
                // 🔥 2순위: Host/Port/ServiceName이 직접 입력된 경우
                else if (!string.IsNullOrWhiteSpace(queryItem.Host) &&
                    !string.IsNullOrWhiteSpace(queryItem.Port) &&
                    !string.IsNullOrWhiteSpace(queryItem.ServiceName))
                {
                    connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={queryItem.Host})(PORT={queryItem.Port}))(CONNECT_DATA=(SERVICE_NAME={queryItem.ServiceName})));";
                }
                // 🔥 3순위: TNS 이름으로 검색
                else if (!string.IsNullOrWhiteSpace(queryItem.TnsName))
                {
                    var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(queryItem.TnsName, StringComparison.OrdinalIgnoreCase));

                    if (selectedTns == null)
                    {
                        throw new Exception($"TNS '{queryItem.TnsName}'를 찾을 수 없습니다.");
                    }

                    connectionString = selectedTns.GetConnectionString();
                }
                else
                {
                    throw new Exception("연결 정보가 없습니다. 쿼리에 TNS 또는 접속 정보를 설정해주세요.");
                }

                string processedQuery = ReplaceQueryParametersWithRowData(queryItem.Query, selectedRow);

                var result = await OracleDatabase.ExecuteQueryAsync(
                    connectionString,
                    queryItem.UserId,
                    queryItem.Password,
                    processedQuery);

                // 🔥 ItemsSource와 Columns를 모두 초기화 후 바인딩
                targetGrid.ItemsSource = null;
                targetGrid.Columns.Clear();
                targetGrid.ItemsSource = result.DefaultView;
                
                // 데이터 바인딩 후 폰트 크기 적용
                ApplyFontSizeToGrid(targetGrid);
            }
            catch (Exception ex)
            {
                throw new Exception($"[{queryItem.QueryName}] {ex.Message}", ex);
            }
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

                    string parameterName = $"@{columnName}";
                    if (result.Contains(parameterName))
                    {
                        result = result.Replace(parameterName, $"'{columnValue}'");
                    }

                    string parameterNameNoUnderscore = $"@{columnName.Replace("_", "")}";
                    if (result.Contains(parameterNameNoUnderscore))
                    {
                        result = result.Replace(parameterNameNoUnderscore, $"'{columnValue}'");
                    }
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
            System.Diagnostics.Debug.WriteLine("=== ReplaceQueryParameters 실행 ===");
            System.Diagnostics.Debug.WriteLine($"Factory: '{factory}'");
            System.Diagnostics.Debug.WriteLine($"Org: '{org}'");
            System.Diagnostics.Debug.WriteLine($"Facility: '{facility}'");
            System.Diagnostics.Debug.WriteLine($"WipLineId: '{wipLineId}'");
            System.Diagnostics.Debug.WriteLine($"EquipLineId: '{equipLineId}'");
            System.Diagnostics.Debug.WriteLine("=====================================");

            result = result.Replace("@REPRESENTATIVE_FACTORY_CODE", $"'{factory}'");
            result = result.Replace("@ORGANIZATION_ID", $"'{org}'");
            result = result.Replace("@PRODUCTION_YMD_START", $"'{DateFromPicker.SelectedDate?.ToString("yyyyMMdd") ?? ""}'");
            result = result.Replace("@PRODUCTION_YMD_END", $"'{DateToPicker.SelectedDate?.ToString("yyyyMMdd") ?? ""}'");
            result = result.Replace("@WIP_LINE_ID", $"'{wipLineId}'");
            result = result.Replace("@LINE_ID", $"'{equipLineId}'");
            result = result.Replace("@FACILITY_CODE", $"'{facility}'");
            result = result.Replace("@WORK_ORDER_ID", $"'{WorkOrderTextBox.Text}'");
            result = result.Replace("@WORK_ORDER_NAME", $"'{WorkOrderNameTextBox.Text}'");
            result = result.Replace("@PRODUCT_SPECIFICATION_ID", $"'{ModelSuffixTextBox.Text}'");
            result = result.Replace("@LOT_ID", $"'{LotIdTextBox.Text}'");
            result = result.Replace("@EQUIPMENT_ID", $"'{EquipmentIdTextBox.Text}'");
            
            // 🔥 PARAM1~PARAM4 치환
            result = result.Replace("@PARAM1", $"'{Param1TextBox.Text}'");
            result = result.Replace("@PARAM2", $"'{Param2TextBox.Text}'");
            result = result.Replace("@PARAM3", $"'{Param3TextBox.Text}'");
            result = result.Replace("@PARAM4", $"'{Param4TextBox.Text}'");

            return result;
        }

        private void DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string header = e.Column.Header as string ?? e.PropertyName;
            
            if (!string.IsNullOrEmpty(header))
            {
                e.Column.Header = header.Replace("_", "__");
            }
            
            // 🔥 CLOB 타입 컬럼을 TextBox 형태로 표시
            if (e.PropertyType == typeof(string) && e.PropertyDescriptor != null)
            {
                // DataView에서 실제 DataColumn 정보 가져오기
                var dataGrid = sender as DataGrid;
                if (dataGrid?.ItemsSource is DataView dataView)
                {
                    var columnName = e.PropertyName;
                    if (dataView.Table.Columns.Contains(columnName))
                    {
                        var dataColumn = dataView.Table.Columns[columnName];
                        
                        // 🔥 CLOB 타입이거나 긴 텍스트 컬럼 감지
                        bool isLongText = dataColumn.DataType == typeof(string) && 
                                         (dataColumn.MaxLength == -1 || dataColumn.MaxLength > 500);
                        
                        // 🔥 또는 데이터를 확인해서 긴 텍스트가 있는지 체크
                        if (!isLongText && dataView.Table.Rows.Count > 0)
                        {
                            var sampleValue = dataView.Table.Rows[0][columnName]?.ToString() ?? "";
                            isLongText = sampleValue.Length > 100 || sampleValue.Contains('\n');
                        }
                        
                        if (isLongText)
                        {
                            // 🔥 기존 자동 생성된 컬럼 취소
                            e.Cancel = true;
                            
                            // 🔥 TextBox 템플릿이 있는 새 컬럼 생성
                            var templateColumn = new DataGridTemplateColumn
                            {
                                Header = header.Replace("_", "__"),
                                Width = new DataGridLength(200), // 기본 너비
                                CellTemplate = CreateClobCellTemplate(columnName)
                            };
                            
                            dataGrid.Columns.Add(templateColumn);
                            
                            System.Diagnostics.Debug.WriteLine($"✅ CLOB 컬럼 감지: {columnName} - TextBox 템플릿 적용");
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
            factory.SetValue(TextBox.MaxHeightProperty, 100.0);
            factory.SetValue(TextBox.BorderThicknessProperty, new Thickness(0));
            factory.SetValue(TextBox.BackgroundProperty, Brushes.Transparent);
            factory.SetValue(TextBox.PaddingProperty, new Thickness(5));
            
            var dataTemplate = new DataTemplate
            {
                VisualTree = factory
            };
            
            return dataTemplate;
        }

        /// <summary>
        /// DataGrid 행 로드 시 CHK 컬럼 값에 따라 배경색 설정
        /// </summary>
        private void DataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is DataRowView rowView)
            {
                var row = rowView.Row;
                
                // CHK 컬럼이 존재하는지 확인
                if (row.Table.Columns.Contains("CHK"))
                {
                    var chkValue = row["CHK"]?.ToString()?.Trim();
                    
                    // CHK 값이 'E'면 빨간 배경
                    if (chkValue == "E")
                    {
                        e.Row.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200)); // 연한 빨강
                        e.Row.Foreground = new SolidColorBrush(Color.FromRgb(139, 0, 0)); // 진한 빨강 텍스트
                    }
                    else
                    {
                        // 🔥 CHK 값이 'E'가 아니면 기본 배경색으로 초기화
                        e.Row.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
                        e.Row.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
                    }
                }
                else
                {
                    // 🔥 CHK 컬럼이 없으면 기본 배경색으로 초기화
                    e.Row.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
                    e.Row.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
                }
            }
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

            // 헤더 폰트 크기 적용
            var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BackgroundProperty, 
                new SolidColorBrush(Color.FromRgb(0, 120, 215)))); // #FF0078D7
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.ForegroundProperty, 
                Brushes.White));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, 
                FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HorizontalContentAlignmentProperty, 
                HorizontalAlignment.Center));
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
    }
}
