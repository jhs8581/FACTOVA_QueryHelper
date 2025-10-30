# FACTOVA Query Helper

Oracle �����ͺ��̽� ���� ���� �� ����͸��� ���� WPF ���ø����̼��Դϴ�.

## �ֿ� ���

### 1. ���� ����
- Excel ���� �Ǵ� SQLite �����ͺ��̽����� ���� �ε�
- ���� �߰�/����/����
- ������ ���� ���� ���� (�Ǽ� üũ, �÷� �� Ȯ�� ��)

### 2. �ڵ� ���� ����
- �ֱ����� ���� �ڵ� ����
- ���ǿ� ���� �˸� ���
- ����Ʈ �� �ڵ� ����

### 3. SFC ����͸�
- SFC �ý��� ��� ���� ����͸�
- �ǽð� ���� ������Ʈ
- ���͸� �� �˻� ���

### 4. �ڵ� ������Ʈ
- ���α׷� ���� �� �ڵ� ������Ʈ Ȯ��
- GitHub Releases�� ���� ������ ������Ʈ
- ������ ��Ʈ �ڵ� ǥ��

## �ý��� �䱸����

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (��ü ���� ���� �� ���ʿ�)
- Oracle Client (19c �̻� ����)

## ��ġ ���

### ��� 1: ������ �ٿ�ε� (����)
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

## ��� ���

### �ʱ� ����
1. **����** �ǿ��� TNS ���� ��� ����
2. **���� ����** �ǿ��� ���� ��� �Ǵ� Excel ���� ��������
3. **�α� �м�** �ǿ��� ���� ����

### ���� ���
1. **���� ����** �� ����
2. **DB �ε�** �Ǵ� **Excel ��������**
3. ���� ���� �Է�:
   - ������, SQL ��
   - DB ���� ���� (TNS �Ǵ� ���� ����)
   - �˸� ���� (���û���)

### �ڵ� ����
1. **�α� �м�** �� ����
2. ������ ���� ����
3. **�ڵ� ���� Ȱ��ȭ** üũ
4. ���� �ֱ� ���� (�� ����)
5. **����** ��ư Ŭ��

## ������Ʈ ����

```
FACTOVA_QueryHelper/
������ Controls/                    # ����� ��Ʈ��
��   ������ LogAnalysisControl.*     # �α� �м� �� ���� ����
��   ������ QueryManagementControl.* # ���� ����
��   ������ SfcMonitoringControl.*   # SFC ����͸�
��   ������ SettingsControl.*        # ����
������ ExampleExcel/                # ���� Excel ����
������ Icons/                       # ������ ���ҽ�
������ .github/workflows/           # GitHub Actions ��ũ�÷ο�
��   ������ release.yml              # �ڵ� ���� �� ����
������ MainWindow.*                 # ���� ������
������ QueryDatabase.cs             # SQLite �����ͺ��̽� ����
������ OracleDatabase.cs            # Oracle ���� �� ���� ����
������ UpdateChecker.cs             # �ڵ� ������Ʈ Ȯ��
������ UpdateNotificationWindow.*   # ������Ʈ �˸� â
������ DEPLOYMENT.md                # ���� ���̵�
```

## ���� ȯ��

- Visual Studio 2022
- .NET 8.0
- WPF (Windows Presentation Foundation)
- C# 12.0

### �ֿ� ���̺귯��
- **Oracle.ManagedDataAccess.Core** (23.26.0) - Oracle DB ����
- **Microsoft.Data.Sqlite** (8.0.0) - SQLite �����ͺ��̽�
- **EPPlus** (7.0.5) - Excel ���� ó��
- **System.Management** (8.0.0) - �ý��� ���� ����

## ����

�ڼ��� ���� ����� [DEPLOYMENT.md](DEPLOYMENT.md)�� �����ϼ���.

### ���� ���� (GitHub Actions)
```bash
# ���� ���� ��
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions�� �ڵ����� �����ϰ� ����� �����մϴ�.

## ���̼���

�� ������Ʈ�� FACTOVA �系 ������Ʈ�Դϴ�.

## �⿩

���� ����Ʈ�� ��� ������ [Issues](https://github.com/jhs8581/FACTOVA_QueryHelper/issues) �������� �̿����ּ���.

## ���� �α�

### v1.0.0 (2024-01-15)
- �ʱ� ������
- ���� ���� ���
- �ڵ� ���� ����
- SFC ����͸�
- �ڵ� ������Ʈ Ȯ��

---

**������**: FACTOVA IT Team  
**����**: [GitHub Issues](https://github.com/jhs8581/FACTOVA_QueryHelper/issues)
