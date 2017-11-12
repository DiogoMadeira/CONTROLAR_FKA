using System;
using System.Threading;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace Preh
{
    public class IOCycle
    {
        #region Events
        public static event Action BKConnectionError;
        public static event Action<string> Error;
        #endregion

        //TODO make it private and handled it in a different way. Was placed like this because of dependecies with HMI
        public DataTable Dt_DO;
        public DataTable Dt_DI;
        public DataTable Dt_AI;
        public DataTable Dt_AO;

        #region Properties
        public ModBus MyBK { get; }
        public bool CycleRunning { get; set; }
        public bool BKConnected { get; set; }
        public bool[] InhibitOutputs { get; set; }



        public int InhibitOutputsRefreshTime { get; set; }
        #endregion

        System.Timers.Timer InhibitOutputsTimer;

        private byte[] bk_Read_DI = new byte[128];
        private byte[] bk_Read_DO = new byte[128];
        private byte[] bk_Write_DO = new byte[128];

        private byte[] bk_Read_AI = new byte[80];
        private int[] bk_Read_AO = new int[20];
        private byte[] bk_Write_AO = new byte[80];

        SafeMovements Safety;

        public bool NeedToWrite;
        public bool NeedToWriteAO;


        private static object thislock = new object();

        private static object thislockReadDI;
        private static object thislockReadDO;
        private static object thislockWriteDI;
        private static object thislockWriteDO;


        private static object thislockReadAI;
        private static object thislockReadAO;
        private static object thislockWriteAO;

        public bool StopReadAO;

        public enum ReadWriteIO { ReadDI, ReadDO, WriteDI, WriteDO }

        public IOCycle(string BKIPAdress)
        {

            thislockReadDI = new object();
            thislockReadDO = new object();
            thislockWriteDI = new object();
            thislockWriteDO = new object();


            thislockReadAI = new object();
            thislockReadAO = new object();
            thislockWriteAO = new object();

            StopReadAO = false;
            MyBK = new ModBus(BKIPAdress);
            ModBus.ConnectionEvent += BKConnectionEvent;
            PrepareIOMap();

            Thread.Sleep(200);
            if (MyBK.wsStatus != 1) BKConnectionError?.Invoke();
            else BKConnected = true;
        }

        //Função para fazer o Update do ValueRead do Array DI
        //Função executada pelo Thread para Escrita/Leitura de Entradas/Saídas de PLC
        private void IOReadWrite()
        {
            do
            {
                //-----------------------------------------------------------------------
                //Ciclo de Leitura de Entradas Digitais:
                //-----------------------------------------------------------------------
                if (bk_Read_DI.Length > 0)
                {
                    MyBK.ReadDigitalInput(0, (Int16)bk_Read_DI.Length, bk_Read_DI);
                    UpdateArrayDI();
                    Thread.Sleep(10);
                }
                

                //-----------------------------------------------------------------------
                //Ciclo de Leitura de Saidas Digitais:
                //-----------------------------------------------------------------------
                if (bk_Read_DO.Length > 0)
                {
                    MyBK.ReadDigitalOutput(0, (Int16)bk_Read_DO.Length, bk_Read_DO);
                    UpdateArrayDO();
                    Thread.Sleep(10);
                }
                

                //-----------------------------------------------------------------------
                //Ciclo de Escrita de Saídas Digitais:
                //-----------------------------------------------------------------------
                if (bk_Write_DO.Length > 0 && NeedToWrite)
                {
                    UpdateBK_Write();
                    MyBK.WriteMultipleDigitalOutputs(0, (Int16)bk_Write_DO.Length, bk_Write_DO);
                    NeedToWrite = false;
                    Thread.Sleep(10);
                }

                //-----------------------------------------------------------------------
                //Ciclo de Leitura de Entradas Analógicas:
                //-----------------------------------------------------------------------
                if (bk_Read_AI.Length > 0)
                {
                    for (int i = 0; i < Dt_AI.Rows.Count; i++)
                    {
                        var valorAnalogica = 0;
                        MyBK.ReadAnalogInput((short)(i * 2 + 1), ref valorAnalogica);
                        Dt_AI.Rows[i]["Value"] = valorAnalogica;
                        Thread.Sleep(10);
                    }
                }
                //-----------------------------------------------------------------------
                //Ciclo de Escrita de Saidas Analógicas:
                //-----------------------------------------------------------------------
                if (bk_Write_AO.Length > 0)
                {
                    if (NeedToWriteAO)
                    {
                        for (int i = 0; i < Dt_AO.Rows.Count; i++)
                        {
                            var valorAnalogica = 0.0;
                            if (double.TryParse(Dt_AO.Rows[i]["Value"].ToString(), out valorAnalogica))
                            {
                                MyBK.WriteAnalogOutput((short)(i * 2 + 1 + (Dt_AI.Rows.Count * 2)), (double)valorAnalogica * 32767 / 10);
                                NeedToWriteAO = false;
                                
                            }
                        }
                        Thread.Sleep(10);
                    }
                }

                //-----------------------------------------------------------------------
                //Ciclo de Leitura de Saidas Analógicas:
                //-----------------------------------------------------------------------
                if (bk_Write_AO.Length > 0)
                {

                    if (NeedToWriteAO)
                    {
                        for (int i = 0; i < Dt_AO.Rows.Count; i++)
                        {
                            var valorAnalogica = 0.0;
                            if (double.TryParse(Dt_AO.Rows[i]["Value"].ToString(), out valorAnalogica))
                            {
                                MyBK.WriteAnalogOutput((short)(i * 2 + 1 + (Dt_AI.Rows.Count * 2)), (double)valorAnalogica * 32767 / 10);
                                NeedToWriteAO = false;
                                Thread.Sleep(10);
                            }


                        }
                    }

                }
                //if (Safety == null) { Safety.Dt_DI = Dt_DI; }
                CycleRunning = true;
            } while (MyBK.wsStatus == 1);

            CycleRunning = false;
        }

        private void PrepareIOMap()
        {
            Dt_DO = new DataTable("DO_Table");
            Dt_DI = new DataTable("DI_Table");
            Dt_AI = new DataTable("AI_Table");
            Dt_AO = new DataTable("AO_Table");

            DataColumn DcName, DcAddr, DcVal, DCValToWrite;
            var Keys = new DataColumn[1];

            // Config Table DI:
            DcName = new DataColumn("DIName", typeof(string));
            DcAddr = new DataColumn("Address", typeof(int));
            DcVal = new DataColumn("Value", typeof(bool));

            Keys[0] = DcName;
            Dt_DI.Columns.AddRange(new DataColumn[] { DcName, DcAddr, DcVal });
            Dt_DI.PrimaryKey = Keys;

            //Config Table DO:
            DcName = new DataColumn("DOName", typeof(string));
            DcAddr = new DataColumn("Address", typeof(int));
            DcVal = new DataColumn("Value", typeof(bool));
            DCValToWrite = new DataColumn("ValueToWrite", typeof(bool));

            Keys[0] = DcName;
            Dt_DO.Columns.AddRange(new DataColumn[] { DcName, DcAddr, DcVal, DCValToWrite });
            Dt_DO.PrimaryKey = Keys;

            //Config Table AI:
            DcName = new DataColumn("AIName", typeof(string));
            DcAddr = new DataColumn("Address", typeof(int));
            DcVal = new DataColumn("Value", typeof(int));

            Keys[0] = DcName;
            Dt_AI.Columns.AddRange(new DataColumn[] { DcName, DcAddr, DcVal });
            Dt_AI.PrimaryKey = Keys;

            //Config Table AO:
            DcName = new DataColumn("AOName", typeof(string));
            DcAddr = new DataColumn("Address", typeof(int));
            DcVal = new DataColumn("Value", typeof(int));

            Keys[0] = DcName;
            Dt_AO.Columns.AddRange(new DataColumn[] { DcName, DcAddr, DcVal });
            Dt_AO.PrimaryKey = Keys;

        }

        private void UpdateArrayDI()
        {
            int j = 0;
            if (bk_Read_DI.Length > 0)
                for (int i = 0; i < Dt_DI.Rows.Count; i++)
                {
                    j = i / 8;
                    try
                    {
                        UpdateDIORows(i, "Value", ((bk_Read_DI[j] & Convert.ToInt16(Math.Pow(2, (i - j * 8)))) == Convert.ToInt16(Math.Pow(2, (i - j * 8)))), ReadWriteIO.WriteDI, "");
                    }
                    catch (Exception exp)
                    {
                        Error?.Invoke("UpdateArrayDI Error: " + exp.ToString());
                    }
                }
        }

        //Função para fazer o Update do ValueRead do Array DO
        private void UpdateArrayDO()
        {
            int j = 0;
            if (bk_Read_DO.Length > 0)
                for (int i = 0; i < Dt_DO.Rows.Count; i++)
                {
                    j = i / 8;
                    try
                    {
                        UpdateDIORows(i, "Value", ((bk_Read_DO[j] & Convert.ToInt16(Math.Pow(2, (i - j * 8)))) == Convert.ToInt16(Math.Pow(2, (i - j * 8)))), ReadWriteIO.WriteDO, "");
                    }
                    catch (Exception exp)
                    {
                        Error?.Invoke("UpdateArrayDO Error: " + exp.ToString());
                    }
                }
        }

        //Função para actualizar Array BK_Write com o valor a escrever no PLC
        private void UpdateBK_Write()
        {
            try
            {
                var j = 0;
                for (j = 0; j <= bk_Write_DO.Length / 8; j++)
                {
                    bk_Write_DO[j] = 0;
                }
                for (int i = 0; i < Dt_DO.Rows.Count; i++)
                {
                    //if (doMap[i].ValueWrite) {
                    if (UpdateDIORows(i, "ValueToWrite", null, ReadWriteIO.ReadDO, ""))
                    {
                        j = i / 8;
                        bk_Write_DO[j] += Convert.ToByte(Math.Pow(2, (i - j * 8)));
                    }
                }
            }
            catch (Exception)
            {
                Error?.Invoke("UpdateBK_Write Exception");
            }
        }

        public bool UpdateDIORows(int IOIndex, string IOField, Nullable<bool> State, ReadWriteIO ReadWrite, string IOName)
        {
            //lock (thislock)
            //{
            //Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
            try
            {
                //Para leitura de DI:
                if (ReadWrite == ReadWriteIO.ReadDI)
                {
                    lock (thislockReadDI)
                    {
                        try
                        {
                            if (IOName.Equals(""))
                            {
                                return (bool)Dt_DI.Rows[IOIndex][IOField];
                            }
                            else
                            {
                                int Rowindex = Dt_DI.Rows.IndexOf(Dt_DI.Select("DIName = '" + IOName + "'")[0]);
                                return (bool)Dt_DI.Rows[Rowindex][IOField];
                            }
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                }

                //Para leitura de DO:
                else if (ReadWrite == ReadWriteIO.ReadDO)
                {
                    lock (thislockReadDO)
                    {
                        try
                        {
                            if (IOName.Equals(""))
                            {
                                return (bool)Dt_DO.Rows[IOIndex][IOField];
                            }
                            else
                            {
                                int Rowindex = Dt_DO.Rows.IndexOf(Dt_DO.Select("DOName = '" + IOName + "'")[0]);
                                return (bool)Dt_DO.Rows[Rowindex][IOField];
                            }

                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                }

                //Para escrita de DI:
                else if (ReadWrite == ReadWriteIO.WriteDI)
                {
                    lock (thislockWriteDI)
                    {
                        try
                        {
                            if (IOName.Equals(""))
                            {
                                Dt_DI.Rows[IOIndex][IOField] = State;
                                return true;
                            }
                            else
                            {
                                int Rowindex = Dt_DI.Rows.IndexOf(Dt_DO.Select("DIName = '" + IOName + "'")[0]);
                                Dt_DI.Rows[Rowindex][IOField] = State;
                                return true;
                            }

                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                }
                //Para escrita de DO:
                else if (ReadWrite == ReadWriteIO.WriteDO)
                {
                    lock (thislockWriteDO)
                    {
                        try
                        {

                            if (IOName.Equals(""))
                            {
                                Dt_DO.Rows[IOIndex][IOField] = State;
                                return true;
                            }
                            else
                            {
                                int Rowindex = Dt_DO.Rows.IndexOf(Dt_DO.Select("DOName = '" + IOName + "'")[0]);
                                Dt_DO.Rows[Rowindex][IOField] = State;

                                return true;
                            }

                        }
                        catch (Exception ex)
                        {

                            throw ex.InnerException;
                        }
                    }
                }
                else
                {

                    Error?.Invoke("UpdateDIORows Error: \r\n\r\nWrong Parameters received");
                    return false;
                }
            }
            catch (Exception ex)
            {

                Error?.Invoke("UpdateDIORows Error: " + ex.ToString());
                return false;
                throw ex.InnerException;
            }
            //}
        }
        #region Read/Write IO
        // NOVAS FUNÇÕES
        public bool ReadDI(EngineData.DI e)
        {
            try
            {
                return UpdateDIORows((int)e, "Value", null, ReadWriteIO.ReadDI, "");
            }
            catch (Exception)
            {

                try
                {
                    return UpdateDIORows((int)e, "Value", null, ReadWriteIO.ReadDI, "");
                }
                catch (Exception) { return false; }
            }
        }


        public bool ReadDO(EngineData.DO e)
        {
            try
            {
                return UpdateDIORows((int)e, "Value", null, ReadWriteIO.ReadDO, "");
            }
            catch (Exception)
            {
                Delay(10);
                try
                {
                    return UpdateDIORows((int)e, "Value", null, ReadWriteIO.ReadDO, "");
                }
                catch (Exception) { return false; }
            }
        }
        public double ReadAI(EngineData.AI e)
        {
            lock (thislockReadAI)
            {
                int Rowindex = (int)Dt_AI.Rows[(int)e]["Value"];
                return Math.Round(((double)Rowindex * 30 / 32767), 2);
            }
        }

        public void WriteDO(EngineData.DO e, bool est)
        {
            NeedToWrite = true;

            try
            {
                if (InhibitOutputs[(int)e])
                {
                    UpdateDIORows((int)e, "ValueToWrite", est, ReadWriteIO.WriteDO, "");
                }
            }
            catch (Exception exp)
            {
                Error?.Invoke("WriteDO Error: " + exp.ToString());
            }
        }

        //FUNÇÃO DE LEITURA DAS ENTRADAS E DAS SAÍDA
        public bool ReadDIO(string tipo, int e)
        {
            try
            {
                if (tipo == "DI") { return UpdateDIORows(e, "Value", null, ReadWriteIO.ReadDI, ""); }
                if (tipo == "DO") { return UpdateDIORows(e, "Value", null, ReadWriteIO.ReadDO, ""); } else return false;
            }
            catch (Exception)
            {
                Delay(10);
                try
                {
                    if (tipo == "DI") { return UpdateDIORows(e, "Value", null, ReadWriteIO.ReadDI, ""); }
                    if (tipo == "DO") { return UpdateDIORows(e, "Value", null, ReadWriteIO.ReadDO, ""); } else return false;
                }
                catch (Exception) { return false; }
            }
        }
        public int ReadDAI(int e)
        {
            return (int)Dt_AI.Rows[e]["Value"];
        }

        public double ReadAO(EngineData.AO e)
        {
            lock (thislockReadAO)
            {
                int Rowindex = (int)Dt_AO.Rows[(int)e]["Value"];
                return Math.Round(((double)Rowindex * 30 / 32767), 2);
            }
        }

        public int ReadAO(int e)
        {
            return (int)Dt_AO.Rows[e]["Value"];
        }

        public void WriteAO(EngineData.AO e, double value)
        {
            //NeedToWriteAO = true;
            WriteAO((int)e, value);
        }
        public void WriteAO(int e, double value)
        {
            lock (thislockWriteAO)
            {
                Dt_AO.Rows[e]["Value"] = (value * 32767) / 10;
                NeedToWriteAO = true;
            }
        }
        public void WriteDO(int e, bool est)
        {
            NeedToWrite = true;

            try
            {
                if (InhibitOutputs[e]) UpdateDIORows(e, "ValueToWrite", est, ReadWriteIO.WriteDO, "");
            }
            catch (Exception exp)
            {
                Error?.Invoke("WriteDO Error: " + exp.ToString());
            }
        }

        private bool ReadDIByName(string DIName)
        {
            try
            {
                return UpdateDIORows(0, "Value", null, ReadWriteIO.ReadDI, DIName);
            }
            catch (Exception)
            {
                Delay(10);
                try
                {
                    return UpdateDIORows(0, "Value", null, ReadWriteIO.ReadDI, DIName);
                }
                catch (Exception)
                {
                    Error?.Invoke("Error in DI: " + DIName);
                    return false;
                }
            }
        }

        private bool ReadDOByName(string DOName)
        {
            return UpdateDIORows(0, "Value", null, ReadWriteIO.ReadDO, DOName);
        }

        private double ReadAIByName(string AIName)
        {
            int Rowindex = Dt_AI.Rows.IndexOf(Dt_AI.Select("AIName = '" + AIName + "'")[0]);
            if (Rowindex < 2) return (double)Math.Round(((double)ReadDAI(Rowindex) * 30 / 32767), 2);
            else return (double)Math.Round(((double)ReadDAI(Rowindex) * 30 / 32767), 2);
        }

        private void WriteDOByName(string DOName, bool est)
        {
            NeedToWrite = true;
            try
            {
                int Rowindex = Dt_DO.Rows.IndexOf(Dt_DO.Select("DOName = '" + DOName + "'")[0]);
                UpdateDIORows(0, "ValueToWrite", est, ReadWriteIO.WriteDO, DOName);
            }
            catch (Exception exp) { Error?.Invoke("WriteDOByName Error: " + exp.ToString()); }
        }
        #endregion Read/Write IO

        public void SetSafeMovements(List<IAIModbusASCII> iaimovements)
        {
            InhibitOutputs = new bool[Dt_DO.Rows.Count];
            InhibitOutputsTimer = new System.Timers.Timer();
            InhibitOutputsRefreshTime = 200;
            StartSafeMovements(iaimovements);
        }

        private void StartSafeMovements(List<IAIModbusASCII> iaimovements)
        {
            Safety = new SafeMovements(this, iaimovements, InhibitOutputs);
            InhibitOutputsTimer.Interval = InhibitOutputsRefreshTime;
            InhibitOutputsTimer.Enabled = true;
            InhibitOutputsTimer.Elapsed += new ElapsedEventHandler(UpdateSafeMovements);
            InhibitOutputsTimer.Start();
        }

        private void UpdateSafeMovements(object source, System.Timers.ElapsedEventArgs e)
        {
            InhibitOutputsTimer.Stop();
            UpdateDOEnableArray(Safety.SafeMovementsLogic);              //Actualiza o array de enable através das restrições
            InhibitOutputsTimer.Start();
        }

        public void UpdateDOEnableArray(Func<bool[]> SafeMovementsLogic)
        {
            var MyList = new List<byte>(new byte[] { });

            for (byte i = 0; i < InhibitOutputs.Length; i++)
            {
                if (!MyList.Contains(i)) InhibitOutputs[i] = true;
            }

            var SafeMovementsTask = Task<bool[]>.Factory.StartNew(() =>
            {
                return SafeMovementsLogic?.Invoke();
            });

            InhibitOutputs = SafeMovementsTask.Result;
        }

        void BKConnectionEvent(bool status)
        {
            BKConnected = status;
            if (status)
            {
                var IOThread = new Thread(new ThreadStart(this.IOReadWrite))
                {
                    Name = "IOThread",
                    IsBackground = true
                };
                IOThread.Start();
            }
        }

        public static void Delay(int mSec)
        {
            double Ti = DateTime.Now.TimeOfDay.TotalMilliseconds, Tf;
            do
            {
                Tf = DateTime.Now.TimeOfDay.TotalMilliseconds;
                if (Tf < Ti) Ti = Tf;
            } while ((Tf - Ti) < (mSec));
        }

        public void Dispose()
        {
            MyBK.CloseSocket();
        }
    }
}
