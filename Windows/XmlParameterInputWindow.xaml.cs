using System;
using System.Collections.Generic;
using System.Windows;
using System.Xml.Linq;

namespace FACTOVA_QueryHelper.Windows
{
    /// <summary>
    /// XmlParameterInputWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class XmlParameterInputWindow : Window
    {
        public Dictionary<string, string> ParsedParameters { get; private set; }

        public XmlParameterInputWindow()
        {
            InitializeComponent();
            ParsedParameters = new Dictionary<string, string>();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string xmlText = XmlTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(xmlText))
            {
                MessageBox.Show("XML 데이터를 입력하세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // XML 파싱
                var xdoc = XDocument.Parse(xmlText);
                var newDataSet = xdoc.Root;

                if (newDataSet == null || newDataSet.Name.LocalName != "NewDataSet")
                {
                    MessageBox.Show("XML 루트 요소가 'NewDataSet'이 아닙니다.", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ParsedParameters.Clear();

                // NewDataSet의 모든 자식 요소 순회 (IN_DATA, IN_PARAM 등)
                foreach (var childElement in newDataSet.Elements())
                {
                    // 각 하위 그룹의 모든 요소 순회
                    foreach (var paramElement in childElement.Elements())
                    {
                        string paramName = paramElement.Name.LocalName;
                        string paramValue = paramElement.Value;

                        // 중복된 파라미터명이 있을 경우 첫 번째 값만 사용
                        if (!ParsedParameters.ContainsKey(paramName))
                        {
                            ParsedParameters[paramName] = paramValue;
                        }
                    }
                }

                if (ParsedParameters.Count == 0)
                {
                    MessageBox.Show("파싱된 파라미터가 없습니다.\nXML 형식을 확인하세요.", "알림",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"XML 파싱 오류:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
