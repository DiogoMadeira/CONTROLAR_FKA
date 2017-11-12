using NurApiDotNet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Preh
{
    public class RFID
    {
        #region Instances

        public delegate void newRFIDDataReceived(string Name, string[] Data);
        public event newRFIDDataReceived newDataReceived;

        #endregion

        #region Properties

        private string _Error = "";
        public string Error { get { return _Error; } }

        private bool _Connected = false;
        public bool Connected { get { return _Connected; } }

        private invInfo _InventoryInfo;
        public invInfo InventoryInfo { get { return _InventoryInfo; } set { _InventoryInfo = value; } }

        private NurApi.ReaderInfo _ReaderInfo;
        public NurApi.ReaderInfo readerinfo { get { return _ReaderInfo; } }

        private int _TimeoutValue = 3000;
        public int TimeoutValue { get { return _TimeoutValue; } set { _TimeoutValue = value; } }

        private bool _Timeout = false;
        public bool Timeout { get { return _Timeout; } }

        private string _ReaderName = string.Empty;
        public string ReaderName { get { return _ReaderName; } }

        private string _ReaderSerial = string.Empty;
        public string ReaderSerial { get { return _ReaderSerial; } }

        private string _ReaderAltSerial = string.Empty;
        public string ReaderAltSerial { get { return _ReaderAltSerial; } }

        private string _ReaderComPort = string.Empty;
        public string ReaderComPort { get { return _ReaderComPort; } }

        private int _Level = 10;
        public int Level { get { return _Level; } set { _Level = value; } }


        public enum RFID_TYPE
        {
            Nordic_Id_Stix = 1,
        }
        private RFID_TYPE RFIDType;

        #endregion

        #region Constructor
        public RFID(string comPort, string serial, RFID_TYPE rt, string name, int level)
        {
            RFIDType = rt;
            _ReaderComPort = comPort;
            _ReaderSerial = serial;
            _ReaderName = name;
            _Level = level;

        }

        #endregion

        #region Variables

        private NurApi hNur = new NurApi();

        private const byte EPCbank = 1;
        private const byte TIDbank = 2;
        private const byte USERbank = 3;

        private const byte TIDbyteCount = 6;
        private const byte TIDEXbyteCount = 12;
        private const byte EPCbyteCount = 16;
        private const byte USERbyteCount = 4;

        #endregion

        #region Structs
        public struct invInfo
        {
            public int numTagsFound;
            public int numTagsMem;
            public int collisions;
            public int Q;
            public int roundsDone;
            public string[] TIDEX;
            public sbyte[] RSSI;

            //epc data
            public long[] EPCuid;

            public long[] EPCref;
            public UInt16[] EPCdate;
            public UInt16[] EPCcontrol;

            public UInt32[] USERdata;
        }

        #endregion

        #region Private Methods

        private string byteArryayToHexString(byte[] bArray)
        {
            var hex = new StringBuilder(bArray.Length * 2);
            foreach (var b in bArray)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private long convertByteArrayToLong(byte[] b)
        {
            Array.Resize(ref b, 8);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(b);
            return BitConverter.ToInt64(b, 0);
        }

        private byte[] convertLongToByteArray(long lng, byte packLen)
        {
            byte[] b;

            if (BitConverter.IsLittleEndian)
            {
                b = BitConverter.GetBytes(lng);
                Array.Resize(ref b, packLen);
            }
            else
            {
                b = BitConverter.GetBytes(lng);
                Array.Reverse(b);
                Array.Resize(ref b, packLen);
            }
            return b;
        }

        #endregion

        #region Public Methods

        public bool Connect()
        {
            try
            {
                var deviceFound = false;
                var comPorts = NurApi.EnumerateComPorts();

                // Try to find the wanted device (depending on serialNr)
                foreach (NurApi.ComPort comPort in comPorts)
                {
                    if (!comPort.friendlyName.StartsWith("NUR Module")) continue;

                    try
                    {
                        hNur.ConnectSerialPort(comPort.port);
                        var info = hNur.GetReaderInfo();
                        if (_ReaderSerial.Equals(info.altSerial))
                        {
                            deviceFound = true;
                            break;
                        }
                        else hNur.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        throw ex.InnerException;
                    }

                    Thread.Sleep(100);
                }
                if (!deviceFound)
                {
                    _Error = "No device found with the defined serial";
                    return false;
                }

                if (hNur.IsConnected())
                {
                    _ReaderInfo = hNur.GetReaderInfo();
                    NurApi.ReaderInfo info = hNur.GetReaderInfo();
                    _ReaderAltSerial = info.altSerial;
                    _Connected = true;
                    return true;
                }
                else throw new Exception("RFID timeout");
            }
            catch (Exception ex)
            {
                _Error = "Error connecting to RFID " + ex.Message;
                return false;
            }
        }
        public void Disconnect()
        {
            try
            {
                hNur.SetUsbAutoConnect(false);
                hNur.Disconnect();
                _Connected = false;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// level [0 ... 19]; 0==27dBm, 19==8dBm ;
        /// clearTagsInfo-> clear tag information ;
        /// numTries-> number of tries to get at least one tag ;
        /// </summary>
        public bool ReadTags(bool clearTagsInfo, int numTries, int level)
        {
            int n = 0;
            try
            {
                hNur.TxLevel = level;
                if (clearTagsInfo) hNur.ClearTags();
                NurApi.InventoryResponse response; //Information about inventory store here
                do
                {
                    response = hNur.SimpleInventory();         //Make Inventory..
                    _InventoryInfo.numTagsFound = response.numTagsFound;
                    _InventoryInfo.numTagsMem = response.numTagsMem;
                    _InventoryInfo.collisions = response.collisions;
                    _InventoryInfo.Q = response.Q;
                    _InventoryInfo.roundsDone = response.roundsDone;
                } while (--numTries > 0 && response.numTagsFound == 0);
                if (numTries < 0)
                    throw new Exception("NOREAD");

                _InventoryInfo.TIDEX = new string[response.numTagsFound];
                _InventoryInfo.RSSI = new sbyte[response.numTagsFound];
                _InventoryInfo.EPCuid = new long[response.numTagsFound];
                _InventoryInfo.EPCref = new long[response.numTagsFound];
                _InventoryInfo.EPCdate = new UInt16[response.numTagsFound];
                _InventoryInfo.EPCcontrol = new UInt16[response.numTagsFound];
                _InventoryInfo.USERdata = new UInt32[response.numTagsFound];

                var inv = hNur.FetchTags(true);
                foreach (NurApi.Tag tag in inv)
                {
                    _InventoryInfo.RSSI[n] = tag.rssi;

                    byte[] TIDEXbytes = tag.ReadTag(0, false, TIDbank, 0, TIDEXbyteCount);
                    _InventoryInfo.TIDEX[n] = byteArryayToHexString(TIDEXbytes);

                    byte[] EPCbytes = tag.ReadTag(0, false, EPCbank, 2, EPCbyteCount);

                    byte[] eUID = new byte[6];
                    Array.Copy(EPCbytes, 0, eUID, 0, 6);
                    _InventoryInfo.EPCuid[n] = convertByteArrayToLong(eUID);

                    byte[] eRef = new byte[6];
                    Array.Copy(EPCbytes, 6, eRef, 0, 6);
                    _InventoryInfo.EPCref[n] = convertByteArrayToLong(eRef);

                    byte[] eDate = new byte[2];
                    Array.Copy(EPCbytes, 12, eDate, 0, 2);
                    _InventoryInfo.EPCdate[n] = (UInt16)convertByteArrayToLong(eDate);

                    byte[] eControl = new byte[2];
                    Array.Copy(EPCbytes, 14, eControl, 0, 2);
                    _InventoryInfo.EPCcontrol[n] = (UInt16)convertByteArrayToLong(eControl);

                    byte[] USERbytes = tag.ReadTag(0, false, USERbank, 0, USERbyteCount);
                    _InventoryInfo.USERdata[n] = (UInt32)convertByteArrayToLong(USERbytes);
                    n++;
                }

                //TODO:
                newDataReceived?.Invoke(ReaderName, _InventoryInfo.TIDEX);

                return true;
            }
            catch (Exception ex)
            {
                _Error = "Error reading RFID Tag " + ex.Message;
                return false;
            }
        }
        public bool ReadTags()
        {
            int n = 0;
            int numTries = 3;
            try
            {
                hNur.TxLevel = _Level;
                hNur.ClearTags();
                NurApi.InventoryResponse response; //Information about inventory store here
                do
                {
                    response = hNur.SimpleInventory();         //Make Inventory..
                    _InventoryInfo.numTagsFound = response.numTagsFound;
                    _InventoryInfo.numTagsMem = response.numTagsMem;
                    _InventoryInfo.collisions = response.collisions;
                    _InventoryInfo.Q = response.Q;
                    _InventoryInfo.roundsDone = response.roundsDone;
                } while (--numTries > 0 && response.numTagsFound == 0);
                if (numTries < 0)
                    throw new Exception("NOREAD");


                _InventoryInfo.TIDEX = new string[response.numTagsFound];
                _InventoryInfo.RSSI = new sbyte[response.numTagsFound];
                _InventoryInfo.EPCuid = new long[response.numTagsFound];
                _InventoryInfo.EPCref = new long[response.numTagsFound];
                _InventoryInfo.EPCdate = new UInt16[response.numTagsFound];
                _InventoryInfo.EPCcontrol = new UInt16[response.numTagsFound];
                _InventoryInfo.USERdata = new UInt32[response.numTagsFound];

                NurApi.TagStorage inv = hNur.FetchTags(true);
                foreach (NurApi.Tag tag in inv)
                {
                    _InventoryInfo.RSSI[n] = tag.rssi;

                    byte[] TIDEXbytes = tag.ReadTag(0, false, TIDbank, 0, TIDEXbyteCount);
                    _InventoryInfo.TIDEX[n] = byteArryayToHexString(TIDEXbytes);

                    byte[] EPCbytes = tag.ReadTag(0, false, EPCbank, 2, EPCbyteCount);

                    byte[] eUID = new byte[6];
                    Array.Copy(EPCbytes, 0, eUID, 0, 6);
                    _InventoryInfo.EPCuid[n] = convertByteArrayToLong(eUID);

                    byte[] eRef = new byte[6];
                    Array.Copy(EPCbytes, 6, eRef, 0, 6);
                    _InventoryInfo.EPCref[n] = convertByteArrayToLong(eRef);

                    byte[] eDate = new byte[2];
                    Array.Copy(EPCbytes, 12, eDate, 0, 2);
                    _InventoryInfo.EPCdate[n] = (UInt16)convertByteArrayToLong(eDate);

                    byte[] eControl = new byte[2];
                    Array.Copy(EPCbytes, 14, eControl, 0, 2);
                    _InventoryInfo.EPCcontrol[n] = (UInt16)convertByteArrayToLong(eControl);

                    byte[] USERbytes = tag.ReadTag(0, false, USERbank, 0, USERbyteCount);
                    _InventoryInfo.USERdata[n] = (UInt32)convertByteArrayToLong(USERbytes);
                    n++;
                }

                //TODO:
                if (response.numTagsFound == 0)
                    newDataReceived(ReaderName, null);
                else
                    newDataReceived(ReaderName, _InventoryInfo.TIDEX);

                return true;
            }
            catch (Exception ex)
            {
                _Error = "Error reading RFID Tag " + ex.Message;
                return false;
            }
        }
        public bool WriteTagByUID(long uidTarget, long NewUID, bool checkOnlyOne, bool writeUserData)
        {
            long thisTagUID = 0;
            int num = 0;
            int invIndex = 0;

            try
            {
                hNur.ClearTags();
                NurApi.InventoryResponse response;          //Information about inventory store here
                response = hNur.SimpleInventory();          //Make Inventory..
                NurApi.TagStorage inv = hNur.FetchTags(true);
                NurApi.Tag tag = null;
                if (checkOnlyOne)
                    if (response.numTagsFound > 1) throw new Exception("more than one tag found in the antena");

                for (num = 0; num < inv.Count; num++)
                {
                    tag = inv[num];
                    byte[] EPCbytes = tag.ReadTag(0, false, EPCbank, 2, EPCbyteCount);
                    byte[] eUID = new byte[6];
                    Array.Copy(EPCbytes, 0, eUID, 0, 6);
                    thisTagUID = convertByteArrayToLong(eUID);
                    if (thisTagUID == uidTarget) break;   // found in the antena
                }

                if (num >= inv.Count) throw new Exception("uidTarget not found in the antena");

                //Verifica se o inventário já consta o novo UID:
                for (invIndex = 0; invIndex < _InventoryInfo.numTagsFound; invIndex++)
                {
                    if (_InventoryInfo.EPCuid[invIndex] == uidTarget || _InventoryInfo.EPCuid[invIndex] == NewUID) break;
                }
                if (invIndex >= _InventoryInfo.numTagsFound) throw new Exception("uidTarget not found in 'inventoryInfo'");

                // start writing
                byte[] EPC = new byte[EPCbyteCount];
                Array.Copy(convertLongToByteArray(_InventoryInfo.EPCuid[invIndex], 6), 0, EPC, 0, 6);
                Array.Copy(convertLongToByteArray(_InventoryInfo.EPCref[invIndex], 6), 0, EPC, 6, 6);
                Array.Copy(convertLongToByteArray(_InventoryInfo.EPCdate[invIndex], 2), 0, EPC, 12, 2);
                Array.Copy(convertLongToByteArray(_InventoryInfo.EPCcontrol[invIndex], 2), 0, EPC, 14, 2);
                tag.WriteTag(0, false, EPCbank, 2, EPC);

                if (writeUserData)
                {
                    hNur.ClearTags();
                    response = hNur.SimpleInventory();          //Make Inventory..
                    inv = hNur.FetchTags(true);
                    tag = null;
                    if (checkOnlyOne)
                        if (response.numTagsFound > 1) throw new Exception("more than one tag found in the antena");

                    for (num = 0; num < inv.Count; num++)
                    {
                        tag = inv[num];
                        byte[] EPCbytes = tag.ReadTag(0, false, EPCbank, 2, EPCbyteCount);
                        byte[] eUID = new byte[6];
                        Array.Copy(EPCbytes, 0, eUID, 0, 6);
                        thisTagUID = convertByteArrayToLong(eUID);
                        if (thisTagUID == uidTarget) break;   // found in the antena
                    }

                    if (num >= inv.Count) throw new Exception("uidTarget not found in the antena");

                    for (invIndex = 0; invIndex < _InventoryInfo.numTagsFound; invIndex++)
                    {
                        if (_InventoryInfo.EPCuid[invIndex] == uidTarget) break;  // found in the struct
                    }
                    if (invIndex >= _InventoryInfo.numTagsFound) throw new Exception("uidTarget not found in 'inventoryInfo'");

                    byte[] USERDATA = new byte[USERbyteCount];
                    Array.Copy(convertLongToByteArray(_InventoryInfo.USERdata[invIndex], 4), 0, USERDATA, 0, 4);
                    tag.WriteTag(0, false, USERbank, 0, USERDATA);
                }

                return true;
            }
            catch (Exception ex)
            {
                _Error = "Error writting in RFID tag " + ex.Message;
                return false;
            }
        }

        public bool InitTag(long uidTarget)
        {
            try
            {
                hNur.ClearTags();
                NurApi.InventoryResponse response;          //Information about inventory store here
                response = hNur.SimpleInventory();          //Make Inventory..
                NurApi.TagStorage inv = hNur.FetchTags(true);
                NurApi.Tag tag = null;

                if (response.numTagsFound > 1) throw new Exception("more than one tag found in the antena writing EPC");

                tag = inv[0];
                byte[] uid = convertLongToByteArray(uidTarget, 6);
                byte[] EPC = new byte[EPCbyteCount];

                Array.Copy(uid, 0, EPC, 0, 6);
                tag.WriteTag(0, false, EPCbank, 2, EPC);

                hNur.ClearTags();
                response = hNur.SimpleInventory();          //Make Inventory..
                inv = hNur.FetchTags(true);
                tag = null;

                if (response.numTagsFound > 1) throw new Exception("more than one tag found in the antena writing userdata");

                tag = inv[0];

                byte[] USERDATA = new byte[USERbyteCount];
                USERDATA[0] = (byte)(USERDATA[0]);
                tag.WriteTag(0, false, USERbank, 0, USERDATA);

                return true;
            }
            catch (Exception ex)
            {
                _Error = "Error initializing RFID tag " + ex.Message;
                return false;
            }
        }

        #endregion
    }
}