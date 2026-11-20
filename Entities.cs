using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    /// <summary>Описание всех таблиц: поля, выпадающие списки, правила проверки.</summary>
    public static class Entities
    {
        private const string NamePattern = @"^[A-Za-zА-Яа-яЁё\-']+$";
        private const string FullNamePattern = @"^[A-Za-zА-Яа-яЁё\-\.']+(\s+[A-Za-zА-Яа-яЁё\-\.']+)+$";

        private static List<KeyValuePair<string, string>> Opts(params string[] items)
            => items.Select(i => new KeyValuePair<string, string>(i, i)).ToList();

        // ---------- Настольные игры ----------
        public static readonly Entity BoardGames = new Entity
        {
            Table = "board_games",
            Title = "Настольные игры",
            ItemName = "настольную игру",
            Searchable = true,
            SetCreatedBy = true,
            Fields = new List<Field>
            {
                new Field { Column = "name", Header = "Название", MinLength = 2, MaxLength = 200 },
                new Field { Column = "genre", Header = "Жанр", Kind = FieldKind.Combo, EditableCombo = true, MergeDistinct = true, MaxLength = 100,
                    Options = Opts("Стратегия", "Карточная", "Ассоциации", "Вечериночная", "Семейная", "Экономическая",
                                   "Кооперативная", "Детективная", "Приключенческая", "Логическая", "Детская") },
                new Field { Column = "price", Header = "Цена, ₽", Kind = FieldKind.Decimal, Min = 0.01m, Max = 1000000m },
                new Field { Column = "manufacturer", Header = "Производитель", Kind = FieldKind.Combo, EditableCombo = true, MergeDistinct = true, MaxLength = 100,
                    Options = Opts("Hobby World", "Мосигра", "Звезда", "Crowd Games", "Cosmodrome Games", "Asmodee", "Hasbro", "Mattel", "Tactic", "Правильные игры") },
                new Field { Column = "stock_quantity", Header = "Остаток, шт.", Kind = FieldKind.Integer, Min = 0, Max = 100000 },
                new Field { Column = "is_available", Header = "В продаже", Kind = FieldKind.Bool },
                new Field { Column = "created_at", Header = "Добавлено", Kind = FieldKind.DateTime, Editable = false },
            },
            Check = (v, id) =>
            {
                if ((bool)v["is_available"] && (int)v["stock_quantity"] == 0)
                    return "Нельзя отметить игру «В продаже», если остаток равен 0.";
                return null;
            }
        };

        // ---------- Напитки ----------
        public static readonly Entity Beverages = new Entity
        {
            Table = "beverages",
            Title = "Напитки",
            ItemName = "напиток",
            Searchable = true,
            SetCreatedBy = true,
            Fields = new List<Field>
            {
                new Field { Column = "name", Header = "Название", MinLength = 2, MaxLength = 150 },
                new Field { Column = "price", Header = "Цена, ₽", Kind = FieldKind.Decimal, Min = 0.01m, Max = 100000m },
                new Field { Column = "is_available", Header = "В наличии", Kind = FieldKind.Bool },
                new Field { Column = "created_at", Header = "Добавлено", Kind = FieldKind.DateTime, Editable = false },
            }
        };

        // ---------- Услуги ----------
        public static readonly Entity Services = new Entity
        {
            Table = "services",
            Title = "Услуги",
            ItemName = "услугу",
            Searchable = true,
            SetCreatedBy = true,
            Fields = new List<Field>
            {
                new Field { Column = "name", Header = "Название", MinLength = 2, MaxLength = 150 },
                new Field { Column = "price", Header = "Цена, ₽", Kind = FieldKind.Decimal, Min = 0.01m, Max = 1000000m },
                new Field { Column = "is_active", Header = "Действует", Kind = FieldKind.Bool },
                new Field { Column = "created_at", Header = "Добавлено", Kind = FieldKind.DateTime, Editable = false },
            }
        };

        // ---------- Клиенты ----------
        public static readonly Entity Customers = new Entity
        {
            Table = "customers",
            Title = "Клиенты",
            ItemName = "клиента",
            Searchable = true,
            SetCreatedBy = true,
            Fields = new List<Field>
            {
                new Field { Column = "full_name", Header = "ФИО", MinLength = 5, MaxLength = 200,
                    Pattern = FullNamePattern, PatternMessage = "введите фамилию и имя (только буквы, пробелы, дефис)" },
                new Field { Column = "phone", Header = "Телефон", Kind = FieldKind.Phone, MaxLength = 20 },
                new Field { Column = "booking_time", Header = "Дата и время брони", Kind = FieldKind.DateTime, FutureOnly = true },
                new Field { Column = "interested_service", Header = "Услуга", Kind = FieldKind.Combo,
                    OptionsSql = "SELECT name FROM services WHERE is_active ORDER BY name", MaxLength = 200 },
                new Field { Column = "created_at", Header = "Добавлен", Kind = FieldKind.DateTime, Editable = false },
            }
        };

        // ---------- Пользователи ----------
        public static readonly Entity Users = new Entity
        {
            Table = "users",
            Title = "Пользователи",
            ItemName = "пользователя",
            Searchable = false,
            SetCreatedBy = false,
            Fields = new List<Field>
            {
                new Field { Column = "username", Header = "Логин", MinLength = 3, MaxLength = 100,
                    Pattern = @"^[A-Za-z0-9_.]+$", PatternMessage = "только латинские буквы, цифры, «_» и «.»" },
                new Field { Column = "password_hash", Header = "Пароль", Kind = FieldKind.Password, InGrid = false, MinLength = 6, MaxLength = 72 },
                new Field { Column = "first_name", Header = "Имя", MinLength = 2, MaxLength = 100,
                    Pattern = NamePattern, PatternMessage = "только буквы и дефис" },
                new Field { Column = "last_name", Header = "Фамилия", MinLength = 2, MaxLength = 100,
                    Pattern = NamePattern, PatternMessage = "только буквы и дефис" },
                new Field { Column = "position", Header = "Должность", Kind = FieldKind.Combo, EditableCombo = true, Required = false, MergeDistinct = true, MaxLength = 100,
                    Options = Opts("Директор", "Управляющий", "Продавец-консультант", "Кассир", "Администратор зала") },
                new Field { Column = "role", Header = "Роль", Kind = FieldKind.Combo, Options = Session.RoleOptions, DefaultValue = "seller" },
                new Field { Column = "is_active", Header = "Активен", Kind = FieldKind.Bool },
                new Field { Column = "last_login", Header = "Последний вход", Kind = FieldKind.DateTime, Editable = false },
                new Field { Column = "created_at", Header = "Создан", Kind = FieldKind.DateTime, Editable = false },
            },
            Check = (v, id) =>
            {
                // Защита от потери доступа: нельзя понизить или отключить самого себя
                if (id == Session.UserId)
                {
                    if ((string)v["role"] != "admin")
                        return "Нельзя изменить роль собственной учётной записи.";
                    if (!(bool)v["is_active"])
                        return "Нельзя отключить собственную учётную запись.";
                }
                return null;
            },
            CheckDelete = row =>
            {
                if ((int)row["id"] == Session.UserId)
                    return "Нельзя удалить собственную учётную запись.";
                return null;
            }
        };
    }
}
