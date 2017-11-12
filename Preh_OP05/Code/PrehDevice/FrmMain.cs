using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Preh;
using Preh.Properties;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Xml;
using PPDBAccess;

namespace Preh
{
    public partial class frmMain : Form
    {
        List<DeviceConst> listaConstantes;
        List<MeasureLimit> listaParametros;
        private static readonly List<string> _imageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
        private Engine MainEngine;
        //public DataSet DGVDSet = new DataSet("DGVDSet");//add DGVDSet.Dispose(); to the Dispose method on another file.

        private bool _stopTimer = false;
        private bool _firstAddList = true;

        private int defaultPdwidth;
        private int defaultPdHeight;
        private Size defaultRbSize;
       

        Dictionary<int, StringBuilder> _instructionsBuilder;
        Dictionary<int, TextBox> _instructionsByCycle;
        private Dictionary<CheckBox, EngineData.DO[]> _checkboxEnums;
        private Dictionary<Label, EngineData.DI> _labelEnums;
        private List<string> _activeAlarms;
        private List<string> _acknoladgedAlarms;


        private CheckBox[] _cbOutput = { };

        private Dictionary<PictureBox,Point> _resultBoxPos;
        private Dictionary<EngineData.DI,PictureBox> _diBoxs;
        private Dictionary<EngineData.AI,PictureBox> _aiBoxs;

