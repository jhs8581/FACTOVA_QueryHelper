namespace FACTOVA_QueryHelper.Models
{
    /// <summary>
    /// 테이블 단축어 모델
    /// </summary>
    public class TableShortcut
    {
        /// <summary>
        /// 단축어 (PRIMARY KEY)
        /// </summary>
        public string Shortcut { get; set; } = string.Empty;
        
        /// <summary>
        /// 전체 테이블명
        /// </summary>
        public string FullTableName { get; set; } = string.Empty;
        
        /// <summary>
        /// 설명
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 표시용 텍스트
        /// </summary>
        public string DisplayText => $"{Shortcut} → {FullTableName}";
        
        public override string ToString() => DisplayText;
    }
}
