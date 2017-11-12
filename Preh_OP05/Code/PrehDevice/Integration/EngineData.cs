using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows.Forms;
namespace Preh
{
    public enum Tools { AU651, AU583 }
    public enum Cavities { Left, Right }
    public static class EngineData
    {
        public static bool FirstCycleHome = true;
        public static string PrehRef { get; set; }
        public static string TraceNr { get; set; }
        public static int DPG_Cycles, DPG_Retries, DPG_NumberCycles;
        public static int[] DPG_Result = { 2, 2, 2, 2 };  //DEPRAG NUMBER OF RESULTS

        public static bool[] digitalWantedResult_Cavidade1 = { true, true };
        public static bool[] digitalWantedResult_MAC = { true, true };
        public static int[] Machine_Result = { };
        public static double Pot_Offset;

        public static DataSet CalibrationMeasurementList = new DataSet();

        //Output to corresponding Checkbox on Form
        public static DO[] SafetyDO = { };
        public static CheckBox[] CbOutput = { };


        public enum TypeOfCycle
        {
            FirstCycle,
            NormalCycle,
            LastCycle
        }

        public enum DI
        {
            Safety_Circuit_On = 0,
            Air_Pressure = 1,
            Security_Air_Pressure = 2,
            Maintenance_L_Door_Open = 4,
            Maintenance_R_Door_Open = 5,
            Foot_Start = 8,
            Button_NOK = 9,
            Button_Cycle = 10,
            Rejection_Box=12,
            Cyl_Table_H = 14,
            Cyl_Table_W = 15,
            Det_PCB_L = 16,
            Det_PCB_R = 17,
            Det_Foil_L = 18,
            Det_Foil_R = 19,
            Cyl_Lock_PCB_L_H = 20,
            Cyl_Lock_PCB_L_W = 21,
            Cyl_Lock_PCB_R_H = 22,
            Cyl_Lock_PCB_R_W = 23,
            Cyl_Clinch_H = 24,
            Cyl_Clinch_W = 25,
            Det_MetalFrame = 26,
            Det_Mask=27,
            Cyl_Proy_H = 32,
            Cyl_Proy_W = 33,
            Det_Display_L = 34,
            Det_Display_R = 35,
            Camera1_Object = 40,
            Camera1_Ready = 41,
            Camera2_Object = 42,
            Camera2_Ready = 43,
            Ionizer_Maintenance = 44,
            Ionizer_NoError = 45,

        }
        public enum DO
        {

            Safety_Relay_On = 0,
            Signal_OK = 1,
            Signal_NOK = 2,
            Relay_Machine_Light = 3,
            Sol_Vacuum_Cleaner = 4,
            Sol_Cyl_Proy_H = 5,
            Sol_Cyl_Proy_W = 6,
            Sol_Cyl_Lock_PCB_W = 8,
            Sol_Cyl_Table_H = 9,
            Sol_Cyl_Table_W = 10,
            Sol_Cyl_Clinch_H = 11,
            Sol_Cyl_Clinch_W = 12,
            Sol_Ionizer_W = 13,
            Sol_Cyl_Enable_Proy_W = 14,
            Camera1_Trigger = 24,
            Camera1_In1 = 25,
            Camera1_In2 = 26,
            Camera2_Trigger = 27,
            Camera2_In1 = 28,
            Camera2_In2 = 29,
            Ionizer_Stop_discharge = 30,
            Ionizer_Electrode_contamination = 31,

        }

        public enum AI
        {

        }
        public enum AO
        {

        }
        public enum Step
        {
            
            Zero=0,
            Step_Initial,

            //Home Cycle Steps
            MoveProyInf,
            MoveProySup,
            MoveTable,
            MoveClinch,
            MovePartLock,
            RemovePart,
            FailMovement,
            RemoveHousing,
            RemoveFoil,
            RemoveCorona,
            RemovePCB,
            RemovePointer,
            RemoveRubber,
            CycleSampleOrHomeEnd,
            State_Vacuum_Init,
            State_Vacuum_Exec,

            //Sample steps
            MoveLockSample,
            InsertSample_Cavidade1,
            InsertSample_MAC,
            ReadSample,
            ScannerFail_Sample,
            ReleaseNOKAcknowledge_Sample,
            PressPedalScanner_Sample,
            GetRefSample,
            Sample_MoveTableWork,
            Sample_MoveProySupWork,
            Sample_MoveProyInfWork,
            RunSample,
            Sample_MoveProyInfHome,
            Sample_MoveProySupHome,
            Sample_MoveTableHome,
            Sample_MovePartLockHome,
            RunSecondSampleOrEnd,


