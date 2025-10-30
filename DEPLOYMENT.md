# FACTOVA Query Helper ���� ���̵�

## ����
1. [���� ����](#����-����)
2. [���� ���](#����-���)
3. [�ڵ� ������Ʈ ����](#�ڵ�-������Ʈ-����)
4. [���� ����](#����-����)

---

## ���� ����

### ���� ��ȣ ��Ģ
������Ʈ�� [Semantic Versioning](https://semver.org/lang/ko/)�� �����ϴ�:
- **MAJOR.MINOR.PATCH** ���� (��: 1.0.0, 1.1.0, 2.0.0)
  - **MAJOR**: ���� ȣȯ���� ���� API ����
  - **MINOR**: ���� ȣȯ���� �ִ� ��� �߰�
  - **PATCH**: ���� ȣȯ���� �ִ� ���� ����

### ���� ���� ���
`FACTOVA_QueryHelper.csproj` ���Ͽ��� ������ �����մϴ�:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>
```

---

## ���� ���

### 1. GitHub Actions�� ���� �ڵ� ���� (����)

#### ���� �غ�
- GitHub ����ҿ� �ڵ� Ǫ�ð� �Ϸ�� ���¿��� �մϴ�.
- GitHub Actions�� Ȱ��ȭ�Ǿ� �־�� �մϴ�.

#### ���� ����

**Step 1: ���� ����**
```bash
# 1. ������Ʈ ���Ͽ��� ���� ������Ʈ
# FACTOVA_QueryHelper.csproj ���� ����

# 2. ������� Ŀ��
git add .
git commit -m "Release v1.0.0"
```

**Step 2: Git �±� ���� �� Ǫ��**
```bash
# �±� ����
git tag v1.0.0

# �±� Ǫ�� (�ڵ� ���� Ʈ����)
git push origin v1.0.0
```

**Step 3: GitHub Actions Ȯ��**
1. GitHub ������� **Actions** ������ �̵�
2. **Build and Release** ��ũ�÷ο찡 ���� ������ Ȯ��
3. ���� �Ϸ� �� **Releases** ���������� �� ������ Ȯ��

**Step 4: ������ ��Ʈ ����**
1. GitHub **Releases** �������� �̵�
2. �ڵ� ������ ����� ã�� **Edit** Ŭ��
3. ���� ������ ���� �ۼ�:
   ```markdown
   ## v1.0.0 - 2024-01-15
   
   ### ���ο� ���
   - �ڵ� ������Ʈ Ȯ�� ��� �߰�
   - ���� ���� UI ����
   
   ### ���� ����
   - TNS ���� �ε� ���� ����
   - �޸� ���� ���� �ذ�
   
   ### ���� ����
   - ���� ���� �ӵ� ���
   - UI ������ ����
   ```

#### �ڵ� �����Ǵ� ����
- `FACTOVA_QueryHelper.exe` - ���� ���� ���� (��ü ����)
- `FACTOVA_QueryHelper_v1.0.0.zip` - ��ü ��Ű�� (�����)

---

### 2. ���� ���� (���� ����)

#### Visual Studio���� ����
```bash
# 1. Release ���� ����
dotnet build -c Release

# 2. ���� ���� ���� ����
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  --output ./publish
```

#### PowerShell ���� ��ũ��Ʈ
```powershell
# deploy.ps1
$version = "1.0.0"
$outputPath = ".\publish"
$deployPath = "\\��Ʈ��ũ���\FACTOVA_QueryHelper"

# ����
Write-Host "Building version $version..." -ForegroundColor Cyan
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  --output $outputPath

# ���� ���� ����
Write-Host "Deploying to $deployPath..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $deployPath | Out-Null

# ���� ����
Copy-Item "$outputPath\*" -Destination $deployPath -Recurse -Force

# ���� ���� ����
$version | Out-File "$deployPath\version.txt"

Write-Host "Deployment completed!" -ForegroundColor Green
```

---

## �ڵ� ������Ʈ ����

### ����� ����
1. ���α׷� ���� �� �ڵ����� ������Ʈ Ȯ��
2. �� ������ ������ �˸� â ǥ��
3. **�ٿ�ε�** ��ư Ŭ�� �� ���������� �ٿ�ε� ������ ����
4. **���߿�** ��ư���� �ǳʶٱ� ����

### ���� ����
- **����** �� �� **������Ʈ ����**
- "���α׷� ���� �� �ڵ����� ������Ʈ Ȯ��" üũ�ڽ�
- "���� ������Ʈ Ȯ��" ��ư���� ���� Ȯ�� ����

### ������Ʈ Ȯ�� ��Ȱ��ȭ
```csharp
// AppSettings���� ����
settings.CheckUpdateOnStartup = false;
```

---

## ���� üũ����Ʈ

### ������ �� Ȯ�λ���
- [ ] ��� ��� �׽�Ʈ �Ϸ�
- [ ] ���� ��ȣ ������Ʈ (`FACTOVA_QueryHelper.csproj`)
- [ ] ������ ��Ʈ �ۼ�
- [ ] �����ͺ��̽� ��Ű�� ������� Ȯ��
- [ ] ���� ���� ȣȯ�� Ȯ��
- [ ] Oracle Client ������ Ȯ��

### ������ �� Ȯ�λ���
- [ ] GitHub Release ���������� ���� �ٿ�ε� �׽�Ʈ
- [ ] �� ���� ���� �׽�Ʈ
- [ ] �ڵ� ������Ʈ �˸� �׽�Ʈ
- [ ] ���� �������� ���׷��̵� �׽�Ʈ

---

## ���� �ذ�

### GitHub Actions ���� ����
```bash
# ���ÿ��� ���� �׽�Ʈ
dotnet restore
dotnet build -c Release

# �α� Ȯ��
# GitHub Actions �ǿ��� ���� �α� Ȯ��
```

### ������Ʈ Ȯ�� ����
- ���ͳ� ���� Ȯ��
- GitHub API ���� Ȯ�� (�ð��� 60ȸ)
- ��ȭ�� ���� Ȯ��

### ���� ���� ����
```csharp
// ���� ���� Ȯ��
var version = Assembly.GetExecutingAssembly().GetName().Version;
Console.WriteLine($"Current Version: {version}");
```

---

## ���� ����

### 1.0.0 �� 1.1.0 ������Ʈ ����

```bash
# 1. �ڵ� ���� �� �׽�Ʈ
git add .
git commit -m "Add query filter feature"

# 2. ���� ����
# FACTOVA_QueryHelper.csproj���� <Version>1.1.0</Version>�� ����

# 3. Ŀ�� �� �±�
git add FACTOVA_QueryHelper.csproj
git commit -m "Bump version to 1.1.0"
git tag v1.1.0
git push origin master
git push origin v1.1.0

# 4. GitHub Actions�� �ڵ����� ���� �� ������ ����
# 5. GitHub Releases ���������� ������ ��Ʈ ����
# 6. ����ڴ� ���α׷� ���� �� �ڵ����� ������Ʈ �˸� ����
```

---

## ���� �ڷ�
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Semantic Versioning](https://semver.org/lang/ko/)
- [.NET Publishing Options](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
