using System;
using System.Windows;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// �Է°� ������ ����ϴ� Ŭ����
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// ���� �� ��ȣ�� �����մϴ�.
        /// </summary>
        public static bool ValidateStartRow(string startRowText, out int startRow)
        {
            startRow = 2;

            if (!int.TryParse(startRowText, out startRow) || startRow < 1)
            {
                MessageBox.Show("���� �� ��ȣ�� 1 �̻��̾�� �մϴ�.", "����",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// ���� ���� �ֱ⸦ �����մϴ�.
        /// </summary>
        public static bool ValidateQueryInterval(string intervalText, out int interval, int minimumSeconds = 5)
        {
            interval = 0;

            if (!int.TryParse(intervalText, out interval) || interval < minimumSeconds)
            {
                MessageBox.Show($"���� ���� �ֱ�� {minimumSeconds}�� �̻��̾�� �մϴ�.", "����",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// ���ڿ��� ������� ������ �����մϴ�.
        /// </summary>
        public static bool ValidateNotEmpty(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show($"{fieldName}��(��) �Է��ϼ���.", "�˸�",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// ����Ʈ�� ������� ������ �����մϴ�.
        /// </summary>
        public static bool ValidateListNotEmpty<T>(System.Collections.Generic.List<T> list, string listName)
        {
            if (list == null || list.Count == 0)
            {
                MessageBox.Show($"{listName}��(��) ����ֽ��ϴ�.", "�˸�",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }

        /// <summary>
        /// ���õ� �׸��� �ִ��� �����մϴ�.
        /// </summary>
        public static bool ValidateSelection(object? selectedItem, string itemName)
        {
            if (selectedItem == null)
            {
                MessageBox.Show($"{itemName}��(��) �����ϼ���.", "�˸�",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }
    }
}
