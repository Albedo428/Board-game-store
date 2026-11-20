using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;

namespace WpfApp1
{
    /// <summary>Универсальная форма добавления и редактирования с проверкой данных.</summary>
    public partial class EditWindow : Window
    {
        // Выбор даты (календарь) + времени (список)
        private class DateTimePanel : StackPanel
        {
            public DatePicker DateBox = new DatePicker { Width = 160, Height = 28 };
            public ComboBox TimeBox = new ComboBox { Width = 90, Height = 28, Margin = new Thickness(10, 0, 0, 0) };

            public DateTimePanel()
            {
                Orientation = Orientation.Horizontal;
                Children.Add(DateBox);
                Children.Add(TimeBox);
            }
        }

        private readonly Entity _entity;
        private readonly int? _id;
        private readonly Dictionary<Field, FrameworkElement> _controls = new Dictionary<Field, FrameworkElement>();
        private DataRow _row;
        private bool _missing;

        public EditWindow(Entity entity, int? id = null)
        {
            InitializeComponent();
            _entity = entity;
            _id = id;
            Title = (id == null ? "Добавить " : "Редактировать ") + entity.ItemName;
            FormTitle.Text = Title.ToUpper();

            if (id != null)
            {
                var columns = _entity.Fields
                    .Where(f => f.Editable && f.Kind != FieldKind.Password)
                    .Select(f => f.Column);
                var table = Db.Query($"SELECT {string.Join(", ", columns)} FROM {_entity.Table} WHERE id = @id",
                    ("@id", id.Value));
                if (table.Rows.Count > 0) _row = table.Rows[0]; else _missing = true;
            }

            BuildForm();

            Loaded += (s, args) =>
            {
                if (_missing)
                {
                    MessageBox.Show("Запись не найдена (возможно, её уже удалили).", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    DialogResult = false;
                }
            };
        }

        // ================= Построение формы =================

        private void BuildForm()
        {
            int rowIndex = 0;
            foreach (var f in _entity.Fields.Where(x => x.Editable))
            {
                FormGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                bool isPasswordEdit = f.Kind == FieldKind.Password && _id != null;
                bool required = f.Required && f.Kind != FieldKind.Bool && !isPasswordEdit;
                string header = f.Header + (required ? " *" : "");
                if (isPasswordEdit) header = f.Header + " (пусто — не менять)";

                var label = new TextBlock
                {
                    Text = header,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 14)
                };
                Grid.SetRow(label, rowIndex);
                Grid.SetColumn(label, 0);
                FormGrid.Children.Add(label);

                var control = CreateControl(f);
                control.Margin = new Thickness(0, 0, 0, 14);
                control.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetRow(control, rowIndex);
                Grid.SetColumn(control, 1);
                FormGrid.Children.Add(control);

                _controls[f] = control;
                rowIndex++;
            }
        }

        private FrameworkElement CreateControl(Field f)
        {
            object current = null;
            if (_row != null && f.Kind != FieldKind.Password && !_row.IsNull(f.Column))
                current = _row[f.Column];

            switch (f.Kind)
            {
                case FieldKind.Bool:
                    return new CheckBox { IsChecked = _row == null ? true : (current is bool flag && flag) };

                case FieldKind.Password:
                    return new PasswordBox
                    {
                        Height = 28,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        MaxLength = f.MaxLength > 0 ? f.MaxLength : 0
                    };

                case FieldKind.Combo:
                    return CreateCombo(f, current);

                case FieldKind.DateTime:
                    return CreateDateTime(f, current);

                default:
                {
                    var box = new TextBox
                    {
                        Height = 28,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Text = current == null ? "" : FormatValue(f, current)
                    };
                    if (f.MaxLength > 0) box.MaxLength = f.MaxLength;
                    if (f.Kind == FieldKind.Phone) box.ToolTip = "Например: +79161234567";
                    return box;
                }
            }
        }

        private static string FormatValue(Field f, object value)
        {
            if (f.Kind == FieldKind.Decimal)
                return Convert.ToDecimal(value).ToString("0.00", CultureInfo.CurrentCulture);
            return Convert.ToString(value, CultureInfo.CurrentCulture);
        }

        private FrameworkElement CreateCombo(Field f, object current)
        {
            var combo = new ComboBox { Height = 28, VerticalContentAlignment = VerticalAlignment.Center };

            if (f.EditableCombo)
            {
                // Выбор из списка или ввод своего значения
                combo.IsEditable = true;
                combo.ItemsSource = GetOptionStrings(f, current);
                combo.Text = current as string ?? "";
            }
            else if (f.Options != null)
            {
                // Значение в БД и русский текст отличаются (роли)
                combo.ItemsSource = f.Options;
                combo.DisplayMemberPath = "Value";
                combo.SelectedValuePath = "Key";
                if (current != null) combo.SelectedValue = current;
                else if (_id == null && f.DefaultValue != null) combo.SelectedValue = f.DefaultValue;
            }
            else
            {
                // Список из базы (например, услуги)
                var items = GetOptionStrings(f, current);
                combo.ItemsSource = items;
                if (current is string text) combo.SelectedItem = text;
            }
            return combo;
        }

