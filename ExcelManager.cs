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
    }
}
