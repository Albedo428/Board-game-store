using System.Collections.Generic;

namespace WpfApp1
{
    /// <summary>Текущий пользователь и права его роли.</summary>
    public static class Session
    {
        public static int UserId { get; set; }
        public static string Username { get; set; }
        public static string FullName { get; set; }
        public static string Role { get; set; }   // admin / manager / seller

        public static readonly List<KeyValuePair<string, string>> RoleOptions = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("admin", "Администратор"),
            new KeyValuePair<string, string>("manager", "Менеджер"),
            new KeyValuePair<string, string>("seller", "Продавец"),
        };

        public static string RoleRu
        {
            get
            {
                foreach (var o in RoleOptions)
                    if (o.Key == Role) return o.Value;
                return Role;
            }
        }

        public static void Clear()
        {
            UserId = 0; Username = null; FullName = null; Role = null;
        }

        // ===== Права ролей (при необходимости меняйте здесь) =====
        // admin   - всё, включая раздел «Пользователи»
        // manager - товары, услуги, клиенты (всё), без раздела «Пользователи»
        // seller  - товары и услуги только просмотр; клиенты: просмотр и добавление

        public static bool CanOpen(string table)
        {
            if (table == "users") return Role == "admin";
            return Role == "admin" || Role == "manager" || Role == "seller";
        }

        public static bool CanAdd(string table)
        {
            switch (Role)
            {
                case "admin": return true;
                case "manager": return table != "users";
                case "seller": return table == "customers";
                default: return false;
            }
        }

        public static bool CanEdit(string table)
        {
            switch (Role)
            {
                case "admin": return true;
                case "manager": return table != "users";
                default: return false;
            }
        }

        public static bool CanDelete(string table) => CanEdit(table);
    }
}