        private List<string> GetOptionStrings(Field f, object current)
        {
            var list = new List<string>();
            if (f.Options != null) list.AddRange(f.Options.Select(o => o.Key));
            if (f.OptionsSql != null)
                list.AddRange(Db.Query(f.OptionsSql).Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0])));
            if (f.MergeDistinct)
                list.AddRange(Db.Query($"SELECT DISTINCT {f.Column} FROM {_entity.Table} WHERE {f.Column} IS NOT NULL ORDER BY {f.Column}")
                    .Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0])));
            if (current is string s) list.Add(s);
            return list.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        }

        private FrameworkElement CreateDateTime(Field f, object current)
        {
            var panel = new DateTimePanel();

            var times = new List<string>();
            for (int h = 9; h <= 23; h++)
            {
                times.Add(h.ToString("00") + ":00");
                times.Add(h.ToString("00") + ":30");
            }

            DateTime? value = current is DateTime d ? d : (DateTime?)null;
            string selected = "18:00";
            if (value != null)
            {
                selected = value.Value.ToString("HH:mm", CultureInfo.InvariantCulture);
                if (!times.Contains(selected)) { times.Add(selected); times.Sort(); }
                panel.DateBox.SelectedDate = value.Value.Date;
            }
            else if (f.FutureOnly)
            {
                panel.DateBox.DisplayDateStart = DateTime.Today;   // в календаре нельзя выбрать прошедшие дни
            }

            panel.TimeBox.ItemsSource = times;
            panel.TimeBox.SelectedItem = selected;
            return panel;
        }

        // ================= Проверка данных =================

        private static string ReadComboText(Field f, ComboBox combo)
        {
            if (combo.IsEditable) return combo.Text;
            if (f.Options != null) return combo.SelectedValue as string;
            return combo.SelectedItem as string;
        }

        /// <summary>Читает и проверяет одно поле. Возвращает текст ошибки или null.</summary>
        private string ReadField(Field f, FrameworkElement control, out object value, out bool skip)
        {
            value = null;
            skip = false;
            string name = $"«{f.Header}»";

            switch (f.Kind)
            {
                case FieldKind.Bool:
                    value = ((CheckBox)control).IsChecked == true;
                    return null;

                case FieldKind.DateTime:
                    return ReadDateTime(f, (DateTimePanel)control, out value);

                case FieldKind.Password:
                {
                    string password = ((PasswordBox)control).Password;
                    if (password.Length == 0)
                    {
                        if (_id != null) { skip = true; return null; }   // при редактировании пустой пароль = не менять
                        return $"Поле {name} обязательно для заполнения.";
                    }
                    if (f.MinLength > 0 && password.Length < f.MinLength)
                        return $"Поле {name}: минимум {f.MinLength} символов.";
                    if (password.Contains(' '))
                        return $"Поле {name}: пароль не должен содержать пробелов.";
                    value = password;
                    return null;
                }
            }

            string text = f.Kind == FieldKind.Combo
                ? ReadComboText(f, (ComboBox)control)
                : ((TextBox)control).Text;
            text = (text ?? "").Trim();

            if (text.Length == 0)
            {
                if (f.Required) return $"Поле {name} обязательно для заполнения.";
                return null;   // необязательное поле остаётся пустым
            }
            if (f.MinLength > 0 && text.Length < f.MinLength)
                return $"Поле {name}: минимум {f.MinLength} симв.";
            if (f.MaxLength > 0 && text.Length > f.MaxLength)
                return $"Поле {name}: максимум {f.MaxLength} симв.";
            if (f.Pattern != null && !Regex.IsMatch(text, f.Pattern))
                return $"Поле {name}: {f.PatternMessage ?? "недопустимые символы"}.";

            switch (f.Kind)
            {
                case FieldKind.Phone:
                {
                    string digits = Regex.Replace(text, @"[\s\-\(\)]", "");
                    if (!Regex.IsMatch(digits, @"^\+?[0-9]{10,15}$"))
                        return $"Поле {name}: введите номер в формате +79161234567 (10–15 цифр).";
                    value = digits;
                    return null;
                }

                case FieldKind.Integer:
                {
                    if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                        return $"Поле {name}: введите целое число.";
                    if (intValue < f.Min || intValue > f.Max)
                        return $"Поле {name}: допустимо от {f.Min:0} до {f.Max:0}.";
                    value = intValue;
                    return null;
                }

                case FieldKind.Decimal:
                {
                    string normalized = text.Replace(" ", "").Replace(',', '.');
                    if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal decValue))
                        return $"Поле {name}: введите число (например, 1500 или 1500,50).";
                    if (decimal.Round(decValue, 2) != decValue)
                        return $"Поле {name}: не более двух знаков после запятой.";
                    if (decValue < f.Min || decValue > f.Max)
                        return $"Поле {name}: допустимо от {f.Min:0.##} до {f.Max:0.##}.";
                    value = decValue;
                    return null;
                }

                default:
                    value = text;
                    return null;
            }
        }

        private string ReadDateTime(Field f, DateTimePanel panel, out object value)
        {
            value = null;
            string name = $"«{f.Header}»";

            if (panel.DateBox.SelectedDate == null)
                return f.Required ? $"Поле {name}: выберите дату в календаре." : null;
            if (panel.TimeBox.SelectedItem == null)
                return $"Поле {name}: выберите время.";

            var time = TimeSpan.Parse((string)panel.TimeBox.SelectedItem, CultureInfo.InvariantCulture);
            var result = DateTime.SpecifyKind(panel.DateBox.SelectedDate.Value.Date + time, DateTimeKind.Unspecified);

            if (f.FutureOnly)
            {
                bool changed = _row == null || _row.IsNull(f.Column) || (DateTime)_row[f.Column] != result;
                if (changed && result < DateTime.Now)
                    return $"Поле {name}: дата и время не могут быть в прошлом.";
                if (changed && result > DateTime.Now.AddYears(2))
                    return $"Поле {name}: дата слишком далеко в будущем (не более 2 лет).";
            }

            value = result;
            return null;
        }

        private void SetMark(FrameworkElement control, bool on)
        {
            Control target = control as Control;
            if (control is DateTimePanel panel) target = panel.DateBox;
            if (target == null) return;

            if (on) target.BorderBrush = Brushes.Red;
            else target.ClearValue(Control.BorderBrushProperty);
        }

        // ================= Сохранение =================

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var errors = new List<string>();
            var values = new Dictionary<string, object>();
            FrameworkElement firstBad = null;

            foreach (var f in _entity.Fields.Where(x => x.Editable))
            {
                var control = _controls[f];
                SetMark(control, false);

                string error = ReadField(f, control, out object value, out bool skip);
                if (error != null)
                {
                    errors.Add(error);
                    SetMark(control, true);
                    if (firstBad == null) firstBad = control;
                    continue;
                }
                if (!skip) values[f.Column] = value;
            }

            if (errors.Count == 0 && _entity.Check != null)
            {
                string message = _entity.Check(values, _id);
                if (message != null) errors.Add(message);
            }

            if (errors.Count > 0)
            {
                MessageBox.Show("Исправьте ошибки:\n\n• " + string.Join("\n• ", errors),
                    "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                firstBad?.Focus();
                return;
            }

            try
            {
                var ps = new List<(string, object)>();
                int i = 0;

                if (_id == null)
                {
                    // id не указываем: его выдаёт триггер БД (наименьший свободный номер)
                    var columns = new List<string>();
                    var placeholders = new List<string>();
                    foreach (var kv in values)
                    {
                        var f = _entity.Fields.First(x => x.Column == kv.Key);
                        string p = "@p" + i++;
                        columns.Add(kv.Key);
                        placeholders.Add(f.Kind == FieldKind.Password ? $"crypt({p}, gen_salt('bf'))" : p);
                        ps.Add((p, kv.Value));
                    }
                    if (_entity.SetCreatedBy)
                    {
                        columns.Add("created_by");
                        placeholders.Add("@cb");
                        ps.Add(("@cb", Session.UserId));
                    }
                    Db.Execute($"INSERT INTO {_entity.Table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", placeholders)})",
                        ps.ToArray());
                }
                else
                {
                    var sets = new List<string>();
                    foreach (var kv in values)
                    {
                        var f = _entity.Fields.First(x => x.Column == kv.Key);
                        string p = "@p" + i++;
                        sets.Add($"{kv.Key} = " + (f.Kind == FieldKind.Password ? $"crypt({p}, gen_salt('bf'))" : p));
                        ps.Add((p, kv.Value));
                    }
                    ps.Add(("@id", _id.Value));
                    Db.Execute($"UPDATE {_entity.Table} SET {string.Join(", ", sets)} WHERE id = @id", ps.ToArray());
                }

                DialogResult = true;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                MessageBox.Show("Запись с такими данными уже существует (например, такой логин уже занят).",
                    "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить данные:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
