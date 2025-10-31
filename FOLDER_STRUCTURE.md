# ?? ������Ʈ ���� ����

## ����
�� ������ FACTOVA_QueryHelper ������Ʈ�� ���� ������ ���� ������ �����մϴ�.

## ���� ����

```
FACTOVA_QueryHelper/
������ ?? Controls/           # UI ��Ʈ�� �� ������
��   ������ LogAnalysisControl.xaml/cs      # ���� ���� ��
��   ������ QueryManagementControl.xaml/cs  # ���� ���� ��
��   ������ SfcMonitoringControl.xaml/cs    # SFC ����͸� ��
��   ������ SettingsControl.xaml/cs         # ���� ��
��   ������ QueryTextEditWindow.xaml/cs     # ���� ���� ������
��   ������ NotificationWindow.xaml/cs      # �˸� ������
��   ������ UpdateNotificationWindow.xaml/cs # ������Ʈ �˸� ������
��   ������ SharedDataContext.cs            # ���� ������ ���ؽ�Ʈ
��
������ ?? Database/           # �����ͺ��̽� ����
��   ������ OracleDatabase.cs              # Oracle DB ���� �� ���� ����
��   ������ QueryDatabase.cs               # SQLite ���� �����
��   ������ QueryExecutionManager.cs       # ���� ���� �Ŵ���
��
������ ?? Excel/              # Excel ���� ó��
��   ������ ExcelManager.cs                # Excel ���� ����
��   ������ ExcelQueryReader.cs            # Excel���� ���� �б�
��
������ ?? Models/             # ������ ��
��   ������ CheckableComboBoxItem.cs       # üũ�ڽ� �޺��ڽ� ������
��   ������ QueryItem.cs                   # ���� ������ (ExcelQueryReader.cs�� ����)
��   ������ TnsEntry.cs                    # TNS �׸� (TnsParser.cs�� ����)
��   ������ SfcEquipmentInfo.cs            # SFC ���� ���� (MainWindow.xaml.cs�� ����)
��   ������ AppSettings.cs                 # �� ���� (SettingsManager.cs�� ����)
��
������ ?? SFC/                # SFC ���� ���
��   ������ SfcFilterManager.cs            # SFC ���� ����
��   ������ SfcQueryManager.cs             # SFC ���� ����
��
������ ?? Services/           # ���� ���� �� ��ƿ��Ƽ
��   ������ SettingsManager.cs             # ���� ����
��   ������ TnsParser.cs                   # TNS ���� �ļ�
��   ������ FileDialogManager.cs           # ���� ��ȭ���� ����
��   ������ ValidationHelper.cs            # ��ȿ�� ���� ����
��   ������ QuerySummaryHelper.cs          # ���� ��� ����
��
������ ?? Monitoring/         # ����͸� ����
��   ������ ProcessMonitor.cs              # ���μ��� ����͸�
��   ������ MonitorLogger.cs               # ����͸� �ΰ�
��
������ ?? Updates/            # ������Ʈ ����
��   ������ UpdateChecker.cs               # ������Ʈ Ȯ��
��   ������ UpdateCheckerTest.cs           # ������Ʈ �׽�Ʈ
��
������ ?? Icons/              # ������ ����
������ ?? ExampleExcel/       # ���� Excel ����
������ ?? ������/              # ���� ���� ����
��
������ App.xaml/cs            # ���ø����̼� ������
������ MainWindow.xaml/cs     # ���� ������
������ AssemblyInfo.cs        # ����� ����
```

## ���ӽ����̽� ����

```csharp
FACTOVA_QueryHelper                    // ��Ʈ ���ӽ����̽�
������ FACTOVA_QueryHelper.Controls      // UI ��Ʈ�� �� ������
������ FACTOVA_QueryHelper.Database      // �����ͺ��̽�
������ FACTOVA_QueryHelper.Excel         // Excel ó��
������ FACTOVA_QueryHelper.Models        // ������ ��
������ FACTOVA_QueryHelper.SFC           // SFC ���
������ FACTOVA_QueryHelper.Services      // ����
������ FACTOVA_QueryHelper.Monitoring    // ����͸�
������ FACTOVA_QueryHelper.Updates       // ������Ʈ
```

