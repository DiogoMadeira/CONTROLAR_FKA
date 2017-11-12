using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using PPDBAccess;

namespace Preh
{
    public class Cycle
    {
        public static event Action<int, string> NewInternalErrorText;
        public static event Action<int, Dictionary<EngineData.AI, int>, Dictionary<EngineData.DI, int>, string> ShowPotiPicture;

        #region Enum for Cycle Type



        #endregion Tags for Language
        struct TimerONPreh
        {
            public string name;
            public double TimeInit;
            public double TimeEnd;
        }
        struct PulseFreqPreh
        {
            public string name;
            public double TimeInit;
            public double TimeNextSwitch;
            public bool Output;
            public double TimeFreq;
        }

        // TODO: Machine State Event
        // TODO: Traceability Property
        private List<TimerONPreh> _timers = null;
        private List<PulseFreqPreh> Pulsers;
        private Dictionary<EngineData.DO, System.Timers.Timer> _runningBlinks;
        private EngineData.Step _step;
        private static List<CalibrationMeasurement> CalibrationMeasurementList;

        public enum ReadWriteIO { ReadDI, ReadDO, WriteDI, WriteDO }
        #region Properties
        private List<EngineData.Step> SentMessages { get; set; }
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
        public Dictionary<string,int> SubWorkCenterIDs { get; }
        private Color PanelColor { get; set; }
        public List<long> TraceNumbers { get; set; }
        public string TraceNumber { get; set; }
        public long LongTraceNumber
        {
            get { return Convert.ToInt64(TraceNumber); }
            set { _traceNumber = value; }
        }
        private long _traceNumber;
        public bool HasScrewing { get; set; }
        public PPTraceStation DBResource { get; set; }
        public bool NeedToWrite { get; set; }
        public bool CycleDone { get; set; }
        public bool CycleFail { get; set; }
        public int CycleId { get; set; }
        public bool HasHomeCycle { get; set; }
        public int MessageID { get; set; }
        //TODO: make it private and handled it in a different way. Was placed like this because of dependecies with HMI
        public EngineData.Step Step
        {
            get { return _step; }
            set
            {
                _step = value;
                UpdateStep(_step);
            }
        }
        public List<VisionSystemBOA> BOAs { get; set; }
        public List<IAIModbusASCII> IAIs { get; set; }
        public Dictionary<string, string> ScannerInfo { get; set; }
        public Dictionary<string, string[]> RFIDInfo { get; set; }
        public string[] RFIDTag { get; set; }
        public IOCycle BKResource { get; set; }
        public int CurrentSubWcId { get; set; }


        #endregion Properties

        // TODO: constructor can be simplyfied and normalized
        public Cycle(string prehref, int wsID, Dictionary<string,int> subWCIDs) : this(null, prehref, wsID, subWCIDs, false, 0, true, false) { }
        public Cycle(PPTraceStation db, string prehref, int wsID, Dictionary<string, int> subWCIDs, bool usesTraceability, int cycleid, bool hashomecycle, bool usesRFIDTraceability)
        {
            DBResource = db;
            PrehRef = prehref;
            WSID = wsID;
            SubWorkCenterIDs = subWCIDs;
            UsingTraceability = usesTraceability;
            UsingRFIDTraceability = usesRFIDTraceability;
            TraceNumbers = new List<long>();
            HasScrewing = false;
            CycleId = cycleid;
            HasHomeCycle = hashomecycle;
            _timers = new List<TimerONPreh>();
            Pulsers = new List<PulseFreqPreh>();
            HMI = new FormInterface(CycleId);
            Hardware = new HardwareInterface();
            ScannerInfo = new Dictionary<string, string>();
            RFIDInfo = new Dictionary<string, string[]>();
            MessageID = 0;
            _runningBlinks = new Dictionary<EngineData.DO, System.Timers.Timer>();
            Engine.ScannerNewData += Engine_ScannerNewData;
            Engine.RFIDNewData += Engine_RFIDNewData;
            CalibrationMeasurementList = new List<CalibrationMeasurement>();
            RFIDTag = new string[] { };
            SentMessages = new List<EngineData.Step>();
        }

