using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using Preh;
using System.Xml.Linq;

namespace frmTest
{
    public partial class Form1 : Form
    {
        Engine MainEngineTest;
        IOCycle NewIO;


        public Form1()
        {
            
            InitializeComponent();
           
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string test="";
            MainEngineTest = new Engine("INIT.xml");
            textBox1.Text = test;
            textBox1.Text += "WorkCenter ID is " + MainEngineTest.WSID.ToString() + "\r\n";
            textBox1.Text += "Languague is " + MainEngineTest.ActualLang.ToString() + "\r\n";
            //textBox1.Text += "Station Name is " + MainEngineTest.StationName + "\r\n";
            foreach(Preh.Scanner show in MainEngineTest.Scanners)
            {
                if (show.BaudRate > 0)
                    textBox1.Text += "Scanner in " + show.ComPort + " with BR " + show.BaudRate.ToString() + "\r\n";
                else
                    textBox1.Text += "Scanner in " + show.ipAddr + "\r\n";
            }
            dataGridView1.DataSource = MainEngineTest.MainIO.Dt_DI;
            dataGridView2.DataSource = MainEngineTest.MainIO.Dt_DO;
            dataGridView3.DataSource = MainEngineTest.MainIO.Dt_AI;
            dataGridView4.DataSource = MainEngineTest.MainIO.Dt_AO;

            MainEngineTest.PrepareDevices();
            textBox1.Text += test+"\r\n";

           
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
           
            UpdateAnalogIO();
            

        }

        private void button4_Click(object sender, EventArgs e)
        {

            NewIO = new IOCycle("192.168.28.11");
            NewIO.MyBK.ConnectToServer();
            Thread.Sleep(200);
            textBox1.Text += "BK Connected = " + NewIO.BKConnected;


            var xmlPath = AppDomain.CurrentDomain.BaseDirectory + @"XML Files\" + "INIT.xml";
            var ConfigFile = XDocument.Load(xmlPath);
            var root = ConfigFile.Root;
            var elIOs = root.Elements("IOs").Elements("IO");

            var IOlist = from IO in elIOs
                         select new
                         {
                             Name = IO.Element("IOName").Value,
                             TypeIO = IO.Element("IOType").Value,
                             Address = IO.Element("IOAddress").Value
                         };

            foreach (var IO in IOlist)
            {
                switch (IO.TypeIO)
                {
                    case "DI":
                        NewIO.Dt_DI.Rows.Add(new object[] { IO.Name, IO.Address, 0 });
                        break;
                    case "DO":
                        NewIO.Dt_DO.Rows.Add(new object[] { IO.Name, IO.Address, 0, false });
                        break;
                    case "AI":
                        NewIO.Dt_AI.Rows.Add(new object[] { IO.Name, IO.Address, 0 });
                        break;
                    case "AO":
                        NewIO.Dt_AO.Rows.Add(new object[] { IO.Name, IO.Address, 0});
                        break;
                }
            }


            AddListAIItems();
            AddListAOItems();

        }

        private void UpdateAnalogIO()
        {
            try
            {
                int Max = Math.Max(Math.Max(NewIO.Dt_AI.Rows.Count, NewIO.Dt_DI.Rows.Count), Math.Max(NewIO.Dt_DI.Rows.Count, NewIO.Dt_DO.Rows.Count));

                for (int j = 0; j < Max; j++)
                {
                    if (j < NewIO.Dt_AI.Rows.Count)
                    {
                        if (j < 2)
                            lvAI.Items[j].SubItems[2].Text = NewIO.ReadDAI(j).ToString();
                        else
                            lvAI.Items[j].SubItems[2].Text = NewIO.ReadDAI(j).ToString();
                    }

                    if (j < NewIO.Dt_AO.Rows.Count)
                    {
                        if (j < 2)
                            lvAO.Items[j].SubItems[2].Text = NewIO.ReadAO(j).ToString();
                        else
                            lvAO.Items[j].SubItems[2].Text = NewIO.ReadAO(j).ToString();
                    }
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.ToString());
                
            }
        }
        private void AddListAIItems()
        {
            string[] strItems = new string[3];

           // if (NewIO.Dt_AI.Rows.Count == 0) tabControlIO.TabPages.Remove(tabAIO);

            for (int j = 0; j < NewIO.Dt_AI.Rows.Count; j++)
            {
                //Array com o nome e endereço para acrescentar os items da lista
                strItems[0] = NewIO.Dt_AI.Rows[j]["Address"].ToString();
                strItems[1] = NewIO.Dt_AI.Rows[j]["AIName"].ToString();
                strItems[2] = NewIO.Dt_AI.Rows[j]["Value"].ToString();
                //Inicializar um objecto com os items de uma linha da lista
                ListViewItem lvItems = new ListViewItem(strItems);
                lvItems.UseItemStyleForSubItems = false;

                //Colocar uma cor diferente para o nome das reservas
                if (NewIO.Dt_AI.Rows[j]["AIName"].ToString().Contains("Reserve")) lvItems.SubItems[1].ForeColor = Color.Red;

                //Acrescentar os items à lista
                lvAI.Items.Add(lvItems);
            }
            lvAI.Columns[0].Width = 40;
            lvAI.Columns[1].Width = 245;
            lvAI.Columns[2].Width = 70;
        }
        private void AddListAOItems()
        {
            string[] strItems = new string[3];

            //if (NewIO.Dt_AI.Rows.Count == 0) tabControlIO.TabPages.Remove(tabAIO);

            for (int j = 0; j < NewIO.Dt_AO.Rows.Count; j++)
            {
                //Array com o nome e endereço para acrescentar os items da lista
                strItems[0] = NewIO.Dt_AO.Rows[j]["Address"].ToString();
                strItems[1] = NewIO.Dt_AO.Rows[j]["AOName"].ToString();
                strItems[2] = NewIO.Dt_AO.Rows[j]["Value"].ToString();
                //Inicializar um objecto com os items de uma linha da lista
                ListViewItem lvItems = new ListViewItem(strItems);
                lvItems.UseItemStyleForSubItems = false;

                //Colocar uma cor diferente para o nome das reservas
                if (NewIO.Dt_AO.Rows[j]["AOName"].ToString().Contains("Reserve")) lvItems.SubItems[1].ForeColor = Color.Red;

                //Acrescentar os items à lista
                lvAO.Items.Add(lvItems);
            }
            lvAO.Columns[0].Width = 40;
            lvAO.Columns[1].Width = 245;
            lvAO.Columns[2].Width = 70;
        }


        

        private void button3_Click(object sender, EventArgs e)
        {


            var ao=0.0;
            double.TryParse(textBoxAO.Text, out ao);
            NewIO.WriteAO((int)NewIO.Dt_AO.Rows[0]["Address"], ao);
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            NewIO.MyBK.CloseSocket();
            Application.Exit();
        }
    }
}
