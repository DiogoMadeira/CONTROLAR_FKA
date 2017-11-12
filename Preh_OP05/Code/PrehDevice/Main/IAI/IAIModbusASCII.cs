using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace Preh
{
    public static class IAICommand
    {
       
        public static void Go2Pos(List<IAIModbusASCII> IAIs,int drive, EngineData.AxisName axis, EngineData.AxisPosition Position, int velocity)
        {
            Go2Pos(IAIs,drive, axis, Position, velocity, 0);
        }
        public static void Go2Pos(List<IAIModbusASCII> IAIs,int drive, EngineData.AxisName axis, int Position, int velocity)
        {
            uint Vel = (uint)velocity;
            ushort acc = (ushort)IAIs[drive].Axis[(int)axis].Position[(int)Position].Acceleration;
            IAIs[drive].Axis[(int)axis].MoveAndWait(IAIs[drive].Axis[(int)axis].Position[(int)Position].TargetPosition, 15000, false, Vel, acc);
        }
        public static void Go2Pos(List<IAIModbusASCII> IAIs,int drive, EngineData.AxisName axis, EngineData.AxisPosition Position, int velocity, int offset)
        {
            uint Vel = (uint)velocity;
            ushort acc = (ushort)IAIs[drive].Axis[(int)axis].Position[(int)Position].Acceleration;
            int pos = IAIs[drive].Axis[(int)axis].Position[(int)Position].TargetPosition + offset;

            IAIs[drive].Axis[(int)axis].MoveAndWait(pos, 15000, false, Vel, acc);
        }
        public static bool inPosition(List<IAIModbusASCII> IAIs, int drive, EngineData.AxisName axis, EngineData.AxisPosition Position)
        {
            return inPosition(IAIs, drive, axis, Position, 0);
        }
        public static bool inPosition(List<IAIModbusASCII> IAIs,int drive, EngineData.AxisName axis, EngineData.AxisPosition Position, int offset)
        {
            int postocheck = IAIs[drive].Axis[(int)axis].Position[(int)Position].TargetPosition + offset;
            int posread = 0;

            IAIs[drive].Axis[(int)axis].Device.ReadCurrentPosition(ref posread, Convert.ToByte((int)axis));

            return postocheck > posread - 20 && postocheck < posread + 20;
        }
        public static bool inPosition(List<IAIModbusASCII> IAIs,int drive, EngineData.AxisName axis, int Position)
        {
            int postocheck = 0;
            int posread = 0;
            postocheck = IAIs[drive].Axis[(int)axis].Position[(int)Position].TargetPosition;
            IAIs[drive].Axis[(int)axis].Device.ReadCurrentPosition(ref posread, Convert.ToByte((int)axis));
            if (postocheck > posread - 100 && postocheck < posread + 100)
            {
                return true;
            }
            return false;
        }


    }

    public class IAIModbusASCII
    {
        #region Variables / Registers

        #region Device control register 1
        const ushort SafetySpeedCommand = 0x0401;
        const ushort ServoOnCommand = 0x0403;
        const ushort AlarmResetCommand = 0x0407;
        const ushort BrakeForceReleaseCommand = 0x0408;
        const ushort PauseCommand = 0x040A;
        const ushort HomingCommand = 0x040B;
        const ushort PositionStartCommand = 0x040C;
        const ushort JogInchCommand = 0x0411;
        const ushort TeachingModeCommand = 0x0414;
        const ushort PositionDataLoadComamnd = 0x415;
        const ushort JogPlusCommand = 0x0416;
        const ushort JogMinusCommand = 0x0417;

        const ushort ModeValidModbus = 0x0427;
        #endregion Device control register 1

        #region Controller Monitor Information Registers
        const ushort CurrentPositionMonitor = 0x9000;
        const ushort PresentAlarmCodeQuery = 0x9002;
        const ushort InputPortQuery = 0x9003;
        const ushort OutputPortMonitorQuery = 0x9004;
        const ushort DeviceStatusQuery1 = 0x9005;
        const ushort DeviceStatusQuery2 = 0x9006;
        const ushort ExpansionDeviceStatusQuery = 0x9007;
        const ushort SystemStatusQuery = 0x9008;
        const ushort CurrentSpeedMonitor = 0x900A;
        const ushort CurrentAmpereMonitor = 0x900C;
        const ushort DeviationMonitor = 0x900E;
        const ushort SystemTimerQuery = 0x9010;
        const ushort SpecialInputPortQuery = 0x9012;
        const ushort ZoneStatusQuery = 0x9013;
        const ushort CompletePositionNumberStatusQuery = 0x9014;
        #endregion Controller Monitor Information Registers

        #region Device Status register
        public const ushort ServoOnStatus = 0x0103;
        public const ushort HomingCompletationStatus = 0x010B;
        public const ushort PositioningCompletationStatus = 0x10C;
        #endregion Device Status register       

        #endregion

        #region Properties
        private string _ComPort;
        public string ComPort { get { return _ComPort; } }

        private int _BaudRate;
        public int BaudRate { get { return _BaudRate; } }

        private byte _AxisId = 1;
        public byte AxisId { get { return _AxisId; } set { _AxisId = value; } }

        private ushort _ControlFlag = 0;
        public ushort ControlFlag { get { return _ControlFlag; } set { _ControlFlag = value; } }

        private ushort _Inposband = 10;
        public ushort Inposband { get { return _Inposband; } set { _Inposband = value; } }

        private uint _Speed = 5000;
        public uint Speed { get { return _Speed; } set { _Speed = value; } }

        private ushort _Acceleration = 20;
        public ushort Acceleration { get { return _Acceleration; } set { _Acceleration = value; } }

        private ushort _Decceleration = 20;
        public ushort Decceleration { get { return _Decceleration; } set { _Decceleration = value; } }

        private ushort _PushCurrentLimiting = 0;
        public ushort PushCurrentLimiting { get { return _PushCurrentLimiting; } set { _PushCurrentLimiting = value; } }

        private int _BondaryZonePosition_Low = 0;
        public int BondaryZonePosition_Low { get { return _BondaryZonePosition_Low; } set { _BondaryZonePosition_Low = value; } }

        private int _BondaryZonePosition_High = 0;
        public int BondaryZonePosition_High { get { return _BondaryZonePosition_High; } set { _BondaryZonePosition_High = value; } }

        private ushort _LoadOutputCurrentThreshold = 0;
        public ushort LoadOutputCurrentThreshold { get { return _LoadOutputCurrentThreshold; } set { _LoadOutputCurrentThreshold = value; } }

        public struct AxisPosition
        {
            public int ID;
            public string Name;
            public int TargetPosition;
            public ushort ControlFlag;
            public ushort Inposband;
            public uint Speed;
            public double Acceleration;
            public double Decceleration;
            public ushort PushCurrentLimiting;
            public int BondaryZonePosition_Low;
            public int BondaryZonePosition_High;
            public ushort LoadOutputCurrentThreshold;
        }
        public class AxisDevice : List<AxisPosition>
        {
            public class PositionsAccess : System.Collections.IEnumerator, System.Collections.IEnumerable
            {
                readonly AxisDevice parent;
                int posEnemurator = -1;

                internal PositionsAccess(AxisDevice p)
                {
                    parent = p;
                }

                public AxisPosition this[int i]
                {
                    get { return parent[i]; }
                }

                public object Current { get { return parent[posEnemurator]; } }

                public IEnumerator GetEnumerator() { return parent.GetEnumerator(); }

                public bool MoveNext()
                {
                    posEnemurator++;
                    if (posEnemurator < parent.Count) return true;
                    return false;
                }

                public void Reset()
                {
                    posEnemurator = -1;
                }
            }

            public byte ID;
            protected IAIModbusASCII device = null;
            public IAIModbusASCII Device { get { return device; } }
            protected List<AxisPosition> position = null;
            public PositionsAccess Position { get; }
            public string AlarmMessage { get; set; }

            public AxisDevice(IAIModbusASCII device)
            {
                this.device = device;
                position = new List<AxisPosition>();
                Position = new PositionsAccess(this);
            }

            public void AddPosition(AxisPosition p) { position.Add(p); }

            public int ReadCurrentAlarmCode()
            {
                int code = device.ReadCurrentAlarmCode(ID);
                AlarmMessage = device.ErrorMsg;
                return code;
            }
            public void AlarmReset() { device.AlarmReset(ID); }
            public void MotorInit() { device.MotorInit(ID); }
            public bool PowerAndWait(bool TurnOn) { return device.PowerAndWait(TurnOn, ID); }
            public bool HomeAndWait() { return device.HomeAndWait(ID); }
            public bool MoveAndWait(int TargetPos, uint MovTimeout, bool blnWaitPosOK) { return device.MoveAndWait(TargetPos, MovTimeout, blnWaitPosOK, ID); }
            public bool MoveAndWait(int TargetPos, uint MovTimeout, bool blnWaitPosOK, uint Speed, ushort Acceleration)
            {
                return device.MoveAndWait(TargetPos, MovTimeout, blnWaitPosOK, ID, Speed, Acceleration);
            }
        }
        public class AxisGroup : Dictionary<int, AxisDevice>
        {
            protected IAIModbusASCII device = null;
            public AxisGroup(IAIModbusASCII device) { this.device = device; }

            public void Add(byte ID)
            {
                AxisDevice temp = new AxisDevice(device);
                temp.ID = ID;
                this.Add(ID, temp);
            }
        }

        private AxisGroup _axis;
        public AxisGroup Axis { get { return _axis; } }

        private string _ErrorMsg;
        public string ErrorMsg { get { return _ErrorMsg; } }
        #endregion properties

        SerialPort comPort;
        byte TimeOut = 10;    // 10 msec is safe

        #region primitive functions
        private int Convert4ByteToInt(byte Byte1, byte Byte2, byte Byte3, byte Byte4)
        {
            try
            {
                return Byte3 * (int)Math.Pow(256, 3) + Byte4 * (int)Math.Pow(256, 2) + Byte1 * 256 + Byte2;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private byte HexToByte(string hex)
        {
            try
            {
                if (hex.Length > 2 || hex.Length <= 0)
                    throw new ArgumentException("hex must be 1 or 2 characters in length");
                byte newByte = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                return newByte;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private string BuildAsciiCommand(byte[] chkbuf, byte len)
        {
            try
            {
                string CRLF = "\r\n";
                uint LRC;
                string auxLRC = ":";

                byte uchLRC = 0;

                for (int i = 0; i < len; i++)
                {
                    uchLRC += chkbuf[i];
                }
                LRC = ((byte)(-((uchLRC))));

                //junta a trama toda em Hexa("X")
                for (int i = 0; i < len; i++)
                {
                    auxLRC = auxLRC + chkbuf[i].ToString("X").PadLeft(2, '0');
                }

                string strCommand = auxLRC + LRC.ToString("X").PadLeft(2, '0') + CRLF;

                return strCommand;
            }
            catch (Exception)
            {
                return "";
            }


        }

        private bool ReadCoilStatus(byte SlaveAddress, ushort StartAddress, ushort Count, byte[] RetBuf)
        {
            try
            {
                byte[] ar = new byte[6];
                //UInt16 CRC=0;
                const byte FunctionCode = 0x01;

                ar[0] = SlaveAddress;
                ar[1] = FunctionCode;                           // function code
                ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
                ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from
                ar[4] = Convert.ToByte(Count / 256);            // Count High
                ar[5] = Convert.ToByte(Count % 256);            // Count Low (number of locations to be read)

                string AsciiCommand = BuildAsciiCommand(ar, 6);

                comPort.Write(AsciiCommand);

                System.Threading.Thread.Sleep(TimeOut * ar.Length);
                comPort.Read(RetBuf, 0, comPort.BytesToRead);

                if ((RetBuf[0] == SlaveAddress) && (RetBuf[1] == FunctionCode))
                    return true;
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool ReadInputStatus(Byte SlaveAddress, ushort StartAddress, ushort Count, byte[] RetBuf)
        {
            try
            {
                byte[] ar = new byte[6];
                const byte FunctionCode = 0x02;

                ar[0] = SlaveAddress;
                ar[1] = FunctionCode;                           // function code
                ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
                ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from
                ar[4] = Convert.ToByte(Count / 256);            // Count High
                ar[5] = Convert.ToByte(Count % 256);            // Count Low (number of locations to be read)            

                string AsciiCommand = BuildAsciiCommand(ar, 6);

                comPort.Write(AsciiCommand);

                System.Threading.Thread.Sleep(TimeOut * ar.Length);
                comPort.Read(RetBuf, 0, comPort.BytesToRead);

                byte SlaveAdd_Received = HexToByte(Convert.ToChar(RetBuf[1]).ToString() + Convert.ToChar(RetBuf[2]).ToString());
                byte FunctionCode_Received = HexToByte(Convert.ToChar(RetBuf[3]).ToString() + Convert.ToChar(RetBuf[4]).ToString());

                if ((SlaveAdd_Received == SlaveAddress) && (FunctionCode_Received == FunctionCode))
                    return true;
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }

        private bool ForceSingleCoil(byte SlaveAddress, ushort StartAddress, bool Status, byte[] RetBuf)
        {
            try
            {
                byte[] ar = new byte[6];
                const byte FunctionCode = 0x05;

                ar[0] = SlaveAddress;
                ar[1] = FunctionCode;                           // function code
                ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
                ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from
                if (Status == true)
                {
                    ar[4] = 0xFF;
                    ar[5] = 0x00;
                }
                else
                {
                    ar[4] = 0x00;
                    ar[5] = 0x00;
                }

                string AsciiCommand = BuildAsciiCommand(ar, 6);

                comPort.Write(AsciiCommand);

                System.Threading.Thread.Sleep(TimeOut * ar.Length);
                comPort.Read(RetBuf, 0, comPort.BytesToRead);

                byte SlaveAdd_Received = HexToByte(Convert.ToChar(RetBuf[1]).ToString() + Convert.ToChar(RetBuf[2]).ToString());
                byte FunctionCode_Received = HexToByte(Convert.ToChar(RetBuf[3]).ToString() + Convert.ToChar(RetBuf[4]).ToString());

                if ((SlaveAdd_Received == SlaveAddress) && (FunctionCode_Received == FunctionCode))
                    return true;
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }

        private bool ReadHoldingRegisters(byte SlaveAddress, ushort StartAddress, ushort Count, byte[] RetBuf)
        {
            try
            {
                byte[] ar = new byte[6];
                const byte FunctionCode = 0x03;

                ar[0] = SlaveAddress;
                ar[1] = FunctionCode;                           // function code
                ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
                ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from
                ar[4] = Convert.ToByte(Count / 256);            // Count High
                ar[5] = Convert.ToByte(Count % 256);            // Count Low (number of locations to be read)            

                string AsciiCommand = BuildAsciiCommand(ar, 6);

                comPort.Write(AsciiCommand);

                System.Threading.Thread.Sleep(TimeOut * ar.Length);
                comPort.Read(RetBuf, 0, comPort.BytesToRead);

                byte SlaveAdd_Received = HexToByte(Convert.ToChar(RetBuf[1]).ToString() + Convert.ToChar(RetBuf[2]).ToString());
                byte FunctionCode_Received = HexToByte(Convert.ToChar(RetBuf[3]).ToString() + Convert.ToChar(RetBuf[4]).ToString());

                if ((SlaveAdd_Received == SlaveAddress) && (FunctionCode_Received == FunctionCode))
                    return true;
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }

        private void ReadInputRegisters()
        {
            try
            {

            }
            catch (Exception)
            {

            }
        }

        private bool PresetSingleRegister(byte SlaveAddress, ushort StartAddress, Int16 Value, byte[] RetBuf)
        {
            try
            {
                byte[] ar = new byte[6];
                const byte FunctionCode = 0x6;

                ar[0] = SlaveAddress;
                ar[1] = FunctionCode;                           // function code
                ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
                ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from
                ar[4] = Convert.ToByte(Value / 256);            // Value High
                ar[5] = Convert.ToByte(Value % 256);            // Value Low

                string AsciiCommand = BuildAsciiCommand(ar, 6);

                comPort.Write(AsciiCommand);

                System.Threading.Thread.Sleep(TimeOut * ar.Length);
                comPort.Read(RetBuf, 0, comPort.BytesToRead);

                byte SlaveAdd_Received = HexToByte(Convert.ToChar(RetBuf[1]).ToString() + Convert.ToChar(RetBuf[2]).ToString());
                byte FunctionCode_Received = HexToByte(Convert.ToChar(RetBuf[3]).ToString() + Convert.ToChar(RetBuf[4]).ToString());

                if ((SlaveAdd_Received == SlaveAddress) && (FunctionCode_Received == FunctionCode))
                    return true;
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }

        public bool ReadExeptionStatus(byte SlaveAddress, ref byte ExceptionStatus)
        {
            byte[] RetBuf = new byte[5];
            byte[] ar = new byte[2];

            const byte FunctionCode = 0x07;

            ar[0] = SlaveAddress;
            ar[1] = FunctionCode;                           // function code

            string AsciiCommand = BuildAsciiCommand(ar, 2);

            comPort.Write(AsciiCommand);

            System.Threading.Thread.Sleep(TimeOut * ar.Length);
            comPort.Read(RetBuf, 0, comPort.BytesToRead);

            byte SlaveAdd_Received = HexToByte(Convert.ToChar(RetBuf[1]).ToString() + Convert.ToChar(RetBuf[2]).ToString());
            byte FunctionCode_Received = HexToByte(Convert.ToChar(RetBuf[3]).ToString() + Convert.ToChar(RetBuf[4]).ToString());

            if ((SlaveAdd_Received == SlaveAddress) && (FunctionCode_Received == FunctionCode))
            {
                ExceptionStatus = RetBuf[2];
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool PresetMultipleRegisters(byte SlaveAddress, ushort StartAddress, ushort RegisterCount, ushort ByteCount, byte[] InpBuf, byte[] RetBuf)
        {
            try
            {
                byte[] ar = new byte[6];
                const byte FunctionCode = 0x10;   // 16 dec

                ar[0] = SlaveAddress;
                ar[1] = FunctionCode;                           // function code
                ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
                ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from
                ar[4] = Convert.ToByte(RegisterCount / 256);    // Count High
                ar[5] = Convert.ToByte(RegisterCount % 256);    // Count Low (number of locations to be read)           

                string AsciiCommand = BuildAsciiCommand(ar, 6);

                comPort.Write(AsciiCommand);

                System.Threading.Thread.Sleep(TimeOut * ar.Length);
                comPort.Read(RetBuf, 0, comPort.BytesToRead);

                byte SlaveAdd_Received = HexToByte(Convert.ToChar(RetBuf[1]).ToString() + Convert.ToChar(RetBuf[2]).ToString());
                byte FunctionCode_Received = HexToByte(Convert.ToChar(RetBuf[3]).ToString() + Convert.ToChar(RetBuf[4]).ToString());

                if ((SlaveAdd_Received == SlaveAddress) && (FunctionCode_Received == FunctionCode))
                    return true;
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }
        #endregion primitive functions

        #region Public Methods
        public IAIModbusASCII(String IAI_ComPort, int IAI_BaudRate)
        {
            _ComPort = IAI_ComPort;
            _BaudRate = IAI_BaudRate;

            _axis = new AxisGroup(this);
        }

        #region ForceSingleCoil -- Write

        /// <summary>        
        /// Register Bit [ServoOnStatus] is active when servo is ON
        /// </summary>
        /// <param name="SOn">Set true to turn it On, or false to turn it off.</param>
        /// <returns></returns>
        public bool ServoOn(bool SOn)
        {
            return ServoOn(SOn, _AxisId);
        }
        /// <summary>
        /// Register Bit [ServoOnStatus] is active when servo is ON
        /// </summary>
        /// <param name="SOn">Set true to turn it On, or false to turn it off.</param>
        /// <param name="SlaveAddress"></param>
        /// <returns></returns>
        public bool ServoOn(bool SOn, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (!ForceSingleCoil(SlaveAddress, ServoOnCommand, SOn, ar))
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command Servo " + (SOn ? "ON" : "OFF");
                    return false;
                }
                else return true;
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command Servo " + (SOn ? "ON" : "OFF");
                return false;
            }
        }

        public bool SafetySpeed(bool OnOff)
        {
            return SafetySpeed(OnOff, _AxisId);
        }
        public bool SafetySpeed(bool OnOff, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (!ForceSingleCoil(SlaveAddress, SafetySpeedCommand, OnOff, ar))
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command SafetySpeed " + (OnOff ? "ON" : "OFF");
                    return false;
                }
                else return true;
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command SafetySpeed " + (OnOff ? "ON" : "OFF");
                return false;
            }

        }

        public bool ChangeModeValidModbus(bool OnOff)
        {
            return ChangeModeValidModbus(OnOff, _AxisId);
        }
        public bool ChangeModeValidModbus(bool OnOff, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (!ForceSingleCoil(SlaveAddress, ModeValidModbus, OnOff, ar))
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command ChangeModeValidModbus " + (OnOff ? "ON" : "OFF");
                    return false;
                }
                else return true;
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command ChangeModeValidModbus " + (OnOff ? "ON" : "OFF");
                return false;
            }

        }

        public void AlarmReset()
        {
            AlarmReset(_AxisId);
        }
        public void AlarmReset(byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (!(ForceSingleCoil(SlaveAddress, AlarmResetCommand, true, ar) && ForceSingleCoil(SlaveAddress, AlarmResetCommand, false, ar)))
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                throw new Exception("IAI axis[" + SlaveAddress + "] - Exception in command AlarmReset");
            }

        }

        public bool Pause(bool Status)
        {
            return Pause(Status, _AxisId);
        }
        public bool Pause(bool Status, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (!ForceSingleCoil(SlaveAddress, PauseCommand, Status, ar))
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command Pause " + (Status ? "ON" : "OFF");
                    return false;
                }
                else return true;
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command Pause " + (Status ? "ON" : "OFF");
                return false;
            }

        }

        /// <summary>
        /// Register Bit [HomingCompletationStatus] is active when axis is at home
        /// </summary>
        /// <returns></returns>
        public bool Homing()
        {
            return Homing(_AxisId);
        }
        /// <summary>
        /// Register Bit [HomingCompletationStatus] is active when axis is at home
        /// </summary>
        /// <param name="SlaveAddress"></param>
        /// <returns></returns>
        public bool Homing(byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];

                if (!(ForceSingleCoil(SlaveAddress, HomingCommand, true, ar) && ForceSingleCoil(SlaveAddress, HomingCommand, false, ar)))
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command Homing";
                    return false;
                }
                else return true;
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command Homing";
                return false;
            }

        }

        public bool PositionStart()
        {
            return PositionStart(_AxisId);
        }
        public bool PositionStart(byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (!(ForceSingleCoil(SlaveAddress, PositionStartCommand, true, ar) && ForceSingleCoil(SlaveAddress, PositionStartCommand, false, ar)))
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command PositionStart";
                    return false;
                }
                else return true;
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command PositionStart";
                return false;
            }

        }

        /// <summary>
        /// </summary>
        /// <param name="OnOff">Set it to true to Jogging or false to Inching.</param>
        /// <returns></returns>
        public bool JogInchSwitching(bool OnOff)
        {
            return JogInchSwitching(OnOff, _AxisId);
        }
        /// <summary>
        /// </summary>
        /// <param name="OnOff">Set it to true to Jogging or false to Inching.</param>
        /// <param name="SlaveAddress"></param>
        /// <returns></returns>
        public bool JogInchSwitching(bool OnOff, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (!ForceSingleCoil(SlaveAddress, JogInchCommand, OnOff, ar))
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command JogInchSwitching " + (OnOff ? "ON" : "OFF");
                    return false;
                }
                else return true;
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command JogInchSwitching " + (OnOff ? "ON" : "OFF");
                return false;
            }

        }

        public bool JogPlus(bool OnOff)
        {
            return JogPlus(OnOff, _AxisId);
        }
        public bool JogPlus(bool OnOff, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (!ForceSingleCoil(SlaveAddress, JogPlusCommand, OnOff, ar))
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command JogPlus " + (OnOff ? "ON" : "OFF");
                    return false;
                }
                else return true;
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command JogPlus " + (OnOff ? "ON" : "OFF");
                return false;
            }

        }

        public bool JogMinus(bool OnOff)
        {
            return JogMinus(OnOff, _AxisId);
        }
        public bool JogMinus(bool OnOff, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (!ForceSingleCoil(SlaveAddress, JogMinusCommand, OnOff, ar))
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command JogMinus " + (OnOff ? "ON" : "OFF");
                    return false;
                }
                else return true;
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command JogMinus " + (OnOff ? "ON" : "OFF");
                return false;
            }
        }

        /// <summary>
        /// Move axis to position with same parameter to Acceleration and Decceleration (_Acceleration)
        /// Register Bit [PositioningCompletationStatus] is active when target reached
        /// </summary>
        /// <param name="TargetPosition"></param>
        /// <returns></returns>
        public bool MoveActuator(int TargetPosition)
        {
            byte[] Ret = new byte[255];

            return MoveActuator(TargetPosition, _AxisId, _Inposband, _Speed, _Acceleration, _PushCurrentLimiting, _ControlFlag, Ret);
        }
        /// <summary>
        /// Move axis to position with same parameter to Acceleration and Decceleration (_Acceleration)
        /// Register Bit [PositioningCompletationStatus] is active when target reached
        /// </summary>
        /// <param name="TargetPosition"></param>
        /// <param name="SlaveAddress"></param>
        /// <returns></returns>
        public bool MoveActuator(int TargetPosition, byte SlaveAddress)
        {
            byte[] Ret = new byte[255];

            return MoveActuator(TargetPosition, SlaveAddress, _Inposband, _Speed, _Acceleration, _PushCurrentLimiting, _ControlFlag, Ret);
        }
        /// <summary>
        /// Move axis to position with same parameter to Acceleration and Decceleration (_Acceleration)
        /// Register Bit [PositioningCompletationStatus] is active when target reached
        /// </summary>
        /// <param name="TargetPosition"></param>
        /// <param name="SlaveAddress"></param>
        /// <param name="InPositionBand"></param>
        /// <param name="Speed"></param>
        /// <param name="AccDec"></param>
        /// <returns></returns>
        public bool MoveActuator(int TargetPosition, byte SlaveAddress, ushort InPositionBand, uint Speed, ushort AccDec)
        {
            byte[] Ret = new byte[255];

            return MoveActuator(TargetPosition, SlaveAddress, InPositionBand, Speed, AccDec, _PushCurrentLimiting, _ControlFlag, Ret);
        }
        /// <summary>
        /// Move axis to position with same parameter to Acceleration and Decceleration (_Acceleration)
        /// Register Bit [PositioningCompletationStatus] is active when target reached
        /// </summary>
        /// <param name="TargetPosition"></param>
        /// <param name="SlaveAddress"></param>
        /// <param name="InPositionBand"></param>
        /// <param name="Speed"></param>
        /// <param name="AccDec"></param>
        /// <param name="PushCurrentLimiting"></param>
        /// <param name="ControlFlag"></param>
        /// <param name="RetBuf"></param>
        /// <returns></returns>
        public bool MoveActuator(int TargetPosition, byte SlaveAddress, ushort InPositionBand, uint Speed, ushort AccDec, ushort PushCurrentLimiting, ushort ControlFlag, byte[] RetBuf)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[25];

                const byte FunctionCode = 0x10;   // 16 dec

                int P3 = (int)Math.Pow(256, 3);
                int P2 = (int)Math.Pow(256, 2);
                int P1 = 256;
                int StartAddress = 0x9900;

                ar[0] = SlaveAddress;
                ar[1] = FunctionCode;                           // function code
                ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
                ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from

                ar[4] = 0;
                ar[5] = 9;                                      // resgister count
                ar[6] = 18;                                     // byte count

                ar[7] = Convert.ToByte(TargetPosition / P3);
                ar[8] = Convert.ToByte((TargetPosition % P3) / P2);

                ar[9] = Convert.ToByte((TargetPosition % P2) / P1);
                ar[10] = Convert.ToByte((TargetPosition % P1));

                ar[11] = Convert.ToByte(InPositionBand / P3);
                ar[12] = Convert.ToByte((InPositionBand % P3) / P2);
                ar[13] = Convert.ToByte((InPositionBand % P2) / P1);
                ar[14] = Convert.ToByte((InPositionBand % P1));

                ar[15] = Convert.ToByte(Speed / P3);
                ar[16] = Convert.ToByte((Speed % P3) / P2);
                ar[17] = Convert.ToByte((Speed % P2) / P1);
                ar[18] = Convert.ToByte((Speed % P1));

                ar[19] = Convert.ToByte(AccDec / 256);
                ar[20] = Convert.ToByte(AccDec % 256);

                ar[21] = Convert.ToByte(PushCurrentLimiting / 256);
                ar[22] = Convert.ToByte(PushCurrentLimiting % 256);

                ar[23] = Convert.ToByte(ControlFlag / 256);
                ar[24] = Convert.ToByte(ControlFlag % 256);

                string AsciiCommand = BuildAsciiCommand(ar, 25);

                comPort.Write(AsciiCommand);

                System.Threading.Thread.Sleep(TimeOut * ar.Length);
                comPort.Read(RetBuf, 0, comPort.BytesToRead);

                byte SlaveAdd_Received = HexToByte(Convert.ToChar(RetBuf[1]).ToString() + Convert.ToChar(RetBuf[2]).ToString());
                byte FunctionCode_Received = HexToByte(Convert.ToChar(RetBuf[3]).ToString() + Convert.ToChar(RetBuf[4]).ToString());

                if ((SlaveAdd_Received == SlaveAddress) && (FunctionCode_Received == FunctionCode))
                    return true;
                else
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command MoveActuator";
                    return false;
                }
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command MoveActuator";
                return false;
            }

        }

        /// <summary>
        /// Move axis to position with Acceleration and Decceleration in two parameters and Bondary Positions to activated digital outputs
        /// Register Bit [PositioningCompletationStatus] is active when target reached
        /// </summary>
        /// <param name="TargetPosition"></param>
        /// <param name="SlaveAddress"></param>
        /// <param name="InPositionBand"></param>
        /// <param name="Speed"></param>
        /// <param name="AccDec"></param>
        /// <param name="PushCurrentLimiting"></param>
        /// <param name="ControlFlag"></param>
        /// <param name="RetBuf"></param>
        /// <returns></returns>
        public bool MoveActuator(uint TargetPosition, byte SlaveAddress, ushort InPositionBand,
                        uint Speed, int BondaryZonePosition_Low, int BondaryZonePosition_High,
                        ushort Acceleration, ushort Decceleration, ushort PushCurrentLimiting,
                        ushort LoadOutputCurrentThreshold, ushort ControlFlag, byte[] RetBuf)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[37];

                const byte FunctionCode = 0x10;   // 16 dec

                int P3 = (int)Math.Pow(256, 3);
                int P2 = (int)Math.Pow(256, 2);
                int P1 = 256;
                int StartAddress = 0x10C0;

                ar[0] = SlaveAddress;
                ar[1] = FunctionCode;                           // function code
                ar[2] = Convert.ToByte(StartAddress / 256);     // High Address to read from
                ar[3] = Convert.ToByte(StartAddress % 256);     // Low Address to read from

                ar[4] = 0;
                ar[5] = 15;                                      // resgister count
                ar[6] = 30;                                     // byte count

                ar[7] = Convert.ToByte((TargetPosition % P3) / P2);
                ar[8] = Convert.ToByte(TargetPosition / P3);
                ar[9] = Convert.ToByte((TargetPosition % P2) / P1);
                ar[10] = Convert.ToByte((TargetPosition % P1));

                ar[11] = Convert.ToByte((InPositionBand % P3) / P2);
                ar[12] = Convert.ToByte(InPositionBand / P3);
                ar[13] = Convert.ToByte((InPositionBand % P2) / P1);
                ar[14] = Convert.ToByte((InPositionBand % P1));

                ar[15] = Convert.ToByte((Speed % P3) / P2);
                ar[16] = Convert.ToByte(Speed / P3);
                ar[17] = Convert.ToByte((Speed % P2) / P1);
                ar[18] = Convert.ToByte((Speed % P1));

                //Limites de uma qualquer zona que possamos querer activar uma saída no controlador High>Low
                ////////////////////////////////////////////////////////////////////////////////////////////
                ar[19] = Convert.ToByte((BondaryZonePosition_High % P3) / P2);
                ar[20] = Convert.ToByte(BondaryZonePosition_High / P3);
                ar[21] = Convert.ToByte((BondaryZonePosition_High % P2) / P1);
                ar[22] = Convert.ToByte((BondaryZonePosition_High % P1));

                ar[23] = Convert.ToByte((BondaryZonePosition_Low % P3) / P2);
                ar[24] = Convert.ToByte(BondaryZonePosition_Low / P3);
                ar[25] = Convert.ToByte((BondaryZonePosition_Low % P2) / P1);
                ar[26] = Convert.ToByte((BondaryZonePosition_Low % P1));
                /////////////////////////////////////////////////////////////////////////////////////////////

                ar[27] = Convert.ToByte(Acceleration / 256);
                ar[28] = Convert.ToByte(Acceleration % 256);

                ar[29] = Convert.ToByte(Decceleration / 256);
                ar[30] = Convert.ToByte(Decceleration % 256);

                ar[31] = Convert.ToByte(PushCurrentLimiting / 256);
                ar[32] = Convert.ToByte(PushCurrentLimiting % 256);

                ar[33] = Convert.ToByte(LoadOutputCurrentThreshold / 256);
                ar[34] = Convert.ToByte(LoadOutputCurrentThreshold % 256);

                ar[35] = Convert.ToByte(ControlFlag / 256);
                ar[36] = Convert.ToByte(ControlFlag % 256);

                string AsciiCommand = BuildAsciiCommand(ar, 36);

                comPort.Write(AsciiCommand);

                System.Threading.Thread.Sleep(TimeOut * ar.Length);
                comPort.Read(RetBuf, 0, comPort.BytesToRead);

                byte SlaveAdd_Received = HexToByte(Convert.ToChar(RetBuf[1]).ToString() + Convert.ToChar(RetBuf[2]).ToString());
                byte FunctionCode_Received = HexToByte(Convert.ToChar(RetBuf[3]).ToString() + Convert.ToChar(RetBuf[4]).ToString());

                if ((SlaveAdd_Received == SlaveAddress) && (FunctionCode_Received == FunctionCode))
                    return true;
                else
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command MoveActuator";
                    return false;
                }
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command MoveActuator";
                return false;
            }

        }

        #endregion ForceSingleCoil -- Write

        #region (Read holding registers) -- Read Controller Monitor Information Registers
        public bool ReadCurrentPosition(ref int MotorPos)
        {
            return ReadCurrentPosition(ref MotorPos, _AxisId);
        }
        public bool ReadCurrentPosition(ref int MotorPos, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (ReadHoldingRegisters(SlaveAddress, CurrentPositionMonitor, 2, ar))
                {
                    char[] charAr = new char[8];
                    byte[] byteAr = new byte[4];

                    for (int i = 7; i < 15; i++)
                    {
                        charAr[i - 7] = Convert.ToChar(ar[i]);
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        string strHex = charAr[2 * i].ToString() + charAr[2 * i + 1].ToString();
                        try
                        {
                            byteAr[i] = HexToByte(strHex);
                        }
                        catch (Exception e)
                        {
                            _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command ReadCurrentPosition";
                            return false;
                        }
                    }
                    MotorPos = (byteAr[1] * (int)Math.Pow(256, 2) + byteAr[2] * 256 + byteAr[3]);

                    return true;
                }
                else
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command ReadCurrentPosition";
                    return false;
                }
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command ReadCurrentPosition";
                return false;
            }

        }

        public int ReadCurrentAlarmCode()
        {
            return ReadCurrentAlarmCode(_AxisId);
        }
        public int ReadCurrentAlarmCode(byte SlaveAddress)
        {
            int AlarmCode = -1;
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (ReadHoldingRegisters(SlaveAddress, PresentAlarmCodeQuery, 1, ar))
                {
                    byte AlarmMSB_Received = HexToByte(Convert.ToChar(ar[7]).ToString() + Convert.ToChar(ar[8]).ToString());
                    byte AlarmLSB_Received = HexToByte(Convert.ToChar(ar[9]).ToString() + Convert.ToChar(ar[10]).ToString());

                    AlarmCode = AlarmMSB_Received * 256 + AlarmLSB_Received;
                    switch (AlarmCode)
                    {
                        case 128: _ErrorMsg = "Motion command while in SERVO-OFF condition."; break;
                    }
                }
                else
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                throw new Exception("IAI axis[" + SlaveAddress + "] - Exception in command ReadPresentAlarmCode");
            }

            return AlarmCode;
        }

        public bool ReadDeviceStatusQuery1(ref int Status)
        {
            return ReadDeviceStatusQuery1(ref Status, _AxisId);
        }
        public bool ReadDeviceStatusQuery1(ref int Status, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (ReadHoldingRegisters(SlaveAddress, ZoneStatusQuery, 1, ar))
                {
                    Status = ar[3] * 256 + ar[4];
                    return true;
                }
                else
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command ReadDeviceStatusQuery1";
                    return false;
                }
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command ReadDeviceStatusQuery1";
                return false;
            }

        }

        public bool ReadSystemStatus(ref int Status)
        {
            return ReadSystemStatus(ref Status, _AxisId);
        }
        public bool ReadSystemStatus(ref int Status, byte SlaveAddress)
        {
            try
            {
                // 2 registers (4 byte)
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (ReadHoldingRegisters(SlaveAddress, SystemStatusQuery, 2, ar))
                {
                    Status = Convert4ByteToInt(ar[3], ar[4], ar[5], ar[6]);
                    return true;
                }
                else
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command ReadSystemStatus";
                    return false;
                }
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command ReadSystemStatus";
                return false;
            }

        }

        public bool ReadCurrentSpeed(ref int Speed)
        {
            return ReadCurrentSpeed(ref Speed, _AxisId);
        }
        public bool ReadCurrentSpeed(ref int Speed, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (ReadHoldingRegisters(SlaveAddress, CurrentSpeedMonitor, 2, ar))
                {
                    Speed = Convert4ByteToInt(ar[3], ar[4], ar[5], ar[6]);
                    return true;
                }
                else
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command ReadCurrentSpeed";
                    return false;
                }
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command ReadCurrentSpeed";
                return false;
            }

        }

        public bool ReadCurrentAmpere(ref int Ampere)
        {
            return ReadCurrentAmpere(ref Ampere, _AxisId);
        }
        public bool ReadCurrentAmpere(ref int Ampere, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (ReadHoldingRegisters(SlaveAddress, CurrentAmpereMonitor, 2, ar))
                {
                    Ampere = Convert4ByteToInt(ar[3], ar[4], ar[5], ar[6]);
                    return true;
                }
                else
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command ReadCurrentAmpere";
                    return false;
                }
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command ReadCurrentAmpere";
                return false;
            }

        }

        public bool ReadDeviation(ref int Dev)
        {
            return ReadDeviation(ref Dev, _AxisId);
        }
        public bool ReadDeviation(ref int Dev, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (ReadHoldingRegisters(SlaveAddress, DeviationMonitor, 2, ar))
                {
                    Dev = Convert4ByteToInt(ar[3], ar[4], ar[5], ar[6]);
                    return true;
                }
                else
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command ReadDeviation";
                    return false;
                }
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command ReadDeviation";
                return false;
            }

        }

        public bool ReadSystemTimer(ref int msec)
        {
            return ReadSystemTimer(ref msec, _AxisId);
        }
        public bool ReadSystemTimer(ref int msec, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (ReadHoldingRegisters(SlaveAddress, SystemTimerQuery, 2, ar))
                {
                    msec = Convert4ByteToInt(ar[3], ar[4], ar[5], ar[6]);
                    return true;
                }
                else
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command ReadSystemTimer";
                    return false;
                }
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command ReadSystemTimer";
                return false;
            }

        }

        public bool ReadZoneStatus(ref int ZStatus)
        {
            return ReadZoneStatus(ref ZStatus, _AxisId);
        }
        public bool ReadZoneStatus(ref int ZStatus, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (ReadHoldingRegisters(SlaveAddress, ZoneStatusQuery, 1, ar))
                {
                    ZStatus = ar[3] * 256 + ar[4];
                    return true;
                }
                else
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command ReadZoneStatus";
                    return false;
                }
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command ReadZoneStatus";
                return false;

            }
        }

        public bool ReadRegisterBit(ushort BitAddress, ref bool OnOff)
        {
            return ReadRegisterBit(BitAddress, ref OnOff, _AxisId);
        }
        public bool ReadRegisterBit(ushort BitAddress, ref bool OnOff, byte SlaveAddress)
        {
            try
            {
                _ErrorMsg = "";
                byte[] ar = new byte[255];
                if (ReadInputStatus(SlaveAddress, BitAddress, 1, ar))
                {
                    byte PosOkFlag = HexToByte(Convert.ToChar(ar[7]).ToString() + Convert.ToChar(ar[8]).ToString());

                    if (PosOkFlag == 0)
                    {
                        OnOff = false;
                        return true;
                    }
                    else if (PosOkFlag == 1)
                    {
                        OnOff = true;
                        return true;
                    }
                    else
                    {
                        _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command ReadRegisterBit " + BitAddress;
                        return false;
                    }
                }
                else
                {
                    _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Error in command ReadRegisterBit " + BitAddress;
                    return false;
                }
            }
            catch (Exception)
            {
                _ErrorMsg = "IAI axis[" + SlaveAddress + "] - Exception in command ReadRegisterBit " + BitAddress;
                return false;
            }
        }
        #endregion (Read holding registers) -- Read Controller Monitor Information Registers               

        public void OpenComPort()
        {
            try
            {
                _ErrorMsg = "";
                comPort = new SerialPort(_ComPort, _BaudRate, Parity.None, 8, StopBits.One)
                {
                    Handshake = Handshake.None,
                    ReceivedBytesThreshold = 1
                };
                comPort.Open();
            }
            catch (Exception ex)
            {
                Log.Instance.Error("Can't connect to the IAI.", ex);
                throw new Exception("Can't connect to the IAI.", ex);
            }
        }
        public void CloseComPort()
        {
            try
            {
                if (comPort.IsOpen) comPort.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("Can't disconnect from the IAI.", ex);
            }
        }

        // Movements
        public void MotorInit() { MotorInit(_AxisId); }
        public void MotorInit(byte SlaveAddress)
        {
            byte[] ret = new byte[256];

            try
            {
                //ChangeModeValidModbus - only in SCON
                if (!this.ChangeModeValidModbus(true, SlaveAddress)) throw new Exception("Can't change mode valid Modbus.");

                // Clean alarms
                int intAlarm = 0;

                this.AlarmReset(SlaveAddress);
                intAlarm = this.ReadCurrentAlarmCode(SlaveAddress);
                if (intAlarm != 0) throw new Exception("Axis controller with alarm code " + intAlarm);

                //Servo ON
                if (!PowerAndWait(true, SlaveAddress)) throw new Exception("Impossible to turn on the Axis.");

                //Turn off Velocity Mode
                if (!this.SafetySpeed(false, SlaveAddress)) throw new Exception("Axis didn't turn off velocity mode.");
            }
            catch (Exception ex)
            {
                throw new Exception("Can't start the IAI" + SlaveAddress + ".", ex);
            }
        }

        public bool PowerAndWait(bool TurnOn) { return PowerAndWait(TurnOn, _AxisId); }
        public bool PowerAndWait(bool TurnOn, byte SlaveAddress)
        {
            double Ti, Tf;
            bool bit = false;

            Ti = DateTime.Now.TimeOfDay.TotalMilliseconds;
            do
            {
                Tf = DateTime.Now.TimeOfDay.TotalMilliseconds;
                if (Tf < Ti) Ti = Tf;
                if ((Tf - Ti) >= 5000) break;

                this.ServoOn(TurnOn, SlaveAddress);
                Thread.Sleep(200);
                this.ReadRegisterBit(IAIModbusASCII.ServoOnStatus, ref bit, SlaveAddress);
            } while (bit != TurnOn);

            if (bit != TurnOn) return false;

            return true;
        }

        public bool HomeAndWait() { return HomeAndWait(_AxisId); }
        public bool HomeAndWait(byte SlaveAddress)
        {
            double Ti, Tf, dblTimeout;
            bool bit = false;

            dblTimeout = 90000;

            this.Homing(SlaveAddress);
            Ti = DateTime.Now.TimeOfDay.TotalMilliseconds;
            do
            {
                Tf = DateTime.Now.TimeOfDay.TotalMilliseconds;
                if (Tf < Ti) Ti = Tf;
                if ((Tf - Ti) > dblTimeout) break;
                Thread.Sleep(200);
                this.ReadRegisterBit(IAIModbusASCII.HomingCompletationStatus, ref bit, SlaveAddress);
            }
            while (!bit);
            if (!bit) return false;

            return true;
        }

        public bool MoveAndWait(int TargetPos, uint MovTimeout, bool blnWaitPosOK) { return MoveAndWait(TargetPos, MovTimeout, blnWaitPosOK, _AxisId); }
        public bool MoveAndWait(int TargetPos, uint MovTimeout, bool blnWaitPosOK, byte SlaveAddress)
        {
            double Ti, Tf;
            bool bit = false;

            //Read actual position
            int MotorPos = -999;

            if (!this.ReadCurrentPosition(ref MotorPos, SlaveAddress)) return false;

            if (MotorPos != TargetPos)
            {
                this.MoveActuator(TargetPos, SlaveAddress);

                if (!blnWaitPosOK) return true;

                Ti = DateTime.Now.TimeOfDay.TotalMilliseconds;
                do
                {
                    Tf = DateTime.Now.TimeOfDay.TotalMilliseconds;
                    if (Tf < Ti) Ti = Tf;
                    if ((Tf - Ti) > MovTimeout) break;
                    Thread.Sleep(200);
                    this.ReadRegisterBit(IAIModbusASCII.PositioningCompletationStatus, ref bit, SlaveAddress);
                }
                while (!bit);
                if (!bit) return false;
            }
            return true;
        }
        public bool MoveAndWait(int TargetPos, uint MovTimeout, bool blnWaitPosOK, byte SlaveAddress, uint Speed, ushort Acceleration)
        {
            double Ti, Tf;
            bool bit = false;

            //Read actual position
            int MotorPos = -999;

            if (!this.ReadCurrentPosition(ref MotorPos, SlaveAddress)) return false;

            if (MotorPos != TargetPos)
            {
                this.MoveActuator(TargetPos, SlaveAddress, _Inposband, Speed, Acceleration);

                if (!blnWaitPosOK) return true;

                Ti = DateTime.Now.TimeOfDay.TotalMilliseconds;
                do
                {
                    Tf = DateTime.Now.TimeOfDay.TotalMilliseconds;
                    if (Tf < Ti) Ti = Tf;
                    if ((Tf - Ti) > MovTimeout) break;
                    Thread.Sleep(200);
                    this.ReadRegisterBit(IAIModbusASCII.PositioningCompletationStatus, ref bit, SlaveAddress);
                }
                while (!bit);
                if (!bit) return false;
            }
            return true;
        }
        #endregion Public Methods
    }
}