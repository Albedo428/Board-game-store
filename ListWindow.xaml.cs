using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace WpfApp1
{
    /// <summary>Универсальное окно-таблица (игры, напитки, услуги, клиенты, пользователи).</summary>
    public partial class ListWindow : Window
    {
        private readonly Entity _entity;

        public ListWindow(Entity entity)
        {
            InitializeComponent();
            _entity = entity;
            Title = entity.Title;
            HeadingText.Text = entity.Title.ToUpper();
            SearchPanel.Visibility = entity.Searchable ? Visibility.Visible : Visibility.Collapsed;

            SetAccess(AddButton, Session.CanAdd(entity.Table));
            SetAccess(EditButton, Session.CanEdit(entity.Table));
            SetAccess(DeleteButton, Session.CanDelete(entity.Table));

            BuildColumns();
            LoadData();
        }

        private static void SetAccess(Button button, bool allowed)
        {
            button.IsEnabled = allowed;
            if (!allowed) button.ToolTip = "Недостаточно прав для вашей роли";
        }

        private void BuildColumns()
        {
            ItemsGrid.Columns.Clear();
            ItemsGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new Binding("[id]") { Mode = BindingMode.OneWay },
                Width = 60
            });

            foreach (var f in _entity.Fields.Where(x => x.InGrid))
            {
                var binding = new Binding($"[{f.Column}]") { Mode = BindingMode.OneWay };
                var width = new DataGridLength(1, DataGridLengthUnitType.Star);

                if (f.Kind == FieldKind.Bool)
                {
                    ItemsGrid.Columns.Add(new DataGridCheckBoxColumn { Header = f.Header, Binding = binding, Width = width });
                    continue;
                }

                if (f.Kind == FieldKind.Decimal) binding.StringFormat = "N2";
                else if (f.Kind == FieldKind.DateTime) binding.StringFormat = "dd.MM.yyyy HH:mm";
                else if (f.Options != null && !f.EditableCombo) binding.Converter = new OptionConverter(f.Options);

                ItemsGrid.Columns.Add(new DataGridTextColumn { Header = f.Header, Binding = binding, Width = width, MinWidth = 90 });
            }
        }

        private void LoadData()
        {
            try
            {
                var columns = new List<string> { "id" };
                columns.AddRange(_entity.Fields.Where(f => f.InGrid).Select(f => f.Column));
                string sql = $"SELECT {string.Join(", ", columns)} FROM {_entity.Table}";
                var ps = new List<(string, object)>();

                string text = SearchBox.Text.Trim();
                if (_entity.Searchable && text.Length > 0)
                {
                    var conditions = _entity.Fields
                        .Where(f => f.InGrid && f.Editable && f.Kind != FieldKind.Bool && f.Kind != FieldKind.Password)
                        .Select(f => $"{f.Column}::text ILIKE @q")
                        .ToList();
                    conditions.Add("id::text = @id");
                    sql += " WHERE " + string.Join(" OR ", conditions);

                    ps.Add(("@q", "%" + Regex.Replace(text, @"([\\%_])", @"\$1") + "%"));
                    ps.Add(("@id", text));
                }
                sql += " ORDER BY id";

                var table = Db.Query(sql, ps.ToArray());
                ItemsGrid.ItemsSource = table.DefaultView;
                CountText.Text = $"Записей: {table.Rows.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить данные:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ----- Поиск -----
        private void Search_Click(object sender, RoutedEventArgs e) => LoadData();

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            LoadData();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) LoadData();
        }

        // ----- Добавление / редактирование / удаление -----
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            OpenEditor(null);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (!(ItemsGrid.SelectedItem is DataRowView row))
            {
                MessageBox.Show("Пожалуйста, выберите запись для редактирования.", "Редактирование",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            OpenEditor((int)row["id"]);
        }

        private void OpenEditor(int? id)
        {
            try
            {
                var window = new EditWindow(_entity, id) { Owner = this };
                if (window.ShowDialog() == true) LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии формы:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!(ItemsGrid.SelectedItem is DataRowView row))
            {
                MessageBox.Show("Пожалуйста, выберите запись для удаления.", "Удаление",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string problem = _entity.CheckDelete?.Invoke(row);
            if (problem != null)
            {
                MessageBox.Show(problem, "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string name = Convert.ToString(row[_entity.Fields[0].Column]);
            var answer = MessageBox.Show(
                $"Вы действительно хотите удалить запись «{name}»?\nЭто действие нельзя отменить.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                Db.Execute($"DELETE FROM {_entity.Table} WHERE id = @id", ("@id", (int)row["id"]));
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить запись:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e) => Close();
    }
}
