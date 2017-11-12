using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NLog;
using System.Timers;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using PPDBAccess;
using System.Net.NetworkInformation;

namespace Preh
{
    
    public class Engine
    {
        private XDocument ConfigFile;
        public static event Action<string, string> ScannerNewData;
        public static event Action<string> ScannerTimeout;
        public static event Action<string, string[]> RFIDNewData;

        public static event Action<string, string> EngineError;

        //public static short CycleType = 1; // 1-1ºCycle, 2-2ºCycle, 3-Cycle
        public static int userLevel = 0;                  // User access 
        public static bool AspirationOK = false;
        public static bool HomePositionCompleted = false;
        public static int CountPartOK = 0;
        public static string strUserName = "";        
        private static object _locker;

        #region fields
        private readonly string _xmlPath;

        public enum DataSource { SQL, XML }
        //TODO: make it private and handled it in a different way. Was placed like this because of dependecies with HMI
        public PPTraceStation MyDB;
        public PPBaseDB MyDBGeneric;
        public DataSet dsInit;
        public bool[] doEnable;
        //TODO: make it private and handled it in a different way. Was placed like this because of dependecies with HMI
        public enum ENUM_Cycle
        {
            Manual = 0,
            Home = 1,
            Auto = 2,
            Calibration = 3,
            Samples = 4,
            ReadyToStart = 5,
        }
        private ENUM_Cycle _engineCurrentStatus = ENUM_Cycle.Manual;
        //TODO: make it private and handled it in a different way. Was placed like this because of dependecies with HMI
        public DataSource DBConnection;
        public DataSource UsersDataSource;
        public DataSource RefsDataSource;
        public DataSource LimitsDataSource;
        public DataSource ConstsDataSource;
        public DataSource LanguageDataSource;
        public IOCycle MainIO;
        public List<Scanner> Scanners;
        public List<IAIModbusASCII> IAIs;
        public List<RFID> RFIDs;
        public List<VisionSystemBOA> BOAs;

        #endregion

        #region Properties
        ///<summary><b>Address of the necessary XML PREH file usually INIT.xml file</b></summary>
        ///<remarks>Address of the necessary XML PREH file usually INIT.xml file</remarks>
        public string ConfigurationFileAddress { get; set; }

        ///<summary><b>Language of the machine by default "EN"</b></summary>
        ///<remarks>Language of the machine by default "EN"</remarks>
        public Language ActualLang { get; set; }

        ///<summary><b>ID on the database of the PREH device </b></summary>
        ///<remarks>ID on the database of the PREH devic</remarks>
        public int WSid { get; set; }

        ///<summary><b>Parameters BD DataSet </b></summary>
        ///<remarks>Parameters BD DataSet </remarks>
        public List<MeasureLimit> OfflineMeasuresList { get; set; }

        ///<summary><b>Parameters BD DataSet </b></summary>
        ///<remarks>Parameters BD DataSet </remarks>
        public List<Reference> RefsList { get; set; }

        public List<DeviceConst> DeviceConstsList { get; set; }


        ///<summary><b>Machine uses Calibration </b></summary>
        ///<remarks>Machine uses Calibration </remarks>
        public bool Calibration { get; set; }

        ///<summary><b>Machine uses Automatic Reference </b></summary>
        ///<remarks>Machine uses Automatic Reference </remarks>
        public bool HasAutoRef
        {
            get {return Cycles[0].AutoRef; }
            //set { _hasAutoRef = value; }
        }
       
        ///<summary><b>Machine StationName </b></summary>
        ///<remarks>Machine StationName </remarks>
        public string StationName { get; set; }

        public string RefDescription { get; set; }

        ///<summary><b>Actual  ID of the reference </b></summary>
        ///<remarks>Actual  ID of the reference </remarks>     
        public int ActualIDRef { get; set; }

        ///<summary><b>Selected Model of the reference </b></summary>
        ///<remarks>Selected Model of the reference  </remarks>     
        private string _selectedModel;
        public string SelectedRefPreh
        {
            get
            {
                return _selectedModel;
            }
            set
            {
                _selectedModel = value;
                foreach (var item in this.Cycles)
                {
                    item.PrehRef = value;
                }
            }
        }
        public int SelectedIdRef
        {
            get
            {
                var reference = RefsList.Find(r => r.RefPreh == SelectedRefPreh);
                if (reference != null)
                {
                    return reference.IDRef;
                }
                else
                {
                    return 0;
                }


            }
        }

        ///<summary><b>MAchine Air Pressure Ok  </b></summary>
        ///<remarks>MAchine Air Pressure Ok  </remarks>  
        public bool AirPressureOk { get; set; }

        ///<summary><b>Machine EmegencyCircuit ON  </b></summary>
        ///<remarks>Machine EmegencyCircuit ON </remarks>     
        public bool SafetyCircuit { get; set; }

        ///<summary><b>Machine sends email  </b></summary>
        ///<remarks>Machine sends email </remarks>     
        public bool SendEmail { get; set; }

        //TODO: Change to Camel Casing no Hungarian
        ///<summary><b>Parameter Profile used to compare with references  </b></summary>
        ///<remarks>Parameter Profile used to compare with references </remarks>
        //public string ParameterProfile { get; set; }

        ///<summary><b>Machine Current Status based on ENUM  </b></summary>
        ///<remarks>Machn</remarks>
        ///
        public bool HasVacuum { get; set; }
        public System.Timers.Timer RfidTimer { get; set; }
        public ENUM_Cycle CurrentStatus { get { return _engineCurrentStatus; } set { _engineCurrentStatus = value; UpdateCycleStatus(); } }
        public List<CycleFactory> Cycles { get; set; }
        public EngineData.Screens Screen { get; set; }
        
