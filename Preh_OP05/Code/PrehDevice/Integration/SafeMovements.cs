using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Preh
{
    class SafeMovements
    {

        public bool[] inhibitArray { get; }
        public IOCycle BKResource { get; set; }
        public List<IAIModbusASCII> IAIs { get; set; }
        public SafeMovements(IOCycle bk, List<IAIModbusASCII> iais, bool[] inhibit)
        {
            BKResource = bk;
            IAIs = iais;
            inhibitArray = inhibit;
        }

        public bool[] SafeMovementsLogic()
        {


            if (isTableWork())
            {
                inhibitArray[(int)EngineData.DO.Sol_Cyl_Proy_W] = true;//can move                
            }
            else
            {
                inhibitArray[(int)EngineData.DO.Sol_Cyl_Proy_W] = false;//can't move

            }

            if (isHome_Proy())
                inhibitArray[(int)EngineData.DO.Sol_Cyl_Table_H] = true;//can move
            else
                inhibitArray[(int)EngineData.DO.Sol_Cyl_Table_H] = false;//can't move

            return inhibitArray;
        }
        //TODO: Implement Read Output
        public bool isTableWork()
        {
            return (bool)BKResource.Dt_DI.Rows[(int)EngineData.DI.Cyl_Table_W]["Value"] && !(bool)BKResource.Dt_DI.Rows[(int)EngineData.DI.Cyl_Table_H]["Value"];
        }
        public bool isWork_Proy()
        {
            return (bool)BKResource.Dt_DI.Rows[(int)EngineData.DI.Cyl_Proy_W]["Value"] && !(bool)BKResource.Dt_DI.Rows[(int)EngineData.DI.Cyl_Proy_H]["Value"];
        }
        public bool isHome_Proy()
        {
            return (bool)BKResource.Dt_DI.Rows[(int)EngineData.DI.Cyl_Proy_H]["Value"] && !(bool)BKResource.Dt_DI.Rows[(int)EngineData.DI.Cyl_Proy_W]["Value"];
        }
    }
}

