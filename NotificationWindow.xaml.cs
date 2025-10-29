using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FACTOVA_QueryHelper
{
    /// <summary>
    /// �˸� �˾� â
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
            
            // �ѱ� �ؽ�Ʈ ���� (�ڵ�����ε忡��)
            Title = "��ȸ ��� �˸�";
            HeaderTextBlock.Text = "�˸��� �ֽ��ϴ�:";
            OkButton.Content = "Ȯ��";
            
            NotificationsItemsControl.ItemsSource = notifications;
            
            // â�� �ε�� �� �۾�ǥ���� ������ ����
            Loaded += NotificationWindow_Loaded;
        }

        private void NotificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // â�� �۾�ǥ���ٿ��� �����̰� �����
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
                        uCount = 5, // 5�� ������
                        dwTimeout = 0
                    };

                    FlashWindowEx(ref info);
                }
            }
            catch
            {
                // ������ ���� �� ����
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
