using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    /// <summary>Логотип магазина: берётся из файла Assets\logo.png рядом с программой.</summary>
    public static class Branding
    {
        private static BitmapImage _logo;
        private static BitmapSource _logoLight;
        private static bool _loaded;

        /// <summary>Логотип для тёмного фона: если у картинки есть прозрачность, рисуется белым.</summary>
        public static BitmapSource LogoLight
        {
            get
            {
                var logo = Logo;
                if (logo == null) return null;
                if (_logoLight == null)
                {
                    try { _logoLight = MakeLight(logo); }
                    catch { _logoLight = logo; }
                }
                return _logoLight;
            }
        }

        private static BitmapSource MakeLight(BitmapSource source)
        {
            var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            int width = converted.PixelWidth, height = converted.PixelHeight, stride = width * 4;
            var pixels = new byte[stride * height];
            converted.CopyPixels(pixels, stride, 0);

            bool hasAlpha = false;
            for (int i = 3; i < pixels.Length; i += 4)
                if (pixels[i] < 250) { hasAlpha = true; break; }
            if (!hasAlpha) return source;   // непрозрачную картинку не трогаем

            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 255; pixels[i + 1] = 255; pixels[i + 2] = 255;   // B, G, R = белый, альфа сохраняется
            }
            var result = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
            result.Freeze();
            return result;
        }

        public static BitmapImage Logo
        {
            get
            {
                if (!_loaded) { _loaded = true; _logo = TryLoad(); }
                return _logo;
            }
        }

        private static BitmapImage TryLoad()
        {
            foreach (var name in new[] { "logo.png", "logo.jpg", "logo.jpeg" })
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Assets", name);
                if (!File.Exists(path)) continue;
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(path);
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                catch { /* повреждённый файл - работаем без логотипа */ }
            }
            return null;
        }
    }
}
