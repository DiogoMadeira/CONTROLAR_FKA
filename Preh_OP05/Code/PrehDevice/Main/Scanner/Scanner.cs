using System;
using System.Net;
using System.Net.Sockets;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Preh {
    public class Scanner {
        #region Instances
        static System.Windows.Forms.Timer tmrTimout = new System.Windows.Forms.Timer();
        public delegate void newScannerDataReceived(string scannerName, string ScannerData);
        public event newScannerDataReceived newDataReceived;
        #endregion

        #region Properties
        private IPAddress _IpAddr;
        public IPAddress ipAddr { get { return _IpAddr; } }

        private int _TcpPort;
        public int TcpPort { get { return _TcpPort; } }

        private string _ComPort;
        public string ComPort { get { return _ComPort; } }

        private int _BaudRate;
        public int BaudRate { get { return _BaudRate; } }

        private string _ErrorMsg = string.Empty;
        public string ErrorMsg { get { return _ErrorMsg; } }

        private bool _Connected = false;
        public bool Connected { get { return _Connected; } }

        private bool _ReadComplete = false;
        public bool ReadComplete { get { return _ReadComplete; } }

        private string _DataReceived = string.Empty;
        public string DataReceived { get { return _DataReceived; } }

        private int _TimeoutValue = 3000;
        public int TimeoutValue { get { return _TimeoutValue; } set { _TimeoutValue = value; } }

        private bool _Timeout = false;
        public bool Timeout { get { return _Timeout; } }

        private int _Type;
        public int Type { get { return _Type; } }

        private string _Name;
        public string Name { get { return _Name; } }
        public Stopwatch InternalStopWatch { get; set; }
        public bool TurnScannerOff { get; set; }
        public static string NoRead { get { return "NOREAD"; } }
        public string CompareString { get; set; }
        private Stopwatch _cmdWatcher { get; set; }
        public bool IsReading { get; set; }
        #endregion

        #region Constructor
        public Scanner(IPAddress ipAddr, int tcpPort, SCANNER_TYPE st, string name) {
            
            TurnScannerOff = false;
            IsReading = false;
            ScannerType = st;
            _IpAddr = ipAddr;
            _TcpPort = tcpPort;
            _Name = name;
            _Type = (int)st;
            InternalStopWatch = new Stopwatch();
            _cmdWatcher = new Stopwatch();


        }
        public Scanner(string comPort, int baudRate, SCANNER_TYPE st, string name) {
            ScannerType = st;
            _ComPort = comPort;
            _BaudRate = baudRate;
            _Name = name;
            _Type = (int)st;
        }
        #endregion

        #region Variables
        /// <summary>
        /// Scanners: 
        ///     Cognex_DataMan_60: 1
        ///     HoneyWell_Xenon_1900:  2
        /// </summary>
        public enum SCANNER_TYPE {
            Cognex_DataMan_60 = 1,
            HoneyWell_Xenon_1900 = 2,
        }
        private SCANNER_TYPE ScannerType;

        private SerialPort SerialPort;
        private Socket EthSocket;

        static StringBuilder sb = new StringBuilder();

        private byte[] HoneyWellReadActivate = new byte[3] { 0x16, 0x54, 0x0D }; // SYN T CR
        private byte[] HoneyWellReadDeactivate = new byte[3] { 0x16, 0x55, 0x0D }; // SYN U CR

        private const byte STX = 0x02;
        private const byte ETX = 0x03;
        #endregion

        #region Private Methods
        //Serial Port
        private void ReceiveSerialPortEvent(object sender, SerialDataReceivedEventArgs e) {
            try {
                string msgRcv = "";
                string aux = "\r\n";  // \r = CR;  \n = LF
                //Stop timeout timer
                tmrTimout.Stop();

                msgRcv = SerialPort.ReadExisting();
                sb.Append(msgRcv);
                msgRcv = sb.ToString();

                if ((msgRcv.Substring(msgRcv.Length - 2, 2) == "\r\n".ToString()) || (msgRcv.Substring(msgRcv.Length - 1, 1) == "\r".ToString())) {
                    string[] array = msgRcv.Split('\r'); //devido ao buffer do leitor que pode enviar varios codigos seguidos          
                    msgRcv = array[0].Replace(aux, "");
                    sb.Remove(0, sb.Length);

                    newDataReceived?.Invoke(Name, msgRcv);
                    _DataReceived = msgRcv;
                    _ReadComplete = true;
                }
            }
            catch (Exception ex) {
                _ErrorMsg = "Error while receive from Scanner " + ex.Message;
            }
        }

        //TCP
        private bool SendCmd(string cmd) {

                
            return SendToSocket(cmd);
            
        }

        private bool SendToSocket(string cmd)
        {
            int nBytes = 0;

            cmd = "||>" + cmd + '\r' + '\n';
            byte[] ar = Encoding.ASCII.GetBytes(cmd);
            try
            {
                nBytes = EthSocket.Send(ar);
                if (nBytes != cmd.Length)
                    throw new Exception();
                else return true;
            }
            catch (Exception ex)
            {
                _ErrorMsg = "Error sending read to scanner " + ex.Message;
                return false;
            }
        }

        private void Receive(Socket client) {
            try {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                                    new AsyncCallback(ReceiveEthCallback), state);
            }
            catch (Exception ex) {
                _ErrorMsg = "Error receiving data from scanner " + ex.Message;
            }
        }
        private void ReceiveEthCallback(IAsyncResult ar) {
            try {
                string msgRcv = "";
                //Stop timeout timer
                tmrTimout.Stop();

                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                var state = (StateObject)ar.AsyncState;
                var client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0) {
                    // There might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    if (state.sb.Length > 1) {
                        msgRcv = state.sb.ToString();

                        if (msgRcv[0] == STX && msgRcv[msgRcv.Length - 1] == ETX) {
                            _DataReceived = msgRcv.Substring(1, msgRcv.Length - 2);
                            newDataReceived?.Invoke(Name, _DataReceived);
                            _ReadComplete = true;
                        } else {
                            _ErrorMsg = "STX / ETX missing , @ReadResponse , " + _DataReceived;
                        }
                    }
                }
            }
            catch (Exception ex) {
                _ErrorMsg = "Error receiving data from scanner " + ex.Message;
            }
        }

        //Timeout Handling
        private void ProcessTimeoutEvent(Object myObject, EventArgs myEventArgs) {
            tmrTimout.Stop();
            _Timeout = true;

            if (ScannerType == SCANNER_TYPE.HoneyWell_Xenon_1900) SerialPort.Write(HoneyWellReadDeactivate, 0, HoneyWellReadDeactivate.Length);
        }
        #endregion

        #region Public Methods
        public bool Connect() {
            try {
                if (ScannerType == SCANNER_TYPE.Cognex_DataMan_60)
                {
                    var endPoint = new IPEndPoint(_IpAddr, _TcpPort);
                    EthSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        ReceiveTimeout = _TimeoutValue,
                        SendTimeout = _TimeoutValue
                    };
                    EthSocket.Connect(endPoint);
                }
                else if (ScannerType == SCANNER_TYPE.HoneyWell_Xenon_1900)
                {
                    SerialPort = new SerialPort(_ComPort, _BaudRate, Parity.None, 8, StopBits.One)
                    {
                        Handshake = Handshake.None,
                        ReceivedBytesThreshold = 1
                    };
                    SerialPort.Open();
                    SerialPort.DataReceived += new SerialDataReceivedEventHandler(ReceiveSerialPortEvent);
                }
                else return false;

                _Connected = true;
                return true;
            }
            catch (Exception ex) {
                Disconnect();
                _ErrorMsg = "Error connecting to Scanner " + ex.Message;
                return false;
            }
        }
        public void Disconnect() {
            try {
                if (ScannerType == SCANNER_TYPE.Cognex_DataMan_60) {
                    if (EthSocket != null) EthSocket.Close();
                    _Connected = false;
                    EthSocket = null;
                } else if (ScannerType == SCANNER_TYPE.HoneyWell_Xenon_1900 && SerialPort.IsOpen) SerialPort.Close();
                _Connected = false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Trigger scanner for reading
        /// Result can be obtained at DataReceived property or newDataReceived Event
        /// </summary>
        /// <returns></returns>
        public bool Read() {
            tmrTimout.Stop();
            _Timeout = false;
            _ReadComplete = false;
            _DataReceived = "";
            _ErrorMsg = "";

            try {
                if (ScannerType == SCANNER_TYPE.Cognex_DataMan_60) {
                    if (!SendCmd("Trigger ON")) return false;
                    Receive(EthSocket);
                } else if (ScannerType == SCANNER_TYPE.HoneyWell_Xenon_1900) {
                    SerialPort.Write(HoneyWellReadActivate, 0, HoneyWellReadActivate.Length);
                } else return false;

                tmrTimout.Tick += new EventHandler(ProcessTimeoutEvent);
                tmrTimout.Interval = _TimeoutValue;
                tmrTimout.Start();
                return true;
            }
            catch (Exception ex) {
                _ErrorMsg = "Error reading from scanner " + ex.Message;
                return false;
            }
        }
        #endregion
    }
    public class StateObject {
        // State object for receiving data from remote device.
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }
}