        public SqlConnectionStringBuilder SbDefaultConnString { get; set; }
        public SqlConnectionStringBuilder SbProdGenericsConnString { get; set; }

        public string UserListGroup { get; set; }
        public int CurrentAccessMask { get; set; }
        public string CurrentUser { get; set; }
        public bool AutoInsertNewMessages { get; set; }
        public bool HasCycleTypes { get; set; }
        public int CurrentUserID { get; internal set; }
        public string ViewForReference { get { return Cycles[0].ViewForReference; } }
        public bool LoadXMLError { get; set; }


        #endregion

        ///<summary><b>Creates an Engine for the machine. It requires to have the XML Address for the INIT.xml file</b></summary>
        ///<remarks>Creates an Engine PREH for the machine. It requires to have the XML Address for the INIT.xml file</remarks>
        public Engine(string xmlAddress)
        {
            BOAs = new List<VisionSystemBOA>();
            IAIs = new List<IAIModbusASCII>();
            Scanners = new List<Scanner>();
            RFIDs = new List<RFID>();
            Cycles = new List<CycleFactory>();
            _xmlPath = AppDomain.CurrentDomain.BaseDirectory + @"XML Files\" + xmlAddress;

            DeviceConstsList = new List<DeviceConst>();
            OfflineMeasuresList = new List<MeasureLimit>();
            DeviceConstsList = new List<DeviceConst>();
            RefsList = new List<Reference>();

            SbDefaultConnString = new SqlConnectionStringBuilder();
            SbProdGenericsConnString = new SqlConnectionStringBuilder();

            ActualLang = Language.EN;
            try
            {
                if (ReadXmlStruct())
                {
                    LoadXMLError = false;
                }
                else
                {
                    LoadXMLError = true;
                }
                
            }
            catch (Exception ex)
            {

                Log.Instance.Error(ex.InnerException, "Error loading the XML file.");
                
            }
            HardwareInterface.TriggerScanner += Cycle_TriggerScanner;
            HardwareInterface.StopAllScanners += HardwareInterface_StopAllScanners;
            HardwareInterface.StopAllRFID += HardwareInterface_StopAllRFID;
            foreach (var scannerDevice in Scanners)
            {
                scannerDevice.newDataReceived += new Scanner.newScannerDataReceived(ScannerEventReceived);
                //ScannerTimer = new System.Timers.Timer();
                //ScannerTimer.Stop();
            }
            HardwareInterface.TriggerRFID += Cycle_TriggerRFID;
            foreach (var rfidDevice in RFIDs)
            {
                rfidDevice.newDataReceived += new RFID.newRFIDDataReceived(RFIDEventReceived);
                RfidTimer = new System.Timers.Timer();
                RfidTimer.Elapsed += (sender, e) => FireTriggersRFID(sender, e, rfidDevice.ReaderName);
            }
            _locker = new object();
        }

        private void HardwareInterface_StopAllRFID()
        {
            RfidTimer.Stop();
        }

        private void HardwareInterface_StopAllScanners()
        {
            foreach (var scanner in Scanners)
            {
                scanner.TurnScannerOff = true;
                scanner.InternalStopWatch.Reset();
            }
        }

        private void ScannerEventReceived(string scannerName, string scannerData)
        {

            try
            {

                if (!Scanners.First(a => a.Name == scannerName).TurnScannerOff)
                {
                    Scanners.First(a => a.Name == scannerName).IsReading = true;
                    if (scannerData == Scanner.NoRead)
                    {

                        if (Scanners.First(a => a.Name == scannerName).InternalStopWatch.ElapsedMilliseconds < 7000)
                        {

                            Scanners.First(a => a.Name == scannerName).Read();
                        }
                        else
                        {

                            Scanners.First(a => a.Name == scannerName).TurnScannerOff = true;
                            Scanners.First(a => a.Name == scannerName).IsReading = false;
                            Scanners.First(a => a.Name == scannerName).InternalStopWatch.Reset();
                            ScannerTimeout?.Invoke(scannerName);
                        }

                    }
                    else
                    {
                        Scanners.First(a => a.Name == scannerName).TurnScannerOff = true;
                        Scanners.First(a => a.Name == scannerName).IsReading = false;
                        Scanners.First(a => a.Name == scannerName).InternalStopWatch.Reset();
                        ScannerNewData?.Invoke(scannerName, scannerData);
                    }
                }
                else
                {
                    Scanners.First(a => a.Name == scannerName).IsReading = false;
                }


            }
            catch (Exception ex)
            {
                Log.Instance.Warn(ex);

                //throw ex.InnerException;
            }


        }

        private void Cycle_TriggerScanner(string scannerName, bool turnOn)
        {
            var scanner = Scanners.Find(s => s.Name == scannerName);

            if (turnOn)
            {
                if (scanner!= null)
                {
                    if (!Scanners.First(a => a.Name == scannerName).IsReading)
                    {
                        Scanners.First(a => a.Name == scannerName).TurnScannerOff = false;
                        Scanners.First(a => a.Name == scannerName).Read();
                        Scanners.First(a => a.Name == scannerName).InternalStopWatch.Start();
                    }
                }
                else
                {
                    EngineError?.Invoke("Start Scanner", "The Scanner " + scannerName + " does not exist!");
                }
                
            }
            else if (!turnOn)
            {

                if (scanner != null)
                {
                    Scanners.First(a => a.Name == scannerName).TurnScannerOff = false;
                }
                else
                {
                    EngineError?.Invoke("Start Scanner", "The Scanner " + scannerName + " does not exist!");
                }

                //turnScannerOff = true;

            }

        }

        //private void FireTriggers(object sender, ElapsedEventArgs e, string name) {
        //    Scanners.First(a => a.Name == name).Read();
        //}

