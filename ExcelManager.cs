using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace FACTOVA_Palletizing_Analysis
{
    /// <summary>
    /// Excel 파일 관리를 담당하는 클래스
    /// </summary>
    public class ExcelManager
    {
        /// <summary>
        /// Excel 파일에서 쿼리 목록을 로드합니다.
        /// N열 값과 상관없이 모든 쿼리를 로드합니다.
        /// </summary>
        public static List<QueryItem> LoadQueries(string filePath, string? sheetName, int startRow)
        {
            var queries = ExcelQueryReader.ReadQueriesFromExcel(
                filePath,
                sheetName,
                "F",     // 쿼리 (필수)
                "D",     // 탭 이름 (필수)
                "",      // 설명 열 사용 안 함
                "A",     // TNS (선택)
                "B",     // User ID (선택)
                "C",     // Password (선택)
                startRow,
                "G",  // 실행 여부
                "H",  // 알림 여부
                "I",  // 이상
                "J",  // 같음
                "K",  // 이하
                "L",  // 컬럼명
                "M",  // 컬럼값
                "N"); // 기본 활성화 여부

            // N열 필터링 제거 - 모든 쿼리 반환
            return queries;
        }

        /// <summary>
        /// Excel 파일의 시트 목록을 가져옵니다.
        /// </summary>
        public static List<string> GetSheetNames(string filePath)
        {
            return ExcelQueryReader.GetSheetNames(filePath);
        }

        /// <summary>
        /// SFC Excel 파일에서 설비 정보를 로드합니다.
        /// </summary>
        public static List<SfcEquipmentInfo> LoadSfcEquipmentList(string filePath)
        {
            var equipmentList = new List<SfcEquipmentInfo>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Excel 파일을 찾을 수 없습니다.", filePath);
            }

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension?.End.Row ?? 0;

                // 2행부터 데이터 읽기 (1행은 헤더)
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
