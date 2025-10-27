using Microsoft.Win32;
using System;
using System.IO;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// ���� ���̾�α� ���� ����� �����ϴ� Ŭ����
    /// </summary>
    public class FileDialogManager
    {
        /// <summary>
        /// Excel ���� ���� ���̾�α׸� ���ϴ�.
        /// </summary>
        public static string? OpenExcelFileDialog(string title = "Excel ���� ����")
        {
            return OpenFileDialog(
                "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                title);
        }

        /// <summary>
        /// TNS ���� ���� ���̾�α׸� ���ϴ�.
        /// </summary>
        public static string? OpenTnsFileDialog(string initialDirectory)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "TNS Names File (tnsnames.ora)|tnsnames.ora|All Files (*.*)|*.*",
                Title = "tnsnames.ora ���� ����",
                InitialDirectory = initialDirectory
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        /// <summary>
        /// �Ϲ� ���� ���� ���̾�α׸� ���ϴ�.
        /// </summary>
        public static string? OpenFileDialog(string filter, string title)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        /// <summary>
        /// ������ �����ϴ��� Ȯ���մϴ�.
        /// </summary>
        public static bool FileExists(string? filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);
        }
    }
}
