using System.Collections.Generic;
using System.Windows.Media;

namespace FACTOVA_QueryHelper.Models
{
    /// <summary>
    /// 행 색상 정의
    /// </summary>
    public class RowColorItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public Color Color { get; set; }
    }

    /// <summary>
    /// 행 색상 목록 제공
    /// </summary>
    public static class RowColorProvider
    {
        public static List<RowColorItem> GetColorList()
        {
            return new List<RowColorItem>
            {
                new RowColorItem { Name = "기본", Value = "", Color = Colors.White },
                new RowColorItem { Name = "빨강 연한", Value = "LightCoral", Color = Color.FromRgb(240, 128, 128) },
                new RowColorItem { Name = "주황 연한", Value = "LightSalmon", Color = Color.FromRgb(255, 160, 122) },
                new RowColorItem { Name = "노랑 연한", Value = "LightYellow", Color = Color.FromRgb(255, 255, 224) },
                new RowColorItem { Name = "초록 연한", Value = "LightGreen", Color = Color.FromRgb(144, 238, 144) },
                new RowColorItem { Name = "하늘 연한", Value = "LightSkyBlue", Color = Color.FromRgb(135, 206, 250) },
                new RowColorItem { Name = "파랑 연한", Value = "LightBlue", Color = Color.FromRgb(173, 216, 230) },
                new RowColorItem { Name = "보라 연한", Value = "Lavender", Color = Color.FromRgb(230, 230, 250) },
                new RowColorItem { Name = "분홍 연한", Value = "LightPink", Color = Color.FromRgb(255, 182, 193) },
                new RowColorItem { Name = "회색 연한", Value = "LightGray", Color = Color.FromRgb(211, 211, 211) },
                new RowColorItem { Name = "빨강", Value = "IndianRed", Color = Color.FromRgb(205, 92, 92) },
                new RowColorItem { Name = "주황", Value = "DarkOrange", Color = Color.FromRgb(255, 140, 0) },
                new RowColorItem { Name = "노랑", Value = "Gold", Color = Color.FromRgb(255, 215, 0) },
                new RowColorItem { Name = "초록", Value = "LimeGreen", Color = Color.FromRgb(50, 205, 50) },
                new RowColorItem { Name = "하늘", Value = "SkyBlue", Color = Color.FromRgb(135, 206, 235) },
                new RowColorItem { Name = "파랑", Value = "CornflowerBlue", Color = Color.FromRgb(100, 149, 237) },
                new RowColorItem { Name = "보라", Value = "MediumPurple", Color = Color.FromRgb(147, 112, 219) },
                new RowColorItem { Name = "분홍", Value = "HotPink", Color = Color.FromRgb(255, 105, 180) },
                new RowColorItem { Name = "갈색", Value = "Peru", Color = Color.FromRgb(205, 133, 63) },
                new RowColorItem { Name = "회색", Value = "Gray", Color = Color.FromRgb(128, 128, 128) }
            };
        }
    }
}
