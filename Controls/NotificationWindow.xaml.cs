using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FACTOVA_QueryHelper.Controls
{
    /// <summary>
    /// 알림 팝업 창
    /// </summary>
    public partial class NotificationWindow : Window
    {
        #region Win32 API for Flashing Window

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        private const uint FLASHW_ALL = 3;
        private const uint FLASHW_TIMERNOFG = 12;

        #endregion

        public NotificationWindow(List<string> notifications)
        {
            InitializeComponent();
            
            // 한글 텍스트 설정 (코드비하인드에서)
            Title = "조회 결과 알림";
            HeaderTextBlock.Text = "알림이 있습니다:";
            OkButton.Content = "확인";
            
            NotificationsItemsControl.ItemsSource = notifications;
            
            // 창이 로드된 후 작업표시줄 깜빡임 시작
            Loaded += NotificationWindow_Loaded;
        }

        private void NotificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 창을 작업표시줄에서 깜빡이게 만들기
            FlashWindow();
        }

        private void FlashWindow()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    var info = new FLASHWINFO
                    {
                        cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                        hwnd = helper.Handle,
                        dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                        uCount = 5, // 5번 깜빡임
                        dwTimeout = 0
                    };

                    FlashWindowEx(ref info);
                }
            }
            catch
            {
                // 깜빡임 실패 시 무시
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
