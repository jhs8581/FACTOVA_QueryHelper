using System.Configuration;
using System.Data;
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
            
            // EPPlus 라이선스 설정 (비상업용)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
    }
}
