using System;
using System.Windows;

namespace WpfApp1
{
    /// <summary>Главное меню.</summary>
    public partial class MenuWindow : Window
    {
        public MenuWindow()
        {
            InitializeComponent();
            ApplyLogo();
            ApplyRole();
        }

        private void ApplyLogo()
        {
            var logo = Branding.Logo;
            if (logo == null) { LogoImage.Visibility = Visibility.Collapsed; return; }
            LogoImage.Source = Branding.LogoLight ?? logo;
            Icon = logo;
        }

        // Показываем только то, что разрешено роли
        private void ApplyRole()
        {
            UsersMenuItem.Visibility = Session.CanOpen("users") ? Visibility.Visible : Visibility.Collapsed;
            UserInfoText.Text = $"Вы вошли как: {Session.FullName} — {Session.RoleRu} (логин: {Session.Username})";
        }

        private void OpenList(Entity entity)
        {
            if (!Session.CanOpen(entity.Table))
            {
                MessageBox.Show("У вашей роли нет доступа к этому разделу.", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Hide();
            try { new ListWindow(entity).ShowDialog(); }
            finally { Show(); }
        }

        private void Games_Click(object sender, RoutedEventArgs e) => OpenList(Entities.BoardGames);
        private void Beverages_Click(object sender, RoutedEventArgs e) => OpenList(Entities.Beverages);
        private void Services_Click(object sender, RoutedEventArgs e) => OpenList(Entities.Services);
        private void Customers_Click(object sender, RoutedEventArgs e) => OpenList(Entities.Customers);
        private void Users_Click(object sender, RoutedEventArgs e) => OpenList(Entities.Users);

        // Выход из учётной записи -> окно авторизации
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Session.Clear();
            new MainWindow().Show();
            Close();
        }

        // Файл -> Обновить: перечитывает данные и роль пользователя из БД
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var table = Db.Query(
                    "SELECT first_name, last_name, role, is_active FROM users WHERE id = @id",
                    ("@id", Session.UserId));

                if (table.Rows.Count == 0 || !(bool)table.Rows[0]["is_active"])
                {
                    MessageBox.Show("Ваша учётная запись удалена или отключена. Войдите заново.",
                        "Обновление", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Logout_Click(sender, e);
                    return;
                }

                var row = table.Rows[0];
                Session.FullName = row["first_name"] + " " + row["last_name"];
                Session.Role = (string)row["role"];
                ApplyRole();
                MessageBox.Show("Данные обновлены.", "Обновление", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось обновить данные:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из программы?", "Выход", MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            new InfoWindow("О программе",
                "Информационная система «Магазин настольных игр»\n\n" +
                "Версия 1.0\n\n" +
                "Программа предназначена для учёта настольных игр, напитков, услуг и клиентов " +
                "магазина, а также для управления сотрудниками.\n\n" +
                "Технологии: C#, WPF, PostgreSQL (Npgsql).").ShowDialog();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            new InfoWindow("Как работает программа",
                "1. Вход. Введите логин и пароль. Кнопка «Показать» открывает пароль. " +
                "Ваша роль определяется автоматически.\n\n" +
                "2. Меню. Разделы выбираются на панели инструментов. Раздел «Пользователи» " +
                "доступен только администратору.\n\n" +
                "3. Таблицы. Кнопки «Добавить», «Редактировать», «Удалить» работают с выбранной " +
                "строкой. Для удаления нужно выбрать запись и подтвердить действие.\n\n" +
                "4. Поиск. В разделах «Настольные игры», «Напитки», «Услуги» и «Клиенты» введите " +
                "текст и нажмите «Найти» (или Enter). «Сбросить» показывает все записи.\n\n" +
                "5. Проверка данных. Обязательные поля отмечены «*». При ошибке программа " +
                "покажет, что нужно исправить.\n\n" +
                "6. Права ролей:\n" +
                "   • Администратор — полный доступ, включая пользователей.\n" +
                "   • Менеджер — игры, напитки, услуги, клиенты (все действия).\n" +
                "   • Продавец — игры, напитки, услуги только просмотр; клиентов может " +
                "просматривать и добавлять.\n\n" +
                "7. Файл → Обновить перечитывает ваши данные и роль из базы. " +
                "Кнопка «Выход» возвращает к окну авторизации.").ShowDialog();
        }
    }
}
