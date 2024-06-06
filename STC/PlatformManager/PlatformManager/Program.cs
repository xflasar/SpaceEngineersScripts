using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private bool _WCFail = false;
        private WcPbApi api;

        private Dictionary<MyDetectedEntityInfo, float> targets = new Dictionary<MyDetectedEntityInfo, float>();
        private IMyTimerBlock _instaTimer;
        private List<IMyTerminalBlock> _timers = new List<IMyTerminalBlock>();

        private bool _triggered = false;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            
            GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(_timers);

            _instaTimer = _timers.Find(t => t.CustomName.ToLower().Contains("battle")) as IMyTimerBlock;
            SetUp();
        }

        void SetUp()
        {
            api = new WcPbApi();
            try
            {
                api.Activate(Me);
                _WCFail = false;
            }
            catch
            {
                _WCFail = true;
                Echo("WeaponCore Api is failing! \n Make sure WeaponCore is enabled!"); return;
            }
        }

        bool CheckInit()
        {
            if (_WCFail)
            {
                SetUp();
                return false;
            }

            return true;
        }

        void GetTargets()
        {
            api.GetSortedThreats(Me, targets);
            Echo($"Targets: {targets.Count}");
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (!CheckInit()) return;
            Echo(_timers.Count.ToString());
            if (_timers.Count == 0) return;
            GetTargets();

            if (targets.Count > 0 && !_triggered && _instaTimer != null)
            {
                _instaTimer.Trigger();
                _triggered = true;
                Echo("Triggered Timer!");
            } else if (targets.Count == 0 && _triggered)
            {
                _triggered = false;
                Echo("Timer was reset!");
            }
        }
    }
}
