using System;
using System.Data;
using Npgsql;

namespace WpfApp1
{
    /// <summary>Работа с базой данных PostgreSQL.</summary>
    public static class Db
    {
        // База «Boardgame shop» (сервер GidroDed в pgAdmin).
        // !!! Впишите свой пароль (и пользователя, если он не postgres) !!!
        // Если сервер на другом компьютере, замените localhost на его адрес.
        public const string ConnectionString =
            "Host=localhost;Port=5432;Database=Boardgame shop;Username=postgres;Password=GeoDed";

        private static NpgsqlConnection Open()
        {
            var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        private static void AddParams(NpgsqlCommand cmd, (string Name, object Value)[] ps)
        {
            foreach (var p in ps)
                cmd.Parameters.AddWithValue(p.Name, p.Value ?? DBNull.Value);
        }

        public static DataTable Query(string sql, params (string Name, object Value)[] ps)
        {
            using var conn = Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            AddParams(cmd, ps);
            using var reader = cmd.ExecuteReader();
            var table = new DataTable();
            table.Load(reader);
            return table;
        }

        public static int Execute(string sql, params (string Name, object Value)[] ps)
        {
            using var conn = Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            AddParams(cmd, ps);
            return cmd.ExecuteNonQuery();
        }
    }
}
