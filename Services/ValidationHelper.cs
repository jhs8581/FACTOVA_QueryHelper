using System;
using System.Windows;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// 입력값 검증을 담당하는 클래스
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// 시작 행 번호를 검증합니다.
        /// </summary>
        public static bool ValidateStartRow(string startRowText, out int startRow)
        {
            startRow = 2;

            if (!int.TryParse(startRowText, out startRow) || startRow < 1)
            {
                MessageBox.Show("시작 행 번호는 1 이상이어야 합니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 쿼리 실행 주기를 검증합니다.
        /// </summary>
        public static bool ValidateQueryInterval(string intervalText, out int interval, int minimumSeconds = 5)
        {
            interval = 0;

            if (!int.TryParse(intervalText, out interval) || interval < minimumSeconds)
            {
                MessageBox.Show($"쿼리 실행 주기는 {minimumSeconds}초 이상이어야 합니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 문자열이 비어있지 않은지 검증합니다.
        /// </summary>
        public static bool ValidateNotEmpty(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show($"{fieldName}을(를) 입력해주세요.", "입력오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 리스트가 비어있지 않은지 검증합니다.
        /// </summary>
        public static bool ValidateListNotEmpty<T>(System.Collections.Generic.List<T> list, string listName)
        {
            if (list == null || list.Count == 0)
            {
                MessageBox.Show($"{listName}이(가) 비어있습니다.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 항목이 선택되었는지 검증합니다.
        /// </summary>
        public static bool ValidateSelection(object? selectedItem, string itemName)
        {
            if (selectedItem == null)
            {
                MessageBox.Show($"{itemName}을(를) 선택해주세요.", "입력오류",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }
    }
}