        #region Form
        public frmMain()
        {
            InitializeComponent();
            MainEngine = new Engine("INIT.xml");
            _checkboxEnums = new Dictionary<CheckBox, EngineData.DO[]>();
            _labelEnums = new Dictionary<Label, EngineData.DI>();
            _activeAlarms = new List<string>();
            _acknoladgedAlarms = new List<string>();
            _resultBoxPos = new Dictionary<PictureBox, Point>();
            _diBoxs = new Dictionary<EngineData.DI, PictureBox>();
            _aiBoxs = new Dictionary<EngineData.AI, PictureBox>();

        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            if (Screen.PrimaryScreen.Bounds.Width <= 900 && Screen.PrimaryScreen.Bounds.Height<=700)
            {
                this.MaximumSize = new Size(808, 607);
                
            }

            if (MainEngine.LoadXMLError)
            {
                MessageBox.Show("Init.xml", MainEngine.PrintGenericText("Error Loading file Init.xml, please make sure the file is correctly configures!"));
                CloseApplication();
            }

            #region Event Registry
            FormInterface.StepChanged += UpdateStepBox;
            FormInterface.NewInstructionTextByText += UpdatetxtInstructionsByText;
            //FormInterface.NewInstructionTextByEnum += FormInterface_NewInstructionTextByEnum;
            FormInterface.EndOfInstructionText += WritetxtInstructions;
            FormInterface.UpdateColorInstructionTextBox += UpdatelblPicturePanel;
            FormInterface.ClearInstructionText += FormInterface_ClearInstructionText;
            FormInterface.ShowPanel += FormInterface_ShowPanel;
            FormInterface.ChangeRef += FormInterface_ChangeRef;
            FormInterface.UpdateBoaState += FormInterface_UpdateBoaState;
            FormInterface.GetTraceErrorDescription += FormInterface_GetTraceErrorDescription;
            FormInterface.CountParts += FormInterface_CountParts;
            IOCycle.BKConnectionError += IOCycle_BKConnectionError;
            Engine.EngineError += Engine_EngineError;
            Cycle.NewInternalErrorText += Cycle_NewInternalErrorText;
            FormInterface.CycleTypeChanged += Cycle_CycleTypeChanged;
            Cycle.ShowPotiPicture += Cycle_ShowPotiPicture;
            #endregion

            if (MainEngine.MainIO != null)
            {
                MainEngine.MainIO.Dt_AO.RowChanged += Dt_AO_RowChanged;
            }

            lvAO.RowEnter += lvAO_RowEnter;

            GenerateLabelArray();
            GenerateCheckBoxArray();

            //Disable the check cross threads
            CheckForIllegalCrossThreadCalls = false;

            //Create Scanners Maintenance controls 
            if (MainEngine.Scanners.Count > 0)
            {
                dgvScannersMaintenanceConfig();

                foreach (var scannerDevice in MainEngine.Scanners)
                {
                    dgvScannersMaintenanceAddScanner(scannerDevice);
                    scannerDevice.newDataReceived += ScannerEventReceived;
                }
            }
            else tabctrlMaintenance.Controls.Remove(tabScanners);

            if (MainEngine.RFIDs.Count > 0)
            {
                var counter = 0;

                foreach (var rfidDevice in MainEngine.RFIDs)
                {
                    comboBox_RFID.Items.Insert(counter, rfidDevice.ReaderName);
                    counter++;
                }
            }
            else tabctrlMaintenance.Controls.Remove(tabNordicRFID);

            if (MainEngine.IAIs.Count > 0)
            {
                dgvIAIsMaintenanceConfig();

                foreach (IAIModbusASCII device in MainEngine.IAIs)
                {
                    foreach (var axis in device.Axis)
                    {
                        dgvIAIsMaintenanceAddIAI(axis.Value);
                    }
                }
            }
            else tabctrlMaintenance.Controls.Remove(tabIAIs);


            //Update Language Label
            UpdateFormLanguageFlag();

            //Load Const.
            if (!MainEngine.dbLoadConsts())
            {
                PrintAndLogError("Error Loading Constants!");
                CloseApplication();
                return;
            }


            //Prepare all devices
            if (!MainEngine.PrepareDevices())
            {
                CloseApplication(); return;
            }

            timerGeneric.Start();

            #region References Configuration
            
                //Automatic selection of reference
                if (MainEngine.HasAutoRef)
                {
                    comboBoxModelo.Visible = false;
                    label_Refs.Visible = true;
                }
                else
                {   //Manual selection of reference
                    comboBoxModelo.Visible = true;
                    label_Refs.Visible = false;
                }
                //Load references:
                if (!MainEngine.dbLoadReferences())
                {
                    PrintAndLogError("Error Loading References!");
                    CloseApplication();
                    return;
                }
                try
                {
                    //Adds "-- Select Reference --" string to References Combobox
                    MainEngine.RefsList = MainEngine.RefsList.
                        Concat(new[] { new Reference { RefPreh = "-- Select Reference --", IDRef = 0 } }).
                        OrderBy(item => item.IDRef).ToList();

                    comboBoxModelo.DataSource = MainEngine.RefsList;


                    comboBoxModelo.DisplayMember = "RefPreh";
                    comboBoxModelo.ValueMember = "IDRef";
                }
                catch (NullReferenceException ex)
                {
                    Log.Instance.Error(ex.Message, ex);
                }

            
            #endregion References Configuration

            //Load Parameters for Reference
            if (!MainEngine.dbLoadParameters())
            {
                PrintAndLogError("Error Loading Parameters!");
                CloseApplication(); return;
            }

            //Login:
            if (MainEngine.DBConnection == Engine.DataSource.SQL)
            {
                //Gets all users from Db
                if (!MainEngine.MyDB.Users_GetAppActiveUsers(MainEngine.SbDefaultConnString.ApplicationName))
                {
                    PrintAndLogError("Error Loading Users!");
                    CloseApplication(); return;
                }
            }

            //Runs Login Form
            if (!LogUserIn())
            {
                PrintAndLogError("Error Logging In Users!");
                CloseApplication();
                return;
            }

            //Loads Machine Consts
            UpdateCurrentMachineConsts();


            //Loads menssages from the language chosen
            if (!UpdateFormLanguage()) { CloseApplication(); return; }


            //Turn on emergency relé
            MainEngine.MainIO.WriteDO(EngineData.DO.Safety_Relay_On, true);


            this.WindowState = FormWindowState.Maximized;

            CreateTxtInstructionBoxes();

            var stationTitle = "";
            if (MainEngine.DBConnection == Engine.DataSource.SQL)
            {
                stationTitle = MainEngine.StationName + " vPPDb" + MainEngine.MyDB.Version;
            }
            else
            {
                stationTitle = MainEngine.StationName + " v" + "XML";
            }

            labelStationTitle.Text = MainEngine.PrintGenericText(stationTitle);

            if (MainEngine.Calibration)
            {
                lbl_CalibrationInfo.Visible = true;
                txtcalibrationDays.Visible = true;

                if (MainEngine.DBConnection == Engine.DataSource.SQL)
                {
                    if (!MainEngine.LoadAllCalibrationOffsets())
                    {
                        MessageBox.Show(MainEngine.PrintGenericText("Error Loading Calibers Offsets"), MainEngine.PrintGenericText("Warning!"), MessageBoxButtons.OK, MessageBoxIcon.Warning); //"Error reading all calibration offsets"
                        CloseApplication();
                    }


                    CheckIfCalibrationIsNeeded(); //Get the calibration Status
                }

            }
            else
            {
                lbl_CalibrationInfo.Visible = false;
                txtcalibrationDays.Visible = false;
            }

            if (MainEngine.HasCycleTypes)
            {
                txtCycleType.Visible = true;

            }
            else
            {
                txtCycleType.Visible = false;
            }



            if (MainEngine.HasVacuum)
            {
                btnVacuumCleaner.Visible = true;
            }
            else
            {
                btnVacuumCleaner.Visible = false;
            }

            #region Load Cliente Logo

            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "ClientLogo"))
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "ClientLogo");

            }

            var images = Directory.GetFiles("ClientLogo");

            if (images.Length > 1)
            {
                foreach (var i in images)
                {
                    if (_imageExtensions.Contains(Path.GetExtension(i).ToUpperInvariant()))
                    {
                        var logo = Image.FromFile(i);
                        if (logo != null)
                        {
                            pictureBoxClientLogo.Image = logo;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show(MainEngine.PrintGenericText("Client Logo image file missing on 'ClientLogo' Folder on ") + AppDomain.CurrentDomain.BaseDirectory + "ClientLogo", "Client Logo");
            }

            if (pictureBoxClientLogo.Image == null && images.Length > 1)
            {
                MessageBox.Show(MainEngine.PrintGenericText("Missing a Suitable image file type on ") + AppDomain.CurrentDomain.BaseDirectory + "ClientLogo", "Client Logo");
            }

            #endregion

            WriteLastErrorAlarmBar();

            foreach (var item in panelDetections.Controls)
            {
                if (item is PictureBox)
                {
                    var picb = (PictureBox)item;

                    if (picb.Tag != null)
                    {
                        var splited = Regex.Split(picb.Tag.ToString(), @"^((D|A)I)\.(\w+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        if (splited.Length > 1)
                        {
                            if (splited[1] == "AI")
                            {
                                var enumSplit = new object();
                                try
                                {
                                    enumSplit = Enum.Parse(typeof(EngineData.AI), splited[3]);
                                }
                                catch (Exception ex)
                                {
                                    if (ex is ArgumentException)
                                    {
                                        enumSplit = null;
                                        MessageBox.Show("The enum "+ splited[3] + " on PictureBox " + picb.Name + " does not exist!", "PictureBox Results");
                                    }
                                    
                                }
    
                                if (enumSplit != null)
                                {
                                    _aiBoxs.Add((EngineData.AI)enumSplit, picb);
                                }
                            }
                            if (splited[1] == "DI")
                            {
                                var enumSplit = new object();
                                try
                                {
                                    enumSplit = Enum.Parse(typeof(EngineData.DI), splited[3]);
                                }
                                catch (Exception ex)
                                {

                                    if (ex is ArgumentException)
                                    {
                                        enumSplit = null;
                                        MessageBox.Show("The enum " + splited[3] + " on PictureBox " + picb.Name + " does not exist!", "PictureBox Results");
                                    }
                                }
                                
                                if (enumSplit != null)
                                {
                                    _diBoxs.Add((EngineData.DI)enumSplit,picb);
                                }
                            }
                        }
                       
                    }

                }
            }

        }

        private void Cycle_ShowPotiPicture(int arg1, Dictionary<EngineData.AI, int> analogResults, Dictionary<EngineData.DI, int> digiResults, string resourceName)
        {

            if (analogResults!=null)
            {
                if (_aiBoxs.Count != analogResults.Count())
                {
                    MessageBox.Show(MainEngine.PrintGenericText("Picturebox AI's number doesn't match the machine Result number!"), "Warning!");

                    foreach (var item in _aiBoxs.Values)
                    {
                        item.Image = null;
                    }
                }
                if (resourceName != null)
                {
                    var resource = (Bitmap)Resources.ResourceManager.GetObject(resourceName);
                    picInformation.Image = resource;
                }

                if (_aiBoxs.Count >= analogResults.Count())
                {
                    var index = 0;

                    foreach (var result in analogResults.Keys)
                    {
                        if (analogResults[result] == 0)
                        {
                            _aiBoxs[result].Image = Resources.OK;
                        }
                        else if (analogResults[result] == 1)
                        {
                            _aiBoxs[result].Image = Resources.NOK;
                        }
                        else
                        {
                            _aiBoxs[result].Image = Resources.NOK;
                        }
                        index++;
                    }
                }
            }

            if (digiResults != null)
            {
                if (_diBoxs.Count != digiResults.Count())
                {
                    MessageBox.Show(MainEngine.PrintGenericText("Picturebox DI's number doesn't match the machine Result number!"), "Warning!");

                    foreach (var item in _diBoxs.Values)
                    {
                        item.Image = null;
                    }
                }
                if (resourceName != null)
                {
                    var resource = (Bitmap)Resources.ResourceManager.GetObject(resourceName);
                    picInformation.Image = resource;
                }

                if (_diBoxs.Count >= digiResults.Count())
                {
                    var index = 0;

                    foreach (var result in digiResults.Keys)
                    {
                        if (digiResults[result] == 0)
                        {
                            _diBoxs[result].Image = Resources.OK;
                        }
                        else if (digiResults[result] == 1)
                        {
                            _diBoxs[result].Image = Resources.NOK;
                        }
                        else
                        {
                            _diBoxs[result].Image = Resources.NOK;
                        }
                        index++;
                    }
                }
            }


            
        }

        private void FormInterface_CountParts(int obj)
        {
            toolStripCountParts.Text = obj.ToString();
        }

        #region Cycle Type
        private void Cycle_CycleTypeChanged(EngineData.TypeOfCycle type)
        {
            if (type == EngineData.TypeOfCycle.FirstCycle)
            {
                txtCycleType.Text = MainEngine.PrintGenericText("First Cycle");
            }

            if (type == EngineData.TypeOfCycle.NormalCycle)
            {
                txtCycleType.Text = MainEngine.PrintGenericText("Normal Cycle");
            }

            if (type == EngineData.TypeOfCycle.LastCycle)
            {
                txtCycleType.Text = MainEngine.PrintGenericText("Last Cycle");
            }

        }
        #endregion Cycle Type
        private void Cycle_NewInternalErrorText(int cycleId, string errorText)
        {
            if (MainEngine.Screen == EngineData.Screens.Base)
            {
                this.BeginInvoke((Action)(() =>
                {
                    _instructionsByCycle[cycleId].Text = errorText;
                }));
            }
        }

        private static void Engine_EngineError(string functionError, string exception)
        {
            MessageBox.Show(exception, "Error on: " + functionError);
        }

        private void FormInterface_GetTraceErrorDescription(int cycleId, int errorCode)
        {
            if (MainEngine.Screen == EngineData.Screens.Base)
            {
                var error = MainEngine.MyDB.TraceErrorMessages.Find(e => e.TraceErrorCode == errorCode);
                _instructionsBuilder[cycleId].Append(error.TraceErrorDescription);
            }
        }

        private void IOCycle_BKConnectionError()
        {
            MessageBox.Show(MainEngine.PrintGenericText("Unable to Connect to: ") + MainEngine.MainIO.MyBK.wsIPAddress);
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.DoEvents();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {

            MainEngine.MainIO.WriteDO(EngineData.DO.Safety_Relay_On, false);
            CloseApplication();
            GC.Collect();

        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            // Resize message boxes for diferent cycles
            if (_instructionsByCycle != null)
            {
                int cycle = 0;
                int maxWidth = this.groupBox2.Width - 10;
                var points = CalcPoints(6, maxWidth, _instructionsByCycle.Count);
                foreach (var tb in _instructionsByCycle)
                {
                    tb.Value.Location = points[cycle];
                    tb.Value.Size = new Size(maxWidth / _instructionsByCycle.Count, this.groupBox2.Height - 30);

                    cycle++;
                }
            }

        }

        private void ProcessControls(Control.ControlCollection ctrls)
        {
            foreach (Control c in ctrls)
            {

                //Translate controls
                if (c.Tag != null && !c.Tag.Equals("") && !(c is CheckBox)) c.Text = MainEngine.PrintGenericText((c.Text));

                if (c.HasChildren) ProcessControls(c.Controls);
            }
        }
        private bool UpdateFormLanguage()
        {
            try
            {
                lvDI.Columns[1].Text = MainEngine.PrintGenericText("Digital inputs");
                lvDO.Columns[1].Text = MainEngine.PrintGenericText("Digital outputs");
                lvAI.Columns[1].Text = MainEngine.PrintGenericText("Analog inputs");
                lvAO.Columns[1].HeaderText = MainEngine.PrintGenericText("Analog outputs");

                ProcessControls(this.Controls);

                //To update language of tabel references
                if (!MainEngine.HasAutoRef)
                {
                    #region Update TableRefs

                    comboBoxModelo.Refresh();

                    #endregion Update TableRefs
                }

                return true;
            }
            catch (Exception exp)
            {
                MessageBox.Show(MainEngine.PrintGenericText("Error initializing Language parameters in") + @"'UpdateFormLanguage()':" + Environment.NewLine + Environment.NewLine + exp.ToString(), MainEngine.PrintGenericText("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void UpdateFormLanguageFlag()
        {
            if (MainEngine.ActualLang.Equals("PT"))
            {
                toolStripSelectLanguage.Text = @"Portuguese";
                toolStripSelectLanguage.Image = knknToolStripMenuItem1.Image;
            }
            else if (MainEngine.ActualLang.Equals("EN"))
            {
                toolStripSelectLanguage.Text = @"English";
                toolStripSelectLanguage.Image = knkToolStripMenuItem.Image;
            }
            else if (MainEngine.ActualLang.Equals("ES"))
            {
                toolStripSelectLanguage.Text = @"Spanish";
                toolStripSelectLanguage.Image = knknToolStripMenuItem2.Image;
            }
            else if (MainEngine.ActualLang.Equals("RO"))
            {
                toolStripSelectLanguage.Text = @"Romanian";
                toolStripSelectLanguage.Image = njnjknToolStripMenuItem.Image;
            }
            else if (MainEngine.ActualLang.Equals("DE"))
            {
                toolStripSelectLanguage.Text = @"German";
                toolStripSelectLanguage.Image = knknToolStripMenuItem.Image;
            }
            else if (MainEngine.ActualLang.Equals("CN"))
            {
                toolStripSelectLanguage.Text = @"Chinese";
                toolStripSelectLanguage.Image = nnjnToolStripMenuItem.Image;
            }
        }

        private void CloseApplication()
        {
            //Desliga o relé de emergência:
            MainEngine.MainIO?.WriteDO(0, false);
            Thread.Sleep(200);

            MainEngine.ReleaseDevices();
            timerGeneric.Stop();
            timerManualMode.Stop();

            try { timerGeneric.Dispose(); }
            catch (Exception ex)
            {
                if (ex.InnerException != null) throw ex.InnerException;
            }
            try { timerManualMode.Dispose(); }
            catch (Exception ex)
            {
                if (ex.InnerException != null) throw ex.InnerException;
            }


            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);

            try { Application.Exit(); }
            catch (Exception) { throw; }

            this.Dispose();
            GC.Collect();
        }
        #region ToolStrip

        private void toolStripSelectLanguage_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem != null)
            {
                toolStripSelectLanguage.Text = e.ClickedItem.Text;
                toolStripSelectLanguage.Image = e.ClickedItem.Image;

                if (e.ClickedItem.Text == @"Portuguese")
                {
                    MainEngine.ActualLang = Language.PT;
                }
                else if (e.ClickedItem.Text == "English")
                {
                    MainEngine.ActualLang = Language.EN;
                }
                else if (e.ClickedItem.Text == "Spanish")
                {
                    MainEngine.ActualLang = Language.ES;
                }
                else if (e.ClickedItem.Text == "Romanian")
                {
                    MainEngine.ActualLang = Language.RO;
                }
                else if (e.ClickedItem.Text == "German")
                {
                    MainEngine.ActualLang = Language.DE;
                }
                else if (e.ClickedItem.Text == "Chinese")
                {
                    MainEngine.ActualLang = Language.CN;
                }

                UpdateFormLanguage();
            }
        }

        private void toolstripUsers_Click(object sender, EventArgs e)
        {
            if (!LogUserIn()) CloseApplication();
        }

        private void toolstripStatus_Click(object sender, EventArgs e)
        {
            if (MainEngine.MainIO.MyBK.wsStatus != 1) MainEngine.MainIO.MyBK.ConnectToServer();
        }
        private void toolStripCountParts_DoubleClick(object sender, EventArgs e)
        {
            Engine.CountPartOK = 0;
            toolStripCountParts.Text = Engine.CountPartOK.ToString();
        }
        #endregion ToolStrip

        #endregion

        #region Login
        private bool LogUserIn()
        {
            //Aspiração e amostras não realizadas;
            Engine.AspirationOK = false;
            //Instance of Login Created and receives the Database Resource and if will load the data from XML or SQL
            using (var f = new Login(MainEngine.MyDB, MainEngine.DBConnection))
            {
                f.IdentityUpdated += new Login.IdentityUpdateHandler(FormModel_ButtonClicked);
                f.ShowDialog();

                return f.GetLoggedIn ? true : false;
            }
        }

        private void FormModel_ButtonClicked(object sender, IdentityUpdateEventArgs e)
        {

            MainEngine.CurrentAccessMask = Convert.ToInt32(e.UserLevel);
            UpdatePasswordLogin(e.Username, e.Identification);
            MainEngine.CurrentUser = e.Username + " ( " + e.Identification + " )";
            MainEngine.CurrentUserID = e.IdUser;
        }

        private void UpdatePasswordLogin(string userName, string identification)
        {
            //Update user


            //ToDo: This Should be visible on the Form -> Find a stop to show the UserName/ Identification
            this.Text = Application.ProductName.ToString() + @"   (" + userName + @" - " + identification + @")";

            //label_user.Text = userName + @" - " + identification;

            //Check permissions:
            UpdateUserPermissions();
        }

        private void UpdateUserPermissions(bool Worker = false)
        {
            var Enable = (MainEngine.CurrentAccessMask > 1 && !Worker) ? true : false;

            if (dataGridView_Parameters.Columns.Count > 3)
            {
                if (Enable)
                {
                    dataGridView_Parameters.Columns[2].ReadOnly = false;
                    dataGridView_Parameters.Columns[3].ReadOnly = false;
                    dataGridView_Parameters.Columns[2].DefaultCellStyle.BackColor = Color.White;
                    dataGridView_Parameters.Columns[3].DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    dataGridView_Parameters.Columns[2].ReadOnly = true;
                    dataGridView_Parameters.Columns[3].ReadOnly = true;
                    dataGridView_Parameters.Columns[2].DefaultCellStyle.BackColor = Color.WhiteSmoke;
                    dataGridView_Parameters.Columns[3].DefaultCellStyle.BackColor = Color.WhiteSmoke;
                }
            }

            foreach (Control a in tabNordicRFID.Controls) if (a is GroupBox) a.Enabled = Enable;
            foreach (Control c in tabIAI_Move.Controls) if (c is GroupBox) c.Enabled = Enable;
            foreach (Control d in tabParams.Controls) if (d is GroupBox) d.Enabled = Enable;
            foreach (Control e in tabDIO.Controls) if (e is ListView) e.Enabled = Enable;
        }
        #endregion

        #region Main
        private void btnStartAuto_Click(object sender, EventArgs e)
        {
            //TODO: Reset alarm IAI
            btnStartAuto.Enabled = false;
            btnStopAuto.Enabled = true;
            MainEngine.CurrentStatus = Engine.ENUM_Cycle.Auto;
            foreach (var cycle in MainEngine.Cycles) cycle.RestartCycles();
        }
        private void btnStopAuto_Click(object sender, EventArgs e)
        {
            panelPicture.Visible = false;
            MainEngine.Screen = EngineData.Screens.Base;
            foreach (var cycle in MainEngine.Cycles)
            {
                cycle.KillCycles();
            }
            MainEngine.CurrentStatus = Engine.ENUM_Cycle.Manual;
            if (backgroundWorker_AutoBM.IsBusy) backgroundWorker_AutoBM.CancelAsync();
        }
        private void btnHomePosition_Click(object sender, EventArgs e)
        {
            if (MainEngine.CurrentStatus != Engine.ENUM_Cycle.Home && (comboBoxModelo.SelectedIndex != 0 || MainEngine.HasAutoRef == true))
            {
                btnStartAuto.Enabled = false;
                btnHomePosition.Enabled = false;
                btnStopAuto.Enabled = true;
                MainEngine.CurrentStatus = Engine.ENUM_Cycle.Home;

                if (backgroundWorker_AutoBM.IsBusy)
                {
                    backgroundWorker_AutoBM.CancelAsync();
                }
                else
                {
                    backgroundWorker_AutoBM.RunWorkerAsync();
                }
            }
            else
            {
                if (comboBoxModelo.Text == "-- Select Reference --")
                {
                    MessageBox.Show(MainEngine.PrintGenericText("Please Select a valid Reference!"), "Warning - Reference");
                }
              
            }
        }
        #endregion

        #region Beckhoff Manual Mode
        //FUNÇÃO PARA ACRESCENTAR A LISTVIEW DAS ENTRADAS
        //Só é chamada a primeira vez quando entra no modo manual
        private void AddListDIItems()
        {
            var strItems = new string[2];

            for (int j = 0; j < MainEngine.MainIO.Dt_DI.Rows.Count; j++)
            {
                //Array com o nome e endereço para acrescentar os items da lista
                strItems[0] = MainEngine.MainIO.Dt_DI.Rows[j]["Address"].ToString();
                strItems[1] = MainEngine.MainIO.Dt_DI.Rows[j]["DIName"].ToString();

                //Inicializar um objecto com os items de uma linha da lista
                var lvItems = new ListViewItem(strItems)
                {
                    UseItemStyleForSubItems = false
                };

                //Colocar uma cor diferente para o nome das reservas
                if (MainEngine.MainIO.Dt_DI.Rows[j]["DIName"].ToString().Contains("Reserve"))
                    lvItems.SubItems[1].ForeColor = Color.Red;

                //Acrescentar os items à lista
                lvDI.Items.Add(lvItems);
            }
            lvDI.Columns[0].Width = 45;
            lvDI.Columns[1].Width = 270;
        }

        //FUNÇÃO PARA ACRESCENTAR A LISTVIEW DAS SAÍDAS
        //Só é chamada a primeira vez quando entra no modo manual
        private void AddListDOItems()
        {
            var strItems = new string[2];

            for (int j = 0; j < MainEngine.MainIO.Dt_DO.Rows.Count; j++)
            {
                //Array com o nome e endereço para acrescentar os items da lista
                strItems[0] = MainEngine.MainIO.Dt_DO.Rows[j]["Address"].ToString();
                strItems[1] = MainEngine.MainIO.Dt_DO.Rows[j]["DOName"].ToString();

                //Inicializar um objecto com os items de uma linha da lista
                var lvItems = new ListViewItem(strItems)
                {
                    UseItemStyleForSubItems = false
                };

                //Colocar uma cor diferente para o nome das reservas
                if (MainEngine.MainIO.Dt_DO.Rows[j]["DOName"].ToString().Contains("Reserve")) lvItems.SubItems[1].ForeColor = Color.Red;

                //Acrescentar os items à lista
                lvDO.Items.Add(lvItems);
            }
            lvDO.Columns[0].Width = 55;
            lvDO.Columns[1].Width = 260;
        }

        //FUNÇÃO PARA ACRESCENTAR A LISTVIEW DAS ANALÓGICAS
        //Só é chamada a primeira vez quando entra no modo manual
        private void AddListAIItems()
        {
            string[] strItems = new string[3];

            if (MainEngine.MainIO.Dt_AI.Rows.Count == 0) tabControlIO.TabPages.Remove(tabAIO);

            for (int j = 0; j < MainEngine.MainIO.Dt_AI.Rows.Count; j++)
            {
                //Array com o nome e endereço para acrescentar os items da lista
                strItems[0] = MainEngine.MainIO.Dt_AI.Rows[j]["Address"].ToString();
                strItems[1] = MainEngine.MainIO.Dt_AI.Rows[j]["AIName"].ToString();
                strItems[2] = MainEngine.MainIO.Dt_AI.Rows[j]["Value"].ToString();
                //Inicializar um objecto com os items de uma linha da lista
                var lvItems = new ListViewItem(strItems)
                {
                    UseItemStyleForSubItems = false
                };

                //Colocar uma cor diferente para o nome das reservas
                if (MainEngine.MainIO.Dt_AI.Rows[j]["AIName"].ToString().Contains("Reserve")) lvItems.SubItems[1].ForeColor = Color.Red;

                //Acrescentar os items à lista
                lvAI.Items.Add(lvItems);
            }
            lvAI.Columns[0].Width = 40;
            lvAI.Columns[1].Width = 245;
            lvAI.Columns[2].Width = 70;
        }

        //Ver AOs
        private void AddListAOItems()
        {
            lvAO.DataSource = MainEngine.MainIO.Dt_AO;

            lvAO.Columns[0].ReadOnly = true;
            lvAO.Columns[1].ReadOnly = true;
            lvAO.Columns[2].ReadOnly = false;

            foreach (DataGridViewRow row in lvAO.Rows)
            {
                if (row.Cells["AOName"].ToString().Contains("Reserve")) row.DefaultCellStyle.ForeColor = Color.Red;
            }
        }

        //ACTUALIZAR O MODO MANUAL
        //Constantemente actualizado sempre que está seleccionado o modo manual
        private void UpdateManualMode()
        {
            try
            {
                var corTrue = Color.LightGreen;
                var corFalse = Color.White;
                var corDisable = Color.LightGray;

                int Max = Math.Max(Math.Max(MainEngine.MainIO.Dt_AI.Rows.Count, MainEngine.MainIO.Dt_DI.Rows.Count),
                                   Math.Max(MainEngine.MainIO.Dt_DI.Rows.Count, MainEngine.MainIO.Dt_DO.Rows.Count));

                for (int j = 0; j < Max; j++)
                {
                    if (j < MainEngine.MainIO.Dt_AI.Rows.Count)
                    {
                        if (j < 2) lvAI.Items[j].SubItems[2].Text = Math.Round(((double)MainEngine.MainIO.ReadDAI(j) * 30 / 32767), 2).ToString();
                        else lvAI.Items[j].SubItems[2].Text = Math.Round(((double)MainEngine.MainIO.ReadDAI(j) * 30 / 32767), 2).ToString();
                    }

                    if (j < MainEngine.MainIO.Dt_DI.Rows.Count)
                    {
                        if (MainEngine.MainIO.UpdateDIORows(j, "Value", null, Preh.IOCycle.ReadWriteIO.ReadDI, ""))
                        {
                            lvDI.Items[j].BackColor = lvDI.Items[j].SubItems[1].BackColor = corTrue;
                        }
                        else lvDI.Items[j].BackColor = lvDI.Items[j].SubItems[1].BackColor = corFalse;
                    }

                    if (j < MainEngine.MainIO.Dt_DO.Rows.Count)
                    {
                        if (MainEngine.MainIO.UpdateDIORows(j, "Value", null, Preh.IOCycle.ReadWriteIO.ReadDO, ""))
                        {
                            lvDO.Items[j].BackColor = lvDO.Items[j].SubItems[1].BackColor = corTrue;
                        }
                        else lvDO.Items[j].BackColor = lvDO.Items[j].SubItems[1].BackColor = corFalse;

                        if (MainEngine.MainIO.InhibitOutputs[j] == false)
                        {
                            lvDO.Items[j].BackColor = lvDO.Items[j].SubItems[1].BackColor = corDisable;
                            lvDO.Items[j].Checked = MainEngine.MainIO.UpdateDIORows(j, "ValueToWrite", null, Preh.IOCycle.ReadWriteIO.ReadDO, "");
                        }
                        else
                        {
                            MainEngine.MainIO.WriteDO(j, lvDO.Items[j].Checked);
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                throw new Exception("UpdateManualMode", exp);
            }
        }
        private void UpdateAnalogIO()
        {
            try
            {
                int Max = Math.Max(Math.Max(MainEngine.MainIO.Dt_AI.Rows.Count, MainEngine.MainIO.Dt_DI.Rows.Count), Math.Max(MainEngine.MainIO.Dt_DI.Rows.Count, MainEngine.MainIO.Dt_DO.Rows.Count));

                for (int j = 0; j < Max; j++)
                {
                    if (j < MainEngine.MainIO.Dt_AI.Rows.Count)
                    {
                        if (j < 2)
                            lvAI.Items[j].SubItems[2].Text = Math.Round(((double)MainEngine.MainIO.ReadDAI(j) * 30 / 32767), 2).ToString();
                        else
                            lvAI.Items[j].SubItems[2].Text = Math.Round(((double)MainEngine.MainIO.ReadDAI(j) * 30 / 32767), 2).ToString();
                    }

                }
                var i = 0;
                if (!MainEngine.MainIO.StopReadAO)
                {
                    foreach (DataRow row in MainEngine.MainIO.Dt_AO.Rows)
                    {
                        row["Value"] = Math.Round(((double)MainEngine.MainIO.ReadAO(i) * 30 / 32767), 2).ToString();
                        i++;
                    }
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.ToString());
            }
        }
        #endregion

        #region Parameters & References


        private void UpdateCurrentMachineConsts()
        {
            MainEngine.dbLoadConsts();
        }

        private void UpdateCurrentModelParameters()
        {

            if (!MainEngine.HasAutoRef && comboBoxModelo.SelectedValue != null)
            {
                #region Manual References Selection
                var idRef = (int)comboBoxModelo.SelectedValue;
                var refer = new Reference();

                if (MainEngine.DBConnection == Engine.DataSource.SQL)
                {
                    refer = MainEngine.MyDB.References.Find(r => r.IDRef == idRef);
                }
                else
                {
                    refer = MainEngine.RefsList.Find(r => r.IDRef == idRef);
                }

                if (refer != null)
                {
                    if (refer.RefPreh != "-- Select Reference --")
                    {
                        MainEngine.SelectedRefPreh = refer.RefPreh;
                        MainEngine.ActualIDRef = refer.IDRef;                        
                        MainEngine.RefDescription = refer.RefDescription;
                        EngineData.PrehRef = refer.RefPreh;
                        MainEngine.GetAllParameters();

                    }
                    else
                    {
                        MessageBox.Show(MainEngine.PrintGenericText("Please Select a Valid Reference!"));
                    }

                }
                else
                {

                    MessageBox.Show(MainEngine.PrintGenericText("Please Select a Valid Reference!"));
                }


                #endregion Manual References Selection

            }
            else if (MainEngine.HasAutoRef)
            {
                // Automatic References Selection
                #region Automatic References Selection
                label_Refs.Text = MainEngine.SelectedRefPreh;
                labelVersion.Text = MainEngine.RefDescription;
                var refer = new Reference();

                if (MainEngine.DBConnection == Engine.DataSource.SQL)
                {
                    refer = MainEngine.MyDB.References.Find(r => r.RefPreh == MainEngine.SelectedRefPreh);
                }
                else
                {
                    refer = MainEngine.RefsList.Find(r => r.RefPreh == MainEngine.SelectedRefPreh);
                }

                if (refer != null)
                {
                    MainEngine.GetAllParameters();
                }


                #endregion Automatic References Selection
            }

            if (MainEngine.DBConnection == Engine.DataSource.SQL)
            {

                if (MainEngine.MyDB.MeasureLimits != null)
                {
                    ConfigDataGridViewMeasureLimits(MainEngine.MyDB.MeasureLimits);
                }

            }
            else
            {

                ConfigDataGridViewMeasureLimits(MainEngine.OfflineMeasuresList);

            }

        }

        private bool CheckIfCalibrationIsNeeded()
        {
            UpdateCurrentModelParameters();

            var calibError = MainEngine.MyDB.DeviceConsts.Find(c => c.ConstName == "CalibrationError");
            var calibWarning = MainEngine.MyDB.DeviceConsts.Find(c => c.ConstName == "CalibrationWarning");

            try
            {
                var daysLastCalibration = 0;
                var intAux = 0;
                #region Check if it's saturday or sunday and if it's dont execute

                var strDayOfWeek = (DateTime.Now).ToString("dddddd");
                switch (strDayOfWeek.ToUpper())
                {
                    case "SÁBADO":
                    case "SATURDAY":
                    case "SABADO":
                    case "DOMINGO":
                    case "SUNDAY":
                        lbl_CalibrationInfo.Visible = false;
                        txtcalibrationDays.Visible = false;
                        return true;

                    default:
                        lbl_CalibrationInfo.Visible = true;
                        txtcalibrationDays.Visible = true;
                        break;
                }

                #endregion Check if it's saturday or sunday and if it's dont execute

                #region Check if calibration is needed


                //Get last time that a calibration was made
                var adjustments = MainEngine.MyDB.Calibration_GetLastAdjustments();
                if (adjustments.Count == 0)
                {
                    daysLastCalibration = Convert.ToInt16(calibError.ConstValue);
                }
                else
                {
                    daysLastCalibration = adjustments.Max(a => a.ElapsedDays);
                }

                if (calibError != null && calibWarning != null)
                {
                    if (daysLastCalibration >= Convert.ToInt16(calibError.ConstValue))
                    {
                        //Necessary to stop
                        txtcalibrationDays.BackColor = Color.Tomato;
                        txtcalibrationDays.Text = "0";
                    }
                    else if ((daysLastCalibration >= Convert.ToInt16(calibWarning.ConstValue)) &&
                        (daysLastCalibration < Convert.ToInt16(calibError.ConstValue)))
                    {
                        //necessary to display a warning
                        txtcalibrationDays.BackColor = Color.Orange;
                        intAux = Convert.ToInt16(calibWarning.ConstValue) - daysLastCalibration;
                        txtcalibrationDays.Text = intAux.ToString();
                    }
                    else
                    {
                        //OK
                        txtcalibrationDays.Visible = true;
                        txtcalibrationDays.BackColor = Color.Chartreuse;
                        intAux = Convert.ToInt16(calibError.ConstValue) - daysLastCalibration;
                        txtcalibrationDays.Text = intAux.ToString(); ;
                    }
                }

                #endregion Check if calibration is needed

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        #region Control DataGridView

        private void ConfigDataGridViewConst(List<DeviceConst> ListConst)
        {
            var f1 = new Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            var f2 = new Font("Verdana", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            if (ListConst != null)
            {
                dataGridView_Constants.DataSource = ListConst;
                dataGridView_Constants.Update();
                dataGridView_Constants.Refresh();
                dataGridView_Constants.DefaultCellStyle.Font = f1;
                dataGridView_Constants.ColumnHeadersDefaultCellStyle.Font = f2;

                foreach (var column in dataGridView_Constants.Columns)
                {
                    var c = (DataGridViewColumn)column;
                    c.ReadOnly = true;
                    c.MinimumWidth = 30;
                    c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    c.DefaultCellStyle.BackColor = Color.WhiteSmoke;
                    c.DefaultCellStyle.Font = f1;
                }

                dataGridView_Constants.Columns[3].ReadOnly = false;
                dataGridView_Constants.Columns[0].Visible = false;
                dataGridView_Constants.Columns[1].Visible = false;


                if (dataGridView_Constants.Columns.Count > 2)
                {

                    dataGridView_Constants.Columns[3].ReadOnly = MainEngine.CurrentAccessMask <= 1;
                    dataGridView_Constants.Columns[3].DefaultCellStyle.BackColor = MainEngine.CurrentAccessMask > 1 ? Color.White : Color.WhiteSmoke;
                }

                for (int i = 0; i < dataGridView_Constants.Columns.Count; i++)
                {
                    dataGridView_Constants.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }
        }

        private void ConfigDataGridViewMeasureLimits(List<MeasureLimit> limits)
        {
            var f1 = new Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            var f2 = new Font("Verdana", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));



            if (limits != null)
            {
                dataGridView_Parameters.DataSource = limits;

                foreach (var column in dataGridView_Parameters.Columns)
                {
                    var c = (DataGridViewColumn)column;
                    c.Visible = false;
                    c.ReadOnly = true;
                    c.MinimumWidth = 30;
                    c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    c.DefaultCellStyle.BackColor = Color.WhiteSmoke;
                    c.DefaultCellStyle.Font = f1;
                }



                dataGridView_Parameters.Refresh();
                dataGridView_Parameters.DefaultCellStyle.Font = f1;
                dataGridView_Parameters.ColumnHeadersDefaultCellStyle.Font = f2;


                var list = new List<int> { 6, 7, 8, 9, 11, 12 };

                foreach (var item in list)
                {
                    dataGridView_Parameters.Columns[item].Visible = true;
                }

                dataGridView_Parameters.Columns[7].ReadOnly = false;
                dataGridView_Parameters.Columns[8].ReadOnly = false;

                if (dataGridView_Parameters.Columns.Count > 3)
                {
                    dataGridView_Parameters.Columns[7].ReadOnly = MainEngine.CurrentAccessMask <= 1;
                    dataGridView_Parameters.Columns[8].ReadOnly = MainEngine.CurrentAccessMask <= 1;
                    dataGridView_Parameters.Columns[7].DefaultCellStyle.BackColor = MainEngine.CurrentAccessMask > 1 ? Color.White : Color.WhiteSmoke;
                    dataGridView_Parameters.Columns[8].DefaultCellStyle.BackColor = MainEngine.CurrentAccessMask > 1 ? Color.White : Color.WhiteSmoke;
                }

                for (int i = 0; i < dataGridView_Parameters.Columns.Count; i++)
                {
                    dataGridView_Parameters.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }
        }

        private void dataGridView_Parameters_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex == 2 || e.ColumnIndex == 3)
            {
                var col = dataGridView_Parameters.Columns[e.ColumnIndex].Name;
                var row = dataGridView_Parameters.Rows[e.RowIndex].Cells[0].Value.ToString();

                double d = 0;
                if (!double.TryParse(e.FormattedValue.ToString(), out d))
                {
                    MessageBox.Show(MainEngine.PrintGenericText("An error occurred while editing data fields||") + "Parameter: " + row + "    Value: " + col + MainEngine.PrintGenericText("|(Click OK to discard changes)"), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Cancel = true;
                }
            }
        }
        private void dataGridView_Parameters_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            btnDiscard.Enabled = true;
            dataGridView_Parameters[e.ColumnIndex, e.RowIndex].Style.ForeColor = Color.Red;
        }

        private void dataGridView_Parameters_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            var col = dataGridView_Parameters.Columns[e.ColumnIndex].Name;
            var row = dataGridView_Parameters.Rows[e.RowIndex].Cells[0].Value.ToString();

            MessageBox.Show(MainEngine.PrintGenericText("An error occurred while editing data fields||") + "Value: '" + col + "'\r\nParameter: '" + row + "'" + MainEngine.PrintGenericText("|(Click OK to discard changes)"), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void ResetDataGridView(bool AcceptChanges, ref DataGridView dgv)
        {

            foreach (DataGridViewRow drw in dgv.Rows)
            {
                foreach (DataGridViewCell drc in drw.Cells)
                {
                    drc.Style.ForeColor = Color.Black;
                }
            }
        }

        private bool SaveMeasureLimits()
        {
            if (MainEngine.LimitsDataSource == Engine.DataSource.XML)
            {
                var num = 0;
                var listasDiferentes = false;
                listaParametros = MainEngine.LoadMeasureLimitsXML();

                var MinValue_CellChanged = new List<int>();
                var MaxValue_CellChanged = new List<int>();

                foreach (var x in listaParametros)
                {
                    if (!x.MinValue.Equals(dataGridView_Parameters.Rows[num].Cells[6].Value))
                    {
                        MinValue_CellChanged.Add(num);
                        listasDiferentes = true;
                        //break;
                    }

                    if (!x.MaxValue.Equals(dataGridView_Parameters.Rows[num].Cells[7].Value))
                    {
                        MaxValue_CellChanged.Add(num);
                        listasDiferentes = true;
                        //break;
                    }

                    num++;
                }


                if (!listasDiferentes)
                {
                    MessageBox.Show(MainEngine.PrintGenericText("There was no change. Nothing will be saved."));

                    for (int i = 0; i < listaParametros.Count; i++)
                    {
                        dataGridView_Parameters.Rows[i].Cells[6].Style.ForeColor = Color.Black;
                        dataGridView_Parameters.Rows[i].Cells[7].Style.ForeColor = Color.Black;
                    }

                    return false;
                }
                else
                {
                    if (MessageBox.Show(MainEngine.PrintGenericText("Are you sure that you want to change settings?"), "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                    {
                        var writeSuccess = WriteLimitsXML((List<MeasureLimit>)dataGridView_Parameters.DataSource);
                        MainEngine.dbLoadParameters();
                        ConfigDataGridViewMeasureLimits(MainEngine.OfflineMeasuresList);   // Reload da Grid

                        foreach (int x in MinValue_CellChanged)
                        {
                            dataGridView_Parameters.Rows[x].Cells[6].Style.ForeColor = Color.Black;
                        }

                        foreach (int x in MaxValue_CellChanged)
                        {
                            dataGridView_Parameters.Rows[x].Cells[7].Style.ForeColor = Color.Black;
                        }

                        return writeSuccess;
                    }

                }


            }
            else
            {

                var hasChanges = false;
                var actualList = MainEngine.MyDB.MeasureLimits;
                MainEngine.MyDB.Measures_LoadMeasureLimits(MainEngine.SelectedIdRef);
                var newList = MainEngine.MyDB.MeasureLimits;

                var index = 0;
                foreach (var item in actualList)
                {
                    if (item.MaxValue != newList[index].MaxValue)
                    {
                        hasChanges = true;
                    }
                    if (item.MinValue != newList[index].MinValue)
                    {
                        hasChanges = true;
                    }

                    index++;
                }

                if (!hasChanges)
                {
                    MessageBox.Show(MainEngine.PrintGenericText("There was no change. Nothing will be saved."));

                    for (int i = 0; i < MainEngine.MyDB.MeasureLimits.Count; i++)
                    {
                        dataGridView_Parameters.Rows[i].Cells[6].Style.ForeColor = Color.Black;
                        dataGridView_Parameters.Rows[i].Cells[7].Style.ForeColor = Color.Black;
                    }

                    return false;
                }
                else
                {
                    if (MessageBox.Show(MainEngine.PrintGenericText("Are you sure that you want to change settings?"), "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                    {
                        var writeSuccess = false;

                        foreach (var item in actualList)
                        {

                            var result = MainEngine.MyDB.Measures_UpdateMeasureLimit
                                (item.ID_SubWS, MainEngine.CurrentUserID, MainEngine.SelectedIdRef, item.ID_MeasureLimit,
                                item.MinValue, item.MaxValue, item.Unit, item.Variant, item.Active, item.Description);

                            if (!result)
                            {
                                break;
                            }

                        }

                        MainEngine.dbLoadParameters();
                        ConfigDataGridViewMeasureLimits(MainEngine.MyDB.MeasureLimits);   // Reload da Grid

                        for (int i = 0; i < MainEngine.MyDB.MeasureLimits.Count; i++)
                        {
                            dataGridView_Parameters.Rows[i].Cells[6].Style.ForeColor = Color.Black;
                            dataGridView_Parameters.Rows[i].Cells[7].Style.ForeColor = Color.Black;
                        }

                        return writeSuccess;
                    }
                }

            }


            return true;
        }

        private bool SaveConsts()
        {
            if (MainEngine.LimitsDataSource == Engine.DataSource.XML)
            {

                int num = 0;
                bool listasDiferentes = false;
                listaConstantes = MainEngine.LoadConstantesXML();

                List<int> CellChanged = new List<int>();

                foreach (var x in listaConstantes)
                {
                    if (!x.ConstValue.Equals(dataGridView_Constants.Rows[num].Cells[3].Value))
                    {
                        CellChanged.Add(num);
                        listasDiferentes = true;
                        //break;
                    }

                    num++;
                }


                if (!listasDiferentes)
                {
                    MessageBox.Show(MainEngine.PrintGenericText("There was no change. Nothing will be saved."));

                    for (int i = 0; i < listaConstantes.Count; i++)
                    {
                        dataGridView_Constants.Rows[i].Cells[3].Style.ForeColor = Color.Black;
                    }
                }
                else
                {
                    if (MessageBox.Show(MainEngine.PrintGenericText("Are you sure that you want to change settings?"), "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                    {
                        WriteConstsXML((List<DeviceConst>)dataGridView_Constants.DataSource);
                        MainEngine.dbLoadConsts();
                        ConfigDataGridViewConst(MainEngine.DeviceConstsList);   // Reload da Grid

                        foreach (int x in CellChanged)
                        {
                            dataGridView_Constants.Rows[x].Cells[3].Style.ForeColor = Color.Black;
                        }
                    }
                }
            }
            else
            {
                var hasChanges = false;
                var actualList = MainEngine.MyDB.DeviceConsts;
                MainEngine.MyDB.Const_LoadConstants();
                var newList = MainEngine.MyDB.DeviceConsts;

                var index = 0;
                foreach (var item in actualList)
                {
                    if (item.ConstValue != newList[index].ConstValue)
                    {
                        hasChanges = true;
                    }

                    index++;
                }

                if (!hasChanges)
                {
                    MessageBox.Show(MainEngine.PrintGenericText("There was no change. Nothing will be saved."));

                    for (int i = 0; i < MainEngine.MyDB.DeviceConsts.Count; i++)
                    {
                        dataGridView_Constants.Rows[i].Cells[3].Style.ForeColor = Color.Black;
                    }

                    return false;
                }
                else
                {
                    if (MessageBox.Show(MainEngine.PrintGenericText("Are you sure that you want to change settings?"), "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                    {
                        var writeSuccess = false;

                        foreach (var item in actualList)
                        {

                            var result = MainEngine.MyDB.Const_UpdateConstant
                                (item.ID_SubWS, MainEngine.CurrentUserID, item.ConstValue, item.ConstName);

                            if (!result)
                            {
                                break;
                            }

                        }

                        MainEngine.dbLoadConsts();
                        ConfigDataGridViewConst(MainEngine.MyDB.DeviceConsts);   // Reload da Grid

                        for (int i = 0; i < MainEngine.MyDB.DeviceConsts.Count; i++)
                        {
                            dataGridView_Constants.Rows[i].Cells[3].Style.ForeColor = Color.Black;
                        }

                        return writeSuccess;
                    }
                }

            }
            return true;
        }

        private void PrintAndLogError(string errorText)
        {
            var txt = MainEngine.PrintGenericText(errorText);
            Log.Instance.Error(txt);
            MessageBox.Show("Error!", txt);
        }

        public void WriteConstsXML(List<DeviceConst> Lista)
        {
            var doc = new XmlDocument();
            doc.Load("XML Files\\Consts.xml");
            var aNodes = doc.SelectNodes("/Constants/Table");

            int num = 0;
            foreach (var x in Lista)
            {
                aNodes[num].SelectSingleNode("ID_Const").InnerText = x.ID_Const.ToString();
                aNodes[num].SelectSingleNode("ID_SubWS").InnerText = x.ID_SubWS.ToString();
                aNodes[num].SelectSingleNode("ConstName").InnerText = x.ConstName.ToString();
                aNodes[num].SelectSingleNode("ConstValue").InnerText = x.ConstValue.ToString();
                aNodes[num].SelectSingleNode("ConstDescription").InnerText = x.ConstDescription.ToString();

                num++;
            }

            doc.Save("XML Files\\Consts.xml");
            doc.Clone();

            num++;
            //}
        }

        public static bool WriteLimitsXML(List<MeasureLimit> Lista)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load("XML Files\\MeasureLimits.xml");
                var aNodes = doc.SelectNodes("/MeasureLimit/Table");

                int num = 0;
                foreach (var x in Lista)
                {
                    aNodes[num].SelectSingleNode("ID_MeasureLimit").InnerText =
                    x.ID_MeasureLimit.ToString();
                    aNodes[num].SelectSingleNode("MeasureProfile").InnerText =
                    x.MeasureProfile.ToString();
                    aNodes[num].SelectSingleNode("ID_SubWS").InnerText =
                    x.ID_SubWS.ToString();
                    aNodes[num].SelectSingleNode("ID_WS").InnerText =
                    x.ID_WS.ToString();
                    aNodes[num].SelectSingleNode("SubWS").InnerText =
                    x.SubWS.ToString();
                    aNodes[num].SelectSingleNode("MeasureName").InnerText =
                    x.MeasureName.ToString();
                    aNodes[num].SelectSingleNode("MinValue").InnerText =
                    x.MinValue.ToString();
                    aNodes[num].SelectSingleNode("MaxValue").InnerText =
                    x.MaxValue.ToString();
                    aNodes[num].SelectSingleNode("Unit").InnerText =
                    x.Unit.ToString();
                    aNodes[num].SelectSingleNode("Active").InnerText =
                    x.Active.ToString();
                    aNodes[num].SelectSingleNode("Description").InnerText =
                    x.Description.ToString();
                    aNodes[num].SelectSingleNode("Variant").InnerText =
                    x.Variant.ToString();

                    num++;
                }

                doc.Save("XML Files\\MeasureLimits.xml");
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }

        #endregion Control DataGridView
        private void comboBoxModelo_SelectionChangeCommitted(object sender, EventArgs e)
        {

            if (comboBoxModelo.SelectedIndex == 0)
            {
                dataGridView_Parameters.DataSource = null;
                dataGridView_Parameters.Refresh();
                labelVersion.Text = "";
            }
            else
            {
                if (!comboBoxModelo.SelectedValue.Equals(0) && !comboBoxModelo.SelectedValue.Equals("0"))
                {
                    UpdateCurrentModelParameters();
                    labelVersion.Text = MainEngine.RefDescription;
                }
            }
        }

        private void buttonTeclado_Click(object sender, EventArgs e)
        {
            Process.Start(@"C:\WINDOWS\system32\osk.exe");  //Open Virtual Key
        }
        private void buttonGuardar_Click(object sender, EventArgs e)
        {
            SaveMeasureLimits();
        }
        private void buttonDescartar_Click(object sender, EventArgs e)
        {
            MainEngine.dbLoadParameters();

            if (MainEngine.DBConnection == Engine.DataSource.SQL)
            {
                ConfigDataGridViewMeasureLimits(MainEngine.MyDB.MeasureLimits);
            }
            else
            {
                ConfigDataGridViewMeasureLimits(MainEngine.OfflineMeasuresList);
            }

            ResetDataGridView(false, ref dataGridView_Parameters);
            btnDiscard.Enabled = false;
        }
        #endregion

        #region Scanners
        private bool dgvScannersMaintenanceConfig()
        {
            try
            {
                dgvScannersMaintenance.ColumnCount = 0;
                dgvScannersMaintenance.DefaultCellStyle.Font = new Font("Verdana", 7F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                dgvScannersMaintenance.ColumnHeadersDefaultCellStyle.Font = new Font("Verdana", 7F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                dgvScannersMaintenance.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvScannersMaintenance.RowHeadersVisible = false;
                dgvScannersMaintenance.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                //IMAGE 
                DataGridViewImageColumn img = new DataGridViewImageColumn();
                dgvScannersMaintenance.Columns.Add(img);
                dgvScannersMaintenance.Rows[dgvScannersMaintenance.ColumnCount - 1].MinimumHeight = 50;
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].MinimumWidth = 20;
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].ReadOnly = true;
                img.HeaderText = "Model";

                //Connection Settings               
                dgvScannersMaintenance.ColumnCount = dgvScannersMaintenance.ColumnCount + 1;
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].Name = "Settings";
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].MinimumWidth = 50;
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].ReadOnly = true;

                //NAME
                dgvScannersMaintenance.ColumnCount = dgvScannersMaintenance.ColumnCount + 1;
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].Name = "Name";
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].MinimumWidth = 130;
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].ReadOnly = true;

                //TRIGGER BTN 
                var btn = new DataGridViewButtonColumn();
                dgvScannersMaintenance.Columns.Add(btn);
                btn.HeaderText = "Trigger";
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].Width = 60;
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;

                //CODE
                dgvScannersMaintenance.ColumnCount = dgvScannersMaintenance.ColumnCount + 1;
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].Name = "Code";
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].MinimumWidth = 350;
                dgvScannersMaintenance.Columns[dgvScannersMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private bool dgvScannersMaintenanceAddScanner(Preh.Scanner NewScanner)
        {
            try
            {
                dgvScannersMaintenance.AllowUserToAddRows = true;

                var row = (DataGridViewRow)dgvScannersMaintenance.Rows[0].Clone();

                Image imgScannerModel;
                string ScannerImgPath = Application.StartupPath + "\\DeviceImages\\Scanner_Type_" + NewScanner.Type + ".png";
                if (System.IO.File.Exists(ScannerImgPath)) imgScannerModel = Image.FromFile(ScannerImgPath);
                else imgScannerModel = System.Drawing.SystemIcons.Exclamation.ToBitmap();

                string ScannerSettings = "";
                if (NewScanner.Type == 1) ScannerSettings = NewScanner.ipAddr + "\n\rPort:" + NewScanner.TcpPort;
                else if (NewScanner.Type == 2) ScannerSettings = NewScanner.ComPort + "\n\rBR:" + NewScanner.BaudRate;

                dgvScannersMaintenance.Columns["Settings"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

                row.Cells[0].Value = imgScannerModel;
                row.Cells[1].Value = ScannerSettings;
                row.Cells[2].Value = NewScanner.Name;
                row.Cells[3].Value = "Trigger ";
                row.Cells[4].Value = "";

                dgvScannersMaintenance.Rows.Add(row);

                dgvScannersMaintenance.AllowUserToAddRows = false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void dgvScannersMaintenance_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                switch (e.ColumnIndex)
                {
                    case 3:
                        if (e.RowIndex >= 0)
                        {
                            var ScannerName = dgvScannersMaintenance[e.ColumnIndex - 1, e.RowIndex].Value.ToString();


                            if (MainEngine.Scanners.First(a => a.Name == ScannerName).IsReading)
                            {
                                MainEngine.Scanners.First(a => a.Name == ScannerName).TurnScannerOff = true;

                            }
                            else
                            {
                                MainEngine.Scanners.First(a => a.Name == ScannerName).TurnScannerOff = false;
                                MainEngine.Scanners.First(a => a.Name == ScannerName).Read();
                            }

                        }
                        break;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ScannerEventReceived(string ScannerName, string ScannerCode)
        {
            try
            {
                var RowIdx = dgvScannersMaintenance.Rows[dgvScannersMaintenance.CurrentRow.Index].Cells["Name"].RowIndex;
                dgvScannersMaintenance.Rows[RowIdx].Cells["Code"].Value = ScannerCode;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region RFID

        #endregion

        #region Timers Manual Mode & Auto
        private void timerGeneric_Tick(object sender, EventArgs e)
        {
            toolStripClock.Text = DateTime.Now.ToLongTimeString();

            #region ShowImages
            if (!backgroundWorker_AutoBM.IsBusy || lblMaintenanceSecurOK.Visible) panelPicture.Visible = false;
            else UpdatePanelPicture(MainEngine.Screen);
            #endregion ShowImages

            #region Status BK
            if (MainEngine.MainIO.MyBK.wsStatus == 1)
                toolstripStatusBK.Text = MainEngine.PrintGenericText("Connected to BK:") + MainEngine.MainIO.MyBK.wsIPAddress; // "Ligado a BK: " + BK_IPAddress;
            else
                toolstripStatusBK.Text = MainEngine.PrintGenericText("Not Connected!"); // "Without comunications!";*/
            #endregion Status BK

            #region Check Security Status

            //Indicate Inicial Position:
            if (!MainEngine.CheckSafety())
            {
                lblMaintenanceSecurOK.Visible = true;
                lblMaintenanceSecurOK.BringToFront();
                btnStartAuto.Enabled = false;
                btnStopAuto.Enabled = false;
                btnHomePosition.Enabled = false;
                return;
            }
            else
            {
                lblMaintenanceSecurOK.Visible = false;
                if (MainEngine.CurrentStatus != Engine.ENUM_Cycle.Auto) btnHomePosition.Enabled = true;
            }
            #endregion Check Security Status


            if (MainEngine.CurrentStatus == Engine.ENUM_Cycle.ReadyToStart)
            {
                btnStartAuto.Enabled = true;
                btnStopAuto.Enabled = false;
                btnHomePosition.Enabled = false;
            }

            if (MainEngine.CurrentStatus == Engine.ENUM_Cycle.Auto)
            {
                btnStartAuto.Enabled = false;
                btnStopAuto.Enabled = true;
                btnHomePosition.Enabled = false;
            }
            if (MainEngine.CurrentStatus == Engine.ENUM_Cycle.Manual)
            {
                btnStartAuto.Enabled = false;
                btnStopAuto.Enabled = false;
                btnHomePosition.Enabled = true;
            }
        }

        private void UpdateLedsFromSensors()
        {
            try
            {
                foreach (var label in _labelEnums.Keys)
                {
                    label.BackColor = MainEngine.MainIO.ReadDI(_labelEnums[label]) ? Color.Green : Color.Black;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void timerManualMode_Tick(object sender, EventArgs e)
        {
            timerManualMode.Enabled = false;
            UpdateLedsFromSensors();
            UpdateDOEnableArray();
            Application.DoEvents();
            if (tabControlIO.SelectedTab == tabDIO) UpdateManualMode(); //Actualiza o modo manual: enable checked, cor ON e enable
            if (tabControlIO.SelectedTab == tabAIO) UpdateAnalogIO();
            Application.DoEvents();
            if (!_stopTimer) timerManualMode.Enabled = true;
        }


        private void GenerateLabelArray()
        {
            foreach (Control control in tabIAI_Move.Controls)
            {
                if (control is GroupBox)
                {
                    foreach (var obj in control.Controls)
                    {

                        if (obj is Label)
                        {
                            var label = (Label)obj;

                            if (!string.IsNullOrEmpty((string)label?.Tag))
                            {
                                try
                                {
                                    var enumin = Enum.Parse(typeof(EngineData.DI), (string)label?.Tag);

                                    if (enumin != null)
                                    {
                                        _labelEnums.Add(label, (EngineData.DI)enumin);
                                    }
                                }
                                catch (Exception ex)
                                {

                                    if (ex is ArgumentException)
                                    {
                                        PrintAndLogError("The Input " + (string)label?.Tag + " does Not Exist on this Machine!");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GenerateCheckBoxArray()
        {
            foreach (Control control in tabIAI_Move.Controls)
            {
                if (control is GroupBox)
                {
                    foreach (var obj in control.Controls)
                    {
                        var box = obj as CheckBox;
                        if (!string.IsNullOrEmpty((string)box?.Tag))
                        {
                            box.MouseDown += Checkbox_CheckedChanged;

                            var splitedCond = Regex.Split(box.Tag.ToString(), @"^(\!)?([a-z0-9_]+)\s*((\&\&|\|\|)\s*(\!)?([a-z0-9_]+))?\s*((\&\&|\|\|)\s*(\!)?([a-z0-9_]+))?$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                            if (splitedCond.Length == 3)
                            {
                                if (splitedCond[1] != "!")
                                {
                                    try
                                    {
                                        var enumout = Enum.Parse(typeof(EngineData.DO), splitedCond[1]);
                                        if (enumout != null)
                                        {
                                            EngineData.DO[] enumArray = { (EngineData.DO)enumout };
                                            _checkboxEnums.Add(box, enumArray);
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                        if (ex is ArgumentException)
                                        {
                                            PrintAndLogError("The Output " + splitedCond[1] + " does Not Exist on this Machine!");
                                        }
                                    }
                                }
                            }

                            if (splitedCond.Length == 6)
                            {
                                var enumout1 = new object();
                                var enumout2 = new object();

                                if (splitedCond[1] != "!")
                                {
                                    try
                                    {
                                        enumout1 = Enum.Parse(typeof(EngineData.DO), splitedCond[1]);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex is ArgumentException)
                                        {
                                            PrintAndLogError("The Output " + splitedCond[1] + " does Not Exist on this Machine!");
                                        }

                                    }

                                }
                                if (splitedCond[4] != "!")
                                {
                                    try
                                    {
                                        enumout2 = Enum.Parse(typeof(EngineData.DO), splitedCond[4]);
                                    }
                                    catch (Exception ex)
                                    {

                                        if (ex is ArgumentException)
                                        {
                                            PrintAndLogError("The Output " + splitedCond[4] + " does Not Exist on this Machine!");
                                        }
                                    }

                                }

                                if (enumout1 != null && enumout2 != null)
                                {
                                    EngineData.DO[] enumArray = { (EngineData.DO)enumout1, (EngineData.DO)enumout2 };
                                    _checkboxEnums.Add(box, enumArray);
                                }

                            }
                            if (splitedCond.Length == 9)
                            {
                                var enumoutW = new object();
                                var enumoutH = new object();
                                var enumoutEna = new object();

                                if (splitedCond[1] != "!")
                                {

                                    try
                                    {
                                        enumoutW = Enum.Parse(typeof(EngineData.DO), splitedCond[1]);
                                    }
                                    catch (Exception ex)
                                    {

                                        if (ex is ArgumentException)
                                        {
                                            PrintAndLogError("The Output " + splitedCond[1] + " does Not Exist on this Machine!");
                                        };
                                    }

                                }
                                if (splitedCond[4] != "!")
                                {

                                    try
                                    {
                                        enumoutH = Enum.Parse(typeof(EngineData.DO), splitedCond[4]);
                                    }
                                    catch (Exception ex)
                                    {

                                        if (ex is ArgumentException)
                                        {
                                            PrintAndLogError("The Output " + splitedCond[4] + " does Not Exist on this Machine!");
                                        };
                                    }
                                }
                                if (splitedCond[7] != "!")
                                {

                                    try
                                    {
                                        enumoutEna = Enum.Parse(typeof(EngineData.DO), splitedCond[7]);
                                    }
                                    catch (Exception ex)
                                    {

                                        if (ex is ArgumentException)
                                        {
                                            PrintAndLogError("The Output " + splitedCond[7] + " does Not Exist on this Machine!");
                                        };
                                    }
                                }

                                if (enumoutW != null && enumoutH != null && enumoutEna != null)
                                {
                                    EngineData.DO[] enumArray = { (EngineData.DO)enumoutW, (EngineData.DO)enumoutH, (EngineData.DO)enumoutEna };
                                    _checkboxEnums.Add(box, enumArray);
                                }
                            }
                        }
                    }
                }
            }


        }

        private void Checkbox_CheckedChanged(object sender, EventArgs e)
        {
            if (timerManualMode.Enabled)
            {
                if (_checkboxEnums.ContainsKey((CheckBox)sender))
                {
                    if (_checkboxEnums[(CheckBox)sender].Length == 1)
                    {
                        if (MainEngine.MainIO.ReadDO(_checkboxEnums[(CheckBox)sender][0]))
                        {
                            MainEngine.MainIO.WriteDO(_checkboxEnums[(CheckBox)sender][0], false);
                        }
                        else if (!MainEngine.MainIO.ReadDO(_checkboxEnums[(CheckBox)sender][0]))
                        {
                            MainEngine.MainIO.WriteDO(_checkboxEnums[(CheckBox)sender][0], true);
                        }
                    }
                    if (_checkboxEnums[(CheckBox)sender].Length == 2)
                    {

                        if (MainEngine.MainIO.ReadDO(_checkboxEnums[(CheckBox)sender][0]) && !(MainEngine.MainIO.ReadDO(_checkboxEnums[(CheckBox)sender][1])))
                        {
                            MainEngine.MainIO.WriteDO(_checkboxEnums[(CheckBox)sender][0], false);
                            MainEngine.MainIO.WriteDO(_checkboxEnums[(CheckBox)sender][1], true);
                        }
                        else if (!MainEngine.MainIO.ReadDO(_checkboxEnums[(CheckBox)sender][0]) && (MainEngine.MainIO.ReadDO(_checkboxEnums[(CheckBox)sender][1])))
                        {
                            MainEngine.MainIO.WriteDO(_checkboxEnums[(CheckBox)sender][0], true);
                            MainEngine.MainIO.WriteDO(_checkboxEnums[(CheckBox)sender][1], false);
                        }

                    }
                    if (_checkboxEnums[(CheckBox)sender].Length == 3)
                    {
                        if (MainEngine.MainIO.ReadDO(_checkboxEnums[(CheckBox)sender][1]) && !(MainEngine.MainIO.ReadDO(_checkboxEnums[(CheckBox)sender][0])))
                        {
                            MainEngine.MainIO.WriteDO(_checkboxEnums[(CheckBox)sender][1], false);
                            MainEngine.MainIO.WriteDO(_checkboxEnums[(CheckBox)sender][0], true);
                            MainEngine.MainIO.WriteDO(_checkboxEnums[(CheckBox)sender][2], true);
                        }
                        else if (MainEngine.MainIO.ReadDO(_checkboxEnums[(CheckBox)sender][0]) && !(MainEngine.MainIO.ReadDO(_checkboxEnums[(CheckBox)sender][1])))
                        {
                            MainEngine.MainIO.WriteDO(_checkboxEnums[(CheckBox)sender][0], false);
                            MainEngine.MainIO.WriteDO(_checkboxEnums[(CheckBox)sender][2], false);
                            MainEngine.MainIO.WriteDO(_checkboxEnums[(CheckBox)sender][1], true);
                        }
                    }
                }


            }
        }

        private void UpdateDOEnableArray()
        {

            ManualModeSafetyMovements();

            InhibitArrayProcess();
        }

        private void tabControl2_Selected(object sender, TabControlEventArgs e)
        {
            if (tabctrlMaintenance.SelectedTab == tabParams)
            {
                UpdateCurrentModelParameters();

                if (MainEngine.DBConnection == Engine.DataSource.SQL)
                {
                    ConfigDataGridViewMeasureLimits(MainEngine.MyDB.MeasureLimits);
                }
                else
                {
                    ConfigDataGridViewMeasureLimits(MainEngine.OfflineMeasuresList);
                }

                dataGridView_Parameters.Update();
            }


            if (tabctrlMaintenance.SelectedTab == tabConsts)
            {
                UpdateCurrentMachineConsts();

                if (MainEngine.DBConnection == Engine.DataSource.SQL)
                {
                    ConfigDataGridViewConst(MainEngine.MyDB.DeviceConsts);
                }
                else
                {
                    ConfigDataGridViewConst(MainEngine.DeviceConstsList);
                }

                dataGridView_Constants.Update();
            }

            if (tabctrlMaintenance.SelectedTab == tabAlarms)
            {
                var ListaTipoAlarmes = new List<string>{

                    MainEngine.PrintGenericText("Error"),
                    MainEngine.PrintGenericText("Warning"),
                    MainEngine.PrintGenericText("Debug"),
                    MainEngine.PrintGenericText("Trace"),
                    MainEngine.PrintGenericText("Information"),
                    MainEngine.PrintGenericText("Fatal"),
                    MainEngine.PrintGenericText("All")

                };

                comboBox_Alarms.DataSource = ListaTipoAlarmes;

                comboBox_Alarms.SelectedItem = "All";
                dataGridView_Alarms.Columns["Classe"].Visible = false;
                dataGridView_Alarms.Columns["Message"].Width = 473;
                dataGridView_Alarms.Enabled = false; ;
                dataGridView_Alarms.DataSource = FilterListViewLog2(6, "");
                dataGridView_Alarms.Columns["Data"].Width = 200;
                dataGridView_Alarms.Columns["TipoAlarme"].Width = 100;
                dataGridView_Alarms.RowHeadersVisible = false;
                dataGridView_Alarms.ColumnHeadersVisible = false;
                dataGridView_Alarms.Update();


                if (dataGridView_Alarms.RowCount == 0) btnClearAlarms.Enabled = false;
                else btnClearAlarms.Enabled = true;

            }

            if (tabControlIO.SelectedTab == tabDIO)
            {
                for (int j = 0; j < MainEngine.MainIO.Dt_DO.Rows.Count; j++)
                {
                    lvDO.Items[j].Checked = MainEngine.MainIO.UpdateDIORows(j, "ValueToWrite", null, Preh.IOCycle.ReadWriteIO.ReadDO, "");
                }
            }
            else if (tabControlIO.SelectedTab == tabIAI_Move)
            {
                UpdateDOEnableArray();
            }

        }
        private void tabControlMode_Deselected(object sender, TabControlEventArgs e)
        {
            _stopTimer = true;
            timerManualMode.Stop();
        }

        private void tabControlMode_Selected(object sender, TabControlEventArgs e)
        {
            if (tabControlMode.SelectedTab == tabAuto)
            {
                WriteLastErrorAlarmBar();
                MainEngine.MainIO.WriteDO(EngineData.DO.Safety_Relay_On, true);
            }

            if (_firstAddList == true)
            {
                AddListDOItems();
                AddListDIItems();
                AddListAIItems();
                AddListAOItems();
                _firstAddList = false;
            }

            //Chosen Tab Manual Modo (IO) with machine stopped
            if (tabControlMode.SelectedTab == tabManual && backgroundWorker_AutoBM.IsBusy == false)
            {
                UpdateUserPermissions();

                //Start timer manual
                _stopTimer = false;
                timerManualMode.Start();
                if (tabControlIO.SelectedTab == tabDIO)
                {
                    for (int j = 0; j < MainEngine.MainIO.Dt_DO.Rows.Count; j++)
                    {
                        lvDO.Items[j].Checked = MainEngine.MainIO.UpdateDIORows(j, "ValueToWrite", null, Preh.IOCycle.ReadWriteIO.ReadDO, "");
                    }
                }
            }
        }
        #endregion

        #region Manual Mode 
        private void tabControlIO_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlIO.SelectedTab == tabDIO)
            {
                for (int j = 0; j < MainEngine.MainIO.Dt_DO.Rows.Count; j++)
                {
                    lvDO.Items[j].Checked = MainEngine.MainIO.UpdateDIORows(j, "ValueToWrite", null, Preh.IOCycle.ReadWriteIO.ReadDO, "");
                }
            }

            UpdateDOEnableArray();
        }

        private void checkBoxAir_CheckedChanged(object sender, EventArgs e)
        {
            if (timerManualMode.Enabled)
            {
                var output = EngineData.DO.Safety_Relay_On;

                ((CheckBox)sender).Checked = !MainEngine.MainIO.ReadDO(output);
                MainEngine.MainIO.WriteDO(output, !MainEngine.MainIO.ReadDO(output));
            }
        }



        #endregion

        //TODO: Engine: contador de peças e cycletype
        //TODO: Fazer evento para o panel
        //TODO: Make changes to use event
        private void UpdatePanelPicture(EngineData.Screens screen)
        {
            try
            {
                

                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // Base screen only hide panel picture that is the default behaviour if the next screens don't enter
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //if (Screen == EngineData.Screens.ShowBase) {
                //    #region Show Base screen
                //    panelPicture.Visible = false;
                //    #endregion
                //} else
                if (screen == EngineData.Screens.Base)
                {
                    #region Show base screen
                    panelPicture.Visible = false;
                    #endregion ShowImageVacuum
                }
                else if (screen == EngineData.Screens.ImageVacuum)
                {
                    #region ShowImageVacuum
                    panelPicture.Visible = true;
                    picInformation.Image = Properties.Resources.VacuumCleaner_Man;
                    lblPicturePanel.Text = MainEngine.PrintGenericText("Vacuum the device workplace - Press Pedal");

                   


                    #endregion ShowImageVacuum
                }
                else if (screen == EngineData.Screens.ImageSensors)
                {
                    #region Update Detections Result Images
                    panelPicture.Visible = true;

                    
                    #endregion Update Detections Result Images
                }
                else if (screen == EngineData.Screens.ImageRef)
                {
                    #region Show image to put label
                    panelPicture.Visible = true;
                    picInformation.Image = Properties.Resources.Label;
                    lblPicturePanel.Text = MainEngine.PrintGenericText("Place Label");
                    #endregion
                }
                else if (screen == EngineData.Screens.ImageLabel)
                {
                    #region ShowImageLabel
                    panelPicture.Visible = true;
                    picInformation.Image = Properties.Resources.Label;
                    lblPicturePanel.Text = " " + MainEngine.PrintGenericText("NOK Button Acknowledge");
                    #endregion ShowImageLabel
                }
                else if (screen == EngineData.Screens.ImageNestScrewSide1 
                    ||
                  screen == EngineData.Screens.ImageNestScrewSide2
              )
                {
                    #region Show screws results
                    if (!panelPicture.Visible)
                    {
                        panelPicture.Visible = true;
                        //lblPicturePanel.Text = MainEngine.TextByTag(TagInformations.ScrewBolt);
                    }

                    var screwPos = new List<Point>();
                    if (screen == EngineData.Screens.ImageNestScrewSide1)
                    {
                        picInformation.Image = Properties.Resources.Nest_Screw1;
                        screwPos.Add(new Point(85, 133));
                        screwPos.Add(new Point(198, 206));
                        screwPos.Add(new Point(288, 133));
                        screwPos.Add(new Point(400, 206));
                    }
                    else if (screen == EngineData.Screens.ImageNestScrewSide2)
                    {
                        picInformation.Image = Properties.Resources.Nest_Screw1;
                        screwPos.Add(new Point(602, 206));
                        screwPos.Add(new Point(706, 133));
                        screwPos.Add(new Point(803, 206));
                        screwPos.Add(new Point(915, 133));
                    }
                    double wfactor = (double)picInformation.Image.Width / picInformation.ClientSize.Width;
                    double hfactor = (double)picInformation.Image.Height / picInformation.ClientSize.Height;
                    double resizeFactor = Math.Max(wfactor, hfactor);
                    Size zoomedImageSize = new Size((int)(picInformation.Image.Width / resizeFactor), (int)(picInformation.Image.Height / resizeFactor));
                    Size smallSize = new Size(90, 90);
                    Point tPoint;

                    resizeFactor = 1 / resizeFactor;
                    int number = 0;
                    foreach (int x in EngineData.DPG_Result)
                    {
                        int numberControl = number + 1;
                        var control = "pic_Screw_" + numberControl.ToString();

                        PictureBox controlToChange = this.Controls.Find(control, true).FirstOrDefault() as PictureBox;

                        if (EngineData.DPG_NumberCycles > number) controlToChange.Visible = true;
                        else controlToChange.Visible = false;
                        controlToChange.Width = (int)(((double)smallSize.Width) * resizeFactor);
                        controlToChange.Height = (int)(((double)smallSize.Height) * resizeFactor);
                        tPoint = new Point();
                        tPoint.X = (int)(resizeFactor * screwPos[number].X) + (picInformation.Width - zoomedImageSize.Width) / 2 - controlToChange.Width / 2;
                        tPoint.Y = (int)(resizeFactor * screwPos[number].Y) + (picInformation.Height - zoomedImageSize.Height) / 2 - controlToChange.Height / 2;
                        controlToChange.Location = tPoint;

                        if (EngineData.DPG_Result[number] == 0) controlToChange.Image = Properties.Resources.OK;
                        else if (EngineData.DPG_Result[number] == 1) controlToChange.Image = Properties.Resources.NOK;
                        else if (EngineData.DPG_Result[number] == 2 && EngineData.DPG_NumberCycles > number)
                            controlToChange.Image = DrawText(numberControl.ToString());

                        number++;
                    }
                    #endregion
                }
                else if (screen == EngineData.Screens.BoaCam)
                {
                    #region Show BOA status
                    panelPicture.Visible = false;

                    #endregion
                }
                else if(screen == EngineData.Screens.ImageMachineResults)
                {
                    #region Machine Results
                    if (!panelPicture.Visible)
                    {
                        panelPicture.Visible = true;
                    }

                    foreach (var pic in panelDetections.Controls)
                    {
                        var picbox = (PictureBox)pic;

                        if (picbox.SizeMode == PictureBoxSizeMode.StretchImage)
                        {
                            picbox.Visible = true;
                        }
                    }


                    #endregion

                }

                if (screen != EngineData.Screens.ImageMachineResults)
                {
                    foreach (var pic in panelDetections.Controls)
                    {
                        var picbox = (PictureBox)pic;

                        if (picbox.SizeMode == PictureBoxSizeMode.StretchImage)
                        {
                            picbox.Visible = false;
                        }
                    }
                }

                if (picInformation.Visible)
                {
                    
                    var g = this.Controls.Find("txtInstructions_0", true);
                    if (g[0] is TextBox)
                    {
                        var txt = (TextBox)g[0];

                        txt.SendToBack();
                    }
                }


            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex, nameof(UpdatePanelPicture));
            }
        }

        private void InhibitArrayProcess()
        {
            try
            {
                foreach (var checkbox in _checkboxEnums.Keys)
                {
                    checkbox.Enabled = false;

                    if (_checkboxEnums[checkbox].Length == 1)
                    {
                        checkbox.Enabled = MainEngine.MainIO.InhibitOutputs[(int)_checkboxEnums[checkbox][0]];

                    }

                    if (_checkboxEnums[checkbox].Length == 2)
                    {
                        checkbox.Enabled = MainEngine.MainIO.InhibitOutputs[(int)_checkboxEnums[checkbox][0]] && MainEngine.MainIO.InhibitOutputs[(int)_checkboxEnums[checkbox][1]];

                    }

                    if (_checkboxEnums[checkbox].Length == 3)
                    {
                        checkbox.Enabled = MainEngine.MainIO.InhibitOutputs[(int)_checkboxEnums[checkbox][0]] && MainEngine.MainIO.InhibitOutputs[(int)_checkboxEnums[checkbox][1]];

                    }
                    checkbox.Checked = false;
                }
            }
            catch
            {


            }


        }

        public Bitmap DrawText(string number)
        {
            var bm = new Bitmap(46, 46);

            using (Graphics g = Graphics.FromImage(bm))
            {
                using (SolidBrush myBrush = new SolidBrush(Color.Black))
                {
                    using (Font myFont = new Font("Microsoft Sans Serif", 26))
                    {
                        g.DrawRectangle(new Pen(Color.Black, 1), 1, 1, 43, 43);
                        g.FillRectangle(new SolidBrush(Color.LightGray), 3, 3, 40, 40);
                        g.TextRenderingHint = TextRenderingHint.AntiAlias;

                        g.DrawString(number, myFont, myBrush, 8, 2);
                    }
                }
            }
            return bm;
        }

        public void CreateTxtInstructionBoxes()
        {
            try
            {
                _instructionsBuilder = new Dictionary<int, StringBuilder>();
                _instructionsByCycle = new Dictionary<int, TextBox>();

                var numOfBoxes = MainEngine.Cycles.Count;
                var maxWidth = this.groupBox2.Width - 10;
                var points = CalcPoints(6, maxWidth, numOfBoxes);
                var txtNames = new TextBox[numOfBoxes];
                for (var u = 0; u < txtNames.Count(); u++)
                {
                    txtNames[u] = new TextBox();
                }
                var i = 0;
                foreach (var txt in txtNames)
                {
                    txt.Font = new Font("Microsoft Sans Serif", 14.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
                    txt.Location = points[i];
                    txt.Multiline = true;
                    txt.Name = "txtInstructions_" + i;
                    txt.ReadOnly = true;
                    txt.ScrollBars = ScrollBars.Both;
                    txt.Size = new Size(maxWidth / numOfBoxes, this.groupBox2.Height - 30);
                    txt.TabIndex = 400;
                    txt.Visible = true;
                    txt.TabStop = false;
                    this.Controls.Add(txt);
                    this.groupBox2.Controls.Add(txt);
                    i++;
                }
                var j = 0;
                foreach (var cycle in MainEngine.Cycles)
                {
                    _instructionsBuilder.Add(cycle.CycleID, new StringBuilder());
                    _instructionsByCycle.Add(cycle.CycleID, txtNames[j]);
                    //cycle.TextBoxInstructions = ;
                    j++;
                }
            }
            catch (Exception ex)
            {

                Log.Instance.Error(ex.Message, nameof(CreateTxtInstructionBoxes));
            }
        }

        private static Point[] CalcPoints(int origin, int refwith, int numofdivs)
        {
            try
            {
                var points = new Point[numofdivs];
                points[0] = new Point(origin, 20);

                for (int i = 1; i < numofdivs; i++)
                {
                    var point = new Point((origin + refwith / numofdivs) * i, 20);
                    points[i] = point;
                }

                return points;
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex.Message, nameof(CalcPoints));
                return null;
            }
        }



        private void UpdatetxtInstructions(int cycleId, string message)
        {
            if (MainEngine.Screen == EngineData.Screens.Base)
                _instructionsBuilder[cycleId].Append(message);
            else if (MainEngine.Screen == EngineData.Screens.ImageNestScrewSide1 || MainEngine.Screen == EngineData.Screens.ImageNestScrewSide2)
                lblPicturePanel.Text = message;
        }


        private void UpdatetxtInstructionsByText(int cycleId, string text)
        {

            if (MainEngine.Screen == EngineData.Screens.Base)
                _instructionsBuilder[cycleId].Append(MainEngine.PrintText(text));
            else if (MainEngine.Screen == EngineData.Screens.ImageNestScrewSide1 || MainEngine.Screen == EngineData.Screens.ImageNestScrewSide2)
                lblPicturePanel.Text = MainEngine.PrintText(text);
        }

        private void WritetxtInstructions(int cycleId)
        {
            this.BeginInvoke((Action)(() =>
            {
                _instructionsByCycle[cycleId].Text = _instructionsBuilder[cycleId].ToString();
                _instructionsBuilder[cycleId].Clear();
            }));
        }

        private void UpdateStepBox(string step)
        {
            try
            {
                this.BeginInvoke((Action)(() => { toolStripStep_BM.Text = step; }));
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null) throw ex.InnerException;
            };
        }

        void UpdatelblPicturePanel(int cycleId, Color color)
        {
            this.BeginInvoke((Action)(() => { _instructionsByCycle[cycleId].BackColor = color; }));
        }

        private bool dgvIAIsMaintenanceConfig()
        {
            //return true;
            try
            {
                dgvIAIsMaintenance.ColumnCount = 0;
                dgvIAIsMaintenance.DefaultCellStyle.Font = new Font("Verdana", 7F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                dgvIAIsMaintenance.ColumnHeadersDefaultCellStyle.Font = new Font("Verdana", 7F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                dgvIAIsMaintenance.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvIAIsMaintenance.RowHeadersVisible = false;
                dgvIAIsMaintenance.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                //IMAGE 
                var img = new DataGridViewImageColumn();
                dgvIAIsMaintenance.Columns.Add(img);
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Width = 60;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].ReadOnly = true;
                img.HeaderText = "Model";

                //Connection Settings
                dgvIAIsMaintenance.ColumnCount = dgvIAIsMaintenance.ColumnCount + 1;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Name = "Settings";
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Width = 70;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].ReadOnly = true;

                //Drive ID
                dgvIAIsMaintenance.ColumnCount = dgvIAIsMaintenance.ColumnCount + 1;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Name = "DriveID";
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Visible = false;

                //Axis ID
                dgvIAIsMaintenance.ColumnCount = dgvIAIsMaintenance.ColumnCount + 1;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Name = "AxisID";
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Visible = false;

                //Positions
                var cmb = new DataGridViewComboBoxColumn
                {
                    HeaderText = "Positions List",
                    Name = "cmbPositions"
                };
                cmb.Items.Add("Test");
                cmb.Width = 150;
                cmb.FlatStyle = FlatStyle.System;
                dgvIAIsMaintenance.Columns.Add(cmb);

                //Position ID
                dgvIAIsMaintenance.ColumnCount = dgvIAIsMaintenance.ColumnCount + 1;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Name = "ID";
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Width = 25;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].ReadOnly = false;

                //Target Pos
                dgvIAIsMaintenance.ColumnCount = dgvIAIsMaintenance.ColumnCount + 1;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Name = "Position";
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].ReadOnly = false;

                //Speed
                dgvIAIsMaintenance.ColumnCount = dgvIAIsMaintenance.ColumnCount + 1;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Name = "Speed";
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].ReadOnly = false;

                //Acc
                dgvIAIsMaintenance.ColumnCount = dgvIAIsMaintenance.ColumnCount + 1;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Name = "Acc";
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].ReadOnly = false;

                //Decc
                dgvIAIsMaintenance.ColumnCount = dgvIAIsMaintenance.ColumnCount + 1;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Name = "Decc";
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].ReadOnly = false;

                //InPosBand
                dgvIAIsMaintenance.ColumnCount = dgvIAIsMaintenance.ColumnCount + 1;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Name = "InPB";
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].ReadOnly = false;

                //Push
                dgvIAIsMaintenance.ColumnCount = dgvIAIsMaintenance.ColumnCount + 1;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Name = "Push";
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].ReadOnly = false;

                //Control Flag
                dgvIAIsMaintenance.ColumnCount = dgvIAIsMaintenance.ColumnCount + 1;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Name = "Flag";
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].ReadOnly = false;

                //Init btn
                var btnInit = new DataGridViewButtonColumn
                {
                    HeaderText = "Init",
                    Text = "Init",
                    Name = "btnInit",
                    UseColumnTextForButtonValue = true
                };
                dgvIAIsMaintenance.Columns.Add(btnInit);
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Width = 50;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;

                //Home btn
                var btnHome = new DataGridViewButtonColumn
                {
                    HeaderText = "Home",
                    Text = "Home",
                    Name = "btnHome",
                    UseColumnTextForButtonValue = true
                };
                dgvIAIsMaintenance.Columns.Add(btnHome);
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Width = 50;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;

                //Move btn
                var btnMove = new DataGridViewButtonColumn
                {
                    HeaderText = "Move",
                    Text = "Move",
                    Name = "btnMoveAxis",
                    UseColumnTextForButtonValue = true
                };
                dgvIAIsMaintenance.Columns.Add(btnMove);
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].Width = 50;
                dgvIAIsMaintenance.Columns[dgvIAIsMaintenance.ColumnCount - 1].SortMode = DataGridViewColumnSortMode.NotSortable;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private bool dgvIAIsMaintenanceAddIAI(IAIModbusASCII.AxisDevice newAxis)
        {
            try
            {
                dgvIAIsMaintenance.AllowUserToAddRows = true;

                var errorCode = -1;
                try
                {
                    errorCode = newAxis.ReadCurrentAlarmCode();
                }
                catch
                {

                }

                var row = new DataGridViewRow();
                //row.ReadOnly = true;

                var st = row.DefaultCellStyle;
                if (errorCode != 0)
                {
                    st.BackColor = Color.Coral;
                    row.ErrorText = newAxis.AlarmMessage;
                }
                row.DefaultCellStyle = st;

                // Image
                Image imgScannerModel;
                var ScannerImgPath = Application.StartupPath + "\\DeviceImages\\IAI.png";
                if (System.IO.File.Exists(ScannerImgPath)) imgScannerModel = Image.FromFile(ScannerImgPath);
                else imgScannerModel = System.Drawing.SystemIcons.Exclamation.ToBitmap();
                var img = new DataGridViewImageCell();
                img.Value = imgScannerModel;
                row.Cells.Add(img);

                // Settings
                var ScannerSettings = newAxis.Device.ComPort + "\n\rBR:" + newAxis.Device.BaudRate;
                var cell = (DataGridViewCell)dgvIAIsMaintenance.Rows[0].Cells["Settings"].Clone();
                cell.Value = ScannerSettings;
                row.Cells.Add(cell);
                dgvIAIsMaintenance.Columns["Settings"].DefaultCellStyle.WrapMode = DataGridViewTriState
                    .True;

                // DriveID
                cell = (DataGridViewCell)dgvIAIsMaintenance.Rows[0].Cells["DriveID"].Clone();
                cell.Value = 0;
                row.Cells.Add(cell);

                // AxisID
                cell = (DataGridViewCell)dgvIAIsMaintenance.Rows[0].Cells["AxisID"].Clone();
                cell.Value = newAxis.ID;
                row.Cells.Add(cell);

                // Posotion ComboBox
                var cmb = new DataGridViewComboBoxCell();
                foreach (IAIModbusASCII.AxisPosition pos in newAxis.Position) cmb.Items.Add(pos.Name);
                cmb.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
                var cboxStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Traditional Arabic", 0x11, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0))),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                };
                cmb.Value = cmb.Items[0];
                cmb.Style = cboxStyle;
                row.Cells.Add(cmb);

                // Position ID
                cell = (DataGridViewCell)dgvIAIsMaintenance.Rows[0].Cells["ID"].Clone();
                cell.Value = newAxis.Position[0].ID;
                row.Cells.Add(cell);

                // Position
                cell = (DataGridViewCell)dgvIAIsMaintenance.Rows[0].Cells["Position"].Clone();
                cell.Value = newAxis.Position[0].TargetPosition;
                row.Cells.Add(cell);

                // Speed
                cell = (DataGridViewCell)dgvIAIsMaintenance.Rows[0].Cells["Speed"].Clone();
                cell.Value = newAxis.Position[0].Speed;
                row.Cells.Add(cell);

                // Accelaration
                cell = (DataGridViewCell)dgvIAIsMaintenance.Rows[0].Cells["Acc"].Clone();
                cell.Value = newAxis.Position[0].Acceleration;
                row.Cells.Add(cell);

                // Deccelaration
                cell = (DataGridViewCell)dgvIAIsMaintenance.Rows[0].Cells["Decc"].Clone();
                cell.Value = newAxis.Position[0].Decceleration;
                row.Cells.Add(cell);

                // InPosBand
                cell = (DataGridViewCell)dgvIAIsMaintenance.Rows[0].Cells["InPB"].Clone();
                cell.Value = newAxis.Position[0].Inposband;
                row.Cells.Add(cell);

                // Push
                cell = (DataGridViewCell)dgvIAIsMaintenance.Rows[0].Cells["Push"].Clone();
                cell.Value = newAxis.Position[0].PushCurrentLimiting;
                row.Cells.Add(cell);

                // Flag
                cell = (DataGridViewCell)dgvIAIsMaintenance.Rows[0].Cells["Flag"].Clone();
                cell.Value = newAxis.Position[0].ControlFlag;
                row.Cells.Add(cell);

                var btn = (DataGridViewButtonCell)dgvIAIsMaintenance.Rows[0].Cells["btnMoveAxis"].Clone();
                row.Cells.Add(btn);

                row.MinimumHeight = 45;
                dgvIAIsMaintenance.Rows.Add(row);

                dgvIAIsMaintenance.AllowUserToAddRows = false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private void dgvIAIsMaintenance_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var cols = (sender as DataGridView).Columns;
            if (e.RowIndex >= 0)
            {
                var dg = sender as DataGridView;
                var tbDriveID = dg.Rows[e.RowIndex].Cells["DriveID"] as DataGridViewTextBoxCell;
                var tbAxisID = dg.Rows[e.RowIndex].Cells["AxisID"] as DataGridViewTextBoxCell;

                int driveID = Convert.ToInt16(tbDriveID.Value);
                int axisID = Convert.ToInt16(tbAxisID.Value);

                if (e.ColumnIndex == 0)
                {
                    MainEngine.IAIs[driveID].Axis[axisID].AlarmReset();
                }
                else if (e.ColumnIndex == cols["btnInit"].Index)
                {
                    MainEngine.IAIs[driveID].Axis[axisID].MotorInit();
                }
                else if (e.ColumnIndex == cols["btnHome"].Index)
                {
                    MainEngine.IAIs[driveID].Axis[axisID].HomeAndWait();
                }
                else if (e.ColumnIndex == cols["btnMoveAxis"].Index)
                {
                    MainEngine.IAIs[driveID].Axis[axisID].MoveAndWait(
                        Convert.ToInt16(dg.Rows[e.RowIndex].Cells["Position"].Value),
                        10000,
                        false,
                        Convert.ToUInt32(dg.Rows[e.RowIndex].Cells["Speed"].Value),
                        Convert.ToUInt16(dg.Rows[e.RowIndex].Cells["Acc"].Value)
                    );
                }
                else if (e.ColumnIndex == 2)
                {
                    var cmb = dg.Rows[e.RowIndex].Cells["cmbPositions"] as DataGridViewComboBoxCell;
                    var position = (int)cmb.Items.IndexOf(cmb.Value);
                    var alarm = -1;

                    MainEngine.IAIs[driveID].AlarmReset();
                    alarm = MainEngine.IAIs[driveID].Axis[axisID].ReadCurrentAlarmCode();
                    if (MainEngine.IAIs[driveID].ServoOn(false)) MainEngine.IAIs[driveID].MotorInit();
                    MainEngine.IAIs[driveID].HomeAndWait();

                    if (alarm == 0)
                    {
                        MainEngine.IAIs[driveID].MoveActuator(MainEngine.IAIs[driveID].Axis[axisID].Position[position].TargetPosition);
                    }
                }
            }
        }
        private void dgvIAIsMaintenance_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if ((sender as DataGridView).CurrentCell.ColumnIndex == 4 && e.Control is ComboBox)
            {
                (e.Control as ComboBox).SelectedIndexChanged += IAIComboBoxPosition_Changed;
            }
        }
        private void IAIComboBoxPosition_Changed(object sender, EventArgs e)
        {
            var dg = (sender as DataGridViewComboBoxEditingControl).EditingControlDataGridView;
            var Position_Idx = (sender as ComboBox).SelectedIndex;
            var rowIndex = dg.CurrentCellAddress.Y;

            var tbDriveID = dg.Rows[rowIndex].Cells["DriveID"] as DataGridViewTextBoxCell;
            var tbAxisID = dg.Rows[rowIndex].Cells["AxisID"] as DataGridViewTextBoxCell;

            int driveID = Convert.ToInt16(tbDriveID.Value);
            int axisID = Convert.ToInt16(tbAxisID.Value);

            dg.Rows[rowIndex].Cells["ID"].Value = MainEngine.IAIs[driveID].Axis[axisID].Position[Position_Idx].ID;
            dg.Rows[rowIndex].Cells["Position"].Value = MainEngine.IAIs[driveID].Axis[axisID].Position[Position_Idx].TargetPosition;
            dg.Rows[rowIndex].Cells["Speed"].Value = MainEngine.IAIs[driveID].Axis[axisID].Position[Position_Idx].Speed;
            dg.Rows[rowIndex].Cells["Acc"].Value = MainEngine.IAIs[driveID].Axis[axisID].Position[Position_Idx].Acceleration;
            dg.Rows[rowIndex].Cells["Decc"].Value = MainEngine.IAIs[driveID].Axis[axisID].Position[Position_Idx].Decceleration;
            dg.Rows[rowIndex].Cells["InPB"].Value = MainEngine.IAIs[driveID].Axis[axisID].Position[Position_Idx].Inposband;
            dg.Rows[rowIndex].Cells["Push"].Value = MainEngine.IAIs[driveID].Axis[axisID].Position[Position_Idx].PushCurrentLimiting;
            dg.Rows[rowIndex].Cells["Flag"].Value = MainEngine.IAIs[driveID].Axis[axisID].Position[Position_Idx].ControlFlag;
        }

        private void FormInterface_ClearInstructionText(int cycleId)
        {
            _instructionsByCycle[cycleId].Clear();
        }
        private void FormInterface_ShowPanel(EngineData.Screens panel)
        {
            MainEngine.Screen = panel;

            //UpdatePanelPicture(MainEngine.Screen);
        }
        private void FormInterface_ChangeRef(string reference)
        {
            //       UpdatePanelPicture(MainEngine.Screen);
            try
            {
                if (MainEngine.dbLoadReferences())
                {

                    var refer = MainEngine.MyDB.References.Find(r => r.RefPreh == reference);

                    if (refer != null)
                    {
                        //var rowindexRef = MainEngine.dsRef.Tables[0].Rows.IndexOf(MainEngine.dsRef.Tables[0].Select("ID_Ref = " + reference)[0]);
                        MainEngine.SelectedRefPreh = refer.RefPreh;
                        MainEngine.ActualIDRef = refer.IDRef;
                        MainEngine.RefDescription = refer.RefDescription;
                        //MainEngine.ParameterProfile = MainEngine.dsRef.Tables[0].Rows[rowindexRef]["ParameterProfile"].ToString();
                        EngineData.PrehRef = refer.RefPreh;
                    }
                    else
                    {
                        MessageBox.Show(MainEngine.PrintGenericText("The Reference " + reference + " Doesn't Exist!"));
                    }
                }
            }
            catch (Exception)
            {

                MainEngine.RefDescription = string.Empty;
            }

            UpdateCurrentModelParameters();
        }




        private void FormInterface_UpdateBoaState(bool state)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((Action)(() =>
                {
                    FormInterface_UpdateBoaState(state);
                }));
                return;
            }
        }

        private void backgroundWorker_Auto_DoWork(object sender, DoWorkEventArgs e)
        {
            if (comboBoxModelo.SelectedIndex != 0 || MainEngine.HasAutoRef == true)
            {
                Control.CheckForIllegalCrossThreadCalls = false;

                Engine.CountPartOK = 0;
                toolStripCountParts.Text = (Engine.CountPartOK).ToString();

                comboBoxModelo.Enabled = false;

                while (MainEngine.CurrentStatus != Engine.ENUM_Cycle.Manual)
                {
                    MainEngine.RunCycles();

                    Thread.Sleep(50);
                }
            }
        }



        private void backgroundWorker_Auto_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //first Cycle
            Engine.HomePositionCompleted = false;
            comboBoxModelo.Enabled = true;
            btnStopAuto.Enabled = false;
        }

        private void tabDIO_Resize(object sender, EventArgs e)
        {

            lvDI.Width = this.Width / 2 - 80;
            lvDO.Width = this.Width / 2 - 40;
            lvDO.Left = this.Width / 2 - 40;

        }

        private void tabAIO_Resize(object sender, EventArgs e)
        {
            lvAI.Width = this.Width / 2 - 80;
            lvAO.Width = this.Width / 2 - 40;
            lvAO.Left = this.Width / 2 - 40;
        }


        private void lvAO_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            MainEngine.MainIO.StopReadAO = true;
        }
        private void lvAO_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            MainEngine.MainIO.NeedToWriteAO = true;
        }

        private void Dt_AO_RowChanged(object sender, DataRowChangeEventArgs e)
        {
            var outvar = 0.0;
            EngineData.AO v2Change;
            //MainEngine.MainIO.StopReadAO = true;
            if (double.TryParse(e.Row["Value"].ToString(), out outvar) && Enum.TryParse<EngineData.AO>(e.Row["AOName"].ToString(), out v2Change))
            {
                MainEngine.MainIO.StopReadAO = false;
            }
        }

        private void btnVacuumCleaner_Click(object sender, EventArgs e)
        {
            if (MainEngine.MainIO.ReadDO(EngineData.DO.Sol_Vacuum_Cleaner))
            {
                MainEngine.MainIO.WriteDO(EngineData.DO.Sol_Vacuum_Cleaner, false);
            }
            else
            {
                MainEngine.MainIO.WriteDO(EngineData.DO.Sol_Vacuum_Cleaner, true);
            }
        }


        public void ManualModeSafetyMovements()
        {

            //if (MainEngine.MainIO.InhibitOutputs[(int)EngineData.DO.Sol_Cyl_Lock_W_EATC] == true && MainEngine.MainIO.InhibitOutputs[10] == true)
            //{

            //    //checkBoxProy_Sup.Enabled = true;
            //}
            //else
            //{

            //    //checkBoxProy_Sup.Enabled = false;
            //}


        }



        private void btn_Save_Click(object sender, EventArgs e)
        {
            SaveConsts();
        }

        private void dataGridView_Constants_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            btnDiscardConsts.Enabled = true;
            dataGridView_Constants.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = Color.Red;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MainEngine.dbLoadConsts();

            if (MainEngine.DBConnection == Engine.DataSource.SQL)
            {
                ConfigDataGridViewConst(MainEngine.MyDB.DeviceConsts);
            }
            else
            {
                ConfigDataGridViewConst(MainEngine.DeviceConstsList);
            }


            ResetDataGridView(false, ref dataGridView_Constants);
            btnDiscardConsts.Enabled = false;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox_Alarms.SelectedIndex)
            {
                case 0: dataGridView_Alarms.DataSource = FilterListViewLog2(0, "ERROR|"); break;
                case 1: dataGridView_Alarms.DataSource = FilterListViewLog2(1, "WARN|"); break;
                case 2: dataGridView_Alarms.DataSource = FilterListViewLog2(2, "DEBUG|"); break;
                case 3: dataGridView_Alarms.DataSource = FilterListViewLog2(3, "TRACE|"); break;
                case 4: dataGridView_Alarms.DataSource = FilterListViewLog2(4, "INFO|"); break;
                case 5: dataGridView_Alarms.DataSource = FilterListViewLog2(5, "FATAL|"); break;
                case 6: dataGridView_Alarms.DataSource = FilterListViewLog2(6, ""); break;
            }

            dataGridView_Alarms.RowHeadersVisible = false;
            dataGridView_Alarms.ColumnHeadersVisible = false;
            dataGridView_Alarms.Update();
        }

        public List<StructNLog> FilterListViewLog2(int index, string FilterType)
        {
            var listErros = new List<string>();
            var List_SNlog = new List<StructNLog>();
            StructNLog SNlog;

            var List_ReadAllLines = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + @"\Main\NLog\Logfile.txt").ToList();

            //if (comboBox2.SelectedIndex.Equals(index))
            if (index != 6)
            {
                foreach (var x in List_ReadAllLines)
                {
                    if (x.Contains(FilterType))
                    {
                        SNlog = new StructNLog
                        {
                            Data = x.Split('|')[0].ToString(),
                            TipoAlarme = x.Split('|')[1].ToString(),
                            Classe = x.Split('|')[2].ToString(),
                            Message = x.Split('|')[3].ToString()
                        };

                        List_SNlog.Add(SNlog);
                    }

                }
            }
            else
            {
                if (index == 6)
                {
                    foreach (string s in List_ReadAllLines)
                    {
                        SNlog = new StructNLog
                        {

                            Data = s.Split('|')[0].ToString(),
                            TipoAlarme = s.Split('|')[1].ToString(),
                            Classe = s.Split('|')[2].ToString(),
                            Message = s.Split('|')[3].ToString()
                        };

                        List_SNlog.Add(SNlog);
                    }
                }
            }


            return List_SNlog;
        }

        private void txtDeviceStatus_MouseDoubleClick(object sender, MouseEventArgs e)
        {

            var textb = (TextBox)sender;

            var alarm = _activeAlarms.Find(a => a == textb.Text);

            if (alarm != null)
            {
                _activeAlarms.Remove(alarm);
                _acknoladgedAlarms.Add(alarm);

            }

            WriteLastErrorAlarmBar();
        }

        public void WriteLastErrorAlarmBar()
        {

            var loaded = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + @"\Main\NLog\Logfile.txt").ToList();

            foreach (var load in loaded)
            {
                var acknoladged = _acknoladgedAlarms.Find(a => a == load);
                if (acknoladged == null)
                {
                    if (load.Contains("|ERROR"))
                    {
                        var dif = _activeAlarms.Find(a => a == load);
                        if (dif == null)
                        {
                            _activeAlarms.Add(load);
                        }
                    }
                }
            }


            try
            {
                txtDeviceStatus.Text = _activeAlarms.Last(); ;
                txtDeviceStatus.ForeColor = Color.Red;
                txtDeviceStatus.BackColor = SystemColors.Control;
            }
            catch (Exception)
            { txtDeviceStatus.Text = "No Error"; txtDeviceStatus.ForeColor = Color.Black; }

        }

        private void btnSaveAlarms_Click(object sender, EventArgs e)
        {

            var fileDialog = new SaveFileDialog();
            fileDialog.FileName = "Logfile.txt";
            //fileDialog.FileName = AppDomain.CurrentDomain.BaseDirectory + @"Main\NLog\Logfile.txt";


            fileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + @"Main\NLog\";
            fileDialog.Title = "Save text Files";
            fileDialog.CheckFileExists = false;
            fileDialog.CheckPathExists = true;
            fileDialog.DefaultExt = "txt";
            fileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            fileDialog.FilterIndex = 0;
            fileDialog.RestoreDirectory = false;


            //=============================

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(fileDialog.FileName))
                {
                    var List_ReadAllLines = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + @"\Main\NLog\Logfile.txt").ToList();
                    File.WriteAllLines(fileDialog.FileName, List_ReadAllLines);
                }
                else
                    File.Copy(AppDomain.CurrentDomain.BaseDirectory + @"Main\NLog\Logfile.txt", fileDialog.FileName);


            }





        }

        private void btnClearAlarms_Click(object sender, EventArgs e)
        {
            var listavazia = new List<string>();
            File.WriteAllLines(AppDomain.CurrentDomain.BaseDirectory + @"\Main\NLog\Logfile.txt", listavazia);
            dataGridView_Alarms.DataSource = null;
            dataGridView_Alarms.Update();

            btnClearAlarms.Enabled = false;
        }


        private void comboBox_RFID_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (RFID RFIDDevice in MainEngine.RFIDs)
            {
                if (RFIDDevice.ReaderName.Equals(comboBox_RFID.Text))
                {
                    if (RFIDDevice.Connected)
                    {
                        btnInventoryRFID.Enabled = true;
                        lblActivity.BackColor = Color.Lime;
                        cboLevelRFID.Text = RFIDDevice.Level.ToString();
                    }
                    else
                    {
                        btnInventoryRFID.Enabled = false;
                        lblActivity.BackColor = Color.Red;
                    }

                    lstResult.Items.Clear();
                    txtStatus.Text = "";
                    break;
                }

            }
        }

        private void btnInventoryRFID_Click(object sender, EventArgs e)
        {
            var level = 10;
            if (chkClear.Checked) lstResult.Items.Clear();
            if (cboLevelRFID.SelectedIndex >= 0)
            {
                level = int.Parse(cboLevelRFID.Text);
            }


            foreach (RFID RFIDDevice in MainEngine.RFIDs)
            {
                if (RFIDDevice.ReaderName.Equals(comboBox_RFID.Text))
                {
                    RFID_ReadInventory(RFIDDevice, level);
                    break;
                }
            }

        }

        private bool RFID_ReadInventory(RFID myRFID, int level)
        {
            var flag = false;

            if (myRFID.ReadTags(true, 2, level))
            {
                txtStatus.Text = "Read rounds: " + myRFID.InventoryInfo.roundsDone +
                                "  Collisions: " + myRFID.InventoryInfo.collisions +
                                "  Quality: " + myRFID.InventoryInfo.Q +
                                "  Tags found: " + myRFID.InventoryInfo.numTagsFound +
                                "  Tags in memory: " + myRFID.InventoryInfo.numTagsMem;

                lstResult.Items.Add("Read rounds: " + myRFID.InventoryInfo.roundsDone);
                lstResult.Items.Add("Collisions: " + myRFID.InventoryInfo.collisions);
                lstResult.Items.Add("Quality: " + myRFID.InventoryInfo.Q);
                lstResult.Items.Add("Tags found: " + myRFID.InventoryInfo.numTagsFound);
                lstResult.Items.Add("Tags in memory: " + myRFID.InventoryInfo.numTagsMem);

                for (int n = 0; n < myRFID.InventoryInfo.numTagsFound; n++)
                {
                    lstResult.Items.Add("");
                    lstResult.Items.Add("-------------------- Tag " + (n + 1).ToString() + " -------------------");
                    lstResult.Items.Add(" [TIDEX]: " + myRFID.InventoryInfo.TIDEX[n]);
                    lstResult.Items.Add(" [RSSI]: " + myRFID.InventoryInfo.RSSI[n]);
                    lstResult.Items.Add("");
                    lstResult.Items.Add(" TraceNr (UID): " + myRFID.InventoryInfo.EPCuid[n]);
                    lstResult.Items.Add(" PrehRef: " + myRFID.InventoryInfo.EPCref[n]);
                    lstResult.Items.Add(" Date: " + myRFID.InventoryInfo.EPCdate[n]);
                    lstResult.Items.Add(" Tray counter: " + myRFID.InventoryInfo.EPCcontrol[n]);
                    lstResult.Items.Add(" User data: " + myRFID.InventoryInfo.USERdata[n]);
                }
                lblActivity.BackColor = Color.Green;

                flag = true;
            }
            else
            {
                lblActivity.BackColor = Color.Red;
                flag = false;
            }
            lstResult.Items.Add("------------------------------------------------");
            lblActivity.BackColor = Color.Yellow;

            return flag;
        }

        private void btnClearListRFID_Click(object sender, EventArgs e)
        {
            lstResult.Items.Clear();
            txtStatus.Text = "";
        }

       

        private void panelDetections_Resize(object sender, EventArgs e)
        {
            defaultPdwidth = 755;
            defaultPdHeight = 252;
            defaultRbSize = new Size(46, 46);

            if (_resultBoxPos.Count == 0)
            {
                foreach (var item in panelDetections.Controls)
                {
                    if (item is PictureBox)
                    {
                        var picb = (PictureBox)item;

                        if (picb.SizeMode == PictureBoxSizeMode.StretchImage)
                        {
                            if (!_resultBoxPos.ContainsKey(picb))
                            {
                                _resultBoxPos.Add(picb,picb.Location);

                            }
                        }
                    }
                }
            }

            var wfactor = (double)panelDetections.Width / defaultPdwidth;
            var hfactor = (double)panelDetections.Height / defaultPdHeight;
            var resizeFactor = Math.Max(wfactor, hfactor);

            foreach (var item in _resultBoxPos.Keys)
            {
                if (MainEngine.Cycles.Count > 0)
                {
                    item.Visible = true;
                }
                else
                {
                    item.Visible = false;
                }
                item.Width = (int)(((double)defaultRbSize.Width) * resizeFactor);
                item.Height = (int)(((double)defaultRbSize.Height) * resizeFactor);

                var defaultLoc = new Point();
                _resultBoxPos.TryGetValue(item, out defaultLoc);

                var tPoint = new Point
                {
                    X = (int)(resizeFactor * defaultLoc.X),
                    Y = (int)(resizeFactor * defaultLoc.Y)
                };
                item.Location = tPoint;
            }
        }

        private void checkBoxProy_Sup_CheckedChanged(object sender, EventArgs e)
        {
            

            if (MainEngine.MainIO.ReadDI(EngineData.DI.Cyl_Proy_H) && !MainEngine.MainIO.ReadDI(EngineData.DI.Cyl_Proy_W))
            {
                MainEngine.MainIO.WriteDO(EngineData.DO.Sol_Cyl_Enable_Proy_W, true);
                MainEngine.MainIO.WriteDO(EngineData.DO.Sol_Cyl_Proy_H, false);
                MainEngine.MainIO.WriteDO(EngineData.DO.Sol_Cyl_Proy_W, true);
            }
            else
                MainEngine.MainIO.WriteDO(EngineData.DO.Sol_Cyl_Enable_Proy_W, true);
                MainEngine.MainIO.WriteDO(EngineData.DO.Sol_Cyl_Proy_H, true);
                MainEngine.MainIO.WriteDO(EngineData.DO.Sol_Cyl_Proy_W, false);
            
        }
    }


    public class StructNLog
    {
        string _data;
        string _tipoAlarme;
        string _classe;
        string _message;

        public string Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public string TipoAlarme
        {
            get { return _tipoAlarme; }
            set { _tipoAlarme = value; }
        }

        public string Classe
        {
            get { return _classe; }
            set { _classe = value; }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

    }


}