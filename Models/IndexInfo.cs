using System;

namespace FACTOVA_QueryHelper.Models
{
    /// <summary>
    /// 데이터베이스 인덱스 정보
    /// </summary>
    public class IndexInfo
    {
        public string IndexName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string IndexType { get; set; } = string.Empty;
        public string Uniqueness { get; set; } = string.Empty;
        public int ColumnPosition { get; set; }
        public string DescendFlag { get; set; } = string.Empty;
        
        // OracleDbService에서 사용하는 추가 속성
        public string Type { get; set; } = string.Empty;
        public string Columns { get; set; } = string.Empty;
    }
}
