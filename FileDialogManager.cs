using Microsoft.Win32;
using System;
using System.IO;

namespace FACTOVA_Palletizing_Analysis
{
    /// <summary>
    /// 파일 다이얼로그 관련 기능을 관리하는 클래스
    /// </summary>
    public class FileDialogManager
    {
        /// <summary>
        /// Excel 파일 선택 다이얼로그를 엽니다.
        /// </summary>
        public static string? OpenExcelFileDialog(string title = "Excel 파일 선택")
        {
            return OpenFileDialog(
                "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                title);
        }

        /// <summary>
        /// TNS 파일 선택 다이얼로그를 엽니다.
        /// </summary>
        public static string? OpenTnsFileDialog(string initialDirectory)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "TNS Names File (tnsnames.ora)|tnsnames.ora|All Files (*.*)|*.*",
                Title = "tnsnames.ora 파일 선택",
                InitialDirectory = initialDirectory
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        /// <summary>
        /// 일반 파일 선택 다이얼로그를 엽니다.
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
        /// 파일이 존재하는지 확인합니다.
        /// </summary>
        public static bool FileExists(string? filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);
        }
    }
}
