using System.Configuration;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using OfficeOpenXml;
using System.Runtime.InteropServices;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 🔥 Windows 메시지 상수
        private const int WM_MOUSEHWHEEL = 0x020E; // 가로 휠 메시지

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
            
            // 🔥 전역 Shift+휠 가로 스크롤 이벤트 등록
            EventManager.RegisterClassHandler(typeof(DataGrid), 
                UIElement.PreviewMouseWheelEvent, 
                new MouseWheelEventHandler(OnDataGridPreviewMouseWheel));
            
            EventManager.RegisterClassHandler(typeof(ScrollViewer), 
                UIElement.PreviewMouseWheelEvent, 
                new MouseWheelEventHandler(OnScrollViewerPreviewMouseWheel));
            
            // 🔥 윈도우 로드 시 가로 휠 메시지 Hook 등록
            EventManager.RegisterClassHandler(typeof(Window),
                Window.LoadedEvent,
                new RoutedEventHandler(OnWindowLoaded));
        }

        /// <summary>
        /// 🔥 윈도우 로드 시 WM_MOUSEHWHEEL 메시지 Hook 등록
        /// </summary>
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                var source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
                source?.AddHook(WndProc);
            }
        }

        /// <summary>
        /// 🔥 Windows 메시지 처리 (터치패드/틸트 휠 가로 스크롤)
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEHWHEEL)
            {
                // wParam의 상위 워드에 델타 값이 있음
                int delta = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
                
                // 마우스 위치에서 DataGrid 또는 ScrollViewer 찾기
                var point = Mouse.GetPosition(null);
                var element = Mouse.DirectlyOver as DependencyObject;
                
                if (element != null)
                {
                    // DataGrid 찾기
                    var dataGrid = FindParent<DataGrid>(element);
                    if (dataGrid != null)
                    {
                        var scrollViewer = GetScrollViewer(dataGrid);
                        if (scrollViewer != null)
                        {
                            // delta > 0: 왼쪽, delta < 0: 오른쪽
                            if (delta > 0)
                                scrollViewer.LineLeft();
                            else
                                scrollViewer.LineRight();
                            
                            handled = true;
                            return IntPtr.Zero;
                        }
                    }
                    
                    // ScrollViewer 찾기
                    var sv = FindParent<ScrollViewer>(element);
                    if (sv != null)
                    {
                        if (delta > 0)
                            sv.LineLeft();
                        else
                            sv.LineRight();
                        
                        handled = true;
                        return IntPtr.Zero;
                    }
                }
            }
            
            return IntPtr.Zero;
        }

        /// <summary>
        /// 🔥 부모 요소 찾기
        /// </summary>
        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        /// <summary>
        /// 🔥 DataGrid Shift+휠 가로 스크롤
        /// </summary>
        private void OnDataGridPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift && sender is DataGrid dataGrid)
            {
                var scrollViewer = GetScrollViewer(dataGrid);
                if (scrollViewer != null)
                {
                    if (e.Delta > 0)
                        scrollViewer.LineRight();
                    else
                        scrollViewer.LineLeft();
                    
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 🔥 ScrollViewer Shift+휠 가로 스크롤
        /// </summary>
        private void OnScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift && sender is ScrollViewer scrollViewer)
            {
                if (e.Delta > 0)
                    scrollViewer.LineRight();
                else
                    scrollViewer.LineLeft();
                
                e.Handled = true;
            }
        }

        /// <summary>
        /// 🔥 DataGrid 내부 ScrollViewer 찾기
        /// </summary>
        private static ScrollViewer? GetScrollViewer(DependencyObject obj)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is ScrollViewer scrollViewer)
                    return scrollViewer;
                
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
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

                    }
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
}
        }
    }
}
