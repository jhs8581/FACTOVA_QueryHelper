# FACTOVA Query Helper

Oracle �����ͺ��̽� ���� ���� �� ����͸��� ���� WPF ���ø����̼��Դϴ�.

## �ֿ� ���

### 1. ���� ����
- Excel ���� �Ǵ� SQLite �����ͺ��̽����� ���� �ε�
- ���� �߰�/����/����
- ���Ǻ� ��� �˸� ���� (�Ǽ� üũ, �÷� �� Ȯ�� ��)

### 2. �ڵ� ���� ����
- �ֱ����� ���� �ڵ� ����
- ���ǿ� ���� �˸� �߻�
- ����Ʈ �� �ڵ� ����

### 3. SFC ����͸�
- SFC �ý��� ��� ���� ����͸�
- �ǽð� ���� ������Ʈ
- ���͸� �� �˻� ���

### 4. �ڵ� ������Ʈ
- ���α׷� ���� �� �ڵ� ������Ʈ Ȯ��
- GitHub Releases�� ���� �ֽ� ���� ������Ʈ
- ������ ��Ʈ �ڵ� ǥ��

## �ý��� �䱸����

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (���� ���� ���� �� ���ʿ�)
- Oracle Client (19c �̻� ����)

## ?? ����

�ڼ��� ������ [Docs](Docs/) ������ �����ϼ���:

- **[���� ����](Docs/QUICKSTART.md)** - ó�� ����ڸ� ���� ���̵�
- **[���� ���̵�](Docs/DEPLOYMENT.md)** - GitHub Releases ����
- **[��Ʈ��ũ ����](Docs/NETWORK_DEPLOYMENT.md)** - �系 ��Ʈ��ũ ����
- **[���� ����](Docs/FOLDER_STRUCTURE.md)** - ������Ʈ ���� ����

## ��ġ ���

### ��� 1: ���̳ʸ� �ٿ�ε� (����)
1. [Releases ������](https://github.com/jhs8581/FACTOVA_QueryHelper/releases)���� �ֽ� ���� �ٿ�ε�
2. `FACTOVA_QueryHelper.exe` ���� ����
3. ���� �ǿ��� TNS ���� ��� ����

### ��� 2: �ҽ� ����
```bash
git clone https://github.com/jhs8581/FACTOVA_QueryHelper.git
cd FACTOVA_QueryHelper
dotnet restore
dotnet build -c Release
dotnet run --project FACTOVA_QueryHelper.csproj
```

## ���� ����

### �ʱ� ����
1. **����** �ǿ��� TNS ���� ��� ����
2. **���� ����** �ǿ��� ���� ��� �Ǵ� Excel ���� ��������
3. **�α� �м�** �ǿ��� ���� ����

### ���� ���
1. **���� ����** �� �̵�
2. **DB �ε�** �Ǵ� **Excel ��������**
3. ���� ���� �Է�:
   - ������, SQL ��
   - DB ���� ���� (TNS �Ǵ� ���� ����)
   - �˸� ���� (���û���)

### �ڵ� ����
1. **�α� �м�** �� �̵�
2. ������ ���� ����
3. **�ڵ� ���� Ȱ��ȭ** üũ
4. ���� �ֱ� �Է� (�� ����)
5. **����** ��ư Ŭ��

## ������Ʈ ����

```
FACTOVA_QueryHelper/
������ Controls/                    # UI ��Ʈ��
��   ������ LogAnalysisControl.*     # �α� �м� �� ���� ����
��   ������ QueryManagementControl.* # ���� ����
��   ������ SfcMonitoringControl.*   # SFC ����͸�
��   ������ SettingsControl.*        # ����
������ Database/                    # �����ͺ��̽� ����
��   ������ OracleDatabase.cs        # Oracle ���� �� ���� ����
��   ������ QueryDatabase.cs         # SQLite ���� �����
��   ������ QueryExecutionManager.cs # ���� ���� �Ŵ���
������ Excel/                       # Excel ó��
������ SFC/                         # SFC ����
������ Services/                    # ����
������ Monitoring/                  # ����͸�
������ Updates/                     # ������Ʈ
������ Docs/                        # ?? ����
��   ������ INDEX.md                 # ���� ���
��   ������ QUICKSTART.md            # ���� ����
��   ������ DEPLOYMENT.md            # ���� ���̵�
��   ������ NETWORK_DEPLOYMENT.md    # ��Ʈ��ũ ����
��   ������ FOLDER_STRUCTURE.md      # ���� ����
������ ExampleExcel/                # ���� Excel ����
������ Icons/                       # ������ ���ҽ�
������ .github/workflows/           # GitHub Actions ��ũ�÷ο�
��   ������ release.yml              # �ڵ� ���� �� ����
������ MainWindow.*                 # ���� ������
```

## ���� ȯ��

- Visual Studio 2022
- .NET 8.0
- WPF (Windows Presentation Foundation)
- C# 12.0

### �ֿ� ���̺귯��
- **Oracle.ManagedDataAccess.Core** (23.6.0) - Oracle DB ����
- **Microsoft.Data.Sqlite** (8.0.0) - SQLite �����ͺ��̽�
- **EPPlus** (7.5.0) - Excel ���� ó��
- **System.Management** (8.0.0) - �ý��� ���� ����

## ����

�ڼ��� ���� ������ [Docs/DEPLOYMENT.md](Docs/DEPLOYMENT.md)�� �����ϼ���.

### �ڵ� ���� (GitHub Actions)
```bash
# �±� ���� �� Ǫ��
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions�� �ڵ����� �����ϰ� ����� �����մϴ�.

## ���̼���

�� ������Ʈ�� FACTOVA ���� ������Ʈ�Դϴ�.

## �⿩

���� ����Ʈ�� ��� ������ [Issues](https://github.com/jhs8581/FACTOVA_QueryHelper/issues) �������� �̿����ּ���.

## ���� �α�

### v1.0.0 (2024-01-15)
- �ʱ� ������
- ���� ���� ���
- �ڵ� ���� ���
- SFC ����͸�
- �ڵ� ������Ʈ Ȯ��

---

**������**: FACTOVA IT Team  
**����**: [GitHub Issues](https://github.com/jhs8581/FACTOVA_QueryHelper/issues)
