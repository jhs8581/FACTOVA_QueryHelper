using System;
using System.Data;
using FACTOVA_QueryHelper.Controls;

namespace FACTOVA_QueryHelper.Services
{
    /// <summary>
    /// 쿼리 파라미터 치환을 담당하는 서비스
    /// </summary>
    public class QueryParameterReplacer
    {
        private readonly SharedDataContext _sharedData;

        public QueryParameterReplacer(SharedDataContext sharedData)
        {
            _sharedData = sharedData ?? throw new ArgumentNullException(nameof(sharedData));
        }

        /// <summary>
        /// DataRowView의 데이터를 기반으로 쿼리 파라미터를 치환합니다.
        /// </summary>
        /// <param name="query">원본 쿼리</param>
        /// <param name="selectedRow">선택된 행 데이터</param>
        /// <returns>치환된 쿼리</returns>
        public string ReplaceWithRowData(string query, DataRowView selectedRow)
        {
            if (string.IsNullOrWhiteSpace(query))
                return query;

            string result = query;

            if (selectedRow != null)
            {
                var row = selectedRow.Row;
                var table = row.Table;

                foreach (DataColumn column in table.Columns)
                {
                    string columnName = column.ColumnName;
                    string columnValue = row[column]?.ToString() ?? "";

                    // @COLUMN_NAME 형식 치환
                    string parameterName = $"@{columnName}";
                    if (result.Contains(parameterName))
                    {
                        result = result.Replace(parameterName, $"'{columnValue}'");
                    }

                    // @COLUMNNAME 형식 치환 (언더스코어 제거)
                    string parameterNameNoUnderscore = $"@{columnName.Replace("_", "")}";
                    if (result.Contains(parameterNameNoUnderscore))
                    {
                        result = result.Replace(parameterNameNoUnderscore, $"'{columnValue}'");
                    }
                }
}

            return result;
        }

        /// <summary>
        /// 입력 필드 값을 기반으로 쿼리 파라미터를 치환합니다.
        /// </summary>
        /// <param name="query">원본 쿼리</param>
        /// <param name="parameters">파라미터 딕셔너리 (키: 파라미터명, 값: 치환할 값)</param>
        /// <returns>치환된 쿼리</returns>
        public string ReplaceWithInputFields(string query, System.Collections.Generic.Dictionary<string, string> parameters)
        {
            if (string.IsNullOrWhiteSpace(query))
                return query;

            string result = query;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    string parameterName = $"@{param.Key}";
                    if (result.Contains(parameterName))
                    {
                        result = result.Replace(parameterName, $"'{param.Value}'");
}
                }
            }

            return result;
        }

        /// <summary>
        /// GMES 표준 파라미터를 치환합니다 (사업장 정보, 일자, W/O 정보 등).
        /// ⚠️ 주의: UI 컨트롤 접근이 필요하므로 GmesInfoControl에서만 사용 가능합니다.
        /// </summary>
        /// <param name="query">원본 쿼리</param>
        /// <param name="gmesParameters">GMES 파라미터 객체</param>
        /// <returns>치환된 쿼리</returns>
        public string ReplaceGmesParameters(string query, GmesParameters gmesParameters)
        {
            if (string.IsNullOrWhiteSpace(query))
                return query;

            string result = query;

            // 사업장 정보 치환
            result = result.Replace("@REPRESENTATIVE_FACTORY_CODE", $"'{gmesParameters.Factory}'");
            result = result.Replace("@ORGANIZATION_ID", $"'{gmesParameters.Organization}'");
            result = result.Replace("@FACILITY_CODE", $"'{gmesParameters.Facility}'");
            result = result.Replace("@WIP_LINE_ID", $"'{gmesParameters.WipLineId}'");
            result = result.Replace("@LINE_ID", $"'{gmesParameters.EquipLineId}'");

            // 일자 정보 치환
            result = result.Replace("@PRODUCTION_YMD_START", $"'{gmesParameters.DateFrom}'");
            result = result.Replace("@PRODUCTION_YMD_END", $"'{gmesParameters.DateTo}'");

            // W/O 정보 치환
            result = result.Replace("@WORK_ORDER_ID", $"'{gmesParameters.WorkOrderId}'");
            result = result.Replace("@WORK_ORDER_NAME", $"'{gmesParameters.WorkOrderName}'");
            result = result.Replace("@PRODUCT_SPECIFICATION_ID", $"'{gmesParameters.ModelSuffix}'");

            // LOT 및 Equipment 정보 치환
            result = result.Replace("@LOT_ID", $"'{gmesParameters.LotId}'");
            result = result.Replace("@EQUIPMENT_ID", $"'{gmesParameters.EquipmentId}'");

            // 파라미터 1~4 치환
            result = result.Replace("@PARAM1", $"'{gmesParameters.Param1}'");
            result = result.Replace("@PARAM2", $"'{gmesParameters.Param2}'");
            result = result.Replace("@PARAM3", $"'{gmesParameters.Param3}'");
            result = result.Replace("@PARAM4", $"'{gmesParameters.Param4}'");


            return result;
        }
    }

    /// <summary>
    /// GMES 파라미터 정보
    /// </summary>
    public class GmesParameters
    {
        // 사업장 정보
        public string Factory { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string Facility { get; set; } = string.Empty;
        public string WipLineId { get; set; } = string.Empty;
        public string EquipLineId { get; set; } = string.Empty;

        // 일자 정보
        public string DateFrom { get; set; } = string.Empty;
        public string DateTo { get; set; } = string.Empty;

        // W/O 정보
        public string WorkOrderId { get; set; } = string.Empty;
        public string WorkOrderName { get; set; } = string.Empty;
        public string ModelSuffix { get; set; } = string.Empty;

        // LOT 및 Equipment
        public string LotId { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;

        // 파라미터 1~4
        public string Param1 { get; set; } = string.Empty;
        public string Param2 { get; set; } = string.Empty;
        public string Param3 { get; set; } = string.Empty;
        public string Param4 { get; set; } = string.Empty;
    }
}
