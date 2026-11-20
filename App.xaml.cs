using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;

namespace WpfApp1
{
    public partial class App : Application
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        protected override void OnStartup(StartupEventArgs e)
        {
            // Русская культура: даты, календарь, числа
            var ru = new CultureInfo("ru-RU");
            CultureInfo.DefaultThreadCurrentCulture = ru;
            CultureInfo.DefaultThreadCurrentUICulture = ru;
            Thread.CurrentThread.CurrentCulture = ru;
            Thread.CurrentThread.CurrentUICulture = ru;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(ru.IetfLanguageTag)));

            // Тёмная строка заголовка у всех окон (Windows 10/11)
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnWindowLoaded));

            base.OnStartup(e);
        }

        private static void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(sender is Window window)) return;
                IntPtr handle = new WindowInteropHelper(window).Handle;
                if (handle == IntPtr.Zero) return;
                int on = 1;
                if (DwmSetWindowAttribute(handle, 20, ref on, sizeof(int)) != 0)
                    DwmSetWindowAttribute(handle, 19, ref on, sizeof(int));
            }
            catch { /* старая версия Windows - остаётся обычный заголовок */ }
        }
    }
}
