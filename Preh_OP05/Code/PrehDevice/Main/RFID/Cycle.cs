using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Data;
using System.Timers;
using System.Drawing;
using System.Windows.Forms;

namespace Preh {
    public class Cycle {
        #region Tags for Language
        public struct TagError {
            public static int Init_Scanner = 32;
            public static int Init_RFID = 34;

            public static int SyncDate = 40;
            public static int FindParam = 42;
            public static int ParamNotfound = 43;

            public static int Save_Params = 56;
            public static int inLine = 58;
            public static int Warning = 61;
            public static int Error = 62;

            public static int Init_Var = 73;
            public static int Init_IAI = 74;
            public static int Get_CalibOffsets = 75;
            public static int Init_Lang = 76;
            public static int Init_CamBOA = 77;

            public static int CycleEnded = 82;
            public static int VacuumFail = 83;

            public static int Traceability = 84;

            public static int FunctionError = 85;

            public static int PositionSensor = 136;
            public static int DetectionsFail = 137;

            public static int ClassButtons = 227;

            public static int ChromeBar_Left = 229;
            public static int ChromeBar_Right = 230;
            public static int ChromeBar_ALL = 231;

            public static int ConnectionNOK = 126;

            public static int RFIDErrorReading = 180;
            public static int RFIDNoTags = 181;
            public static int RFIDMultipleTags = 182;
            public static int RFIDTraceabilityError = 183;
            public static int ErrorGettingTraceNr = 184;
            public static int RFIDTrayEmpty = 185;
        }
        public struct TagGeneric {
            public static int RefSelect = 15;
            public static int lblParam = 20;
            public static int DI = 28;
            public static int DO = 29;
            public static int AI = 30;
            public static int AO = 31;

            public static int Machine_In_Home = 68;
            public static int Machine_Out_Home = 69;

            public static int MoveHomePos = 70;
            public static int ModeAuto_ON = 71;
            public static int FaultMoveHomePos = 72;

            public static int DataGridView = 79;

            public static int RFIDSelect = 99;

            public static int PutSampleOK = 108;
            public static int PutSampleNOK = 109;
            public static int RemoveSampleOK = 110;
            public static int RemoveSampleNOK = 111;

            public static int SampleOK_NotConform = 112;
            public static int SampleNOK_NotConform = 113;

            public static int Remove_To_Rejection = 114;

            public static int FirstCycle = 37;
            public static int NormalCycle = 38;
            public static int LastCycle = 39;

            public static int VerifyLabelTracePart = 115;
            public static int VerifyLabelTraceButton_L = 116;
            public static int VerifyLabelTraceButton_R = 117;
        }
        public struct TagActuators {
            public static int Proy = 150;
            public static int Proy_Sup = 151;
            public static int Proy_Inf = 152;
            public static int Proy_N1 = 153;
            public static int Proy_N2 = 154;
            public static int Door = 155;
            public static int Table = 156;
            public static int Lock_L = 157;
            public static int Lock_R = 158;
            public static int Lock = 159;
            public static int Lock_N1 = 160;
            public static int Lock_N2 = 161;
            public static int CylMark = 162;
            public static int CylCod = 163;
            public static int IAI_Axis = 164;
            public static int Ionizer = 165;
            public static int Elevator_L = 167;
            public static int Elevator_R = 168;
        }
        public struct TagInformations {
            public static int PutPartNest = 106;

            public static int PutLabel = 124;
            public static int PutAuxiliaryNest = 222;
            public static int ButtonLeft = 223;
            public static int ButtonRight = 224;
            public static int RemoveButtonsNest = 225;

            public static int ReadLabel = 118;

            //Using
            public static int Vacuum = 7;
            public static int StopVacuum = 6;

            public static int EndApplication = 33;

            public static int AskVacuum = 44;

            public static int ConfirmSaveParms = 55;
            public static int Params_NotChange = 57;
            public static int Connect_BK = 59;
            public static int wo_Connect_BK = 60;

            public static int Wo_ref = 63;
            public static int L_Door_open = 64;
            public static int R_Door_open = 65;
            public static int Safety_Circuit_On = 66;
            public static int Air_Pressure = 67;

            public static int DiscardChanges = 80;

            public static int VerifySensor = 86;
            public static int checkingDetections = 94;

