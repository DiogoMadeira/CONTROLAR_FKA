using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Preh {
    public class HardwareInterface {
        public static event Action<string, bool> TriggerScanner;
        public static event Action<string, bool> TriggerRFID;

        public static event Action StopAllScanners;
        public static event Action StopAllRFID;

        private static int CounterTimeout;

        public List<EngineData.Step> SentMessages { get; set; }
        public EngineData.Step Step { get; set; }

        public HardwareInterface()
        {
            CounterTimeout = 0;
            SentMessages = new List<EngineData.Step>();
        }

        public bool StartScanner(string name)
        {
            if (!SentMessages.Contains(Step))
            {
                if (CounterTimeout < 100)
                {
                    TriggerScanner?.Invoke(name, true);
                    SentMessages.Add(Step);
                    CounterTimeout++;

                }
                else
                {
                    CounterTimeout = 0;
                    return false;
                }

                return true;
            }
            CounterTimeout = 0;
            return false;
        }

        public void StopScanner(string name)
        {
            if (!SentMessages.Contains(Step))
            {
                TriggerScanner?.Invoke(name, false);
                SentMessages.Remove(Step);
                CounterTimeout = 0;
            }
        }

        public void StartRFID(string name)
        {
            if (!SentMessages.Contains(Step)) TriggerRFID?.Invoke(name, true);
        }

        public void StopRFID(string name)
        {
            if (!SentMessages.Contains(Step))
            {
                TriggerRFID?.Invoke(name, false);
                SentMessages.Remove(Step);
            }

        }
        public void ReleaseAllHardware()
        {
            SentMessages.Clear();
        }
        public void ReleaseHardwareOnThisStep()
        {
            SentMessages.Remove(Step);
        }

        public  void StopAllHardware()
        {
            StopAllScanners?.Invoke();
            ReleaseAllHardware();
            StopAllRFID?.Invoke();
        }
    }
}