using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.Xml;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class JsonInputDialog : Window
    {
        public Dictionary<string, string>? ParsedData { get; private set; }

        public JsonInputDialog()
        {
            InitializeComponent();
            
            // 포커스를 TextBox로
            Loaded += (s, e) => JsonTextBox.Focus();
        }

        /// <summary>
        /// 적용 버튼 클릭
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inputText = JsonTextBox.Text?.Trim();

                if (string.IsNullOrWhiteSpace(inputText))
                {
                    StatusText.Text = "⚠️ 데이터를 입력해주세요.";
                    return;
                }

                // XML 또는 JSON 자동 감지 및 파싱
                if (inputText.StartsWith("<"))
                {
                    // XML 파싱
                    ParseXml(inputText);
                }
                else if (inputText.StartsWith("{"))
                {
                    // JSON 파싱
                    ParseJson(inputText);
                }
                else
                {
                    StatusText.Text = "❌ XML(&lt;로 시작) 또는 JSON({로 시작) 형식이어야 합니다.";
                    return;
                }

                if (ParsedData != null && ParsedData.Count > 0)
                {
DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"❌ 오류:\n{ex.Message}";
}
        }

        /// <summary>
        /// XML 파싱 - 중첩된 요소에서 변수 추출 (중복 시 첫 번째 값 사용)
        /// </summary>
        private void ParseXml(string xmlText)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlText);

                ParsedData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // 모든 요소를 순회하면서 텍스트 값이 있는 요소만 추출
                TraverseXmlNodes(xmlDoc.DocumentElement);
StatusText.Text = "";
            }
            catch (XmlException ex)
            {
                StatusText.Text = $"❌ XML 파싱 오류:\n{ex.Message}";
throw;
            }
        }

        /// <summary>
        /// XML 노드를 재귀적으로 순회하면서 변수 추출
        /// </summary>
        private void TraverseXmlNodes(XmlNode? node)
        {
            if (node == null)
                return;

            // 텍스트 값이 있는 리프 노드인 경우
            if (node.HasChildNodes && node.ChildNodes.Count == 1 && node.FirstChild?.NodeType == XmlNodeType.Text)
            {
                var variableName = node.Name;
                var value = node.InnerText?.Trim() ?? "";

                // 🔥 중복된 변수가 있으면 첫 번째 값만 사용 (이미 존재하면 무시)
                if (!ParsedData!.ContainsKey(variableName))
                {
                    ParsedData[variableName] = value;
                    
                }
                else
                {
                    
                }
            }

            // 자식 노드 순회
            if (node.HasChildNodes)
            {
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    if (childNode.NodeType == XmlNodeType.Element)
                    {
                        TraverseXmlNodes(childNode);
                    }
                }
            }
        }

        /// <summary>
        /// JSON 파싱
        /// </summary>
        private void ParseJson(string jsonText)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(jsonText);
                var root = jsonDoc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    StatusText.Text = "❌ JSON은 객체 형식이어야 합니다. { } 를 사용하세요.";
                    return;
                }

                ParsedData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var property in root.EnumerateObject())
                {
                    var key = property.Name;
                    var value = property.Value.ValueKind == JsonValueKind.String 
                        ? property.Value.GetString() ?? ""
                        : property.Value.ToString();
                    
                    ParsedData[key] = value;
                    
                }
StatusText.Text = "";
            }
            catch (JsonException ex)
            {
                StatusText.Text = $"❌ JSON 파싱 오류:\n{ex.Message}";
throw;
            }
        }

        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
