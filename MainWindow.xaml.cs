using System;
using System.Windows;

namespace WpfApp1
{
    /// <summary>Окно авторизации.</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ApplyLogo();
            LoginBox.Focus();
        }

        private void ApplyLogo()
        {
            var logo = Branding.Logo;
            if (logo == null) { LogoImage.Visibility = Visibility.Collapsed; return; }
            LogoImage.Source = Branding.LogoLight ?? logo;
            Icon = logo;   // иконка окна
        }

        private string CurrentPassword =>
            ShowPasswordButton.IsChecked == true ? PasswordText.Text : PasswordInput.Password;

        private void ShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            PasswordText.Text = PasswordInput.Password;
            PasswordInput.Visibility = Visibility.Collapsed;
            PasswordText.Visibility = Visibility.Visible;
            ShowPasswordButton.Content = "Скрыть";
            PasswordText.Focus();
            PasswordText.CaretIndex = PasswordText.Text.Length;
        }

        private void ShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordInput.Password = PasswordText.Text;
            PasswordText.Visibility = Visibility.Collapsed;
            PasswordInput.Visibility = Visibility.Visible;
            ShowPasswordButton.Content = "Показать";
            PasswordInput.Focus();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = CurrentPassword;

            if (login.Length == 0)
            {
                MessageBox.Show("Введите логин.", "Авторизация", MessageBoxButton.OK, MessageBoxImage.Warning);
                LoginBox.Focus();
                return;
            }
            if (password.Length == 0)
            {
                MessageBox.Show("Введите пароль.", "Авторизация", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var table = Db.Query(
                    "SELECT id, username, first_name, last_name, role FROM users " +
                    "WHERE username = @u AND is_active AND password_hash = crypt(@p, password_hash)",
                    ("@u", login), ("@p", password));

                if (table.Rows.Count == 0)
                {
                    MessageBox.Show("Неверный логин или пароль, либо учётная запись отключена.",
                        "Авторизация", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var row = table.Rows[0];
                Session.UserId = (int)row["id"];
                Session.Username = (string)row["username"];
                Session.FullName = row["first_name"] + " " + row["last_name"];
                Session.Role = (string)row["role"];   // роль сохраняется на всё время работы

                Db.Execute("UPDATE users SET last_login = now() WHERE id = @id", ("@id", Session.UserId));

                new MenuWindow().Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось подключиться к базе данных:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
