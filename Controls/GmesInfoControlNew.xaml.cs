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
using OfficeOpenXml;
using System.IO;
using Microsoft.Win32;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// GmesInfoControlNew.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GmesInfoControlNewNew : UserControl
    {
        private SharedDataContext? _sharedData;
        private QueryDatabase? _database;
        private List<QueryItem> _infoQueries = new List<QueryItem>();
        private bool _isInitializing = false;
        private List<DynamicGridInfo> _dynamicGrids = new List<DynamicGridInfo>();
        
        // 🔥 파라미터 관리
        private System.Collections.ObjectModel.ObservableCollection<ParameterInfo>? _parameters;
        private ParameterInfo? _selectedParameter;
        
        // 🔥 OracleDbService 추가
        private OracleDbService? _dbService;

        private class DynamicGridInfo
        {
            public int Index { get; set; }
            public ComboBox QueryComboBox { get; set; } = null!;
            public DataGrid DataGrid { get; set; } = null!;
            public Button ClearButton { get; set; } = null!;
            public TextBlock ResultInfoTextBlock { get; set; } = null!;
        }

        public GmesInfoControlNewNew()
        {
            InitializeComponent();
            
            // 🔥 OracleDbService 초기화
            _dbService = new OracleDbService();
            
            QuerySelectComboBox.SelectionChanged += QueryComboBox_SelectionChanged;
            PlanInfoDataGrid.AutoGeneratingColumn += DataGrid_AutoGeneratingColumn;
            PlanInfoDataGrid.LoadingRow += DataGrid_LoadingRow;
            PlanInfoDataGrid.SelectionChanged += PlanInfoDataGrid_SelectionChanged;
            
            // 🔥 Ctrl+C 키보드 이벤트 핸들러 추가
            PlanInfoDataGrid.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    try
                    {
                        if (PlanInfoDataGrid.SelectedItem is ParameterInfo selectedParam)
                        {
                            Clipboard.SetText(selectedParam.Value ?? "");
                            e.Handled = true;
                            System.Diagnostics.Debug.WriteLine($"✅ 파라미터 값 복사 완료: {selectedParam.Value}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"복사 오류: {ex.Message}");
                    }
                }
            };
        }

        public void Initialize(SharedDataContext sharedData)
        {
            _isInitializing = true;
            
            _sharedData = sharedData;
            _database = new QueryDatabase(sharedData.Settings.DatabasePath);
            
            // 🔥 사업장 정보 로드
            LoadSiteInfos();
            
            // 🔥 파라미터 로드
            LoadParameters();
            
            LoadInfoQueries();
            
            // 그리드를 항상 20개로 고정 생성
            CreateDynamicGrids(20);
            
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

        #region 사업장 관리

        /// <summary>
        /// 사업장 정보를 로드합니다.
        /// </summary>
        private void LoadSiteInfos()
        {
            if (_database == null) return;

            try
            {
                var sites = _database.GetAllSites();
                
                SiteComboBox.ItemsSource = sites;

                if (sites.Count > 0)
                {
                    SiteComboBox.SelectedItem = sites[0];
                    
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

            if (_sharedData != null)
            {
                System.Diagnostics.Debug.WriteLine("=== 사업장 선택 변경 ===");
                
                _sharedData.Settings.GmesFactory = selectedSite.RepresentativeFactory;
                _sharedData.Settings.GmesOrg = selectedSite.Organization;
                _sharedData.Settings.GmesFacility = selectedSite.Facility;
                _sharedData.Settings.GmesWipLineId = selectedSite.WipLineId;
                _sharedData.Settings.GmesEquipLineId = selectedSite.EquipLineId;
                _sharedData.SaveSettingsCallback?.Invoke();
                
                System.Diagnostics.Debug.WriteLine($"사업장명: {selectedSite.SiteName}");
                System.Diagnostics.Debug.WriteLine($"Factory: {_sharedData.Settings.GmesFactory}");
                System.Diagnostics.Debug.WriteLine($"Org: {_sharedData.Settings.GmesOrg}");
                System.Diagnostics.Debug.WriteLine($"Facility: {_sharedData.Settings.GmesFacility}");
                System.Diagnostics.Debug.WriteLine("========================");
            }
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

        #endregion

        #region 기준정보 파라미터 관리

        /// <summary>
        /// 파라미터 목록 로드
        /// </summary>
        private void LoadParameters()
        {
            if (_database == null) return;

            try
            {
                var parameters = _database.GetAllParameters();
                _parameters = new System.Collections.ObjectModel.ObservableCollection<ParameterInfo>(parameters);
                PlanInfoDataGrid.ItemsSource = _parameters;
                
                ParameterStatusTextBlock.Text = $"{parameters.Count}개 파라미터 로드됨";
                ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파라미터 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ParameterStatusTextBlock.Text = "파라미터 로드 실패";
                ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        /// <summary>
        /// 파라미터 추가 버튼 클릭
        /// </summary>
        private void AddParameterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_parameters == null) return;

            var newParameter = new ParameterInfo
            {
                Id = 0,
                Parameter = "@NEW_PARAM",
                Description = "새 파라미터",
                Value = ""
            };

            _parameters.Add(newParameter);
            PlanInfoDataGrid.SelectedItem = newParameter;
            PlanInfoDataGrid.ScrollIntoView(newParameter);
            
            ParameterStatusTextBlock.Text = "새 파라미터가 추가되었습니다. 저장 버튼을 눌러주세요.";
            ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Blue);
        }

        /// <summary>
        /// 파라미터 삭제 버튼 클릭
        /// </summary>
        private void DeleteParameterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedParameter == null || _parameters == null)
            {
                MessageBox.Show("삭제할 파라미터를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"파라미터 '{_selectedParameter.Parameter}'을(를) 삭제하시겠습니까?",
                "삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (_selectedParameter.Id > 0)
                    {
                        _database?.DeleteParameter(_selectedParameter.Id);
                    }

                    _parameters.Remove(_selectedParameter);
                    _selectedParameter = null;
                    DeleteParameterButton.IsEnabled = false;

                    ParameterStatusTextBlock.Text = "파라미터가 삭제되었습니다.";
                    ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"파라미터 삭제 실패:\n{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 파라미터 저장 버튼 클릭
        /// </summary>
        private void SaveParametersButton_Click(object sender, RoutedEventArgs e)
        {
            if (_parameters == null || _database == null) return;

            try
            {
                int newCount = 0;
                int updateCount = 0;

                foreach (var param in _parameters)
                {
                    if (string.IsNullOrWhiteSpace(param.Parameter))
                    {
                        MessageBox.Show("파라미터 이름을 입력해주세요.", "입력 오류",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (param.Id == 0)
                    {
                        _database.AddParameter(param);
                        newCount++;
                    }
                    else
                    {
                        _database.UpdateParameter(param);
                        updateCount++;
                    }
                }

                MessageBox.Show($"저장 완료!\n\n신규: {newCount}개\n수정: {updateCount}개", "성공",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadParameters();

                ParameterStatusTextBlock.Text = $"저장 완료 (신규: {newCount}, 수정: {updateCount})";
                ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ParameterStatusTextBlock.Text = "저장 실패";
                ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        /// <summary>
        /// 색상 초기화 버튼 클릭
        /// </summary>
        private void ClearHighlightButton_Click(object sender, RoutedEventArgs e)
        {
            if (_parameters == null) return;

            try
            {
                foreach (var param in _parameters)
                {
                    param.IsHighlighted = false;
                }

                PlanInfoDataGrid.Items.Refresh();

                ParameterStatusTextBlock.Text = "파라미터 색상이 초기화되었습니다.";
                ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Gray);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"색상 초기화 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 값 초기화 버튼 클릭
        /// </summary>
        private void ClearValuesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_parameters == null) return;

            var result = MessageBox.Show(
                "모든 파라미터의 값을 초기화하시겠습니까?\n\n" +
                "초기화된 값은 저장 버튼을 눌러야 DB에 반영됩니다.",
                "값 초기화 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    foreach (var param in _parameters)
                    {
                        param.Value = "";
                    }

                    PlanInfoDataGrid.Items.Refresh();

                    ParameterStatusTextBlock.Text = "모든 값이 초기화되었습니다. 저장 버튼을 눌러주세요.";
                    ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"값 초기화 실패:\n{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 파라미터 DataGrid 선택 변경 이벤트
        /// </summary>
        private void PlanInfoDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedParameter = PlanInfoDataGrid.SelectedItem as ParameterInfo;
            DeleteParameterButton.IsEnabled = _selectedParameter != null;
        }

        /// <summary>
        /// XML 입력 버튼 클릭
        /// </summary>
        private void XmlInputButton_Click(object sender, RoutedEventArgs e)
        {
            if (_parameters == null)
            {
                MessageBox.Show("파라미터 목록이 초기화되지 않았습니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var xmlWindow = new Windows.XmlParameterInputWindow
                {
                    Owner = Window.GetWindow(this),
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (xmlWindow.ShowDialog() == true)
                {
                    var parsedParams = xmlWindow.ParsedParameters;
                    
                    if (parsedParams.Count == 0)
                    {
                        MessageBox.Show("파싱된 파라미터가 없습니다.", "알림",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    int updatedCount = 0;
                    int matchedCount = 0;

                    // 파싱된 파라미터를 기준정보 파라미터에 매칭
                    foreach (var param in _parameters)
                    {
                        string paramName = param.Parameter.TrimStart('@');
                        
                        if (parsedParams.ContainsKey(paramName))
                        {
                            matchedCount++;
                            
                            // 값이 변경된 경우만 카운트
                            if (param.Value != parsedParams[paramName])
                            {
                                param.Value = parsedParams[paramName];
                                updatedCount++;
                            }
                        }
                    }

                    PlanInfoDataGrid.Items.Refresh();

                    if (updatedCount > 0)
                    {
                        ParameterStatusTextBlock.Text = $"✅ {updatedCount}개 파라미터 값이 업데이트되었습니다. (총 {matchedCount}개 매칭됨)";
                        ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);

                        MessageBox.Show(
                            $"XML 파싱 완료!\n\n" +
                            $"• 총 파싱된 항목: {parsedParams.Count}개\n" +
                            $"• 매칭된 파라미터: {matchedCount}개\n" +
                            $"• 업데이트된 파라미터: {updatedCount}개\n\n" +
                            $"변경사항을 저장하려면 '저장' 버튼을 눌러주세요.",
                            "완료",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else if (matchedCount > 0)
                    {
                        ParameterStatusTextBlock.Text = $"ℹ️ {matchedCount}개 파라미터가 매칭되었으나 값은 동일합니다.";
                        ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Blue);

                        MessageBox.Show(
                            $"XML 파싱 완료!\n\n" +
                            $"• 총 파싱된 항목: {parsedParams.Count}개\n" +
                            $"• 매칭된 파라미터: {matchedCount}개\n" +
                            $"• 업데이트된 파라미터: 0개 (모두 동일한 값)\n",
                            "완료",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        ParameterStatusTextBlock.Text = $"⚠️ 매칭되는 파라미터가 없습니다.";
                        ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);

                        MessageBox.Show(
                            $"XML 파싱 완료!\n\n" +
                            $"• 총 파싱된 항목: {parsedParams.Count}개\n" +
                            $"• 매칭된 파라미터: 0개\n\n" +
                            $"XML의 태그명과 기준정보 파라미터명이 일치하는지 확인하세요.",
                            "알림",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"XML 입력 중 오류가 발생했습니다:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ParameterStatusTextBlock.Text = "XML 입력 실패";
                ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        #endregion

        #region 쿼리 관리

        private void QueryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            // 선택된 계획정보 쿼리의 그룹명으로 상세 쿼리 자동 로드
            if (QuerySelectComboBox.SelectedItem is QueryItem selectedPlanQuery &&
                !string.IsNullOrWhiteSpace(selectedPlanQuery.QueryName) &&
                selectedPlanQuery.OrderNumber >= 0)
            {
                LoadDetailQueriesByQueryName(selectedPlanQuery.QueryName);
            }
            else
            {
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

                var detailQueries = allQueries
                    .Where(q => q.QueryType == "정보 조회" && 
                                q.QueryName == queryName && 
                                q.OrderNumber >= 1)
                    .OrderBy(q => q.OrderNumber)
                    .ToList();

                if (detailQueries.Count > 0)
                {
                    GenerateDynamicGridsWithQueries(detailQueries);
                    System.Diagnostics.Debug.WriteLine($"✅ 그룹명 '{queryName}'에 대한 {detailQueries.Count}개의 상세 쿼리가 자동 바인딩되었습니다.");
                }
                else
                {
                    CreateDynamicGrids(20);
                    System.Diagnostics.Debug.WriteLine($"⚠️ 그룹명 '{queryName}'에 대한 상세 쿼리(순번 1 이상)가 없습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"상세 쿼리 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateDynamicGrids(int count)
        {
            CreateDynamicGridsCore(count, null);
        }

        private void GenerateDynamicGridsWithQueries(List<QueryItem> queries)
        {
            CreateDynamicGridsCore(20, queries);
        }

        private void CreateDynamicGridsCore(int count, List<QueryItem>? queriesToBind)
        {
            _isInitializing = true;

            DynamicGridsContainer.Children.Clear();
            DynamicGridsContainer.RowDefinitions.Clear();
            _dynamicGrids.Clear();

            const int gridCount = 20;

            for (int i = 0; i < gridCount; i++)
            {
                DynamicGridsContainer.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(350, GridUnitType.Pixel)
                });
            }

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

            BindGridComboBoxes(queriesToBind);
            
            _isInitializing = false;
        }

        private void BindGridComboBoxes(List<QueryItem>? queriesToBind)
        {
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

            if (queriesToBind != null && queriesToBind.Count > 0)
            {
                int count = Math.Min(queriesToBind.Count, _dynamicGrids.Count);
                
                for (int i = 0; i < count; i++)
                {
                    var gridInfo = _dynamicGrids[i];
                    var query = queriesToBind[i];

                    var matchingQuery = queriesWithBizName.FirstOrDefault(q => 
                        q.QueryName == query.QueryName && 
                        q.BizName == query.BizName && 
                        q.OrderNumber == query.OrderNumber);

                    if (matchingQuery != null)
                    {
                        gridInfo.QueryComboBox.SelectedItem = matchingQuery;
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
                
                var infoQueries = allQueries
                    .Where(q => q.QueryType == "정보 조회")
                    .OrderBy(q => q.BizName)
                    .ThenBy(q => q.OrderNumber)
                    .ToList();

                var planQueries = infoQueries.Where(q => q.OrderNumber == 0).ToList();

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

                var planQueryList = new List<QueryItem> { placeholderItem };
                planQueryList.AddRange(planQueries);
                
                QuerySelectComboBox.ItemsSource = planQueryList;
                
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
            QuerySelectComboBox.SelectedIndex = 0;
        }

        private void LoadQueriesButton_Click(object sender, RoutedEventArgs e)
        {
            LoadInfoQueries();
            LoadParameters();
            UpdateAllGridComboBoxes();
        }

        private void ClearQuerySelectButton_Click(object sender, RoutedEventArgs e)
        {
            QuerySelectComboBox.SelectedItem = null;
        }

        private void UpdateAllGridComboBoxes()
        {
            BindGridComboBoxes(null);
        }

        #endregion

        #region 동적 그리드 생성

        private DynamicGridInfo CreateDynamicGrid(int index)
        {
            var queryComboBox = new ComboBox
            {
                Width = 180,
                Height = 28,
                DisplayMemberPath = "BizName",
                Margin = new Thickness(10, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

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
                CanUserResizeColumns = true,
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                SelectionMode = DataGridSelectionMode.Extended,
                SelectionUnit = DataGridSelectionUnit.CellOrRowHeader
            };
            
            // 🔥 NERP 스타일 헤더 (DataGrid.Resources에 추가)
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
            headerStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.VerticalContentAlignmentProperty, 
                VerticalAlignment.Center));
            dataGrid.ColumnHeaderStyle = headerStyle;
            
            // 🔥 NERP 스타일 셀 (선택 시 연한 파란색 + 검정 글자)
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(5, 3, 5, 3)));
            
            var selectedTrigger = new System.Windows.Trigger
            {
                Property = DataGridCell.IsSelectedProperty,
                Value = true
            };
            selectedTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, 
                new SolidColorBrush(Color.FromRgb(227, 242, 253)))); // #E3F2FD
            selectedTrigger.Setters.Add(new Setter(DataGridCell.ForegroundProperty, Brushes.Black));
            cellStyle.Triggers.Add(selectedTrigger);
            dataGrid.CellStyle = cellStyle;
            
            dataGrid.AutoGeneratingColumn += DataGrid_AutoGeneratingColumn;
            dataGrid.LoadingRow += DataGrid_LoadingRow;
            
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

                            var textToCopy = string.Join(Environment.NewLine, rows);
                            Clipboard.SetText(textToCopy);
                            e.Handled = true;

                            System.Diagnostics.Debug.WriteLine($"✅ 동적 그리드 복사 완료: {rows.Count}행");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"복사 오류: {ex.Message}");
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
                Margin = new Thickness(0, 0, 5, 0),
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

            // 파라미터 확인 버튼
            var validateParamsButton = new Button
            {
                Content = "✔️ 파라미터",
                Width = 100,
                Height = 28,
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)), // Blue
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 10, 0),
                ToolTip = "이 쿼리의 파라미터 확인"
            };

            // 마우스 오버 스타일
            var validateButtonStyle = new Style(typeof(Button));
            validateButtonStyle.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 120, 215))));
            var validateTrigger = new System.Windows.Trigger { Property = Button.IsMouseOverProperty, Value = true };
            validateTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 86, 160))));
            validateButtonStyle.Triggers.Add(validateTrigger);
            validateParamsButton.Style = validateButtonStyle;

            validateParamsButton.Click += (s, e) => ValidateGridParameters(gridInfo);

            headerPanel.Children.Add(titleBlock);
            headerPanel.Children.Add(gridInfo.QueryComboBox);  // 쿼리 선택 콜백박스
            headerPanel.Children.Add(gridInfo.ClearButton);     // 취소 버튼
            headerPanel.Children.Add(executeButton);            // 실행 버튼
            headerPanel.Children.Add(popupButton);              // 🔥 팝업 보기 버튼
            headerPanel.Children.Add(viewQueryButton);          // 쿼리 보기 버튼
            headerPanel.Children.Add(validateParamsButton);     // 파라미터 확인 버튼
            headerPanel.Children.Add(gridInfo.ResultInfoTextBlock); // 결과 정보

            Grid.SetRow(headerPanel, 0);
            Grid.SetRow(gridInfo.DataGrid, 1);

            grid.Children.Add(headerPanel);
            grid.Children.Add(gridInfo.DataGrid);

            return grid;
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

        #endregion

        #region 쿼리 실행

        private async System.Threading.Tasks.Task ExecuteDynamicGridQuery(DynamicGridInfo gridInfo)
        {
            if (gridInfo.QueryComboBox.SelectedItem is not QueryItem query)
            {
                MessageBox.Show("조회할 쿼리를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                await ExecuteQueryToGrid(query, gridInfo.DataGrid);
                
                stopwatch.Stop();
                
                int rowCount = gridInfo.DataGrid.Items.Count;
                double seconds = stopwatch.Elapsed.TotalSeconds;
                gridInfo.ResultInfoTextBlock.Text = $"📊 {rowCount}건 | ⏱️ {seconds:F2}초";
                gridInfo.ResultInfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                gridInfo.ResultInfoTextBlock.Text = $"❌ 오류";
                gridInfo.ResultInfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                
                MessageBox.Show($"쿼리 실행 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                await ExecuteQueryToGrid(queryItem, gridInfo.DataGrid);
                
                stopwatch.Stop();
                
                int rowCount = gridInfo.DataGrid.Items.Count;
                double seconds = stopwatch.Elapsed.TotalSeconds;
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    gridInfo.ResultInfoTextBlock.Text = $"📊 {rowCount}건 | ⏱️ {seconds:F2}초";
                    gridInfo.ResultInfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    gridInfo.ResultInfoTextBlock.Text = $"❌ 오류";
                    gridInfo.ResultInfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                });
                
                throw new Exception($"[{queryItem.QueryName}] {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 전체 조회 버튼 클릭
        /// </summary>
        private async void ExecuteAllGridsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExecuteAllGridsButton.IsEnabled = false;
                ExecuteAllGridsButton.Content = "조회 중...";

                var tasks = new List<System.Threading.Tasks.Task>();
                var stopwatches = new Dictionary<int, System.Diagnostics.Stopwatch>();

                foreach (var gridInfo in _dynamicGrids)
                {
                    if (gridInfo.QueryComboBox.SelectedItem is QueryItem query)
                    {
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        stopwatches[gridInfo.Index] = stopwatch;

                        tasks.Add(ExecuteQueryToGridWithRowDataAndMeasure(query, gridInfo, null!, stopwatch));
                    }
                }

                if (tasks.Count == 0)
                {
                    MessageBox.Show("조회할 쿼리가 선택되지 않았습니다.", "알림",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await System.Threading.Tasks.Task.WhenAll(tasks);
                
                // 🔥 조회 완료 팝업 제거 - 각 그리드에 결과 표시됨
                System.Diagnostics.Debug.WriteLine($"✅ {tasks.Count}개의 쿼리 조회 완료");
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
                        new TextBlock { Text = "전체 조회" }
                    }
                };
            }
        }

        private async System.Threading.Tasks.Task ExecuteQueryToGrid(QueryItem queryItem, DataGrid targetGrid)
        {
            if (_sharedData == null) return;

            try
            {
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
                    // 🔥 데이터가 없으면 그리드를 비워둠
                    System.Diagnostics.Debug.WriteLine($"⚠️ 조회 결과 0건 - 그리드 초기화됨");
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
            // 🔥 Disconnect() 제거 - ExecuteQueryAsync 내부에서 연결 관리하므로 불필요
            // 병렬 실행 시 다른 쿼리의 연결을 끊는 문제 해결
        }

        private async System.Threading.Tasks.Task ExecuteQueryToGridWithRowData(
            QueryItem queryItem, 
            DataGrid targetGrid, 
            DataRowView selectedRow)
        {
            await ExecuteQueryToGrid(queryItem, targetGrid);
        }

        private string ReplaceQueryParameters(string query)
        {
            string result = query;

            // 사업장 선택 정보 가져오기
            var selectedSite = SiteComboBox.SelectedItem as SiteInfo;
            
            string factory = selectedSite?.RepresentativeFactory ?? _sharedData?.Settings.GmesFactory ?? "";
            string org = selectedSite?.Organization ?? _sharedData?.Settings.GmesOrg ?? "";
            string facility = selectedSite?.Facility ?? _sharedData?.Settings.GmesFacility ?? "";
            string wipLineId = selectedSite?.WipLineId ?? _sharedData?.Settings.GmesWipLineId ?? "";
            string equipLineId = selectedSite?.EquipLineId ?? _sharedData?.Settings.GmesEquipLineId ?? "";
            string division = selectedSite?.Division ?? "";

            // 🔥 DB Link 보호: 파라미터만 치환 (TABLE@DBLINK 형식은 치환하지 않음)
            result = SafeReplaceParameter(result, "@REPRESENTATIVE_FACTORY_CODE", $"'{factory}'");
            result = SafeReplaceParameter(result, "@ORGANIZATION_ID", $"'{org}'");
            result = SafeReplaceParameter(result, "@FACILITY_CODE", $"'{facility}'");
            result = SafeReplaceParameter(result, "@WIP_LINE_ID", $"'{wipLineId}'");
            result = SafeReplaceParameter(result, "@LINE_ID", $"'{equipLineId}'");
            result = SafeReplaceParameter(result, "@DIVISION", $"'{division}'");

            if (_parameters != null)
            {
                foreach (var param in _parameters)
                {
                    if (!string.IsNullOrWhiteSpace(param.Parameter))
                    {
                        string parameterName = param.Parameter.StartsWith("@") ? param.Parameter : $"@{param.Parameter}";
                        string parameterValue = param.Value ?? "";
                        
                        // 🔥 DB Link 보호
                        result = SafeReplaceParameter(result, parameterName, $"'{parameterValue}'");
                    }
                }
            }

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

        #endregion

        #region DataGrid 이벤트

        private void DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string header = e.Column.Header as string ?? e.PropertyName;
            
            if (!string.IsNullOrEmpty(header))
            {
                e.Column.Header = header.Replace("_", "__");
            }
            
            // 🔥 NERP 스타일: 정렬 활성화
            e.Column.CanUserSort = true;
            
            // 🔥 NERP 스타일: 숫자 타입 컬럼 자동 인식
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
                
                // 🔥 NERP 스타일: 자동 너비 + 최소 너비
                e.Column.MinWidth = 80;
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
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
        
        private DataTemplate CreateClobCellTemplate(string columnName)
        {
            var factory = new FrameworkElementFactory(typeof(TextBox));
            
            factory.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding(columnName)
            {
                Mode = System.Windows.Data.BindingMode.OneWay
            });
            
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

        private void DataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            // 동적 그리드만 처리 (파라미터 그리드는 XAML의 DataTrigger로 처리됨)
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
                        e.Row.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
                        e.Row.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
                    }
                }
                else
                {
                    e.Row.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
                    e.Row.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCellValue 오류: {ex.Message}");
            }
            
            return "";
        }

        #endregion

        #region 폰트 및 Excel

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
                    foreach (var gridInfo in _dynamicGrids)
                    {
                        if (gridInfo.DataGrid.ItemsSource != null)
                        {
                            var dataView = gridInfo.DataGrid.ItemsSource as DataView;
                            if (dataView != null && dataView.Count > 0)
                            {
                                var queryName = (gridInfo.QueryComboBox.SelectedItem as QueryItem)?.QueryName ?? $"그리드{gridInfo.Index}";
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

                    var fileInfo = new FileInfo(saveFileDialog.FileName);
                    package.SaveAs(fileInfo);

                    MessageBox.Show($"Excel 파일이 성공적으로 저장되었습니다.\n\n파일: {fileInfo.Name}\n시트 수: {package.Workbook.Worksheets.Count}개",
                        "완료", MessageBoxButton.OK, MessageBoxImage.Information);

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

        private void AddDataGridToExcel(ExcelPackage package, string sheetName, DataTable dataTable)
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            for (int col = 0; col < dataTable.Columns.Count; col++)
            {
                var columnName = dataTable.Columns[col].ColumnName;
                worksheet.Cells[1, col + 1].Value = columnName.Replace("_", " ");
            }

            using (var range = worksheet.Cells[1, 1, 1, dataTable.Columns.Count])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 120, 215));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            for (int row = 0; row < dataTable.Rows.Count; row++)
            {
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    var cellValue = dataTable.Rows[row][col];
                    var cell = worksheet.Cells[row + 2, col + 1];

                    if (cellValue != null && cellValue != DBNull.Value)
                    {
                        if (cellValue is decimal || cellValue is double || cellValue is float || 
                            cellValue is int || cellValue is long)
                        {
                            cell.Value = cellValue;
                        }
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

                    cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

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

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            for (int col = 1; col <= dataTable.Columns.Count; col++)
            {
                var column = worksheet.Column(col);
                if (column.Width < 10)
                    column.Width = 10;
                else if (column.Width > 50)
                    column.Width = 50;
            }

            worksheet.View.FreezePanes(2, 1);
        }

        private string SanitizeSheetName(string name, int index)
        {
            var invalidChars = new char[] { '\\', '/', '*', '?', ':', '[', ']' };
            string sanitized = name;

            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c.ToString(), "");
            }

            if (sanitized.Length > 25)
            {
                sanitized = sanitized.Substring(0, 25);
            }

            sanitized = $"{index}_{sanitized}";

            return sanitized;
        }

        public void ApplyFontSize()
        {
            if (_sharedData == null) return;

            foreach (var gridInfo in _dynamicGrids)
            {
                ApplyFontSizeToGrid(gridInfo.DataGrid);
            }
        }

        private void ApplyFontSizeToGrid(DataGrid dataGrid)
        {
            if (_sharedData == null) return;

            int fontSize = _sharedData.Settings.FontSize;

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

        #endregion

        #region 파라미터 확인

        /// <summary>
        /// 파라미터 확인 버튼 클릭
        /// </summary>
        private void ValidateParametersButton_Click(object sender, RoutedEventArgs e)
        {
            // 선택된 쿼리 확인
            if (QuerySelectComboBox.SelectedItem is not QueryItem selectedQuery || selectedQuery.OrderNumber < 0)
            {
                MessageBox.Show("먼저 기준정보 쿼리를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_parameters == null || _parameters.Count == 0)
            {
                MessageBox.Show("파라미터가 없습니다.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 선택된 쿼리 그룹의 모든 쿼리 가져오기
                var groupQueries = _infoQueries
                    .Where(q => q.QueryName == selectedQuery.QueryName)
                    .ToList();

                if (groupQueries.Count == 0)
                {
                    MessageBox.Show("해당 그룹의 쿼리를 찾을 수 없습니다.", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 모든 쿼리에서 사용되는 파라미터 추출
                var allUsedParameters = new HashSet<string>();
                foreach (var query in groupQueries)
                {
                    var usedParams = ExtractParametersFromQuery(query.Query);
                    foreach (var param in usedParams)
                    {
                        allUsedParameters.Add(param);
                    }
                }

                // 기본 사업장 파라미터 제외
                var defaultParams = new HashSet<string>
                {
                    "@REPRESENTATIVE_FACTORY_CODE",
                    "@ORGANIZATION_ID",
                    "@FACILITY_CODE",
                    "@WIP_LINE_ID",
                    "@LINE_ID",
                    "@DIVISION"
                };

                // 사용자 정의 파라미터만 필터링
                var userDefinedParams = allUsedParameters.Except(defaultParams).ToList();

                // 기준정보 파라미터에 정의된 파라미터 목록
                var definedParams = _parameters
                    .Where(p => !string.IsNullOrWhiteSpace(p.Parameter))
                    .Select(p => p.Parameter.StartsWith("@") ? p.Parameter : $"@{p.Parameter}")
                    .ToHashSet();

                // 없는 파라미터 확인
                var missingParams = userDefinedParams.Except(definedParams).ToList();

                // 모든 파라미터 행의 색상 초기화
                foreach (var param in _parameters)
                {
                    param.IsHighlighted = false;
                }

                if (missingParams.Count > 0)
                {
                    // 없는 파라미터가 있으면 경고 메시지
                    var missingParamList = string.Join("\n", missingParams.Select(p => $"  - {p}"));
                    MessageBox.Show(
                        $"다음 파라미터가 기준정보에 정의되어 있지 않습니다:\n\n{missingParamList}\n\n" +
                        "이 파라미터들을 기준정보에 추가하거나 쿼리를 수정하세요.",
                        "파라미터 누락",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    ParameterStatusTextBlock.Text = $"⚠️ {missingParams.Count}개 파라미터 누락";
                    ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    // 사용되는 파라미터 행의 하이라이트
                    foreach (var param in _parameters)
                    {
                        var paramName = param.Parameter.StartsWith("@") ? param.Parameter : $"@{param.Parameter}";
                        if (userDefinedParams.Contains(paramName))
                        {
                            param.IsHighlighted = true;
                        }
                    }

                    ParameterStatusTextBlock.Text = $"✅ {userDefinedParams.Count}개 파라미터 확인 완료 (LightCoral 색상 표시)";
                    ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파라미터 확인 중 오류가 발생했습니다:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<string> ExtractParametersFromQuery(string query)
        {
            var parameters = new List<string>();

            try
            {
                // 단순 공백 및 개행 문자 기준으로 파라미터 분리
                var tokens = query.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var token in tokens)
                {
                    // '@'로 시작하고 그 다음이 영문자 또는 '_'인 경우에만 파라미터로 인식
                    if (token.StartsWith("@") && 
                        token.Length > 1 && 
                        (char.IsLetter(token[1]) || token[1] == '_'))
                    {
                        // 특수문자 제거 후 파라미터만 추출
                        var cleaned = new string(token.Skip(1).TakeWhile(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
                        if (!string.IsNullOrEmpty(cleaned))
                        {
                            var param = "@" + cleaned;
                            if (!parameters.Contains(param))
                            {
                                parameters.Add(param);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"파라미터 추출 오류: {ex.Message}");
            }

            return parameters;
        }

        /// <summary>
        /// 개별 그리드의 파라미터 확인
        /// </summary>
        private void ValidateGridParameters(DynamicGridInfo gridInfo)
        {
            // 선택된 쿼리 확인
            if (gridInfo.QueryComboBox.SelectedItem is not QueryItem selectedQuery)
            {
                MessageBox.Show($"그리드 {gridInfo.Index}에서 쿼리를 먼저 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_parameters == null || _parameters.Count == 0)
            {
                MessageBox.Show("파라미터가 없습니다.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 선택된 쿼리에서 사용되는 파라미터 추출
                var usedParams = ExtractParametersFromQuery(selectedQuery.Query);

                // 기본 사업장 파라미터 제외
                var defaultParams = new HashSet<string>
                {
                    "@REPRESENTATIVE_FACTORY_CODE",
                    "@ORGANIZATION_ID",
                    "@FACILITY_CODE",
                    "@WIP_LINE_ID",
                    "@LINE_ID",
                    "@DIVISION"
                };

                // 사용자 정의 파라미터만 필터링
                var userDefinedParams = usedParams.Except(defaultParams).ToList();

                // 기준정보 파라미터에 정의된 파라미터 목록
                var definedParams = _parameters
                    .Where(p => !string.IsNullOrWhiteSpace(p.Parameter))
                    .Select(p => p.Parameter.StartsWith("@") ? p.Parameter : $"@{p.Parameter}")
                    .ToHashSet();

                // 없는 파라미터 확인
                var missingParams = userDefinedParams.Except(definedParams).ToList();

                // 모든 파라미터 행의 색상 초기화
                foreach (var param in _parameters)
                {
                    param.IsHighlighted = false;
                }

                if (missingParams.Count > 0)
                {
                    // 없는 파라미터가 있으면 경고 메시지
                    var missingParamList = string.Join("\n", missingParams.Select(p => $"  - {p}"));
                    MessageBox.Show(
                        $"[그리드 {gridInfo.Index}]\n\n" +
                        $"다음 파라미터가 기준정보에 정의되어 있지 않습니다:\n\n{missingParamList}\n\n" +
                        "이 파라미터들을 기준정보에 추가하거나 쿼리를 수정하세요.",
                        "파라미터 누락",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    gridInfo.ResultInfoTextBlock.Text = $"⚠️ {missingParams.Count}개 파라미터 누락";
                    gridInfo.ResultInfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));

                    ParameterStatusTextBlock.Text = $"⚠️ 그리드 {gridInfo.Index}: {missingParams.Count}개 파라미터 누락";
                    ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    // 사용되는 파라미터 행을 하이라이트
                    foreach (var param in _parameters)
                    {
                        var paramName = param.Parameter.StartsWith("@") ? param.Parameter : $"@{param.Parameter}";
                        if (userDefinedParams.Contains(paramName))
                        {
                            param.IsHighlighted = true;
                        }
                    }

                    if (userDefinedParams.Count > 0)
                    {
                        gridInfo.ResultInfoTextBlock.Text = $"✅ {userDefinedParams.Count}개 파라미터 확인";
                        gridInfo.ResultInfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));

                        ParameterStatusTextBlock.Text = $"✅ 그리드 {gridInfo.Index}: {userDefinedParams.Count}개 파라미터 확인 완료 (LightCoral 색상 표시)";
                        ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        gridInfo.ResultInfoTextBlock.Text = "ℹ️ 파라미터 없음";
                        gridInfo.ResultInfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125));

                        ParameterStatusTextBlock.Text = $"ℹ️ 그리드 {gridInfo.Index}: 사용자 정의 파라미터 없음";
                        ParameterStatusTextBlock.Foreground = new SolidColorBrush(Colors.Gray);
                    }
                }

                PlanInfoDataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파라미터 확인 중 오류가 발생했습니다:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                gridInfo.ResultInfoTextBlock.Text = "❌ 확인 실패";
                gridInfo.ResultInfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
            }
        }

        #endregion
    }
}