            //Auto cycle steps
            LockReverse,
            RemoveHousingAuto,
            InsertRubberandChrome,
            InsertPCB,
            PressPedalNoHousing,
            TabletoWorkNoHousing,
            ProySuptoWorkNoHousing,
            ProyPressTimeNoHousing,
            ProySuptoHomeNoHousing,
            TabletoHomeNoHousing,
            InsertHousing,
            LockHousing,
            LockPCB,
            InsertBRCPCinHousing,
            InsertLightGuide,
            InsertLCD,
            InsertMetalFrame,
            InsertLightCaseandRubbersandDiffuserFoil,
            ReleasePedal,
            ReloadRubberandChrome,
            PressPedal,
            TabletoWork,
            ProyInftoWork,
            ClinchtoWork,
            ProyInfPressTimer,
            ProySuptoWork,
            ProySupPressTimer,
            ProySuptoHome,
            ProyInftoHome,
            ClinchtoHome,
            TabletoHome,
            UnlockHousing,
            RemoveHousingComplete,
            PressPedalNoExt,
            TabletoWorkNoExt,
            ProyInftoWorkNoExt,
            ProySuptoWorkNoExt,
            ProyPressTimeNoExt,
            ProySuptoHomeNoExt,
            ProyInftoHomeNoExt,
            TabletoHomeNoExt,
            UnlockHousingNoExt,
            RemoveHousingCompleteNoExt,
            RemoveFoils,
            SaveResults,
            CheckCaliber,
            MeasurementCalibPart,
            SaveCaliber,
            ReadRFID,
            LastCycleStep,
            ReleaseButtonNOKFail,
            Fail,
            FailTabletoWorkNoHousingMov,
            ReleaseButtonNOKFailTableNoHousingMov,
            TableMovFailMSGContinueorCancelNoHousing,
            RemovePartandDiscardNoHousing,
            FailTabletoWorkMov,
            ReleaseButtonNOKFailTableMov,
            TableMovFailMSGContinueorCancel,
            RemovePartandDiscard,
            LastCycleMsg,
            FailTabletoWorkMovNoExt,
            ReleaseButtonNOKFailTableMovNoExt,
            TableMovFailMSGContinueorCancelNoExt,
            RemovePartandDiscardNoExt,
            InsertCoronaParts,
            InsertRubberandChromeInHousing,
            ScannerRead,
            ScannerFail,
            MoveProyInftoWork,
            ReleaseNOKAcknowledge,
            PressPedalScanner,
            RunCalibrationOrTestPart,
            DecisionTryAgainOrReject,
            DecisionPartOKorNOK,
            DecisionPartOKorNOK_FirstCycle,
            PressPedalOrButtonNOK,
            State_CheckIFM1,
            State_CheckIFM2,
            State_CheckIFM3,
            State_CheckIFM4,
            State_CheckIFM5,
            State_CheckIFM1_Camera1,
            State_CheckIFM2_Camera1,
            State_CheckIFM3_Camera1,
            State_CheckIFM4_Camera1,
            State_CheckIFM5_Camera1,
            State_CheckIFM1_Camera2,
            State_CheckIFM2_Camera2,
            State_CheckIFM3_Camera2,
            State_CheckIFM4_Camera2,
            State_CheckIFM5_Camera2,
            PotiFail,
            InsertMask,
            RemoveMask,

            //Traceability steps in auto cycle
            TraceabilityGetTraceNrByTag,
            TraceabilityCheckTraceNrRFID,
            TraceabilityGetRefByTraceNr,
            TraceabilityCheckAssemblyByRef,
            TraceabilityCheckTraceNr,
            TraceabilityJobStart,
            TraceabilityUpgradeRef,
            TraceabilityCheckAssembly,
            TraceabilitySaveAssemblyByBatch,
            ErrorTrace,
            TraceabilityJobEnd,
            TraceabilityJobEndNoExt,
            TraceabilityJobEndFail,
            TraceabilityGetLCDInspectionData,
            TraceabilitySaveInfo_LCD,
            TraceabilityAckButtonNOK,

        }

        public enum Screens
        {
            Base,
            ImageVacuum,
            ImageRef,
            ImageLabel,
            ImageNestScrewLeft,
            ImageNestScrewRight,
            ImageSensors,
            BoaCam,
            ShowHighlight_R,
            ShowHighlight_L,
            ShowImageLabelL3Light,
            ShowImageLabelR3Light,
            ShowImageLabelL2Light,
            ShowImageLabelR2Light,
            ImageKeys,
            ImageNestScrewSide2,
            ImageNestScrewSide1,
            ImageMachineResults,
        }

        public enum AxisName
        {
            //VertPen = 3
        }
        public enum AxisPosition
        {
            //Home = 0,
            //GlueLeft = 1,
            //GlueRight = 2,
            //Test = 3,
            //MeedleGlue = 4,
            //Work0 = 5,
            //Work1 = 6,
            //Work2 = 7,
            //Work3 = 8,
            //Work4 = 9,
            //CalibZStartPos = 10,
            //RetreatPos = 11
        }


    }
}

