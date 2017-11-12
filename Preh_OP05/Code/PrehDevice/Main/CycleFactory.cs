using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Windows.Forms;
using PPDBAccess;

namespace Preh {
    public class CycleFactory {


        public static event Action<Engine.ENUM_Cycle> CyclesKilled;


        private static Task _cycleTask;
        public static Action CycleMethod;
        private HomeCycle HomePosition { get; set; }
        private AutoCycle Auto { get; set; }
        public TextBox TextBoxInstructions { get; set; }
        public bool HomeCycleDone { get; set; }
        public bool HomeCycleFail { get; set; }
        public bool AutoCycleFail { get; set; }
        public PPTraceStation Db { get; set; }
        public string PrehRef { get; set; }
        public int WSID { get; set; }
        public Dictionary<string,int> SubWorkCenterIDs { get; }
        public bool Traceability { get; }
        public bool RFIDTraceability { get; }
        public bool AutoRef { get; }
        public bool SaveResults { get; }
        public DataTable Dt_DO { get; set; }
        public DataTable Dt_DI { get; set; }
        public DataTable Dt_AI { get; set; }
        public DataTable Dt_AO { get; set; }
        public List<IAIModbusASCII> IAIs {get;set;}

        public List<VisionSystemBOA> BOAs { get; set; }
        public Engine.ENUM_Cycle CurrentCycle { get; set; }
        private static System.Threading.CancellationTokenSource CycleTaskCancellationToken { get; set; }
        public IOCycle BKResource
        {
            set
            {
                HomePosition.BKResource = value;
                Auto.BKResource = value;
            }
        }

        public bool HasHomeCycle { get; set; }
        public int CycleID { get; set; }
        public string ViewForReference { get; set; }

        public CycleFactory(int wsID, Dictionary<string, int> subWCIDs, bool usingTraceability, bool usingAutoRef, int cycleid, bool hasHomeCycle, bool usingRFIDTraceability) :
             this(wsID, subWCIDs, usingTraceability, usingAutoRef, cycleid, hasHomeCycle, usingRFIDTraceability, null,null) {

        }
        public CycleFactory(int wsID, Dictionary<string,int> subWCIDs, bool usingTraceability, bool usingAutoRef, int cycleid, bool hasHomeCycle, bool usingRFIDTraceability, PPTraceStation db, string viewForRef)
        {
            //PrehRef = prehref;
            WSID = wsID;
            SubWorkCenterIDs = subWCIDs;
            Traceability = usingTraceability;
            AutoRef = usingAutoRef;
            CycleID = cycleid;
            HasHomeCycle = hasHomeCycle;
            RFIDTraceability = usingRFIDTraceability;
            this.Db = db;
            ViewForReference = viewForRef;
            //CycleTaskCancellationToken = new System.Threading.CancellationTokenSource();
            HomePosition = new HomeCycle(db, PrehRef, WSID, subWCIDs, Traceability, CycleID, HasHomeCycle, usingRFIDTraceability);
            Auto = new AutoCycle(db, PrehRef, WSID, subWCIDs, Traceability, CycleID, HasHomeCycle, usingRFIDTraceability);
        }

        public bool CycleModeRun(Engine.ENUM_Cycle Type) {
            CurrentCycle = Type;
            if (Type == Engine.ENUM_Cycle.Auto && !AutoCycleFail) {
             
                Auto.IAIs = IAIs;
                Auto.BOAs = BOAs;

                CycleTaskCancellationToken = new System.Threading.CancellationTokenSource();
                _cycleTask = new Task(Auto.RunCycle, CycleTaskCancellationToken.Token, TaskCreationOptions.AttachedToParent);

                
                _cycleTask.RunSynchronously();
                _cycleTask.Wait();
                _cycleTask.Dispose();

                BOAs = Auto.BOAs;
                IAIs = Auto.IAIs;
                if (Auto.CycleFail == true) {
                    Auto.Step = 0;
                    AutoCycleFail = true;
                }
            }

            if (Type == Engine.ENUM_Cycle.Home && !HomeCycleFail && HasHomeCycle) {
                
                HomePosition.IAIs = IAIs;
                HomePosition.BOAs = BOAs;


                _cycleTask = new Task(HomePosition.RunCycle, TaskCreationOptions.AttachedToParent);
              
                _cycleTask.RunSynchronously();
                
                
                _cycleTask.Wait();
                _cycleTask.Dispose();

                BOAs = HomePosition.BOAs;
                IAIs = HomePosition.IAIs;

                //ciclo Home Feito ou nao
                if (HomePosition.CycleDone) {
                    HomeCycleDone = true;
                    HomePosition.Step = 0;
                    HomeCycleFail = false;
                } else {
                    HomeCycleDone = false;
                    HomeCycleFail = false;
                }
                if (HomePosition.CycleFail == true) {
                    HomeCycleFail = true;
                    HomePosition.Step = 0;
                }
            }
            return true;
        }

        public void RestartCycles() {
            HomePosition.Step = EngineData.Step.Zero;
            Auto.Step = EngineData.Step.Zero;
            HomePosition.HMI.ReleaseAllMessages();
            Auto.HMI.ReleaseAllMessages();
        }
        public void KillCycles() {
            try {

                StopAllCurrentCycleHardware();
                CycleTaskCancellationToken.Cancel();
                CycleTaskCancellationToken.Dispose();
                CyclesKilled?.Invoke(CurrentCycle);
                //CycleTask.Dispose();
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null) throw ex.InnerException;
            }
        }

        private void StopAllCurrentCycleHardware()
        {
            if (CurrentCycle == Engine.ENUM_Cycle.Auto)
            {
                Auto.Hardware.StopAllHardware();
            }
            if (CurrentCycle == Engine.ENUM_Cycle.Home)
            {
                HomePosition.Hardware.StopAllHardware();
            }
        }
    }
}
