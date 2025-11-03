using System.Configuration;
using System.Data;
using System.Text;
using System.Windows;
using OfficeOpenXml;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // UTF-8 인코딩 설정 (한글 깨짐 방지)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            
            // EPPlus 라이선스 설정 (비상업용)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
    }
}