            public static int CaliberProcess = 89;
            public static int CaliberAdjustedOK = 90;
            public static int CaliberAdjustedNOK = 91;
            public static int RemoveCaliber = 92;
            public static int CaliberOutTolerance = 93;

            public static int PartChecked_W_Success = 95;

            public static int AxisOutPosition = 100;

            public static int PartFinished = 102;
            public static int PressButtonNOK = 103;
            public static int PressPedal = 104;
            public static int PressPedalToMoveTable = 123;
            public static int RemovePartNest = 105;
            public static int PressPedal_or_ButtonNOK = 107;
            public static int ConnectScanner = 127;
            public static int ConnectSuccessed = 128;
            public static int ScannerNotConnected = 130;
            public static int Remove_To_RejectionBox = 114;
            public static int RejectionBox_Close = 147;
            public static int RelasePedal = 143;
        }
        public struct TagMovements {
            public static int payAttention = 130;
            public static int MovTable = 131;
            public static int MovProy_Sup = 132;
            public static int MovProy_Inf = 133;
            public static int MovProy = 134;
            public static int MovLock = 135;
        }
        #endregion Tags for Language
        struct TimerONPreh {
            public string name;
            public double TimeInit;
            public double TimeEnd;
        }
        struct PulseFreqPreh {
            public string name;
            public double TimeInit;
            public double TimeNextSwitch;
            public bool Output;
            public double TimeFreq;
        }

        // TODO: Machine State Event
        // TODO: Traceability Property
        System.Timers.Timer MyTimer;
        private List<TimerONPreh> Timers = null;
        private List<PulseFreqPreh> Pulsers;
        public enum ReadWriteIO { ReadDI, ReadDO, WriteDI, WriteDO }

        #region Properties
        public FormInterface HMI { get; set; }
        public HardwareInterface Hardware { get; set; }
        public TextBox TextBoxInstructions { get; set; }
        public bool IsSample { get; set; }
        public bool IsCaliber { get; set; }
        public int PartCountOK { get; set; }
        public int PartCountNOK { get; set; }
        public bool UsingTraceability { get; set; }
        public bool UsingRFIDTraceability { get; set; }
        public string PrehRef { get; set; }
        public int WSID { get; set; }
        public int SubWorkCenterID { get; }
        private Color PanelColor { get; set; }
        public List<long> TraceNumbers { get; set; }
        public bool HasScrewing { get; set; }
        public PPTraceStation DBResource { get; set; }
        public bool NeedToWrite { get; set; }
        public bool CycleDone { get; set; }
        public bool CycleFail { get; set; }
        public int CycleId { get; set; }
        public bool HasHomeCycle { get; set; }
        public int MessageID { get; set; }
        //TODO: make it private and handled it in a different way. Was placed like this because of dependecies with HMI
        public DataTable Dt_DO { get; set; }
        public DataTable Dt_DI { get; set; }
        public DataTable Dt_AI { get; set; }
        public DataTable Dt_AO { get; set; }
        public DataTable InDevices { get; set; }
        public DataTable OutDevices { get; set; }
        public EngineData.Enum_Step Step { get; set; }
        public List<IAIModbusASCII> IAIs { get; set; }
        public Dictionary<string, string> ScannerInfo { get; set; }
        public Dictionary<string, string[]> RFIDInfo { get; set; }
        #endregion Properties

        // TODO: constructor can be simplyfied and normalized
        public Cycle( string prehref, int wsID, int subWCID) : this(null, prehref, wsID, subWCID, false, 0, true, false) { }
        public Cycle( PPTraceStation db ,string prehref, int wsID, int subWCID, bool usesTraceability, int cycleid, bool hashomecycle, bool usesRFIDTraceability) {
            DBResource = db;
            PrehRef = prehref;
            WSID = wsID;
            SubWorkCenterID = subWCID;
            UsingTraceability = usesTraceability;
            UsingRFIDTraceability = usesRFIDTraceability;
            TraceNumbers = new List<long>();
            HasScrewing = false;
            CycleId = cycleid;
            HasHomeCycle = hashomecycle;
            Timers = new List<TimerONPreh>();
            Pulsers = new List<PulseFreqPreh>();
            MyTimer = new System.Timers.Timer();
            HMI = new FormInterface(CycleId);
            Hardware = new HardwareInterface();
            ScannerInfo = new Dictionary<string, string>();
            RFIDInfo = new Dictionary<string, string[]>();
            MessageID = 0;
            Engine.ScannerNewData += Engine_ScannerNewData;
            Engine.RFIDNewData += Engine_RFIDNewData;
        }
     
