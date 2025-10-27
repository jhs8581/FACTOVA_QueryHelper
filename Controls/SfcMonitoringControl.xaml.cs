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
    /// SfcMonitoringControl.xaml�� ���� ��ȣ �ۿ� ��
    /// </summary>
    public partial class SfcMonitoringControl : UserControl
    {
        private const string DEFAULT_SFC_QUERY = "SELECT * FROM TB_SFC_MAINFRAME_CONFIG_N A WHERE A.TRANSACTION_TYPE_CODE = 'LOGIN_AUTO' AND SFC_MODE = 'PROD' AND A.CONFIG_REGISTER_YMD = @CONFIG_REGISTER_YMD AND PC_IP_ADDR IN (@PC_IP_ADDR)";
        
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
        /// ���� ������ ���ؽ�Ʈ�� �ʱ�ȭ�մϴ�.
        /// </summary>
        public void Initialize(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            LoadSettings();
            InitializeFilterComboBoxes();
            
            // �����ͱ׸��� ���ε�
            SfcMonitorDataGrid.ItemsSource = _sfcFilteredList;
        }

        /// <summary>
        /// ������ �ε��մϴ�.
        /// </summary>
        private void LoadSettings()
        {
            if (_sharedData == null) return;

            // SFC Excel ���� ��� �ε�
            if (!string.IsNullOrWhiteSpace(_sharedData.Settings.SfcExcelFilePath) && 
                File.Exists(_sharedData.Settings.SfcExcelFilePath))
            {
                SfcExcelFilePathTextBox.Text = _sharedData.Settings.SfcExcelFilePath;
                LoadSfcExcelButton.IsEnabled = true;
            }

            // ���� �ε�
            SfcQueryTextBox.Text = DEFAULT_SFC_QUERY;

            // SFC ���� ���� �ε�
            SfcUserIdTextBox.Text = _sharedData.Settings.SfcUserId;
            SfcPasswordBox.Password = _sharedData.Settings.SfcPassword;

            // SFC ��ȸ ��¥�� ���÷� ����
            ConfigDatePicker.SelectedDate = DateTime.Today;

            // SFC TNS �޺��ڽ� ����
            LoadSfcTnsComboBox();
        }

        private void LoadSfcTnsComboBox()
        {
            if (_sharedData == null || _sharedData.TnsEntries.Count == 0) return;

            SfcTnsComboBox.ItemsSource = _sharedData.TnsEntries.Select(t => t.Name).ToList();
            
            // ����� TNS �̸��� ������ �ش� �׸� ����
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
            // ���� ���� �ʱ�ȭ (�⺻��: OFF�� ����)
            _statusFilterItems.Add(new CheckableComboBoxItem { Text = "��ü", IsChecked = false });
            _statusFilterItems.Add(new CheckableComboBoxItem { Text = "ON", IsChecked = false });
            _statusFilterItems.Add(new CheckableComboBoxItem { Text = "OFF", IsChecked = true });
            FilterStatusComboBox.ItemsSource = _statusFilterItems;

            // BIZACTOR ���� �ʱ�ȭ
            _bizActorFilterItems.Add(new CheckableComboBoxItem { Text = "��ü", IsChecked = true });
            _bizActorFilterItems.Add(new CheckableComboBoxItem { Text = "SQL", IsChecked = false });
            _bizActorFilterItems.Add(new CheckableComboBoxItem { Text = "WIP", IsChecked = false });
            _bizActorFilterItems.Add(new CheckableComboBoxItem { Text = "RPT", IsChecked = false });
            FilterBizActorComboBox.ItemsSource = _bizActorFilterItems;

            // SFC ���� �Ŵ��� �ʱ�ȭ
            if (_sharedData != null)
            {
                _sharedData.SfcFilterManager = new SfcFilterManager(
                    _sfcEquipmentList,
                    _sfcFilteredList,
                    _statusFilterItems,
                    _bizActorFilterItems);
            }

            // �޺��ڽ� �ؽ�Ʈ �ʱ�ȭ
            UpdateFilterComboBoxText();
        }

        #region Excel ���� �� ���� ����

        private void BrowseSfcExcelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData == null) return;

            string? filePath = FileDialogManager.OpenExcelFileDialog("SFC ���� ��� Excel ���� ����");

            if (filePath != null)
            {
                SfcExcelFilePathTextBox.Text = filePath;
                LoadSfcExcelButton.IsEnabled = true;
                
                // ���� ����
                _sharedData.Settings.SfcExcelFilePath = filePath;
                _sharedData.SaveSettingsCallback?.Invoke();
            }
        }

        private void LoadSfcExcelButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== SFC Excel �ε� ���� ===");
            System.Diagnostics.Debug.WriteLine($"Excel ���� ���: {SfcExcelFilePathTextBox.Text}");

            try
            {
                var equipmentList = ExcelManager.LoadSfcEquipmentList(SfcExcelFilePathTextBox.Text);
                
                System.Diagnostics.Debug.WriteLine($"�ε�� ���� ��: {equipmentList.Count}��");

                _sfcEquipmentList.Clear();
                foreach (var item in equipmentList)
                {
                    _sfcEquipmentList.Add(item);
                }

                ExecuteSfcQueryButton.IsEnabled = _sfcEquipmentList.Count > 0;
                ApplySfcFilter();
                UpdateStatus($"{_sfcEquipmentList.Count}���� ���� ������ �ε��߽��ϴ�.", Colors.Green);
                
                System.Diagnostics.Debug.WriteLine("=== SFC Excel �ε� �Ϸ� ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== SFC Excel �ε� ���� ===");
                System.Diagnostics.Debug.WriteLine($"���� �޽���: {ex.Message}");

                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"Excel ���� �ε� ����: {ex.Message}");
                errorMessage.AppendLine();
                errorMessage.AppendLine("�� ����:");
                errorMessage.AppendLine($"- Excel ����: {SfcExcelFilePathTextBox.Text}");
                errorMessage.AppendLine($"- ���� ����: {File.Exists(SfcExcelFilePathTextBox.Text)}");

                MessageBox.Show(errorMessage.ToString(), "SFC Excel �ε� ����",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"Excel ���� �ε� ����: {ex.Message}", Colors.Red);
            }
        }

        private async void ExecuteSfcQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sharedData?.SfcQueryManager == null) return;

            // �Է°� ����
            if (!ValidationHelper.ValidateListNotEmpty(_sfcEquipmentList.ToList(), "���� ���"))
                return;

            if (!ValidationHelper.ValidateSelection(SfcTnsComboBox.SelectedItem, "TNS"))
                return;

            if (!ValidationHelper.ValidateSelection(ConfigDatePicker.SelectedDate, "��ȸ ��¥"))
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
                
                // ���� ����
                _sharedData.Settings.SfcUserId = userId;
                _sharedData.Settings.SfcPassword = password;
                _sharedData.Settings.SfcTnsName = SfcTnsComboBox.SelectedItem?.ToString() ?? "";
                _sharedData.SaveSettingsCallback?.Invoke();

                // SfcQueryManager�� ����Ͽ� ���� ����
                var result = await _sharedData.SfcQueryManager.ExecuteQueryAsync(
                    SfcTnsComboBox.SelectedItem.ToString() ?? "",
                    userId,
                    password,
                    SfcQueryTextBox.Text,
                    ConfigDatePicker.SelectedDate!.Value,
                    _sfcEquipmentList.ToList());

                if (result != null)
                {
                    // ��� ó��
                    _sharedData.SfcQueryManager.ProcessQueryResult(result, _sfcEquipmentList.ToList());

                    // UI ������Ʈ
                    SfcMonitorDataGrid.Items.Refresh();
                    ApplySfcDataGridRowStyle();
                    ApplySfcFilter();

                    // OFF ������ ���� �˸�
                    ShowOffEquipmentNotification();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"���� ���� ����:\n{ex.Message}", "����",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"���� ���� ����: {ex.Message}", Colors.Red);
            }
            finally
            {
                ExecuteSfcQueryButton.IsEnabled = true;
            }
        }

        private void ResetSfcQueryButton_Click(object sender, RoutedEventArgs e)
        {
            SfcQueryTextBox.Text = DEFAULT_SFC_QUERY;
            UpdateStatus("SFC ������ �⺻������ �ʱ�ȭ�Ǿ����ϴ�.", Colors.Green);
        }

        private void ShowOffEquipmentNotification()
        {
            var offEquipments = _sfcEquipmentList.Where(e => e.Status == "OFF").ToList();

            if (offEquipments.Count > 0)
            {
                var message = new StringBuilder();
                message.AppendLine($"?? OFF ���� ���� {offEquipments.Count}�� �߰�");
                message.AppendLine();

                foreach (var equipment in offEquipments)
                {
                    message.AppendLine($"? {equipment.EquipmentName} ({equipment.IpAddress})");
                }

                MessageBox.Show(message.ToString(), "SFC ���� ���� �˸�",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region ���͸� ���� �޼���

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

            // �޺��ڽ� �ؽ�Ʈ ������Ʈ
            UpdateFilterComboBoxText();

            // ���� ���� ����
            var criteria = new SfcFilterManager.FilterCriteria
            {
                IpAddress = FilterIpTextBox.Text,
                EquipmentId = FilterEquipmentIdTextBox.Text,
                EquipmentName = FilterEquipmentNameTextBox.Text
            };

            // ���� ����
            var result = _sharedData.SfcFilterManager.ApplyFilter(criteria);

            // ���� ���� ������Ʈ
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

            // �ؽ�Ʈ ���� �ʱ�ȭ
            FilterIpTextBox.Text = "";
            FilterEquipmentIdTextBox.Text = "";
            FilterEquipmentNameTextBox.Text = "";

            // ���� �Ŵ����� ����Ͽ� �ʱ�ȭ
            _sharedData.SfcFilterManager.ClearAllFilters();
            UpdateFilterComboBoxText();

            // ���� ����
            ApplySfcFilter();
            UpdateStatus("���Ͱ� �ʱ�ȭ�Ǿ����ϴ�.", Colors.Green);
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

        #region ��ƿ��Ƽ �޼���

        private void UpdateStatus(string message, Color color)
        {
            // ���� ������ ���¹� ������Ʈ
            _sharedData?.UpdateStatusCallback?.Invoke(message, color);
        }

        #endregion
    }
}
