using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// Excel 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙求占?클占쏙옙占쏙옙
    /// </summary>
    public class ExcelManager
    {
        /// <summary>
        /// Excel 占쏙옙占싹울옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占?占싸듸옙占쌌니댐옙.
        /// N占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙占?占쏙옙占?占쏙옙占쏙옙占쏙옙 占싸듸옙占쌌니댐옙.
        /// </summary>
        public static List<QueryItem> LoadQueries(string filePath, string? sheetName, int startRow)
        {
            var queries = ExcelQueryReader.ReadQueriesFromExcel(
                filePath,
                sheetName,
                "F",     // 占쏙옙占쏙옙 (占십쇽옙)
                "D",     // 占쏙옙 占싱몌옙 (占십쇽옙)
                "",      // 占쏙옙占쏙옙 占쏙옙 占쏙옙占?占쏙옙 占쏙옙
                "A",     // TNS (占쏙옙占쏙옙)
                "B",     // User ID (占쏙옙占쏙옙)
                "C",     // Password (占쏙옙占쏙옙)
                startRow,
                "G",  // 占쏙옙占쏙옙 占쏙옙占쏙옙
                "H",  // 占싯몌옙 占쏙옙占쏙옙
                "I",  // 占싱삼옙
                "J",  // 占쏙옙占쏙옙
                "K",  // 占쏙옙占쏙옙
                "L",  // 占시뤄옙占쏙옙
                "M",  // 占시뤄옙占쏙옙
                "N"); // 占썩본 활占쏙옙화 占쏙옙占쏙옙

            // N占쏙옙 占쏙옙占싶몌옙 占쏙옙占쏙옙 - 占쏙옙占?占쏙옙占쏙옙 占쏙옙환
            return queries;
        }

        /// <summary>
        /// Excel 占쏙옙占쏙옙占쏙옙 占쏙옙트 占쏙옙占쏙옙占?占쏙옙占쏙옙占심니댐옙.
        /// </summary>
        public static List<string> GetSheetNames(string filePath)
        {
            return ExcelQueryReader.GetSheetNames(filePath);
        }

        /// <summary>
        /// SFC Excel 占쏙옙占싹울옙占쏙옙 占쏙옙占쏙옙 占쏙옙占쏙옙占쏙옙 占싸듸옙占쌌니댐옙.
        /// </summary>
        public static List<SfcEquipmentInfo> LoadSfcEquipmentList(string filePath)
        {
            var equipmentList = new List<SfcEquipmentInfo>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Excel 占쏙옙占쏙옙占쏙옙 찾占쏙옙 占쏙옙 占쏙옙占쏙옙占싹댐옙.", filePath);
            }

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension?.End.Row ?? 0;

                // 2占쏙옙占쏙옙占?占쏙옙占쏙옙占쏙옙 占싻깍옙 (1占쏙옙占쏙옙 占쏙옙占?
                for (int row = 2; row <= rowCount; row++)
                {
                    var ipAddress = worksheet.Cells[row, 1].Text?.Trim();
                    var equipmentId = worksheet.Cells[row, 2].Text?.Trim();
                    var equipmentName = worksheet.Cells[row, 3].Text?.Trim();

                    if (!string.IsNullOrWhiteSpace(ipAddress))
                    {
                        equipmentList.Add(new SfcEquipmentInfo
                        {
                            IpAddress = ipAddress,
                            EquipmentId = equipmentId ?? "",
                            EquipmentName = equipmentName ?? "",
                            Status = ""
                        });
                    }
                }
            }

            return equipmentList;
        }

        /// <summary>
        /// 쿼리 목록을 Excel 파일로 내보냅니다.
        /// </summary>
        public static void ExportQueries(List<QueryItem> queries, string filePath)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Queries");

            // 헤더 작성
            worksheet.Cells[1, 1].Value = "TNS 이름";
            worksheet.Cells[1, 2].Value = "User ID";
            worksheet.Cells[1, 3].Value = "Password";
            worksheet.Cells[1, 4].Value = "탭 이름";
            worksheet.Cells[1, 5].Value = "Host";
            worksheet.Cells[1, 6].Value = "SQL 쿼리";
            worksheet.Cells[1, 7].Value = "실행 여부";
            worksheet.Cells[1, 8].Value = "알림 여부";
            worksheet.Cells[1, 9].Value = "이상 건수";
            worksheet.Cells[1, 10].Value = "같음 건수";
            worksheet.Cells[1, 11].Value = "이하 건수";
            worksheet.Cells[1, 12].Value = "컬럼명";
            worksheet.Cells[1, 13].Value = "컬럼값";
            worksheet.Cells[1, 14].Value = "제외 여부";

            // 헤더 스타일
            using (var range = worksheet.Cells[1, 1, 1, 14])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // 데이터 작성
            int row = 2;
            foreach (var query in queries)
            {
                worksheet.Cells[row, 1].Value = query.TnsName;
                worksheet.Cells[row, 2].Value = query.UserId;
                worksheet.Cells[row, 3].Value = query.Password;
                worksheet.Cells[row, 4].Value = query.QueryName;
                
                // Host:Port:ServiceName 형식으로 저장
                if (!string.IsNullOrEmpty(query.Host))
                {
                    worksheet.Cells[row, 5].Value = $"{query.Host}:{query.Port}:{query.ServiceName}";
                }
                
                worksheet.Cells[row, 6].Value = query.Query;
                worksheet.Cells[row, 7].Value = query.EnabledFlag;
                worksheet.Cells[row, 8].Value = query.NotifyFlag;
                worksheet.Cells[row, 9].Value = query.CountGreaterThan;
                worksheet.Cells[row, 10].Value = query.CountEquals;
                worksheet.Cells[row, 11].Value = query.CountLessThan;
                worksheet.Cells[row, 12].Value = query.ColumnNames;
                worksheet.Cells[row, 13].Value = query.ColumnValues;
                worksheet.Cells[row, 14].Value = query.ExcludeFlag;
                
                row++;
            }

            // 열 너비 자동 조정
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // 파일 저장
            package.SaveAs(new FileInfo(filePath));
        }
    }
}
