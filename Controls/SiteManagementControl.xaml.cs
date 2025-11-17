using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FACTOVA_QueryHelper.Database;
using FACTOVA_QueryHelper.Models;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class SiteManagementControl : UserControl
    {
        private QueryDatabase _database;
        private ObservableCollection<SiteInfo> _sites;
        private bool _hasUnsavedChanges = false;
        
        // 🔥 변경된 항목 추적 (신규 + 수정)
        private HashSet<SiteInfo> _modifiedSites = new HashSet<SiteInfo>();

        // 🔥 TNS 목록 (ComboBox용)
        private SharedDataContext? _sharedData;

        // 저장 완료 시 발생하는 이벤트
        public event EventHandler? SiteInfosSaved;

        public SiteManagementControl()
        {
            InitializeComponent();
            _sites = new ObservableCollection<SiteInfo>();
            SiteDataGrid.ItemsSource = _sites;
        }

        public void Initialize(QueryDatabase database)
        {
            _database = database;
            LoadSites();
        }

        /// <summary>
        /// SharedDataContext를 설정하여 TNS 목록을 ComboBox에 바인딩합니다.
        /// </summary>
        public void SetSharedDataContext(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            
            // 🔥 TNS ComboBox에 ItemsSource 설정
            if (SiteDataGrid.Columns.Count >= 9) // TNS (1.0) 컬럼
            {
                if (SiteDataGrid.Columns[8] is DataGridComboBoxColumn tns10Column)
                {
                    tns10Column.ItemsSource = _sharedData.TnsEntries;
                }
            }
            
            if (SiteDataGrid.Columns.Count >= 10) // TNS (2.0) 컬럼
            {
                if (SiteDataGrid.Columns[9] is DataGridComboBoxColumn tns20Column)
                {
                    tns20Column.ItemsSource = _sharedData.TnsEntries;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ TNS ComboBox ItemsSource 설정 완료 ({_sharedData.TnsEntries.Count}개 TNS)");
        }

        private void LoadSites()
        {
            if (_database == null) return;

            _sites.Clear();
            _modifiedSites.Clear();
            
            try
            {
                var sites = _database.GetAllSites();
                foreach (var site in sites)
                {
                    _sites.Add(site);
                }

                TotalCountText.Text = $"{_sites.Count}개";
                _hasUnsavedChanges = false;
                
                // 🔥 편집 모드 Border 숨김
                EditModeBorder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"사업장 목록 로드 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "저장하지 않은 변경사항이 있습니다. 새로고침하면 변경사항이 사라집니다.\n계속하시겠습니까?",
                    "경고",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            LoadSites();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // 🔥 신규 항목 생성 (ID = 0)
            var newSite = new SiteInfo
            {
                Id = 0, // 신규 항목 표시
                SiteName = "새 사업장",
                RepresentativeFactory = "",
                Organization = "",
                Facility = "",
                WipLineId = "",
                EquipLineId = "",
                IsDefault = 0  // 🔥 표시순번 0으로 초기화
            };

            // 컬렉션에 추가
            _sites.Add(newSite);
            
            // 🔥 수정 목록에 추가 (신규 항목)
            _modifiedSites.Add(newSite);
            
            TotalCountText.Text = $"{_sites.Count}개";
            _hasUnsavedChanges = true;
            
            // 🔥 편집 모드 Border 표시
            EditModeBorder.Visibility = Visibility.Visible;

            // 새로 추가된 행으로 스크롤 & 선택
            SiteDataGrid.SelectedItem = newSite;
            SiteDataGrid.ScrollIntoView(newSite);
            
            System.Diagnostics.Debug.WriteLine($"✅ 신규 사업장 추가: {newSite.SiteName} (ID: {newSite.Id})");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SiteDataGrid.SelectedItem is SiteInfo selectedSite)
            {
                var result = MessageBox.Show(
                    $"'{selectedSite.SiteName}' 사업장을 삭제하시겠습니까?",
                    "삭제 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // ID가 있으면 DB에서도 삭제
                        if (selectedSite.Id > 0)
                        {
                            _database.DeleteSite(selectedSite.Id);
                            
                            // 🔥 삭제 시에도 이벤트 발생
                            SiteInfosSaved?.Invoke(this, EventArgs.Empty);
                            
                            System.Diagnostics.Debug.WriteLine($"🗑️ DB에서 삭제: {selectedSite.SiteName} (ID: {selectedSite.Id})");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"🗑️ 신규 항목 삭제 (DB 저장 전): {selectedSite.SiteName}");
                        }

                        // 컬렉션 및 수정 목록에서 제거
                        _sites.Remove(selectedSite);
                        _modifiedSites.Remove(selectedSite);
                        
                        TotalCountText.Text = $"{_sites.Count}개";

                        MessageBox.Show("사업장이 삭제되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"삭제 실패:\n{ex.Message}", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("삭제할 사업장을 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 🔥 수정된 항목이 없으면 종료
                if (_modifiedSites.Count == 0)
                {
                    MessageBox.Show("변경된 항목이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int newCount = 0;
                int updateCount = 0;

                // 🔥 변경된 항목만 처리
                foreach (var site in _modifiedSites.ToList())
                {
                    // 필수 필드 검증
                    if (string.IsNullOrWhiteSpace(site.SiteName))
                    {
                        MessageBox.Show($"사업장명을 입력해주세요.\n(ID: {site.Id})", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 🔥 ID가 0이면 신규 추가, 아니면 업데이트
                    if (site.Id == 0)
                    {
                        _database.AddSite(site);
                        
                        // 🔥 추가 후 생성된 ID를 가져오기 위해 다시 조회
                        var allSites = _database.GetAllSites();
                        var addedSite = allSites.OrderByDescending(s => s.Id).FirstOrDefault();
                        if (addedSite != null)
                        {
                            site.Id = addedSite.Id;
                        }
                        
                        newCount++;
                        System.Diagnostics.Debug.WriteLine($"✅ 신규 저장: {site.SiteName} (새 ID: {site.Id})");
                    }
                    else
                    {
                        _database.UpdateSite(site);
                        updateCount++;
                        System.Diagnostics.Debug.WriteLine($"✅ 업데이트: {site.SiteName} (ID: {site.Id})");
                    }
                }

                // 🔥 수정 목록 초기화
                _modifiedSites.Clear();
                _hasUnsavedChanges = false;
                
                // 🔥 편집 모드 Border 숨김
                EditModeBorder.Visibility = Visibility.Collapsed;
                
                // 🔥 저장 완료 이벤트 발생
                SiteInfosSaved?.Invoke(this, EventArgs.Empty);
                
                // 성공 메시지
                string message = $"저장 완료!\n\n신규: {newCount}개\n수정: {updateCount}개\n총: {newCount + updateCount}개";
                MessageBox.Show(message, "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                
                System.Diagnostics.Debug.WriteLine($"🔔 SiteInfosSaved event raised (신규: {newCount}, 수정: {updateCount})");
                
                // 🔥 DataGrid 새로고침
                SiteDataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ 저장 오류: {ex.Message}");
            }
        }

        private void CancelChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_modifiedSites.Count == 0)
            {
                MessageBox.Show("변경된 항목이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"변경된 {_modifiedSites.Count}개 항목을 취소하고 다시 로드하시겠습니까?",
                "취소 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoadSites();
                
                // 🔥 편집 모드 Border 숨김
                EditModeBorder.Visibility = Visibility.Collapsed;
                
                System.Diagnostics.Debug.WriteLine("🔄 변경사항 취소 및 다시 로드");
            }
        }

        private void SiteDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                // 🔥 편집된 행을 수정 목록에 추가
                if (e.Row.Item is SiteInfo site)
                {
                    _modifiedSites.Add(site);
                    _hasUnsavedChanges = true;
                    
                    // 🔥 편집 모드 Border 표시
                    EditModeBorder.Visibility = Visibility.Visible;
                    
                    System.Diagnostics.Debug.WriteLine($"📝 사업장 수정됨: {site.SiteName} (ID: {site.Id})");
                    System.Diagnostics.Debug.WriteLine($"   현재 수정된 항목 수: {_modifiedSites.Count}");
                }
            }
        }

        private void SiteDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Selection changed logic if needed
        }
    }
}
