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
        
        // 🔥 접속 정보 참조 (신규)
        public int? ConnectionInfoId { get; set; }
        
        // 🔥 기존 직접 입력 방식 (하위 호환성 유지)
        public string TnsName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        
        // 직접 연결 정보 (TNS 대신 사용 가능)
        public string Host { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;

        // 구분 필드 추가 (쿼리 실행 / 정보 조회)
        public string QueryType { get; set; } = "쿼리 실행";
        
        // 추가 정보 필드
        public string BizName { get; set; } = string.Empty; // 비즈명
        public string Description2 { get; set; } = string.Empty; // 설명
        public int OrderNumber { get; set; } = 0; // 순번

        // 🔥 신규 필드: 쿼리비즈명
        public string QueryBizName { get; set; } = string.Empty;
        
        // 🔥 신규 필드: 버전 정보 (1.0, 2.0 등)
        public string Version { get; set; } = string.Empty;

        // 새로운 옵션 필드 추가됨
        public string EnabledFlag { get; set; } = string.Empty; // G열 'Y'이면 실행 활성
        public string NotifyFlag { get; set; } = string.Empty; // H열 'Y'이면 알림 표시
        public string CountGreaterThan { get; set; } = string.Empty; // I열 N건 이상이면 알림
        public string CountEquals { get; set; } = string.Empty; // J열 N건 정확히 일치 시 알림
        public string CountLessThan { get; set; } = string.Empty; // K열 N건 이하이면 알림
        public string ColumnNames { get; set; } = string.Empty; // L열 체크할 컬럼명(A,B,C 형식)
        public string ColumnValues { get; set; } = string.Empty; // M열 체크할 컬럼값(1,2,3 형식)
        public string ExcludeFlag { get; set; } = string.Empty; // N열 'Y'이면 제외
        public string DefaultFlag { get; set; } = string.Empty; // O열 'Y'이면 디폴트값

        // DataGrid CheckBox 바인딩용 Bool 속성
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

        // "포함" 체크박스 (ExcludeFlag와 반대 로직)
        // 체크 = 포함 (ExcludeFlag = "N")
        // 체크 해제 = 제외 (ExcludeFlag = "Y")
        public bool ExcludeFlagBool
        {
            get => !string.Equals(ExcludeFlag, "Y", StringComparison.OrdinalIgnoreCase);
            set => ExcludeFlag = value ? "N" : "Y";
        }

        // 디폴트값 체크박스
        public bool DefaultFlagBool
        {
            get => string.Equals(DefaultFlag, "Y", StringComparison.OrdinalIgnoreCase);
            set => DefaultFlag = value ? "Y" : "N";
        }
    }

    public class ExcelQueryReader
    {
        // EPPlus 라이선스 설정
        static ExcelQueryReader()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Excel 열 문자(A, B, C...)를 숫자 인덱스로 변환 (1-based)
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
        /// Excel 파일에서 쿼리 목록을 읽어옵니다.
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
                throw new FileNotFoundException($"Excel 파일을 찾을 수 없습니다: {filePath}");
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
                    throw new ArgumentException("쿼리 컬럼 문자열이 잘못되었습니다.");
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
                            throw new ArgumentException($"시트를 찾을 수 없습니다: {sheetName}");
                        }
                    }

                    int rowCount = worksheet.Dimension?.End.Row ?? 0;

                    for (int row = startRow; row <= rowCount; row++)
                    {
                        // 쿼리 읽기
                        var query = worksheet.Cells[row, queryColIndex].Text?.Trim();

                        // 쿼리가 비어있으면 건너뜀
                        if (string.IsNullOrWhiteSpace(query))
                            continue;

                        var queryItem = new QueryItem
                        {
                            RowNumber = row,
                            Query = query
                        };

                        // 쿼리 이름 읽기
                        if (nameColIndex > 0)
                        {
                            queryItem.QueryName = worksheet.Cells[row, nameColIndex].Text?.Trim() ?? "";
                        }
                        if (string.IsNullOrEmpty(queryItem.QueryName))
                        {
                            queryItem.QueryName = $"Query {row - startRow + 1}";
                        }

                        // 설명 읽기
                        if (descColIndex > 0)
                        {
                            queryItem.Description = worksheet.Cells[row, descColIndex].Text?.Trim() ?? "";
                        }

                        // TNS 이름 읽기 (또는 Host:Port:ServiceName 형식)
                        if (tnsColIndex > 0)
                        {
                            var tnsValue = worksheet.Cells[row, tnsColIndex].Text?.Trim() ?? "";
                            
                            // Host:Port:ServiceName 형식인지 확인
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

                        // User ID 읽기
                        if (userColIndex > 0)
                        {
                            queryItem.UserId = worksheet.Cells[row, userColIndex].Text?.Trim() ?? "";
                        }

                        // Password 읽기
                        if (passColIndex > 0)
                        {
                            queryItem.Password = worksheet.Cells[row, passColIndex].Text?.Trim() ?? "";
                        }

                        // BizName 읽기
                        if (passColIndex > 0)
                        {
                            queryItem.BizName = worksheet.Cells[row, passColIndex].Text?.Trim() ?? "";
                        }

                        // Description2 읽기
                        if (passColIndex > 0)
                        {
                            queryItem.Description2 = worksheet.Cells[row, passColIndex].Text?.Trim() ?? "";
                        }

                        // 순번 읽기
                        if (passColIndex > 0)
                        {
                            if (int.TryParse(worksheet.Cells[row, passColIndex].Text?.Trim(), out int orderNumber))
                            {
                                queryItem.OrderNumber = orderNumber;
                            }
                        }

                        // 옵션 필드들 읽기
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
                throw new Exception($"Excel 파일 읽기 오류: {ex.Message}", ex);
            }

            return queries;
        }

        /// <summary>
        /// Excel 파일의 시트 목록을 반환합니다.
        /// </summary>
        public static List<string> GetSheetNames(string filePath)
        {
            var sheetNames = new List<string>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel 파일을 찾을 수 없습니다: {filePath}");
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
                throw new Exception($"Excel 파일 읽기 오류: {ex.Message}", ex);
            }

            return sheetNames;
        }

        /// <summary>
        /// Excel 파일의 특정 시트에 있는 컬럼명을 반환합니다.
        /// </summary>
        public static List<string> GetColumnHeaders(string filePath, string? sheetName = null, int headerRow = 1)
        {
            var headers = new List<string>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel 파일을 찾을 수 없습니다: {filePath}");
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
                            throw new ArgumentException($"시트를 찾을 수 없습니다: {sheetName}");
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
                throw new Exception($"Excel 파일 읽기 오류: {ex.Message}", ex);
            }

            return headers;
        }

        /// <summary>
        /// 숫자 인덱스를 Excel 열 문자로 변환 (1-based)
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
