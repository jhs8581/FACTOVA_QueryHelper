using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FACTOVA_QueryHelper.Database;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// QueryManagementControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class QueryManagementControl : UserControl
    {
        private SharedDataContext? _sharedData;
        private QueryDatabase? _database;
        private QueryItem? _selectedQuery;
        
        // 🔥 접속 정보 목록
        private List<Models.ConnectionInfo> _connectionInfos = new List<Models.ConnectionInfo>();
        
        // 🔥 각 탭별 쿼리 컬렉션
        private System.Collections.ObjectModel.ObservableCollection<QueryItem>? _queryExecutionQueries;
        private System.Collections.ObjectModel.ObservableCollection<QueryItem>? _infoQueries;
        private System.Collections.ObjectModel.ObservableCollection<QueryItem>? _bizQueries;
        
        // 🔥 각 탭별 수정 추적 (독립적으로 관리)
        private HashSet<QueryItem> _queryExecutionModified = new HashSet<QueryItem>();
        private HashSet<QueryItem> _infoQueriesModified = new HashSet<QueryItem>();
        private HashSet<QueryItem> _bizQueriesModified = new HashSet<QueryItem>();

        // 🔥 각 탭별 UI 요소 (동적 생성)
        private DataGrid? _currentDataGrid;
        private TextBlock? _currentQueryCountTextBlock;
        private Border? _currentEditModeBorder;
        private TextBlock? _currentStatusTextBlock;
        private Button? _currentDeleteButton;
        private Button? _currentDuplicateButton;

        public QueryManagementControl()
        {
            InitializeComponent();
            
            // 초기화는 Loaded 이벤트에서 수행
            Loaded += QueryManagementControl_Loaded;
        }

        private void QueryManagementControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 첫 번째 탭(쿼리 실행)의 UI 생성
            if (QueryTypeTabControl.SelectedIndex == 0)
            {
                CreateTabContent("쿼리 실행");
            }
        }

        /// <summary>
        /// 공유 데이터 컨텍스트를 초기화합니다.
        /// </summary>
        public void Initialize(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            _database = new QueryDatabase(sharedData.Settings.DatabasePath);
            
            // 🔥 접속 정보 로드
            LoadConnectionInfos();
            
            LoadQueriesFromDatabase();
        }
        
        /// <summary>
        /// 접속 정보 목록을 로드합니다.
        /// </summary>
        private void LoadConnectionInfos()
        {
            if (_sharedData == null) return;
            
            try
            {
                var connectionService = new Services.ConnectionInfoService(_sharedData.Settings.DatabasePath);
                _connectionInfos = connectionService.GetAllConnections();
                
                System.Diagnostics.Debug.WriteLine($"접속 정보 {_connectionInfos.Count}개 로드됨");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"접속 정보 로드 실패: {ex.Message}");
                _connectionInfos = new List<Models.ConnectionInfo>();
            }
        }

        /// <summary>
        /// 데이터베이스에서 쿼리 목록을 로드합니다.
        /// </summary>
        private void LoadQueriesFromDatabase()
        {
            if (_database == null) return;

            try
            {
                var allQueries = _database.GetAllQueries().OrderBy(q => q.RowNumber).ToList();
                
                // 🔥 구분별로 쿼리 분류
                _queryExecutionQueries = new System.Collections.ObjectModel.ObservableCollection<QueryItem>(
                    allQueries.Where(q => q.QueryType == "쿼리 실행"));
                
                _infoQueries = new System.Collections.ObjectModel.ObservableCollection<QueryItem>(
                    allQueries.Where(q => q.QueryType == "정보 조회"));
                
                _bizQueries = new System.Collections.ObjectModel.ObservableCollection<QueryItem>(
                    allQueries.Where(q => q.QueryType == "비즈 조회"));
                
                // 🔥 현재 탭의 DataGrid 업데이트
                UpdateCurrentTabDataGrid();
                
                // 🔥 각 탭별 변경 초track 초기화
                _queryExecutionModified.Clear();
                _infoQueriesModified.Clear();
                _bizQueriesModified.Clear();
                
                if (_currentEditModeBorder != null)
                {
                    _currentEditModeBorder.Visibility = Visibility.Collapsed;
                }
                
                UpdateStatus($"{allQueries.Count}개의 쿼리가 로드되었습니다.", Colors.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"쿼리 로드 실패: {ex.Message}", Colors.Red);
            }
        }

        /// <summary>
        /// 현재 선택된 탭의 DataGrid 업데이트
        /// </summary>
        private void UpdateCurrentTabDataGrid()
        {
            if (_currentDataGrid == null || _currentQueryCountTextBlock == null) return;

            var selectedIndex = QueryTypeTabControl.SelectedIndex;
            System.Collections.ObjectModel.ObservableCollection<QueryItem>? queries = null;

            switch (selectedIndex)
            {
                case 0: // 쿼리 실행
                    queries = _queryExecutionQueries;
                    break;
                case 1: // 정보 조회
                    queries = _infoQueries;
                    break;
                case 2: // 비즈 조회
                    queries = _bizQueries;
                    break;
            }

            if (queries != null)
            {
                _currentDataGrid.ItemsSource = queries;
                _currentQueryCountTextBlock.Text = $"{queries.Count}개";
            }
        }

        /// <summary>
        /// 탭 변경 이벤트
        /// </summary>
        private void QueryTypeTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is not TabControl) return;

            var selectedTab = QueryTypeTabControl.SelectedItem as TabItem;
            if (selectedTab == null) return;

            var headerPanel = selectedTab.Header as StackPanel;
            if (headerPanel == null || headerPanel.Children.Count < 2) return;

            var headerText = (headerPanel.Children[1] as TextBlock)?.Text ?? "";

            CreateTabContent(headerText);
        }

        /// <summary>
        /// 선택된 탭의 컨텐츠 생성
        /// </summary>
        private void CreateTabContent(string tabType)
        {
            Grid? targetGrid = null;
            
            switch (QueryTypeTabControl.SelectedIndex)
            {
                case 0:
                    targetGrid = QueryExecutionGrid;
                    break;
                case 1:
                    targetGrid = InfoQueryGrid;
                    break;
                case 2:
                    targetGrid = BizQueryGrid;
                    break;
            }

            if (targetGrid == null || targetGrid.Children.Count > 0) return;

            // UI 생성
            CreateQueryManagementUI(targetGrid, tabType);
        }

        /// <summary>
        /// 쿼리 관리 UI 생성
        /// </summary>
        private void CreateQueryManagementUI(Grid parentGrid, string queryType)
        {
            parentGrid.Children.Clear();
            parentGrid.RowDefinitions.Clear();
            
            parentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            parentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            parentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 🔥 상단 툴바
            var toolbar = CreateToolbar(queryType);
            Grid.SetRow(toolbar, 0);
            parentGrid.Children.Add(toolbar);

            // 🔥 쿼리 목록 영역
            var queryListBorder = CreateQueryListArea(queryType);
            Grid.SetRow(queryListBorder, 1);
            parentGrid.Children.Add(queryListBorder);

            // 🔥 하단 상태바
            var statusBar = CreateStatusBar();
            Grid.SetRow(statusBar, 2);
            parentGrid.Children.Add(statusBar);

            // 데이터 바인딩
            UpdateCurrentTabDataGrid();
        }

        /// <summary>
        /// 상단 툴바 생성
        /// </summary>
        private Border CreateToolbar(string queryType)
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 왼쪽 버튼 그룹
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 새로고침 버튼
            var refreshButton = CreateButton("🔄", "새로고침", 120, "#FF0078D7");
            refreshButton.Click += LoadFromDbButton_Click;
            refreshButton.Margin = new Thickness(0, 0, 10, 0);
            buttonPanel.Children.Add(refreshButton);

            // 구분선
            buttonPanel.Children.Add(new Rectangle
            {
                Width = 1,
                Height = 24,
                Fill = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                Margin = new Thickness(5, 0, 5, 0)
            });

            // 쿼리 추가 버튼
            var addButton = CreateButton("➕", "쿼리 추가", 120, "#FF28A745");
            addButton.Click += (s, e) => AddQueryButton_Click(s, e, queryType);
            addButton.Margin = new Thickness(10, 0, 5, 0);
            buttonPanel.Children.Add(addButton);

            // 쿼리 복제 버튼
            var duplicateButton = CreateButton("📋", "쿼리 복제", 120, "#FF17A2B8");
            duplicateButton.Click += DuplicateQueryButton_Click;
            duplicateButton.IsEnabled = false;
            duplicateButton.Margin = new Thickness(5, 0, 5, 0);
            _currentDuplicateButton = duplicateButton;
            buttonPanel.Children.Add(duplicateButton);

            // 삭제 버튼
            var deleteButton = CreateButton("🗑️", "삭제", 100, "#FFDC3545");
            deleteButton.Click += DeleteQueryButton_Click;
            deleteButton.IsEnabled = false;
            _currentDeleteButton = deleteButton;
            deleteButton.Margin = new Thickness(5, 0, 10, 0);
            buttonPanel.Children.Add(deleteButton);

            // 구분선
            buttonPanel.Children.Add(new Rectangle
            {
                Width = 1,
                Height = 24,
                Fill = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                Margin = new Thickness(5, 0, 5, 0)
            });

            // 🔥 Excel 다운로드 버튼
            var excelButton = CreateButton("📊", "Excel 다운로드", 150, "#FF28A745");
            excelButton.Click += ExportToExcelButton_Click;
            excelButton.Margin = new Thickness(10, 0, 0, 0);
            buttonPanel.Children.Add(excelButton);

            Grid.SetColumn(buttonPanel, 0);
            grid.Children.Add(buttonPanel);

            // 오른쪽: 쿼리 수 표시
            var countBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 248, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15, 8, 15, 8)
            };

            var countPanel = new StackPanel { Orientation = Orientation.Horizontal };
            countPanel.Children.Add(new TextBlock
            {
                Text = "📊 로드된 쿼리:",
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 8, 0)
            });

            var countText = new TextBlock
            {
                Text = "0개",
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                FontSize = 16
            };
            _currentQueryCountTextBlock = countText;
            countPanel.Children.Add(countText);

            countBorder.Child = countPanel;
            Grid.SetColumn(countBorder, 1);
            grid.Children.Add(countBorder);

            border.Child = grid;
            return border;
        }

        /// <summary>
        /// 버튼 생성 헬퍼 메서드
        /// </summary>
        private Button CreateButton(string icon, string text, double width, string colorHex)
        {
            var button = new Button
            {
                Width = width,
                Height = 36,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.SemiBold
            };

            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 14,
                Margin = new Thickness(0, 0, 5, 0)
            });
            panel.Children.Add(new TextBlock { Text = text });

            button.Content = panel;
            return button;
        }

        /// <summary>
        /// 쿼리 목록 영역 생성
        /// </summary>
        private Border CreateQueryListArea(string queryType)
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 헤더
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            headerGrid.Children.Add(new TextBlock
            {
                Text = $"{queryType} 쿼리 목록",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });

            headerGrid.Children.Add(new TextBlock
            {
                Text = "셀 더블클릭: 편집 | Ctrl+C: 복사",
                FontSize = 11,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            });

            Grid.SetRow(headerGrid, 0);
            grid.Children.Add(headerGrid);

            // 편집 모드 Border
            var editModeBorder = CreateEditModeBorder();
            _currentEditModeBorder = editModeBorder;
            Grid.SetRow(editModeBorder, 1);
            grid.Children.Add(editModeBorder);

            // 🔥 ScrollViewer로 DataGrid를 감싸기
            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            
            // DataGrid
            var dataGrid = CreateDataGrid(queryType);
            _currentDataGrid = dataGrid;
            scrollViewer.Content = dataGrid;
            
            Grid.SetRow(scrollViewer, 2);
            grid.Children.Add(scrollViewer);

            border.Child = grid;
            return border;
        }

        /// <summary>
        /// 편집 모드 Border 생성
        /// </summary>
        private Border CreateEditModeBorder()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 234, 167)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 10, 0, 10),
                Visibility = Visibility.Collapsed
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            grid.Children.Add(new TextBlock
            {
                Text = "✏️",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });

            var messageTextBlock = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            messageTextBlock.Inlines.Add(new Run("편집 중입니다. ") { FontWeight = FontWeights.Bold });
            messageTextBlock.Inlines.Add(new Run("필드를 수정한 후 저장하거나 취소하세요."));
            Grid.SetColumn(messageTextBlock, 1);
            grid.Children.Add(messageTextBlock);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var saveButton = new Button
            {
                Width = 130,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0)
            };
            saveButton.Click += SaveEditButton_Click;
            
            var savePanel = new StackPanel { Orientation = Orientation.Horizontal };
            savePanel.Children.Add(new TextBlock { Text = "💾", Margin = new Thickness(0, 0, 5, 0) });
            savePanel.Children.Add(new TextBlock { Text = "변경사항 저장", FontWeight = FontWeights.SemiBold });
            saveButton.Content = savePanel;
            buttonPanel.Children.Add(saveButton);

            var cancelButton = new Button
            {
                Width = 80,
                Height = 30,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            cancelButton.Click += CancelEditButton_Click;
            
            var cancelPanel = new StackPanel { Orientation = Orientation.Horizontal };
            cancelPanel.Children.Add(new TextBlock { Text = "❌", Margin = new Thickness(0, 0, 5, 0) });
            cancelPanel.Children.Add(new TextBlock { Text = "취소" });
            cancelButton.Content = cancelPanel;
            buttonPanel.Children.Add(cancelButton);

            Grid.SetColumn(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            border.Child = grid;
            return border;
        }

        /// <summary>
        /// DataGrid 생성
        /// </summary>
        private DataGrid CreateDataGrid(string queryType)
        {
            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                SelectionMode = DataGridSelectionMode.Single,
                CanUserSortColumns = true,
                CanUserResizeColumns = true,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                HeadersVisibility = DataGridHeadersVisibility.All,
                FontSize = 11,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                Background = Brushes.White,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,  // 🔥 가로 스크롤바 추가
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            dataGrid.SelectionChanged += QueriesDataGrid_SelectionChanged;
            dataGrid.BeginningEdit += QueriesDataGrid_BeginningEdit;
            dataGrid.CellEditEnding += QueriesDataGrid_CellEditEnding;

            // 헤더 스타일
            var headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, 
                new SolidColorBrush(Color.FromRgb(0, 120, 215))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, Brushes.White));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.HeightProperty, 35.0));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, 
                HorizontalAlignment.Center));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, 
                new SolidColorBrush(Color.FromRgb(0, 86, 160))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, 
                new Thickness(0, 0, 1, 0)));
            dataGrid.ColumnHeaderStyle = headerStyle;

            // 셀 스타일
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, 
                new SolidColorBrush(Color.FromRgb(224, 224, 224))));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0, 0, 1, 0)));
            cellStyle.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(8, 5, 8, 5)));
            dataGrid.CellStyle = cellStyle;

            // 컬럼 정의
            AddDataGridColumns(dataGrid, queryType);

            return dataGrid;
        }

        /// <summary>
        /// DataGrid 컬럼 추가
        /// </summary>
        private void AddDataGridColumns(DataGrid dataGrid, string queryType)
        {
            // ID 형식화
            var idColumn = new DataGridTextColumn
            {
                Header = "ID",
                Binding = new System.Windows.Data.Binding("RowNumber")
                {
                    StringFormat = "D3" // 3자리 숫자 형식
                },
                Width = 60,
                IsReadOnly = true
            };
            var idStyle = new Style(typeof(TextBlock));
            idStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            idStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
            idStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, 
                new SolidColorBrush(Color.FromRgb(108, 117, 125))));
            idColumn.ElementStyle = idStyle;
            dataGrid.Columns.Add(idColumn);


            // 🔥 그룹명 (기존 쿼리명 컬럼의 헤더만 변경)
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "그룹명",
                Binding = new System.Windows.Data.Binding("QueryName") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                Width = 150
            });

            // 🔥 비즈명 (모든 탭에서 표시)
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "비즈명",
                Binding = new System.Windows.Data.Binding("BizName") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                Width = 150
            });


            // 🔥 쿼리비즈명 (신규 컬럼)
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "쿼리비즈명",
                Binding = new System.Windows.Data.Binding("QueryBizName") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                Width = 150
            });

            // 🔥 설명 - 멀티라인 TextBox 템플릿 사용
            var descriptionTemplate = new DataTemplate();
            var descriptionFactory = new FrameworkElementFactory(typeof(TextBox));
            descriptionFactory.SetBinding(TextBox.TextProperty, 
                new System.Windows.Data.Binding("Description2") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
            descriptionFactory.SetValue(TextBox.AcceptsReturnProperty, true);
            descriptionFactory.SetValue(TextBox.TextWrappingProperty, TextWrapping.Wrap);
            descriptionFactory.SetValue(TextBox.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            descriptionFactory.SetValue(TextBox.MinHeightProperty, 40.0);
            descriptionFactory.SetValue(TextBox.MaxHeightProperty, 100.0);
            descriptionFactory.SetValue(TextBox.BorderThicknessProperty, new Thickness(0));
            descriptionFactory.SetValue(TextBox.BackgroundProperty, Brushes.Transparent);
            descriptionFactory.SetValue(TextBox.PaddingProperty, new Thickness(4));
            descriptionTemplate.VisualTree = descriptionFactory;

            // 읽기 전용 모드용 템플릿
            var descriptionDisplayTemplate = new DataTemplate();
            var descriptionDisplayFactory = new FrameworkElementFactory(typeof(TextBlock));
            descriptionDisplayFactory.SetBinding(TextBlock.TextProperty, 
                new System.Windows.Data.Binding("Description2"));
            descriptionDisplayFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            descriptionDisplayFactory.SetValue(TextBlock.PaddingProperty, new Thickness(4));
            descriptionDisplayFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Top);
            descriptionDisplayTemplate.VisualTree = descriptionDisplayFactory;

            dataGrid.Columns.Add(new DataGridTemplateColumn
            {
                Header = "설명",
                CellTemplate = descriptionDisplayTemplate,
                CellEditingTemplate = descriptionTemplate,
                Width = 250
            });

            // 순번 (정보 조회, 비즈 조회에서만 표시)
            if (queryType == "정보 조회" || queryType == "비즈 조회")
            {
                var orderColumn = new DataGridTextColumn
                {
                    Header = "순번",
                    Binding = new System.Windows.Data.Binding("OrderNumber") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                    Width = 60
                };
                var orderStyle = new Style(typeof(TextBlock));
                orderStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
                orderColumn.ElementStyle = orderStyle;
                dataGrid.Columns.Add(orderColumn);
            }

            // 🔥 접속 정보 선택 콤보박스
            var connectionTemplate = new DataTemplate();
            var connectionFactory = new FrameworkElementFactory(typeof(ComboBox));
            connectionFactory.SetValue(ComboBox.ItemsSourceProperty, _connectionInfos);
            connectionFactory.SetValue(ComboBox.DisplayMemberPathProperty, "DisplayName");
            connectionFactory.SetValue(ComboBox.SelectedValuePathProperty, "Id");
            connectionFactory.SetBinding(ComboBox.SelectedValueProperty, 
                new System.Windows.Data.Binding("ConnectionInfoId") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
            connectionFactory.SetValue(ComboBox.HeightProperty, 28.0);
            connectionFactory.SetValue(ComboBox.FontSizeProperty, 11.0);
            connectionTemplate.VisualTree = connectionFactory;

            // 읽기 전용 모드 (접속 정보 이름 표시)
            var connectionDisplayTemplate = new DataTemplate();
            var connectionDisplayFactory = new FrameworkElementFactory(typeof(TextBlock));
            
            // Converter를 사용하여 ConnectionInfoId로부터 DisplayName을 가져옴
            var connectionInfoConverter = new ConnectionInfoIdToNameConverter(_connectionInfos);
            var connectionBinding = new System.Windows.Data.Binding("ConnectionInfoId");
            connectionBinding.Converter = connectionInfoConverter;
            connectionDisplayFactory.SetBinding(TextBlock.TextProperty, connectionBinding);
            connectionDisplayFactory.SetValue(TextBlock.PaddingProperty, new Thickness(4));
            connectionDisplayFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            connectionDisplayTemplate.VisualTree = connectionDisplayFactory;

            dataGrid.Columns.Add(new DataGridTemplateColumn
            {
                Header = "🔌 접속 정보",
                CellTemplate = connectionDisplayTemplate,
                CellEditingTemplate = connectionTemplate,
                Width = 200
            });

            // TNS (숨김 - 과거 버전 호환성 유지)
            var tnsColumn = new DataGridTextColumn
            {
                Header = "TNS",
                Binding = new System.Windows.Data.Binding("TnsName"),
                Width = 0,
                IsReadOnly = true,
                Visibility = Visibility.Collapsed
            };
            dataGrid.Columns.Add(tnsColumn);

            // User ID (숨김 - 과거 버전 호환성 유지)
            var userIdColumn = new DataGridTextColumn
            {
                Header = "User ID",
                Binding = new System.Windows.Data.Binding("UserId"),
                Width = 0,
                IsReadOnly = true,
                Visibility = Visibility.Collapsed
            };
            dataGrid.Columns.Add(userIdColumn);

            // Password (숨김 - 과거 버전 호환성 유지)
            var passwordColumn = new DataGridTextColumn
            {
                Header = "Password",
                Binding = new System.Windows.Data.Binding("Password"),
                Width = 0,
                IsReadOnly = true,
                Visibility = Visibility.Collapsed
            };
            dataGrid.Columns.Add(passwordColumn);

            // SQL 쿼리
            var queryTemplate = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Button));
            factory.SetValue(Button.ContentProperty, "📝 편집");
            factory.SetValue(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(108, 117, 125)));
            factory.SetValue(Button.ForegroundProperty, Brushes.White);
            factory.SetValue(Button.BorderThicknessProperty, new Thickness(0));
            factory.SetValue(Button.HeightProperty, 24.0);
            factory.SetValue(Button.FontSizeProperty, 10.0);
            factory.SetValue(Button.CursorProperty, Cursors.Hand);
            factory.SetValue(Button.MarginProperty, new Thickness(2));
            factory.SetBinding(Button.TagProperty, new System.Windows.Data.Binding());
            factory.AddHandler(Button.ClickEvent, new RoutedEventHandler(EditQueryButton_InGrid_Click));
            queryTemplate.VisualTree = factory;

            dataGrid.Columns.Add(new DataGridTemplateColumn
            {
                Header = "SQL 쿼리",
                CellTemplate = queryTemplate,
                Width = 120
            });

            // 쿼리 실행 탭 전용 컬럼들
            if (queryType == "쿼리 실행")
            {
                dataGrid.Columns.Add(new DataGridCheckBoxColumn
                {
                    Header = "실행",
                    Binding = new System.Windows.Data.Binding("EnabledFlagBool") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                    Width = 50
                });

                dataGrid.Columns.Add(new DataGridCheckBoxColumn
                {
                    Header = "알림",
                    Binding = new System.Windows.Data.Binding("NotifyFlagBool") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                    Width = 50
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "≥건수",
                    Binding = new System.Windows.Data.Binding("CountGreaterThan") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                    Width = 60
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "=건수",
                    Binding = new System.Windows.Data.Binding("CountEquals") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                    Width = 60
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "≤건수",
                    Binding = new System.Windows.Data.Binding("CountLessThan") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                    Width = 60
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "컬럼명",
                    Binding = new System.Windows.Data.Binding("ColumnNames") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                    Width = 120
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "컬럼값",
                    Binding = new System.Windows.Data.Binding("ColumnValues") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                    Width = 120
                });

                dataGrid.Columns.Add(new DataGridCheckBoxColumn
                {
                    Header = "포함",
                    Binding = new System.Windows.Data.Binding("ExcludeFlagBool") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                    Width = 50
                });

                dataGrid.Columns.Add(new DataGridCheckBoxColumn
                {
                    Header = "디폴트",
                    Binding = new System.Windows.Data.Binding("DefaultFlagBool") { UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged },
                    Width = 60
                });
            }
        }

        /// <summary>
        /// 하단 상태바 생성
        /// </summary>
        private Border CreateStatusBar()
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(0, 15, 0, 0)
            };

            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock
            {
                Text = "💡 ",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            });

            var statusText = new TextBlock
            {
                Text = "준비됨",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };
            _currentStatusTextBlock = statusText;
            panel.Children.Add(statusText);

            border.Child = panel;
            return border;
        }

        #region 이벤트 핸들러

        private void LoadFromDbButton_Click(object sender, RoutedEventArgs e)
        {
            // 🔥 현재 탭에 저장하지 않은 변경사항 확인
            if (HasCurrentTabUnsavedChanges())
            {
                var result = MessageBox.Show(
                    "저장하지 않은 변경사항이 있습니다. 새로고침하면 변경사항이 사라집니다.\n계속하시겠습니까?",
                    "경고",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            var currentTabIndex = QueryTypeTabControl.SelectedIndex;
            LoadQueriesFromDatabase();
            QueryTypeTabControl.SelectedIndex = currentTabIndex;
        }

        private void DeleteQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedQuery == null)
            {
                MessageBox.Show("삭제할 쿼리를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var currentQueries = GetCurrentQueryCollection();
            if (currentQueries == null) return;

            var queryName = _selectedQuery.QueryName;
            
            var result = MessageBox.Show(
                $"'{queryName}' 쿼리를 삭제하시겠습니까?\n\n" +
                "이 작업은 되돌릴 수 없습니다!",
                "삭제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (_selectedQuery.RowNumber > 0)
                    {
                        _database?.DeleteQuery(_selectedQuery.RowNumber);
                    }

                    currentQueries.Remove(_selectedQuery);
                    
                    // 🔥 현재 탭의 수정 추적에서도 제거
                    var modifiedQueries = GetCurrentModifiedCollection();
                    modifiedQueries.Remove(_selectedQuery);
                    
                    _selectedQuery = null;
                    
                    if (_currentQueryCountTextBlock != null)
                    {
                        _currentQueryCountTextBlock.Text = $"{currentQueries.Count}개";
                    }
                    
                    UpdateStatus($"'{queryName}' 쿼리가 삭제되었습니다.", Colors.Orange);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"쿼리 삭제 실패:\n{ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"쿼리 삭제 실패: {ex.Message}", Colors.Red);
                }
            }
        }

        private void QueriesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedQuery = (sender as DataGrid)?.SelectedItem as QueryItem;
            bool hasSelection = _selectedQuery != null;
            
            if (_currentDeleteButton != null)
            {
                _currentDeleteButton.IsEnabled = hasSelection;
            }
            
            if (_currentDuplicateButton != null)
            {
                _currentDuplicateButton.IsEnabled = hasSelection;
            }
            
            if (hasSelection && _selectedQuery != null)
            {
                UpdateStatus($"선택됨: {_selectedQuery.QueryName}", Colors.Blue);
            }
        }

        private void QueriesDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (_currentEditModeBorder != null)
            {
                _currentEditModeBorder.Visibility = Visibility.Visible;
            }
            UpdateStatus("편집 모드: 변경 후 '💾 변경사항 저장' 버튼을 클릭하세요.", Colors.Orange);
        }

        private void QueriesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Row.Item is QueryItem query)
                {
                    // 🔥 현재 탭의 수정 추적 컬렉션에 추가
                    var modifiedQueries = GetCurrentModifiedCollection();
                    modifiedQueries.Add(query);
                }
            }
            
            // 디폴트 체크박스 변경 시 다른 모든 항목의 디폴트를 해제
            if (e.Column != null && e.Column.Header?.ToString() == "디폴트" && !e.Cancel)
            {
                if (e.Row.Item is QueryItem changedQuery)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (changedQuery.DefaultFlagBool && _queryExecutionQueries != null)
                        {
                            var modifiedQueries = GetCurrentModifiedCollection();
                            
                            foreach (var query in _queryExecutionQueries)
                            {
                                if (query != changedQuery && query.DefaultFlagBool)
                                {
                                    query.DefaultFlagBool = false;
                                    modifiedQueries.Add(query);
                                }
                            }
                            
                            _currentDataGrid?.Items.Refresh();
                            UpdateStatus($"'{changedQuery.QueryName}'이(가) 디폴트 폼으로 설정됩니다. '💾 변경사항 저장'을 클릭하세요.", Colors.Orange);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private void SaveEditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 🔥 현재 탭의 수정 추적 컬렉션 사용
                var modifiedQueries = GetCurrentModifiedCollection();
                
                if (modifiedQueries.Count == 0)
                {
                    MessageBox.Show("변경된 항목이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int newCount = 0;
                int updateCount = 0;

                foreach (var query in modifiedQueries.ToList())
                {
                    // 필수 필드 검증
                    if (string.IsNullOrWhiteSpace(query.QueryName) ||
                        string.IsNullOrWhiteSpace(query.UserId) ||
                        string.IsNullOrWhiteSpace(query.Password) ||
                        (string.IsNullOrWhiteSpace(query.TnsName) && string.IsNullOrWhiteSpace(query.Host)))
                    {
                        MessageBox.Show($"'{query.QueryName}'의 필수 정보를 입력해주세요.", "입력 오류", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (query.RowNumber == 0)
                    {
                        _database?.AddQuery(query);
                        newCount++;
                    }
                    else
                    {
                        _database?.UpdateQuery(query);
                        updateCount++;
                    }
                }

                // 🔥 현재 탭의 수정 추적만 초기화
                modifiedQueries.Clear();
                
                if (_currentEditModeBorder != null)
                {
                    _currentEditModeBorder.Visibility = Visibility.Collapsed;
                }
                
                MessageBox.Show($"저장 완료!\n\n신규: {newCount}개\n수정: {updateCount}개", "성공", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                // 현재 탭만 새로고침
                var currentTabIndex = QueryTypeTabControl.SelectedIndex;
                LoadQueriesFromDatabase();
                QueryTypeTabControl.SelectedIndex = currentTabIndex;
                
                UpdateStatus($"변경사항이 저장되었습니다. (신규: {newCount}, 수정: {updateCount})", Colors.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다:\n{ex.Message}", "오류", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"저장 실패: {ex.Message}", Colors.Red);
            }
        }

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            // 🔥 현재 탭의 수정 추적 컬렉션 사용
            var modifiedQueries = GetCurrentModifiedCollection();
            
            if (modifiedQueries.Count == 0)
            {
                MessageBox.Show("변경된 항목이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"변경된 {modifiedQueries.Count}개 항목을 취소하고 다시 로드하시겠습니까?",
                "취소 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var currentTabIndex = QueryTypeTabControl.SelectedIndex;
                LoadQueriesFromDatabase();
                QueryTypeTabControl.SelectedIndex = currentTabIndex;
                
                UpdateStatus("변경사항이 취소되었습니다.", Colors.Gray);
            }
        }

        private void EditQueryButton_InGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is QueryItem query)
            {
                if (query.RowNumber == 0)
                {
                    MessageBox.Show(
                        "신규 쿼리는 먼저 기본 정보를 입력하고 저장한 후\n쿼리를 편집할 수 있습니다.",
                        "안내",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var window = new QueryTextEditWindow(query.Query);
                if (window.ShowDialog() == true)
                {
                    query.Query = window.QueryText;
                    
                    // 🔥 현재 탭의 수정 추적 컬렉션에 추가
                    var modifiedQueries = GetCurrentModifiedCollection();
                    modifiedQueries.Add(query);
                    
                    if (_currentEditModeBorder != null)
                    {
                        _currentEditModeBorder.Visibility = Visibility.Visible;
                    }
                    
                    UpdateStatus($"'{query.QueryName}' 쿼리가 수정되었습니다. '💾 변경사항 저장'을 클릭하세요.", Colors.Orange);
                }
            }
        }

        private void DuplicateQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedQuery == null)
            {
                MessageBox.Show("복제할 쿼리를 선택하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var currentQueries = GetCurrentQueryCollection();
            if (currentQueries == null) return;

            try
            {
                var duplicatedQuery = new QueryItem
                {
                    RowNumber = 0,
                    QueryName = $"{_selectedQuery.QueryName} (복사)",
                    QueryType = _selectedQuery.QueryType,
                    BizName = _selectedQuery.BizName,
                    QueryBizName = _selectedQuery.QueryBizName,
                    Description2 = _selectedQuery.Description2,
                    OrderNumber = _selectedQuery.OrderNumber,
                    ConnectionInfoId = null, // 🔥 접속 정보는 콤보박스에서 선택
                    TnsName = "",
                    Host = "",
                    Port = "",
                    ServiceName = "",
                    UserId = "",
                    Password = "",
                    Query = _selectedQuery.Query,
                    EnabledFlag = _selectedQuery.EnabledFlag,
                    NotifyFlag = _selectedQuery.NotifyFlag,
                    ExcludeFlag = _selectedQuery.ExcludeFlag,
                    DefaultFlag = "N",
                    CountGreaterThan = _selectedQuery.CountGreaterThan,
                    CountEquals = _selectedQuery.CountEquals,
                    CountLessThan = _selectedQuery.CountLessThan,
                    ColumnNames = _selectedQuery.ColumnNames,
                    ColumnValues = _selectedQuery.ColumnValues
                };

                currentQueries.Add(duplicatedQuery);
                
                // 🔥 현재 탭의 수정 추적에 추가
                var modifiedQueries = GetCurrentModifiedCollection();
                modifiedQueries.Add(duplicatedQuery);
                
                if (_currentEditModeBorder != null)
                {
                    _currentEditModeBorder.Visibility = Visibility.Visible;
                }

                if (_currentQueryCountTextBlock != null)
                {
                    _currentQueryCountTextBlock.Text = $"{currentQueries.Count}개";
                }

                if (_currentDataGrid != null)
                {
                    _currentDataGrid.SelectedItem = duplicatedQuery;
                    _currentDataGrid.ScrollIntoView(duplicatedQuery);
                }
                
                UpdateStatus($"'{_selectedQuery.QueryName}' 쿼리가 복제되었습니다.", Colors.Blue);
                
                MessageBox.Show(
                    $"'{_selectedQuery.QueryName}' 쿼리가 복제되었습니다.\n\n" +
                    "복제된 쿼리명: " + duplicatedQuery.QueryName,
                    "복제 완료",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 복제 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"쿼리 복제 실패: {ex.Message}", Colors.Red);
            }
        }

        /// <summary>
        /// 하단 상태바 업데이트
        /// </summary>
        private void UpdateStatus(string message, Color color)
        {
            if (_currentStatusTextBlock != null)
            {
                _currentStatusTextBlock.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
                _currentStatusTextBlock.Foreground = new SolidColorBrush(color);
            }

            _sharedData?.UpdateStatusCallback?.Invoke(message, color);
        }
        
        /// <summary>
        /// 현재 탭의 쿼리 컬렉션 반환
        /// </summary>
        private System.Collections.ObjectModel.ObservableCollection<QueryItem>? GetCurrentQueryCollection()
        {
            return QueryTypeTabControl.SelectedIndex switch
            {
                0 => _queryExecutionQueries,
                1 => _infoQueries,
                2 => _bizQueries,
                _ => null
            };
        }
        
        /// <summary>
        /// 🔥 현재 탭의 수정 추적 컬렉션 반환
        /// </summary>
        private HashSet<QueryItem> GetCurrentModifiedCollection()
        {
            return QueryTypeTabControl.SelectedIndex switch
            {
                0 => _queryExecutionModified,
                1 => _infoQueriesModified,
                2 => _bizQueriesModified,
                _ => new HashSet<QueryItem>()
            };
        }
        
        /// <summary>
        /// 🔥 현재 탭에 변경사항이 있는지 확인
        /// </summary>
        private bool HasCurrentTabUnsavedChanges()
        {
            return GetCurrentModifiedCollection().Count > 0;
        }
        
        /// <summary>
        /// Excel 다운로드 버튼 클릭
        /// </summary>
        private void ExportToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            // 🔥 모든 탭의 쿼리를 한 번에 다운로드
            var totalCount = (_queryExecutionQueries?.Count ?? 0) + 
                            (_infoQueries?.Count ?? 0) + 
                            (_bizQueries?.Count ?? 0);

            if (totalCount == 0)
            {
                MessageBox.Show("다운로드할 쿼리가 없습니다.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 파일 저장 대화상자
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = $"전체쿼리목록_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    DefaultExt = ".xlsx"
                };

                if (saveFileDialog.ShowDialog() != true)
                    return;

                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    int totalSheetCount = 0;

                    // 🔥 쿼리 실행 시트 추가
                    if (_queryExecutionQueries != null && _queryExecutionQueries.Count > 0)
                    {
                        AddQuerySheetToExcel(package, "쿼리 실행", _queryExecutionQueries, true);
                        totalSheetCount++;
                    }

                    // 🔥 정보 조회 시트 추가
                    if (_infoQueries != null && _infoQueries.Count > 0)
                    {
                        AddQuerySheetToExcel(package, "정보 조회", _infoQueries, false);
                        totalSheetCount++;
                    }

                    // 🔥 비즈 조회 시트 추가
                    if (_bizQueries != null && _bizQueries.Count > 0)
                    {
                        AddQuerySheetToExcel(package, "비즈 조회", _bizQueries, false);
                        totalSheetCount++;
                    }

                    if (totalSheetCount == 0)
                    {
                        MessageBox.Show("다운로드할 쿼리가 없습니다.", "알림",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Excel 파일 저장
                    var fileInfo = new FileInfo(saveFileDialog.FileName);
                    package.SaveAs(fileInfo);

                    MessageBox.Show($"Excel 파일이 성공적으로 저장되었습니다.\n\n파일: {fileInfo.Name}\n시트 수: {totalSheetCount}개\n총 쿼리 수: {totalCount}개",
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

                    UpdateStatus($"{totalCount}개 쿼리가 Excel로 다운로드되었습니다. ({totalSheetCount}개 시트)", Colors.Green);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel 파일 생성 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Excel 다운로드 실패: {ex.Message}", Colors.Red);
            }
        }

        /// <summary>
        /// Excel 패키지에 쿼리 시트를 추가합니다.
        /// </summary>
        private void AddQuerySheetToExcel(
            OfficeOpenXml.ExcelPackage package, 
            string sheetName, 
            System.Collections.ObjectModel.ObservableCollection<QueryItem> queries,
            bool isQueryExecutionTab)
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // 헤더 작성
            var headers = new List<string>
            {
                "ID", "그룹명", "비즈명", "쿼리비즈명", "설명", "순번", 
                "접속 정보", "SQL 쿼리"
            };

            // 쿼리 실행 탭만 추가 컬럼
            if (isQueryExecutionTab)
            {
                headers.AddRange(new[] { "실행", "알림", "≥건수", "=건수", "≤건수", "컬럼명", "컬럼값", "포함", "디폴트" });
            }

            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            // 헤더 스타일
            using (var range = worksheet.Cells[1, 1, 1, headers.Count])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 120, 215));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // 데이터 작성
            int row = 2;
            foreach (var query in queries)
            {
                int col = 1;
                
                worksheet.Cells[row, col++].Value = query.RowNumber;
                worksheet.Cells[row, col++].Value = query.QueryName;
                worksheet.Cells[row, col++].Value = query.BizName;
                worksheet.Cells[row, col++].Value = query.QueryBizName;
                worksheet.Cells[row, col++].Value = query.Description2;
                worksheet.Cells[row, col++].Value = query.OrderNumber;
                
                // 접속 정보 이름
                if (query.ConnectionInfoId.HasValue)
                {
                    var connInfo = _connectionInfos.FirstOrDefault(c => c.Id == query.ConnectionInfoId.Value);
                    worksheet.Cells[row, col++].Value = connInfo?.DisplayName ?? "-";
                }
                else
                {
                    worksheet.Cells[row, col++].Value = "-";
                }
                
                worksheet.Cells[row, col++].Value = query.Query;

                // 쿼리 실행 탭 전용 컬럼
                if (isQueryExecutionTab)
                {
                    worksheet.Cells[row, col++].Value = query.EnabledFlag;
                    worksheet.Cells[row, col++].Value = query.NotifyFlag;
                    worksheet.Cells[row, col++].Value = query.CountGreaterThan;
                    worksheet.Cells[row, col++].Value = query.CountEquals;
                    worksheet.Cells[row, col++].Value = query.CountLessThan;
                    worksheet.Cells[row, col++].Value = query.ColumnNames;
                    worksheet.Cells[row, col++].Value = query.ColumnValues;
                    worksheet.Cells[row, col++].Value = query.ExcludeFlag == "N" ? "Y" : "N"; // 포함
                    worksheet.Cells[row, col++].Value = query.DefaultFlag;
                }

                // 테두리 추가
                using (var range = worksheet.Cells[row, 1, row, headers.Count])
                {
                    range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                row++;
            }

            // 열 너비 자동 조정
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // 최소/최대 열 너비 설정
            for (int col = 1; col <= headers.Count; col++)
            {
                var column = worksheet.Column(col);
                if (column.Width < 10)
                    column.Width = 10;
                else if (column.Width > 60)
                    column.Width = 60;
            }

            // SQL 쿼리 컬럼은 더 넓게
            worksheet.Column(8).Width = 80;

            // 틀 고정 (헤더 행)
            worksheet.View.FreezePanes(2, 1);
        }

        private void AddQueryButton_Click(object sender, RoutedEventArgs e, string queryType)
        {
            var currentQueries = GetCurrentQueryCollection();
            if (currentQueries == null) return;

            // 🔥 신규 쿼리 항목 생성 (ID = 0)
            var newQuery = new QueryItem
            {
                RowNumber = 0,
                QueryName = "새 쿼리",
                QueryType = queryType, // 🔥 현재 탭의 쿼리 구분 자동 설정
                BizName = "",
                QueryBizName = "",
                Description2 = "",
                OrderNumber = 0,
                ConnectionInfoId = null, // 🔥 접속 정보는 콤보박스에서 선택
                TnsName = "",
                Host = "",
                Port = "",
                ServiceName = "",
                UserId = "",
                Password = "",
                Query = "",
                EnabledFlag = "Y",
                NotifyFlag = "N",
                ExcludeFlag = "N",
                DefaultFlag = "N",
                CountGreaterThan = "",
                CountEquals = "",
                CountLessThan = "",
                ColumnNames = "",
                ColumnValues = ""
            };

            currentQueries.Add(newQuery);
            
            // 🔥 현재 탭의 수정 추적에 추가
            var modifiedQueries = GetCurrentModifiedCollection();
            modifiedQueries.Add(newQuery);
            
            if (_currentEditModeBorder != null)
            {
                _currentEditModeBorder.Visibility = Visibility.Visible;
            }

            if (_currentQueryCountTextBlock != null)
            {
                _currentQueryCountTextBlock.Text = $"{currentQueries.Count}개";
            }

            if (_currentDataGrid != null)
            {
                _currentDataGrid.SelectedItem = newQuery;
                _currentDataGrid.ScrollIntoView(newQuery);
                _currentDataGrid.CurrentCell = new DataGridCellInfo(newQuery, _currentDataGrid.Columns[1]);
                _currentDataGrid.BeginEdit();
            }

            UpdateStatus("새 쿼리 항목이 추가되었습니다. 정보를 입력하고 '💾 변경사항 저장'을 클릭하세요.", Colors.Blue);
        }
        #endregion
    }
    
    /// <summary>
    /// ConnectionInfoId를 DisplayName으로 변환하는 컨버터
    /// </summary>
    public class ConnectionInfoIdToNameConverter : System.Windows.Data.IValueConverter
    {
        private readonly List<Models.ConnectionInfo> _connectionInfos;

        public ConnectionInfoIdToNameConverter(List<Models.ConnectionInfo> connectionInfos)
        {
            _connectionInfos = connectionInfos;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int id)
            {
                var connInfo = _connectionInfos.FirstOrDefault(c => c.Id == id);
                return connInfo?.DisplayName ?? "(접속 정보 없음)";
            }
            else if (value == null)
            {
                return "(접속 정보 선택 안됨)";
            }
            
            return "(알 수 없음)";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
