using System;
using System.Data;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace FACTOVA_QueryHelper.Services
{
    /// <summary>
    /// GMES 정보 조회 결과를 Excel로 내보내는 서비스
    /// </summary>
    public class GmesExcelExporter
    {
        /// <summary>
        /// Excel 내보내기 결과
        /// </summary>
        public class ExportResult
        {
            public bool Success { get; set; }
            public string? FilePath { get; set; }
            public int SheetCount { get; set; }
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// Excel 파일로 내보내기
        /// </summary>
        public ExportResult ExportToExcel(DataView? planDataView, System.Collections.Generic.List<(DataView DataView, string QueryName, int Index)> dynamicGridData)
        {
            try
            {
                // 파일 저장 대화상자
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = $"GMES정보조회_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    DefaultExt = ".xlsx"
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    return new ExportResult { Success = false };
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    // 1. 계획정보 시트 추가
                    if (planDataView != null && planDataView.Count > 0)
                    {
                        AddDataGridToExcel(package, "계획정보", planDataView.ToTable());
                    }

                    // 2. 각 동적 그리드를 개별 시트로 추가
                    foreach (var gridData in dynamicGridData)
                    {
                        if (gridData.DataView != null && gridData.DataView.Count > 0)
                        {
                            var queryName = gridData.QueryName ?? $"그리드{gridData.Index}";
                            
                            // 시트 이름은 최대 31자로 제한하고 특수문자 제거
                            string sheetName = SanitizeSheetName(queryName, gridData.Index);
                            
                            AddDataGridToExcel(package, sheetName, gridData.DataView.ToTable());
                        }
                    }

                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        return new ExportResult 
                        { 
                            Success = false, 
                            ErrorMessage = "다운로드할 데이터가 없습니다.\n먼저 쿼리를 조회하세요." 
                        };
                    }

                    // Excel 파일 저장
                    var fileInfo = new FileInfo(saveFileDialog.FileName);
                    package.SaveAs(fileInfo);

                    return new ExportResult
                    {
                        Success = true,
                        FilePath = fileInfo.FullName,
                        SheetCount = package.Workbook.Worksheets.Count
                    };
                }
            }
            catch (Exception ex)
            {
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = $"Excel 파일 생성 실패:\n{ex.Message}"
                };
            }
        }

        /// <summary>
        /// DataTable을 Excel 시트로 추가합니다.
        /// </summary>
        private void AddDataGridToExcel(ExcelPackage package, string sheetName, DataTable dataTable)
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // 헤더 작성
            for (int col = 0; col < dataTable.Columns.Count; col++)
            {
                var columnName = dataTable.Columns[col].ColumnName;
                // 언더스코어를 공백으로 변경하여 가독성 향상
                worksheet.Cells[1, col + 1].Value = columnName.Replace("_", " ");
            }

            // 헤더 스타일
            using (var range = worksheet.Cells[1, 1, 1, dataTable.Columns.Count])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 120, 215)); // #FF0078D7
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // 데이터 작성
            for (int row = 0; row < dataTable.Rows.Count; row++)
            {
                for (int col = 0; col < dataTable.Columns.Count; col++)
                {
                    var cellValue = dataTable.Rows[row][col];
                    var cell = worksheet.Cells[row + 2, col + 1];

                    // 값 설정
                    if (cellValue != null && cellValue != DBNull.Value)
                    {
                        // 숫자 타입 처리
                        if (cellValue is decimal || cellValue is double || cellValue is float || 
                            cellValue is int || cellValue is long)
                        {
                            cell.Value = cellValue;
                        }
                        // DateTime 타입 처리
                        else if (cellValue is DateTime dateTime)
                        {
                            cell.Value = dateTime;
                            cell.Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                        }
                        else
                        {
                            cell.Value = cellValue.ToString();
                        }
                    }

                    // 테두리 추가
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // CHK 컬럼이 'E'인 경우 행 배경색을 빨간색으로 설정
                if (dataTable.Columns.Contains("CHK"))
                {
                    var chkValue = dataTable.Rows[row]["CHK"]?.ToString()?.Trim();
                    if (chkValue == "E")
                    {
                        using (var rowRange = worksheet.Cells[row + 2, 1, row + 2, dataTable.Columns.Count])
                        {
                            rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            rowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 200, 200));
                            rowRange.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(139, 0, 0));
                        }
                    }
                }
            }

            // 열 너비 자동 조정
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // 최소/최대 열 너비 설정
            for (int col = 1; col <= dataTable.Columns.Count; col++)
            {
                var column = worksheet.Column(col);
                if (column.Width < 10)
                    column.Width = 10;
                else if (column.Width > 50)
                    column.Width = 50;
            }

            // 틀 고정 (헤더 행)
            worksheet.View.FreezePanes(2, 1);
        }

        /// <summary>
        /// Excel 시트 이름에서 사용할 수 없는 문자를 제거하고 길이를 제한합니다.
        /// </summary>
        private string SanitizeSheetName(string name, int index)
        {
            // Excel 시트 이름에서 사용할 수 없는 문자: \ / * ? : [ ]
            var invalidChars = new char[] { '\\', '/', '*', '?', ':', '[', ']' };
            string sanitized = name;

            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c.ToString(), "");
            }

            // 최대 31자로 제한 (Excel 시트 이름 제한)
            if (sanitized.Length > 25)
            {
                sanitized = sanitized.Substring(0, 25);
            }

            // 그리드 번호 추가
            sanitized = $"{index}_{sanitized}";

            return sanitized;
        }
    }
}
