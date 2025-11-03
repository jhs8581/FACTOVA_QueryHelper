using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// SfcMonitoringControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SfcMonitoringControl : UserControl
    {
        private const string DEFAULT_SFC_QUERY = "SELECT * FROM TB_SFC_MAINFRAME_CONFIG_N A WHERE A.TRANSACTION_TYPE_CODE = 'LOGIN_AUTO' AND SFC_MODE = 'PROD' AND PC_IP_ADDR IN (@PC_IP_ADDR)" + "\r\n" + "/*AND A.CONFIG_REGISTER_YMD = @CONFIG_REGISTER_YMD */";
        
        private SharedDataContext? _sharedData;
        private ObservableCollection<SfcEquipmentInfo> _sfcEquipmentList;
        private ObservableCollection<SfcEquipmentInfo> _sfcFilteredList;
        private ObservableCollection<CheckableComboBoxItem> _statusFilterItems;
        private ObservableCollection<CheckableComboBoxItem> _bizActorFilterItems;

        public SfcMonitoringControl()
        {
            InitializeComponent();
            
            _sfcEquipmentList = new ObservableCollection<SfcEquipmentInfo>();
            _sfcFilteredList = new ObservableCollection<SfcEquipmentInfo>();
            _statusFilterItems = new ObservableCollection<CheckableComboBoxItem>();
            _bizActorFilterItems = new ObservableCollection<CheckableComboBoxItem>();
        }

        /// <summary>
        /// 공유 데이터 컨텍스트를 초기화합니다.
        /// </summary>
        public void Initialize(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            LoadSettings();
            InitializeFilterComboBoxes();
            
            // 데이터그리드 바인딩
            SfcMonitorDataGrid.ItemsSource = _sfcFilteredList;
        }

        /// <summary>
        /// 설정을 로드합니다.
        /// </summary>
        private void LoadSettings()
        {
            if (_sharedData == null) return;

            // SFC Excel 파일 경로 로드
            if (!string.IsNullOrWhiteSpace(_sharedData.Settings.SfcExcelFilePath) && 
                File.Exists(_sharedData.Settings.SfcExcelFilePath))
            {
                SfcExcelFilePathTextBox.Text = _sharedData.Settings.SfcExcelFilePath;
                LoadSfcExcelButton.IsEnabled = true;
            }

            // 쿼리 로드
            SfcQueryTextBox.Text = DEFAULT_SFC_QUERY;

            // SFC 계정 정보 로드
            SfcUserIdTextBox.Text = _sharedData.Settings.SfcUserId;
            SfcPasswordBox.Password = _sharedData.Settings.SfcPassword;

            // SFC 조회 날짜를 오늘로 설정
            ConfigDatePicker.SelectedDate = DateTime.Today;

            // SFC TNS 콤보박스 설정
            LoadSfcTnsComboBox();
        }

        private void LoadSfcTnsComboBox()
        {
            if (_sharedData == null || _sharedData.TnsEntries.Count == 0) return;

            SfcTnsComboBox.ItemsSource = _sharedData.TnsEntries.Select(t => t.Name).ToList();
            
            // 저장된 TNS 이름이 있으면 해당 항목 선택
            if (!string.IsNullOrWhiteSpace(_sharedData.Settings.SfcTnsName))
            {
                var savedIndex = _sharedData.TnsEntries.FindIndex(t => t.Name == _sharedData.Settings.SfcTnsName);
                if (savedIndex >= 0)
                {
                    SfcTnsComboBox.SelectedIndex = savedIndex;
                }
                else if (_sharedData.TnsEntries.Count > 0)
                {
                    SfcTnsComboBox.SelectedIndex = 0;
                }
            }
            else if (_sharedData.TnsEntries.Count > 0)
            {
                SfcTnsComboBox.SelectedIndex = 0;
            }
        }

        private void InitializeFilterComboBoxes()
        {
            // 상태 필터 초기화 (기본값: OFF만 선택)
            _statusFilterItems.Add(new CheckableComboBoxItem { Text = "전체", IsChecked = false });
            _statusFilterItems.Add(new CheckableComboBoxItem { Text = "ON", IsChecked = false });
            _statusFilterItems.Add(new CheckableComboBoxItem { Text = "OFF", IsChecked = true });
            FilterStatusComboBox.ItemsSource = _statusFilterItems;

            // BIZACTOR 필터 초기화
            _bizActorFilterItems.Add(new CheckableComboBoxItem { Text = "전체", IsChecked = true });
            _bizActorFilterItems.Add(new CheckableComboBoxItem { Text = "SQL", IsChecked = false });
            _bizActorFilterItems.Add(new CheckableComboBoxItem { Text = "WIP", IsChecked = false });
            _bizActorFilterItems.Add(new CheckableComboBoxItem { Text = "RPT", IsChecked = false });
            FilterBizActorComboBox.ItemsSource = _bizActorFilterItems;

            // SFC 필터 매니저 초기화
            if (_sharedData != null)
            {
                _sharedData.SfcFilterManager = new SfcFilterManager(
                    _sfcEquipmentList,
                    _sfcFilteredList,
                    _statusFilterItems,
                    _bizActorFilterItems);
            }

            // 콤보박스 텍스트 초기화
            UpdateFilterComboBoxText();
        }

        #region Excel 파일 및 쿼리 실행

        private void BrowseSfcExcelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            string? filePath = FileDialogManager.OpenExcelFileDialog("SFC 설비 목록 Excel 파일 선택");

            if (filePath != null)
            {
                SfcExcelFilePathTextBox.Text = filePath;
                LoadSfcExcelButton.IsEnabled = true;
                
                // 설정 저장
                _sharedData.Settings.SfcExcelFilePath = filePath;
                _sharedData.SaveSettingsCallback?.Invoke();
            }
        }

        private void LoadSfcExcelButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== SFC Excel 로드 시작 ===");
            System.Diagnostics.Debug.WriteLine($"Excel 파일 경로: {SfcExcelFilePathTextBox.Text}");

            try
            {
                var equipmentList = ExcelManager.LoadSfcEquipmentList(SfcExcelFilePathTextBox.Text);
                
                System.Diagnostics.Debug.WriteLine($"로드된 설비 수: {equipmentList.Count}개");

                _sfcEquipmentList.Clear();
                foreach (var item in equipmentList)
                {
                    _sfcEquipmentList.Add(item);
                }

                ExecuteSfcQueryButton.IsEnabled = _sfcEquipmentList.Count > 0;
                ApplySfcFilter();
                UpdateStatus($"{_sfcEquipmentList.Count}개의 설비 정보를 로드했습니다.", Colors.Green);
                
                System.Diagnostics.Debug.WriteLine("=== SFC Excel 로드 완료 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== SFC Excel 로드 실패 ===");
                System.Diagnostics.Debug.WriteLine($"오류 메시지: {ex.Message}");

                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"Excel 파일 로드 실패: {ex.Message}");
                errorMessage.AppendLine();
                errorMessage.AppendLine("상세 정보:");
                errorMessage.AppendLine($"- Excel 파일: {SfcExcelFilePathTextBox.Text}");
                errorMessage.AppendLine($"- 파일 존재: {File.Exists(SfcExcelFilePathTextBox.Text)}");

                MessageBox.Show(errorMessage.ToString(), "SFC Excel 로드 오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Excel 파일 로드 실패: {ex.Message}", Colors.Red);
            }
        }

        private async void ExecuteSfcQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData?.SfcQueryManager == null) return;

            // 입력값 검증
            if (!ValidationHelper.ValidateListNotEmpty(_sfcEquipmentList.ToList(), "설비 목록"))
                return;

            if (!ValidationHelper.ValidateSelection(SfcTnsComboBox.SelectedItem, "TNS"))
                return;

            if (!ValidationHelper.ValidateSelection(ConfigDatePicker.SelectedDate, "조회 날짜"))
                return;

            string userId = SfcUserIdTextBox.Text?.Trim() ?? "";
            string password = SfcPasswordBox.Password ?? "";

            if (!ValidationHelper.ValidateNotEmpty(userId, "User ID"))
            {
                SfcUserIdTextBox.Focus();
                return;
            }

            if (!ValidationHelper.ValidateNotEmpty(password, "Password"))
            {
                SfcPasswordBox.Focus();
                return;
            }

            try
            {
                ExecuteSfcQueryButton.IsEnabled = false;
                
                // 설정 저장
                _sharedData.Settings.SfcUserId = userId;
                _sharedData.Settings.SfcPassword = password;
                _sharedData.Settings.SfcTnsName = SfcTnsComboBox.SelectedItem?.ToString() ?? "";
                _sharedData.SaveSettingsCallback?.Invoke();

                // SfcQueryManager를 사용하여 쿼리 실행
                var result = await _sharedData.SfcQueryManager.ExecuteQueryAsync(
                    SfcTnsComboBox.SelectedItem.ToString() ?? "",
                    userId,
                    password,
                    SfcQueryTextBox.Text,
                    ConfigDatePicker.SelectedDate!.Value,
                    _sfcEquipmentList.ToList());

                if (result != null)
                {
                    // 결과 처리
                    _sharedData.SfcQueryManager.ProcessQueryResult(result, _sfcEquipmentList.ToList());

                    // UI 업데이트
                    SfcMonitorDataGrid.Items.Refresh();
                    ApplySfcDataGridRowStyle();
                    ApplySfcFilter();

                    // OFF 상태인 설비 알림
                    ShowOffEquipmentNotification();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"쿼리 실행 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"쿼리 실행 실패: {ex.Message}", Colors.Red);
            }
            finally
            {
                ExecuteSfcQueryButton.IsEnabled = true;
            }
        }

        private void ResetSfcQueryButton_Click(object sender, RoutedEventArgs e)
        {
            SfcQueryTextBox.Text = DEFAULT_SFC_QUERY;
            UpdateStatus("SFC 쿼리가 기본값으로 초기화되었습니다.", Colors.Green);
        }

        private void ShowOffEquipmentNotification()
        {
            var offEquipments = _sfcEquipmentList.Where(e => e.Status == "OFF").ToList();

            if (offEquipments.Count > 0)
            {
                var message = new StringBuilder();
                message.AppendLine($"OFF 상태 설비 {offEquipments.Count}개 발견");
                message.AppendLine();

                foreach (var equipment in offEquipments)
                {
                    message.AppendLine($"{equipment.EquipmentName} ({equipment.IpAddress})");
                }

                MessageBox.Show(message.ToString(), "SFC 설비 상태 알림",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region 필터링 관련 메서드

        private void FilterCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && 
                checkBox.DataContext is CheckableComboBoxItem item &&
                _sharedData?.SfcFilterManager != null)
            {
                _sharedData.SfcFilterManager.HandleCheckBoxChanged(item);
                UpdateFilterComboBoxText();
            }
        }

        private void ApplyFilter(object sender, EventArgs e)
        {
            ApplySfcFilter();
        }

        private void ApplySfcFilter()
        {
            if (_sharedData?.SfcFilterManager == null ||
                FilterIpTextBox == null || 
                FilterEquipmentIdTextBox == null || 
                FilterEquipmentNameTextBox == null)
                return;

            // 콤보박스 텍스트 업데이트
            UpdateFilterComboBoxText();

            // 필터 조건 생성
            var criteria = new SfcFilterManager.FilterCriteria
            {
                IpAddress = FilterIpTextBox.Text,
                EquipmentId = FilterEquipmentIdTextBox.Text,
                EquipmentName = FilterEquipmentNameTextBox.Text
            };

            // 필터 적용
            var result = _sharedData.SfcFilterManager.ApplyFilter(criteria);

            // 필터 상태 업데이트
            UpdateFilterStatus(result);
        }

        private void UpdateFilterComboBoxText()
        {
            if (_sharedData?.SfcFilterManager == null)
                return;

            var filterText = _sharedData.SfcFilterManager.GetFilterComboBoxText();
            FilterStatusComboBox.Text = filterText.StatusText;
            FilterBizActorComboBox.Text = filterText.BizActorText;
        }

        private void UpdateFilterStatus(FilterResult result)
        {
            if (FilterStatusTextBlock == null || result == null)
                return;

            FilterStatusTextBlock.Text = result.GetStatusMessage();
            FilterStatusTextBlock.Foreground = new SolidColorBrush(
                result.IsFiltered ? Colors.Blue : Colors.Gray);
        }

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData?.SfcFilterManager == null)
                return;

            // 텍스트 필터 초기화
            FilterIpTextBox.Text = "";
            FilterEquipmentIdTextBox.Text = "";
            FilterEquipmentNameTextBox.Text = "";

            // 필터 매니저를 사용하여 초기화
            _sharedData.SfcFilterManager.ClearAllFilters();
            UpdateFilterComboBoxText();

            // 필터 적용
            ApplySfcFilter();
            UpdateStatus("필터가 초기화되었습니다.", Colors.Green);
        }

        private void ApplySfcDataGridRowStyle()
        {
            SfcMonitorDataGrid.LoadingRow -= SfcMonitorDataGrid_LoadingRow;
            SfcMonitorDataGrid.LoadingRow += SfcMonitorDataGrid_LoadingRow;
        }

        private void SfcMonitorDataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            var item = e.Row.Item as SfcEquipmentInfo;
            if (item != null && item.Status == "OFF")
            {
                e.Row.Background = new SolidColorBrush(Colors.LightCoral);
            }
            else
            {
                e.Row.Background = new SolidColorBrush(Colors.White);
            }
        }

        #endregion

        #region 유틸리티 메서드

        private void UpdateStatus(string message, Color color)
        {
            // 메인 윈도우 상태바 업데이트
            _sharedData?.UpdateStatusCallback?.Invoke(message, color);
        }

        #endregion
    }
}
