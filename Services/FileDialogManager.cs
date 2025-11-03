using Microsoft.Win32;
using System;
using System.IO;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// 占쏙옙占쏙옙 占쏙옙占싱억옙慣占?占쏙옙占쏙옙 占쏙옙占쏙옙占?占쏙옙占쏙옙占싹댐옙 클占쏙옙占쏙옙
    /// </summary>
    public class FileDialogManager
    {
        /// <summary>
        /// Excel 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占싱억옙慣琉占?占쏙옙占싹댐옙.
        /// </summary>
        public static string? OpenExcelFileDialog(string title = "Excel 占쏙옙占쏙옙 占쏙옙占쏙옙")
        {
            return OpenFileDialog(
                "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                title);
        }

        /// <summary>
        /// TNS 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占싱억옙慣琉占?占쏙옙占싹댐옙.
        /// </summary>
        public static string? OpenTnsFileDialog(string initialDirectory)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "TNS Names File (tnsnames.ora)|tnsnames.ora|All Files (*.*)|*.*",
                Title = "tnsnames.ora 占쏙옙占쏙옙 占쏙옙占쏙옙",
                InitialDirectory = initialDirectory
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        /// <summary>
        /// 占싹뱄옙 占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占싱억옙慣琉占?占쏙옙占싹댐옙.
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
        /// 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占싹댐옙占쏙옙 확占쏙옙占쌌니댐옙.
        /// </summary>
        public static bool FileExists(string? filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);
        }
    }
}
