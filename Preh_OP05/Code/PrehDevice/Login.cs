using PPDBAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Preh {
    public partial class Login : Form {
        public bool GetLoggedIn { get; private set; } = false;
        public Engine.DataSource DataSource { get; set; }
        public AppUser CurrentUser { get; set; }

        //Database Connection
        public SqlConnectionStringBuilder SbDefaultConnString { get; set; }

        //adicionar um delegate
        public delegate void IdentityUpdateHandler(object sender, IdentityUpdateEventArgs e);

        //adicionar um event do tipo delegate
        public event IdentityUpdateHandler IdentityUpdated;

        private readonly PPTraceStation _myDb;
        private List<AppUser> _usersList;
        //private readonly DataSet _dsLanguage;
        //private readonly Language _language;

 
        public Login(PPTraceStation appDb, Engine.DataSource dataSource) {
            InitializeComponent();  
            //_dsLanguage = language;
            //_language = lang;
            //_dsUser = users;
            DataSource = dataSource;
            _myDb = appDb;

        }

        private void Login_Load(object sender, EventArgs e) {
            //WinAPI.AnimateWindow(this.Handle, 1500, WinAPI.AW_ACTIVATE | WinAPI.AW_CENTER);

            cboUsers.Focus();
            _usersList = new List<AppUser>();
            cboUsers.Focus();
            try {
                if (DataSource == Engine.DataSource.XML)
                {
                    var usersXml = XDocument.Load("XML Files\\Users.xml");

                    var root = usersXml.Root;
                    var users = root.Elements("TableUsers");
                    var usersXmlList = from u in users
                                    select new
                                     {
                                        ID_User = u.Element("ID_User").Value,    
                                        Identification = u.Element("Identification").Value,           
                                        Username = u.Element("Username").Value,   
                                        Psw = u.Element("Psw").Value,             
                                        AccessMask = u.Element("AccessMask").Value,
                                     };

                    foreach (var use in usersXmlList)
                    {
                        var user = new AppUser
                        {
                            ID_User = Convert.ToInt16(use.ID_User),
                            Identification = use.Identification,
                            UserName = use.Username,
                            Psw = use.Psw,
                            AccessMask = Convert.ToInt16(use.AccessMask)
                        };

                        _usersList.Add(user);
                    }

                }
                else if (DataSource == Engine.DataSource.SQL)
                {
                    _usersList = _myDb.AppUsers;
                }

                


                _usersList =
                        _usersList.Concat(new[] { new AppUser { Identification = "Select User", ID_User = 0, UserName = " " } }).
                        OrderBy(item => item.ID_User).ToList();
                cboUsers.DataSource = _usersList;
               cboUsers.DisplayMember = "Identification";
                cboUsers.ValueMember = "ID_User";

                this.FormBorderStyle = FormBorderStyle.FixedSingle;

                UpdateFormLanguage();
            }
            catch (Exception exp) {

                MessageBox.Show(PrintGenericText("PLEASE CHECK YOUR NETWORK CONNECTION!||An error ocurred when getting the Users list. The program will now exit.||Details:") + Environment.NewLine + exp.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void UpdateFormLanguage() {

            groupBoxAuth.Text = PrintGenericText("Authentication");
            UserId.Text = PrintGenericText("User ID");
            buttonLogin.Text = PrintGenericText("Login");
            btn_Clear.Text = PrintGenericText("Clear");
            buttonSair.Text = PrintGenericText("Exit");
        }

        private void numericButtons_Click(object sender, EventArgs e)
        {
            var senderNumber = (Control)sender;

            //Acrescentar o numero correspondente
            textBoxPassword.Text += senderNumber.Text;
        }

        private void btn_Clear_Click(object sender, EventArgs e) {
            textBoxPassword.Text = "";
        }

        //LOGIN
        private void buttonLogin_Click(object sender, EventArgs e) {

            if (cboUsers.SelectedIndex == 0)
            {
                MessageBox.Show(PrintGenericText("Please Select a User!"), nameof(Login), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (textBoxPassword.Text.Length != 4)
            {
                MessageBox.Show(PrintGenericText("The Password only requires 4 Digits!"), nameof(Login), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (textBoxPassword.Text.Length == 4)
            {

                CurrentUser = _usersList.Find(u => u.Identification == cboUsers.Text);

                if (CurrentUser!=null)
                {

                    if (DataSource == Engine.DataSource.SQL)
                    {
                        if (_myDb.Users_CheckUserPassword(CurrentUser.ID_User, textBoxPassword.Text))
                        {
                            UpdateLoginUser();
                        }
                        else
                        {
                            MessageBox.Show(PrintGenericText("Wrong Password!"), nameof(Login), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            textBoxPassword.Clear();
                        }
                    }
                    else
                    {
                        if (CurrentUser.Psw == textBoxPassword.Text)
                        {
                            UpdateLoginUser();
                        }
                        else
                        {
                            MessageBox.Show(PrintGenericText("Wrong Password!"), nameof(Login), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            textBoxPassword.Clear();
                        }
                    }

                    
                }

                
            }
           
        }

        //TEXTBOX PASSWORD SELECCIONADA POR ULTIMO
       

        private void UpdateLoginUser() {
            

            var args = new IdentityUpdateEventArgs(CurrentUser.ID_User, CurrentUser.Identification, CurrentUser.UserName, CurrentUser.AccessMask);

            IdentityUpdated?.Invoke(this, args);
            GetLoggedIn = true;
            this.Dispose();
        }

        private void cboUsers_SelectedIndexChanged(object sender, EventArgs e) {
            try {

                var user = _usersList[cboUsers.SelectedIndex];
                lblNome.Text = user.UserName;
                buttonLogin.Enabled = true;
                textBoxPassword.Focus();
            }
            catch (Exception ex) {
                MessageBox.Show(PrintGenericText("Error in selection of the operator. The program will ending!"), nameof(Login) + ex.InnerException, MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Dispose();
                GC.Collect();
                Application.ExitThread();
                Application.Exit();
            }
        }

        private void buttonSair_Click(object sender, EventArgs e) {
            if (MessageBox.Show(PrintGenericText("Are you sure that you want to Exit?"), PrintGenericText("Exit"), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                try {
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null) throw ex.InnerException;
                }
        }
        public string PrintGenericText(string text)
        {
            if (DataSource == Engine.DataSource.SQL)
            {
                return _myDb.Languages_GetGenericMessage(text);
            }
            else
            {
                return text;
            }
        }

        
    }

    public class IdentityUpdateEventArgs : System.EventArgs {
        //add local member variable to hold text

        //private string mPsw;

        //constructor da classe
        public IdentityUpdateEventArgs(int sIdUser, string sIdentification, string sUsername, int sUserLevel) {
            this.IdUser = sIdUser;
            this.Identification = sIdentification;
            this.Username = sUsername;
            this.UserLevel = sUserLevel;
        }

        //Properties - Accessible by the listener
        public int IdUser { get; }

        public string Identification { get; }

        public string Username { get; }

        public int UserLevel { get; }
    }
}