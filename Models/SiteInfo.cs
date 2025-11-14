namespace FACTOVA_QueryHelper.Models
{
    /// <summary>
    /// 사업장 정보 모델
    /// </summary>
    public class SiteInfo
    {
        public int Id { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string RepresentativeFactory { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string Facility { get; set; } = string.Empty;
        public string WipLineId { get; set; } = string.Empty;
        public string EquipLineId { get; set; } = string.Empty;
        
        // 🔥 IsDefault를 int로 사용 (표시순번으로 재사용)
        public int IsDefault { get; set; }

        /// <summary>
        /// ComboBox 표시용 텍스트
        /// </summary>
        public string DisplayText =>
            $"[{SiteName}] ({RepresentativeFactory}, {Organization}, {Facility}, {WipLineId}, {EquipLineId})";

        public override string ToString() => DisplayText;
    }
}
