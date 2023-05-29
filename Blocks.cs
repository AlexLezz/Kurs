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
    public partial class Blocks : Form
    {

        int blocks_n;
        int mark_n;
        public DataTable blocks = new DataTable();

        public Blocks(string img_loc, int block_count, int mark_count)
        {
            InitializeComponent();
            pictureBox1.ImageLocation = img_loc;
            blocks_n = block_count;
            mark_n = mark_count;
            // !!
        }

        //Функция, обновляющая окно(создает новую таблицу значений, опустошает списки)

        private void Reboot()
        {
            blocks.Rows.Clear();        //Очищение таблицы
            blocks.Columns.Clear();

            listBox1.Items.Clear();     //Очищение списков
            listBox2.Items.Clear();
            listBox3.Items.Clear();

            for (int i = 0; i < blocks_n; i++)
            {
                listBox3.Items.Add((char)(i + 1040)); //Вывод букв кириллицы с помощью юникода
                blocks.Columns.Add();
            }
            for (int i = 1; i <= mark_n; i++)
            {
                listBox1.Items.Add(i);
            }
            for (int i = 0; i < mark_n / blocks_n; i++)
            {
                blocks.Rows.Add();
            }

            listBox3.SelectedIndex = 0;

        }

        //обновление списка марок блока
        private void ReMark2()
        {
            listBox2.Items.Clear();
            for (int i = 0; i < blocks.Rows.Count; i++)
            {
                if (blocks.Rows[i][listBox3.SelectedIndex] != DBNull.Value)
                {
                    listBox2.Items.Add(blocks.Rows[i][listBox3.SelectedIndex]);
                }
            }
        }

        private void Blocks_Load(object sender, EventArgs e)
        {
            FormBorderStyle = FormBorderStyle.FixedSingle; // Фиксированный размер
            Reboot();
        }

        //когда выбирается блок
        private void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            ReMark2();
        }

        //когда выбирается марка проверяем есть ли место для нее в блоке
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int last_ind = -1;
            for (int i = 0; i < blocks.Rows.Count; i++) // находим последний ненулевой элемент
            {
                if (blocks.Rows[i][listBox3.SelectedIndex] == DBNull.Value)
                {
                    last_ind = i;
                    break;
                }
            }
            if (last_ind != -1)               //если в блоке "есть место"
            {
                blocks.Rows[last_ind][listBox3.SelectedIndex] = listBox1.SelectedItem;

                listBox1.Items.Remove(listBox1.SelectedItem);
                ReMark2();
            }

        }


        //Кнопка "Сбросить". Вызывает функцию обновления
        private void button1_Click(object sender, EventArgs e)
        {
            Reboot();
        }

        //Кнопка "Применить". Проверяет заполнена ли таблица и передает её в основную форму
        private void button2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < blocks.Rows.Count; i++)
                for (int j = 0; j < blocks.Columns.Count; j++)
                    if (blocks.Rows[i][j] == DBNull.Value)
                    {
                        MessageBox.Show($"Пожалуйста распределите по {blocks.Rows.Count} марок на блок.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
            this.DialogResult = DialogResult.OK;    //если все марки распределены, результат ОК
            this.Close();                           //закрыть форму
        }

        public DataTable GetBTable()
        {
            //Сортировка вставками полученной таблицы
            for (int i = 0; i < blocks.Columns.Count; i++)
            {
                int k, temp;
                for (int j = 0; j <= blocks.Rows.Count - 1; j++)
                {
                    temp = Convert.ToInt32(blocks.Rows[j][i]);
                    k = j - 1;
                    while (k >= 0 && Convert.ToInt32(blocks.Rows[k][i]) > temp)
                    {
                        blocks.Rows[k + 1][i] = blocks.Rows[k][i];
                        k--;
                    }
                    blocks.Rows[k + 1][i] = temp;
                }
            }
            return blocks;
        }

        //заполняет блоки элементами
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Convert.ToInt32(listBox2.SelectedItem) != 0)
            {
                for (int i = 0; i < blocks.Rows.Count; i++) //находим в таблице ячейку, которую нужно занулить
                {
                    if (blocks.Rows[i][listBox3.SelectedIndex] == listBox2.SelectedItem)
                    {
                        blocks.Rows[i][listBox3.SelectedIndex] = DBNull.Value;
                        break;
                    }
                }
                listBox1.Items.Add(listBox2.SelectedItem);
                ReMark2();
            }

        }
    }
}
