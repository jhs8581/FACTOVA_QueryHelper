using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FACTOVA_QueryHelper
{
    public class QueryItem
    {
        public int RowNumber { get; set; }
        public string QueryName { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TnsName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        
        // 吏곸젒 ?곌껐 ?뺣낫 (TNS ????ъ슜 媛??
        public string Host { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;

        // ?덈줈???듭뀡 ?꾨뱶 ?띿꽦??
        public string EnabledFlag { get; set; } = string.Empty; // G?? 'Y'?대㈃ ?ㅽ뻾 ?쒖꽦
        public string NotifyFlag { get; set; } = string.Empty; // H?? 'Y'?대㈃ ?뚮┝ ?쒖떆
        public string CountGreaterThan { get; set; } = string.Empty; // I?? N嫄??댁긽?????뚮┝
        public string CountEquals { get; set; } = string.Empty; // J?? N嫄??뺥솗???쇱튂 ???뚮┝
        public string CountLessThan { get; set; } = string.Empty; // K?? N嫄??댄븯?????뚮┝
        public string ColumnNames { get; set; } = string.Empty; // L?? 泥댄겕??而щ읆紐?(A,B,C ?뺤떇)
        public string ColumnValues { get; set; } = string.Empty; // M?? 泥댄겕??而щ읆媛?(1,2,3 ?뺤떇)
        public string ExcludeFlag { get; set; } = string.Empty; // N?? 'Y'?대㈃ ?쒖쇅
        public string DefaultFlag { get; set; } = string.Empty; // O?? 'Y'?대㈃ ?뷀뤃????

        // DataGrid CheckBox 諛붿씤?⑹슜 Bool ?띿꽦
        public bool EnabledFlagBool
        {
            get => string.Equals(EnabledFlag, "Y", StringComparison.OrdinalIgnoreCase);
            set => EnabledFlag = value ? "Y" : "N";
        }

        public bool NotifyFlagBool
        {
            get => string.Equals(NotifyFlag, "Y", StringComparison.OrdinalIgnoreCase);
            set => NotifyFlag = value ? "Y" : "N";
        }

        // "?ы븿" 泥댄겕諛뺤뒪 (ExcludeFlag? 諛섎? 濡쒖쭅)
        // 泥댄겕 = ?ы븿 (ExcludeFlag = "N")
        // 泥댄겕 ?댁젣 = ?쒖쇅 (ExcludeFlag = "Y")
        public bool ExcludeFlagBool
        {
            get => !string.Equals(ExcludeFlag, "Y", StringComparison.OrdinalIgnoreCase);
            set => ExcludeFlag = value ? "N" : "Y";
        }

        // ?뷀뤃????泥댄겕諛뺤뒪
        public bool DefaultFlagBool
        {
            get => string.Equals(DefaultFlag, "Y", StringComparison.OrdinalIgnoreCase);
            set => DefaultFlag = value ? "Y" : "N";
        }
    }

    public class ExcelQueryReader
    {
        // EPPlus 占쏙옙占싱쇽옙占쏙옙 占쏙옙占쏙옙
        static ExcelQueryReader()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Excel 占쏙옙 占쏙옙占쏙옙(A, B, C...)占쏙옙 占쏙옙占쏙옙 占싸듸옙占쏙옙占쏙옙 占쏙옙환 (1-based)
        /// </summary>
        public static int ColumnLetterToNumber(string columnLetter)
        {
            if (string.IsNullOrWhiteSpace(columnLetter))
                return 0;

            columnLetter = columnLetter.ToUpper().Trim();
            int result = 0;
            
            for (int i = 0; i < columnLetter.Length; i++)
            {
                result = result * 26 + (columnLetter[i] - 'A' + 1);
            }
            
            return result;
        }

        /// <summary>
        /// Excel 占쏙옙占싹울옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占?占싻억옙求占?
        /// </summary>
        public static List<QueryItem> ReadQueriesFromExcel(
            string filePath,
            string? sheetName = null,
            string queryColumn = "A",
            string nameColumn = "",
            string descriptionColumn = "",
            string tnsColumn = "",
            string userIdColumn = "",
            string passwordColumn = "",
            int startRow = 2,
            string enabledColumn = "G",
            string notifyColumn = "H",
            string countGreaterColumn = "I",
            string countEqualsColumn = "J",
            string countLessColumn = "K",
            string columnNamesColumn = "L",
            string columnValuesColumn = "M",
            string excludeColumn = "N")
        {
            var queries = new List<QueryItem>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel 占쏙옙占쏙옙占쏙옙 찾占쏙옙 占쏙옙 占쏙옙占쏙옙占싹댐옙: {filePath}");
            }

            try
            {
                int queryColIndex = ColumnLetterToNumber(queryColumn);
                int nameColIndex = ColumnLetterToNumber(nameColumn);
                int descColIndex = ColumnLetterToNumber(descriptionColumn);
                int tnsColIndex = ColumnLetterToNumber(tnsColumn);
                int userColIndex = ColumnLetterToNumber(userIdColumn);
                int passColIndex = ColumnLetterToNumber(passwordColumn);
                int enabledColIndex = ColumnLetterToNumber(enabledColumn);
                int notifyColIndex = ColumnLetterToNumber(notifyColumn);
                int countGreaterColIndex = ColumnLetterToNumber(countGreaterColumn);
                int countEqualsColIndex = ColumnLetterToNumber(countEqualsColumn);
                int countLessColIndex = ColumnLetterToNumber(countLessColumn);
                int columnNamesColIndex = ColumnLetterToNumber(columnNamesColumn);
                int columnValuesColIndex = ColumnLetterToNumber(columnValuesColumn);
                int excludeColIndex = ColumnLetterToNumber(excludeColumn);

                if (queryColIndex == 0)
                {
                    throw new ArgumentException("占쏙옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙占쏙옙 占십았쏙옙占싹댐옙.");
                }

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet worksheet;

                    if (string.IsNullOrEmpty(sheetName))
                    {
                        worksheet = package.Workbook.Worksheets[0];
                    }
                    else
                    {
                        worksheet = package.Workbook.Worksheets[sheetName];
                        if (worksheet == null)
                        {
                            throw new ArgumentException($"占쏙옙트占쏙옙 찾占쏙옙 占쏙옙 占쏙옙占쏙옙占싹댐옙: {sheetName}");
                        }
                    }

                    int rowCount = worksheet.Dimension?.End.Row ?? 0;

                    for (int row = startRow; row <= rowCount; row++)
                    {
                        // 占쏙옙占쏙옙 占싻깍옙
                        var query = worksheet.Cells[row, queryColIndex].Text?.Trim();

                        // 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙占쏙옙占?占실너뛰깍옙
                        if (string.IsNullOrWhiteSpace(query))
                            continue;

                        var queryItem = new QueryItem
                        {
                            RowNumber = row,
                            Query = query
                        };

                        // 占쏙옙占쏙옙 占싱몌옙 占싻깍옙
                        if (nameColIndex > 0)
                        {
                            queryItem.QueryName = worksheet.Cells[row, nameColIndex].Text?.Trim() ?? "";
                        }
                        if (string.IsNullOrEmpty(queryItem.QueryName))
                        {
                            queryItem.QueryName = $"Query {row - startRow + 1}";
                        }

                        // 占쏙옙占쏙옙 占싻깍옙
                        if (descColIndex > 0)
                        {
                            queryItem.Description = worksheet.Cells[row, descColIndex].Text?.Trim() ?? "";
                        }

                        // TNS 占싱몌옙 占싻깍옙 (占실댐옙 Host:Port:ServiceName 占쏙옙占쏙옙)
                        if (tnsColIndex > 0)
                        {
                            var tnsValue = worksheet.Cells[row, tnsColIndex].Text?.Trim() ?? "";
                            
                            // Host:Port:ServiceName 占쏙옙占쏙옙占쏙옙占쏙옙 확占쏙옙
                            if (tnsValue.Contains(":"))
                            {
                                var parts = tnsValue.Split(':');
                                if (parts.Length == 3)
                                {
                                    queryItem.Host = parts[0].Trim();
                                    queryItem.Port = parts[1].Trim();
                                    queryItem.ServiceName = parts[2].Trim();
                                }
                                else
                                {
                                    queryItem.TnsName = tnsValue;
                                }
                            }
                            else
                            {
                                queryItem.TnsName = tnsValue;
                            }
                        }

                        // User ID 占싻깍옙
                        if (userColIndex > 0)
                        {
                            queryItem.UserId = worksheet.Cells[row, userColIndex].Text?.Trim() ?? "";
                        }

                        // Password 占싻깍옙
                        if (passColIndex > 0)
                        {
                            queryItem.Password = worksheet.Cells[row, passColIndex].Text?.Trim() ?? "";
                        }

                        // 占쏙옙占싸울옙 占심쇽옙 占십듸옙 占쏙옙 占싻깍옙
                        if (enabledColIndex > 0)
                        {
                            queryItem.EnabledFlag = worksheet.Cells[row, enabledColIndex].Text?.Trim().ToUpper() ?? "";
                        }

                        if (notifyColIndex > 0)
                        {
                            queryItem.NotifyFlag = worksheet.Cells[row, notifyColIndex].Text?.Trim().ToUpper() ?? "";
                        }

                        if (countGreaterColIndex > 0)
                        {
                            queryItem.CountGreaterThan = worksheet.Cells[row, countGreaterColIndex].Text?.Trim() ?? "";
                        }

                        if (countEqualsColIndex > 0)
                        {
                            queryItem.CountEquals = worksheet.Cells[row, countEqualsColIndex].Text?.Trim() ?? "";
                        }

                        if (countLessColIndex > 0)
                        {
                            queryItem.CountLessThan = worksheet.Cells[row, countLessColIndex].Text?.Trim() ?? "";
                        }

                        if (columnNamesColIndex > 0)
                        {
                            queryItem.ColumnNames = worksheet.Cells[row, columnNamesColIndex].Text?.Trim() ?? "";
                        }

                        if (columnValuesColIndex > 0)
                        {
                            queryItem.ColumnValues = worksheet.Cells[row, columnValuesColIndex].Text?.Trim() ?? "";
                        }

                        if (excludeColIndex > 0)
                        {
                            queryItem.ExcludeFlag = worksheet.Cells[row, excludeColIndex].Text?.Trim().ToUpper() ?? "";
                        }

                        queries.Add(queryItem);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Excel 占쏙옙占쏙옙 占싻깍옙 占쏙옙占쏙옙: {ex.Message}", ex);
            }

            return queries;
        }

        /// <summary>
        /// Excel 占쏙옙占쏙옙占쏙옙 占쏙옙트 占쏙옙占쏙옙占?占쏙옙占쏙옙占심니댐옙.
        /// </summary>
        public static List<string> GetSheetNames(string filePath)
        {
            var sheetNames = new List<string>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel 占쏙옙占쏙옙占쏙옙 찾占쏙옙 占쏙옙 占쏙옙占쏙옙占싹댐옙: {filePath}");
            }

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    sheetNames = package.Workbook.Worksheets.Select(ws => ws.Name).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Excel 占쏙옙占쏙옙 占싻깍옙 占쏙옙占쏙옙: {ex.Message}", ex);
            }

            return sheetNames;
        }

        /// <summary>
        /// Excel 占쏙옙占쏙옙占쏙옙 특占쏙옙 占쏙옙트占쏙옙 占쏙옙占?占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占심니댐옙.
        /// </summary>
        public static List<string> GetColumnHeaders(string filePath, string? sheetName = null, int headerRow = 1)
        {
            var headers = new List<string>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel 占쏙옙占쏙옙占쏙옙 찾占쏙옙 占쏙옙 占쏙옙占쏙옙占싹댐옙: {filePath}");
            }

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet worksheet;

                    if (string.IsNullOrEmpty(sheetName))
                    {
                        worksheet = package.Workbook.Worksheets[0];
                    }
                    else
                    {
                        worksheet = package.Workbook.Worksheets[sheetName];
                        if (worksheet == null)
                        {
                            throw new ArgumentException($"占쏙옙트占쏙옙 찾占쏙옙 占쏙옙 占쏙옙占쏙옙占싹댐옙: {sheetName}");
                        }
                    }

                    int colCount = worksheet.Dimension?.End.Column ?? 0;

                    for (int col = 1; col <= colCount; col++)
                    {
                        var header = worksheet.Cells[headerRow, col].Text?.Trim() ?? GetColumnLetter(col);
                        headers.Add(header);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Excel 占쏙옙占쏙옙 占싻깍옙 占쏙옙占쏙옙: {ex.Message}", ex);
            }

            return headers;
        }

        /// <summary>
        /// 占쏙옙占쏙옙 占싸듸옙占쏙옙占쏙옙 Excel 占쏙옙 占쏙옙占쌘뤄옙 占쏙옙환 (1-based)
        /// </summary>
        public static string GetColumnLetter(int columnNumber)
        {
            string columnLetter = "";
            
            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnLetter = Convert.ToChar('A' + modulo) + columnLetter;
                columnNumber = (columnNumber - modulo) / 26;
            }
            
            return columnLetter;
        }
    }
}
