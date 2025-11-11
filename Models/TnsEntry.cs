using System;

namespace FACTOVA_QueryHelper.Models
{
    public class TnsEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Protocol { get; set; } = "TCP";

        /// <summary>
        /// 전체 TNS 연결 문자열 생성
        /// </summary>
        public string GetConnectionString()
        {
            return $"(DESCRIPTION=(ADDRESS=(PROTOCOL={Protocol})(HOST={Host})(PORT={Port}))(CONNECT_DATA=(SERVICE_NAME={ServiceName})))";
        }

        /// <summary>
        /// 상세 정보 (툴팁이나 디버그용)
        /// </summary>
        public string GetDetailString()
        {
            return $"{Name} ({Host}:{Port}/{ServiceName})";
        }
    }
}
