using DynamicPatcher;
using Extension.Utilities;
using PatcherYRpp;
using PatcherYRpp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace Extension.Ext
{

    [Serializable]
    public class FireSuperWeapon
    {
        public HouseExt HouseExt;
        public Pointer<HouseClass> House => null != HouseExt ? HouseExt.OwnerObject : default;
        public CellStruct Location;
        public FireSuperEntity Data;

        private int count;
        private int initDelay;
        private TimerStruct initDelayTimer;
        private int delay;
        private TimerStruct delayTimer;

        public FireSuperWeapon(HouseExt houseExt, CellStruct location, FireSuperEntity data)
        {
            this.HouseExt = houseExt;
            this.Location = location;
            this.Data = data;

            this.count = 0;
            this.initDelay = data.RandomInitDelay.GetRandomValue(data.InitDelay);
            this.initDelayTimer.Start(this.initDelay);
            this.delay = data.RandomDelay.GetRandomValue(data.Delay);
            this.delayTimer.Start(0);
        }

        public bool CanLaunch()
        {
            return initDelayTimer.Expired() && delayTimer.Expired();
        }

        public bool Cooldown()
        {
            count++;
            delayTimer.Start(delay);
            return IsDone();
        }

        public bool IsDone()
        {
            return Data.LaunchCount > 0 && count >= Data.LaunchCount;
        }

    }

}
