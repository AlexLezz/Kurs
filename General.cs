using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;
using System.Collections.Generic;
using System.Linq;

namespace Kurs1
{
    public partial class General : Form
    {
        private SQLiteConnection SQLConn;
        private DataTable Data;
        private DataTable blocks;
        private DataTable sub_blocks;
        private DataTable Prev; // таблица превышений

        private double E; //погрешность
        private double A; // графики

        //создаем объекты Chart, которые будут хранить все серии графиков
        private Chart chartPage2 = new Chart();
        private Chart chartPage3 = new Chart();
        private Chart chartPage4 = new Chart();
        private Chart chartPage5 = new Chart();

        public General()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Не растягивать
            FormBorderStyle = FormBorderStyle.FixedSingle;

            //отключаем чтобы избежать ошибки
            groupBox1.Enabled = false;
            button2.Enabled = false;
            button10.Enabled = false;
            button11.Enabled = false;
            comboBox1.Enabled = false;
            button7.Enabled = false;
            label6.Text = "";
            // Открывается в середине экрана
            StartPosition = FormStartPosition.CenterScreen;

            // Отключаем пустые вкладки
            tabControl1.TabPages.Remove(tabPage2);
            tabControl1.TabPages.Remove(tabPage3);
            tabControl1.TabPages.Remove(tabPage4);
            tabControl1.TabPages.Remove(tabPage5);
            SQLConn = new SQLiteConnection(); // подключение к Бд
            Data = new DataTable(); // подключение к таблице
            toolStripStatusLabel1.Text = "Файл не выбран";
            toolStripStatusLabel2.Text = "Погрешность не введена";
            toolStripStatusLabel3.Text = "Коэффициент не введен";
        }

