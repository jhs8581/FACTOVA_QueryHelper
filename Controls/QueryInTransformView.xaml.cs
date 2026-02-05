using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FACTOVA_QueryHelper.Controls
{
    public partial class QueryInTransformView : UserControl
    {
        public QueryInTransformView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// IN 조건 변환 버튼 클릭
        /// </summary>
        private void TransformButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inputText = InputTextBox.Text;
                
                if (string.IsNullOrWhiteSpace(inputText))
                {
                    MessageBox.Show("변환할 데이터를 입력해주세요.", 
                        "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 🔥 줄 단위로 분리
                var lines = inputText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (lines.Length == 0)
                {
                    MessageBox.Show("유효한 데이터가 없습니다.", 
                        "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 🔥 각 줄을 트림하고 작은따옴표로 감싸기
                var transformedValues = lines
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => $"'{line}'")
                    .ToList();

                if (transformedValues.Count == 0)
                {
                    MessageBox.Show("유효한 데이터가 없습니다.", 
                        "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 🔥 쉼표로 연결
                var result = string.Join(",\n", transformedValues);

                // 🔥 결과 출력
                OutputTextBox.Text = result;
}
            catch (Exception ex)
            {
MessageBox.Show($"변환 중 오류가 발생했습니다:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 초기화 버튼 클릭
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InputTextBox.Text = string.Empty;
                OutputTextBox.Text = string.Empty;
}
            catch (Exception ex)
            {
}
        }

        /// <summary>
        /// 복사 버튼 클릭
        /// </summary>
        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var outputText = OutputTextBox.Text;
                
                if (string.IsNullOrWhiteSpace(outputText))
                {
                    MessageBox.Show("복사할 데이터가 없습니다.", 
                        "복사 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 🔥 클립보드에 복사
                Clipboard.SetText(outputText);
                
                MessageBox.Show("클립보드에 복사되었습니다.", 
                    "복사 완료", MessageBoxButton.OK, MessageBoxImage.Information);
}
            catch (Exception ex)
            {
MessageBox.Show($"클립보드 복사 중 오류가 발생했습니다:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
