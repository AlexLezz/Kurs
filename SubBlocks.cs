using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kurs1
{
    public partial class SubBlocks : Form
    {
        int subBlocks_n;
        int mark_n;
        string block_name;
        public DataTable subBlocks = new DataTable();
        private DataTable prev;
        double E;              //отклонение E для выделения ячеек
        DataColumn block_marks;

        public SubBlocks(string img_loc, int SubBlock_count, int mark_count, string name, DataTable Prev, double E, DataColumn block_marks)
        {
            InitializeComponent();
            pictureBox1.ImageLocation = img_loc;
            subBlocks_n = SubBlock_count;
            mark_n = mark_count;
            block_name = name;
            this.prev = Prev;
            this.E = E;
            this.block_marks = block_marks;
            pictureBox1.Image = Image.FromFile(pictureBox1.ImageLocation);

        }

        //Функция, обновляющая окно(создает новую таблицу значений, опустошает списки)
        private void Reboot()
        {
            subBlocks.Rows.Clear();        //Очищение таблицы
            subBlocks.Columns.Clear();

            listBox1.Items.Clear();        //Очищение списков
            listBox2.Items.Clear();
            listBox3.Items.Clear();

            for (int i = 1; i <= subBlocks_n; i++)
            {
                listBox3.Items.Add(block_name + i);  //Вывод названий подблоков
                subBlocks.Columns.Add();             //Создание столбцов в таблице распределения по подблокам (= количеству подблоков)
            }
            for (int i = 0; i < mark_n; i++)
            {
                listBox1.Items.Add(block_marks.Table.Rows[i][block_marks.Ordinal]);
            }
            for (int i = 0; i < mark_n / subBlocks_n; i++)
            {
                subBlocks.Rows.Add();
            }

            listBox3.SelectedIndex = 0;

        }


        //При загрузке вызывается функция обновления
        private void SubBlocks_Load(object sender, EventArgs e)
        {
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Reboot();
            //Вывод таблицы превышений
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            for (int col = 0; col < prev.Columns.Count; col++)
            {
                string ColName = prev.Columns[col].ColumnName;
                dataGridView1.Columns.Add(ColName, ColName);
                dataGridView1.Columns[col].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }


            //Заполнение строк 
            for (int row = 0; row < prev.Rows.Count; row++)
            {
                dataGridView1.Rows.Add(prev.Rows[row].ItemArray);
            }

            //Цикл проходит по всем ячейкам таблицы, проверяет значения и заполняет нужным цветом
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
                for (int j = 1; j < dataGridView1.Columns.Count; j++)
                    if (Convert.ToDouble(dataGridView1.Rows[i].Cells[j].Value) > E)
                        dataGridView1.Rows[i].Cells[j].Style.BackColor = Color.Red;
                    else dataGridView1.Rows[i].Cells[j].Style.BackColor = Color.Green;

            //Отключение возможности сортировки столбцов
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            dataGridView1.AutoResizeColumns();
            dataGridView1.AutoResizeRows();



        }

        //когда выбирается подблок
        private void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            ReMark2();
        }

        //обновление списка марок подблока
        private void ReMark2()
        {
            listBox2.Items.Clear();
            for (int i = 0; i < subBlocks.Rows.Count; i++)
            {
                if (subBlocks.Rows[i][listBox3.SelectedIndex] != DBNull.Value)
                {
                    listBox2.Items.Add(subBlocks.Rows[i][listBox3.SelectedIndex]);
                }
            }

        }

        //когда выбирается марка
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int last_ind = -1;
            for (int i = 0; i < subBlocks.Rows.Count; i++)    //находим последний ненулевой элемент
            {
                if (subBlocks.Rows[i][listBox3.SelectedIndex] == DBNull.Value)
                {
                    last_ind = i;
                    break;
                }
            }
            if (last_ind != -1)                               //если в блоке "есть место"
            {
                subBlocks.Rows[last_ind][listBox3.SelectedIndex] = listBox1.SelectedItem;

                listBox1.Items.Remove(listBox1.SelectedItem);
                ReMark2();
            }
        }

        //Кнопка "Сбросить". Вызывает функцию обновления
        private void button1_Click(object sender, EventArgs e)
        {
            Reboot();
        }

        //Кнопка "Применить". Проверяет заполнена ли таблица и закрывает форму
        private void button2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < subBlocks.Rows.Count; i++)
                for (int j = 0; j < subBlocks.Columns.Count; j++)
                    if (subBlocks.Rows[i][j] == DBNull.Value)
                    {
                        MessageBox.Show($"Пожалуйста распределите по {subBlocks.Rows.Count} марок на подблок.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

            this.DialogResult = DialogResult.OK;    //если все марки распределены, результат ОК
            this.Close();
        }

        public DataTable GetBTable()
        {
            //Сортировка вставками полученной таблицы
            for (int i = 0; i < subBlocks.Columns.Count; i++)
            {
                int k, temp;
                for (int j = 0; j <= subBlocks.Rows.Count - 1; j++)
                {
                    temp = Convert.ToInt32(subBlocks.Rows[j][i]);
                    k = j - 1;
                    while (k >= 0 && Convert.ToInt32(subBlocks.Rows[k][i]) > temp)
                    {
                        subBlocks.Rows[k + 1][i] = subBlocks.Rows[k][i];
                        k--;
                    }
                    subBlocks.Rows[k + 1][i] = temp;
                }
            }
            return subBlocks;
        }

        //возвращение марки
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Convert.ToInt32(listBox2.SelectedItem) != 0)
            {
                for (int i = 0; i < subBlocks.Rows.Count; i++) //находим в таблице ячейку, которую нужно занулить
                {
                    if (subBlocks.Rows[i][listBox3.SelectedIndex] == listBox2.SelectedItem)
                    {
                        subBlocks.Rows[i][listBox3.SelectedIndex] = DBNull.Value;
                        break;
                    }
                }
                listBox1.Items.Add(listBox2.SelectedItem);
                ReMark2();
            }

        }
    }
}