## �ֿ� ���� ����

### Controls (UI ��Ʈ�� �� ������)
- **LogAnalysisControl**: ���� ���� �� ��� ǥ��
- **QueryManagementControl**: ���� CRUD ����
- **SfcMonitoringControl**: SFC ���� ����͸�
- **SettingsControl**: ���ø����̼� ����
- **QueryTextEditWindow**: ���� SQL ���� ������
- **NotificationWindow**: �˸� �˾� ������
- **UpdateNotificationWindow**: ������Ʈ �˸� ������
- **SharedDataContext**: �� �� ������ ����

### Database
- **OracleDatabase**: Oracle �����ͺ��̽� ���� �� ���� ����
- **QueryDatabase**: SQLite�� ����� ���� �����
- **QueryExecutionManager**: ���� ���� ���� �� �˸� ó��

### Excel
- **ExcelManager**: Excel ���� �б�/����
- **ExcelQueryReader**: Excel���� ���� ���� ����

### Services
- **SettingsManager**: ���ø����̼� ���� ����/�ε�
- **TnsParser**: tnsnames.ora ���� �Ľ�
- **ValidationHelper**: �Է� ��ȿ�� ����
- **FileDialogManager**: ���� ���� ��ȭ���� ����
- **QuerySummaryHelper**: ���� ��� ���

### SFC
- **SfcQueryManager**: SFC ���� ���� ����
- **SfcFilterManager**: SFC ������ ���͸�

### Monitoring
- **ProcessMonitor**: �ܺ� ���μ��� ����͸�
- **MonitorLogger**: ����͸� �α� ���

### Updates
- **UpdateChecker**: GitHub ���������� ������Ʈ Ȯ��
- **UpdateCheckerTest**: ������Ʈ ��� �׽�Ʈ

## ���� ����

```
MainWindow
  ������ Controls (��� UI ��Ʈ�� �� ������)
  ��     ������ Database (OracleDatabase, QueryDatabase)
  ��     ������ Excel (ExcelManager, ExcelQueryReader)
  ��     ������ SFC (SfcQueryManager, SfcFilterManager)
  ��     ������ Services (SettingsManager, ValidationHelper, etc.)
  ��
  ������ Updates (UpdateChecker)
```

## �ֱ� �������

### 2025-01-31: ���� ���� ����
- ? **Controls�� Windows ���� ����**: ��� UI ���� ������ Controls ������ ����
- ? **Database ���� ����**: Oracle, SQLite, ���� ���� ���� ���� �и�
- ? **Excel ���� ����**: Excel ó�� ���� ���� �и�
- ? **SFC ���� ����**: SFC ���� ��� �и�
- ? **Monitoring ���� ����**: ����͸� ���� ���� �и�
- ? **Updates ���� ����**: ������Ʈ ���� ���� �и�

## �ڵ� ��Ģ

1. **���ӽ����̽�**: ���� ������ ��ġ�ϴ� ���ӽ����̽� ���
2. **���ϸ�**: Ŭ������� ������ ���ϸ� ��� (PascalCase)
3. **���� ������**: ������ �� ���������� ��� (internal, private)
4. **�ּ�**: XML ���� �ּ� ��� (`/// <summary>`)

## ���� ���� ����

- [ ] Models ������ ���� Ŭ���� ���Ϸ� �� �и�
- [ ] Interfaces ���� �����Ͽ� �������̽� �и�
- [ ] Tests ���� �����Ͽ� ���� �׽�Ʈ �߰�
- [ ] Resources �������� ���ҽ� ����
- [ ] Converters �������� WPF ������ ����
