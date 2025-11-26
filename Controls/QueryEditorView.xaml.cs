using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FACTOVA_QueryHelper.Models;
using FACTOVA_QueryHelper.Services;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class QueryEditorView : UserControl
    {
        private readonly OracleDbService _dbService;
        private MetadataCacheService? _cacheService;
        private ObservableCollection<string> _tables;
        private SharedDataContext? _sharedData;  // 🔥 SharedDataContext 추가

        public QueryEditorView()
        {
            InitializeComponent();
            _dbService = new OracleDbService();
            _tables = new ObservableCollection<string>();

            InitializeQueryExecutors();
            LoadSettings();
        }

        /// <summary>
        /// 🔥 SharedDataContext 설정
        /// </summary>
        public void SetSharedDataContext(SharedDataContext sharedData)
        {
            _sharedData = sharedData;
            
            // 모든 QueryExecutor에 SharedDataContext 설정
            QueryExecutor1.SetSharedDataContext(sharedData);
            QueryExecutor2.SetSharedDataContext(sharedData);
            QueryExecutor3.SetSharedDataContext(sharedData);
            QueryExecutor4.SetSharedDataContext(sharedData);
            QueryExecutor5.SetSharedDataContext(sharedData);
            QueryExecutor6.SetSharedDataContext(sharedData);
            QueryExecutor7.SetSharedDataContext(sharedData);
            QueryExecutor8.SetSharedDataContext(sharedData);
            QueryExecutor9.SetSharedDataContext(sharedData);
            QueryExecutor10.SetSharedDataContext(sharedData);
            
            System.Diagnostics.Debug.WriteLine("✅ SharedDataContext set to all QueryExecutors in QueryEditorView");
        }

        private void InitializeQueryExecutors()
        {
            // 쿼리 실행 시 DbService 설정
            QueryExecutor1.SetDbService(_dbService);
            QueryExecutor2.SetDbService(_dbService);
            QueryExecutor3.SetDbService(_dbService);
            QueryExecutor4.SetDbService(_dbService);
            QueryExecutor5.SetDbService(_dbService);
            QueryExecutor6.SetDbService(_dbService);
            QueryExecutor7.SetDbService(_dbService);
            QueryExecutor8.SetDbService(_dbService);
            QueryExecutor9.SetDbService(_dbService);
            QueryExecutor10.SetDbService(_dbService);
        }

        /// <summary>
        /// 모든 QueryExecutor에 캐시 서비스 설정
        /// </summary>
        private void SetCacheServiceToAllExecutors()
        {
            QueryExecutor1.SetCacheService(_cacheService);
            QueryExecutor2.SetCacheService(_cacheService);
            QueryExecutor3.SetCacheService(_cacheService);
            QueryExecutor4.SetCacheService(_cacheService);
            QueryExecutor5.SetCacheService(_cacheService);
            QueryExecutor6.SetCacheService(_cacheService);
            QueryExecutor7.SetCacheService(_cacheService);
            QueryExecutor8.SetCacheService(_cacheService);
            QueryExecutor9.SetCacheService(_cacheService);
            QueryExecutor10.SetCacheService(_cacheService);
            
            System.Diagnostics.Debug.WriteLine($"✅ CacheService set to all QueryExecutors");
        }


        /// <summary>
        /// 모든 QueryExecutor의 ConnectionInfo 새로고침
        /// </summary>
        public void RefreshAllQueryExecutorConnections()
        {
            System.Diagnostics.Debug.WriteLine("🔄 Refreshing all QueryExecutor connections...");
            
            QueryExecutor1.RefreshConnectionInfos();
            QueryExecutor2.RefreshConnectionInfos();
            QueryExecutor3.RefreshConnectionInfos();
            QueryExecutor4.RefreshConnectionInfos();
            QueryExecutor5.RefreshConnectionInfos();
            QueryExecutor6.RefreshConnectionInfos();
            QueryExecutor7.RefreshConnectionInfos();
            QueryExecutor8.RefreshConnectionInfos();
            QueryExecutor9.RefreshConnectionInfos();
            QueryExecutor10.RefreshConnectionInfos();
            
            System.Diagnostics.Debug.WriteLine("✅ All QueryExecutor connections refreshed");
        }
        
        /// <summary>
        /// 🔥 모든 QueryExecutor의 테이블 단축어 재로드
        /// </summary>
        public void ReloadAllShortcuts()
        {
            System.Diagnostics.Debug.WriteLine("🔄 Reloading all QueryExecutor shortcuts...");
            
            if (_sharedData == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ SharedData is null, cannot reload shortcuts");
                return;
            }
            
            var dbPath = _sharedData.Settings.DatabasePath;
            
            QueryExecutor1.ReloadShortcuts(dbPath);
            QueryExecutor2.ReloadShortcuts(dbPath);
            QueryExecutor3.ReloadShortcuts(dbPath);
            QueryExecutor4.ReloadShortcuts(dbPath);
            QueryExecutor5.ReloadShortcuts(dbPath);
            QueryExecutor6.ReloadShortcuts(dbPath);
            QueryExecutor7.ReloadShortcuts(dbPath);
            QueryExecutor8.ReloadShortcuts(dbPath);
            QueryExecutor9.ReloadShortcuts(dbPath);
            QueryExecutor10.ReloadShortcuts(dbPath);
            
            System.Diagnostics.Debug.WriteLine("✅ All QueryExecutor shortcuts reloaded");
        }

        private void LoadSettings()
        {
            UpdateConnectionStatus(false);
            
            // 프로그램 시작 시 자동으로 캐시 로드 시도
            _ = TryAutoLoadCacheAsync();
        }

        /// <summary>
        /// 프로그램 시작 시 자동으로 캐시 로드 시도
        /// </summary>
        private async Task TryAutoLoadCacheAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("✅ QueryEditorView initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Initialization warning: {ex.Message}");
            }
        }

        private void UpdateConnectionStatus(bool isConfigured, string detail = "")
        {
            if (isConfigured)
            {
                ConnectionStatusText.Text = "Cached (Offline)";
                ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36));
                ConnectionStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                ConnectionStatusBorder.Background = new SolidColorBrush(Color.FromRgb(212, 237, 218));
                ConnectionStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(195, 230, 203));
                
                ClearCacheButton.IsEnabled = true;
            }
            else
            {
                ConnectionStatusText.Text = "No Cache";
                ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36));
                ConnectionStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                ConnectionStatusBorder.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218));
                ConnectionStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 198, 203));
                CacheStatusText.Text = "";
                
                ClearCacheButton.IsEnabled = false;
            }
        }

        private void UpdateConnectionStatusLoading(string message = "Loading...")
        {
            ConnectionStatusText.Text = message;
            ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(133, 100, 4));
            ConnectionStatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            ConnectionStatusBorder.Background = new SolidColorBrush(Color.FromRgb(255, 243, 205));
            ConnectionStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 234, 167));
            CacheStatusText.Text = "";
        }

        private void UpdateCacheStatus(CacheInfo cacheInfo)
        {
            if (cacheInfo.Exists)
            {
                CacheStatusText.Text = $"💾 캐시: {cacheInfo.TableCount}개 테이블 | {cacheInfo.FileSizeMB:F2} MB | {cacheInfo.LastModified:yyyy-MM-dd HH:mm}";
            }
            else
            {
                CacheStatusText.Text = "";
            }
        }

        private async void LoadCacheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== LoadCacheButton_Click START ===");
                
                // 캐시 폴더의 모든 캐시 파일 찾기
                var cacheDir = MetadataCacheService.GetCacheDirectory();
                System.Diagnostics.Debug.WriteLine($"Cache Directory: {cacheDir}");
                
                if (!System.IO.Directory.Exists(cacheDir))
                {
                    MessageBox.Show("캐시 폴더가 존재하지 않습니다.\n쿼리 생성기에서 먼저 캐시를 빌드해주세요.", 
                        "캐시 없음", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var cacheFiles = System.IO.Directory.GetFiles(cacheDir, "*_metadata.json");
                System.Diagnostics.Debug.WriteLine($"Found {cacheFiles.Length} cache files");
                
                if (cacheFiles.Length == 0)
                {
                    MessageBox.Show("캐시 파일이 없습니다.\n쿼리 생성기에서 먼저 캐시를 빌드해주세요.", 
                        "캐시 없음", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 가장 최근 파일 자동 선택
                var cacheOptions = new System.Collections.Generic.List<(string FileName, DateTime LastModified, long FileSize)>();
                foreach (var file in cacheFiles)
                {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                    var fileInfo = new System.IO.FileInfo(file);
                    cacheOptions.Add((fileName, fileInfo.LastWriteTime, fileInfo.Length));
                }

                var latestCache = cacheOptions.OrderByDescending(c => c.LastModified).First();
                System.Diagnostics.Debug.WriteLine($"🔥 Auto-loading latest cache: {latestCache.FileName}");

                LoadCacheButton.IsEnabled = false;
                UpdateConnectionStatusLoading("캐시 로드 중...");

                // 캐시 서비스 초기화
                var dbIdentifier = latestCache.FileName.Replace("_metadata", "");
                _cacheService = new MetadataCacheService(_dbService, dbIdentifier);
                
                var metadata = await _cacheService.LoadFromCacheAsync();

                if (metadata != null)
                {
                    _tables.Clear();
                    var tableNames = _cacheService.GetAllTableNames();
                    
                    foreach (var table in tableNames)
                    {
                        _tables.Add(table);
                    }

                    var cacheInfo = _cacheService.GetCacheInfo();
                    UpdateConnectionStatus(true, $"캐시 로드 완료 | 테이블: {tableNames.Count}개");
                    UpdateCacheStatus(cacheInfo);

                    // 모든 QueryExecutor에 테이블 목록 등록
                    RegisterTableNamesToAllExecutors();
                    
                    // 모든 QueryExecutor에 CacheService 설정
                    SetCacheServiceToAllExecutors();
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Cache loaded successfully: {tableNames.Count} tables");
                }
                else
                {
                    UpdateConnectionStatus(false, "캐시 로드 실패");
                    MessageBox.Show("캐시 파일을 로드할 수 없습니다.", 
                        "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception in LoadCacheButton_Click: {ex.Message}");
                UpdateConnectionStatus(false, "캐시 로드 오류");
                MessageBox.Show($"캐시 로드 중 오류가 발생했습니다:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadCacheButton.IsEnabled = true;
            }
        }

        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cacheService == null)
                {
                    MessageBox.Show("캐시 서비스가 초기화되지 않았습니다.", "정보", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var cacheInfo = _cacheService.GetCacheInfo();
                
                if (!cacheInfo.Exists)
                {
                    MessageBox.Show("캐시 파일이 없습니다.", "정보", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"캐시 파일을 삭제하시겠습니까?\n\n" +
                    $"테이블: {cacheInfo.TableCount}개\n" +
                    $"파일 크기: {cacheInfo.FileSizeMB:F2} MB\n" +
                    $"마지막 수정: {cacheInfo.LastModified:yyyy-MM-dd HH:mm:ss}",
                    "캐시 삭제 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                _cacheService.ClearCache();
                _tables.Clear();

                UpdateConnectionStatus(false, "캐시가 삭제되었습니다.");

                MessageBox.Show("캐시 파일이 삭제되었습니다.", 
                    "캐시 삭제 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"캐시 삭제 중 오류가 발생했습니다:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OpenCacheFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cacheDir = MetadataCacheService.GetCacheDirectory();
                
                // 디렉토리가 없으면 생성
                if (!System.IO.Directory.Exists(cacheDir))
                {
                    System.IO.Directory.CreateDirectory(cacheDir);
                }

                // 탐색기로 폴더 열기
                System.Diagnostics.Process.Start("explorer.exe", cacheDir);
                
                System.Diagnostics.Debug.WriteLine($"📂 Opened cache folder: {cacheDir}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"캐시 폴더를 열 수 없습니다:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 모든 QueryExecutor에 테이블 목록 등록
        /// </summary>
        private void RegisterTableNamesToAllExecutors()
        {
            if (_tables == null || _tables.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No tables to register");
                return;
            }

            var tableList = _tables.ToList();
            
            QueryExecutor1.RegisterTableNames(tableList);
            QueryExecutor2.RegisterTableNames(tableList);
            QueryExecutor3.RegisterTableNames(tableList);
            QueryExecutor4.RegisterTableNames(tableList);
            QueryExecutor5.RegisterTableNames(tableList);
            QueryExecutor6.RegisterTableNames(tableList);
            QueryExecutor7.RegisterTableNames(tableList);
            QueryExecutor8.RegisterTableNames(tableList);
            QueryExecutor9.RegisterTableNames(tableList);
            QueryExecutor10.RegisterTableNames(tableList);
            
            System.Diagnostics.Debug.WriteLine($"✅ Registered {tableList.Count} table names to all QueryExecutors");
        }

        /// <summary>
        /// 탭 이름 변경 이벤트 핸들러
        /// </summary>
        private void RenameTab_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;
                if (menuItem?.Tag == null)
                    return;

                int tabIndex = Convert.ToInt32(menuItem.Tag);
                var tabItem = QueryTabControl.Items[tabIndex] as TabItem;
                
                if (tabItem == null)
                    return;

                // 현재 탭 이름
                string currentName = tabItem.Header?.ToString() ?? $"Query {tabIndex + 1}";

                // 이름 입력 다이얼로그
                var dialog = new TabRenameDialog(currentName);
                if (dialog.ShowDialog() == true)
                {
                    string newName = dialog.NewTabName;
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        tabItem.Header = newName;
                        System.Diagnostics.Debug.WriteLine($"✅ Tab {tabIndex + 1} renamed to: {newName}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error renaming tab: {ex.Message}");
                MessageBox.Show($"탭 이름 변경 중 오류가 발생했습니다:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
