using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FACTOVA_QueryHelper.Database;
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
            
            FactoryTextBox.LostFocus += InputField_LostFocus;
            OrgTextBox.LostFocus += InputField_LostFocus;
            DateFromPicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            DateToPicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            WipLineIdTextBox.LostFocus += InputField_LostFocus;
            EquipLineIdTextBox.LostFocus += InputField_LostFocus;
            FacilityTextBox.LostFocus += InputField_LostFocus;
            WorkOrderTextBox.LostFocus += InputField_LostFocus;
            WorkOrderNameTextBox.LostFocus += InputField_LostFocus;
            ModelSuffixTextBox.LostFocus += InputField_LostFocus;
            
            QuerySelectComboBox.SelectionChanged += QueryComboBox_SelectionChanged;
            PlanInfoDataGrid.AutoGeneratingColumn += DataGrid_AutoGeneratingColumn;
            PlanInfoDataGrid.LoadingRow += DataGrid_LoadingRow; // CHK 컬럼 체크
        }

        public void Initialize(SharedDataContext sharedData)
        {
            _isInitializing = true;
            
            _sharedData = sharedData;
            _database = new QueryDatabase(sharedData.Settings.DatabasePath);
            
            LoadInputValues();
            LoadInfoQueries();
            
            // 저장된 그리드 개수 복원 (기본값: 6)
            int gridCount = _sharedData.Settings.GmesGridCount;
            if (gridCount < 1 || gridCount > 20)
                gridCount = 6;
            
            GridCountTextBox.Text = gridCount.ToString();
            GenerateDynamicGrids(gridCount);
            
            // 폰트 크기 적용
            ApplyFontSize();
            UpdateFontSizeDisplay();
            
            _isInitializing = false;
        }

        private void LoadInputValues()
        {
            if (_sharedData == null) return;

            FactoryTextBox.Text = _sharedData.Settings.GmesFactory;
            OrgTextBox.Text = _sharedData.Settings.GmesOrg;
            DateFromPicker.SelectedDate = _sharedData.Settings.GmesDateFrom ?? DateTime.Today;
            DateToPicker.SelectedDate = _sharedData.Settings.GmesDateTo ?? DateTime.Today;
            WipLineIdTextBox.Text = _sharedData.Settings.GmesWipLineId;
            EquipLineIdTextBox.Text = _sharedData.Settings.GmesEquipLineId;
            FacilityTextBox.Text = _sharedData.Settings.GmesFacility;
            WorkOrderTextBox.Text = _sharedData.Settings.GmesWorkOrder;
            WorkOrderNameTextBox.Text = _sharedData.Settings.GmesWorkOrderName;
            ModelSuffixTextBox.Text = _sharedData.Settings.GmesModelSuffix;
        }

        private void SaveInputValues()
        {
            if (_sharedData == null || _isInitializing) return;

            _sharedData.Settings.GmesFactory = FactoryTextBox.Text;
            _sharedData.Settings.GmesOrg = OrgTextBox.Text;
            _sharedData.Settings.GmesDateFrom = DateFromPicker.SelectedDate;
            _sharedData.Settings.GmesDateTo = DateToPicker.SelectedDate;
            _sharedData.Settings.GmesWipLineId = WipLineIdTextBox.Text;
            _sharedData.Settings.GmesEquipLineId = EquipLineIdTextBox.Text;
            _sharedData.Settings.GmesFacility = FacilityTextBox.Text;
            _sharedData.Settings.GmesWorkOrder = WorkOrderTextBox.Text;
            _sharedData.Settings.GmesWorkOrderName = WorkOrderNameTextBox.Text;
            _sharedData.Settings.GmesModelSuffix = ModelSuffixTextBox.Text;

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
            SaveSelectedQueries();
        }

        private void LoadInfoQueries()
        {
            if (_database == null) return;

            try
            {
                _isInitializing = true;
                
                var allQueries = _database.GetAllQueries();
                _infoQueries = allQueries.Where(q => q.QueryType == "정보 조회").ToList();

                QuerySelectComboBox.ItemsSource = _infoQueries;

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

            if (!string.IsNullOrEmpty(_sharedData.Settings.GmesPlanQueryName))
            {
                var query = _infoQueries.FirstOrDefault(q => q.QueryName == _sharedData.Settings.GmesPlanQueryName);
                if (query != null)
                    QuerySelectComboBox.SelectedItem = query;
            }

            LoadDynamicGridQueries();
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

        private void GenerateGridsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(GridCountTextBox.Text, out int gridCount) || gridCount < 1 || gridCount > 20)
            {
                MessageBox.Show("1~20 사이의 숫자를 입력하세요.", "입력 오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 그리드 개수 저장
            if (_sharedData != null)
            {
                _sharedData.Settings.GmesGridCount = gridCount;
                _sharedData.SaveSettingsCallback?.Invoke();
            }

            GenerateDynamicGrids(gridCount);
        }

        private void GenerateDynamicGrids(int count)
        {
            _isInitializing = true;
            
            DynamicGridsContainer.Children.Clear();
            DynamicGridsContainer.RowDefinitions.Clear();
            _dynamicGrids.Clear();

            // 필요한 행 개수 계산 (2열 레이아웃이므로 올림 나누기)
            int rowCount = (int)Math.Ceiling(count / 2.0);
            
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

                // Grid.Row와 Grid.Column 설정
                int row = i / 2;  // 0-based row index
                int col = i % 2;  // 0 or 1
                
                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);

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
                DisplayMemberPath = "QueryName",
                Margin = new Thickness(10, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            queryComboBox.SelectionChanged += (s, e) => SaveDynamicGridQueries();

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
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                HeadersVisibility = DataGridHeadersVisibility.All,
                FontSize = 10
            };
            dataGrid.AutoGeneratingColumn += DataGrid_AutoGeneratingColumn;
            dataGrid.LoadingRow += DataGrid_LoadingRow; // CHK 컬럼 체크를 위한 이벤트

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

            var executeButton = new Button
            {
                Content = "▶",
                Width = 30,
                Height = 28,
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.Bold
            };
            executeButton.Click += async (s, e) => await ExecuteDynamicGridQuery(gridInfo);

            headerPanel.Children.Add(titleBlock);
            headerPanel.Children.Add(gridInfo.QueryComboBox);
            headerPanel.Children.Add(gridInfo.ClearButton);
            headerPanel.Children.Add(executeButton);
            headerPanel.Children.Add(gridInfo.ResultInfoTextBlock); // 결과 정보 추가

            Grid.SetRow(headerPanel, 0);
            Grid.SetRow(gridInfo.DataGrid, 1);

            grid.Children.Add(headerPanel);
            grid.Children.Add(gridInfo.DataGrid);

            return grid;
        }

        private void UpdateAllGridComboBoxes()
        {
            foreach (var gridInfo in _dynamicGrids)
            {
                gridInfo.QueryComboBox.ItemsSource = _infoQueries;
            }

            LoadDynamicGridQueries();
        }

        private async System.Threading.Tasks.Task ExecuteDynamicGridQuery(DynamicGridInfo gridInfo)
        {
            if (gridInfo.QueryComboBox.SelectedItem is not QueryItem query)
            {
                MessageBox.Show("조회할 쿼리를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (PlanInfoDataGrid.SelectedItem == null)
            {
                MessageBox.Show("계획정보에서 행을 먼저 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selectedRow = (DataRowView)PlanInfoDataGrid.SelectedItem;
            
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
            if (PlanInfoDataGrid.SelectedItem == null)
            {
                MessageBox.Show("계획정보에서 행을 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                ExecuteAllGridsButton.IsEnabled = false;
                ExecuteAllGridsButton.Content = "조회 중...";

                var selectedRow = (DataRowView)PlanInfoDataGrid.SelectedItem;

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

                MessageBox.Show($"전체 조회 완료: {tasks.Count}개 그리드", "완료",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (QuerySelectComboBox.SelectedItem is not QueryItem selectedQuery)
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

                if (!string.IsNullOrWhiteSpace(queryItem.Host) &&
                    !string.IsNullOrWhiteSpace(queryItem.Port) &&
                    !string.IsNullOrWhiteSpace(queryItem.ServiceName))
                {
                    connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={queryItem.Host})(PORT={queryItem.Port}))(CONNECT_DATA=(SERVICE_NAME={queryItem.ServiceName})));";
                }
                else
                {
                    var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(queryItem.TnsName, StringComparison.OrdinalIgnoreCase));

                    if (selectedTns == null)
                    {
                        MessageBox.Show($"TNS '{queryItem.TnsName}'를 찾을 수 없습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    connectionString = selectedTns.ConnectionString;
                }

                string processedQuery = ReplaceQueryParameters(queryItem.Query);

                var result = await OracleDatabase.ExecuteQueryAsync(
                    connectionString,
                    queryItem.UserId,
                    queryItem.Password,
                    processedQuery);

                targetGrid.ItemsSource = result.DefaultView;
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

                if (!string.IsNullOrWhiteSpace(queryItem.Host) &&
                    !string.IsNullOrWhiteSpace(queryItem.Port) &&
                    !string.IsNullOrWhiteSpace(queryItem.ServiceName))
                {
                    connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={queryItem.Host})(PORT={queryItem.Port}))(CONNECT_DATA=(SERVICE_NAME={queryItem.ServiceName})));";
                }
                else
                {
                    var selectedTns = _sharedData.TnsEntries.FirstOrDefault(t =>
                        t.Name.Equals(queryItem.TnsName, StringComparison.OrdinalIgnoreCase));

                    if (selectedTns == null)
                    {
                        throw new Exception($"TNS '{queryItem.TnsName}'를 찾을 수 없습니다.");
                    }

                    connectionString = selectedTns.ConnectionString;
                }

                string processedQuery = ReplaceQueryParametersWithRowData(queryItem.Query, selectedRow);

                var result = await OracleDatabase.ExecuteQueryAsync(
                    connectionString,
                    queryItem.UserId,
                    queryItem.Password,
                    processedQuery);

                targetGrid.ItemsSource = result.DefaultView;
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

            result = result.Replace("@REPRESENTATIVE_FACTORY_CODE", $"'{FactoryTextBox.Text}'");
            result = result.Replace("@ORGANIZATION_ID", $"'{OrgTextBox.Text}'");
            result = result.Replace("@PRODUCTION_YMD_START", $"'{DateFromPicker.SelectedDate?.ToString("yyyyMMdd") ?? ""}'");
            result = result.Replace("@PRODUCTION_YMD_END", $"'{DateToPicker.SelectedDate?.ToString("yyyyMMdd") ?? ""}'");
            result = result.Replace("@WIP_LINE_ID", $"'{WipLineIdTextBox.Text}'");
            result = result.Replace("@LINE_ID", $"'{EquipLineIdTextBox.Text}'");
            result = result.Replace("@FACILITY_CODE", $"'{FacilityTextBox.Text}'");
            result = result.Replace("@WORK_ORDER_ID", $"'{WorkOrderTextBox.Text}'");
            result = result.Replace("@WORK_ORDER_NAME", $"'{WorkOrderNameTextBox.Text}'");
            result = result.Replace("@PRODUCT_SPECIFICATION_ID", $"'{ModelSuffixTextBox.Text}'");

            return result;
        }

        private void DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header is string header)
            {
                e.Column.Header = header.Replace("_", "__");
            }
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
                    
                    // CHK 값이 'E'이면 빨간 배경
                    if (chkValue == "E")
                    {
                        e.Row.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200)); // 연한 빨강
                        e.Row.Foreground = new SolidColorBrush(Color.FromRgb(139, 0, 0)); // 진한 빨강 텍스트
                    }
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
            PlanInfoDataGrid.FontSize = fontSize;

            // 계획정보 DataGrid 헤더 폰트 크기도 업데이트
            var planHeaderStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            planHeaderStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BackgroundProperty, 
                new SolidColorBrush(Color.FromRgb(108, 117, 125)))); // #FF6C757D
            planHeaderStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.ForegroundProperty, 
                Brushes.White));
            planHeaderStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, 
                FontWeights.Bold));
            planHeaderStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HorizontalContentAlignmentProperty, 
                HorizontalAlignment.Center));
            planHeaderStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty, 
                (double)fontSize));
            
            PlanInfoDataGrid.ColumnHeaderStyle = planHeaderStyle;

            // 모든 동적 그리드에 폰트 크기 적용
            foreach (var gridInfo in _dynamicGrids)
            {
                gridInfo.DataGrid.FontSize = fontSize;
                
                // 헤더 폰트 크기도 업데이트
                var newHeaderStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
                newHeaderStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BackgroundProperty, 
                    new SolidColorBrush(Color.FromRgb(0, 120, 215))));
                newHeaderStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.ForegroundProperty, 
                    Brushes.White));
                newHeaderStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty, 
                    FontWeights.Bold));
                newHeaderStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HorizontalContentAlignmentProperty, 
                    HorizontalAlignment.Center));
                newHeaderStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty, 
                    (double)fontSize));
                
                gridInfo.DataGrid.ColumnHeaderStyle = newHeaderStyle;
            }
        }
    }
}
