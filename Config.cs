using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace PvPChecks
{
    public class Config
    {
        //public List<int> weaponBans = new List<int>();
        public List<ValueTuple<int, int, bool>> weaponBans = new List<ValueTuple<int, int, bool>>();
        public List<int> accsBans = new List<int>();
        public List<int> armorBans = new List<int>();
        public List<int> buffBans = new List<int>();
        public List<ValueTuple<int, bool>> projBans = new List<ValueTuple<int, bool>>();
        public bool portalGunBlock = true;
        public bool solarArmorDebuff = true;
        public string[] disabledcommandsInPvp = new string[]
        {
            "back"
        };
    }
}
