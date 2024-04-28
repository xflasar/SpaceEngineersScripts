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
using Sandbox.Game.Entities;
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
        private WcPbApi api;
        private List<IMyTerminalBlock> shieldcontrollers = new List<IMyTerminalBlock>();

        private IMyTerminalBlock mainShieldController;

        private List<IMyTerminalBlock> _phasers = new List<IMyTerminalBlock>();
        private List<IMyTerminalBlock> _disruptors = new List<IMyTerminalBlock>();
        private List<IMyTerminalBlock> _pdc = new List<IMyTerminalBlock>();
        private List<IMyTerminalBlock> _blocks = new List<IMyTerminalBlock>();

        // Power blocks
        private List<IMyReactor> _reactors = new List<IMyReactor>();
        private List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>();
        private List<IMyPowerProducer> _hydrogenEngineGenerators = new List<IMyPowerProducer>();

        private List<IMyRadioAntenna> _antennas = new List<IMyRadioAntenna>();

        public List<IMyTextPanel> myTextPanels = new List<IMyTextPanel>();

        static public Program pInstance = null;
        private IMyTextPanel _logOutput;
        public StringBuilder EchoString = new StringBuilder();

        private int counter = 0;
        private bool _WCFail = false;

        private string mode = "OnlyDisplay";

        public Program()
        {
            pInstance = this;
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            Echo = EchoToLCD;

            GridTerminalSystem.GetBlocks(_blocks);
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(myTextPanels);
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(_phasers, b => b.DetailedInfo.Contains("Phase Cannon") || b.DetailedInfo.Contains("Phaser Bank"));
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(_disruptors, b => b.GetType().FullName.ToLower().Contains("disruptor cannon"));
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(_pdc, b => b.GetType().FullName.ToLower().Contains("point defence phaser"));
            
            GridTerminalSystem.GetBlocksOfType<IMyReactor>(_reactors);
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(_batteries);
            GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(_hydrogenEngineGenerators, b => b.CubeGrid == Me.CubeGrid && b.DetailedInfo.ToLower().Contains("hydrogen engine"));

            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(_antennas);


            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(shieldcontrollers,
                b => b.CubeGrid == Me.CubeGrid && b.CustomName.ToLower().Contains("shield controller"));

            _logOutput = myTextPanels.Find(t => t.CustomName.Contains("WeaponLog")); // Set the name here so the script can get the lcd to put Echo to

            SetUp();
            InitPhasers();

            // Set Antennas
            _antennas.ForEach(ant =>
            {
                ant.Radius = 5000;
                ant.EnableBroadcasting = true;
                ant.ApplyAction("OnOff_On");
            });

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

            if (shieldcontrollers.Count != 0)
            {
                _shield = new ShieldApi(Me);
                mainShieldController = shieldcontrollers[0];
                _shield.SetActiveShield(mainShieldController);

                BootUp();
            }
            else
            {
                Echo("Shield is not present!");
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument != String.Empty)
            {
                int arg = Int32.MinValue;
                if(!int.TryParse(argument, out arg))
                mode = argument;


            }

            if(_WCFail)
            SetUp();

            counter++;
            _logOutput?.WriteText("");
            EchoString.Clear();
            Echo(mode);


            // Get sorted enemies
            GetSortedEnemies();

            if (shieldcontrollers.Count != 0)
            {
                // Shields
                CheckShield();
                GetData();

                PrintShieldStatus();
            }

            // Used to get maxAvailablePower
            GetMaxAvailablePower();

            if (mode == "OnlyDisplay")
            {
                ShutdownWeapons();
            } 
            else if (mode == "BattleMode")
            {
                if (api.GetAiFocus(Me.EntityId).HasValue && !api.GetAiFocus(Me.CubeGrid.EntityId).Value.IsEmpty())
                {
                    _target = api.GetAiFocus(Me.CubeGrid.EntityId).Value.EntityId;
                    try
                    {
                        FireAtTarget();
                        FireDisruptors();
                    }
                    catch (Exception e)
                    {
                        Echo("problem firing: " + e);
                        throw new Exception();
                    }
                }
                else if (_target != 0)
                {
                    try
                    {
                        api.SetAiFocus(Me, _target);
                    }
                    catch (Exception)
                    {
                        // We don't need to raise Exception just default target to 0
                        _target = 0;
                    }
                }
            }

            Echo(EchoString.ToString());
        }

        public void EchoToLCD(string text)
        {
            // Append the text and a newline to the logging LCD
            // A nice little C# trick here:
            // - The ?. after _logOutput means "call only if _logOutput is not null".
            //_logOutput?.WriteText("");
            _logOutput?.WriteText($"{text}\n", true);
        }
    }
}
