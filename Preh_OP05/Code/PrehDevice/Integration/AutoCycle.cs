using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Data;
using PPDBAccess;

namespace Preh
{
    public class AutoCycle : Cycle
    {

        Reference RefLCD;
        private Engine MainEngine;
        public static bool LastCycle = false;
        public static bool FirstCycle = true;
        private static bool PartOK = false;
        private static bool PartOK_FirstCycle = false;
        private bool Result;
        public static bool FailPartLockWorkMov = false;
        public static bool FailPartunLockMov = false;
        public static bool FailScannerRead = false;
        public static bool PedalPressed = false;
        public static bool FailTableWorkMov = false;
        public static bool FailTabletoHomeMov = false;
        public static bool FailProySupWorkMov = false;
        public static bool FailProyInfWorkMov = false;
        public static bool FailProySupHomeMov = false;
        public static bool FailProyInfHomeMov = false;
        public static bool FailIonizer = false;
        public static bool FailCalibCycle = false;
        public static bool FailTestAnalogDigital = false;
        public static bool RemoveFirstCycle = false;

        #region Clean this after test with new cycle
        //Local Variables to potis 
        //public struct Cavidade1_Potis
        //{
        //    public static double Poti5_Knob_Cap_UP_L_EATC = 0;
        //    public static double Poti6_Knob_Cap_UP_R_EATC = 0;
        //    public static double Poti7_Divider_Chrome_L_EATC = 0;
        //    public static double Poti8_Divider_Chrome_R_EATC = 0;
        //    public static double Poti11_ChromeRing_L_EATC_C2_1 = 0;
        //    public static double Poti12_ChromeRing_R_EATC_C2_1 = 0;
        //    public static double Poti16_Housing_GF_EATC = 0;
        //    public static double Poti17_LG_Rotary_L_EATC = 0;
        //    public static double Poti18_LG_Rotary_R_EATC = 0;
        //    public static double Poti19_Button_Rotary_L_EATC = 0;
        //    public static double Poti20_Button_Rotary_R_EATC = 0;
        //}

        //public struct MAC_Potis
        //{
        //    public static double Poti1_Knob_Cap_Blower_L_MAC = 0;
        //    public static double Poti2_Knob_Cap_Blower_R_MAC = 0;
        //    public static double Poti3_Divider_Chrome_L_MAC = 0;
        //    public static double Poti4_Divider_Chrome_R_MAC = 0;
        //    public static double Poti9_ChromeRing_L_MAC_C1_1 = 0;
        //    public static double Poti10_ChromeRing_R_MAC_C1_1 = 0;
        //    public static double Poti13_Housing_GF_MAC = 0;
        //    public static double Poti14_LG_Max_Defrost_MAC = 0;
        //    public static double Poti15_LG_Max_AC_MAC = 0;
        //}
        #endregion

        private EngineData.Step nextStep;
        private int JobStartError, CheckSNError, UpgradeRefError, WSJobID, JobEndError, AssignRefError, SaveFoilError, SavePlasmaError, GetRefError, CheckAssemblyError, SaveBatchAssemblyError, SaveInfoError, ScannerRetries, TestPartRetries;
        private bool CheckLCDinspection;

        private string TraceNR, PotiError, ValidCavity;
        long TagTraceNr = 0;
        private int IFMRetries;
        private int IFM1_Prog;
        private int IFM2_Prog;

        public static int RFID_IDRef = 0;
        public static string RFID_RefPreh = "";
        public static string RFID_RefShortType = "";
        public static string RFID_RefDescription = "";
        public static string RFID_TrayCapacity = "";
        public static string RFID_TrayCicleLimit = "";

        private string[] RFIDTag;
        private byte GlueTestNumber = 1;

        EngineData.AI[] analogResults;
        EngineData.DI[] digitalResults;
        private int[] analogResult;
        Dictionary<Preh.EngineData.DI,int> digitalResult;
        
        EngineData.DI[] DigitalDetect_C1 = new EngineData.DI[] { EngineData.DI.Det_Display_L, EngineData.DI.Det_Display_R};
        
        public AutoCycle(PPTraceStation db, string prehref, int wsID, Dictionary<string, int> subWCIDs, bool usesTraceability, int cycleid, bool hashomecycle, bool usesRFIDTraceability) :
           base(db, prehref, wsID, subWCIDs, usesTraceability, cycleid, hashomecycle, usesRFIDTraceability)
        { }