        public void UpdateStep(EngineData.Enum_Step step) {
            HMI.StepChange(step);
            Hardware.Step = step;
        }

        private void Engine_ScannerNewData(string arg1, string arg2) {
            try {
                ScannerInfo.Add(arg1, arg2);
            }
            catch { }
        }

        public bool ReadScanner(string name, out string data) {
            if (ScannerInfo.ContainsKey(name)) {
                data = ScannerInfo[name];
                ScannerInfo.Remove(name);
                return true;
            }
            data = "";
            return false;
        }

        private void Engine_RFIDNewData(string arg1, string[] arg2)
        {
            try
            {
                RFIDInfo.Add(arg1, arg2);
            }
            catch (Exception ex) { }
        }

        public bool ReadRFID(string name, out string[] data)
        {
            if (RFIDInfo.ContainsKey(name))
            {
                data = RFIDInfo[name];
                RFIDInfo.Remove(name);
                return true;
            }
            data = null;
            return false;
        }


        public void NewTraceNumber(int traceNumber) {
            TraceNumbers.Add(traceNumber);
        }
        public long FindTraceNumber(long traceNumber) {
            return TraceNumbers.Find(item => item == traceNumber);
        }
        public bool RemoveTraceNumber(int traceNumber) {
            return TraceNumbers.Remove(traceNumber);
        }
        public bool UpdateDIORows(int IOIndex, string IOField, Nullable<bool> State, ReadWriteIO ReadWrite, string IOName) {
            //Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
            try {
                //Para leitura de DI:
                if (ReadWrite == ReadWriteIO.ReadDI) {
                    if (IOName.Equals("")) {
                        return (bool)Dt_DI.Rows[IOIndex][IOField];
                    } else {
                        int Rowindex = Dt_DI.Rows.IndexOf(Dt_DI.Select("DIName = '" + IOName + "'")[0]);
                        return (bool)Dt_DI.Rows[Rowindex][IOField];
                    }
                }

                //Para leitura de DO:
                else if (ReadWrite == ReadWriteIO.ReadDO) {
                    if (IOName.Equals("")) {
                        return (bool)Dt_DO.Rows[IOIndex][IOField];
                    } else {
                        int Rowindex = Dt_DO.Rows.IndexOf(Dt_DO.Select("DOName = '" + IOName + "'")[0]);
                        return (bool)Dt_DO.Rows[Rowindex][IOField];
                    }
                }

                //Para escrita de DI:
                else if (ReadWrite == ReadWriteIO.WriteDI) {
                    if (IOName.Equals("")) {
                        Dt_DI.Rows[IOIndex][IOField] = State;
                        return true;
                    } else {
                        int Rowindex = Dt_DI.Rows.IndexOf(Dt_DO.Select("DIName = '" + IOName + "'")[0]);
                        Dt_DI.Rows[Rowindex][IOField] = State;
                        return true;
                    }
                }

                //Para escrita de DO:
                else if (ReadWrite == ReadWriteIO.WriteDO) {
                    if (IOName.Equals("")) {
                        Dt_DO.Rows[IOIndex][IOField] = State;
                        return true;
                    } else {
                        int Rowindex = Dt_DO.Rows.IndexOf(Dt_DO.Select("DOName = '" + IOName + "'")[0]);
                        Dt_DO.Rows[Rowindex][IOField] = State;
                        return true;
                    }
                } else {
                    //Error?.Invoke("UpdateDIORows Error: \r\n\r\nWrong Parameters received");
                    Log.Instance.Error("UpdateDIORows Error during cycle: \r\n\r\nWrong Parameters received");
                    return false;
                }
            }
            catch (Exception ex) {
                //Error?.Invoke("UpdateDIORows Error: " + ex.ToString());
                Log.Instance.Error(ex, "UpdateDIORows Error: " + ex.ToString());
                return false;
            }
        }
        public bool ReadDI(EngineData.Enum_DI e) {
            try {
                return UpdateDIORows((int)e, "Value", null, ReadWriteIO.ReadDI, "");
            }
            catch (Exception) {

                try {
                    return UpdateDIORows((int)e, "Value", null, ReadWriteIO.ReadDI, "");
                }
                catch (Exception) { return false; }
            }
        }
        public bool ReadDO(EngineData.Enum_DO e) {
            try {
                return UpdateDIORows((int)e, "Value", null, ReadWriteIO.ReadDO, "");
            }
            catch (Exception) {

                try {
                    return UpdateDIORows((int)e, "Value", null, ReadWriteIO.ReadDO, "");
                }
                catch (Exception exp) {
                    Log.Instance.Error(exp, "ReadDO Error in Cycle: " + exp.ToString());
                    return false;
                }
            }
        }
        
