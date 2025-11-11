using System.Configuration;
using System.Data;
using System.IO;
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
            
            // TNS_ADMIN 환경변수 설정 (tnsnames.ora 파일 경로)
            SetTnsAdminPath();
        }

        /// <summary>
        /// TNS_ADMIN 환경변수를 설정하여 tnsnames.ora 파일을 찾을 수 있도록 합니다.
        /// </summary>
        private void SetTnsAdminPath()
        {
            try
            {
                // 사용자 설정에서 TNS 경로 읽기
                var settings = Services.UserSettingsService.Load();
                var tnsPath = settings.TnsFilePath;
                
                // 경로가 없으면 기본 경로 사용
                if (string.IsNullOrEmpty(tnsPath))
                {
                    tnsPath = Services.TnsParserService.GetDefaultTnsPath();
                }
                
                // tnsnames.ora 파일이 있는 디렉토리 경로 추출
                if (!string.IsNullOrEmpty(tnsPath) && File.Exists(tnsPath))
                {
                    var tnsDirectory = Path.GetDirectoryName(tnsPath);
                    
                    if (!string.IsNullOrEmpty(tnsDirectory))
                    {
                        // 현재 프로세스의 환경변수 설정 (앱 실행 중에만 유효)
                        Environment.SetEnvironmentVariable("TNS_ADMIN", tnsDirectory);
                        System.Diagnostics.Debug.WriteLine($"✅ TNS_ADMIN set to: {tnsDirectory}");
                        System.Diagnostics.Debug.WriteLine($"✅ tnsnames.ora file: {tnsPath}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ tnsnames.ora file not found: {tnsPath}");
                    System.Diagnostics.Debug.WriteLine($"⚠️ TNS_ADMIN not set - Oracle connections may fail with ORA-12154");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error setting TNS_ADMIN: {ex.Message}");
            }
        }
    }
}