        //функция запрашивает путь к файлу БД и осуществляет подключение
        private bool OpenBD()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog(); // открытие окна выбора
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); //Задает начальную папку, отображенную диалоговым окном на рабочем столе
            openFileDialog.Filter = "Базы данных (*.sqlite)|*.sqlite|Все файлы (*.*)|*.*"; //возможные форматы файлов
            if (openFileDialog.ShowDialog(this) == DialogResult.OK) // Если задаёт правильный файл, то..
            {
                SQLConn = new SQLiteConnection("Data Source=" + openFileDialog.FileName + ";Version=3;"); //создаем файл sqlite
                SQLConn.Open();

                toolStripStatusLabel1.Text = openFileDialog.FileName; // Демонстрация пути к файлу БД

                // вытаскиваем картинку, A, E из БД

                SQLiteCommand imgSQL = SQLConn.CreateCommand();
                imgSQL.CommandText = "SELECT Img FROM Config";
                SQLiteCommand aSQL = SQLConn.CreateCommand();
                aSQL.CommandText = "SELECT aVal FROM Config";
                SQLiteCommand eSQL = SQLConn.CreateCommand();
                eSQL.CommandText = "SELECT eVal FROM Config";

                string aValue = aSQL.ExecuteScalar()?.ToString();
                string eValue = eSQL.ExecuteScalar()?.ToString();

                A = Convert.ToDouble( aValue );
                E = Convert.ToDouble( eValue );

                textBox1.Text = aValue;
                textBox2.Text = eValue;
                toolStripStatusLabel2.Text = "Точность E = " + E;
                toolStripStatusLabel3.Text = "Коэффициент эксп. сглаживания А = " + A;


                // Если нашёл картинку
                SQLiteDataReader reader = imgSQL.ExecuteReader();
                if (reader.Read())
                {
                    byte[] imageBytes = (byte[])reader["Img"];
                    // Теперь есть массив байтов с изображением.
                    // Преобразуем его в объект Image и установите его как изображение для pictureBox1.
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        Image image = Image.FromStream(ms);
                        pictureBox1.Image = image;
                    }
                }
                reader.Close();



                return true;
            }
            else return false;
        }

        //функция заполнения comboBox1 названиями таблиц в БД
        private void ComboBox1()
        {
            string SQLQuery = "SELECT name FROM sqlite_master WHERE type='table' AND name <> 'Config' ORDER BY name;"; //запоминает имена таблиц, кроме Config
            SQLiteCommand command = new SQLiteCommand(SQLQuery, SQLConn); //Для выполнения запросов к базе данных SQLite
            SQLiteDataReader reader = command.ExecuteReader(); // Чтение
            comboBox1.Items.Clear(); // Очищаем от старых данных, если он существуют
            while (reader.Read())
            {
                comboBox1.Items.Add(reader[0].ToString()); // Заполняем новыми таблицами
            }
        }


        private void button10_Click(object sender, EventArgs e)
        {
            int rowIndex = dataGridView1.CurrentCell.RowIndex;
            // Проверяем, что есть выделенная строка
            if (rowIndex > 0)
            {
                // Получаем индекс выделенной строки

                // Удаляем строку из dataGridView1
                dataGridView1.Rows.RemoveAt(rowIndex);
                string deleteQuery = $"DELETE FROM {comboBox1.SelectedItem.ToString()} WHERE ROWID = {rowIndex + 1}";
                SQLiteCommand command = new SQLiteCommand(deleteQuery, SQLConn);
                command.ExecuteNonQuery();

                Data.Rows.RemoveAt(rowIndex); //удаляем строку в таблице
                OutPutTable(Data, dataGridView1); //выводим в таблицу с удаленной строкой
                Decommposition(dataGridView2, chartPage2, Data, checkedListBox1, checkedListBox5, checkedListBox8); //Проводим 1-й уровень декомпозиции при добавлении новой строки
                //Marks(); //обновляем график марок

                if (comboBox2.Items.Count != 0)    //если блоки определены на 2 уровне
                {
                    comboBox2.SelectedIndex = 1;   //обновление таблиц блоков. Блоков всегда будет минимум два
                    comboBox2.SelectedIndex = 0;
                }

                if (comboBox4.Items.Count != 0)    //если блоки определены на 3 уровне
                {
                    comboBox4.SelectedIndex = 1;   //обновление таблиц блоков. Блоков всегда будет минимум два
                    comboBox4.SelectedIndex = 0;
                }

                Stabilnost_inf(); //выводим данные о стабильности объекта

            }
            else
            {
                MessageBox.Show("Не выбрана строка для удаления!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //Функция заполняющая таблицу данными из БД
        private void TableBD()
        {
            Data.Rows.Clear(); //Очищаем старые строки
            Data.Columns.Clear(); // Очищаем старые столбцы
            SQLiteDataAdapter adapter = new SQLiteDataAdapter("SELECT * FROM [" + comboBox1.SelectedItem + "] order by 1", SQLConn); //Заполнение данными
            adapter.Fill(Data);
        }

        //функция для отображения таблицы данных в компоненте dataGridView 
        private void OutPutTable(DataTable DTable, DataGridView Table)
        {
            Table.Columns.Clear();
            Table.Rows.Clear();

            //создание столбцов и задание размера
            for (int rows = 0; rows < DTable.Columns.Count; rows++)
            {
                string ColName = DTable.Columns[rows].ColumnName;
                Table.Columns.Add(ColName, ColName);
                Table.Columns[rows].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
            //Заполнение строк в dataGridView1
            for (int GridRows = 0; GridRows < DTable.Rows.Count; GridRows++)
            {
                Table.Rows.Add(DTable.Rows[GridRows].ItemArray);
            }

            //Отключение возможности сортировки столбцов
            foreach (DataGridViewColumn column in Table.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            Table.AutoResizeColumns(); //Корректирует ширину всех столбцов по содержимому их ячеек.
            Table.AutoResizeRows(); //Корректирует высоту всех столбцов по содержимому их ячеек.
        }

        // кнопка для загрузки бд
        private void button1_Click(object sender, EventArgs e)
        {
            // Если база данных успешно подключена, заполняются comboBox1 названиями таблиц базы данных
            if (OpenBD() == true)
            {
                ComboBox1();
                comboBox1.SelectedIndex = 0;      //Выставляем первый из возможных индексов.
                TableBD();                     //Заполняем таблицу данными из таблицы базы данных
                OutPutTable(Data, dataGridView1);   //Выводим первую таблицу

                Decommposition(dataGridView2, chartPage2, Data, checkedListBox1, checkedListBox5, checkedListBox8); //Проводим 1-й уровень декомпозиции при подключении базы данных. С выведением в таблицы, графиком и списком

                //Marks(); //создаем график марок

                if (tabControl1.TabPages.Count == 1)
                {
                    tabControl1.TabPages.Add(tabPage2);   //Подключаем вкладки декомпозиции
                    tabControl1.TabPages.Add(tabPage3);
                    tabControl1.TabPages.Add(tabPage4);
                    tabControl1.TabPages.Add(tabPage5);



                    groupBox1.Enabled = true;             //подключаем выключенные элементы
                    button2.Enabled = true;
                    comboBox1.Enabled = true;
                    button10.Enabled = true;
                    button11.Enabled = true;

                    button5.Enabled = false;        //Блоки еще не распределены, поэтому отключаем эти элементы
                    label5.Enabled = false;
                    comboBox2.Enabled = false;
                    label5.Text = "";
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DataRow NewRow = Data.NewRow();                    //создаем объект строку
            int lastRowIndex = Data.Rows.Count - 1;
            NewRow[0] = Convert.ToInt32(Data.Rows[lastRowIndex][0]) + 1;      //добавляем новый номер эпохи

            //Вычисляем среднюю высоту

            double MediumHeight = 0;

            //в следующем цикле вычисляется числитель формулы

            for (int i = 0; i < (Data.Rows.Count - 1); i++)
            {
                MediumHeight = MediumHeight + Math.Abs(Convert.ToDouble(Data.Rows[i][1]) - Convert.ToDouble(Data.Rows[i + 1][1]));
            }

            //затем числитель делится на знаменатель. Количество строк в dataGrid равно числу эпох (n)
            MediumHeight = MediumHeight / (Data.Rows.Count - 1);

            //создаем объект-генератор случайных чисел
            Random random = new Random();

            //вычисляем новые значения для меток
            for (int j = 1; j < Data.Columns.Count; j++)
            {
                double random_num = Math.Round(MediumHeight, 4);           //создаем переменную, хранящую случайное число
                random_num = random_num * 10000;                             //переводим в целые числа для случайной генерации
                random_num = random.Next(-(Convert.ToInt32(random_num) / 2), Convert.ToInt32(random_num) / 2);  //генерируем число в диапазоне по формуле
                random_num = random_num / 10000;                             //возвращаемся к дробному числу. Получаем отклонение
                NewRow[j] = Convert.ToDouble(Data.Rows[0][j]) + random_num; //определяем новое значение метки, добавив к нему случайное отклонение
            }

            Data.Rows.Add(NewRow); //добавляем заполненную строку в таблицу
            OutPutTable(Data, dataGridView1); //выводим в таблицу с новой строкой
            Decommposition(dataGridView2, chartPage2, Data, checkedListBox1, checkedListBox5, checkedListBox8); //Проводим 1-й уровень декомпозиции при добавлении новой строки
            //Marks(); //обновляем график марок

            if (comboBox2.Items.Count != 0)    //если блоки определены на 2 уровне
            {
                comboBox2.SelectedIndex = 1;   //обновление таблиц блоков. Блоков всегда будет минимум два
                comboBox2.SelectedIndex = 0;
            }

            if (comboBox4.Items.Count != 0)    //если блоки определены на 3 уровне
            {
                comboBox4.SelectedIndex = 1;   //обновление таблиц блоков. Блоков всегда будет минимум два
                comboBox4.SelectedIndex = 0;
            }

            Stabilnost_inf(); //выводим данные о стабильности объекта
        }

        //кнопка для принятия коэффициента и погрешности
        private void button3_Click(object sender, EventArgs e)
        {
            //Проверка ввода данных
            if (textBox1.Text == "")
            {
                if (textBox2.Text == "") MessageBox.Show("Не введено значение точности и коэффициента эксп. сглаживания ", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else MessageBox.Show("Не введено значение точности", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (textBox2.Text == "")
            {
                MessageBox.Show("Не введено значение коэффициента эксп. сглаживания", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //Проверка корректности вводимого значения коэффициента эксп. сглаживания
            if (Convert.ToDouble(textBox2.Text) > 1 || Convert.ToDouble(textBox2.Text) < 0)
            {
                MessageBox.Show("Значение коэффициента эксп. сглаживания должно находиться в интервале от 0 до 1", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            E = Convert.ToDouble(textBox1.Text);
            A = Convert.ToDouble(textBox2.Text);

            toolStripStatusLabel2.Text = "Точность E = " + E;
            toolStripStatusLabel3.Text = "Коэффициент эксп. сглаживания А = " + A;
            Decommposition(dataGridView2, chartPage2, Data, checkedListBox1, checkedListBox5, checkedListBox8); //Проводим 1-й уровень декомпозиции
            Stabilnost_inf(); //показываем информацию о стабильности объекта

            if (comboBox2.Items.Count != 0)   //если блоки определены
            {
                comboBox2.SelectedIndex = 1;  //обновление таблиц блоков
                comboBox2.SelectedIndex = 0;
            }

            if (comboBox4.Items.Count != 0)
            {
                comboBox4.SelectedIndex = 0;
                comboBox4.SelectedIndex = 1;
            }
        }

        // Функция, проверяющая правильность ввода значений в textBox1 и textBox2
        private void textBox1n2_KeyPress(object sender, KeyPressEventArgs e)
        {
            System.Windows.Forms.TextBox textbox = sender as System.Windows.Forms.TextBox;

            //числа от нуля до девяти разрешены
            if ((e.KeyChar >= '0') && (e.KeyChar <= '9'))
            {
                return;
            }

            //нельзя использовать точку
            if (e.KeyChar == '.')
            {
                MessageBox.Show("Используйте запятую вместо точки", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //Запятая может быть только одна
            if (e.KeyChar == ',')
            {
                if (textbox.Text.IndexOf(',') != -1)
                {
                    e.Handled = true;
                }
                return;
            }

            //Разрешены также Enter, Backspace, Escape
            if (Char.IsControl(e.KeyChar))
            {
                return;
            }

            //Остальные символы запрещены
            e.Handled = true;
        }

        //Вычисления таблицы декомпозиции
        private DataTable TableData(DataTable dataTable)
        {
            DataTable dekomp1 = new DataTable(); //создание таблицы декомпозиции
            dekomp1.Columns.Add("Эпоха"); //добавление столбцов с названиями
            dekomp1.Columns.Add("M");
            dekomp1.Columns.Add("A(сек.)");
            dekomp1.Columns.Add("M(пр.)");
            dekomp1.Columns.Add("A(пр.)");
            dekomp1.Columns.Add("M+");
            dekomp1.Columns.Add("A+");
            dekomp1.Columns.Add("M-");
            dekomp1.Columns.Add("A-");
            dekomp1.Columns.Add("М+ (пр.)");
            dekomp1.Columns.Add("М- (пр.)");
            dekomp1.Columns.Add("A+ (пр.)");
            dekomp1.Columns.Add("A- (пр.)");
            dekomp1.Columns.Add("Устойчивость");

            //Вычисление значений для графика фазовой траектории
            double M0 = 0; //вводим переменную для M0
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                dekomp1.Rows.Add();                        //добавляем строки
                dekomp1.Rows[i][0] = dataTable.Rows[i][0];    //добавляем номера эпох

                double Mi = 0;                              //вводим переменную для Mi
                for (int j = 1; j < dataTable.Columns.Count; j++)      //цикл по точкам
                //вычисляем сумму квадратов высот
                {
                    Mi = Mi + Convert.ToDouble(dataTable.Rows[i][j]) * Convert.ToDouble(dataTable.Rows[i][j]);
                }

                Mi = Math.Sqrt(Mi); //вычисляем корень
                if (i == 0) M0 = Mi; //запоминаем M0

                double Ai = 0;                      //вводим переменную для Аi
                for (int j = 1; j < dataTable.Columns.Count; j++)  //цикл по точкам
                {
                    Ai = Ai + Convert.ToDouble(dataTable.Rows[0][j]) * Convert.ToDouble(dataTable.Rows[i][j]); //вычисляем числитель
                }
                Ai = Ai / (Mi * M0);                              //в скобках
                if (Ai > 1) Ai = 1;

                Ai = Math.Round(Math.Acos(Ai), 6) * 206264.816;           //вычисляем значение Ai в радианах и переводим в секунды
                Mi = Math.Round(Mi, 4);                       //округляем
                dekomp1.Rows[i][1] = Mi;               //вписываем в таблицу M и A
                dekomp1.Rows[i][2] = Ai;
            }

            //вычисляем значения для графика прогнозируемой траектории
            double sumMi = 0;
            double sumA = 0;
            for (int i = 0; i < dekomp1.Rows.Count; i++)
            {
                sumMi = sumMi + Convert.ToDouble(dekomp1.Rows[i][1]);
                sumA = sumA + Convert.ToDouble(dekomp1.Rows[i][2]);
            }
            double Mpr = A * M0 + (1 - A) * (sumMi / dekomp1.Rows.Count);     //вычисляем Mpr нулевое
            double Apr = A * Convert.ToDouble(dekomp1.Rows[0][2]) + (1 - A) * (sumA / dekomp1.Rows.Count); //вычисляем Apr нулевое
            dekomp1.Rows[0][3] = Math.Round(Mpr, 4);
            dekomp1.Rows[0][4] = Math.Round(Apr, 4);

            for (int i = 1; i < dekomp1.Rows.Count; i++)
            {
                Mpr = A * Convert.ToDouble(dekomp1.Rows[i][1]) + (1 - A) * Mpr;
                dekomp1.Rows[i][3] = Math.Round(Mpr, 4);
                Apr = A * Convert.ToDouble(dekomp1.Rows[i][2]) + (1 - A) * Apr;
                dekomp1.Rows[i][4] = Math.Round(Apr, 4);
            }

            dekomp1.Rows.Add();  //добавляем прогнозируемую эпоху
            dekomp1.Rows[dekomp1.Rows.Count - 1][0] = "Прогноз";
            dekomp1.Rows[dekomp1.Rows.Count - 1][3] = A * sumMi / (dekomp1.Rows.Count - 1) + (1 - A) * Convert.ToDouble(dekomp1.Rows[dekomp1.Rows.Count - 2][3]);    //вычисления для n+1 эпохи
            dekomp1.Rows[dekomp1.Rows.Count - 1][4] = A * sumA / (dekomp1.Rows.Count - 1) + (1 - A) * Convert.ToDouble(dekomp1.Rows[dekomp1.Rows.Count - 2][4]);

            //вычисляем значения для верхнего предела
            double M0Verh = 0;
            double MiV = 0;
            double AV = 0;
            //вводим переменную для M0
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                double MiVerh = 0; //вводим переменную для Mi
                for (int j = 1; j < dataTable.Columns.Count; j++)                      //цикл по точкам
                {
                    MiVerh += Math.Pow(Convert.ToDouble(dataTable.Rows[i][j]) + E, 2);  //вычисляем сумму квадратов высот
                }
                MiVerh = Math.Sqrt(MiVerh);                                           //берем кв.корень
                if (i == 0) M0Verh = MiVerh;                                          //запоминаем M0

                double AiVerh = 0;
                for (int j = 1; j < dataTable.Columns.Count; j++)                      //цикл по точкам
                {
                    AiVerh += (Convert.ToDouble(dataTable.Rows[0][j]) + E) * (Convert.ToDouble(dataTable.Rows[i][j]) + E);    //вычисляем числитель
                }
                AiVerh /= MiVerh * M0Verh;                                             //в скобках
                if (AiVerh > 0.99999999999) AiVerh = 1;
                AiVerh = Math.Acos(AiVerh);                                           //значение Ai в радианах
                AiVerh = AiVerh * 206264.816;                                         //значение Ai в секундах

                MiVerh = Math.Round(MiVerh, 6);                                       //округляем
                AiVerh = Math.Round(AiVerh, 4);
                dekomp1.Rows[i][5] = MiVerh;                                        //вписываем в таблицу M и A
                dekomp1.Rows[i][6] = AiVerh;

                AV += AiVerh;
                MiV += MiVerh;
            }



            //вычисляем значения для нижнего предела
            double M0Niz = 0; //вводим переменную для M0
            double MiN = 0;
            double AN = 0;
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                double MiNiz = 0;                                                   //вводим переменную для Mi
                for (int j = 1; j < dataTable.Columns.Count; j++)                      //цикл по точкам
                {
                    MiNiz += Math.Pow(Convert.ToDouble(dataTable.Rows[i][j]) - E, 2);  //вычисляем сумму квадратов высот
                }

                MiNiz = Math.Sqrt(MiNiz);                                           //берем кв.корень
                if (i == 0) M0Niz = MiNiz;                                          //запоминаем M0

                double AiNiz = 0;
                for (int j = 1; j < dataTable.Columns.Count; j++)                      //цикл по точкам
                {
                    AiNiz += (Convert.ToDouble(dataTable.Rows[0][j]) - E) * (Convert.ToDouble(dataTable.Rows[i][j]) - E);    //вычисляем числитель
                }
                AiNiz /= MiNiz * M0Niz;                                             //в скобках
                if (AiNiz > 0.99999999999) AiNiz = 1;
                AiNiz = Math.Acos(AiNiz);                                           //значение Ai в радианах
                AiNiz = AiNiz * 206264.816;                                         //значение Ai в секундах


                MiNiz = Math.Round(MiNiz, 4);                                       //округляем
                AiNiz = Math.Round(AiNiz, 4);
                dekomp1.Rows[i][7] = MiNiz;                                        //вписываем в таблицу M и A
                dekomp1.Rows[i][8] = AiNiz;

                MiN += MiNiz;
                AN += AiNiz;
            }

            dekomp1.Rows[dekomp1.Rows.Count - 1][5] = Math.Round(A * MiV / (dekomp1.Rows.Count - 1) + (1 - A) * Convert.ToDouble(dekomp1.Rows[dekomp1.Rows.Count - 2][5]), 6);
            dekomp1.Rows[dekomp1.Rows.Count - 1][6] = Math.Round(A * AV / (dekomp1.Rows.Count - 1) + (1 - A) * Convert.ToDouble(dekomp1.Rows[dekomp1.Rows.Count - 2][6]), 6);
            dekomp1.Rows[dekomp1.Rows.Count - 1][7] = Math.Round(A * MiN / (dekomp1.Rows.Count - 1) + (1 - A) * Convert.ToDouble(dekomp1.Rows[dekomp1.Rows.Count - 2][7]), 6);
            dekomp1.Rows[dekomp1.Rows.Count - 1][8] = Math.Round(A * AN / (dekomp1.Rows.Count - 1) + (1 - A) * Convert.ToDouble(dekomp1.Rows[dekomp1.Rows.Count - 2][8]), 6);

            //!!!
            //вычисляем значения для графика А+пр.(Mпр+)
            double SumPlusM = 0;
            double SumPlusA = 0;
            for (int i = 0; i < dekomp1.Rows.Count; i++)
            {
                SumPlusM = SumPlusM + Convert.ToDouble(dekomp1.Rows[i][5]);
                SumPlusA = SumPlusA + Convert.ToDouble(dekomp1.Rows[i][6]);
            }
            double PlusMpr = A * M0 + (1 - A) * (SumPlusM / dekomp1.Rows.Count);     //вычисляем Mpr нулевое
            double PlusApr = A * Convert.ToDouble(dekomp1.Rows[0][6]) + (1 - A) * (SumPlusA / dekomp1.Rows.Count); //вычисляем Apr нулевое
            dekomp1.Rows[0][9] = Math.Round(PlusMpr, 6);
            dekomp1.Rows[0][11] = Math.Round(PlusApr, 6);

            for (int i = 1; i < dekomp1.Rows.Count; i++)
            {
                PlusMpr = A * Convert.ToDouble(dekomp1.Rows[i][5]) + (1 - A) * PlusMpr;
                dekomp1.Rows[i][9] = Math.Round(PlusMpr, 6);
                PlusApr = A * Convert.ToDouble(dekomp1.Rows[i][6]) + (1 - A) * PlusApr;
                dekomp1.Rows[i][11] = Math.Round(PlusApr, 6);
            }

            //dekomp1.Rows[dekomp1.Rows.Count - 1][9] = A * SumPlusM / (dekomp1.Rows.Count - 1) + (1 - A) * Convert.ToDouble(dekomp1.Rows[dekomp1.Rows.Count - 2][9]);
            //dekomp1.Rows[dekomp1.Rows.Count - 1][11] = A * SumPlusM / (dekomp1.Rows.Count - 1) + (1 - A) * Convert.ToDouble(dekomp1.Rows[dekomp1.Rows.Count - 2][11]);

            //вычисляем значения для графика А-пр.(Mпр-)
            double SumMinusM = 0;
            double SumMinusA = 0;
            for (int i = 0; i < dekomp1.Rows.Count; i++)
            {
                SumMinusM = SumMinusM + Convert.ToDouble(dekomp1.Rows[i][7]);
                SumMinusA = SumMinusA + Convert.ToDouble(dekomp1.Rows[i][8]);
            }
            double MinusMpr = A * M0 + (1 - A) * (SumMinusM / dekomp1.Rows.Count);     //вычисляем Mpr нулевое
            double MinusApr = A * Convert.ToDouble(dekomp1.Rows[0][8]) + (1 - A) * (SumMinusA / dekomp1.Rows.Count); //вычисляем Apr нулевое
            dekomp1.Rows[0][10] = Math.Round(MinusMpr, 6);
            dekomp1.Rows[0][12] = Math.Round(MinusApr, 6);

            for (int i = 1; i < dekomp1.Rows.Count; i++)
            {
                MinusMpr = A * Convert.ToDouble(dekomp1.Rows[i][7]) + (1 - A) * MinusMpr;
                dekomp1.Rows[i][10] = Math.Round(MinusMpr, 6);
                MinusApr = A * Convert.ToDouble(dekomp1.Rows[i][8]) + (1 - A) * MinusApr;
                dekomp1.Rows[i][12] = Math.Round(MinusApr, 6);
            }

            //dekomp1.Rows[dekomp1.Rows.Count - 1][10] = A * SumMinusM / (dekomp1.Rows.Count - 1) + (1 - A) * Convert.ToDouble(dekomp1.Rows[dekomp1.Rows.Count - 2][10]);
            //dekomp1.Rows[dekomp1.Rows.Count - 1][12] = A * SumPlusM / (dekomp1.Rows.Count - 1) + (1 - A) * Convert.ToDouble(dekomp1.Rows[dekomp1.Rows.Count - 2][12]);

            //определение устойчивости по эпохам
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                double left = Math.Abs(Convert.ToDouble(dekomp1.Rows[i][5]) - Convert.ToDouble(dekomp1.Rows[i][7]));
                double right = Math.Abs(Convert.ToDouble(dekomp1.Rows[i][1]) - Convert.ToDouble(dekomp1.Rows[0][1]));

                if (left >= right) dekomp1.Rows[i][13] = "+";
                else dekomp1.Rows[i][13] = "-";
            }

            double PrLeft = Math.Abs(Convert.ToDouble(dekomp1.Rows[(dataTable.Rows.Count - 1)][5]) - Convert.ToDouble(dekomp1.Rows[(dataTable.Rows.Count - 1)][7]));
            double PrRight = Math.Abs(Convert.ToDouble(dekomp1.Rows[(dataTable.Rows.Count - 1)][2]) - Convert.ToDouble(dekomp1.Rows[0][2]));

            if (PrLeft >= PrRight) dekomp1.Rows[(dekomp1.Rows.Count - 1)][13] = "+";
            else dekomp1.Rows[(dekomp1.Rows.Count - 1)][13] = "-";

            return dekomp1;
        }

        //Функция рисующая график по двум столбцам таблицы
        //private void ChartMain(Chart chart, DataTable dataTable, int col1, int col2, string name)
        //{
        //    Series serie = new Series(name);
        //    for (int i = 0; i < dataTable.Rows.Count; i++)
        //    {
        //        if (dataTable.Rows[i][col1] != DBNull.Value && dataTable.Rows[i][col2] != DBNull.Value)
        //        {
        //            if (dataTable.Rows[i][col1].ToString() != "Прогноз")
        //            {
        //                serie.Points.AddXY(Convert.ToDouble(dataTable.Rows[i][col1]), Convert.ToDouble(dataTable.Rows[i][col2]));
        //            }
        //            else
        //            {
        //                serie.Points.AddXY(i, Convert.ToDouble(dataTable.Rows[i][col2]));
        //            }
        //            serie.Points[i].Label = $"{i}";
        //        }
        //        else break;
        //    }

        //    chart.Series.Add(serie);
        //}

        private void ChartMain(Chart chart, DataTable dataTable, int col1, int col2, string name)
        {
            Series serie = new Series(name);
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                if (dataTable.Rows[i][col1] != DBNull.Value && dataTable.Rows[i][col2] != DBNull.Value)
                {
                    if (dataTable.Rows[i][col1].ToString() != "Прогноз")
                    {
                        serie.Points.AddXY(Convert.ToDouble(dataTable.Rows[i][col1]), Convert.ToDouble(dataTable.Rows[i][col2]));
                    }
                    else
                    {
                        serie.Points.AddXY(i, Convert.ToDouble(dataTable.Rows[i][col2]));
                    }
                    serie.Points[i].Label = dataTable.Rows[i][0].ToString(); // Изменение наименования точки на значение из dataTable.Rows[i][0]
                }
                else break;
            }

            chart.Series.Add(serie);
        }


        //Полная декомпозиция по таблице. С выведением в таблицы, графиком и списком
        private void Decommposition(DataGridView DataGrid, Chart chart, DataTable dataTable, CheckedListBox chBox1, CheckedListBox chBox2, CheckedListBox chBox3)
        { // !!!
            DataTable dataT = TableData(dataTable); //Вычисляем таблицу значений
            OutPutTable(dataT, DataGrid); // Показываем таблицу значений
            chart.Series.Clear(); //очищаем график и список
            chBox1.Items.Clear();
            chBox2.Items.Clear();
            ChartMain(chart, dataT, 0, 1, "М(t)");
            ChartMain(chart, dataT, 0, 5, "М+(t)");
            ChartMain(chart, dataT, 0, 7, "М-(t)");
            ChartMain(chart, dataT, 0, 3, "Прогнозное М(t)");

            ChartMain(chart, dataT, 1, 2, "М(a)");        //Добавляем графики
            ChartMain(chart, dataT, 5, 6, "Верхний предел М(a)+");
            ChartMain(chart, dataT, 7, 8, "Нижний предел М(a)-");
            ChartMain(chart, dataT, 9, 11, "Прогногнозная М(a)+");
            ChartMain(chart, dataT, 10, 12, "Прогногнозная М(a)-");
            ChartMain(chart, dataT, 3, 4, "Прогнозируемая М(a)");

            ChartMain(chart, dataT, 0, 2, "A(t)");
            ChartMain(chart, dataT, 0, 6, "A(t)+");
            ChartMain(chart, dataT, 0, 8, "A(t)-");
            int countSerie = 0;
            //добавляем серии в список
            foreach (Series serie in chart.Series)
            {
                if (countSerie < 4)
                {
                    chBox1.Items.Add(serie.Name);
                    countSerie++;
                }
                else if (countSerie < 10)
                {
                    chBox2.Items.Add(serie.Name);
                    countSerie++;
                }
                else
                {
                    chBox3.Items.Add(serie.Name);
                }

            }

            chBox1.SelectedIndex = -1;
            //Помечаем нестабильные эпохи
            for (int i = 0; i < DataGrid.Rows.Count; i++)
            {
                if (DataGrid.Rows[i].Cells[13].Value.ToString() == "-")
                    DataGrid.Rows[i].Cells[13].Style.BackColor = Color.Red;
                else if (DataGrid.Rows[i].Cells[13].Value.ToString() == "+") DataGrid.Rows[i].Cells[13].Style.BackColor = Color.Green;
                else DataGrid.Rows[i].Cells[13].Style.BackColor = Color.Yellow;

            }
        }
        // Убираем галочки, если в активе другой бокс
        private void CheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            CheckedListBox currentCheckedListBox = (CheckedListBox)sender;

            List<CheckedListBox> otherCheckedListBoxes = new List<CheckedListBox>
            {
                    checkedListBox1,
                    checkedListBox2,
                    checkedListBox3,
                    checkedListBox4,
                    checkedListBox5,
                    checkedListBox6,
                    checkedListBox7,
                    checkedListBox8,
                    checkedListBox9,
                    checkedListBox10,
            };

            // Проходимся по всем CheckedListBox'ам, кроме текущего
            foreach (CheckedListBox checkedListBox in otherCheckedListBoxes.Where(clb => clb != currentCheckedListBox))
            {
                // Сбрасываем галочки в других CheckedListBox'ах
                for (int i = 0; i < checkedListBox.Items.Count; i++)
                {
                    if (checkedListBox.GetItemChecked(i))
                        checkedListBox.SetItemChecked(i, false);
                }
            }
        }

        //Функция, работающая при изменении выбранного индекса в checkedListBox-ах
        private void checkedListBox1_MouseUp(object sender, MouseEventArgs e)
        {
            Chart chart = chart1;
            Chart donorChart = chartPage2;
            CheckedListBox checkedListBox = sender as CheckedListBox;
            switch (checkedListBox.Name)
            {
                case "checkedListBox1":
                    chart = chart1;
                    donorChart = chartPage2;
                    break;

                case "checkedListBox2":
                    chart = chart2;
                    donorChart = chartPage3;
                    break;

                case "checkedListBox3":
                    chart = chart3;
                    donorChart = chartPage5;
                    break;

                case "checkedListBox4":
                    chart = chart4;
                    donorChart = chartPage4;
                    break;
                case "checkedListBox5":
                    chart = chart1;
                    donorChart = chartPage2;
                    break;
                case "checkedListBox6":
                    chart = chart2;
                    donorChart = chartPage3;
                    break;
                case "checkedListBox7":
                    chart = chart4;
                    donorChart = chartPage4;
                    break;
                case "checkedListBox8":
                    chart = chart1;
                    donorChart = chartPage2;
                    break;
                case "checkedListBox9":
                    chart = chart2;
                    donorChart = chartPage3;
                    break;
                case "checkedListBox10":
                    chart = chart3;
                    donorChart = chartPage5;
                    break;

                    //!!
            }
            chart.Series.Clear();
            for (int i = 0; i < checkedListBox.Items.Count; i++) //выводит на экран выбранные графики
            {
                if (checkedListBox.GetItemChecked(i))
                {
                    chart.Series.Add(checkedListBox.Items[i].ToString());
                    foreach (DataPoint point in donorChart.Series[checkedListBox.Items[i].ToString()].Points)
                    {
                        chart.Series[checkedListBox.Items[i].ToString()].Points.Add(point);
                    }
                }
            }
            foreach (Series serie in chart.Series) //вид графика
            {
                serie.ChartType = SeriesChartType.Spline;
                chart.Series[serie.Name].MarkerBorderColor = System.Drawing.Color.Black;
                chart.Series[serie.Name].MarkerStyle = MarkerStyle.Circle;
                chart.Series[serie.Name].MarkerSize = 6;
                chart.ChartAreas[0].AxisX.IsStartedFromZero = false;
                chart.ChartAreas[0].AxisY.IsStartedFromZero = false;
            }
            if (checkedListBox.CheckedItems.Count == 0) //оси графиков
            {
                chart.ChartAreas[0].AxisY.Enabled = AxisEnabled.True;
                chart.ChartAreas[0].AxisX.Enabled = AxisEnabled.True;
            }
        }

        //второй уровень декомпозиции. Кнопка для вызова окна распределения марок
        private void button4_Click(object sender, EventArgs e)
        {
            //проверка правильности ввода количества блоков
            if (textBox3.Text == "" || Convert.ToInt32(textBox3.Text) > (int)((Data.Columns.Count - 1) / 2) || Convert.ToInt32(textBox3.Text) < 2)
            {
                MessageBox.Show($"Неверное количество блоков. Введите значение от 2 до {(int)((Data.Columns.Count - 1) / 2)}.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Blocks blocks = new Blocks(pictureBox1.Image, Convert.ToInt32(textBox3.Text), Data.Columns.Count - 1);
            //Если марки распределены по блокам
            if (blocks.ShowDialog() == DialogResult.OK)
            {
                this.blocks = blocks.GetBTable();
                comboBox2.Items.Clear();
                comboBox3.Items.Clear();
                dataGridView3.Rows.Clear();
                dataGridView3.Columns.Clear();
                dataGridView4.Rows.Clear();
                dataGridView4.Columns.Clear();

                comboBox5.Items.Clear();

                for (int i = 0; i < this.blocks.Columns.Count; i++)
                {
                    comboBox2.Items.Add((char)(i + 1040));  //Вывод букв кириллицы с помощью юникода в список блоков на вкладке "Блок"
                    comboBox3.Items.Add((char)(i + 1040));  //Вывод букв кириллицы с помощью юникода в список блоков на вкладке "Подблок"
                    comboBox5.Items.Add((char)(i + 1040));
                }
                comboBox5.Items.Add("Все марки");


                label5.Text = "";
                comboBox2.SelectedIndex = 0;
                comboBox3.SelectedIndex = 0;

                button5.Enabled = true;                     //Включаем элементы выбора блоков
                label5.Enabled = true;
                comboBox2.Enabled = true;

                tabControl1.TabPages.Remove(tabPage5);      //Убираем старую вкладку с подблоками
                tabControl1.TabPages.Remove(tabPage4);
                tabControl1.TabPages.Add(tabPage4); //убираем вкладку марок, ставим на ее место вкладку подблоков, возвращаем вкладку марок
                tabControl1.TabPages.Add(tabPage5);

                label7.Text = $"Достаточное количество связей: {(this.blocks.Rows.Count * 3) - 6}"; //выводим информацию для пользователя

                checkedListBox4.Items.Clear();              //очищаем декомпозицию подблоков после новой расстановки по блокам
                chart4.Series.Clear();
                dataGridView5.Rows.Clear();
                dataGridView5.Columns.Clear();
                dataGridView6.Rows.Clear();
                dataGridView6.Columns.Clear();
                comboBox4.Items.Clear();
                label6.Text = "";
                button9.Enabled = false;
                comboBox4.Enabled = false;
                label8.Enabled = false;
            }
        }

        //проверка вводимых символов при выборе количества блоков
        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            //числа от нуля до девяти разрешены
            if ((e.KeyChar >= '0') && (e.KeyChar <= '9'))
            {
                return;
            }

            //Разрешены также Enter, Backspace, Escape
            if (Char.IsControl(e.KeyChar))
            {
                return;
            }

            //Остальные символы запрещены
            e.Handled = true;
        }

        //Выбор блока для демонстрации таблицы
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataTable block = new DataTable();

            for (int i = 0; i < Data.Rows.Count; i++) //добавляем строки
            {
                block.Rows.Add();
            }
            block.Columns.Add("Эпоха");
            for (int i = 0; i < blocks.Rows.Count; i++) //добавляем столбцы
            {
                block.Columns.Add(Convert.ToString(blocks.Rows[i][comboBox2.SelectedIndex]));
            }

            for (int i = 0; i < block.Rows.Count; i++) //заполняем таблицу
                for (int j = 0; j < block.Columns.Count; j++)
                {
                    if (j == 0) block.Rows[i][j] = i;
                    else block.Rows[i][j] = Data.Rows[i][Convert.ToInt32(blocks.Rows[j - 1][comboBox2.SelectedIndex])];
                }
            OutPutTable(block, dataGridView4); //вывод таблицы
            Decommposition(dataGridView3, chartPage3, block, checkedListBox2, checkedListBox6, checkedListBox9); // проводим декомпозицию

            for (int i = 0; i < dataGridView3.RowCount; i++) //Проверяем есть ли нестабильные эпохи и выводим сообщение для пользователя
            {
                if (dataGridView3.Rows[i].Cells[dataGridView3.ColumnCount - 1].Value.ToString() == "-")
                {
                    label5.Text = "Блок нестабилен, требуется декомпозиция.";
                    label5.ForeColor = Color.Red;
                    break;
                }
                else
                {
                    label5.Text = "Блок стабилен, декомпозиция не требуется.";
                    label5.ForeColor = Color.Green;
                }
            }
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            Marks();

            //DataTable block = new DataTable();

            //for (int i = 0; i < Data.Rows.Count; i++) //добавляем строки
            //{
            //    block.Rows.Add();
            //}
            //block.Columns.Add("Эпоха");
            //for (int i = 0; i < blocks.Rows.Count; i++) //добавляем столбцы
            //{
            //    block.Columns.Add(Convert.ToString(blocks.Rows[i][comboBox5.SelectedIndex]));
            //}

            //for (int i = 0; i < block.Rows.Count; i++) //заполняем таблицу
            //    for (int j = 0; j < block.Columns.Count; j++)
            //    {
            //        if (j == 0) block.Rows[i][j] = i;
            //        else block.Rows[i][j] = Data.Rows[i][Convert.ToInt32(blocks.Rows[j - 1][comboBox2.SelectedIndex])];
            //    }
            //OutPutTable(block, dataGridView4); //вывод таблицы
            //Decommposition(dataGridView3, chartPage3, block, checkedListBox2, checkedListBox6, checkedListBox9); // проводим декомпозицию

            //for (int i = 0; i < dataGridView3.RowCount; i++) //Проверяем есть ли нестабильные эпохи и выводим сообщение для пользователя
            //{
            //    if (dataGridView3.Rows[i].Cells[dataGridView3.ColumnCount - 1].Value.ToString() == "-")
            //    {
            //        label5.Text = "Блок нестабилен, требуется декомпозиция.";
            //        label5.ForeColor = Color.Red;
            //        break;
            //    }
            //    else
            //    {
            //        label5.Text = "Блок стабилен, декомпозиция не требуется.";
            //        label5.ForeColor = Color.Green;
            //    }
            //}
        }

        //Кнопка "Все графики". Создает новое окно, в котором демонстрируются графики всех блоков
        Graphics open_graphs = new Graphics();
        private void button7_Click(object sender, EventArgs e)
        {
            Graphics Graphs = new Graphics();
            if (!open_graphs.IsFormOpen) //создаем график
            {
                Graphs.Show();
                open_graphs = Graphs;
                open_graphs.IsFormOpen = true;
                open_graphs.my_chart.Series.Clear();
                open_graphs.checkedListBox1.Items.Clear();

                for (int index = 0; index < comboBox2.Items.Count; index++)
                {
                    DataTable block = new DataTable();
                    for (int i = 0; i < Data.Rows.Count; i++) //добавляем строки
                    {
                        block.Rows.Add();
                    }
                    block.Columns.Add("Эпоха");
                    for (int i = 0; i < blocks.Rows.Count; i++) //добавляем столбцы
                    {
                        block.Columns.Add(Convert.ToString(blocks.Rows[i][index]));
                    }

                    for (int i = 0; i < block.Rows.Count; i++) //заполняем таблицу
                        for (int j = 0; j < block.Columns.Count; j++)
                        {
                            if (j == 0) block.Rows[i][j] = i;
                            else block.Rows[i][j] = Data.Rows[i][Convert.ToInt32(blocks.Rows[j - 1][index])];
                        }
                    //строим графики
                    ChartMain(open_graphs.my_chart, TableData(block), 1, 2, $"Фазовая траектория блок {comboBox2.Items[index]}");         //Добавляем графики
                    ChartMain(open_graphs.my_chart, TableData(block), 5, 6, $"Верхний предел блок {comboBox2.Items[index]}");
                    ChartMain(open_graphs.my_chart, TableData(block), 7, 8, $"Нижний предел блок {comboBox2.Items[index]}");
                    ChartMain(open_graphs.my_chart, TableData(block), 0, 1, $"M(t) подблок {comboBox2.Items[index]}");
                    ChartMain(open_graphs.my_chart, TableData(block), 0, 5, $"M+(t) подблок {comboBox2.Items[index]}");
                    ChartMain(open_graphs.my_chart, TableData(block), 0, 7, $"M-(t) подблок {comboBox2.Items[index]}");

                    //!!!
                }

                //добавляем серии в список
                foreach (Series serie in open_graphs.my_chart.Series)
                {
                    open_graphs.checkedListBox1.Items.Add(serie.Name);
                }

                open_graphs.checkedListBox1.SelectedIndex = 0;
                open_graphs.checkedListBox1.SelectedIndex = -1;
            }

            else open_graphs.BringToFront();
        }

        //Функция, создающая график высот марок по одной
        //private void Marks() //!!
        //{
        //    checkedListBox3.Items.Clear();  //очищаем график
        //    chart3.ChartAreas[0].AxisX.Title = "Эпоха";        //настройки поля графиков
        //    chart3.ChartAreas[0].AxisY.Title = "Высота";
        //    chart3.ChartAreas[0].AxisY.IsStartedFromZero = false;
        //    chart3.ChartAreas[0].AxisX.IsStartedFromZero = true;

        //    chartPage5 = new Chart();

        //    for (int i = 1; i < Data.Columns.Count; i++)       //для каждой марки создаем серию
        //    {
        //        Series serie = new Series($"Марка {i}");
        //        for (int j = 0; j < Data.Rows.Count; j++)      //добавляем точки графика
        //        {
        //            serie.Points.AddXY($"{j}", Convert.ToDouble(Data.Rows[j][i]));
        //            serie.Points[j].Label = $"{j}";
        //        }

        //        chartPage5.Series.Add(serie);
        //        checkedListBox3.Items.Add(serie.Name);
        //    }

        //    checkedListBox3.SelectedIndex = 0;
        //    checkedListBox3.SelectedIndex = -1;
        //}

        private void Marks()
        {
            checkedListBox3.Items.Clear();
            chart3.ChartAreas[0].AxisX.Title = "Эпоха";
            chart3.ChartAreas[0].AxisY.Title = "Высота";
            chart3.ChartAreas[0].AxisY.IsStartedFromZero = false;
            chart3.ChartAreas[0].AxisX.IsStartedFromZero = true;

            chartPage5 = new Chart();

            bool showAllMarks = comboBox5.SelectedItem.ToString() == "Все марки"; // Проверяем выбранный элемент в comboBox

            int blockCount = comboBox5.Items.Count - 1; // Количество блоков в ComboBox, исключая "Все марки"

            int marksPerBlock = (Data.Columns.Count - 1) / blockCount; // Количество марок на каждый блок

            for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
            {
                int startIndex = blockIndex * marksPerBlock;
                int endIndex = startIndex + marksPerBlock - 1;

                if (showAllMarks || comboBox5.SelectedItem.ToString() == comboBox5.Items[blockIndex].ToString())
                {
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        Series serie = new Series($"Марка {i + 1}");
                        for (int j = 0; j < Data.Rows.Count; j++)
                        {
                            serie.Points.AddXY($"{j}", Convert.ToDouble(Data.Rows[j][i + 1]));
                            serie.Points[j].Label = $"{j}";
                        }

                        chartPage5.Series.Add(serie);
                        checkedListBox3.Items.Add(serie.Name);
                    }
                }
            }

            checkedListBox3.SelectedIndex = 0;
            checkedListBox3.SelectedIndex = -1;
        }




        //Кнопка "выбрать все"
        private void button8_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox3.Items.Count; i++)
            {
                checkedListBox3.SetItemChecked(i, true);
            }
            checkedListBox3.SelectedIndex = -1;
            checkedListBox3.SelectedIndex = 0;
        }

        //кнопка "убрать все"
        private void button9_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox3.Items.Count; i++)
            {
                checkedListBox3.SetItemChecked(i, false);
            }
            checkedListBox3.SelectedIndex = -1;
            checkedListBox3.SelectedIndex = 0;

        }

        //Выбор таблицы в списке
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            TableBD(); //заполняем таблицу данными
            OutPutTable(Data, dataGridView1);                          //выводим в таблицу
            Decommposition(dataGridView2, chartPage2, Data, checkedListBox1, checkedListBox5, checkedListBox8);   //Проводим 1-й уровень декомпозиции 
            /*Marks();                                             *///обновляем график марок

            comboBox2.Items.Clear(); //очищаем второй уровень декомпозиции

            dataGridView3.Rows.Clear();
            dataGridView3.Columns.Clear();
            dataGridView4.Rows.Clear();
            dataGridView4.Columns.Clear();
            chart2.Series.Clear();
            checkedListBox2.Items.Clear();
            checkedListBox8.Items.Clear(); // !!!


            button5.Enabled = false; //Блоки еще не распределены, поэтому отключаем эти элементы
            label5.Enabled = false;
            comboBox2.Enabled = false;
            label5.Text = "";

            Stabilnost_inf();
        }

        //Функция выводит в label12 сообщение о стабильности объекта

        private void Stabilnost_inf()
        {
            for (int i = 0; i < dataGridView2.RowCount; i++) //Проверяем есть ли нестабильные эпохи в объекте и выводим сообщение для пользователя
            {
                if (dataGridView2.Rows[i].Cells[dataGridView2.ColumnCount - 1].Value.ToString() == "-")
                {
                    label2.Text = "Объект нестабилен\nТребуется декомпозиция";
                    label2.ForeColor = Color.Red;
                    break;
                }
                else
                {
                    label2.Text = "Объект стабилен\nДекомпозиция не требуется";
                    label2.ForeColor = Color.Green;
                }
            }
        }

        //Кнопка "Распределить по подблокам"
        private void button6_Click(object sender, EventArgs e)
        {
            //проверка правильности ввода количества подблоков
            if (textBox4.Text == "" || Convert.ToInt32(textBox4.Text) > (int)(this.blocks.Rows.Count / 2) || Convert.ToInt32(textBox4.Text) < 2)
            {
                MessageBox.Show($"Неверное количество подблоков. Введите значение от 2 до {(int)(this.blocks.Rows.Count / 2)}.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //создание окна распределения блоков (аргументы: картинка, количество подблоков, количество марок на блоке, имя выбранного блока, таблица превышений, погрешность E, столбец с марками блока
            SubBlocks sub_blocks = new SubBlocks(pictureBox1.Image, Convert.ToInt32(textBox4.Text), this.blocks.Rows.Count, comboBox3.SelectedItem.ToString(), Prev, E, this.blocks.Columns[comboBox3.SelectedIndex]);

            //Если марки распределены по подблокам
            if (sub_blocks.ShowDialog() == DialogResult.OK)
            {
                this.sub_blocks = sub_blocks.GetBTable();
                comboBox4.Items.Clear();
                dataGridView5.Rows.Clear();
                dataGridView5.Columns.Clear();
                dataGridView6.Rows.Clear();
                dataGridView6.Columns.Clear();

                for (int i = 1; i <= this.sub_blocks.Columns.Count; i++)
                {
                    comboBox4.Items.Add(comboBox3.SelectedItem.ToString() + i);
                }

                label6.Text = "";
                comboBox4.SelectedIndex = 0;
                button9.Enabled = true;             //Включаем элементы выбора подблоков
                label8.Enabled = true;
                comboBox4.Enabled = true;
                button7.Enabled = true;

            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataTable subBlock = new DataTable();
            for (int i = 0; i < Data.Rows.Count; i++) //добавляем строки
            {
                subBlock.Rows.Add();
            }

            subBlock.Columns.Add("Эпоха");
            for (int i = 0; i < sub_blocks.Rows.Count; i++) //добавляем столбцы
            {
                subBlock.Columns.Add(Convert.ToString(sub_blocks.Rows[i][comboBox4.SelectedIndex]));
            }

            for (int i = 0; i < subBlock.Rows.Count; i++) //заполняем таблицу
                for (int j = 0; j < subBlock.Columns.Count; j++)
                {
                    if (j == 0) subBlock.Rows[i][j] = i;
                    else subBlock.Rows[i][j] = Data.Rows[i][Convert.ToInt32(sub_blocks.Rows[j - 1][comboBox4.SelectedIndex])];
                }

            OutPutTable(subBlock, dataGridView5);
            Decommposition(dataGridView6, chartPage4, subBlock, checkedListBox4, checkedListBox7, checkedListBox10);

            for (int i = 0; i < dataGridView6.RowCount; i++)//Проверяем есть ли нестабильные эпохи и выводим сообщение для пользователя
            {
                if (dataGridView6.Rows[i].Cells[dataGridView6.ColumnCount - 1].Value.ToString() == "-")
                {
                    label6.Text = "Подблок нестабилен, требуется декомпозиция.";
                    label6.ForeColor = Color.Red;
                    break;
                }
                else
                {
                    label6.Text = "Подблок стабилен, декомпозиция не требуется.";
                    label6.ForeColor = Color.Green;
                }
            }
        }

        //Выбор блока на вкладке подблоки
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkedListBox4.Items.Clear();    //очищаем декомпозицию подблоков после новой расстановки по блокам
            chart4.Series.Clear();
            dataGridView5.Rows.Clear();
            dataGridView5.Columns.Clear();
            dataGridView6.Rows.Clear();
            dataGridView6.Columns.Clear();
            comboBox4.Items.Clear();
            label6.Text = "";
            button9.Enabled = false;
            comboBox4.Enabled = false;
            label8.Enabled = false;

            //Заполнение таблицы превышений
            DataTable prev = new DataTable();
            for (int i = 0; i < Data.Rows.Count; i++)       //добавление строк = кол-во эпох
                prev.Rows.Add();
            prev.Columns.Add("Эпоха");                      //добавление столбца эпохи
            for (int i = 0; i < prev.Rows.Count; i++)       //заполнение столбца эпохи
                prev.Rows[i][0] = i;
            int stolb = 0;                                    //счетчик столбцов
            for (int i = 0; i < blocks.Rows.Count - 1; i++)  //для каждой кроме последней марки в блоке
                for (int j = i + 1; j < blocks.Rows.Count; j++)//и каждой возможно свзанной с ней маркой
                {
                    prev.Columns.Add($"{blocks.Rows[i][comboBox3.SelectedIndex]}-{blocks.Rows[j][comboBox3.SelectedIndex]}");   //создать соотв. колонку
                    stolb++;
                    for (int k = 0; k < prev.Rows.Count; k++)                                                                    //заполнение созданного столбца
                    {
                        double value = Math.Abs(Convert.ToDouble(Data.Rows[k][Convert.ToInt32(blocks.Rows[i][comboBox3.SelectedIndex])]) - Convert.ToDouble(Data.Rows[k][Convert.ToInt32(blocks.Rows[j][comboBox3.SelectedIndex])]));
                        prev.Rows[k][stolb] = Math.Round(value, 4);
                    }
                }
            Prev = prev.Copy();
            for (int i = 0; i < Prev.Rows.Count; i++) //вычисляем значения
            {
                for (int j = 1; j < Prev.Columns.Count; j++)
                {
                    double value = Convert.ToDouble(prev.Rows[i][j]) - Convert.ToDouble(prev.Rows[0][j]);
                    value = Math.Round(value, 4);
                    Prev.Rows[i][j] = Math.Abs(value);
                }
            }
        }

        //Кнопка "Все блоки". Создает новое окно, в котором демонстрируются графики всех блоков
        Graphics open_sub = new Graphics();

        private void button5_Click(object sender, EventArgs e)
        {
            Graphics Subs = new Graphics();
            if (!open_sub.IsFormOpen)
            {
                Subs.Show();
                open_sub = Subs;
                open_sub.IsFormOpen = true;
                open_sub.my_chart.Series.Clear();
                open_sub.checkedListBox1.Items.Clear();

                for (int index = 0; index < comboBox3.Items.Count; index++)
                {
                    DataTable subblock = new DataTable();

                    for (int i = 0; i < Data.Rows.Count; i++) //добавляет строки
                    {
                        subblock.Rows.Add();
                    }
                    subblock.Columns.Add("Эпоха");
                    for (int i = 0; i < blocks.Rows.Count; i++) //добавляет столбцы
                    {
                        subblock.Columns.Add(Convert.ToString(blocks.Rows[i][index]));
                    }

                    for (int i = 0; i < subblock.Rows.Count; i++) //заполняем таблицу
                        for (int j = 0; j < subblock.Columns.Count; j++)
                        {
                            if (j == 0) subblock.Rows[i][j] = i;
                            else subblock.Rows[i][j] = Data.Rows[i][Convert.ToInt32(blocks.Rows[j - 1][index])];
                        }
                    // !!!
                    //строим графики по маркам
                    ChartMain(open_sub.my_chart, TableData(subblock), 1, 2, $"Фазовая траектория подблок {comboBox3.Items[index]}");         //Добавляем графики
                    ChartMain(open_sub.my_chart, TableData(subblock), 5, 6, $"Верхний предел подблок {comboBox3.Items[index]}");
                    ChartMain(open_sub.my_chart, TableData(subblock), 7, 8, $"Нижний предел подблок {comboBox3.Items[index]}");
                    ChartMain(open_sub.my_chart, TableData(subblock), 0, 1, $"M(t) подблок {comboBox3.Items[index]}");
                    ChartMain(open_sub.my_chart, TableData(subblock), 0, 5, $"M+(t) подблок {comboBox3.Items[index]}");
                    ChartMain(open_sub.my_chart, TableData(subblock), 0, 7, $"M-(t) подблок {comboBox3.Items[index]}");
                }

                //Выводим легенду, изменяем вид маркеров, добавляем серии в список
                foreach (Series serie in open_sub.my_chart.Series)
                {
                    open_sub.checkedListBox1.Items.Add(serie.Name);
                }

                open_sub.checkedListBox1.SelectedIndex = 0;
                open_sub.checkedListBox1.SelectedIndex = -1;
            }
            else open_sub.BringToFront();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            // Получаем выбранное значение из comboBox1
            string tableName = comboBox1.SelectedItem.ToString();

            // Проверяем, что таблица с указанным именем существует в базе данных
            if (CheckTableExists(tableName))
            {
                // Получаем ссылку на существующую таблицу
                string selectTableQuery = $"SELECT * FROM {tableName}";
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(selectTableQuery, SQLConn);
                SQLiteCommandBuilder builder = new SQLiteCommandBuilder(adapter);
                DataTable existingTable = new DataTable();
                adapter.Fill(existingTable);

                // Импортируем данные из DataTable в существующую таблицу
                existingTable.Merge(Data);

                // Обновляем базу данных
                adapter.Update(existingTable);

                MessageBox.Show($"Таблица с именем {tableName} переписана.", "Ура");
            }
            else
            {
                // Выводим сообщение, что таблица не существует
                MessageBox.Show($"Таблица с именем {tableName} не существует в базе данных.", "Ошибка");
            }
        }

        private bool CheckTableExists(string tableName)
        {
            string checkTableQuery = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            SQLiteCommand command = new SQLiteCommand(checkTableQuery, SQLConn);
            object result = command.ExecuteScalar();
            return (result != null);
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void checkedListBox4_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}