        public void WriteDO(EngineData.Enum_DO e, bool est) {
            NeedToWrite = true;
            try {

                UpdateDIORows((int)e, "ValueToWrite", est, ReadWriteIO.WriteDO, "");
            }
            catch (Exception exp) {
                //Error?.Invoke("WriteDO Error: " + exp.ToString());
                Log.Instance.Error(exp, "WriteDO Error in Cycle: " + exp.ToString());
            }
        }
        public double ReadAI(EngineData.Enum_AI e)
        {
            int Rowindex = (int)Dt_AI.Rows[(int)e]["Value"];

            return Math.Round(((double)Rowindex * 30 / 32767), 2);
        }
        public void WriteAO(EngineData.Enum_AO e, double value)
        {
            //y=mx +b 
            // :)
            //
            double value2send;
            value2send = (value * 32767)/10;
            Dt_AO.Rows[(int)e]["Value"] = value2send;

        }

        //Other
        public bool TON(string name, int msec) {
            var temp = new TimerONPreh();
            double CurrentTime;

            if (Timers.Exists(p => p.name == name)) {
                temp = Timers.Find(p => p.name == name);
                //Check if Time has passed
                CurrentTime = CurrentCycleTime();
                return CurrentTime > temp.TimeEnd;
            }
            //timer doesn't Exist
            temp.name = name;
            temp.TimeInit = CurrentCycleTime();
            temp.TimeEnd = CurrentCycleTime() + (double)msec;
            Timers.Add(temp);
            return false;
        }
        public void StopTON(string name) {
            if (Timers.Exists(p => p.name == name)) {
                var temp = Timers.Find(p => p.name == name);
                //Check if Time has passed
                Timers.Remove(temp);
            }
        }
        public void ClearAllTimers() {
            HMI.ReleaseAllMessages();
            CycleDone = false;
            CycleFail = false;
            if (Timers != null) Timers.Clear();
            Pulsers = new List<PulseFreqPreh>();
        }

        public bool Flash(string flashName, int msec) {

            bool returner;
            //return true;
            if (Pulsers.Exists(p => p.name == flashName)) {


                //timer Exists   //colect data from him
                var Pulse = Pulsers.FirstOrDefault(d => d.name == flashName);
                Pulsers.Remove(Pulse);
                if (CurrentCycleTime() > Pulse.TimeNextSwitch) {
                    var temp = new PulseFreqPreh {
                        name = Pulse.name,
                        TimeInit = Pulse.TimeNextSwitch,
                        TimeNextSwitch = Pulse.TimeInit + Pulse.TimeFreq,
                        TimeFreq = Pulse.TimeFreq,
                        Output = !Pulse.Output
                    };
                    Pulsers.Add(temp);
                }

                returner = Pulse.Output;
            } else {

                //timer doesn't Exist
                var temp = new PulseFreqPreh {
                    name = flashName,
                    TimeInit = CurrentCycleTime(),
                    TimeNextSwitch = CurrentCycleTime() + (double)msec,
                    TimeFreq = (double)msec
                };
                Pulsers.Add(temp);
                returner = temp.Output;
            }
            return returner;

        }

        private static double CurrentCycleTime() {
            return DateTime.Now.TimeOfDay.TotalMilliseconds;
        }

        public void Blink(EngineData.Enum_DO signalEnum, bool turnOnOff, int freq) {
            MyTimer.Elapsed += (sender, e) => ToggleOutput(sender, e, signalEnum);

            if (turnOnOff && !MyTimer.Enabled) {
                MyTimer.Interval = freq;
                MyTimer.Start();
            } else if (!turnOnOff && MyTimer.Enabled) {
                MyTimer.Stop();
                WriteDO(signalEnum, false);
            }
        }

        private void ToggleOutput(object source, System.Timers.ElapsedEventArgs e, EngineData.Enum_DO output) {
            if (ReadDO(output)) WriteDO(output, false);
            else WriteDO(output, true);
        }
    }
}
