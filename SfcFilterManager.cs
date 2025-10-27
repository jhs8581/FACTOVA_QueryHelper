using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// SFC ���� ��� ���͸��� �����ϴ� Ŭ����
    /// </summary>
    public class SfcFilterManager
    {
        private readonly ObservableCollection<SfcEquipmentInfo> _sourceList;
        private readonly ObservableCollection<SfcEquipmentInfo> _filteredList;
        private readonly ObservableCollection<CheckableComboBoxItem> _statusFilterItems;
        private readonly ObservableCollection<CheckableComboBoxItem> _bizActorFilterItems;

        public SfcFilterManager(
            ObservableCollection<SfcEquipmentInfo> sourceList,
            ObservableCollection<SfcEquipmentInfo> filteredList,
            ObservableCollection<CheckableComboBoxItem> statusFilterItems,
            ObservableCollection<CheckableComboBoxItem> bizActorFilterItems)
        {
            _sourceList = sourceList ?? throw new ArgumentNullException(nameof(sourceList));
            _filteredList = filteredList ?? throw new ArgumentNullException(nameof(filteredList));
            _statusFilterItems = statusFilterItems ?? throw new ArgumentNullException(nameof(statusFilterItems));
            _bizActorFilterItems = bizActorFilterItems ?? throw new ArgumentNullException(nameof(bizActorFilterItems));
        }

        /// <summary>
        /// ���� ����
        /// </summary>
        public class FilterCriteria
        {
            public string IpAddress { get; set; } = string.Empty;
            public string EquipmentId { get; set; } = string.Empty;
            public string EquipmentName { get; set; } = string.Empty;
        }

        /// <summary>
        /// ���͸� �����մϴ�.
        /// </summary>
        public FilterResult ApplyFilter(FilterCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            // ���� ���� ����ȭ
            string ipFilter = criteria.IpAddress?.Trim().ToLower() ?? "";
            string equipmentIdFilter = criteria.EquipmentId?.Trim().ToLower() ?? "";
            string equipmentNameFilter = criteria.EquipmentName?.Trim().ToLower() ?? "";

            // ���õ� ���� ���
            var selectedStatuses = _statusFilterItems
                .Where(i => i.IsChecked && i.Text != "��ü")
                .Select(i => i.Text)
                .ToList();
            bool isAllStatusSelected = _statusFilterItems.FirstOrDefault(i => i.Text == "��ü")?.IsChecked ?? true;

            // ���õ� BIZACTOR ���
            var selectedBizActors = _bizActorFilterItems
                .Where(i => i.IsChecked && i.Text != "��ü")
                .Select(i => i.Text)
                .ToList();
            bool isAllBizActorSelected = _bizActorFilterItems.FirstOrDefault(i => i.Text == "��ü")?.IsChecked ?? true;

            // ���͸� ����
            var filtered = _sourceList.Where(item =>
            {
                // IP �ּ� ����
                if (!string.IsNullOrEmpty(ipFilter) && 
                    !item.IpAddress.ToLower().Contains(ipFilter))
                    return false;

                // ���� ID ����
                if (!string.IsNullOrEmpty(equipmentIdFilter) && 
                    !item.EquipmentId.ToLower().Contains(equipmentIdFilter))
                    return false;

                // ����� ����
                if (!string.IsNullOrEmpty(equipmentNameFilter) && 
                    !item.EquipmentName.ToLower().Contains(equipmentNameFilter))
                    return false;

                // ���� ���� (��Ƽ����Ʈ)
                if (!isAllStatusSelected && selectedStatuses.Count > 0)
                {
                    if (!selectedStatuses.Contains(item.Status))
                        return false;
                }

                // BIZACTOR ���� (��Ƽ����Ʈ)
                if (!isAllBizActorSelected && selectedBizActors.Count > 0)
                {
                    if (!selectedBizActors.Contains(item.BizActor))
                        return false;
                }

                return true;
            }).ToList();

            // ���͸��� ����Ʈ ������Ʈ
            _filteredList.Clear();
            foreach (var item in filtered)
            {
                _filteredList.Add(item);
            }

            return new FilterResult
            {
                FilteredCount = filtered.Count,
                TotalCount = _sourceList.Count,
                IsFiltered = filtered.Count != _sourceList.Count
            };
        }

        /// <summary>
        /// ��� ���͸� �ʱ�ȭ�մϴ�.
        /// </summary>
        public void ClearAllFilters()
        {
            // ���� ���� �ʱ�ȭ
            foreach (var item in _statusFilterItems)
            {
                item.IsChecked = item.Text == "��ü";
            }

            // BIZACTOR ���� �ʱ�ȭ
            foreach (var item in _bizActorFilterItems)
            {
                item.IsChecked = item.Text == "��ü";
            }
        }

        /// <summary>
        /// ���� �޺��ڽ� �ؽ�Ʈ�� ������Ʈ�մϴ�.
        /// </summary>
        public FilterComboBoxText GetFilterComboBoxText()
        {
            var result = new FilterComboBoxText();

            // ���� ���� �ؽ�Ʈ
            var checkedStatusItems = _statusFilterItems.Where(i => i.IsChecked && i.Text != "��ü").ToList();
            if (checkedStatusItems.Count == 0 || 
                _statusFilterItems.FirstOrDefault(i => i.Text == "��ü")?.IsChecked == true)
            {
                result.StatusText = "��ü";
            }
            else
            {
                result.StatusText = string.Join(", ", checkedStatusItems.Select(i => i.Text));
            }

            // BIZACTOR ���� �ؽ�Ʈ
            var checkedBizActorItems = _bizActorFilterItems.Where(i => i.IsChecked && i.Text != "��ü").ToList();
            if (checkedBizActorItems.Count == 0 || 
                _bizActorFilterItems.FirstOrDefault(i => i.Text == "��ü")?.IsChecked == true)
            {
                result.BizActorText = "��ü";
            }
            else
            {
                result.BizActorText = string.Join(", ", checkedBizActorItems.Select(i => i.Text));
            }

            return result;
        }

        /// <summary>
        /// ���� üũ�ڽ� ������ ó���մϴ�.
        /// </summary>
        public void HandleCheckBoxChanged(CheckableComboBoxItem changedItem)
        {
            if (changedItem == null)
                return;

            var collection = _statusFilterItems.Contains(changedItem) 
                ? _statusFilterItems 
                : _bizActorFilterItems;

            // "��ü"�� �����ϸ� �ٸ� ��� �׸� ���� ����
            if (changedItem.Text == "��ü" && changedItem.IsChecked)
            {
                foreach (var otherItem in collection.Where(i => i.Text != "��ü"))
                {
                    otherItem.IsChecked = false;
                }
            }
            // �ٸ� �׸��� �����ϸ� "��ü" ���� ����
            else if (changedItem.Text != "��ü" && changedItem.IsChecked)
            {
                var allItem = collection.FirstOrDefault(i => i.Text == "��ü");
                if (allItem != null)
                {
                    allItem.IsChecked = false;
                }
            }
            // ��� �׸��� ���� �����Ǹ� "��ü" ����
            else if (!changedItem.IsChecked)
            {
                if (!collection.Any(i => i.IsChecked))
                {
                    var allItem = collection.FirstOrDefault(i => i.Text == "��ü");
                    if (allItem != null)
                    {
                        allItem.IsChecked = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// ���� ���� ���
    /// </summary>
    public class FilterResult
    {
        public int FilteredCount { get; set; }
        public int TotalCount { get; set; }
        public bool IsFiltered { get; set; }

        public string GetStatusMessage()
        {
            if (!IsFiltered)
            {
                return $"��ü {TotalCount}��";
            }
            else
            {
                return $"���͸�: {FilteredCount}�� / ��ü: {TotalCount}��";
            }
        }
    }

    /// <summary>
    /// ���� �޺��ڽ� �ؽ�Ʈ
    /// </summary>
    public class FilterComboBoxText
    {
        public string StatusText { get; set; } = "��ü";
        public string BizActorText { get; set; } = "��ü";
    }
}
