using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using FACTOVA_QueryHelper.Models;

namespace FACTOVA_QueryHelper.Converters
{
    /// <summary>
    /// ConnectionInfoId를 접속 정보 이름으로 변환하는 Converter
    /// </summary>
    public class ConnectionInfoIdToNameConverter : IValueConverter
    {
        private readonly List<ConnectionInfo> _connectionInfos;

        public ConnectionInfoIdToNameConverter(List<ConnectionInfo> connectionInfos)
        {
            _connectionInfos = connectionInfos ?? new List<ConnectionInfo>();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == System.DBNull.Value)
                return "-- 접속 정보 선택 --";

            if (value is int id && id > 0)
            {
                var connection = _connectionInfos.FirstOrDefault(c => c.Id == id);
                if (connection != null)
                {
                    return $"[{connection.Id}] {connection.Name}";
                }
            }

            return "-- 접속 정보 선택 --";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
