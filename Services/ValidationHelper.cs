using System;
using System.Windows;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// 占쌉력곤옙 占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙求占?클占쏙옙占쏙옙
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// 占쏙옙占쏙옙 占쏙옙 占쏙옙호占쏙옙 占쏙옙占쏙옙占쌌니댐옙.
        /// </summary>
        public static bool ValidateStartRow(string startRowText, out int startRow)
        {
            startRow = 2;

            if (!int.TryParse(startRowText, out startRow) || startRow < 1)
            {
                MessageBox.Show("占쏙옙占쏙옙 占쏙옙 占쏙옙호占쏙옙 1 占싱삼옙占싱억옙占?占쌌니댐옙.", "占쏙옙占쏙옙",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 占쏙옙占쏙옙 占쏙옙占쏙옙 占쌍기를 占쏙옙占쏙옙占쌌니댐옙.
        /// </summary>
        public static bool ValidateQueryInterval(string intervalText, out int interval, int minimumSeconds = 5)
        {
            interval = 0;

            if (!int.TryParse(intervalText, out interval) || interval < minimumSeconds)
            {
                MessageBox.Show($"占쏙옙占쏙옙 占쏙옙占쏙옙 占쌍깍옙占?{minimumSeconds}占쏙옙 占싱삼옙占싱억옙占?占쌌니댐옙.", "占쏙옙占쏙옙",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 占쏙옙占쌘울옙占쏙옙 占쏙옙占쏙옙占쏙옙占?占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쌌니댐옙.
        /// </summary>
        public static bool ValidateNotEmpty(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show($"{fieldName}占쏙옙(占쏙옙) 占쌉뤄옙占싹쇽옙占쏙옙.", "占싯몌옙",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 占쏙옙占쏙옙트占쏙옙 占쏙옙占쏙옙占쏙옙占?占쏙옙占쏙옙占쏙옙 占쏙옙占쏙옙占쌌니댐옙.
        /// </summary>
        public static bool ValidateListNotEmpty<T>(System.Collections.Generic.List<T> list, string listName)
        {
            if (list == null || list.Count == 0)
            {
                MessageBox.Show($"{listName}???놁뒿?덈떎.", "?뚮┝",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 占쏙옙占시듸옙 占쌓몌옙占쏙옙 占쌍댐옙占쏙옙 占쏙옙占쏙옙占쌌니댐옙.
        /// </summary>
        public static bool ValidateSelection(object? selectedItem, string itemName)
        {
            if (selectedItem == null)
            {
                MessageBox.Show($"{itemName}占쏙옙(占쏙옙) 占쏙옙占쏙옙占싹쇽옙占쏙옙.", "占싯몌옙",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }
    }
}
