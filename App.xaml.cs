using System.Configuration;
using System.Data;
using System.Text;
using System.Windows;
using OfficeOpenXml;
using System.Runtime.InteropServices;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 작업 표시줄 고정을 위한 Application User Model ID 설정
            SetCurrentProcessExplicitAppUserModelID("FACTOVA.QueryHelper.WPF.1.6.0");
            
            // UTF-8 인코딩 설정 (한글 깨짐 방지)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            // EPPlus 라이선스 설정 (비상업용)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
    }
}
