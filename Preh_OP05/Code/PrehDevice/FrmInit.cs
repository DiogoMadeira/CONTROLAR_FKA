using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;


namespace Preh {
    public partial class FrmInit : Form {
        private readonly DataSet _ds = GetDs();

        private static DataSet GetDs() {
            return new DataSet();
        }

        public FrmInit() {
            InitializeComponent();
            try {
                LoadXML();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString());
            }
            finally {
                _ds.Dispose();
            }
        }


        private void button1_Click_1(object sender, EventArgs e) {
            try {
                _ds.Tables[0].WriteXml("Init.xml", XmlWriteMode.WriteSchema);
                SaveButton.BackColor = Color.Green;
            }
            catch (Exception ex) {
                SaveButton.BackColor = Color.Red;
                MessageBox.Show(ex.ToString());
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            _ds.Dispose();
        }

        private void LoadXML() {
            var xmlFile = XmlReader.Create("Init.xml", new XmlReaderSettings());
            _ds.ReadXml(xmlFile);
            dataGridView1.DataSource = _ds.Tables[0];
            xmlFile.Close();
        }

        private bool CheckDbConnection() {
            try {
                var DBServer = _ds.Tables[0].Rows[0]["DB_SERVER"].ToString();
                var DBName = _ds.Tables[0].Rows[0]["DB_CATALOG"].ToString();
                var DBUser = _ds.Tables[0].Rows[0]["DB_USER"].ToString();
                var DBUserPsw = _ds.Tables[0].Rows[0]["DB_PSW"].ToString();
                using (var conn = new SqlConnection("Data Source=" + DBServer + ";Initial Catalog=" + DBName + ";Persist Security Info=True" + ";User ID=" + DBUser + ";Password=" + DBUserPsw)) {
                    conn.Open();
                    ConnTesterButton.BackColor = Color.Green;
                    conn.Dispose();
                    return true;
                }
            }
            catch (Exception){
                ConnTesterButton.BackColor = Color.Red;
                return false; // any error is considered as db connection error for now
            }
        }

        private static bool CheckBKConnection() {
            return true;
        }

        private void button2_Click(object sender, EventArgs e) {
            CheckDbConnection();
        }

        private void button3_Click_1(object sender, EventArgs e) {
            this.Dispose();
            this.Close();
        }

        private void buttonBKConn_Click(object sender, EventArgs e) {
            CheckBKConnection();
        }
    }
}