        public void RunCycle()
        {

            //UpdateStep(Step);
            switch (Step)
            {
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case 0:
                    LastCycle = false;
                    FirstCycle = true;
                    Step = EngineData.Step.Step_Initial;
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.Step_Initial:
                    #region Initial step
                    ResetAll();
                    ScannerRetries = 0;
                    ValidCavity = "";
                    IsCaliber = false;

                    WriteDO(EngineData.DO.Signal_OK, false);
                    WriteDO(EngineData.DO.Signal_NOK, false);
                    Blink(EngineData.DO.Signal_NOK, false, 500);
                    WriteDO(EngineData.DO.Relay_Machine_Light, true);
                    
                    ValidCavity = "Cavidade1";
                    EngineData.DI[] digital = {
                            EngineData.DI.Det_Display_L,
                            EngineData.DI.Det_Display_R,
                        };
                    digitalResults = digital;



                    if (ReadDI(EngineData.DI.Ionizer_NoError) && !ReadDI(EngineData.DI.Ionizer_Maintenance))
                    {
                        Step = EngineData.Step.InsertPCB;
                    }
                    else
                    {
                        FailIonizer = true;
                        Step = EngineData.Step.FailMovement;
                    }                    

                    #endregion
                    break;
                    
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.InsertPCB:
                    #region Insert PCB on nest and lock

                    ClearIFM_Trigger_Camera1();
                    ClearIFM_Trigger_Camera2();

                    WriteDO(EngineData.DO.Sol_Cyl_Clinch_H, true);
                    WriteDO(EngineData.DO.Sol_Cyl_Clinch_W, false);
                    WriteDO(EngineData.DO.Sol_Ionizer_W, true);
                    WriteDO(EngineData.DO.Ionizer_Stop_discharge, false);

                    //pedir para inserir PCB
                    HMI.JoinMessage("Insert PCB");
                    HMI.EndMessage();

                    if (hasHousing(ValidCavity))
                    {
                        if (TON("TimeToLockPCB", 500))
                        {
                            StopTON("TimeToLockPCB");
                            WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, true);
                            if (isPartLockWork(ValidCavity))
                            {
                                StopTON("TimeoutforLockWorkMov");
                                if (UsingTraceability)
                                    Step = EngineData.Step.ScannerRead;
                                else
                                    Step = EngineData.Step.InsertLightCaseandRubbersandDiffuserFoil;
                            }

                            else if (TON("TimeoutforLockWorkMov", 1000))
                            {
                                StopTON("TimeoutforLockWorkMov");
                                FailPartLockWorkMov = true;
                                Blink(EngineData.DO.Signal_NOK, true, 500);
                                Step = EngineData.Step.FailMovement;
                            }
                        }
                    }



                    #endregion
                    break;

                #region Scanner Reads and Traceability
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ScannerRead:
                    #region Traceability label scanner read

                    HMI.JoinAndSendMessage("Reading TraceNr");
                    var msg = "";
                    Hardware.StartScanner(ValidCavity);
                    ReadScanner(ValidCavity, out msg);

                    if (!string.IsNullOrEmpty(msg))
                    {
                        StopTON("TimeoutforScanner");
                        TraceNR = msg;
                        Hardware.StopScanner(ValidCavity);
                        Hardware.ReleaseHardwareOnThisStep();
                        Step = EngineData.Step.CheckCaliber;
                    }
                    else if (TON("TimeoutforScanner", 5000))
                    {
                        StopTON("TimeoutforScanner");
                        Hardware.StopScanner(ValidCavity);
                        Hardware.ReleaseHardwareOnThisStep();
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        Step = EngineData.Step.ScannerFail;
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.CheckCaliber:
                    #region Check if it's caliber part
                    if (CheckCaliber(long.Parse(TraceNR)))
                    {
                        IsCaliber = true;
                        ReadCaliber(long.Parse(TraceNR));
                        Step = EngineData.Step.PressPedal;
                    }
                    else
                        Step = EngineData.Step.TraceabilityCheckTraceNr;

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TraceabilityCheckTraceNr:
                    #region Traceability Check TraceNr
                   
                    CheckSNError = DBResource.Trace_CheckTraceNr(long.Parse(TraceNR), EngineData.PrehRef, WSID, 1);

                    if (CheckSNError == 0)
                    {
                        Step = EngineData.Step.TraceabilityGetLCDInspectionData;
                    }
                    else
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinMessage("Function Error:");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Check Serial Number Error!");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Press Button NOK");
                        HMI.EndMessage();                      
                        Step = EngineData.Step.TraceabilityAckButtonNOK;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TraceabilityGetLCDInspectionData:
                    #region Traceability Get LCD Info (red or blue)


                    RefLCD = new Reference();
                    CheckLCDinspection = DBResource.GetLCDInspectionData(DBResource.SelectedReference.IDRef, WSID, out RefLCD, out IFM1_Prog, out IFM2_Prog);
                                        
                    if (CheckLCDinspection == true)
                    {
                        Step = EngineData.Step.TraceabilityJobStart;
                    }
                    else
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinMessage("Function Error");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinTraceErrorDescription(CheckSNError);
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Press Button NOK");
                        HMI.EndMessage();                        
                        Step = EngineData.Step.TraceabilityAckButtonNOK;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TraceabilityJobStart:
                    #region Traceability Job Start                   
                        JobStartError = DBResource.Trace_JobStart(long.Parse(TraceNR));

                    if (JobStartError == 0)
                    {
                        Step = EngineData.Step.InsertLightCaseandRubbersandDiffuserFoil;                   

                    }
                    else
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinTraceErrorDescription(JobStartError);
                        HMI.JoinMessage("Function Error");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Press Button NOK");
                        HMI.EndMessage();                        
                        Step = EngineData.Step.TraceabilityAckButtonNOK;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TraceabilityAckButtonNOK:
                    #region Traceability ACK the error (press button)                  

                    if (ReadDI(EngineData.DI.Button_NOK))
                        Step = EngineData.Step.ErrorTrace;

                    #endregion
                    break;
                    
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ErrorTrace:
                    #region Error Trace
                    HMI.JoinAndSendMessage("Release NOK Button");

                    if (!ReadDI(EngineData.DI.Button_NOK))
                        Step = EngineData.Step.RemovePartandDiscard;
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ScannerFail:
                    #region Timeout for scanner read
                    HMI.ReleaseAllMessages();
                    HMI.JoinMessage("Fail Scanner Read");
                    HMI.JoinMessage(Environment.NewLine);
                    HMI.JoinMessage("Press Button NOK");
                    HMI.EndMessage();

                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        FailScannerRead = true;
                        Step = EngineData.Step.ReleaseNOKAcknowledge;
                    }
                    #endregion
                    break;                                 

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.PressPedalScanner:
                    #region Press Pedal to Retry Scanner Read

                    Blink(EngineData.DO.Signal_NOK, false, 500);
                    WriteDO(EngineData.DO.Signal_NOK, true);
                    HMI.JoinMessage("Press Pedal or Button NOK");
                    HMI.EndMessage();


