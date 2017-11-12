using PPDBAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Preh
{
    public class HomeCycle : Cycle
    {
        private bool FailProyInfMov = false;
        private bool FailProySupMov = false;
        private bool FailTableMov = false;
        private bool FailPartLockMov = false;
        private bool FailPartLockMov_Sample = false;
        private bool FailIonizer = false;        
        private bool FailScannerRead = false;
        private bool ErrorTraceFail = false;
        private string ValidCavity, TraceNR, PrehReference;

        public static bool BlinkState = false;
        private int ScannerRetries;

        bool leftRightOK = false;
        bool frontBackOK = false;
        int currPos = 0;

        EngineData.AI[] analogResults;
        EngineData.DI[] digitalResults;

        public HomeCycle(PPTraceStation db, string prehref, int wsID, Dictionary<string, int> subWCIDs, bool usesTraceability, int cycleid, bool hashomecycle, bool usesRFIDTraceability) :
            base(db, prehref, wsID, subWCIDs, usesTraceability, cycleid, hashomecycle, usesRFIDTraceability)
        { }
        public void RunCycle()
        {

            //HMI.StepChange(Step);

            switch (Step)
            {
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.Zero:
                    #region Initial Config
                    ValidCavity = "";
                    ResetAll();
                    Step = EngineData.Step.Step_Initial;

                    HMI.JoinMessage("Going to Home Position");
                    HMI.JoinMessage(Environment.NewLine);
                    HMI.EndMessage();


                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.Step_Initial:
                    #region State Initial
                    WriteDO(EngineData.DO.Relay_Machine_Light, false);
                    WriteDO(EngineData.DO.Sol_Ionizer_W, false);
                    WriteDO(EngineData.DO.Ionizer_Stop_discharge, true);
                    WriteDO(EngineData.DO.Signal_NOK, false);
                    WriteDO(EngineData.DO.Signal_OK, false);
                    Blink(EngineData.DO.Signal_NOK, false, 500);


                    if (ReadDI(EngineData.DI.Ionizer_NoError) && !ReadDI(EngineData.DI.Ionizer_Maintenance))
                    {
                        Step = EngineData.Step.MoveProySup;
                    }
                    else
                    {
                        FailIonizer = true;                       
                        Step = EngineData.Step.FailMovement;
                    }



                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.MoveProySup:
                    #region Move Proy Sup to home position

                    WriteDO(EngineData.DO.Sol_Cyl_Proy_W, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Proy_H, true);
                    WriteDO(EngineData.DO.Sol_Cyl_Enable_Proy_W, false);

                    if (ReadDI(EngineData.DI.Cyl_Proy_H) && !ReadDI(EngineData.DI.Cyl_Proy_W))
                    {
                        StopTON("TimeoutforProySupMov");
                        Step = EngineData.Step.MoveTable;
                    }
                    else if (TON("TimeoutforProySupMov", 2000))
                    {
                        FailProySupMov = true;
                        StopTON("TimeoutforProySupMov");
                        Step = EngineData.Step.FailMovement;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.MoveTable:
                    #region Move Table to home position

                    WriteDO(EngineData.DO.Sol_Cyl_Table_W, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Table_H, true);

                    if (ReadDI(EngineData.DI.Cyl_Table_H) && !ReadDI(EngineData.DI.Cyl_Table_W))
                    {
                        StopTON("TimeoutforTableMov");
                        Step = EngineData.Step.MoveClinch;
                    }
                    else if (TON("TimeoutforTableMov", 4000))
                    {
                        FailTableMov = true;
                        StopTON("TimeoutforTableMov");
                        Step = EngineData.Step.FailMovement;
                    }
                    #endregion
                    break;


                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.MoveClinch:
                    #region Move Clinch to home position

                    WriteDO(EngineData.DO.Sol_Cyl_Clinch_W, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Clinch_H, true);

                    if (ReadDI(EngineData.DI.Cyl_Clinch_H) && !ReadDI(EngineData.DI.Cyl_Clinch_W))
                    {
                        StopTON("TimeoutforClinchMov");
                        Step = EngineData.Step.MovePartLock;
                    }
                    else if (TON("TimeoutforTableMov", 4000))
                    {
                        FailTableMov = true;
                        StopTON("TimeoutforClinchMov");
                        Step = EngineData.Step.FailMovement;
                    }
                    #endregion
                    break;


                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.MovePartLock:
                    #region Move part lock Cavidade1 to home position

                    WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);

                    if (isPartLockHome("Cavidade1") && isPartLockHome("MAC"))
                    {
                        StopTON("TimeoutforPartLockMov");
                        Step = EngineData.Step.RemoveFoil;
                    }
                    else if (TON("TimeoutforPartLockMov", 2000))
                    {
                        FailPartLockMov = true;
                        StopTON("TimeoutforPartLockMov");
                        Step = EngineData.Step.FailMovement;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.RemoveFoil:
                    #region Remove foil from the nest

                    if (hasPartsonNest("Cavidade1", "Foil"))
                        HMI.JoinAndSendMessage("Remove Foil");
                    else
                        Step = EngineData.Step.RemovePCB;

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.RemovePCB:
                    #region Remove PCB from the nest

                    if (hasPartsonNest("Cavidade1", "PCB"))
                        HMI.JoinAndSendMessage("Remove PCB");
                    else
                        Step = EngineData.Step.CycleSampleOrHomeEnd;

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.CycleSampleOrHomeEnd:
                    #region Decide if it is need to run the sample cycle to validate machine

                    if (EngineData.FirstCycleHome || Convert.ToInt16(GetDeviceConsts("Trigger_SampleCycle").ConstValue) == 1)
                    {
                        //EngineData.Params_From_DataBase.Trigger_SampleCycle = 0; //need to save this parameter on DB
                        //EngineData.Params_From_DataBase.Trigger_SampleCycle = 0;
                        EngineData.FirstCycleHome = false;
                        Step = EngineData.Step.State_Vacuum_Init;
                        //Step = EngineData.Step.InsertSample_Cavidade1;
                    }
                    else
                        Step = EngineData.Step.State_Vacuum_Init;

                    #endregion
                    break;

                #region Sample Cycle
                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.InsertSample_Cavidade1:
                    #region Insert Sample part on nest
                    HMI.ReleaseAllMessages();
                    HMI.JoinAndSendMessage("Insert Sample Part Cavidade1");
                    if (hasPartsonNest("Cavidade1", "Housing"))
                    {
                        ValidCavity = "Cavidade1";
                        EngineData.AI[] analog = {

                            };
                        EngineData.DI[] digital = {
                            EngineData.DI.Det_PCB_L,
                            EngineData.DI.Det_PCB_L
                        };
                        analogResults = analog;
                        digitalResults = digital;
                        WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, true);
                        Step = EngineData.Step.MoveLockSample;
                    }
                    else if (hasPartsonNest("MAC", "PCB"))
                    {
                        HMI.JoinAndSendMessage("First Sample Cavidade1");
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.InsertSample_MAC:
                    #region Insert Sample part on nest
                    HMI.JoinAndSendMessage("Insert Sample Part MAC");
                    if (hasPartsonNest("MAC", "Housing"))
                    {
                        ValidCavity = "MAC";
                        EngineData.AI[] analog = {

                            };
                        EngineData.DI[] digital = {
                            EngineData.DI.Det_PCB_L,
                            EngineData.DI.Det_PCB_R
                        };
                        analogResults = analog;
                        digitalResults = digital;
                        WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, true);
                        Step = EngineData.Step.MoveLockSample;
                    }
                    else if (hasPartsonNest("Cavidade1", "Housing"))
                    {
                        HMI.JoinAndSendMessage("Already Inspect Cavidade1 Part");
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.MoveLockSample:
                    #region Lock Part Cavidade1
                    if (ValidCavity == "Cavidade1")
                    {
                        WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, true);

                        if (!isPartLockHome(ValidCavity))
                        {
                            StopTON("TimeOutLockMov");
                            Step = EngineData.Step.ReadSample;
                        }
                        else if (TON("TimeOutLockMov", 2000))
                        {
                            FailPartLockMov_Sample = true;
                            StopTON("TimeOutLockMov");
                            Step = EngineData.Step.FailMovement;
                        }
                    }
                    else
                    {
                        WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, true);

                        if (!isPartLockHome(ValidCavity))
                        {
                            StopTON("TimeOutLockMov");
                            Step = EngineData.Step.ReadSample;
                        }
                        else if (TON("TimeOutLockMov", 2000))
                        {
                            FailPartLockMov_Sample = true;
                            StopTON("TimeOutLockMov");
                            Step = EngineData.Step.FailMovement;
                        }
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ReadSample:
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
                        Step = EngineData.Step.GetRefSample;
                    }
                    else if (TON("TimeoutforScanner", 5000))
                    {
                        StopTON("TimeoutforScanner");
                        Hardware.StopScanner(ValidCavity);
                        Hardware.ReleaseHardwareOnThisStep();
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        Step = EngineData.Step.ScannerFail_Sample;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ScannerFail_Sample:
                    #region Timeout for scanner read
                    HMI.JoinMessage("Fail Scanner Read");
                    HMI.JoinMessage(Environment.NewLine);
                    HMI.JoinMessage("Press Button NOK");
                    HMI.EndMessage();

                    if (ReadDI(EngineData.DI.Button_NOK))
                    {
                        FailScannerRead = true;
                        Step = EngineData.Step.ReleaseNOKAcknowledge_Sample;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ReleaseNOKAcknowledge_Sample:
                    #region Release NOK Button
                    HMI.JoinAndSendMessage("Release NOK Button");

                    if (!ReadDI(EngineData.DI.Button_NOK))
                    {
                        FailScannerRead = false;
                        Step = EngineData.Step.PressPedalScanner_Sample;
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.PressPedalScanner_Sample:
                    #region Press Pedal to Retry Scanner Read

                    Blink(EngineData.DO.Signal_NOK, false, 500);
                    WriteDO(EngineData.DO.Signal_NOK, true);
                    HMI.JoinMessage("PressPedal or Button NOK");
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
                            WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);
                            if (!hasPartsonNest(ValidCavity, "Housing"))
                            {
                                CycleDone = false;
                                CycleFail = true;
                                Step = EngineData.Step.Step_Initial;
                            }

                        }
                    }
                    else
                    {
                        ScannerRetries = 0;
                        HMI.JoinAndSendMessage("Remove Part Nest");
                        WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);
                        WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);
                        if (!!hasPartsonNest(ValidCavity, "Housing"))
                        {
                            CycleDone = false;
                            CycleFail = true;
                            Step = EngineData.Step.Step_Initial;
                        }
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.GetRefSample:
                    //#region Get Ref for the sample

                    //PrehReference = DBResource.GetPrehRefFromTraceNr(TraceNR);
                    //if (!string.IsNullOrEmpty(PrehReference))
                    //{
                    //    HMI.ChangeReference(PrehReference);
                    //    Step = EngineData.Step.Sample_MoveTableWork;
                    //}
                    //else
                    //{
                    //    Blink(EngineData.DO.Signal_NOK, true, 500);
                    //    HMI.JoinMessage("Function Error");
                    //    HMI.JoinMessage(Environment.NewLine);
                    //    HMI.JoinMessage("Press Button NOK");
                    //    HMI.EndMessage();
                    //    if (ReadDI(EngineData.DI.Button_NOK))
                    //        Step = EngineData.Step.ErrorTrace;
                    //}

                    //#endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.ErrorTrace:
                    #region Error Trace
                    HMI.JoinAndSendMessage("Release NOK Button");

                    if (!ReadDI(EngineData.DI.Button_NOK))
                    {
                        Blink(EngineData.DO.Signal_NOK, false, 500);
                        ErrorTraceFail = true;
                        Step = EngineData.Step.Sample_MovePartLockHome;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.Sample_MoveTableWork:
                    #region Move Table to work position

                    WriteDO(EngineData.DO.Sol_Cyl_Table_H, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Table_W, true);

                    if (!ReadDI(EngineData.DI.Cyl_Table_H) && ReadDI(EngineData.DI.Cyl_Table_W))
                    {
                        StopTON("TimeoutforTableMov");
                        Step = EngineData.Step.Sample_MoveProySupWork;
                    }
                    else if (TON("TimeoutforTableMov", 4000))
                    {
                        FailTableMov = true;
                        StopTON("TimeoutforTableMov");
                        Step = EngineData.Step.FailMovement;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.Sample_MoveProySupWork:
                    #region Move Proy Sup to work position

                    WriteDO(EngineData.DO.Sol_Cyl_Enable_Proy_W, true);
                    WriteDO(EngineData.DO.Sol_Cyl_Proy_W, true);
                    WriteDO(EngineData.DO.Sol_Cyl_Proy_H, false);

                    if (!ReadDI(EngineData.DI.Cyl_Proy_H) && ReadDI(EngineData.DI.Cyl_Proy_W))
                    {
                        StopTON("TimeoutforProySupMov");
                        Step = EngineData.Step.RunSample;
                    }
                    else if (TON("TimeoutforProySupMov", 2000))
                    {
                        FailProySupMov = true;
                        StopTON("TimeoutforProySupMov");
                        Step = EngineData.Step.FailMovement;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.RunSample:
                    //#region Run Sample avaliation

                    //    if (ValidCavity == "Cavidade1")
                    //    {
                    //        if (RunSampleVerification(EngineData.Params_From_DataBase.sampleAnalogResult, analogResults, ResultUnit.V, EngineData.Params_From_DataBase.sampleDigitalResult, digitalResults, EngineData.digitalWantedResult_Cavidade1))
                    //        {
                    //            WriteDO(EngineData.DO.Signal_OK, true);
                    //            Step = EngineData.Step.Sample_MoveProyInfHome;
                    //        }
                    //        else
                    //        {
                    //            Blink(EngineData.DO.Signal_NOK, true, 500);
                    //            HMI.JoinMessage("Fail Sample Cycle");
                    //            HMI.JoinMessage(Environment.NewLine);
                    //            HMI.JoinMessage("PressPedal or Button NOK");
                    //            HMI.EndMessage();
                    //        }
                    //    }
                    //    else
                    //    {
                    //        if (RunSampleVerification(EngineData.Params_From_DataBase.sampleAnalogResult, analogResults, ResultUnit.V, EngineData.Params_From_DataBase.sampleDigitalResult, digitalResults, EngineData.digitalWantedResult_MAC))
                    //        {
                    //            WriteDO(EngineData.DO.Signal_OK, true);
                    //            Step = EngineData.Step.Sample_MoveProySupHome;
                    //        }
                    //        else
                    //        {
                    //            Blink(EngineData.DO.Signal_NOK, true, 500);
                    //            HMI.JoinMessage("Fail Sample Cycle");
                    //            HMI.JoinMessage(Environment.NewLine);
                    //            HMI.JoinMessage("PressPedal or Button NOK");
                    //            HMI.EndMessage();
                    //        }
                    //    }
                    //    #endregion
                    //    break;

                    //// ////////////////////////////////////////////////////////////////////////////////////////////////
                    //case EngineData.Step.Sample_MoveProySupHome:
                    //    #region Move Proy Sup to home position

                    //    WriteDO(EngineData.DO.Sol_Cyl_Enable_Proy_W, false);
                    //    WriteDO(EngineData.DO.Sol_Cyl_Proy_W, false);
                    //    WriteDO(EngineData.DO.Sol_Cyl_Proy_H, true);

                    //    if (ReadDI(EngineData.DI.Cyl_Proy_H) && !ReadDI(EngineData.DI.Cyl_Proy_W))
                    //    {
                    //        StopTON("TimeoutforProySupMov");
                    //        Step = EngineData.Step.Sample_MoveTableHome;
                    //    }
                    //    else if (TON("TimeoutforProySupMov", 2000))
                    //    {
                    //        FailProySupMov = true;
                    //        StopTON("TimeoutforProySupMov");
                    //        Step = EngineData.Step.FailMovement;
                    //    }
                    //    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.Sample_MoveTableHome:
                    #region Move Table to home position

                    WriteDO(EngineData.DO.Sol_Cyl_Table_W, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Table_H, true);

                    if (ReadDI(EngineData.DI.Cyl_Table_H) && !ReadDI(EngineData.DI.Cyl_Table_W))
                    {
                        StopTON("TimeoutforTableMov");
                        Step = EngineData.Step.MovePartLock;
                    }
                    else if (TON("TimeoutforTableMov", 4000))
                    {
                        FailTableMov = true;
                        StopTON("TimeoutforTableMov");
                        Step = EngineData.Step.FailMovement;
                    }
                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.Sample_MovePartLockHome:
                    #region Move part Lock to Home

                    WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);
                    WriteDO(EngineData.DO.Sol_Cyl_Lock_PCB_W, false);

                    if (isPartLockHome("Cavidade1"))
                    {
                        StopTON("TimeoutforPartLockMov");
                        if (ErrorTraceFail)
                            Step = EngineData.Step.InsertSample_Cavidade1;
                        else
                            Step = EngineData.Step.RunSecondSampleOrEnd;
                    }
                    else if (TON("TimeoutforPartLockMov", 2000))
                    {
                        FailPartLockMov = true;
                        StopTON("TimeoutforPartLockMov");
                        Step = EngineData.Step.FailMovement;
                    }

                    #endregion
                    break;

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.RunSecondSampleOrEnd:
                    #region Decide if it has to run the second part or end with vaccum
                    if (ValidCavity == "Cavidade1")
                    {
                        HMI.JoinAndSendMessage("Remove Sample Unit");
                        if (!hasPartsonNest(ValidCavity, "PCB"))
                        {
                            ValidCavity = "";
                            Step = EngineData.Step.InsertSample_MAC;
                        }
                    }
                    else
                    {
                        HMI.JoinAndSendMessage("Remove Sample Unit");
                        if (!hasPartsonNest(ValidCavity, "PCB"))
                        {
                            ValidCavity = "";
                            Step = EngineData.Step.State_Vacuum_Init;
                        }
                    }
                    #endregion
                    break;

                #endregion

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_Vacuum_Init:
                    #region State_Vacuum_Init

                    Vacuum(true);
                    HMI.PanelStatus(EngineData.Screens.ImageVacuum);

                    if (TON("VacuumMinTime", Convert.ToInt16(GetDeviceConsts("VacuumMinTime").ConstValue)) && ReadDI(EngineData.DI.Foot_Start))
                    {
                        Vacuum(false);
                        HMI.PanelStatus(EngineData.Screens.Base);
                        Step = EngineData.Step.State_Vacuum_Exec;
                    }
                    //Vacuum(false);
                    //HMI.PanelStatus(EngineData.Screens.Base);
                    //Step = EngineData.Step.State_Vacuum_Exec;



                    break;
                #endregion State_Vacuum_Init

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.State_Vacuum_Exec:
                    #region State_Vacuum_Exec

                    HMI.JoinAndSendMessage("Home Cycle Done");
                    CycleDone = true;

                    break;
                #endregion State_Vacuum_Exec

                // ////////////////////////////////////////////////////////////////////////////////////////////////
                case EngineData.Step.FailMovement:
                    #region Fail movement
                    if (FailProyInfMov)
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinMessage("Fail Proy Inf. Movement");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Press Button NOK");
                        HMI.EndMessage();
                        if (ReadDI(EngineData.DI.Button_NOK))
                        {
                            FailProyInfMov = false;
                            CycleFail = true;
                            CycleDone = false;
                            Blink(EngineData.DO.Signal_NOK, false, 500);
                            Step = EngineData.Step.Zero;
                        }
                    }
                    else if (FailProySupMov)
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinMessage("Fail Proy Sup Movement");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Press Button NOK");
                        HMI.EndMessage();
                        if (ReadDI(EngineData.DI.Button_NOK))
                        {
                            FailProySupMov = false;
                            CycleDone = false;
                            CycleFail = true;
                            Blink(EngineData.DO.Signal_NOK, false, 500);
                            Step = EngineData.Step.Zero;
                        }
                    }
                    else if (FailTableMov)
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinMessage("Fail Table Movement");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Press Button NOK");
                        HMI.EndMessage();
                        if (ReadDI(EngineData.DI.Button_NOK))
                        {
                            FailTableMov = false;
                            CycleFail = true;
                            CycleDone = false;
                            Blink(EngineData.DO.Signal_NOK, false, 500);
                            Step = EngineData.Step.Zero;
                        }
                    }
                    else if (FailPartLockMov)
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinMessage("Fail Part Lock Movement");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Press Button NOK");
                        HMI.EndMessage();
                        if (ReadDI(EngineData.DI.Button_NOK))
                        {
                            FailPartLockMov = false;
                            CycleFail = true;
                            CycleDone = false;
                            Blink(EngineData.DO.Signal_NOK, false, 500);
                            Step = EngineData.Step.Zero;
                        }
                    }

                    else if (FailIonizer)
                    {
                        Blink(EngineData.DO.Signal_NOK, true, 500);
                        HMI.JoinMessage("Fail Ionizer");
                        HMI.JoinMessage(Environment.NewLine);
                        HMI.JoinMessage("Press Button NOK");
                        HMI.EndMessage();
                        if (ReadDI(EngineData.DI.Button_NOK))
                        {
                            FailIonizer = false;
                            CycleFail = true;
                            CycleDone = false;
                            Blink(EngineData.DO.Signal_NOK, false, 500);
                            Step = EngineData.Step.Zero;
                        }
                    }
                    #endregion
                    break;

            }
        }

        public void Go2Pos(int drive, EngineData.AxisName axis, EngineData.AxisPosition Position, int velocity)
        {
            uint Vel = (uint)velocity;
            ushort acc = (ushort)IAIs[drive].Axis[(int)axis].Position[(int)Position].Acceleration;
            IAIs[drive].Axis[(int)axis].MoveAndWait(IAIs[drive].Axis[(int)axis].Position[(int)Position].TargetPosition, 15000, false, Vel, acc);
        }
        public void Go2Pos(int drive, EngineData.AxisName axis, int Position, int velocity)
        {
            uint Vel = (uint)velocity;
            ushort acc = 100;
            IAIs[drive].Axis[(int)axis].MoveAndWait(Position, 15000, false, Vel, acc);
        }
        public bool inPosition(int drive, EngineData.AxisName axis, EngineData.AxisPosition Position)
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
        public bool inPosition(int drive, EngineData.AxisName axis, int Position)
        {
            int postocheck = 0;
            int posread = 0;
            postocheck = Position;
            IAIs[drive].Axis[(int)axis].Device.ReadCurrentPosition(ref posread, Convert.ToByte((int)axis));
            if (postocheck > posread - 100 && postocheck < posread + 100)
            {
                return true;
            }
            return false;
        }
        public int getPosition(int drive, EngineData.AxisName axis)
        {
            int posread = 0;
            IAIs[drive].Axis[(int)axis].Device.ReadCurrentPosition(ref posread, Convert.ToByte((int)axis));
            return posread;
        }
        bool SSEL_TryToClean = false;
        public bool CleanSSelOutput()
        {
            //if (SSEL_TryToClean)
            //{
            //    // Reset other program bits
            //    WriteDO(EngineData.DO.Signal_OK, false);
            //    WriteDO(EngineData.DO.Signal_NOK, false);
            //    WriteDO(EngineData.DO.SSEL_PC1, false);
            //    WriteDO(EngineData.DO.SSEL_PC2, false);
            //    WriteDO(EngineData.DO.SSEL_PC4, false);
            //    WriteDO(EngineData.DO.SSEL_PC8, false);
            //    WriteDO(EngineData.DO.SSEL_PC10, false);
            //    WriteDO(EngineData.DO.SSEL_PC20, false);
            //    WriteDO(EngineData.DO.SSEL_PC40, false);
            //    WriteDO(EngineData.DO.SSEL_Start, false);
            //    WriteDO(EngineData.DO.SSEL_Continue, false);
            //    WriteDO(EngineData.DO.SSEL_Reset, false);

            //    WriteDO(EngineData.DO.SSEL_PC20, true);
            //    WriteDO(EngineData.DO.SSEL_Start, true);

            //    if (TON("CleanSSelOutput" + Step, 400) && !ReadDI(EngineData.DI.SSEL_Completed))
            //    {
            //        WriteDO(EngineData.DO.SSEL_PC20, false);
            //        WriteDO(EngineData.DO.SSEL_Start, false);
            //        SSEL_TryToClean = false;
            //        return true;
            //    }
            //    else
            //    {
            //        return false;
            //    }
            //}
            //else
            //{
            //    WriteDO(EngineData.DO.SSEL_PC20, false);
            //    WriteDO(EngineData.DO.SSEL_Start, false);

            //    SSEL_TryToClean = true;
            //}
            return false;
        }

        public bool isPartLockHome(string cav)
        {
            if (cav == "Cavidade1")
            {
                return (ReadDI(EngineData.DI.Cyl_Lock_PCB_L_H) && ReadDI(EngineData.DI.Cyl_Lock_PCB_R_H) && !ReadDI(EngineData.DI.Cyl_Lock_PCB_L_W) && !ReadDI(EngineData.DI.Cyl_Lock_PCB_R_W));
            }
            else if (cav == "MAC")
            {
                return (ReadDI(EngineData.DI.Cyl_Lock_PCB_L_H) && ReadDI(EngineData.DI.Cyl_Lock_PCB_R_H) && !ReadDI(EngineData.DI.Cyl_Lock_PCB_L_W) && !ReadDI(EngineData.DI.Cyl_Lock_PCB_R_W));
            }

            return false;
        }

        public bool hasPartsonNest(string cav, string part)
        {
            if (cav == "Cavidade1" && part == "Foil")
                return (ReadDI(EngineData.DI.Det_Foil_L) || ReadDI(EngineData.DI.Det_Foil_R));
            if (cav == "Cavidade1" && part == "PCB")
                return (ReadDI(EngineData.DI.Det_PCB_L) || ReadDI(EngineData.DI.Det_PCB_R));
            return false;
        }

        public void Vacuum(bool blnState)
        {
            WriteDO(EngineData.DO.Sol_Vacuum_Cleaner, blnState);
        }
    }
}
