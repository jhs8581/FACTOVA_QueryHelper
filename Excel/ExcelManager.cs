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
    /// Excel ���� ������ ����ϴ� Ŭ����
    /// </summary>
    public class ExcelManager
    {
        /// <summary>
        /// Excel ���Ͽ��� ���� ����� �ε��մϴ�.
        /// N�� ���� ������� ��� ������ �ε��մϴ�.
        /// </summary>
        public static List<QueryItem> LoadQueries(string filePath, string? sheetName, int startRow)
        {
            var queries = ExcelQueryReader.ReadQueriesFromExcel(
                filePath,
                sheetName,
                "F",     // ���� (�ʼ�)
                "D",     // �� �̸� (�ʼ�)
                "",      // ���� �� ��� �� ��
                "A",     // TNS (����)
                "B",     // User ID (����)
                "C",     // Password (����)
                startRow,
                "G",  // ���� ����
                "H",  // �˸� ����
                "I",  // �̻�
                "J",  // ����
                "K",  // ����
                "L",  // �÷���
                "M",  // �÷���
                "N"); // �⺻ Ȱ��ȭ ����

            // N�� ���͸� ���� - ��� ���� ��ȯ
            return queries;
        }

        /// <summary>
        /// Excel ������ ��Ʈ ����� �����ɴϴ�.
        /// </summary>
        public static List<string> GetSheetNames(string filePath)
        {
            return ExcelQueryReader.GetSheetNames(filePath);
        }

        /// <summary>
        /// SFC Excel ���Ͽ��� ���� ������ �ε��մϴ�.
        /// </summary>
        public static List<SfcEquipmentInfo> LoadSfcEquipmentList(string filePath)
        {
            var equipmentList = new List<SfcEquipmentInfo>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Excel ������ ã�� �� �����ϴ�.", filePath);
            }

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension?.End.Row ?? 0;

                // 2����� ������ �б� (1���� ���)
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
