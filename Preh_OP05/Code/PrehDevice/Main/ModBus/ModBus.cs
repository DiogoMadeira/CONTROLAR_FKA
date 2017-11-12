using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Preh
{
    public class ModBus
    {

        public static event Action<bool> ConnectionEvent;
        public static event Action<string> Error;

        private byte TransactionID = 0;

        private byte Disconnected = 0;
        private byte Connected = 1;
        private int skTimeout = 250;  //Socket (send / receive timeout)

        private byte NoError = 0;
        private byte SendError = 1;
        private byte ReceiveError = 2;
        private byte FunctionError = 3;

        private string p_wsIPAddress;
        private IPEndPoint p_wsIPEndPoint;
        private Socket p_wsSocket;
        private byte p_wsStatus;
        private int p_wsErrors;
        private StringBuilder p_wsLastError = new StringBuilder("");
        private long p_wsTickCounts;


        public ModBus(string ip)
        {
            p_wsIPAddress = ip;
        }

        public string wsIPAddress
        {
            
            get { return p_wsIPAddress; }
        }

        public Socket wsSocket
        {
            get { return p_wsSocket; }
        }

        public byte wsStatus
        {
            get { return p_wsStatus; }
        }

        public long wsErrors
        {
            get { return p_wsErrors; }
        }

        public StringBuilder wsLastError
        {
            get { return p_wsLastError; }
        }

        public long wsTickCounts
        {
            get { return p_wsTickCounts; }
        }

        public void ConnectToServer()
        {
            CloseSocket();
            p_wsIPEndPoint = new IPEndPoint(IPAddress.Parse(p_wsIPAddress), 502);
            var skt = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                p_wsStatus = Disconnected;
                p_wsSocket = skt;   // save this socket handle

                var onCon = new AsyncCallback(OnConnect);
                skt.BeginConnect(p_wsIPEndPoint, onCon, skt);
            }
            catch (Exception ex)
            {
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                Error?.Invoke(ex.InnerException.ToString());
            }
        }

        private void OnConnect(IAsyncResult ar)
        {
            var sock = (Socket)ar.AsyncState;
            {
                if (sock.Connected)
                {
                    p_wsStatus = Connected;
                    ConnectionEvent?.Invoke(true);
                }
                else
                {
                    sock.Close();
                }
            }
        }

        

        public void CloseSocket()
        {
            if (p_wsSocket != null) { p_wsSocket.Close(); }
            p_wsStatus = Disconnected;
            ConnectionEvent?.Invoke(false);
            p_wsSocket = null;
        }

        public byte ReadDigitalInput(short StartAddress, Int16 Count, byte[] myBuffer)
        {
            //Some bytes are predefined; ModBus function = 2
            byte[] ar = new byte[12];       // TX buffer
            byte[] Buffer = new byte[255];  // RX buffer
            int nBytes = 0;

            if (TransactionID == 255)
            {
                TransactionID = 0;
            }
            else
            {
                TransactionID++;
            }
            ar[0] = 0;  // transaction identifier (returned by the slave)
            ar[1] = TransactionID;  // transaction identifier
            ar[2] = 0;  // Protocoll identifier (always 0)
            ar[3] = 0;  // Protocoll identifier
            ar[4] = 0;  // Length High Byte (0 if the msg is less than 255 bytes)
            ar[5] = 6;  // length Low Byte (# bytes to follow)
            ar[6] = 1;  // Unit identifier (returned by the slave - protocoll number)
            ar[7] = 2;  // ModBus function
            ar[8] = Convert.ToByte(StartAddress / 256); ;  // High Address to read from
            ar[9] = Convert.ToByte(StartAddress % 256); ;  // Low Address to read from
            ar[10] = Convert.ToByte(Count / 256); // Count High
            ar[11] = Convert.ToByte(Count % 256); // Count Low (number of locations to be read)

            myBuffer.Initialize();

            try
            {
                p_wsSocket.ReceiveTimeout = skTimeout;
                p_wsSocket.SendTimeout = skTimeout;

                nBytes = p_wsSocket.Send(ar);  // Send data to the machine
                p_wsTickCounts++;
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "ReadDigitalIn (Send)@ " + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return SendError;
            }
            try
            {
                nBytes = p_wsSocket.Receive(Buffer);
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "ReadDigitalIn (Receive)@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return ReceiveError;
            }
            if (Buffer[1] != ar[1] || Buffer[7] != ar[7])    // we have a ModBus error
            {
                p_wsSocket = null;
                p_wsErrors = p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "ReadDigitalIn (FunctionError))@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                return FunctionError;
            }
            else
            {
                Array.Copy(Buffer, 9, myBuffer, 0, myBuffer.Length); // only starting index 9 it's data
                return NoError;
            }
        }

        public byte ReadDigitalOutput(short StartAddress, Int16 Count, byte[] myBuffer)
        {
            //Some bytes are predefined; ModBus function = 2
            byte[] ar = new byte[12];       // TX buffer
            byte[] Buffer = new byte[255];  // RX buffer
            int nBytes = 0;

            if (TransactionID == 255)
            {
                TransactionID = 0;
            }
            else
            {
                TransactionID++;
            }
            ar[0] = 0;  // transaction identifier (returned by the slave)
            ar[1] = TransactionID;  // transaction identifier
            ar[2] = 0;  // Protocoll identifier (always 0)
            ar[3] = 0;  // Protocoll identifier
            ar[4] = 0;  // Length High Byte (0 if the msg is less than 255 bytes)
            ar[5] = 6;  // length Low Byte (# bytes to follow)
            ar[6] = 1;  // Unit identifier (returned by the slave - protocoll number)
            ar[7] = 1;  // ModBus function
            ar[8] = Convert.ToByte(StartAddress / 256); ;  // High Address to read from
            ar[9] = Convert.ToByte(StartAddress % 256); ;  // Low Address to read from
            ar[10] = Convert.ToByte(Count / 256); // Count High
            ar[11] = Convert.ToByte(Count % 256); // Count Low (number of locations to be read)

            myBuffer.Initialize();

            try
            {
                p_wsSocket.ReceiveTimeout = skTimeout;
                p_wsSocket.SendTimeout = skTimeout;

                nBytes = p_wsSocket.Send(ar);  // Send data to the machine
                p_wsTickCounts++;
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "ReadDigitalOut (Send)@ " + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return SendError;
            }
            try
            {
                nBytes = p_wsSocket.Receive(Buffer);
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "ReadDigitalOut (Receive)@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return ReceiveError;
            }
            if (Buffer[1] != ar[1] || Buffer[7] != ar[7])    // we have a ModBus error
            {
                p_wsSocket = null;
                p_wsErrors = p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "ReadDigitalOut (FunctionError))@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                return FunctionError;
            }
            else
            {
                Array.Copy(Buffer, 9, myBuffer, 0, myBuffer.Length); // only starting index 9 it's data
                return NoError;
            }
        }

        public byte WriteDigitalOutput(short Address, bool bValue)
        {
            //Some bytes are predefined; ModBus function = 5
            byte[] ar = new byte[12];       // TX buffer
            byte[] Buffer = new byte[255];  // RX buffer
            int nBytes = 0;

            byte bitValue;

            if (bValue == true)
                bitValue = 0xFF;
            else
                bitValue = 0x0;
            if (TransactionID == 255)
            {
                TransactionID = 0;
            }
            else
            {
                TransactionID++;
            }
            ar[0] = 0;  // transaction identifier (returned by the slave)
            ar[1] = 5;  // transaction identifier
            ar[2] = 0;  // Protocoll identifier (always 0)
            ar[3] = 0;  // Protocoll identifier
            ar[4] = 0;  // Length High Byte (0 if the msg is less than 255 bytes)
            ar[5] = 6;  // length Low Byte (# bytes to follow)
            ar[6] = 1;  // Unit identifier (returned by the slave - protocoll number)
            ar[7] = 5;  // ModBus function
            ar[8] = Convert.ToByte(Address / 256); ;  // High Address to read from
            ar[9] = Convert.ToByte(Address % 256); ;  // Low Address to read from
            ar[10] = bitValue;
            ar[11] = 0; // allways

            try
            {
                p_wsSocket.ReceiveTimeout = skTimeout;
                p_wsSocket.SendTimeout = skTimeout;

                nBytes = p_wsSocket.Send(ar);  // Send data to the machine
                p_wsTickCounts++;
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "WriteDigital (Send)@ " + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return SendError;
            }
            try
            {
                nBytes = p_wsSocket.Receive(Buffer);
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "WriteDigital (Receive)@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return ReceiveError;
            }
            if (Buffer[1] != ar[1] || Buffer[7] != ar[7])    // we have a ModBus error
            {
                p_wsSocket = null;
                p_wsErrors = p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "WriteDigital (FunctionError))@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                return FunctionError;
            }
            else
            {
                return NoError;
            }
        }

        public byte ReadAnalogInput(short StartAddress, ref int Analog)
        {
            //Some bytes are predefined; ModBus function = 4
            byte[] ar = new byte[12];       // TX buffer
            byte[] Buffer = new byte[255];  // RX buffer
            int nBytes = 0;

            if (TransactionID == 255)
            {
                TransactionID = 0;
            }
            else
            {
                TransactionID++;
            }
            ar[0] = 0;  // transaction identifier (returned by the slave)
            ar[1] = TransactionID;  // transaction identifier
            ar[2] = 0;  // Protocoll identifier (always 0)
            ar[3] = 0;  // Protocoll identifier
            ar[4] = 0;  // Length High Byte (0 if the msg is less than 255 bytes)
            ar[5] = 6;  // length Low Byte (# bytes to follow)
            ar[6] = 1;  // Unit identifier (returned by the slave - protocoll number)
            ar[7] = 4;  // ModBus function
            ar[8] = Convert.ToByte(StartAddress / 256); ;  // High Address to read from
            ar[9] = Convert.ToByte(StartAddress % 256); ;  // Low Address to read from
            ar[10] = 0; // always 0
            ar[11] = 1; // always 1 (1 analog channel)

            try
            {
                p_wsSocket.ReceiveTimeout = skTimeout;
                p_wsSocket.SendTimeout = skTimeout;

                nBytes = p_wsSocket.Send(ar);  // Send data to the machine
                p_wsTickCounts++;
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "ReadDigital (Send)@ " + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return SendError;
            }
            try
            {
                nBytes = p_wsSocket.Receive(Buffer);
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "ReadDigital (Receive)@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return ReceiveError;
            }
            if (Buffer[1] != ar[1] || Buffer[7] != ar[7])    // we have a ModBus error
            {
                p_wsSocket = null;
                p_wsErrors = p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "ReadDigital (FunctionError))@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                return FunctionError;
            }
            else
            {
                Analog = Convert.ToByte(Buffer[9]) * 256 + Convert.ToByte(Buffer[10]);
                return NoError;
            }
        }

        public byte ReadAnalogOutput(short StartAddress, ref int Analog)
        {
            //Some bytes are predefined
            byte[] ar = new byte[12];
            byte[] Buffer = new byte[255];
            Int32[] BufferInt = new Int32[255];
            int nBytes = 0;

            ar[0] = 0;  // transaction identifier (returned by the slave)
            ar[1] = 0;  // transaction identifier
            ar[2] = 0;  // Protocoll identifier (always 0)
            ar[3] = 0;  // Protocoll identifier
            ar[4] = 0;  // Length High Byte (0 if the msg is less than 255 bytes)
            ar[5] = 6;  // length Low Byte (# bytes to follow)
            ar[6] = 4;  // Unit identifier (returned by the slave - protocoll number)
            ar[7] = 3;  // ModBus function (3)
            ar[8] = Convert.ToByte(StartAddress / 256);//WSStartHighAddress;  // High Address to read from
            ar[9] = Convert.ToByte(StartAddress % 256);//WSStartLowAddress;  // Low Address to read from
            ar[10] = 0; // Count High (make it 0)
            ar[11] = 1;// Convert.ToByte(Count % 256);//WSnLocations; // Count Low (number of locations to be read)

            try
            {
                p_wsSocket.ReceiveTimeout = skTimeout;
                p_wsSocket.SendTimeout = skTimeout;
                //rx.Initialize();

                nBytes = p_wsSocket.Send(ar);  // Send data to the machine
                p_wsTickCounts++;
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;

                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "ReadHoldingRegister (Send)@ " + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return SendError;
            }
            try
            {
                nBytes = p_wsSocket.Receive(Buffer);
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "ReadHoldingRegister (Receive)@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return ReceiveError;
            }
            if (Buffer[7] != ar[7])    // we have a ModBus error = rxBuffer[8]
            {
                p_wsSocket = null;
                p_wsErrors = p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "ReadHoldingRegister (FunctionError))@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                return FunctionError;
            }
            else
            {
                Analog = Convert.ToByte(Buffer[9]) * 256 + Convert.ToByte(Buffer[10]);
                return NoError;
            }
        }

        public byte WriteAnalogOutput(int Address, int Value)
        {
            //Some bytes are predefined
            byte[] ar = new byte[12];
            byte[] rxBuffer = new byte[255];
            int nBytes = 0;

            Value = Value & 65535;

            ar[0] = 0;  // transaction identifier (returned by the slave)
            ar[1] = 0;  // transaction identifier
            ar[2] = 0;  // Protocoll identifier (always 0)
            ar[3] = 0;  // Protocoll identifier
            ar[4] = 0;  // Length High Byte (0 if the msg is less than 255 bytes)
            ar[5] = 6;  // length Low Byte (# bytes to follow)
            ar[6] = 1;  // Unit identifier (returned by the slave - protocoll number)
            ar[7] = 6;  // ModBus function (6)
            ar[8] = Convert.ToByte(Address / 256);  // High Address to write to
            ar[9] = Convert.ToByte(Address % 256);  // Low Address to write to
            ar[10] = Convert.ToByte(Value / 256);  //  DataHigh
            ar[11] = Convert.ToByte(Value % 256);  //  DataLow

            try
            {
                p_wsSocket.ReceiveTimeout = skTimeout;
                p_wsSocket.SendTimeout = skTimeout;

                nBytes = p_wsSocket.Send(ar);  // Send data to the machine
                p_wsTickCounts++;
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null)
                    p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "WriteSingleRegister (Send))@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return SendError;
            }
            try
            {
                nBytes = p_wsSocket.Receive(rxBuffer);
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null)
                    p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "WriteSingleRegister (Receive))@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return ReceiveError;
            }
            if (rxBuffer[7] != ar[7])    // we have a ModBus error = rxBuffer[8]
            {
                if (p_wsSocket != null)
                    p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "WriteSingleRegister (FunctionError))@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                return FunctionError;
            }
            else
            {
                return NoError;
            }
        }

        public byte WriteAnalogOutput(short StartAddress, double AnalogValue)
        {
            //Some bytes are predefined; ModBus function = 5
            byte[] ar = new byte[12];       // TX buffer
            byte[] Buffer = new byte[255];  // RX buffer
            int nBytes = 0;

            if (TransactionID == 255)
            {
                TransactionID = 0;
            }
            else
            {
                TransactionID++;
            }

            ar[0] = 0;  // transaction identifier (returned by the slave)
            ar[1] = 0;  // transaction identifier
            ar[2] = 0;  // Protocoll identifier (always 0)
            ar[3] = 0;  // Protocoll identifier
            ar[4] = 0;  // Length High Byte (0 if the msg is less than 255 bytes)
            ar[5] = 6;  // length Low Byte (# bytes to follow)
            ar[6] = 1;  // Unit identifier (returned by the slave - protocoll number)
            ar[7] = 6;  // ModBus function
            ar[8] = Convert.ToByte((2048 + StartAddress) / 256);   // High Address to read from
            ar[9] = Convert.ToByte((2048 + StartAddress) % 256);  // Low Address to read from
            ar[10] = Convert.ToByte(AnalogValue / 256);
            //ar[11] = Convert.ToByte(AnalogValue % 256); // allways
            ar[11] = 0;

            try
            {
                p_wsSocket.ReceiveTimeout = skTimeout;
                p_wsSocket.SendTimeout = skTimeout;

                nBytes = p_wsSocket.Send(ar);  // Send data to the machine
                p_wsTickCounts++;
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "WriteAnalogOutput (Send)@ " + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return SendError;
            }
            try
            {
                nBytes = p_wsSocket.Receive(Buffer);
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                p_wsSocket = null;
                p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "WriteAnalogOutput (Receive)@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                Error?.Invoke(ex.InnerException.ToString());
                return ReceiveError;
            }
            if (Buffer[1] != ar[1] || Buffer[7] != ar[7])    // we have a ModBus error
            {
                p_wsSocket = null;
                p_wsErrors = p_wsErrors++;
                if (p_wsLastError.Length > 0)
                    p_wsLastError.Remove(0, p_wsLastError.Length - 1);
                p_wsLastError.Insert(0, "WriteAnalogOutput (FunctionError))@" + DateTime.Now.ToString("G"));
                p_wsErrors = p_wsErrors + 1;
                return FunctionError;
            }
            else
            {
                return NoError;
            }
        }

        public byte WriteMultipleDigitalOutputs(short StartAddress, Int16 Count, byte[] myBuffer)
        {
            byte numBytesToWrite = 0;
            byte[] Buffer = new byte[255];  // RX buffer
            int nBytes = 0;
            byte arrIndex = 0;

            if ((Count % 8) > 0)
                numBytesToWrite = Convert.ToByte((Count / 8) + 1);
            else
                numBytesToWrite = Convert.ToByte(Count / 8);

            byte[] ar = new byte[13 + numBytesToWrite];       // TX buffer

            if (TransactionID == 255)
            {
                TransactionID = 0;
            }
            else
            {
                TransactionID++;
            }
            //myBuffer contains the array with the outputs status; Count = number of bits

            ar[arrIndex++] = 0;  // transaction identifier (returned by the slave)
            ar[arrIndex++] = TransactionID;  // transaction identifier
            ar[arrIndex++] = 0;  // Protocoll identifier (always 0)
            ar[arrIndex++] = 0;  // Protocoll identifier
            ar[arrIndex++] = 0;  // Length High Byte (0 if the msg is less than 255 bytes)
            ar[arrIndex++] = Convert.ToByte(7 + numBytesToWrite);  // length Low Byte (# bytes to follow)
            ar[arrIndex++] = 1;  // Unit identifier (returned by the slave - protocoll number)
            ar[arrIndex++] = 15;  // ModBus function
            ar[arrIndex++] = Convert.ToByte(StartAddress / 256); ;  // High Address to read from
            ar[arrIndex++] = Convert.ToByte(StartAddress % 256); ;  // Low Address to read from
            ar[arrIndex++] = Convert.ToByte(Count / 256); // Count High
            ar[arrIndex++] = Convert.ToByte(Count % 256); // Count Low
            ar[arrIndex++] = numBytesToWrite;

            ar[arrIndex++] = myBuffer[0];
            ar[arrIndex++] = myBuffer[1];
            ar[arrIndex++] = myBuffer[2];
            ar[arrIndex++] = myBuffer[3];
            ar[arrIndex++] = myBuffer[4];
            ar[arrIndex++] = myBuffer[5];
            ar[arrIndex++] = myBuffer[6];
            ar[arrIndex++] = myBuffer[7];
            ar[arrIndex++] = myBuffer[8];
            ar[arrIndex++] = myBuffer[9];

            myBuffer.Initialize();

            try
            {
                p_wsSocket.ReceiveTimeout = skTimeout;
                p_wsSocket.SendTimeout = skTimeout;

                nBytes = p_wsSocket.Send(ar);  // Send data to the machine
                p_wsTickCounts++;
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                SetError("WriteMultipleOutputs (Send)@ ");
                Error?.Invoke(ex.InnerException.ToString());
                return SendError;
            }
            try
            {
                nBytes = p_wsSocket.Receive(Buffer);
            }
            catch (Exception ex)
            {
                if (p_wsSocket != null) p_wsSocket.Close();
                p_wsStatus = Disconnected;
                SetError("WriteMultipleOutputs (Receive)@");
                Error?.Invoke(ex.InnerException.ToString());
                return ReceiveError;
            }
            if (Buffer[1] != ar[1] || Buffer[7] != ar[7])    // we have a ModBus error
            {
                SetError("WriteMultipleOutputs (FunctionError))@");
                return FunctionError;
            }
            else
            {
                return NoError;
            }
        }

        private void SetError(string str)
        {
            p_wsSocket = null;
            p_wsErrors++;
            if (p_wsLastError.Length > 0)
                p_wsLastError.Remove(0, p_wsLastError.Length - 1);
            p_wsLastError.Insert(0, str + DateTime.Now.ToString("G"));
            p_wsErrors = p_wsErrors + 1;
        }
    }
}