using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// LogAnalysisControl.xaml�� ���� ��ȣ �ۿ� ��
    /// </summary>
    public partial class LogAnalysisControl : UserControl
    {
        private SharedDataContext? _sharedData;
        private DispatcherTimer? _queryTimer;
        private DispatcherTimer? _countdownTimer;
        private int _remainingSeconds;
        private int _totalIntervalSeconds;
        private bool _isAutoQueryRunning = false;

        public LogAnalysisControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ���� ������ ���ؽ�Ʈ�� �ʱ�ȭ�մϴ�.
        /// </summary>
        public void Initialize(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            
            // QueryExecutionManager�� LogAnalysisControl���� �ʱ�ȭ
            // (ResultTabControl�� CreateResultTab�� �� ��Ʈ�ѿ��� ����)
            _sharedData.QueryExecutionManager = new QueryExecutionManager(
                UpdateStatus,
                ResultTabControl,
                _sharedData.TnsEntries,
                _sharedData.Settings,
                CreateResultTab);
            
            LoadSettings();
        }

        /// <summary>
        /// ������ �ε��մϴ�.
        /// </summary>
        private void LoadSettings()
        {
            if (_sharedData == null) return;

            // Excel ���� ��� �ε�
            if (!string.IsNullOrWhiteSpace(_sharedData.Settings.ExcelFilePath) && 
                File.Exists(_sharedData.Settings.ExcelFilePath))
            {
                ExcelFilePathTextBox.Text = _sharedData.Settings.ExcelFilePath;
                LoadQueriesButton.IsEnabled = true;

                // ��Ʈ ��� �ε�
                try
                {
                    var sheets = ExcelQueryReader.GetSheetNames(_sharedData.Settings.ExcelFilePath);
                    SheetComboBox.ItemsSource = sheets;
                    if (sheets.Count > 0)
                    {
                        SheetComboBox.SelectedIndex = 0;
                    }
                }
                catch
                {
                    // ��Ʈ �ε� ���� ����
                }
            }

            // ���� Ÿ�̸� ���� �ε�
            QueryIntervalTextBox.Text = _sharedData.Settings.QueryIntervalSeconds.ToString();

            // �˸� �� �ڵ� ���� ���� ���� �ε�
            StopOnNotificationCheckBox.IsChecked = _sharedData.Settings.StopOnNotification;
        }

        /// <summary>
        /// �ڵ� ���� Ÿ�̸Ӹ� �����մϴ�.
        /// </summary>
        public void StopAutoQuery()
        {
            if (_queryTimer != null)
            {
                _queryTimer.Stop();
                _queryTimer = null;
            }

            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer = null;
            }

            _isAutoQueryRunning = false;
            _remainingSeconds = 0;

            // ī��Ʈ�ٿ� �ؽ�Ʈ �ʱ�ȭ �� �����
            AutoQueryCountdownTextBlock.Text = "";
            AutoQueryCountdownBorder.Visibility = Visibility.Collapsed;

            if (_sharedData != null && _sharedData.LoadedQueries != null)
            {
                StartAutoQueryButton.IsEnabled = _sharedData.LoadedQueries.Count > 0;
            }
            StopAutoQueryButton.IsEnabled = false;
            QueryIntervalTextBox.IsEnabled = true;
            LoadQueriesButton.IsEnabled = true;
            BrowseExcelButton.IsEnabled = true;

            UpdateStatus("�ڵ� ���� ���� ����", Colors.Orange);
        }

        #region Excel ���� ���� �̺�Ʈ �ڵ鷯

        private void BrowseExcelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            string? filePath = FileDialogManager.OpenExcelFileDialog("Excel ���� ���� ����");

            if (filePath != null)
            {
                ExcelFilePathTextBox.Text = filePath;
                LoadQueriesButton.IsEnabled = true;

                try
                {
                    var sheets = ExcelManager.GetSheetNames(filePath);
                    SheetComboBox.ItemsSource = sheets;
                    if (sheets.Count > 0)
                    {
                        SheetComboBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Excel ���� �б� ����:\n{ex.Message}", "����",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // ���� ����
                _sharedData.Settings.ExcelFilePath = filePath;
                _sharedData.SaveSettingsCallback?.Invoke();
            }
        }

        private void LoadQueriesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            System.Diagnostics.Debug.WriteLine("=== ���� �ε� ���� ===");
            System.Diagnostics.Debug.WriteLine($"Excel ���� ���: {ExcelFilePathTextBox.Text}");
            System.Diagnostics.Debug.WriteLine($"���� ��: {StartRowTextBox.Text}");
            System.Diagnostics.Debug.WriteLine($"���õ� ��Ʈ: {SheetComboBox.SelectedItem?.ToString()}");

            if (!ValidationHelper.ValidateStartRow(StartRowTextBox.Text, out int startRow))
            {
                System.Diagnostics.Debug.WriteLine("���� �� ���� ����");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"Excel ���Ͽ��� ���� �ε� ����... (���� ��: {startRow})");
                
                _sharedData.LoadedQueries = ExcelManager.LoadQueries(
                    ExcelFilePathTextBox.Text,
                    SheetComboBox.SelectedItem?.ToString(),
                    startRow);

                System.Diagnostics.Debug.WriteLine($"�ε�� ���� ��: {_sharedData.LoadedQueries.Count}��");
                
                // ���� ���� �޺��ڽ� �ʱ�ȭ
                InitializeQueryFilterComboBox();

                LoadedQueriesTextBlock.Text = $"{_sharedData.LoadedQueries.Count}��";
                ExecuteAllButton.IsEnabled = _sharedData.LoadedQueries.Count > 0;
                StartAutoQueryButton.IsEnabled = _sharedData.LoadedQueries.Count > 0;

                UpdateStatus($"{_sharedData.LoadedQueries.Count}���� ������ �ε��߽��ϴ�.", Colors.Green);
                System.Diagnostics.Debug.WriteLine("=== ���� �ε� �Ϸ� ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== ���� �ε� ���� ===");
                System.Diagnostics.Debug.WriteLine($"���� �޽���: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"���� Ʈ���̽�:\n{ex.StackTrace}");
                
                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"���� �ε� ����: {ex.Message}");
                errorMessage.AppendLine();
                errorMessage.AppendLine("�� ����:");
                errorMessage.AppendLine($"- Excel ����: {ExcelFilePathTextBox.Text}");
                errorMessage.AppendLine($"- ��Ʈ: {SheetComboBox.SelectedItem?.ToString() ?? "(���õ��� ����)"}");
                errorMessage.AppendLine($"- ���� ��: {startRow}");
                errorMessage.AppendLine();
                errorMessage.AppendLine($"���� ��:");
                errorMessage.AppendLine(ex.ToString());

                MessageBox.Show(errorMessage.ToString(), "���� �ε� ����",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"���� �ε� ����: {ex.Message}", Colors.Red);
            }
        }

        #endregion

        #region ���� ���� ���� �޼���

        /// <summary>
        /// ���� ���� �޺��ڽ��� �ʱ�ȭ�մϴ�.
        /// </summary>
        private void InitializeQueryFilterComboBox()
        {
            if (_sharedData == null) return;

            _sharedData.QueryFilterItems.Clear();

            // "��ü" �׸� �߰� (�⺻��: üũ ����)
            _sharedData.QueryFilterItems.Add(new CheckableComboBoxItem 
            { 
                Text = "��ü", 
                IsChecked = false 
            });

            // �� ������ �׸����� �߰�
            foreach (var query in _sharedData.LoadedQueries)
            {
                // ExcludeFlag�� 'Y'�̸� �⺻ üũ
                bool isChecked = string.Equals(query.ExcludeFlag, "Y", StringComparison.OrdinalIgnoreCase);
                
                _sharedData.QueryFilterItems.Add(new CheckableComboBoxItem
                {
                    Text = query.QueryName,
                    IsChecked = isChecked
                });
            }

            QueryFilterComboBox.ItemsSource = _sharedData.QueryFilterItems;
            UpdateQueryFilterComboBoxText();
        }

        /// <summary>
        /// ���� ���� �޺��ڽ��� ǥ�� �ؽ�Ʈ�� ������Ʈ�մϴ�.
        /// </summary>
        private void UpdateQueryFilterComboBoxText()
        {
            if (_sharedData?.QueryFilterItems == null || _sharedData.QueryFilterItems.Count == 0)
            {
                QueryFilterComboBox.Text = "";
                return;
            }

            var checkedItems = _sharedData.QueryFilterItems.Where(item => item.IsChecked && item.Text != "��ü").ToList();
            int totalQueries = _sharedData.QueryFilterItems.Count - 1;
            
            if (checkedItems.Count == 0)
            {
                QueryFilterComboBox.Text = "���� �� ��";
                var allItem = _sharedData.QueryFilterItems.FirstOrDefault(item => item.Text == "��ü");
                if (allItem != null && allItem.IsChecked)
                {
                    allItem.IsChecked = false;
                }
            }
            else if (checkedItems.Count == totalQueries)
            {
                QueryFilterComboBox.Text = "��ü";
                var allItem = _sharedData.QueryFilterItems.FirstOrDefault(item => item.Text == "��ü");
                if (allItem != null && !allItem.IsChecked)
                {
                    allItem.IsChecked = true;
                }
            }
            else
            {
                QueryFilterComboBox.Text = string.Join(", ", checkedItems.Select(item => item.Text));
                var allItem = _sharedData.QueryFilterItems.FirstOrDefault(item => item.Text == "��ü");
                if (allItem != null && allItem.IsChecked)
                {
                    allItem.IsChecked = false;
                }
            }
        }

        private void QueryFilterCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && 
                checkBox.DataContext is CheckableComboBoxItem item)
            {
                HandleQueryFilterCheckBoxChanged(item);
            }
        }

        private void QueryFilterComboBox_DropDownClosed(object sender, EventArgs e)
        {
            UpdateQueryFilterComboBoxText();
        }

        private void HandleQueryFilterCheckBoxChanged(CheckableComboBoxItem changedItem)
        {
            if (_sharedData == null) return;

            if (changedItem.Text == "��ü")
            {
                foreach (var item in _sharedData.QueryFilterItems.Where(i => i.Text != "��ü"))
                {
                    item.IsChecked = changedItem.IsChecked;
                }
            }
            else
            {
                var allItem = _sharedData.QueryFilterItems.FirstOrDefault(item => item.Text == "��ü");
                if (allItem != null)
                {
                    int totalItems = _sharedData.QueryFilterItems.Count - 1;
                    int checkedItems = _sharedData.QueryFilterItems.Count(item => item.IsChecked && item.Text != "��ü");
                    
                    if (checkedItems == totalItems && !allItem.IsChecked)
                    {
                        allItem.IsChecked = true;
                    }
                    else if (checkedItems < totalItems && allItem.IsChecked)
                    {
                        allItem.IsChecked = false;
                    }
                }
            }

            UpdateQueryFilterComboBoxText();
        }

        /// <summary>
        /// ���õ� ���� ����� ��ȯ�մϴ�.
        /// </summary>
        private List<QueryItem> GetSelectedQueries()
        {
            if (_sharedData == null || _sharedData.QueryFilterItems == null || _sharedData.QueryFilterItems.Count == 0)
            {
                return _sharedData?.LoadedQueries ?? new List<QueryItem>();
            }

            var selectedQueryNames = _sharedData.QueryFilterItems
                .Where(item => item.IsChecked && item.Text != "��ü")
                .Select(item => item.Text)
                .ToList();

            if (selectedQueryNames.Count == 0)
            {
                return new List<QueryItem>();
            }

            return _sharedData.LoadedQueries
                .Where(q => selectedQueryNames.Contains(q.QueryName))
                .ToList();
        }

        #endregion

        #region ���� ���� ���� �޼���

        private async void ExecuteAllButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteQueries();
        }

        private async System.Threading.Tasks.Task ExecuteQueries()
        {
            if (_sharedData == null) return;

            var selectedQueries = GetSelectedQueries();
            
            if (!ValidationHelper.ValidateListNotEmpty(selectedQueries, "���õ� ���� ���"))
                return;

            if (_sharedData.QueryExecutionManager == null)
            {
                MessageBox.Show("���� ���� �Ŵ����� �ʱ�ȭ���� �ʾҽ��ϴ�.", "����",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // UI ��Ȱ��ȭ
            SetQueryExecutionUIEnabled(false);

            try
            {
                var result = await _sharedData.QueryExecutionManager.ExecuteQueriesAsync(selectedQueries);

                UpdateStatus(
                    $"��ü �Ϸ�: ���� {result.SuccessCount}��, ���� {result.FailCount}�� (�ҿ�ð�: {result.TotalDuration:F2}��)",
                    result.FailCount > 0 ? Colors.Orange : Colors.Green);

                // �۾� �α� �� ����
                CreateExecutionLogTab(
                    result.ExecutionLogs,
                    result.StartTime,
                    result.TotalDuration,
                    result.SuccessCount,
                    result.FailCount,
                    result.Notifications.Count);

                // ù ��° �� ����
                if (ResultTabControl.Items.Count > 0)
                {
                    ResultTabControl.SelectedIndex = 0;
                }

                // �˸� ǥ��
                if (result.Notifications.Count > 0)
                {
                    ShowNotificationsPopup(result.Notifications);
                }
            }
            finally
            {
                SetQueryExecutionUIEnabled(true);
            }
        }

        private void SetQueryExecutionUIEnabled(bool enabled)
        {
            ExecuteAllButton.IsEnabled = enabled;
            LoadQueriesButton.IsEnabled = enabled;
            BrowseExcelButton.IsEnabled = enabled;
        }

        private void ShowNotificationsPopup(List<string> notifications)
        {
            // �˾��� �߸� üũ�ڽ� ������ ���� �ڵ� ��ȸ Ÿ�̸� ����
            if (_isAutoQueryRunning && StopOnNotificationCheckBox.IsChecked == true)
            {
                StopAutoQuery();
            }

            var message = new StringBuilder();
            message.AppendLine("�˸��� �ֽ��ϴ�:");
            message.AppendLine();

            foreach (var notification in notifications)
            {
                message.AppendLine($"? {notification}");
            }

            MessageBox.Show(message.ToString(), "��ȸ ��� �˸�", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region �ڵ� ���� ���� �޼���

        private void StartAutoQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            var selectedQueries = GetSelectedQueries();
            if (!ValidationHelper.ValidateListNotEmpty(selectedQueries, "���õ� ���� ���"))
                return;

            if (!ValidationHelper.ValidateQueryInterval(QueryIntervalTextBox.Text, out int interval))
                return;

            // ���� ����
            _sharedData.Settings.QueryIntervalSeconds = interval;
            _sharedData.SaveSettingsCallback?.Invoke();

            StartAutoQuery(interval);
        }

        private void StopAutoQueryButton_Click(object sender, RoutedEventArgs e)
        {
            StopAutoQuery();
        }

        private void StartAutoQuery(int intervalSeconds)
        {
            _isAutoQueryRunning = true;
            _totalIntervalSeconds = intervalSeconds;
            _remainingSeconds = intervalSeconds;
            
            // ���� ���� Ÿ�̸� ����
            _queryTimer = new DispatcherTimer();
            _queryTimer.Interval = TimeSpan.FromSeconds(intervalSeconds);
            _queryTimer.Tick += async (s, e) =>
            {
                _remainingSeconds = _totalIntervalSeconds;
                await ExecuteQueries();
            };
            _queryTimer.Start();

            // ī��Ʈ�ٿ� Ÿ�̸� ����
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();

            // ��� �� �� ����
            _ = ExecuteQueries();

            StartAutoQueryButton.IsEnabled = false;
            StopAutoQueryButton.IsEnabled = true;
            QueryIntervalTextBox.IsEnabled = false;
            LoadQueriesButton.IsEnabled = false;
            BrowseExcelButton.IsEnabled = false;

            AutoQueryCountdownBorder.Visibility = Visibility.Visible;
            UpdateCountdownDisplay();

            UpdateStatus($"�ڵ� ���� ���� ���� (�ֱ�: {intervalSeconds}��)", Colors.Green);
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateCountdownDisplay();
            }
        }

        private void UpdateCountdownDisplay()
        {
            if (_remainingSeconds > 0)
            {
                int minutes = _remainingSeconds / 60;
                int seconds = _remainingSeconds % 60;
                
                if (minutes > 0)
                {
                    AutoQueryCountdownTextBlock.Text = $"{minutes}�� {seconds}��";
                }
                else
                {
                    AutoQueryCountdownTextBlock.Text = $"{seconds}��";
                }
            }
            else
            {
                AutoQueryCountdownTextBlock.Text = "���� ��...";
            }
        }

        #endregion

        #region ��� �� ���� �޼���

        private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
        {
            ResultTabControl.Items.Clear();
            UpdateStatus("��� ���� �ʱ�ȭ�Ǿ����ϴ�.", Colors.Gray);
        }

        /// <summary>
        /// ���� ��� ���� �����մϴ�.
        /// </summary>
        private void CreateResultTab(QueryItem queryItem, System.Data.DataTable? result, double duration, string? errorMessage)
        {
            var tabItem = new TabItem
            {
                Header = queryItem.QueryName
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            if (result != null && errorMessage == null)
            {
                // ���� - ������ �׸��� ����
                var dataGrid = new DataGrid
                {
                    AutoGenerateColumns = true,
                    IsReadOnly = true,
                    AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                    GridLinesVisibility = DataGridGridLinesVisibility.All,
                    HeadersVisibility = DataGridHeadersVisibility.All,
                    ItemsSource = result.DefaultView,
                    CanUserSortColumns = true,
                    CanUserResizeColumns = true,
                    CanUserReorderColumns = true,
                    SelectionMode = DataGridSelectionMode.Extended,
                    SelectionUnit = DataGridSelectionUnit.Cell,
                    ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader
                };

                // ���ؽ�Ʈ �޴� �߰�
                var contextMenu = new ContextMenu();
                
                var copyMenuItem = new MenuItem { Header = "���� (Ctrl+C)" };
                copyMenuItem.Click += (s, e) =>
                {
                    try
                    {
                        if (dataGrid.SelectedCells.Count > 0)
                        {
                            dataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.ExcludeHeader;
                            ApplicationCommands.Copy.Execute(null, dataGrid);
                            dataGrid.UnselectAllCells();
                            UpdateStatus("������ ���� ����Ǿ����ϴ�.", Colors.Green);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"���� ����:\n{ex.Message}", "����", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                contextMenu.Items.Add(copyMenuItem);

                var copyWithHeaderMenuItem = new MenuItem { Header = "��� ���� ����" };
                copyWithHeaderMenuItem.Click += (s, e) =>
                {
                    try
                    {
                        if (dataGrid.SelectedCells.Count > 0)
                        {
                            dataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                            ApplicationCommands.Copy.Execute(null, dataGrid);
                            dataGrid.UnselectAllCells();
                            UpdateStatus("��� �����Ͽ� ����Ǿ����ϴ�.", Colors.Green);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"���� ����:\n{ex.Message}", "����", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                contextMenu.Items.Add(copyWithHeaderMenuItem);

                contextMenu.Items.Add(new Separator());

                var selectAllMenuItem = new MenuItem { Header = "��� ���� (Ctrl+A)" };
                selectAllMenuItem.Click += (s, e) => dataGrid.SelectAllCells();
                contextMenu.Items.Add(selectAllMenuItem);

                dataGrid.ContextMenu = contextMenu;

                // Ctrl+C Ű���� ����Ű ����
                dataGrid.PreviewKeyDown += (s, e) =>
                {
                    if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        try
                        {
                            if (dataGrid.SelectedCells.Count > 0)
                            {
                                dataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.ExcludeHeader;
                                ApplicationCommands.Copy.Execute(null, dataGrid);
                                e.Handled = true;
                            }
                        }
                        catch { }
                    }
                };

                Grid.SetRow(dataGrid, 0);
                grid.Children.Add(dataGrid);

                // ���� ǥ��
                var statusPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5)
                };

                statusPanel.Children.Add(new TextBlock
                {
                    Text = $"? {result.Rows.Count}�� �� | {result.Columns.Count}�� �� | �ҿ�ð�: {duration:F2}��",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Colors.Green),
                    VerticalAlignment = VerticalAlignment.Center
                });

                // DB ���� ���� ǥ��
                if (!string.IsNullOrEmpty(queryItem.TnsName))
                {
                    statusPanel.Children.Add(new TextBlock
                    {
                        Text = $" | TNS: {queryItem.TnsName}, User: {queryItem.UserId}",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Colors.Blue),
                        Margin = new Thickness(5, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }
                else if (!string.IsNullOrEmpty(queryItem.Host))
                {
                    statusPanel.Children.Add(new TextBlock
                    {
                        Text = $" | DB: {queryItem.Host}:{queryItem.Port}/{queryItem.ServiceName}, User: {queryItem.UserId}",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Colors.Blue),
                        Margin = new Thickness(5, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                Grid.SetRow(statusPanel, 1);
                grid.Children.Add(statusPanel);
            }
            else
            {
                // ���� - ���� �޽��� ǥ��
                var errorInfo = new StringBuilder();
                errorInfo.AppendLine("���� ���� ����");
                errorInfo.AppendLine();
                errorInfo.AppendLine($"����: {errorMessage}");
                errorInfo.AppendLine();
                
                if (!string.IsNullOrEmpty(queryItem.TnsName))
                {
                    errorInfo.AppendLine($"TNS: {queryItem.TnsName}");
                }
                else if (!string.IsNullOrEmpty(queryItem.Host))
                {
                    errorInfo.AppendLine($"DB: {queryItem.Host}:{queryItem.Port}/{queryItem.ServiceName}");
                }
                
                errorInfo.AppendLine($"User ID: {queryItem.UserId}");
                errorInfo.AppendLine();
                errorInfo.AppendLine("����:");
                errorInfo.AppendLine(queryItem.Query);

                var errorTextBox = new TextBox
                {
                    Text = errorInfo.ToString(),
                    IsReadOnly = true,
                    Background = new SolidColorBrush(Color.FromRgb(255, 240, 240)),
                    Foreground = new SolidColorBrush(Colors.Red),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    FontFamily = new FontFamily("Consolas"),
                    Padding = new Thickness(10)
                };

                Grid.SetRow(errorTextBox, 0);
                grid.Children.Add(errorTextBox);

                var statusText = new TextBlock
                {
                    Text = "? ���� ����",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Colors.Red),
                    Margin = new Thickness(5)
                };

                Grid.SetRow(statusText, 1);
                grid.Children.Add(statusText);

                // �� ����� ���������� ǥ��
                tabItem.Foreground = new SolidColorBrush(Colors.Red);
            }

            tabItem.Content = grid;
            ResultTabControl.Items.Add(tabItem);
        }

        private void CreateExecutionLogTab(List<string> logs, DateTime startTime, double totalDuration, 
            int successCount, int failCount, int notificationCount)
        {
            var tabItem = new TabItem
            {
                Header = "?? �۾� �α�",
                FontWeight = FontWeights.Bold
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // ��� ��� �г�
            var summaryBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 248, 255)),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var summaryPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"? ����: {startTime:HH:mm:ss}",
                FontSize = 12,
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"?? �ҿ�ð�: {totalDuration:F2}��",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Blue),
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            summaryPanel.Children.Add(new TextBlock
            {
                Text = $"? ����: {successCount}��",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Green),
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            if (failCount > 0)
            {
                summaryPanel.Children.Add(new TextBlock
                {
                    Text = $"? ����: {failCount}��",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.Red),
                    Margin = new Thickness(0, 0, 20, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            if (notificationCount > 0)
            {
                summaryPanel.Children.Add(new TextBlock
                {
                    Text = $"?? �˸�: {notificationCount}��",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.Orange),
                    Margin = new Thickness(0, 0, 20, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            summaryBorder.Child = summaryPanel;
            Grid.SetRow(summaryBorder, 0);
            grid.Children.Add(summaryBorder);

            // �α� ����
            var logTextBox = new TextBox
            {
                Text = string.Join(Environment.NewLine, logs),
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap,
                Padding = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
            };

            Grid.SetRow(logTextBox, 1);
            grid.Children.Add(logTextBox);

            tabItem.Content = grid;
            ResultTabControl.Items.Insert(0, tabItem);
        }

        #endregion

        #region ��ƿ��Ƽ �޼���

        private void UpdateStatus(string message, Color color)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = new SolidColorBrush(color);
            
            // ���� ������ ���¹ٵ� ������Ʈ
            _sharedData?.UpdateStatusCallback?.Invoke(message, color);
        }

        #endregion
    }
}