        public void UpdateStep(EngineData.Step step)
        {
            HMI.StepChange(step);
            Hardware.Step = step;
        }
        public void ResetMessagesAndHardware()
        {
            HMI.ReleaseAllMessages();
            ReleaseCycleMessages();
            Hardware.ReleaseAllHardware();
        }
        public void ResetAll()
        {
            Blink(EngineData.DO.Signal_NOK, false, 500);
            ReleaseCycleMessages();
            ResetMessagesAndHardware();
            ClearAllTimers();
        }

        private void Engine_ScannerNewData(string arg1, string arg2)
        {
            try
            {
                if (ScannerInfo.Count == 0)
                {
                    ScannerInfo.Add(arg1, arg2);
                }
                else
                {
                    ScannerInfo[arg1] = arg2;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool ReadScanner(string name, out string data)
        {
            if (ScannerInfo.ContainsKey(name))
            {
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

                if (RFIDInfo.Count == 0)
                {
                    RFIDInfo.Add(arg1, arg2);
                }
                else
                {
                    RFIDInfo[arg1] = arg2;
                }

            }
            catch (Exception)
            {
                throw;
            }
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


        public void NewTraceNumber(int traceNumber)
        {
            TraceNumbers.Add(traceNumber);
        }
        public long FindTraceNumber(long traceNumber)
        {
            return TraceNumbers.Find(item => item == traceNumber);
        }
        public bool RemoveTraceNumber(int traceNumber)
        {
            return TraceNumbers.Remove(traceNumber);
        }
        private bool UpdateDIORows(int IOIndex, string IOField, bool? State, IOCycle.ReadWriteIO ReadWrite, string IOName)
        {

            return BKResource.UpdateDIORows(IOIndex, IOField, State, ReadWrite, IOName);

        }
        public bool ReadDI(EngineData.DI e)
        {
            try
            {
                return UpdateDIORows((int)e, "Value", null, IOCycle.ReadWriteIO.ReadDI, "");
            }
            catch (Exception)
            {

                try
                {
                    return UpdateDIORows((int)e, "Value", null, IOCycle.ReadWriteIO.ReadDI, "");
                }
                catch (Exception) { return false; }
            }
        }
        public bool ReadDO(EngineData.DO e)
        {
            try
            {
                return UpdateDIORows((int)e, "Value", null, IOCycle.ReadWriteIO.ReadDO, "");
            }
            catch (Exception)
            {

                try
                {
                    return UpdateDIORows((int)e, "Value", null, IOCycle.ReadWriteIO.ReadDO, "");
                }
                catch (Exception exp)
                {
                    Log.Instance.Error(exp, "ReadDO Error in Cycle: " + exp.ToString());
                    return false;
                }
            }
        }
        public void WriteDO(EngineData.DO e, bool est)
        {
            NeedToWrite = true;
            try
            {

                UpdateDIORows((int)e, "ValueToWrite", est, IOCycle.ReadWriteIO.WriteDO, "");
            }
            catch (Exception exp)
            {

                Log.Instance.Error(exp, "WriteDO Error in Cycle: " + exp.ToString());
            }
        }
        public double ReadAI(EngineData.AI e)
        {

            return BKResource.ReadAI(e);
        }
        public double ReadAO(EngineData.AO e)
        {

            return BKResource.ReadAO(e);

        }
        public void WriteAO(EngineData.AO e, double value)
        {

            BKResource.WriteAO(e, value);
        }

        //Other
        public bool TON(string name, int msec)
        {
            var temp = new TimerONPreh();
            double CurrentTime;

            if (_timers.Exists(p => p.name == name))
            {
                temp = _timers.Find(p => p.name == name);
                //Check if Time has passed
                CurrentTime = CurrentCycleTime();
                return CurrentTime > temp.TimeEnd;
            }
            //timer doesn't Exist
            temp.name = name;
            temp.TimeInit = CurrentCycleTime();
            temp.TimeEnd = CurrentCycleTime() + (double)msec;
            _timers.Add(temp);
            return false;
        }
        public void StopTON(string name)
        {
            if (_timers.Exists(p => p.name == name))
            {
                var temp = _timers.Find(p => p.name == name);
                //Check if Time has passed
                _timers.Remove(temp);
            }
        }
        public void ClearAllTimers()
        {
            HMI.ReleaseAllMessages();
            CycleDone = false;
            CycleFail = false;
            _timers?.Clear();
            Pulsers = new List<PulseFreqPreh>();
        }

        public bool Flash(string flashName, int msec)
        {

            bool returner;
            //return true;
            if (Pulsers.Exists(p => p.name == flashName))
            {


                //timer Exists   //colect data from him
                var Pulse = Pulsers.FirstOrDefault(d => d.name == flashName);
                Pulsers.Remove(Pulse);
                if (CurrentCycleTime() > Pulse.TimeNextSwitch)
                {
                    var temp = new PulseFreqPreh
                    {
                        name = Pulse.name,
                        TimeInit = Pulse.TimeNextSwitch,
                        TimeNextSwitch = Pulse.TimeInit + Pulse.TimeFreq,
                        TimeFreq = Pulse.TimeFreq,
                        Output = !Pulse.Output
                    };
                    Pulsers.Add(temp);
                }

                returner = Pulse.Output;
            }
            else
            {

                //timer doesn't Exist
                var temp = new PulseFreqPreh
                {
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

        private static double CurrentCycleTime()
        {
            return DateTime.Now.TimeOfDay.TotalMilliseconds;
        }

        public void Blink(EngineData.DO signalEnum, bool turnOnOff, int freq)
        {
            if (!_runningBlinks.ContainsKey(signalEnum) && turnOnOff)
            {
                var MyTimer = new System.Timers.Timer();

                MyTimer.Elapsed += (sender, e) => ToggleOutput(sender, e, signalEnum);
                _runningBlinks.Add(signalEnum, MyTimer);

                if (!MyTimer.Enabled)
                {
                    MyTimer.Interval = freq;
                    MyTimer.Start();
                }
            }

            if (_runningBlinks.ContainsKey(signalEnum) && !turnOnOff)
            {
                System.Timers.Timer TimerToStop;

                if (_runningBlinks.TryGetValue(signalEnum, out TimerToStop))
                {
                    if (TimerToStop.Enabled)
                    {
                        TimerToStop.Stop();
                        TimerToStop = null;
                        WriteDO(signalEnum, false);
                    }
                    _runningBlinks.Remove(signalEnum);
                }
            }
        }

        private void ToggleOutput(object source, System.Timers.ElapsedEventArgs e, EngineData.DO output)
        {
            WriteDO(output, !ReadDO(output));
        }

        public void ClearAllOutputs()
        {
            var valuesAsList = Enum.GetValues(typeof(EngineData.DO)).Cast<EngineData.DO>().ToList();

            foreach (var output in valuesAsList)
            {

                if (output != EngineData.DO.Safety_Relay_On)
                {
                    WriteDO(output, false);
                }

            }
        }


        public bool TestWithDigitalInput(EngineData.DI[] detections, bool[] wantedResult, out Dictionary<EngineData.DI,int> testResult)
        {
            var validation = true;
            var builder = new StringBuilder();
            testResult = new Dictionary<EngineData.DI, int>();
            var error = "";

            var index = 0;

            foreach (var DI in detections)
            {
                var result = ReadDI(DI);
                if (result != wantedResult[index])
                {
                    validation = false;
                    builder.Append(Enum.GetName(typeof(EngineData.DI), DI)+" = "+result+" Wanted = "+ wantedResult[index] + Environment.NewLine);
                    testResult.Add(DI, 1);
                    

                    DBResource?.Result_SaveResult(DBResource.JobID, result, wantedResult[index], "", "", ResultSubCategory.Mechanical_DimensionControl, ResultUnit.Bool, "");

                }
                else
                {
                    testResult.Add(DI, 0);
                    DBResource?.Result_SaveResult(DBResource.JobID, result, wantedResult[index], "", "", ResultSubCategory.Mechanical_DimensionControl, ResultUnit.Bool, "");
                }
                index++;
            }

            
            error = builder.ToString();
            NewInternalErrorText?.Invoke(CycleId, error);
            return validation;
        }

        public bool TestWithDigitalInput(EngineData.DI[] detections, out Dictionary<EngineData.DI,int> testResult)
        {
            var validation = true;
            var builder = new StringBuilder();
            testResult = new Dictionary<EngineData.DI, int>();
            var error = "";

            foreach (var DI in detections)
            {
                var result = ReadDI(DI);
                if (!result)
                {
                    validation = false;
                    builder.Append(Enum.GetName(typeof(EngineData.DI), DI) + " = " + result + " Wanted = true" + Environment.NewLine);
                    testResult.Add(DI, 1);
                    DBResource?.Result_SaveResult(DBResource.JobID, result, true, "", "", ResultSubCategory.Mechanical_DimensionControl, ResultUnit.Bool, "");

                }
                else
                {
                    testResult.Add(DI, 0);
                    DBResource?.Result_SaveResult(DBResource.JobID, result, false, "", "", ResultSubCategory.Mechanical_DimensionControl, ResultUnit.Bool, "");
                }
            }

           

            error = builder.ToString();
            NewInternalErrorText?.Invoke(CycleId, error);
            return validation;
        }
        public bool TestWithAnalogInput(EngineData.AI[] detections, out Dictionary<EngineData.AI,int> testResult, ResultUnit unit)
        {
            var validation = true;
            testResult = new Dictionary<EngineData.AI, int>();
            var builder = new StringBuilder();
            var error = "";

            var index = 0;

            var adjustments = DBResource?.Calibration_GetLastAdjustments(WSID, 1);

            //Check Analog Inputs
            foreach (var AI in detections)
            {

                var adjustment = adjustments?.Find(a => a.Name == Enum.GetName(typeof(EngineData.AI), AI));

                if (adjustment == null)
                {
                    adjustment = new CalibrationAdjustment
                    {
                        OffSet = 0
                    };
                }

                var realTimeValue = Math.Round(ReadAI(AI) - adjustment.OffSet, 2);

                var measure = DBResource?.MeasureLimits.Find(m => m.MeasureName == Enum.GetName(typeof(EngineData.AI), AI));

                if (measure != null)
                {
                    if (realTimeValue > measure.MinValue && realTimeValue < measure.MaxValue)
                    {
                        testResult.Add(AI, 0);
                        DBResource?.Result_SaveResult(DBResource.JobID, realTimeValue, measure.MinValue, measure.MaxValue, "", "", ResultSubCategory.Mechanical_DimensionControl, unit, "");

                    }
                    else
                    {
                        testResult.Add(AI, 1);
                        validation = false;
                        builder.Append(Enum.GetName(typeof(EngineData.AI), detections[index]) + " -> " + " [" + measure.MinValue + " < " + realTimeValue + " < " + measure.MaxValue + "]" + Environment.NewLine);

                        DBResource?.Result_SaveResult(DBResource.JobID, realTimeValue, measure.MinValue, measure.MaxValue, "", "", ResultSubCategory.Mechanical_DimensionControl, ResultUnit.V, "");

                    }
                }
                index++;
            }

            error = builder.ToString();
            NewInternalErrorText?.Invoke(CycleId, error);
            return validation;
        }


        public int GetSubWcId(string subWcName)
        {
            var id = SubWorkCenterIDs.FirstOrDefault(s => s.Key == subWcName);
            CurrentSubWcId = id.Value;
            return id.Value;
        }

        public bool RunCalibration(string traceNumber, EngineData.AI[] detections, int subWCid)
        {
            var validation = true;
            var builder = new StringBuilder();

            var inputName = "";
            var error = "";

            GetCalibrationMeasurementList(traceNumber);

            // Check Analog Inputs
            foreach (var AI in detections)
            {
                inputName = Enum.GetName(typeof(EngineData.AI), AI);
                var calib = CalibrationMeasurementList.Find(p => p.Name == inputName);

                if (calib != null)
                {
                    var min = calib.NominalValue - calib.Tolerance;
                    var max = calib.NominalValue + calib.Tolerance;

                    var result = ReadAI(AI);

                    if (min < result && max > result)
                    {
                        SaveCalibrationResult(calib, traceNumber, WSID, (byte)subWCid, result, 0);
                    }
                    else
                    {

                        validation = false;
                        SaveCalibrationResult(calib, traceNumber, WSID, (byte)subWCid, result, 1);
                        builder.Append("Calibration:" + Environment.NewLine + Enum.GetName(typeof(EngineData.AI), AI) + " -> " + " [" + min + " < " + result + " < " + max + "]" + Environment.NewLine);
                    }
                }
            }

            error = builder.ToString();
            NewInternalErrorText?.Invoke(CycleId, error);
            return validation;
        }

        private bool SaveCalibrationResult(CalibrationMeasurement caliber, string traceNr, int WSID, byte SubWS, double measuredValue, double calibrationError)
        {
            if (DBResource!=null)
            {
                return DBResource.Calibration_SaveMeasurement(Int64.Parse(traceNr), WSID, SubWS, caliber.ID_DefaultValue, measuredValue, caliber.NominalValue, calibrationError, caliber.Name);
            }
            else
            {
                return true;
            }

           

        }
        private void GetCalibrationMeasurementList(string traceNumber)
        {
            CalibrationMeasurementList = DBResource?.Calibration_GetMeasurementList(long.Parse(traceNumber), WSID, 1);

        }

        public DeviceConst GetDeviceConsts(string name)
        {
            var constant = DBResource?.DeviceConsts.Find(m => m.ConstName == name);

            if (constant != null)
            {
                return constant;
            }
            else
            {
                NewInternalErrorText?.Invoke(CycleId, "The Device Constant " + name + "Doesn't Exist!");
                return new DeviceConst();
            }
        }

        public MeasureLimit GetMeasureLimit(string name)
        {
            var limit = DBResource?.MeasureLimits.Find(m => m.MeasureName == name);

            if (limit != null)
            {
                return limit;
            }
            else
            {
                NewInternalErrorText?.Invoke(CycleId, "The Measure Limit " + name + "Doesn't Exist!");
                return new MeasureLimit();
            }
        }

        public SystemConfiguration GetSystemConfiguration(string name)
        {
            var config = DBResource?.SystemConfigurations.Find(c => c.ConfigurationName == name);

            if (config != null)
            {
                return config;
            }
            else
            {
                NewInternalErrorText?.Invoke(CycleId, "The System Configuration " + name + "Doesn't Exist!");
                return new SystemConfiguration();
            }

        }
        public bool RunSampleVerification(long traceNr, EngineData.AI[] analogDetections = null, ResultUnit unit = ResultUnit.V, EngineData.DI[] digitalDetections = null, bool[] wantedDigitalResult = null)
        {

            var analogResult = new Dictionary<EngineData.AI,int>();
            var digitalResult = new Dictionary<EngineData.DI, int>();

            if (analogDetections != null)
            {
                TestWithAnalogInput(analogDetections, out analogResult, unit);
            }
            else
            {
                var ais =(EngineData.AI[])Enum.GetValues(typeof(EngineData.AI));
                TestWithAnalogInput(ais, out analogResult, unit);
            }

            if (digitalDetections != null)
            {
                if (wantedDigitalResult != null)
                {
                    TestWithDigitalInput(digitalDetections, wantedDigitalResult, out digitalResult);

                }
                else
                {

                    wantedDigitalResult = new bool[digitalDetections.Length];
                    for (int i = 0; i < wantedDigitalResult.Length; i++)
                    {
                        wantedDigitalResult[i] = true;
                    }

                    TestWithDigitalInput(digitalDetections, wantedDigitalResult, out digitalResult);
                }

            }

            if (DBResource != null)
            {
                return DBResource.Samples_ValidateSampleTesting(traceNr);
            }
            else
            {
                return true;
            }

            
        }

        public void ShowPotiResultPicture(Dictionary<EngineData.AI, int> analogResults = null, Dictionary<EngineData.DI, int> digiResults = null, string resourceName = null)
        {
            if (!SentMessages.Contains(Step))
            {
                ShowPotiPicture?.Invoke(CycleId, analogResults, digiResults, resourceName);
                SentMessages.Add(Step);
                HMI.PanelStatus(EngineData.Screens.ImageMachineResults);
            }
        }

        public void ReleaseCycleMessages()
        {
            SentMessages.Clear();
        }

        

    }
}
