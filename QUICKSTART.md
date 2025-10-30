# ù ���� ���� ���̵�

�� ������ **ó������ ����� ����**�ϴ� ����� ������ �����մϴ�.

## ?? ���� üũ����Ʈ

- [ ] Visual Studio���� ������Ʈ�� ���� �����
- [ ] GitHub ����ҿ� �ڵ尡 Ǫ�õ�
- [ ] GitHub Actions�� Ȱ��ȭ�� (����� Settings �� Actions �� Allow all actions)

## ?? ���� �ܰ�

### 1�ܰ�: ������Ʈ ���� Ȯ��/����

`FACTOVA_QueryHelper.csproj` ������ ���� ������ Ȯ���մϴ�:

```xml
<Version>1.0.0</Version>
```

ù ������� `1.0.0`���� �����ϼ���.

### 2�ܰ�: �ڵ� Ŀ��

```bash
# ��� ������� �߰�
git add .

# Ŀ��
git commit -m "Release v1.0.0"

# GitHub�� Ǫ��
git push origin master
```

### 3�ܰ�: Git �±� ���� �� Ǫ��

```bash
# �±� ���� (v���λ� �ʼ�!)
git tag v1.0.0

# �±� Ǫ�� (�� ����� �ڵ� ���带 �����մϴ�)
git push origin v1.0.0
```

### 4�ܰ�: GitHub���� ���� Ȯ��

1. ���������� GitHub ����ҷ� �̵�
2. **Actions** �� Ŭ��
3. **Build and Release** ��ũ�÷ο찡 ���� ������ Ȯ��
4. ��� üũ ǥ�ð� ��Ÿ�� ������ ��� (�� 3-5��)

### 5�ܰ�: ������ Ȯ�� �� ����

1. **Releases** �� Ŭ��
2. `v1.0.0` ����� �����Ǿ����� Ȯ��
3. **Edit** ��ư Ŭ��
4. ������ ��Ʈ �ۼ�:

```markdown
## FACTOVA Query Helper v1.0.0

### �ֿ� ���
- Oracle �����ͺ��̽� ���� ����
- �ڵ� ���� ���� �� ����͸�
- SFC ��� ���� ����͸�
- �ڵ� ������Ʈ Ȯ��

### ��ġ ���
1. FACTOVA_QueryHelper.exe ������ �ٿ�ε��մϴ�.
2. ���ϴ� ��ġ�� �����ϰ� �����մϴ�.
3. ���� �ǿ��� TNS ���� ��θ� �����մϴ�.

### �ý��� �䱸����
- Windows 10/11 (64-bit)
- Oracle Client 19c �̻�
```

5. **Update release** ��ư Ŭ��

### 6�ܰ�: �ٿ�ε� �׽�Ʈ

1. **Releases** ���������� `FACTOVA_QueryHelper.exe` �ٿ�ε�
2. �����Ͽ� ���� �۵� Ȯ��
3. **����** �޴� �� **����** ���� ���� Ȯ��

## ? �Ϸ�!

���� ����ڵ��� ���α׷��� �����ϸ� �ڵ����� ������Ʈ �˸��� �ް� �˴ϴ�.

## ?? ���� ������Ʈ �����ϱ�

�� ��° ��������ʹ� �� �����մϴ�:

```bash
# 1. ���� ���� (FACTOVA_QueryHelper.csproj)
<Version>1.1.0</Version>

# 2. Ŀ�� �� Ǫ��
git add .
git commit -m "Release v1.1.0"
git push origin master

# 3. �±� ���� �� Ǫ��
git tag v1.1.0
git push origin v1.1.0

# 4. GitHub���� �ڵ� ���� �Ϸ� ���
# 5. ������ ��Ʈ ����
```

## ? ���� �ذ�

### "GitHub Actions ��ũ�÷ο찡 ������� �ʾƿ�"
- ����� Settings �� Actions �� Allow all actions Ȯ��
- `.github/workflows/release.yml` ������ master �귣ġ�� �ִ��� Ȯ��

### "GitHub release failed with status: 403" ����
�� ������ ���� �����Դϴ�. ���� �� ���� ������� �ذ��� �� �ֽ��ϴ�:

**��� 1: ��ũ�÷ο� ���� ���� (����)**
1. GitHub ����ҷ� �̵�
2. **Settings** �� **Actions** �� **General**
3. **Workflow permissions** ���ǿ��� **Read and write permissions** ����
4. **Save** Ŭ��
5. ���� �±� ���� �� �ٽ� Ǫ��:
   ```bash
   git tag -d v0.0.9
   git push origin :refs/tags/v0.0.9
   git tag v0.0.9
   git push origin v0.0.9
   ```

**��� 2: Personal Access Token ���**
1. GitHub ������ �� Settings �� Developer settings �� Personal access tokens �� Tokens (classic)
2. **Generate new token (classic)** Ŭ��
3. **repo** ���� ��ü ����
4. ��ū ���� �� ����
5. ����� Settings �� Secrets and variables �� Actions �� New repository secret
6. Name: `GH_TOKEN`, Value: ������ ��ū
7. ��ũ�÷ο� ���Ͽ��� `GITHUB_TOKEN` ��� `secrets.GH_TOKEN` ���

### "���尡 �����߾��"
1. Actions �ǿ��� ������ ��ũ�÷ο� Ŭ��
2. �α׿��� ���� �޽��� Ȯ��
3. ���ÿ��� `dotnet build -c Release` �����Ͽ� ���� Ȯ��

### "������Ʈ �˸��� ǥ�õ��� �ʾƿ�"
- ���ͳ� ���� Ȯ��
- ���� �� ������Ʈ �������� "�ڵ� Ȯ��" üũ�ڽ� Ȯ��
- GitHub Releases �������� ����� public���� �����Ǿ� �ִ��� Ȯ��

## ?? �߰� �ڷ�

- ���� ���� ���̵�: [DEPLOYMENT.md](DEPLOYMENT.md)
- ������Ʈ ����: [README.md](README.md)
- ���� ����Ʈ: [GitHub Issues](https://github.com/jhs8581/FACTOVA_QueryHelper/issues)