                    if (Convert.ToInt16(GetDeviceConsts("ScannerRetries").ConstValue) > ScannerRetries)
                    {
                        if (ReadDI(EngineData.DI.Foot_Start))
                        {
                            ScannerRetries++;
                            WriteDO(EngineData.DO.Signal_NOK, false);
                            Step = EngineData.Step.ScannerRead;
                        }
                        else if (ReadDI(EngineData.DI.Button_NOK))
                        {
                            ScannerRetries = 0;
                            HMI.JoinAndSendMessage("Remove Part Nest");
                            WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);
                            if (!hasHousing(ValidCavity))
                                Step = EngineData.Step.RemovePartandDiscard;

                        }
                    }
                    else
                    {
                        ScannerRetries = 0;
                        HMI.JoinAndSendMessage("Remove Part Nest");
                        WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);
                        if (!hasHousing(ValidCavity))
                            Step = EngineData.Step.Step_Initial;
                    }

                    #endregion
                    break;
                #endregion

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.InsertLightCaseandRubbersandDiffuserFoil:
                    #region Insert Light Case, Rubbers, Diffuser Foil in PCB

                    if (ValidCavity == "Cavidade1")
                    {
                        HMI.JoinMessage("Insert Light Case");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Insert Rubbers");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Insert Diffuser");
                        HMI.JoinMessage(Environment.NewLine);                       
                        HMI.JoinAndSendMessage("Press Pedal");
                        HMI.JoinMessage(Environment.NewLine);
                        
                        if (ReadDI(EngineData.DI.Foot_Start))
                        {
                            Step = EngineData.Step.State_CheckIFM1;
                        }
                    }
                    else
                    {
                        HMI.JoinMessage("Insert Light Case");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Insert Rubbers");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Insert Diffuser");
                        HMI.JoinMessage(Environment.NewLine);                        
                        HMI.JoinAndSendMessage("Press Pedal");
                        HMI.JoinMessage(Environment.NewLine);
                        
                        if (ReadDI(EngineData.DI.Foot_Start))
                        {
                            Step = EngineData.Step.State_CheckIFM1_Camera2;
                        }
                    }

                    #endregion
                    break;

                #region IFM (VERIFY THE PRESENCE OF THE LIGHT CASE/RUBBERS/DIFFUSER)                  
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM1:
                    #region State_CheckIFM

                    IFMRetries = 0;
                    
                    if (Convert.ToInt16(GetDeviceConsts("IFM_Enable").ConstValue) == 0)
                    {
                        Step = EngineData.Step.InsertLCD;
                    }
                    else if (Convert.ToInt16(GetDeviceConsts("IFM_Enable").ConstValue) == 1)
                    {
                        if (ReadDI(EngineData.DI.Foot_Start))
                        {
                            ClearAllTimers();
                            Step = EngineData.Step.State_CheckIFM2;
                        }
                        else
                        {
                            HMI.JoinMessage("Press Pedal");
                            HMI.EndMessage();

                        }
                    }


                    #endregion State_CheckIFM
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM2:
                    #region State_CheckIFM

                    HMI.ReleaseAllMessages();
                    HMI.JoinMessage("IFM Cycles:Verify the presence of the rubbers and diffuser!");
                    //HMI.JoinMessage(IFMRetries.ToString());
                    HMI.EndMessage();

                    RunIFMTest();

                    if (TON("TriggerDelay", 1000))
                    {
                        if (CheckIFMTest())
                        {
                            HMI.JoinMessage("ReadingIFM");
                            HMI.EndMessage();
                            ClearIFM_Trigger();
                            Step = EngineData.Step.InsertLCD;
                        }
                        else if (TON("State_CheckIFM2", 5000))
                        {


                            if (Convert.ToInt16(GetDeviceConsts("IFM_Retries").ConstValue) > IFMRetries)
                            {
                                Step = EngineData.Step.State_CheckIFM3;
                                IFMRetries++;
                            }
                            else
                            {
                                //DBResource.SaveStringResult(DBResource.JobID, "IFM Foil inspection", "", "", DBResource.Result.Devices.Hardware.Move, (int)ResultObject.Device_HW_IFM, ResultUnit.TEXT, false);
                                HMI.JoinMessage("Fail IFM Read");
                                HMI.EndMessage();
                                Step = EngineData.Step.RemovePartandDiscard;

                            }
                        }
                    }

                    #endregion State_CheckIFM
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM3:
                    #region State_CheckIFM

                    HMI.ReleaseAllMessages();
                    Blink(EngineData.DO.Signal_NOK, true, 500);
                    HMI.JoinMessage("Fail Inspection: Verify the presence of the rubbers and diffuser!");
                    HMI.JoinMessage(Environment.NewLine);
                    HMI.JoinMessage("Press Button NOK");
                    HMI.EndMessage();
                    ClearIFM_Trigger();

                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        Blink(EngineData.DO.Signal_NOK, false, 500);
                        WriteDO(EngineData.DO.Signal_NOK, true);

                        //Move_Lock(false);
                        Step = EngineData.Step.State_CheckIFM4;
                    }

                    #endregion State_CheckIFM
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM4:
                    #region State_CheckIFM
                    HMI.ReleaseAllMessages();
                    if (!ReadDI(EngineData.DI.Button_NOK))
                    {
                        ClearAllTimers();
                        ClearIFM_Trigger();
                        Step = EngineData.Step.State_CheckIFM5;
                    }

                    #endregion State_CheckIFM
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM5:
                    #region State_CheckIFM
                    Blink(EngineData.DO.Signal_NOK, false, 500);
                    HMI.ReleaseAllMessages();
                    HMI.JoinMessage("Press Pedal or Button NOK");
                    HMI.EndMessage();

                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        //DBResource.SaveStringResult(DBResource.JobID, "IFM Foil inspection", "", "", DBResource.Result.Devices.Hardware.Move, (int)ResultObject.Device_HW_IFM, ResultUnit.TEXT, false);
                        WriteDO(EngineData.DO.Signal_NOK, false);                       
                        Step = EngineData.Step.RemovePartandDiscard;
                    }
                    else if (ReadDI(EngineData.DI.Foot_Start))
                    {                       
                        WriteDO(EngineData.DO.Signal_NOK, false);
                        Step = EngineData.Step.State_CheckIFM2;
                    }

                    #endregion State_CheckIFM
                    break;

                #endregion IFM

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.InsertLCD:
                    #region Insert LCD on the Nest

                    ChangeIFMTest_Camera2();

                    if (!ReadDI(EngineData.DI.Det_Foil_L) && ReadDI(EngineData.DI.Det_Foil_R) && TON("Det_Foil", 1000))
                    {
                        Step = EngineData.Step.State_CheckIFM1_Camera2;
                    }
                    else
                    {
                        if (IFM2_Prog == 1) //LCD RED
                        {
                            HMI.JoinMessage("Remove the Yellow Foil from the LCD (LCD With Red Foil)");
                            HMI.JoinMessage(Environment.NewLine);
                            HMI.JoinMessage("Place The LCD on the Nest");
                            HMI.JoinMessage(Environment.NewLine);
                            HMI.JoinMessage("Press Pedal");
                            HMI.EndMessage();
                        }
                        else //LCD Blue
                        {

                            HMI.JoinMessage("Remove the Yellow Foil from the LCD (LCD With Blue Foil)");
                            HMI.JoinMessage(Environment.NewLine);
                            HMI.JoinMessage("Place The LCD on the Nest");
                            HMI.JoinMessage(Environment.NewLine);
                            HMI.JoinMessage("Press Pedal");
                            HMI.EndMessage();
                        }

                    }

                    #endregion
                    break;

                #region IFM_Camera 2 (LCD RED FOIL)                  
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM1_Camera2:
                    #region State_CheckIFM

                    IFMRetries = 0;

                    if (Convert.ToInt16(GetDeviceConsts("IFM_Enable").ConstValue) == 0)
                    {
                        Step = EngineData.Step.RemoveFoil;
                    }
                    else if (Convert.ToInt16(GetDeviceConsts("IFM_Enable").ConstValue) == 1)
                    {
                        if (ReadDI(EngineData.DI.Foot_Start))
                        {
                            ClearAllTimers();
                            Step = EngineData.Step.State_CheckIFM2_Camera2;
                        }
                        else
                        {
                            HMI.JoinMessage("Press Pedal");
                            HMI.EndMessage();

                        }
                    }


                    #endregion State_CheckIFM
                    break;
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM2_Camera2:
                    #region State_CheckIFM

                    HMI.ReleaseAllMessages();
                    HMI.JoinMessage("IFMCycles: Verify the LCD Type!");
                    //HMI.JoinMessage(IFMRetries.ToString());
                    HMI.EndMessage();

                    RunIFMTest_Camera2();

                    if (TON("TriggerDelay", 1000))
                    {
                        if (CheckIFMTest_Camera2())
                        {
                            HMI.JoinMessage("ReadingIFM");
                            HMI.EndMessage();
                            ClearIFM_Trigger_Camera2();
                            Step = EngineData.Step.RemoveFoil;
                        }
                        else if (TON("State_CheckIFM2", 5000))
                        {

                            if (Convert.ToInt16(GetDeviceConsts("IFM_Retries").ConstValue) > IFMRetries)
                            {
                                Step = EngineData.Step.State_CheckIFM3_Camera2;
                                IFMRetries++;
                            }
                            else
                            {
                                //DBResource.SaveStringResult(DBResource.JobID, "IFM Rubber/Difusor inspection", "", "", DBResource.Result.Devices.Hardware.Move, (int)ResultObject.Device_HW_IFM, ResultUnit.TEXT, false);
                                HMI.JoinMessage("Fail IFM Read");
                                HMI.EndMessage();
                                Step = EngineData.Step.RemovePartandDiscard;

                            }
                        }
                    }

                    #endregion State_CheckIFM
                    break;
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM3_Camera2:
                    #region State_CheckIFM

                    HMI.ReleaseAllMessages();
                    Blink(EngineData.DO.Signal_NOK, true, 500);
                    HMI.JoinMessage("Fail Inspection: Verify the LCD Type!");
                    HMI.JoinMessage(Environment.NewLine);
                    HMI.JoinMessage("Press Button NOK");
                    HMI.EndMessage();

                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        Blink(EngineData.DO.Signal_NOK, false, 500);
                        WriteDO(EngineData.DO.Signal_NOK, true);

                        //Move_Lock(false);
                        Step = EngineData.Step.State_CheckIFM4_Camera2;
                    }

                    #endregion State_CheckIFM
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM4_Camera2:
                    #region State_CheckIFM
                    HMI.ReleaseAllMessages();
                    if (!ReadDI(EngineData.DI.Button_NOK))
                    {
                        ClearAllTimers();
                        ChangeIFMTest_Camera2();
                        //ClearIFM_Trigger_Camera1();
                        Step = EngineData.Step.State_CheckIFM5_Camera2;
                    }

                    #endregion State_CheckIFM
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM5_Camera2:
                    #region State_CheckIFM
                    Blink(EngineData.DO.Signal_NOK, false, 500);
                    HMI.ReleaseAllMessages();
                    HMI.JoinMessage("Press Pedal or Button NOK");
                    HMI.EndMessage();

                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        //DBResource.SaveStringResult(DBResource.JobID, "IFM Rubber/Difusor inspection", "", "", DBResource.Result.Devices.Hardware.Move, (int)ResultObject.Device_HW_IFM, ResultUnit.TEXT, false);
                        WriteDO(EngineData.DO.Signal_NOK, false);
                        ClearIFM_Trigger_Camera2();
                        Step = EngineData.Step.RemovePartandDiscard;
                    }
                    else if (ReadDI(EngineData.DI.Foot_Start))
                    {                       
                        WriteDO(EngineData.DO.Signal_NOK, false);
                        Step = EngineData.Step.State_CheckIFM2_Camera2;
                    }

                    #endregion State_CheckIFM
                    break;

                #endregion IFM

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.RemoveFoil:
                    #region Remove upper red or Blue Flag-foil


                    if (IFM2_Prog == 1) //LCD RED
                    {
                        HMI.JoinAndSendMessage("Remove the red Foil");
                    }
                    else //LCD BLUE
                    {
                        HMI.JoinAndSendMessage("Remove the blue Foil");
                    }
                    
                    if (!ReadDI(EngineData.DI.Det_Foil_L) && !ReadDI(EngineData.DI.Det_Foil_R))
                        Step = EngineData.Step.InsertMask;

                    #endregion
                    break;


                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.InsertMask:
                    #region Insert the MAsk 
                   
                    HMI.JoinMessage("Insert Mask on the Nest!");
                    HMI.JoinAndSendMessage(Environment.NewLine);
                       
                    if (ReadDI(EngineData.DI.Det_Mask))
                    {
                        Step = EngineData.Step.InsertMetalFrame;
                    }
                        
                    #endregion
                    break;
                    
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.InsertMetalFrame:
                    #region Insert the metal-frame over the display on PCB / FootStart
                    
                    ChangeIFMTest_Camera1();

                    if (Convert.ToInt16(GetDeviceConsts("IFM_Enable").ConstValue) == 1)
                    {
                        if (ReadDI(EngineData.DI.Det_MetalFrame))
                        {
                            Step = EngineData.Step.RemoveMask;
                        }
                        else
                        {
                            HMI.JoinMessage("Insert Metal Frame on the Nest!");
                            HMI.JoinAndSendMessage(Environment.NewLine);
                        }
                    }
                    else
                    {

                        if (ReadDI(EngineData.DI.Det_MetalFrame) && ReadDI(EngineData.DI.Foot_Start))
                        {
                            Step = EngineData.Step.RemoveMask;
                        }
                        else
                        {
                            HMI.JoinMessage("Insert Metal Frame on the Nest!");
                            HMI.JoinMessage(Environment.NewLine);
                            HMI.JoinMessage("Press Pedal");
                            HMI.EndMessage();
                        }

                    }
                        #endregion
                        break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.RemoveMask:
                    #region Insert the MAsk 

                    HMI.JoinMessage("Remove Mask from the Nest!");
                    HMI.JoinAndSendMessage(Environment.NewLine);

                    if (!ReadDI(EngineData.DI.Det_Mask))
                    {
                        Step = EngineData.Step.State_CheckIFM1_Camera1;
                    }

                    #endregion
                    break;


                #region IFM_Camera 2 (VERIFY THE PRESENCE OF THE LCD)                  
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM1_Camera1:
                    #region State_CheckIFM
                    
                    IFMRetries = 0;

                    if (Convert.ToInt16(GetDeviceConsts("IFM_Enable").ConstValue) == 0)
                    {
                        Step = EngineData.Step.TabletoWork;
                    }
                    else if (Convert.ToInt16(GetDeviceConsts("IFM_Enable").ConstValue) == 1)
                    {
                        if (ReadDI(EngineData.DI.Foot_Start))
                        {
                            ClearAllTimers();
                            Step = EngineData.Step.State_CheckIFM2_Camera1;
                        }
                        else
                        {
                            HMI.JoinMessage("Press Pedal to Start the Inspection");
                            HMI.EndMessage();

                        }
                    }


                    #endregion State_CheckIFM
                    break;
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM2_Camera1:
                    #region State_CheckIFM

                    HMI.ReleaseAllMessages();
                    HMI.JoinMessage("IFMCycles:Verify the presence of the LCD!");
                    //HMI.JoinMessage(IFMRetries.ToString());
                    HMI.EndMessage();

                    RunIFMTest_Camera1();

                    if (TON("TriggerDelay", 1000))
                    {
                        if (CheckIFMTest_Camera1())
                        {
                            HMI.JoinMessage("Reading Left IFM ");
                            HMI.EndMessage();

                            ClearIFM_Trigger_Camera1();
                            Step = EngineData.Step.TabletoWork;
                        }
                        else if (TON("State_CheckIFM2", 5000))
                        {

                            if (Convert.ToInt16(GetDeviceConsts("IFM_Retries").ConstValue) > IFMRetries)
                            {
                                Step = EngineData.Step.State_CheckIFM3_Camera1;
                                IFMRetries++;
                                ClearIFM_Trigger_Camera1();
                            }
                            else
                            {
                                //DBResource.SaveStringResult(DBResource.JobID, "IFM Rubber/Difusor inspection", "", "", DBResource.Result.Devices.Hardware.Move, (int)ResultObject.Device_HW_IFM, ResultUnit.TEXT, false);
                                HMI.JoinMessage("Fail  Left IFM Read");
                                HMI.EndMessage();
                                Step = EngineData.Step.RemovePartandDiscard;

                            }
                        }
                    }

                    #endregion State_CheckIFM
                    break;
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM3_Camera1:
                    #region State_CheckIFM

                    HMI.ReleaseAllMessages();
                    Blink(EngineData.DO.Signal_NOK, true, 500);
                    HMI.JoinMessage("Fail Inspection: Verify the presence of the LCD!");
                    HMI.JoinMessage(Environment.NewLine);
                    HMI.JoinMessage("Press Button NOK");
                    HMI.EndMessage();

                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        Blink(EngineData.DO.Signal_NOK, false, 500);
                        WriteDO(EngineData.DO.Signal_NOK, true);
                        //Move_Lock(false);
                        Step = EngineData.Step.State_CheckIFM4_Camera1;
                    }

                    #endregion State_CheckIFM
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM4_Camera1:
                    #region State_CheckIFM
                    HMI.ReleaseAllMessages();
                    if (!ReadDI(EngineData.DI.Button_NOK))
                    {
                        ClearAllTimers();
                        ChangeIFMTest_Camera1();
                        Step = EngineData.Step.State_CheckIFM5_Camera1;
                    }

                    #endregion State_CheckIFM
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_CheckIFM5_Camera1:
                    #region State_CheckIFM
                    Blink(EngineData.DO.Signal_NOK, false, 500);
                    HMI.ReleaseAllMessages();
                    HMI.JoinMessage("Press Pedal to continue or Button NOK to cancel!");
                    HMI.EndMessage();
                    

                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        //DBResource.SaveStringResult(DBResource.JobID, "IFM Rubber/Difusor inspection", "", "", DBResource.Result.Devices.Hardware.Move, (int)ResultObject.Device_HW_IFM, ResultUnit.TEXT, false);
                        WriteDO(EngineData.DO.Signal_NOK, false);                       
                        Step = EngineData.Step.RemovePartandDiscard;
                    }
                    else if (ReadDI(EngineData.DI.Foot_Start))
                    {                        
                        WriteDO(EngineData.DO.Signal_NOK, false);
                        Step = EngineData.Step.InsertMetalFrame;
                    }

                    #endregion State_CheckIFM
                    break;

                #endregion IFM
                    
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TabletoWork:
                    #region Move table to Work position without housing
                    HMI.JoinAndSendMessage("Working!");

                    WriteDO(EngineData.DO.Sol_Ionizer_W, false);
                    WriteDO(EngineData.DO.Ionizer_Stop_discharge, true);
                    WriteDO(EngineData.DO.Sol_Cyl_Table_H, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Table_W, true);

                    if (!ReadDI(EngineData.DI.Cyl_Table_H) && ReadDI(EngineData.DI.Cyl_Table_W))
                    {
                        StopTON("ToutTableWorkMov");
                        Step = EngineData.Step.ProySuptoWork;
                    }
                    else if (TON("ToutTableWorkMov", 5000))
                    {
                        StopTON("ToutTableWorkMov");
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        WriteDO(EngineData.DO.Sol_Cyl_Table_H, true);
                        WriteDO(EngineData.DO.Sol_Cyl_Table_W, false);
                        Step = EngineData.Step.FailTabletoWorkMov;
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ProySuptoWork:
                    #region Move superior Proy to Work position

                    WriteDO(EngineData.DO.Sol_Cyl_Enable_Proy_W, true);
                    WriteDO(EngineData.DO.Sol_Cyl_Proy_H, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Proy_W, true);

                    if (ReadDI(EngineData.DI.Cyl_Proy_W) && !ReadDI(EngineData.DI.Cyl_Proy_H))
                    {
                        StopTON("ToutProySupWorkMov");
                        Step = EngineData.Step.ProySupPressTimer;
                    }
                    else if (TON("ToutProySupWorkMov", 5000))
                    {
                        StopTON("ToutProySupWorkMov");
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        WriteDO(EngineData.DO.Sol_Cyl_Proy_W, false);
                        WriteDO(EngineData.DO.Sol_Cyl_Proy_H, true);
                        WriteDO(EngineData.DO.Sol_Cyl_Enable_Proy_W, false);
                        FailProySupWorkMov = true;
                        Step = EngineData.Step.FailMovement;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ProySupPressTimer:
                    #region Superior Proy Press Time

                    if (TON("ProySupPressTime", Convert.ToInt16(GetDeviceConsts("ProySupPressTime").ConstValue)))
                    {
                        StopTON("ProySupPressTime");
                        Step = EngineData.Step.RunCalibrationOrTestPart;
                    }
                    #endregion
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.RunCalibrationOrTestPart:
                    #region Run Calibration
                    
                    if (IsCaliber)
                    {
                        if (RunCalibration(TraceNR, analogResults,1))
                        {
                            PartOK = true;
                            StopTON("WaitProyHome");
                            Step = EngineData.Step.ClinchtoWork;
                        }
                        else
                        {
                            FailCalibCycle = true;
                            Step = EngineData.Step.ClinchtoWork;

                        }
                    }
                    else
                    {
                        if (UsingTraceability)
                        {
                            PartOK = TestWithDigitalInput(DigitalDetect_C1, out digitalResult);

                            if (PartOK)
                            {
                                StopTON("WaitProyHome");
                                Step = EngineData.Step.ClinchtoWork;
                            }
                            else
                            {
                                FailTestAnalogDigital = true;
                                Step = EngineData.Step.PotiFail;
                            }
                        }
                    }
                    
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ClinchtoWork:
                    #region Clinch to Work position

                    WriteDO(EngineData.DO.Sol_Cyl_Clinch_H, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Clinch_W, true);

                    if (ReadDI(EngineData.DI.Cyl_Clinch_W) && !ReadDI(EngineData.DI.Cyl_Clinch_H))
                    {
                        StopTON("ToutProyInfWorkMov");
                        Step = EngineData.Step.ProySuptoHome;
                    }
                    else if (TON("ToutProyInfWorkMov", 5000))
                    {
                        StopTON("ToutProyInfWorkMov");
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        WriteDO(EngineData.DO.Sol_Cyl_Clinch_W, false);
                        WriteDO(EngineData.DO.Sol_Cyl_Clinch_H, true);
                        FailProyInfWorkMov = true;
                        Step = EngineData.Step.FailMovement;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ProySuptoHome:
                    #region Move superior proy to home

                    WriteDO(EngineData.DO.Sol_Cyl_Enable_Proy_W, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Proy_W, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Proy_H, true);
                    WriteDO(EngineData.DO.Sol_Cyl_Clinch_H, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Clinch_W, false);

                    if (!ReadDI(EngineData.DI.Cyl_Proy_W) && ReadDI(EngineData.DI.Cyl_Proy_H))
                    {
                        StopTON("ToutProySupHomekMov");
                        Step = EngineData.Step.TabletoHome;
                    }
                    else if (TON("ToutProySupHomekMov", 5000))
                    {
                        StopTON("ToutProySupHomekMov");
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        FailProySupHomeMov = true;
                        Step = EngineData.Step.FailMovement;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TabletoHome:
                    #region Move table to home

                    WriteDO(EngineData.DO.Sol_Cyl_Table_W, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Table_H, true);
                    WriteDO(EngineData.DO.Sol_Ionizer_W, false);
                    WriteDO(EngineData.DO.Ionizer_Stop_discharge, true);

                    if (ReadDI(EngineData.DI.Cyl_Table_H) && !ReadDI(EngineData.DI.Cyl_Table_W))
                    {
                        StopTON("ToutTableMov");
                        Step = EngineData.Step.DecisionPartOKorNOK;
                    }
                    else if (TON("ToutTableMov", 5000))
                    {
                        StopTON("ToutTableMov");
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        FailProySupHomeMov = true;
                        Step = EngineData.Step.FailMovement;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.DecisionPartOKorNOK:
                    #region Step to check if Part is OK or NOK
                    HMI.ReleaseAllMessages();
                    if (PartOK)
                    {
                        if (IsCaliber)
                            Step = EngineData.Step.UnlockHousing;
                        else
                            Step = EngineData.Step.TraceabilitySaveInfo_LCD;
                    }
                    else
                    {
                        if (IsCaliber)
                        {
                            Blink(EngineData.DO.Signal_NOK, true, 500);
                            HMI.JoinMessage("Fail Calibration Cylce");
                            HMI.JoinMessage(Environment.NewLine);
                            HMI.JoinAndSendMessage("Press Button NOK");
                            if (ReadDI(EngineData.DI.Button_NOK))
                                Step = EngineData.Step.ReleaseNOKAcknowledge;
                        }
                        else
                        {
                            Blink(EngineData.DO.Signal_NOK, true, 500);                            
                            Step = EngineData.Step.DecisionTryAgainOrReject;
                        }
                    }

                    #endregion
                    break;

                ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.DecisionTryAgainOrReject:
                    #region Try Again test or reject part
                    HMI.PanelStatus(EngineData.Screens.ImageSensors);

                    if (Convert.ToInt16(GetDeviceConsts("TestPartRetries").ConstValue) > TestPartRetries)
                    {
                        HMI.JoinAndSendMessage("Press Pedal to continue or Button NOK to cancel!");
                        if (ReadDI(EngineData.DI.Foot_Start))
                        {
                            HMI.PanelStatus(EngineData.Screens.Base);
                            WriteDO(EngineData.DO.Signal_NOK, false);
                            Result = false;
                            TestPartRetries++;
                            Step = EngineData.Step.TabletoWork;
                        }
                        else if (ReadDI(EngineData.DI.Button_NOK))
                        {
                            HMI.PanelStatus(EngineData.Screens.Base);
                            WriteDO(EngineData.DO.Signal_NOK, false);
                            TestPartRetries = 0;
                            if(UsingTraceability)
                            Step = EngineData.Step.TraceabilityJobEndFail;
                            else
                                Step = EngineData.Step.RemovePartandDiscard;
                        }
                    }
                    else
                    {
                        TestPartRetries = 0;
                        if (IsCaliber)
                        {
                            WriteDO(EngineData.DO.Signal_NOK, false);
                            Step = EngineData.Step.UnlockHousing;
                        }
                        else
                        {
                            WriteDO(EngineData.DO.Signal_NOK, false);
                            Step = EngineData.Step.TraceabilityJobEnd;
                        }

                    }
                    #endregion
                    break;

                #region Traceability End
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TraceabilitySaveAssemblyByBatch:
                    #region Traceability save assembly by batch
                    if (UsingTraceability)
                    {
                        if (UsingRFIDTraceability)
                        {
                            //SaveBatchAssemblyError = DBResource.RFID_SaveBatchAssembly(long.Parse(TraceNR), TagTraceNr, WSJobID);

                            if (SaveBatchAssemblyError == 0)
                            {
                                if (PartOK)
                                    Step = EngineData.Step.TraceabilityUpgradeRef;
                                else
                                    Step = EngineData.Step.TraceabilityJobEndFail;
                            }
                            else
                            {
                                Blink(EngineData.DO.Signal_NOK, true, 500);
                                HMI.JoinTraceErrorDescription(SaveBatchAssemblyError);
                                HMI.JoinMessage("Function Error");
                                HMI.JoinMessage(Environment.NewLine);
                                HMI.JoinMessage("Press Button NOK");
                                HMI.EndMessage();
                                if (ReadDI(EngineData.DI.Button_NOK))
                                    Step = EngineData.Step.ErrorTrace;
                            }
                        }
                        else
                        {
                            if (PartOK)
                                Step = EngineData.Step.TraceabilityUpgradeRef;
                            else
                                Step = EngineData.Step.TraceabilityJobEndFail;
                        }
                    }
                    else
                        Step = EngineData.Step.UnlockHousing;
                    #endregion
                    break;                

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TraceabilityUpgradeRef:
                    #region Traceability Upgrade Ref
                    UpgradeRefError = DBResource.Trace_UpgradeRef(long.Parse(TraceNR), EngineData.PrehRef, WSID, 1, WSJobID);

                    if (UpgradeRefError == 0)
                    {
                        Step = EngineData.Step.TraceabilityJobEnd;
                    }
                    else
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinMessage("Function Error");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinTraceErrorDescription(UpgradeRefError);
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Press Button NOK");
                        HMI.EndMessage();
                        if (ReadDI(EngineData.DI.Button_NOK))
                            Step = EngineData.Step.TraceabilityJobEndFail;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TraceabilitySaveInfo_LCD:
                    #region Traceability Save info LCD
                    if (UsingTraceability)

                    {
                        string LCD_Color;
                        if (IFM2_Prog == 1) LCD_Color = "RED";
                        else
                            LCD_Color = "BLUE";

                        SaveInfoError = DBResource.Trace_SaveInfo(DBResource.JobID,"LCD",RefLCD.IDRef.ToString());

                        if (SaveInfoError == 0)
                        {
                            WriteDO(EngineData.DO.Signal_OK, true);
                            Step = EngineData.Step.TraceabilityJobEnd;
                        }
                        else
                        {
                            Blink(EngineData.DO.Signal_NOK, true, 500);
                            HMI.JoinTraceErrorDescription(JobEndError);
                            HMI.JoinMessage(Environment.NewLine);
                            HMI.JoinMessage("Function Error");
                            HMI.JoinMessage(Environment.NewLine);
                            HMI.JoinMessage("Press Button NOK");
                            HMI.EndMessage();
                            if (ReadDI(EngineData.DI.Button_NOK))
                                Step = EngineData.Step.ErrorTrace;
                        }

                    }
                    else
                        Step = EngineData.Step.UnlockHousing;
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TraceabilityJobEnd:
                    #region Traceability JobEnd without error
                    if (UsingTraceability)
                    {
                        JobEndError = DBResource.Trace_JobEnd(0);

                        if (JobEndError == 0)
                        {
                            WriteDO(EngineData.DO.Signal_OK, true);
                            Step = EngineData.Step.UnlockHousing;
                        }
                        else
                        {
                            Blink(EngineData.DO.Signal_NOK, true, 500);
                            HMI.JoinTraceErrorDescription(JobEndError);
                            HMI.JoinMessage(Environment.NewLine);
                            HMI.JoinMessage("Function Error");
                            HMI.JoinMessage(Environment.NewLine);
                            HMI.JoinMessage("Press Button NOK");
                            HMI.EndMessage();
                            if (ReadDI(EngineData.DI.Button_NOK))
                                Step = EngineData.Step.ErrorTrace;
                        }
                    }
                    else
                    {
                        Step = EngineData.Step.UnlockHousing;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TraceabilityJobEndFail:
                    #region Traceability JobEnd with error
                    if (UsingTraceability)
                    {
                        JobEndError = DBResource.Trace_JobEnd(2);

                        if (JobEndError == 0)
                        {
                            WriteDO(EngineData.DO.Signal_NOK, true);
                            Step = EngineData.Step.RemovePartandDiscard;
                        }
                        else
                        {
                            Blink(EngineData.DO.Signal_NOK, true, 500);
                            HMI.JoinTraceErrorDescription(JobEndError);
                            HMI.JoinMessage(Environment.NewLine);
                            HMI.JoinMessage("Function Error");
                            HMI.JoinMessage(Environment.NewLine);
                            HMI.JoinMessage("Press Button NOK");
                            HMI.EndMessage();
                            if (ReadDI(EngineData.DI.Button_NOK))
                                Step = EngineData.Step.ErrorTrace;
                        }
                    }
                    else
                    {
                        Step = EngineData.Step.UnlockHousing;
                    }
                    #endregion
                    break;

                #endregion

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.UnlockHousing:
                    #region Unlock Housing

                    WriteDO(EngineData.DO.Sol_Ionizer_W, false);
                    WriteDO(EngineData.DO.Ionizer_Stop_discharge, true);

                    if (ValidCavity == "Cavidade1")
                        WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);
                    else
                        WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);

                    if (!isPartLockWork(ValidCavity))
                    {
                        StopTON("TimeoutforUnLockMov");
                        Step = EngineData.Step.RemoveHousingComplete;
                    }
                    else if (TON("TimeoutforUnLockMov", 3000))
                    {
                        StopTON("TimeoutforUnLockMov");
                        FailPartunLockMov = true;
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        Step = EngineData.Step.FailMovement;
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.RemoveHousingComplete:
                    #region Remove Complete Housing

                    HMI.JoinAndSendMessage("Part Finished!");
                    WriteDO(EngineData.DO.Signal_OK, true);

                    if (hasnoHousing(ValidCavity))
                    {
                        if (TON("WaitRemovePCB", 2000))
                        {
                            WriteDO(EngineData.DO.Signal_OK, false);
                            Step = EngineData.Step.Step_Initial;
                        }
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ReleaseNOKAcknowledge:
                    #region Release NOK Button
                    HMI.JoinAndSendMessage("Release NOK Button");

                    if (!ReadDI(EngineData.DI.Button_NOK))
                    {
                        if (FailScannerRead)
                        {
                            FailScannerRead = false;
                            Step = EngineData.Step.PressPedalScanner;
                        }

                        if (FailCalibCycle)
                        {
                            FailCalibCycle = false;
                            Blink(EngineData.DO.Signal_NOK, false, 500);
                            WriteDO(EngineData.DO.Signal_NOK, true);
                            Step = EngineData.Step.DecisionTryAgainOrReject;
                        }
                        if (FailTestAnalogDigital)
                        {
                            FailTestAnalogDigital = false;
                            Blink(EngineData.DO.Signal_NOK, false, 500);
                            WriteDO(EngineData.DO.Signal_NOK, true);
                            Step = EngineData.Step.ProySuptoHome;
                        }
                    }

                    #endregion
                    break;      

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.RemovePart:
                    #region Remove part

                    Blink(EngineData.DO.Signal_NOK, false, 500);
                    Step = EngineData.Step.Step_Initial;

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.PotiFail:
                    #region Fail PotiSensors Test

                    Blink(EngineData.DO.Signal_NOK, true, 500);

                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        Step = EngineData.Step.ReleaseNOKAcknowledge;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.FailMovement:
                    #region fail movement

                    if (FailPartLockWorkMov)
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinAndSendMessage("Fail Part Lock Movement");

                        if (ReadDI(EngineData.DI.Button_NOK))
                            Step = EngineData.Step.ReleaseButtonNOKFail;
                    }
                    if (FailProySupWorkMov)
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinAndSendMessage("Fail Proy Sup. Movement");

                        if (ReadDI(EngineData.DI.Button_NOK))
                            Step = EngineData.Step.ReleaseButtonNOKFail;
                    }
                    if (FailProyInfWorkMov)
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinAndSendMessage("Fail Proy Inf. Movement");

                        if (ReadDI(EngineData.DI.Button_NOK))
                            Step = EngineData.Step.ReleaseButtonNOKFail;
                    }
                    if (FailProyInfHomeMov)
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinAndSendMessage("Fail Proy Inf. Movement");

                        if (ReadDI(EngineData.DI.Button_NOK))
                            Step = EngineData.Step.ReleaseButtonNOKFail;
                    }
                    if (FailProySupHomeMov)
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinAndSendMessage("Fail Proy Sup. Movement");

                        if (ReadDI(EngineData.DI.Button_NOK))
                            Step = EngineData.Step.ReleaseButtonNOKFail;
                    }
                    if (FailTabletoHomeMov)
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinAndSendMessage("Fail Table Movement");

                        if (ReadDI(EngineData.DI.Button_NOK))
                            Step = EngineData.Step.ReleaseButtonNOKFail;
                    }
                    if (FailIonizer)
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinAndSendMessage("Fail Ionizer..");

                        if (ReadDI(EngineData.DI.Button_NOK))
                            Step = EngineData.Step.ReleaseButtonNOKFail;
                    }



                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ReleaseButtonNOKFail:
                    #region wait for button release when movement fail

                    FailPartLockWorkMov = false;
                    FailProySupWorkMov = false;
                    FailProyInfWorkMov = false;
                    FailTabletoHomeMov = false;
                    FailProySupHomeMov = false;
                    FailProyInfHomeMov = false;
                    FailIonizer = false;

                    if (!ReadDI(EngineData.DI.Button_NOK))
                    {
                        WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);
                        Step = EngineData.Step.Fail;
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.Fail:
                    #region Press button stop and homecycle

                    HMI.JoinAndSendMessage("Press Stop");

                    #endregion
                    break;
                    

                #region Fail movement table to work
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.FailTabletoWorkMov:
                    #region fail movement table to work

                    //Fail msg
                    HMI.JoinAndSendMessage("Fail Movement Table To Work Movement");

                    Blink(EngineData.DO.Signal_NOK, true, 500);
                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        Step = EngineData.Step.ReleaseButtonNOKFailTableMov;
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ReleaseButtonNOKFailTableMov:
                    #region wait for button release when table movement fail

                    if (!ReadDI(EngineData.DI.Button_NOK))
                    {
                        Step = EngineData.Step.TableMovFailMSGContinueorCancel;
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TableMovFailMSGContinueorCancel:
                    #region Continue or cancel after fail table movement

                    //Msg for continue or cancel
                    HMI.JoinAndSendMessage("Continue or Cancel");

                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        Step = EngineData.Step.RemovePartandDiscard;
                    }
                    else if (ReadDI(EngineData.DI.Foot_Start))
                    {
                        Step = EngineData.Step.TabletoWork;
                        WriteDO(EngineData.DO.Signal_NOK, false);
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.RemovePartandDiscard:
                    #region Remove part and discard

                    WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);
                    WriteDO(EngineData.DO.Sol_Ionizer_W, false);
                    WriteDO(EngineData.DO.Ionizer_Stop_discharge, true);
                    WriteDO(EngineData.DO.Signal_NOK, true);

                    if (!isPartLockWork(ValidCavity))
                    {
                        StopTON("TimeoutforUnLockMov");
                        //Msg to remove part
                        HMI.JoinAndSendMessage("Remove To Rejection Box");

                        if (!hasHousing(ValidCavity) && (ReadDI(EngineData.DI.Rejection_Box)))
                        {
                            Step = EngineData.Step.Step_Initial;
                        }
                                               
                    }
                    else if (TON("TimeoutforUnLockMov", 3000))
                    {
                        StopTON("TimeoutforUnLockMov");
                        FailPartunLockMov = true;
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        Step = EngineData.Step.FailMovement;
                    }
                                      

                    #endregion
                    break;

                #endregion

                #region Fail movement table to work without external
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.FailTabletoWorkMovNoExt:
                    #region fail movement table to work

                    //Fail msg
                    HMI.JoinAndSendMessage("Fail Movement Table To Work Movement");

                    Blink(EngineData.DO.Signal_NOK, true, 500);
                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        Step = EngineData.Step.ReleaseButtonNOKFailTableMovNoExt;
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ReleaseButtonNOKFailTableMovNoExt:
                    #region wait for button release when table movement fail

                    if (!ReadDI(EngineData.DI.Button_NOK))
                    {
                        Step = EngineData.Step.TableMovFailMSGContinueorCancelNoExt;
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.TableMovFailMSGContinueorCancelNoExt:
                    #region Continue or cancel after fail table movement

                    //Msg for continue or cancel
                    HMI.JoinAndSendMessage("Continue or Cancel");

                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        Step = EngineData.Step.RemovePartandDiscardNoExt;
                    }
                    else if (ReadDI(EngineData.DI.Foot_Start))
                    {
                        Step = EngineData.Step.TabletoWorkNoExt;
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.RemovePartandDiscardNoExt:
                    #region Remove part and discard

                    WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);

                    //Msg to remove part 
                    HMI.JoinAndSendMessage("Remove To Rejection Box");

                    if (!hasHousing(ValidCavity))
                    {
                        Step = EngineData.Step.Step_Initial;
                    }

                    #endregion
                    break;

                    #endregion
            }

           
        }

        public bool hasHousing(string cav)
        {
            if (cav == "Cavidade1")
                return ReadDI(EngineData.DI.Det_PCB_L) && ReadDI(EngineData.DI.Det_PCB_R);
            else if (cav == "Cavidade1")
                return ReadDI(EngineData.DI.Det_PCB_L) && ReadDI(EngineData.DI.Det_PCB_R);
            return false;
        }

        public bool hasnoHousing(string cav)
        {
            if (cav == "Cavidade1")
                return !ReadDI(EngineData.DI.Det_PCB_L) && !ReadDI(EngineData.DI.Det_PCB_R);
            else if (cav == "Cavidade1")
                return !ReadDI(EngineData.DI.Det_PCB_L) && !ReadDI(EngineData.DI.Det_PCB_R);
            return false;
        }

        public bool isPartLockWork(string cav)
        {
            if (cav == "Cavidade1")
                return (ReadDI(EngineData.DI.Cyl_Lock_PCB_L_W) && !ReadDI(EngineData.DI.Cyl_Lock_PCB_L_H) &&
                    !ReadDI(EngineData.DI.Cyl_Lock_PCB_R_H) && ReadDI(EngineData.DI.Cyl_Lock_PCB_R_W));
            else if (cav == "Cavidade1")
                return (!ReadDI(EngineData.DI.Cyl_Lock_PCB_L_W) && !ReadDI(EngineData.DI.Cyl_Lock_PCB_L_H) &&
                    !ReadDI(EngineData.DI.Cyl_Lock_PCB_R_H) && ReadDI(EngineData.DI.Cyl_Lock_PCB_R_W));

            return false;
        }


        public void ClearIFM_Trigger()
        {
            WriteDO(EngineData.DO.Camera1_Trigger, false);
            WriteDO(EngineData.DO.Camera1_In1, false);
            WriteDO(EngineData.DO.Camera1_In2, false);
            WriteDO(EngineData.DO.Camera2_Trigger, false);
            WriteDO(EngineData.DO.Camera2_In1, false);
            WriteDO(EngineData.DO.Camera2_In2, false);
        }

        public void ClearIFM_Trigger_Camera1()
        {
            WriteDO(EngineData.DO.Camera1_Trigger, false);
            WriteDO(EngineData.DO.Camera1_In1, false);
            WriteDO(EngineData.DO.Camera1_In2, false);
        }
        public void ClearIFM_Trigger_Camera2()
        {
            WriteDO(EngineData.DO.Camera2_Trigger, false);
            WriteDO(EngineData.DO.Camera2_In1, false);
            WriteDO(EngineData.DO.Camera2_In2, false);
        }
        public bool CheckIFMTest()
        {
            if (ReadDI(EngineData.DI.Camera1_Object) && ReadDI(EngineData.DI.Camera1_Ready) && ReadDI(EngineData.DI.Camera2_Object) && ReadDI(EngineData.DI.Camera2_Ready))
            {
                WriteDO(EngineData.DO.Camera1_Trigger, false);
                WriteDO(EngineData.DO.Camera1_In1, false);
                WriteDO(EngineData.DO.Camera1_In2, false);
                WriteDO(EngineData.DO.Camera2_Trigger, false);
                WriteDO(EngineData.DO.Camera2_In1, false);
                WriteDO(EngineData.DO.Camera2_In2, false);
                return true;
            }
            else
                return false;
        }

        public bool CheckIFMTest_Camera1()
        {
            if (ReadDI(EngineData.DI.Camera1_Object) && ReadDI(EngineData.DI.Camera1_Ready))
            {
                WriteDO(EngineData.DO.Camera1_Trigger, false);
                WriteDO(EngineData.DO.Camera1_In1, false);
                WriteDO(EngineData.DO.Camera1_In2, false);

                return true;
            }
            else
                return false;
        }

        public bool CheckIFMTest_Camera2()
        {
            if (ReadDI(EngineData.DI.Camera2_Object) && ReadDI(EngineData.DI.Camera2_Ready))
            {
                WriteDO(EngineData.DO.Camera2_Trigger, false);
                WriteDO(EngineData.DO.Camera2_In1, false);
                WriteDO(EngineData.DO.Camera2_In2, false);

                return true;
            }
            else
                return false;
        }

        public void ChangeIFMTest()
        {
            WriteDO(EngineData.DO.Camera1_Trigger, false);
            WriteDO(EngineData.DO.Camera1_In1, false);
            WriteDO(EngineData.DO.Camera1_In2, false);
            WriteDO(EngineData.DO.Camera1_Trigger, false);
            WriteDO(EngineData.DO.Camera1_In1, false);
            WriteDO(EngineData.DO.Camera1_In2, false);

        }

        public void ChangeIFMTest_Camera1()
        {           
            WriteDO(EngineData.DO.Camera1_Trigger, false);
            WriteDO(EngineData.DO.Camera1_In1, true);
            WriteDO(EngineData.DO.Camera1_In2, false);
            
        }

        public void ChangeIFMTest_Camera2()
        {
            //var measure = DBResource?.MeasureLimits.Find(m => m.MeasureName == "Prog_IFM_Right");
            //int IFM_Prog = Convert.ToInt16(measure.MinValue);

            if (IFM2_Prog == 1) //LCD RED
            {
                WriteDO(EngineData.DO.Camera2_Trigger, false);
                WriteDO(EngineData.DO.Camera2_In1, true);
                WriteDO(EngineData.DO.Camera2_In2, false);
            }
            else //LCD BLUE
            {
                WriteDO(EngineData.DO.Camera2_Trigger, false);
                WriteDO(EngineData.DO.Camera2_In1, false);
                WriteDO(EngineData.DO.Camera2_In2, true);
            }

        }

        public void RunIFMTest()
        {
            WriteDO(EngineData.DO.Camera1_In2, false);
            WriteDO(EngineData.DO.Camera1_In1, false);
            WriteDO(EngineData.DO.Camera1_Trigger, true);
            WriteDO(EngineData.DO.Camera2_In2, false);
            WriteDO(EngineData.DO.Camera2_In1, false);
            WriteDO(EngineData.DO.Camera2_Trigger, true);

        }
        public void RunIFMTest_Camera1()
        {
            
            WriteDO(EngineData.DO.Camera1_Trigger, true);

        }

        public void RunIFMTest_Camera2()
        {
            
            WriteDO(EngineData.DO.Camera2_Trigger, true);

        }
        public bool CheckCaliber(long caliber)
        {
            //return DBResource.CheckCaliber(caliber);
            return false;
        }

        public void ReadCaliber(long tracenr)
        {
            try
            {
                // EngineData.CalibrationMeasurementList = DBResource.Calibration_GetMeasurementList(tracenr, WSID, SubWorkCenterID);
            }
            catch (Exception ex)
            {
                throw new Exception("Calibration_GetMeasurementList", ex);
            }
        }

        public static double caliberLimits(string parameterName, string limitType)
        {

            DataRow[] drs = EngineData.CalibrationMeasurementList.Tables[0].Select("Name='" + parameterName + "'");
            DataRow dr = drs[0];

            if (limitType.Equals("min"))
                return double.Parse(dr["NominalValue"].ToString()) - double.Parse(dr["Tolerance"].ToString());
            else if (limitType.Equals("max"))
                return double.Parse(dr["NominalValue"].ToString()) + double.Parse(dr["Tolerance"].ToString());
            else return -1;
        }

        public static double NominalValue(string name)
        {
            DataRow[] drs = EngineData.CalibrationMeasurementList.Tables[0].Select("Name='" + name + "'");
            DataRow dr = drs[0];

            return double.Parse(dr["NominalValue"].ToString());
        }
        public static int DefaultValue(string name)
        {
            DataRow[] drs = EngineData.CalibrationMeasurementList.Tables[0].Select("Name='" + name + "'");
            DataRow dr = drs[0];

            return int.Parse(dr["ID_DefaultValue"].ToString());

        }

        private bool SaveCalibrationResult(string traceNr, int WSID, byte SubWS, string name, double measuredValue, double calibrationError)
        {
            DataRow[] drs = EngineData.CalibrationMeasurementList.Tables[0].Select("Name='" + name + "'");
            DataRow dr = drs[0];

            return DBResource.Calibration_SaveMeasurement(Int64.Parse(traceNr), WSID, SubWS, int.Parse(dr["ID_DefaultValue"].ToString()), measuredValue, double.Parse(dr["NominalValue"].ToString()), calibrationError, name);
            
        }
               

    }

}