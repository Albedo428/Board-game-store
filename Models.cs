using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1
{
    public enum FieldKind { Text, Integer, Decimal, Bool, Combo, DateTime, Phone, Password }

    /// <summary>Описание одного поля таблицы (для таблицы-списка, формы и валидации).</summary>
    public class Field
    {
        public string Column { get; set; }
        public string Header { get; set; }
        public FieldKind Kind { get; set; } = FieldKind.Text;
        public bool Required { get; set; } = true;
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public string Pattern { get; set; }          // регулярное выражение
        public string PatternMessage { get; set; }
        public bool InGrid { get; set; } = true;     // показывать в таблице
        public bool Editable { get; set; } = true;   // показывать в форме
        public bool EditableCombo { get; set; }      // в списке можно вписать своё значение
        public bool MergeDistinct { get; set; }      // добавить в список значения, уже имеющиеся в таблице
        public bool FutureOnly { get; set; }         // дата не может быть в прошлом
        public string OptionsSql { get; set; }       // SQL для списка (один столбец)
        public string DefaultValue { get; set; }
        public List<KeyValuePair<string, string>> Options { get; set; } // значение БД -> текст
    }

    /// <summary>Описание таблицы БД.</summary>
    public class Entity
    {
        public string Table { get; set; }
        public string Title { get; set; }       // заголовок окна списка
        public string ItemName { get; set; }    // «настольную игру» (для «Добавить ...»)
        public bool Searchable { get; set; }
        public bool SetCreatedBy { get; set; }
        public List<Field> Fields { get; set; }

        // Дополнительные проверки перед сохранением: возвращает текст ошибки или null
        public Func<Dictionary<string, object>, int?, string> Check { get; set; }
        // Проверка перед удалением
        public Func<DataRowView, string> CheckDelete { get; set; }
    }

    /// <summary>Показывает в таблице русский текст вместо значения из БД (например, admin -> Администратор).</summary>
    public class OptionConverter : IValueConverter
    {
        private readonly Dictionary<string, string> _map = new Dictionary<string, string>();

        public OptionConverter(List<KeyValuePair<string, string>> options)
        {
            foreach (var o in options) _map[o.Key] = o.Value;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && _map.TryGetValue(s, out var text)) return text;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