        private void RFIDEventReceived(string RFIDName, string[] RFIDData)
        {
            try
            {
                //TODO:
                if (RFIDData[0] == "NOREAD")
                {
                    RfidTimer.Stop();
                    RfidTimer.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Warn(ex);
            }
            RFIDNewData?.Invoke(RFIDName, RFIDData);
        }

        private void Cycle_TriggerRFID(string RFIDName, bool turnOnOff)
        {
            if (turnOnOff && !RfidTimer.Enabled)
            {
                RfidTimer.Interval = 500;
                RfidTimer.Enabled = true;
                RfidTimer.Start();
            }
            else if (!turnOnOff && RfidTimer.Enabled)
            {
                RfidTimer.Stop();
                RfidTimer.Enabled = false;
            }
        }

        private void FireTriggersRFID(object sender, ElapsedEventArgs e, string name)
        {
            var rfid = RFIDs.Find(s => s.ReaderName == name);

            if (rfid != null)
            {
                RFIDs.First(a => a.ReaderName == name).ReadTags();
            }
            else
            {
                EngineError?.Invoke("Start RFID", "The RFID " + name + " does not exist!");
            }

        }

        ///         ///<summary><b>Initializes the connection of the machine with the Database/XML</b></summary>
        /// <param name="DBServer">todo: describe DBServer parameter on dbConnect</param>
        /// <param name="DBName">todo: describe DBName parameter on dbConnect</param>
        /// <param name="DBUser">todo: describe DBUser parameter on dbConnect</param>
        /// <param name="DBUserPsw">todo: describe DBUserPsw parameter on dbConnect</param>
        /// <param name="WSID">todo: describe WSID parameter on dbConnect</param>
        ///         ///<remarks>Initializes the connection of the machine with the Database/XML</remarks>
        private bool DbConnect()
        {
            try
            {
                MyDBGeneric = new PPBaseDB(SbProdGenericsConnString);

                if (!string.IsNullOrEmpty(MyDBGeneric.LastErrorDescription))
                {
                    Log.Instance.Error("Error on" + SbProdGenericsConnString.DataSource + " : " + MyDBGeneric.LastErrorDescription);
                    return false;
                }

                MyDB = new PPTraceStation(SbDefaultConnString, SbProdGenericsConnString, WSid, 1)
                {
                    SystemLanguage = ActualLang,
                    Language_AutoInsertNewMessages = AutoInsertNewMessages

                };
                MyDB.SyncDate();
                if (!string.IsNullOrEmpty(MyDB.LastErrorDescription))
                {

                    Log.Instance.Error("Error on" + SbDefaultConnString.DataSource + " : " + MyDB.LastErrorDescription);
                    return false;
                }

                if (Log.IsDebugMessages)
                {
                    Log.Instance.Info("Connected to " + SbDefaultConnString.DataSource + " " + SbDefaultConnString.InitialCatalog);
                    Log.Instance.Info("Connected to  " + SbProdGenericsConnString.DataSource + " " + SbProdGenericsConnString.InitialCatalog);
                }
                return true;
            }
            catch (Exception ex)
            {

                EngineError?.Invoke("DBConnect", ex.InnerException.Message);
                Log.Instance.Error(ex.InnerException,nameof(DbConnect));
                return false;
            }
        }

        ///<summary><b>Reads the languague file</b></summary>
        ///<remarks>Reads the languague file</remarks>
        //public bool ReadLangXml()
        //{
        //    Messages = LoadLanguage();
        //    return Messages != null;
        //}

        ///<summary><b>Reads the parameters file</b></summary>
        ///<remarks>Reads the parameters file</remarks>
        public bool dbLoadParameters()
        {
            if (LimitsDataSource == DataSource.SQL)
            {
                if (SelectedIdRef != 0)
                {
                    if (MyDB.Measures_LoadMeasureLimits(SelectedIdRef))
                    {
                        OfflineMeasuresList = MyDB.MeasureLimits;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }

            }
            else
            {
                try
                {
                    var LimitsXml = XDocument.Load("XML Files\\MeasureLimits.xml");
                    var root = LimitsXml.Root;
                    var par = root.Elements("Table");
                    var limitsXmlList = from u in par
                                        select new
                                        {
                                            ID_MeasureLimit = u.Element("ID_MeasureLimit").Value,
                                            MeasureProfile = u.Element("MeasureProfile").Value,
                                            ID_SubWS = u.Element("ID_SubWS").Value,
                                            ID_WS = u.Element("ID_WS").Value,
                                            SubWS = u.Element("SubWS").Value,
                                            MeasureName = u.Element("MeasureName").Value,
                                            MinValue = u.Element("MinValue").Value,
                                            MaxValue = u.Element("MaxValue").Value,
                                            Unit = u.Element("Unit").Value,
                                            Active = u.Element("Active").Value,
                                            Description = u.Element("Description").Value,
                                            Variant = u.Element("Variant").Value,
                                            IdRef = u.Element("IDRef").Value,
                                        };

                    OfflineMeasuresList.Clear();

                    foreach (var param in limitsXmlList)
                    {
                        var measure = new MeasureLimit
                        {
                            ID_MeasureLimit = Convert.ToInt16(param.ID_MeasureLimit),
                            MeasureProfile = param.MeasureProfile,
                            ID_SubWS = Convert.ToInt16(param.ID_SubWS),
                            ID_WS = Convert.ToInt16(param.ID_WS),
                            SubWS = Convert.ToInt16(param.SubWS),
                            MeasureName = param.MeasureName,
                            MinValue = Convert.ToInt16(param.MinValue),
                            MaxValue = Convert.ToInt16(param.MaxValue),
                            Unit = param.Unit,
                            Active = Convert.ToBoolean(param.Active),
                            Description = param.Description,
                            Variant = param.Variant,
                            ID_Ref = Convert.ToInt16(param.IdRef)

                        };

                        OfflineMeasuresList.Add(measure);
                    }

                    var filtered = OfflineMeasuresList.Where(i=> i.ID_Ref == SelectedIdRef);
                    OfflineMeasuresList = filtered.ToList();
                }
                catch (Exception ex)
                {
                    Log.Instance.Warn(ex);
                    throw;
                }
                return OfflineMeasuresList != null ? true : false;
            }


        }

        public List<DeviceConst> LoadConstantesXML()
        {
            var constsXml = XDocument.Load("XML Files\\Consts.xml");
            var root = constsXml.Root;
            var consts = root.Elements("Table");
            var DeviceConstsLista = (from u in consts
                                     select new DeviceConst
                                     {
                                         ID_Const = int.Parse(u.Element("ID_Const").Value),
                                         ID_SubWS = int.Parse(u.Element("ID_SubWS").Value),
                                         ConstName = u.Element("ConstName").Value,
                                         ConstValue = u.Element("ConstValue").Value,
                                         ConstDescription = u.Element("ConstDescription").Value,

                                     }).ToList();

            return DeviceConstsLista;
        }

        public List<MeasureLimit> LoadMeasureLimitsXML()
        {
            var limitsXml = XDocument.Load("XML Files\\MeasureLimits.xml");
            var root = limitsXml.Root;
            var limits = root.Elements("Table");
            var limitsList = (from u in limits
                                   select new MeasureLimit
                                   {
                                       ID_MeasureLimit = int.Parse(u.Element("ID_MeasureLimit").Value),
                                       MeasureProfile = u.Element("MeasureProfile").Value,
                                       ID_SubWS = int.Parse(u.Element("ID_SubWS").Value),
                                       ID_WS = int.Parse(u.Element("ID_WS").Value),
                                       SubWS = int.Parse(u.Element("SubWS").Value),
                                       MeasureName = u.Element("MeasureName").Value,
                                       MinValue = int.Parse(u.Element("MinValue").Value),
                                       MaxValue = int.Parse(u.Element("MaxValue").Value),
                                       Unit = u.Element("Unit").Value,
                                       Active = bool.Parse(u.Element("Active").Value),
                                       Description = u.Element("Description").Value,
                                       Variant = u.Element("Variant").Value,

                                   }).ToList();
            return limitsList;
        }

        ///<summary><b>Reads the parameters file</b></summary>
        ///<remarks>Reads the parameters file</remarks>
        public bool dbLoadConsts()
        {
            if (ConstsDataSource == DataSource.SQL)
            {

                MyDB.Const_LoadConstants();
                DeviceConstsList = MyDB.DeviceConsts;
                return true;


            }
            else
            {
                var constsXml = XDocument.Load("XML Files\\Consts.xml");
                var root = constsXml.Root;
                var consts = root.Elements("Table");
                var constsXmlList = from u in consts
                                    select new
                                    {
                                        ID_Const = u.Element("ID_Const").Value,
                                        ID_SubWS = u.Element("ID_SubWS").Value,
                                        ConstName = u.Element("ConstName").Value,
                                        ConstValue = u.Element("ConstValue").Value,
                                        ConstDescription = u.Element("ConstDescription").Value,

                                    };

                DeviceConstsList.Clear();

                foreach (var cons in constsXmlList)
                {
                    var devConst = new DeviceConst
                    {
                        ID_Const = Convert.ToInt16(cons.ID_Const),
                        ID_SubWS = Convert.ToInt16(cons.ID_SubWS),
                        ConstName = cons.ConstName,
                        ConstValue = cons.ConstValue,
                        ConstDescription = cons.ConstDescription,

                    };

                    DeviceConstsList.Add(devConst);
                }

                return DeviceConstsList != null ? true : false;
            }


        }

        ///<summary><b>Reads the parameters file</b></summary>
        ///<remarks>Reads the parameters file</remarks>
        public bool dbLoadReferences()
        {
            if (RefsDataSource == DataSource.SQL)
            {
                if (MyDB.Refs_LoadReferences("vw_GetReferenceOP05"))
                {
                    RefsList = MyDB.References;
                    return true;
                }
                else
                {
                    return false;
                }

            }
            else
            {

                try
                {


                    var refsXml = XDocument.Load("XML Files\\Refs.xml");
                    var root = refsXml.Root;
                    var refs = root.Elements("Table");
                    var refsXmlList = from r in refs
                                      select new
                                      {
                                          IDRef = r.Element("IDRef").Value,
                                          RefPreh = r.Element("RefPreh").Value,
                                          IDRefProfile = r.Element("IDRefProfile").Value,
                                          IDRefSeqGroup = r.Element("IDRefSeqGroup").Value,
                                          Active = r.Element("Active").Value,
                                          RefDescription = r.Element("RefDescription").Value,
                                          RefShortType = r.Element("RefShortType").Value,
                                          ExtraCode = r.Element("ExtraCode").Value,

                                      };


                    foreach (var re in refsXmlList)
                    {
                        var reference = new Reference
                        {
                            IDRef = Convert.ToInt16(re.IDRef),
                            IDRefProfile = Convert.ToInt16(re.IDRefProfile),
                            IDRefSeqGroup = Convert.ToInt16(re.IDRefSeqGroup),
                            RefPreh = re.RefPreh,
                            Active = Convert.ToBoolean(re.Active),
                            RefDescription = re.RefDescription,
                            RefShortType = re.RefShortType,
                            ExtraCode = re.ExtraCode,
                        };

                        RefsList.Add(reference);
                    }

                }
                catch (Exception ex)
                {
                    Log.Instance.Error(ex.InnerException,"Failed Loading Refs from XML file");
                    //throw new Exception(ex.InnerException.Message);
                }

                return RefsList != null ? true : false;
            }

        }

        public bool GetAllParameters()
        {
            return dbLoadParameters() ? false : true;
        }


        ///         ///<summary><b>Write parameter into db</b></summary>
        /// <param name="ID_Parameter">todo: describe ID_Parameter parameter on dbSaveParameter</param>
        /// <param name="ValueMin">todo: describe ValueMin parameter on dbSaveParameter</param>
        /// <param name="ValueMax">todo: describe ValueMax parameter on dbSaveParameter</param>
        ///         ///<remarks>Write parameter into db</remarks>
       
        
        public bool ReadXmlStruct()
        {

            ConfigFile = XDocument.Load(_xmlPath);
            try
            {
                //SbProdGenericsConnString.InitialCatalog = "ProdGenerics";
                var root = ConfigFile.Root;
                var elCavities = root.Elements("Cavities").Elements("Cavity");
                var elScanners = root.Elements("Scanners").Elements("Scanner");
                var elRFIDs = root.Elements("RFIDs").Elements("RFID");
                var elIAIs = root.Elements("IAIs").Elements("IAI");
                var elBOAs = root.Elements("BOAcams").Elements("BOA");
                var elDevice = root.Element("Device");
                var elIOs = root.Elements("IOs").Elements("IO");

                ActualLang = (Language)Enum.Parse(typeof(Language), root.Element("Languague")?.Value);
                AutoInsertNewMessages = IsTrue(root.Element("AutoInsertNewMessages")?.Value);

                if (IsNumerical(root.Element("IDWorkStation")?.Value)) WSid = Convert.ToInt16(root.Element("IDWorkStation")?.Value);
                else return false;

                HasVacuum = IsTrue(root.Element("HasVacuum")?.Value);
                StationName = root.Element("StationName")?.Value;
                UserListGroup = root.Element("UsersListGroup")?.Value;
                Log.IsDebugMessages = IsTrue(root.Element("DebugMessages")?.Value);
                Calibration = IsTrue(root.Element("HasCalibration")?.Value);
                HasCycleTypes = IsTrue(root.Element("HasCycleTypes")?.Value);
                var database = root.Element("Database");
                if (!string.Equals(database.Attribute("Type")?.Value, "xml", StringComparison.OrdinalIgnoreCase))
                {
                    DBConnection = DataSource.SQL;
                    //Database connection string
                    SbDefaultConnString.DataSource = database.Element("Server")?.Value;
                    SbDefaultConnString.InitialCatalog = database.Element("Catalog")?.Value;
                    SbDefaultConnString.UserID = database.Element("User")?.Value;
                    SbDefaultConnString.Password = database.Element("Password")?.Value;
                    SbDefaultConnString.ApplicationName = database.Element("ApplicationName")?.Value;

                    //Prodgenerics connection string
                    SbProdGenericsConnString.DataSource = database.Element("GenericCatalogServer")?.Value;
                    SbProdGenericsConnString.InitialCatalog = database.Element("GenericCatalog")?.Value;
                    SbProdGenericsConnString.UserID = database.Element("GenericCatalogUser")?.Value;
                    SbProdGenericsConnString.Password = database.Element("GenericCatalogPassword")?.Value;
                    SbProdGenericsConnString.ApplicationName = database.Element("ApplicationName")?.Value;


                    //Datasources
                    UsersDataSource = database.Element("UsersDataSource").Value.ToUpper().Equals("XML") ? DataSource.XML : DataSource.SQL;
                    RefsDataSource = database.Element("RefsDataSource").Value.ToUpper().Equals("XML") ? DataSource.XML : DataSource.SQL;
                    LimitsDataSource = database.Element("LimitsDataSource").Value.ToUpper().Equals("XML") ? DataSource.XML : DataSource.SQL;
                    LanguageDataSource = database.Element("LanguagueDataSource").Value.ToUpper().Equals("XML") ? DataSource.XML : DataSource.SQL;
                    ConstsDataSource = database.Element("ConstsDataSource").Value.ToUpper().Equals("XML") ? DataSource.XML : DataSource.SQL;
                }
                else if (string.Equals(database.Attribute("Type")?.Value, "xml", StringComparison.OrdinalIgnoreCase))
                {

                    DBConnection = DataSource.XML;
                    UsersDataSource = DataSource.XML;
                    RefsDataSource = DataSource.XML;
                    LimitsDataSource = DataSource.XML;
                    LanguageDataSource = DataSource.XML;
                    ConstsDataSource = DataSource.XML;
                }

                //CONNECT TO DATABASE
                if (DBConnection == DataSource.SQL)
                {
                    TestConnection(SbDefaultConnString.DataSource);
                    DbConnect();

                }

                #region Cavity instance



                var cavityList = from Cav in elCavities
                                 select new
                                 {
                                     SubWorkCenterIDs = Cav.Elements("SubWorkstations"),     //int
                                     Traceability = Cav.Element("Traceability").Value,            //bool
                                     RFIDTraceability = Cav.Element("RFIDTraceability").Value,   //bool
                                     AutoRef = IsTrue(Cav.Element("HasAutoRef").Value),             //bool
                                     HomeCycle = Cav.Element("HasHomeCycle").Value,
                                     ViewForReference = database.Element("DBViewForReference")?.Value
                                 };

                var dic = new Dictionary<string, int>();
                var index = 1;
                foreach (var cav in cavityList)
                {
                    try
                    {
                        foreach (var sub in cav.SubWorkCenterIDs.Elements("SubWorkstation"))
                        {
                            dic.Add(sub.Attribute("Name").Value, Convert.ToInt16(sub.Attribute("ID").Value));
                        }

                        
                        if (DBConnection == DataSource.SQL)
                            Cycles.Add(new CycleFactory(WSid, dic,
                                IsTrue(cav.Traceability), cav.AutoRef,
                                index, IsTrue(cav.HomeCycle), IsTrue(cav.RFIDTraceability), MyDB,cav.ViewForReference));
                        else
                            Cycles.Add(new CycleFactory(WSid, dic, IsTrue(cav.Traceability), cav.AutoRef, index, IsTrue(cav.HomeCycle), IsTrue(cav.RFIDTraceability)));


                        if (Log.IsDebugMessages)
                        {
                            foreach (var nest in dic)
                            {
                                Log.Instance.Info("Loaded on Cavity "+ index +" the SubWC " + nest.Value+" - " + nest.Key);
                            }
                            
                            if (IsTrue(cav.HomeCycle)) Log.Instance.Info("Loaded Cycle for " + index + " has HomeCycle of the machine");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error(ex.Message, "Creating Cavity");
                    }
                    index++;
                }
                #endregion Cavity
                #region Scanners Instance
                var ScannerList = from Sca in elScanners
                                  select new
                                  {
                                      Type = Sca.Attribute("Type").Value,
                                      Name = Sca.Element("Name").Value,
                                      IP = Sca.Element("IP").Value,
                                      COMPort = Sca.Element("COMPort").Value,
                                      BaudRate = Sca.Element("BaudRate").Value,
                                  };

                foreach (var Scanner in ScannerList)
                {
                    switch (Scanner.Type)
                    {
                        case "1":
                            var newIp = IPAddress.Parse(Scanner.IP);
                            Scanners.Add(new Scanner(newIp, 23, Preh.Scanner.SCANNER_TYPE.Cognex_DataMan_60, Scanner.Name));//Scanners[0]
                            break;
                        case "2":
                            if (IsNumerical(Scanner.BaudRate)) Scanners.Add(new Scanner(Scanner.COMPort, Convert.ToInt32(Scanner.BaudRate), Preh.Scanner.SCANNER_TYPE.HoneyWell_Xenon_1900, Scanner.Name));//Scanners[0]
                            break;
                    }
                }
                #endregion
                #region RFIDs Instance
                var RFIDList = from RF in elRFIDs
                               select new
                               {
                                   Type = RF.Attribute("Type").Value,
                                   Name = RF.Element("Name").Value,
                                   Serial = RF.Element("Serial").Value,
                                   COMPort = RF.Element("COMPort").Value,
                                   Level = RF.Element("Level").Value,
                               };


              
                foreach (var RFID in RFIDList)
                {
                    switch (RFID.Type)
                    {
                        case "1":
                            RFIDs.Add(new RFID(RFID.COMPort, RFID.Serial, Preh.RFID.RFID_TYPE.Nordic_Id_Stix, RFID.Name, Convert.ToInt32(RFID.Level)));
                            break;
                    }
                }
                #endregion
                #region IAIs Instance

                var IAIList = from IAIActuator in elIAIs
                              select new
                              {
                                  COMPort = IAIActuator.Element("COMPort").Value,
                                  BaudRate = IAIActuator.Element("BaudRate").Value,
                                  Axis = from eAxis in IAIActuator.Elements("Axis")
                                         select new
                                         {
                                             ID = byte.Parse(eAxis.Attribute("ID").Value),
                                             PosList = from myIAIPos in eAxis.Elements("Position")
                                                       select new
                                                       {
                                                           ID = myIAIPos.Element("ID").Value,
                                                           Name = myIAIPos.Element("Name").Value,
                                                           Position = myIAIPos.Element("Position").Value,
                                                           ControlFlag = myIAIPos.Element("ControlFlag").Value,
                                                           Inposband = myIAIPos.Element("Inposband").Value,
                                                           Speed = myIAIPos.Element("Speed").Value,
                                                           Acceleration = myIAIPos.Element("Acceleration").Value,
                                                           Decceleration = myIAIPos.Element("Decceleration").Value,
                                                           PushCurrentLimiting = myIAIPos.Element("PushCurrentLimiting").Value,
                                                           BondaryZonePositionLow = myIAIPos.Element("BondaryZonePositionLow").Value,
                                                           BondaryZonePositionHigh = myIAIPos.Element("BondaryZonePositionHigh").Value,
                                                           LoadOutputCurrentThreshold = myIAIPos.Element("LoadOutputCurrentThreshold").Value
                                                       }
                                         }

                              };

                IAIModbusASCII newIAI = null;
                foreach (var IAI in IAIList)
                {
                    if (IsNumerical(IAI.BaudRate))
                        newIAI = new IAIModbusASCII(IAI.COMPort, Convert.ToInt32(IAI.BaudRate));

                    foreach (var IAIAxis in IAI.Axis)
                    {
                        newIAI.Axis.Add(IAIAxis.ID);
                        foreach (var IAIPos in IAIAxis.PosList)
                        {
                            var NewAxisPos = new IAIModbusASCII.AxisPosition
                            {
                                ID = Convert.ToByte(IAIPos.ID),
                                Name = IAIPos.Name,
                                TargetPosition = Convert.ToInt32(IAIPos.Position),
                                ControlFlag = Convert.ToUInt16(IAIPos.ControlFlag),
                                Inposband = Convert.ToUInt16(IAIPos.Inposband),
                                Speed = Convert.ToUInt32(IAIPos.Speed),
                                Acceleration = double.Parse(IAIPos.Acceleration, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo),
                                Decceleration = double.Parse(IAIPos.Decceleration, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo),
                                PushCurrentLimiting = Convert.ToUInt16(IAIPos.PushCurrentLimiting),
                                BondaryZonePosition_Low = Convert.ToInt32(IAIPos.BondaryZonePositionLow),
                                BondaryZonePosition_High = Convert.ToInt32(IAIPos.BondaryZonePositionHigh),
                                LoadOutputCurrentThreshold = Convert.ToUInt16(IAIPos.LoadOutputCurrentThreshold)
                            };

                            newIAI.Axis[IAIAxis.ID].Add(NewAxisPos);
                        }
                    }

                    IAIs.Add(newIAI);
                }

                #endregion
                #region BOA cameras



                var BOAList = from boa in elBOAs
                              select new
                              {
                                  IP = boa.Attribute("ip").Value,
                                  withDisplay = boa.Attribute("withDisplay").Value
                              };
                foreach (var boa in BOAList)
                {
                    var newBoa = new VisionSystemBOA()
                    {
                        IP = boa.IP,
                        WithDisplayConnection = IsTrue(boa.withDisplay)
                    };
                    //newBOA.visionDisplay = boaDisplay;
                    BOAs.Add(newBoa);
                }

                #endregion
                #region BK&IO Instance
                MainIO = new IOCycle(elDevice.Element("IP").Value);
                var IOlist = from IO in elIOs
                             select new
                             {
                                 Name = IO.Element("IOName").Value,
                                 TypeIO = IO.Element("IOType").Value,
                                 Address = IO.Element("IOAddress").Value
                             };
                try
                {
                    foreach (var IO in IOlist)
                    {
                        switch (IO.TypeIO)
                        {
                            case "DI":
                                MainIO.Dt_DI.Rows.Add(new object[] { IO.Name, IO.Address, 0 });
                                break;
                            case "DO":
                                MainIO.Dt_DO.Rows.Add(new object[] { IO.Name, IO.Address, 0, false });
                                break;
                            case "AI":
                                MainIO.Dt_AI.Rows.Add(new object[] { IO.Name, IO.Address, 0 });
                                break;
                            case "AO":
                                MainIO.Dt_AO.Rows.Add(new object[] { IO.Name, IO.Address, 0 });
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Error(ex, "Can't create IOs.");
                    //throw new Exception("Can't create IOs.", ex);
                }

                MainIO.SetSafeMovements(IAIs);
                if (Log.IsDebugMessages) Log.Instance.Info("Set safe movements !");

                //////////////////////////////////////////////////////
                foreach (var cycle in Cycles)
                {

                    cycle.BKResource = MainIO;
                }

                #endregion


                return true;
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex, "Loading XML error.");
                return false;
                //throw new Exception("Loading XML error.", ex);
            }
            
        }

        ///<summary><b>Force the Engine to try to connect to all devices</b></summary>
        ///<remarks>Force the Engine to try to connect to all devices</remarks>
        public bool PrepareDevices()
        {
            #region ConnectScanners

            foreach (Preh.Scanner Scanner in Scanners)
            {
                try
                {
                    TestConnection(Scanner.ipAddr.ToString());
                    Scanner.TimeoutValue = 100;
                    
                        if (Scanner.Connect())
                        {
                            if (Log.IsDebugMessages) Log.Instance.Info("Scanner " + Scanner.Name + " was  connected!");
                        }
                    
                    else Log.Instance.Error("Scanner " + Scanner.Name + " failed to connect!");
                }
                catch (DeviceConnectException ex)
                {
                    Log.Instance.Error(ex.InnerException,nameof(Scanner));
                }
            }
            #endregion

            #region ConnectRFIDs

            foreach (var RFID in RFIDs)
            {
                try
                {
                    RFID.TimeoutValue = 100;
                    if (RFID.Connect())
                    {
                        if (Log.IsDebugMessages) Log.Instance.Info("RFID " + RFID.ReaderName + " was  connected!");
                    }
                    else throw new DeviceConnectException("RFID " + RFID.ReaderName + " failed to connect! - " + RFID.Error);
                }
                catch (DeviceConnectException ex)
                {
                    Log.Instance.Error(ex.InnerException, nameof(RFID));
                }
            }
            #endregion
          
            #region ConnectBoas
            foreach (var boa in BOAs)
            {

                if (!boa.connect(boa.WithDisplayConnection))
                {
                    Log.Instance.Error("Error: Unable to Connect to BOA " + boa.IP);
                    break;
                }
                
            }
            #endregion

            #region OpenComIAI

            foreach (var iai in IAIs)
            {

                try
                {
                    iai.OpenComPort();
                }
                catch (Exception ex)
                {
                    Log.Instance.Error(ex.InnerException, nameof(iai));
                }

                
            }


            #endregion OpenComIAI

            #region PrepareIO
            try
            {
                TestConnection(MainIO.MyBK.wsIPAddress);
                MainIO.MyBK.ConnectToServer();
                Thread.Sleep(200);


                //MainIO.MyBK.ConnectToServer();
                //Thread.Sleep(200);

                if (!MainIO.BKConnected)
                {
                    throw new DeviceConnectException("Beckhoff " + MainIO.MyBK.wsIPAddress + " failed to connect!");
                }
                else Log.Instance.Info("Beckhoff " + MainIO.MyBK.wsIPAddress + " was  connected!");
            }
            catch (DeviceConnectException ex)
            {
                Log.Instance.Error(ex.InnerException, nameof(MainIO.BKConnected));
                EngineError?.Invoke("Connection to BK", ex.Message);
                return false;
            }
            #endregion

            return true;
        }
        public void ReleaseDevices()
        {
            foreach (var scanner in Scanners)
            {
                scanner.Disconnect();
                Log.Instance.Info("Scanner "+scanner.Name + " was Disconnected!");
            }

            foreach (var rfid in RFIDs)
            {
                rfid.Disconnect();
                Log.Instance.Info("RFID " + rfid.ReaderName + " was Disconnected!");
            }

            foreach (var boa in BOAs)
            {
                boa.disconnect();
                Log.Instance.Info("BOA " + boa.IP + " was Disconnected!");
            }

            foreach (var iai in IAIs)
            {
                iai.CloseComPort();
                Log.Instance.Info("IAI axisID " + iai.AxisId + " was Disconnected!");
            }


        }


        ///<summary><b>Check the Safety of the machine </b></summary>
        ///<remarks>Check the Safety of the machine </remarks>
        public bool RunCycles()
        {
            //TODO: Verificar uma maneira nova de fazer isto

            //Indicate Inicial Position:
            SafetyCircuit = MainIO.ReadDI(EngineData.DI.Safety_Circuit_On);
            AirPressureOk = MainIO.ReadDI(EngineData.DI.Air_Pressure);

            if (SafetyCircuit && AirPressureOk)
            {
                foreach (var cycle in Cycles)
                {

                    //Cycle.PrehRef = SelectedModel;
                    cycle.BOAs = BOAs;
                    cycle.IAIs = IAIs;

                    SafetyCircuit = MainIO.ReadDI(EngineData.DI.Safety_Circuit_On);
                    AirPressureOk = MainIO.ReadDI(EngineData.DI.Air_Pressure);
                    if (!SafetyCircuit || !AirPressureOk) break;
                    //correr função
                    cycle.CycleModeRun(_engineCurrentStatus);

                    MainIO.NeedToWrite = true;
                    MainIO.NeedToWriteAO = true;

                    if (cycle.HomeCycleDone && CurrentStatus == ENUM_Cycle.Home && cycle.HasHomeCycle)
                        _engineCurrentStatus = ENUM_Cycle.ReadyToStart;
                    if (cycle.HomeCycleFail && CurrentStatus == ENUM_Cycle.Home && cycle.HasHomeCycle)
                    {
                        _engineCurrentStatus = ENUM_Cycle.Manual;
                        cycle.HomeCycleFail = false;
                    }
                }
                return true;
            }
            if (!SafetyCircuit || !AirPressureOk)
            {
                foreach (var cycle in Cycles)
                {

                    cycle.KillCycles();
                    _engineCurrentStatus = ENUM_Cycle.Manual;
                    cycle.HomeCycleFail = false;
                }
            }

            return false;
        }

        ///<summary><b>Check the Safety of the machine retunr TRUE if all OK </b></summary>
        ///<remarks>Check the Safety of the machine retunr TRUE if all OK  </remarks>
        public bool CheckSafety()
        {
            //Indicate Inicial Position:
            SafetyCircuit = MainIO.ReadDI(EngineData.DI.Safety_Circuit_On);
            AirPressureOk = MainIO.ReadDI(EngineData.DI.Air_Pressure);

            if (SafetyCircuit && AirPressureOk) return true;

            return false;
        }

        //private methods
        private bool LoadReferences()
        {
            return MyDB.Refs_LoadReferences(ViewForReference);
        }
       

        public bool LoadAllCalibrationOffsets()
        {
            try
            {
                return MyDB.Calibration_LoadCalibers();
            }
            catch (Exception)
            {
                Log.Instance.Warn("Failed Loading Calibers.");
                return false;
            }
        }

        public string PrintText(string text)
        {
            if (LanguageDataSource == Engine.DataSource.SQL && AutoInsertNewMessages)
            {

                if (text != Environment.NewLine)
                {
                    return MyDB.Languages_GetMessage(text);
                }
                else
                {
                    return Environment.NewLine;
                }

                
            }
            else
            {

                if (text != Environment.NewLine)
                {
                    return "[AutoInsertOFF] " + text;
                }
                else
                {
                    return Environment.NewLine;
                }


            }
        }
        private static bool TestConnection(string hostOrAddress)
        {
            var counter = 0;
            using (var p1 = new Ping())
            {
                try
                {
                    var PR = p1.Send(hostOrAddress, 100);
                    // check when the ping is not success
                    while (!PR.Status.ToString().Equals("Success"))
                    {
                        if (counter < 3)
                        {
                            counter++;
                        }
                        else
                        {
                            Log.Instance.Info("Unable to Ping: " + hostOrAddress);
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is PingException)
                    {
                        Log.Instance.Info("Unable to Ping: " + hostOrAddress);
                        return false;
                    }
                    
                }
                
                return true;
            }
        }

        public string PrintGenericText(string text)
        {
            if (LanguageDataSource == Engine.DataSource.SQL && AutoInsertNewMessages)
            {
                return MyDB.Languages_GetGenericMessage(text);
            }
            else
            {
                return text;
            }
        }

        public void Dispose()
        {
            try
            {
                MainIO = null;
                this.Dispose();
            }
            catch (Exception exp)
            {
                Log.Instance.Error(exp.InnerException, "Engine Dispose Error");
            }
        }

        private void UpdateCycleStatus()
        {
            
        }
        private static bool IsNumerical(string test)
        {
            var rgNumerical = new Regex("^[0-9]+$", RegexOptions.Compiled);
            if (rgNumerical.IsMatch(test)) return true;

            return false;
        }
        private static bool IsTrue(string test)
        {
            if (String.Equals(test, "TRUE", StringComparison.OrdinalIgnoreCase) || test == "1") return true;

            return false;
        }


        #region ExceptionHandling
        //TODO: Maintain and increase Expcetion - Hugo Vaz
        [Serializable]
        public class DeviceConnectException : Exception
        {
            public DeviceConnectException() : base() { }
            public DeviceConnectException(string message) : base(message)
            {
            }
            public DeviceConnectException(string message, Exception innerException) : base(message, innerException) { }
            public DeviceConnectException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }
        #endregion

       





    }
}
