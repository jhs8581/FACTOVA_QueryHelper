using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// SFC 설비 목록 필터링을 담당하는 클래스
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
        /// 필터 조건
        /// </summary>
        public class FilterCriteria
        {
            public string IpAddress { get; set; } = string.Empty;
            public string EquipmentId { get; set; } = string.Empty;
            public string EquipmentName { get; set; } = string.Empty;
        }

        /// <summary>
        /// 필터를 적용합니다.
        /// </summary>
        public FilterResult ApplyFilter(FilterCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            // 입력 필터 정규화
            string ipFilter = criteria.IpAddress?.Trim().ToLower() ?? "";
            string equipmentIdFilter = criteria.EquipmentId?.Trim().ToLower() ?? "";
            string equipmentNameFilter = criteria.EquipmentName?.Trim().ToLower() ?? "";

            // 선택된 상태 목록
            var selectedStatuses = _statusFilterItems
                .Where(i => i.IsChecked && i.Text != "전체")
                .Select(i => i.Text)
                .ToList();
            bool isAllStatusSelected = _statusFilterItems.FirstOrDefault(i => i.Text == "전체")?.IsChecked ?? false;

            // 선택된 BIZACTOR 목록
            var selectedBizActors = _bizActorFilterItems
                .Where(i => i.IsChecked && i.Text != "전체")
                .Select(i => i.Text)
                .ToList();
            bool isAllBizActorSelected = _bizActorFilterItems.FirstOrDefault(i => i.Text == "전체")?.IsChecked ?? false;

            // 필터링 수행
            var filtered = _sourceList.Where(item =>
            {
                // IP 주소 필터
                if (!string.IsNullOrEmpty(ipFilter) && 
                    !item.IpAddress.ToLower().Contains(ipFilter))
                    return false;

                // 설비 ID 필터
                if (!string.IsNullOrEmpty(equipmentIdFilter) && 
                    !item.EquipmentId.ToLower().Contains(equipmentIdFilter))
                    return false;

                // 설비명 필터
                if (!string.IsNullOrEmpty(equipmentNameFilter) && 
                    !item.EquipmentName.ToLower().Contains(equipmentNameFilter))
                    return false;

                // 상태 필터 (멀티셀렉트)
                // "전체"가 선택되지 않았고, 특정 항목이 선택되었을 때만 필터링
                if (!isAllStatusSelected && selectedStatuses.Count > 0)
                {
                    if (!selectedStatuses.Contains(item.Status))
                        return false;
                }

                // BIZACTOR 필터 (멀티셀렉트)
                // "전체"가 선택되지 않았고, 특정 항목이 선택되었을 때만 필터링
                if (!isAllBizActorSelected && selectedBizActors.Count > 0)
                {
                    if (!selectedBizActors.Contains(item.BizActor))
                        return false;
                }

                return true;
            }).ToList();

            // 필터링된 리스트 업데이트
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
        /// 모든 필터를 초기화합니다.
        /// </summary>
        public void ClearAllFilters()
        {
            // 상태 필터 초기화 - OFF만 체크
            foreach (var item in _statusFilterItems)
            {
                if (item.Text == "OFF")
                {
                    item.IsChecked = true;
                }
                else
                {
                    item.IsChecked = false;
                }
            }

            // BIZACTOR 필터 초기화 - 전체 체크
            foreach (var item in _bizActorFilterItems)
            {
                item.IsChecked = item.Text == "전체";
            }
        }

        /// <summary>
        /// 필터 콤보박스 텍스트를 업데이트합니다.
        /// </summary>
        public FilterComboBoxText GetFilterComboBoxText()
        {
            var result = new FilterComboBoxText();

            // 상태 필터 텍스트
            var checkedStatusItems = _statusFilterItems.Where(i => i.IsChecked && i.Text != "전체").ToList();
            var allStatusItem = _statusFilterItems.FirstOrDefault(i => i.Text == "전체");
            
            if (allStatusItem?.IsChecked == true)
            {
                result.StatusText = "전체";
            }
            else if (checkedStatusItems.Count == 0)
            {
                result.StatusText = "선택 안 됨";
            }
            else
            {
                result.StatusText = string.Join(", ", checkedStatusItems.Select(i => i.Text));
            }

            // BIZACTOR 필터 텍스트
            var checkedBizActorItems = _bizActorFilterItems.Where(i => i.IsChecked && i.Text != "전체").ToList();
            var allBizActorItem = _bizActorFilterItems.FirstOrDefault(i => i.Text == "전체");
            
            if (allBizActorItem?.IsChecked == true)
            {
                result.BizActorText = "전체";
            }
            else if (checkedBizActorItems.Count == 0)
            {
                result.BizActorText = "선택 안 됨";
            }
            else
            {
                result.BizActorText = string.Join(", ", checkedBizActorItems.Select(i => i.Text));
            }

            return result;
        }

        /// <summary>
        /// 필터 체크박스 변경을 처리합니다.
        /// </summary>
        public void HandleCheckBoxChanged(CheckableComboBoxItem changedItem)
        {
            if (changedItem == null)
                return;

            var collection = _statusFilterItems.Contains(changedItem) 
                ? _statusFilterItems 
                : _bizActorFilterItems;

            // "전체"가 선택되면 다른 모든 항목 해제
            if (changedItem.Text == "전체" && changedItem.IsChecked)
            {
                foreach (var otherItem in collection.Where(i => i.Text != "전체"))
                {
                    otherItem.IsChecked = false;
                }
            }
            // 다른 항목이 선택되면 "전체" 항목 해제
            else if (changedItem.Text != "전체" && changedItem.IsChecked)
            {
                var allItem = collection.FirstOrDefault(i => i.Text == "전체");
                if (allItem != null)
                {
                    allItem.IsChecked = false;
                }
            }
            // 모든 항목이 해제 상태이면 "전체" 선택
            else if (!changedItem.IsChecked)
            {
                if (!collection.Any(i => i.IsChecked))
                {
                    var allItem = collection.FirstOrDefault(i => i.Text == "전체");
                    if (allItem != null)
                    {
                        allItem.IsChecked = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 필터 결과 클래스
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
                return $"전체 {TotalCount}개";
            }
            else
            {
                return $"필터링: {FilteredCount}개 / 전체: {TotalCount}개";
            }
        }
    }

    /// <summary>
    /// 필터 콤보박스 텍스트
    /// </summary>
    public class FilterComboBoxText
    {
        public string StatusText { get; set; } = "전체";
        public string BizActorText { get; set; } = "전체";
    }
}
