using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FACTOVA_QueryHelper.Database;
using FACTOVA_QueryHelper.SFC;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// 모든 UserControl에서 공유하는 데이터 컨텍스트
    /// </summary>
    public class SharedDataContext
    {
        public AppSettings Settings { get; set; }
        public List<TnsEntry> TnsEntries { get; set; }
        public List<QueryItem> LoadedQueries { get; set; }
        public ObservableCollection<CheckableComboBoxItem> QueryFilterItems { get; set; }
        
        // 각 컨트롤에서 사용하는 Manager들
        public QueryExecutionManager? QueryExecutionManager { get; set; }
        public SfcQueryManager? SfcQueryManager { get; set; }
        public SfcFilterManager? SfcFilterManager { get; set; }
        
        // 상태 업데이트 콜백
        public Action<string, System.Windows.Media.Color>? UpdateStatusCallback { get; set; }
        
        // 설정 저장 콜백
        public Action? SaveSettingsCallback { get; set; }

        public SharedDataContext()
        {
            Settings = new AppSettings();
            TnsEntries = new List<TnsEntry>();
            LoadedQueries = new List<QueryItem>();
            QueryFilterItems = new ObservableCollection<CheckableComboBoxItem>();
        }
    }
}
