using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Kurs1
{
    public partial class Graphics : Form
    {
        public Graphics()
        {
            InitializeComponent();
        }

        public Chart my_chart = new Chart();
        public bool IsFormOpen = false;

        private void Graphics_Load(object sender, EventArgs e)
        {

        }

        private void AllGraphs_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.IsFormOpen = false;
        }

        private void AllGraphs_Load(object sender, EventArgs e)
        {
            this.OnResize(null);                    //Изменяем размер объектов при загрузке формы
        }

        private void AllGraphs_Resize(object sender, EventArgs e)
        {
            if ((int)this.ClientSize.Width != 0)
            {
                checkedListBox1.Left = 5;
                checkedListBox1.Top = 40;
                checkedListBox1.Height = (int)this.ClientSize.Height - 50;

                chart1.Left = checkedListBox1.Width + 30;
                chart1.Top = 5;
                chart1.Width = (int)this.ClientSize.Width - checkedListBox1.Width - 30;
                chart1.Height = (int)this.ClientSize.Height;
            }
        }

        private void checkedListBox1_MouseUp(object sender, MouseEventArgs e)
        {
            chart1.Series.Clear();

            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                if (checkedListBox1.GetItemChecked(i))
                {
                    chart1.Series.Add(checkedListBox1.Items[i].ToString());
                    foreach (DataPoint point in my_chart.Series[checkedListBox1.Items[i].ToString()].Points)
                    {
                        chart1.Series[checkedListBox1.Items[i].ToString()].Points.Add(point);
                    }
                }
            }

            foreach (Series serie in chart1.Series)
            {
                serie.ChartType = SeriesChartType.Spline;
                chart1.Series[serie.Name].MarkerBorderColor = System.Drawing.Color.Black;
                chart1.Series[serie.Name].MarkerStyle = MarkerStyle.Circle;
                chart1.Series[serie.Name].MarkerSize = 6;
                chart1.ChartAreas[0].AxisX.IsStartedFromZero = false;
                chart1.ChartAreas[0].AxisY.IsStartedFromZero = false;
            }

            if (checkedListBox1.CheckedItems.Count == 0)
            {
                chart1.ChartAreas[0].AxisY.Enabled = AxisEnabled.True;
                chart1.ChartAreas[0].AxisX.Enabled = AxisEnabled.True;
            }
        }
    }
}
