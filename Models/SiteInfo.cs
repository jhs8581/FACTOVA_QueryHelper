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

        // 🔥 신규: 버전별 TNS 매핑 (JSON 형식)
        // 예: {"1.0": "GM1MKCP", "2.0": "GM2MKCP"}
        public string TnsVersionMapping { get; set; } = "{}";

        // 🔥 신규: UI 바인딩용 속성 (Tns10, Tns20)
        private string _tns10 = string.Empty;
        private string _tns20 = string.Empty;
        
        // 🔥 ConnectionInfo 객체 바인딩용 속성 추가
        private ConnectionInfo? _tns10ConnectionInfo;
        private ConnectionInfo? _tns20ConnectionInfo;
        
        /// <summary>
        /// TNS (1.0) 접속 정보 객체
        /// </summary>
        public ConnectionInfo? Tns10ConnectionInfo
        {
            get => _tns10ConnectionInfo;
            set
            {
                _tns10ConnectionInfo = value;
                // 선택된 접속 정보의 Name을 Tns10에 저장
                _tns10 = value?.Name ?? string.Empty;
                UpdateTnsMapping("1.0", _tns10);
            }
        }
        
        /// <summary>
        /// TNS (2.0) 접속 정보 객체
        /// </summary>
        public ConnectionInfo? Tns20ConnectionInfo
        {
            get => _tns20ConnectionInfo;
            set
            {
                _tns20ConnectionInfo = value;
                // 선택된 접속 정보의 Name을 Tns20에 저장
                _tns20 = value?.Name ?? string.Empty;
                UpdateTnsMapping("2.0", _tns20);
            }
        }
        
        public string Tns10
        {
            get
            {
                if (string.IsNullOrEmpty(_tns10))
                {
                    var mapping = GetTnsMapping();
                    _tns10 = mapping.ContainsKey("1.0") ? mapping["1.0"] : string.Empty;
                }
                return _tns10;
            }
            set
            {
                _tns10 = value;
                UpdateTnsMapping("1.0", value);
            }
        }
        
        public string Tns20
        {
            get
            {
                if (string.IsNullOrEmpty(_tns20))
                {
                    var mapping = GetTnsMapping();
                    _tns20 = mapping.ContainsKey("2.0") ? mapping["2.0"] : string.Empty;
                }
                return _tns20;
            }
            set
            {
                _tns20 = value;
                UpdateTnsMapping("2.0", value);
            }
        }

        /// <summary>
        /// ComboBox 표시용 텍스트
        /// </summary>
        public string DisplayText =>
            $"[{SiteName}] ({RepresentativeFactory}, {Organization}, {Facility}, {WipLineId}, {EquipLineId})";

        public override string ToString() => DisplayText;
        
        /// <summary>
        /// 버전에 맞는 TNS 이름 가져오기
        /// </summary>
        public string? GetTnsForVersion(string version)
        {
            try
            {
                var mapping = System.Text.Json.JsonSerializer
                    .Deserialize<Dictionary<string, string>>(TnsVersionMapping);
                
                if (mapping != null && mapping.TryGetValue(version, out var tnsName))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ 사업장 '{SiteName}' 버전 {version} → TNS: {tnsName}");
                    return tnsName;
                }
                
                System.Diagnostics.Debug.WriteLine($"⚠️ 사업장 '{SiteName}'에 버전 {version}에 대한 TNS 매핑 없음");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TNS 매핑 파싱 오류: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 버전별 TNS 매핑 설정
        /// </summary>
        public void SetTnsMapping(Dictionary<string, string> mapping)
        {
            TnsVersionMapping = System.Text.Json.JsonSerializer.Serialize(mapping);
            
            // 캐시 업데이트
            _tns10 = mapping.ContainsKey("1.0") ? mapping["1.0"] : string.Empty;
            _tns20 = mapping.ContainsKey("2.0") ? mapping["2.0"] : string.Empty;
        }
        
        /// <summary>
        /// 버전별 TNS 매핑 가져오기
        /// </summary>
        public Dictionary<string, string> GetTnsMapping()
        {
            try
            {
                return System.Text.Json.JsonSerializer
                    .Deserialize<Dictionary<string, string>>(TnsVersionMapping) 
                    ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }
        
        /// <summary>
        /// 개별 버전의 TNS 업데이트
        /// </summary>
        private void UpdateTnsMapping(string version, string tnsName)
        {
            var mapping = GetTnsMapping();
            
            if (string.IsNullOrWhiteSpace(tnsName))
            {
                mapping.Remove(version);
            }
            else
            {
                mapping[version] = tnsName;
            }
            
            TnsVersionMapping = System.Text.Json.JsonSerializer.Serialize(mapping);
        }
    }
}
