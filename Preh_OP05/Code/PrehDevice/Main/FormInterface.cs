using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Preh {
    public class FormInterface {
        public static System.Timers.Timer MyTimer;
        private Color PanelColor;
        private Color ActualColor;
        public int CycleID { get; set; }
        private List<EngineData.Step> SentMessages { get; set; }
        public EngineData.Step Step { get; set; }



        public static event Action<int, string> NewInstructionTextByText;
        public static event Action<int> EndOfInstructionText;
        public static event Action<int> ClearInstructionText;
        public static event Action<string> StepChanged;
        public static event Action<int> CountParts;
        public static event Action<int, Color> UpdateColorInstructionTextBox;
        public static event Action<string, string> ShowPictureMessage;
        public static event Action<EngineData.Screens> ShowPanel;
        public static event Action<string> ChangeRef;
        public static event Action<bool> UpdateBoaState;
        public static event Action<int, int> GetTraceErrorDescription;
        public static event Action<EngineData.TypeOfCycle> CycleTypeChanged;

        

        public FormInterface(int cycleId) {
            ActualColor = SystemColors.Control;
            CycleID = cycleId;
            SentMessages = new List<EngineData.Step>();
        }
        public void ChangeReference(string Reference) 
        {
            ChangeRef?.Invoke(Reference);
        }
        public void PanelStatus(EngineData.Screens panel)
        {
            ShowPanel?.Invoke(panel);

        }
       
        public void ChangeCycleType(EngineData.TypeOfCycle cycleType)
        {
            CycleTypeChanged?.Invoke(cycleType);
        }


        public void JoinMessage(string text) {
            if (!SentMessages.Contains(Step)) NewInstructionTextByText?.Invoke(CycleID, text);
        }
        public void JoinTraceErrorDescription(int errorCode)
        {
            if (!SentMessages.Contains(Step)) GetTraceErrorDescription?.Invoke(CycleID, errorCode);
        }

        public void EndMessage() {
            if (!SentMessages.Contains(Step)) {
                SentMessages.Add(Step);
                EndOfInstructionText?.Invoke(CycleID);
            }
        }

        public void JoinAndSendMessage(string text)
        {
            JoinMessage(text);
            EndMessage();

        }
        
        public void StepChange(EngineData.Step step) {
            Step = step;
            StepChanged?.Invoke(step.ToString().Replace("State_", ""));
        }

        public void ClearInstructions() {
            SentMessages.Add(Step);
            ClearInstructionText?.Invoke(CycleID);
        }

        public static void SendPartCout(int count) {
            CountParts?.Invoke(count);
        }

        public void ShowPicAndMessage(string picName, string message) {
            ShowPictureMessage?.Invoke(picName, message);
        }
        public void ReleaseMessageOnStep(EngineData.Step step) {
            SentMessages.Remove(step);
        }
        public void ReleaseAllMessages() {
            SentMessages.Clear();
        }

        public void BlinkPanel(Color color, bool turnOnOff, int freq) {
            MyTimer.Elapsed += (sender, e) => TogglePanelColor(sender, e, color);

            if (turnOnOff && !MyTimer.Enabled) {
                MyTimer.Interval = freq;
                MyTimer.Enabled = true;
                MyTimer.Start();
            } else if (!turnOnOff && MyTimer.Enabled) {
                MyTimer.Stop();
                MyTimer.Enabled = false;
            }
        }

        private void TogglePanelColor(object source, System.Timers.ElapsedEventArgs e, Color color) {
            PanelColor = color;
            if (ActualColor == SystemColors.Control) {
                UpdateColorInstructionTextBox?.Invoke(CycleID, PanelColor);
                ActualColor = PanelColor;
            } else {
                UpdateColorInstructionTextBox?.Invoke(CycleID, SystemColors.Control);
                ActualColor = SystemColors.Control;
            }
        }

        public void ChangeBoaState(bool State) {
            UpdateBoaState?.Invoke(State);
        }
    }
}
