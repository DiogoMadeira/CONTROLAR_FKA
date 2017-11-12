using AxipermtinterfaceLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Preh {
    public class VisionSystemBOA {
        #region variables
    
        public bool newData = false;
        private AxipermtinterfaceLib.AxIIpeDisplay visionDisplay;
        ipermtinterfaceLib.IIpeRmtInterface hSherlock;
        ipermtinterfaceLib.RunState currentRunState;
        #endregion


        private Task _runStateTask;
        private Task _abortStateTask;
        private Task _changeProgTask;

        private List<Task> _runningTasks;
        public bool InRunState { get; set; }
        public bool InAbortState { get; set; }
        public bool ProgramChanged { get; set; }
        public int ProgramNumber { get; set; }
        public bool WithDisplayConnection { get; set; }
        public string IP { get; set; }
        public string Status { get; set; }
        //private AxIIpeDisplay visionDisplay { get; set; }

        public VisionSystemBOA()
        {
            InRunState = false;
            InAbortState = false;
            ProgramChanged = false;

            _runningTasks = new List<Task>();
        }


        public bool connect(bool withDisplay) {
            return this.connect(ref visionDisplay, IP, withDisplay);
        }
        public bool connect(ref AxipermtinterfaceLib.AxIIpeDisplay display, string ipAddr, bool withdisp) {
            visionDisplay = display;
            hSherlock = new ipermtinterfaceLib.IIpeRmtInterface();

            //Connect
            if (!hSherlock.connectServer_1(ipAddr)) {
                Status = "Error While Connect " + ipAddr;
                return false;
            }

            //Initialize
            if (!hSherlock.initialize()) {
                Status = "Error While Initialize " + ipAddr;
                return false;
            }

            //Connect Display
            //hSherlock.connectDisplay("image_windowA", BoaProDisplay1.displayHandle());
            if (withdisp)
            {
                try
                {
                    hSherlock.connectDisplay("image_windowA", display.displayHandle());
                }
                catch (Exception ex)
                {
                    throw ex.InnerException;
                }
                ;
            }
            
            Status = "Connection " + ipAddr;

            hSherlock.programLoopCompleted +=
                new ipermtinterfaceLib.IIIpeRmtInterfaceEvents_programLoopCompletedEventHandler(Device_ProgramLoopCompleted);

            return true;
        }

        public bool doubleconnect(ref AxipermtinterfaceLib.AxIIpeDisplay display1, ref AxipermtinterfaceLib.AxIIpeDisplay display2, string ipAddr) {
            visionDisplay = display1;
            hSherlock = new ipermtinterfaceLib.IIpeRmtInterface();

            //Connect
            if (!hSherlock.connectServer_1(ipAddr)) {
                Status = "Error While Connect " + ipAddr;
                return false;
            }

            //Initialize
            if (!hSherlock.initialize()) {
                Status = "Error While Initialize " + ipAddr;
                return false;
            }

            //Connect Display
            //hSherlock.connectDisplay("image_windowA", BoaProDisplay1.displayHandle());
            hSherlock.connectDisplay("image_windowA", display1.displayHandle());
            hSherlock.connectDisplay("image_windowA", display2.displayHandle());
            Status = "Connection " + ipAddr;

            hSherlock.programLoopCompleted +=
                new ipermtinterfaceLib.IIIpeRmtInterfaceEvents_programLoopCompletedEventHandler(Device_ProgramLoopCompleted);

            return true;
        }

        private void Device_ProgramLoopCompleted() {
            newData = true;
        }

        public bool disconnect() {
            try {
                //Disconnect Display
                hSherlock.disconnectDisplay(visionDisplay.displayHandle());

                //Disconnect
                if (!hSherlock.disconnectServer()) {
                    Status = "Error While Disconnect ";
                    return false;
                } else {
                    Status = "No connection";
                    return true;
                }
            }
            catch (Exception) {
                Status = "Error While Disconnect ";
                return false;
            }
        }

        public string getRunState() {
            currentRunState = hSherlock.getRunState();

            hSherlock.setLiveMode("Display", true);

            return currentRunState.ToString();
        }

        public bool setRunState() {

            InRunState = hSherlock.setRunState(ipermtinterfaceLib.RunState.Run);
            return InRunState;
        }

        public bool setRunOnceState() {
            return hSherlock.setRunState(ipermtinterfaceLib.RunState.RunOnce);
        }

        public bool setAbortState() {
            InAbortState = hSherlock.setRunState(ipermtinterfaceLib.RunState.AbortRequested);
            return InAbortState;
        }

        public bool loadProgram(int programIndex, bool withDisplay) {
            //load our program

            ProgramChanged = hSherlock.loadProgram(programIndex);
            var bReturn = ProgramChanged;

            if (bReturn != true) {
                Status = "Unable to load program" + programIndex;
                return false;
            } else {
                if (withDisplay)
                {
                    hSherlock.connectDisplay("image_windowA", visionDisplay.displayHandle());
                }
                
                Status = "Program - " + programIndex + " Loaded!";
                return true;
            }
        }

        public string getPropertyValue(string propertyName) {
            string propertyValue = hSherlock.getPropertyValue(propertyName).ToString();

            if (propertyValue.Equals("System.Reflection.Missing"))
                return "Invalid Property Name";
            else
                return propertyValue;
        }


        public void StartSetAbortStateAsync()
        {
            if (!_runningTasks.Contains(_abortStateTask))
            {

                _abortStateTask = Task.Factory.StartNew(() => setAbortState());
                _runningTasks.Add(_abortStateTask);
            }

        }

        public void StartSetRunStateAsync()
        {

            if (!_runningTasks.Contains(_runStateTask))
            {

                _runStateTask = Task.Factory.StartNew(() => setRunState());
                _runningTasks.Add(_runStateTask);
            }
        }

        public void StartLoadProgramAsync(int programIndex, bool withDisplay)
        {

            if (!_runningTasks.Contains(_changeProgTask))
            {
                ProgramNumber = programIndex;
                _changeProgTask = Task.Factory.StartNew(() => loadProgram(programIndex, withDisplay));
                _runningTasks.Add(_changeProgTask);
            }

        }


        public void ClearsRunningTasks()
        {
            _runningTasks.Clear();
            _changeProgTask.Dispose();
            _runStateTask.Dispose();
            _abortStateTask.Dispose();

            InRunState = false;
            InAbortState = false;
            ProgramChanged = false;


            _changeProgTask = null;
            _runStateTask = null;
            _abortStateTask = null;

        }
    }
}
