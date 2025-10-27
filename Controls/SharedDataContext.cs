using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// ��� UserControl���� �����ϴ� ������ ���ؽ�Ʈ
    /// </summary>
    public class SharedDataContext
    {
        public AppSettings Settings { get; set; }
        public List<TnsEntry> TnsEntries { get; set; }
        public List<QueryItem> LoadedQueries { get; set; }
        public ObservableCollection<CheckableComboBoxItem> QueryFilterItems { get; set; }
        
        // �� ��Ʈ�ѿ��� ����ϴ� Manager��
        public QueryExecutionManager? QueryExecutionManager { get; set; }
        public SfcQueryManager? SfcQueryManager { get; set; }
        public SfcFilterManager? SfcFilterManager { get; set; }
        
        // ���� ������Ʈ �ݹ�
        public Action<string, System.Windows.Media.Color>? UpdateStatusCallback { get; set; }
        
        // ���� ���� �ݹ�
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
