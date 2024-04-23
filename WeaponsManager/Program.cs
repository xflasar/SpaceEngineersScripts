using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        static public Program pInstance = null;
        public List<IMyTextPanel> myTextPanels = new List<IMyTextPanel>();

        private MyIni _ini = new MyIni();

        public Program()
        {
            pInstance = this;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            Echo = EchoToLCD;

            // Fetch a log text panel
            
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(myTextPanels);
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(WeaponsRails, b => b.CustomName.ToLower().Contains("rail"));
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(Weapons, b => b.CustomName.ToLower().Contains("pdc"));
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(batteries, b => b.CustomName.ToLower().Contains("battery"));

            _logOutput = myTextPanels.Find(t => t.CustomName.Contains("WeaponLog")); // Set the name here so the script can get the lcd to put Echo to

            api = new WcPbApi();
            try
            {
                api.Activate(Me);
            }
            catch
            {
                Echo("WeaponCore Api is failing! \n Make sure WeaponCore is enabled!"); return;
            }

        }

        void InitCustomData()
        {
            if (Me.CustomData.Length > 0)
            {
                MyIniParseResult result;
                if (!_ini.TryParse(Me.CustomData, out result))
                    throw new Exception(result.ToString());

                
            }
            else
            {
                MyIniParseResult result;
                if (!_ini.TryParse(Me.CustomData, out result)) throw new Exception(result.ToString());

                Me.CustomData = _ini.ToString();
            }
        }

        IMyTextPanel _logOutput;

        public void EchoToLCD(string text)
        {
            // Append the text and a newline to the logging LCD
            // A nice little C# trick here:
            // - The ?. after _logOutput means "call only if _logOutput is not null".
            //_logOutput?.WriteText("");
            _logOutput?.WriteText($"{text}\n", true);
        }

        public WcPbApi api;

        public List<IMyTerminalBlock> Weapons = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> WeaponsRails = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> batteries = new List<IMyTerminalBlock>();
        public StringBuilder EchoString = new StringBuilder();

        string mode = "RailsOff";

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument != "")
            {
                mode = argument;
            }

            _logOutput?.WriteText("");

            EchoString.Clear();

            // PDC Management
            Echo("PDC Management: \nPDC count: " + Weapons.Count.ToString() + "\n Test");
            if (Weapons.Count > 0
                && api.GetProjectilesLockedOn(Me.EntityId).Item1)
            {
                try
                {
                    PDCMain();
                } catch (Exception ex)
                {
                    Echo(ex + "errored here");
                }
            }

            Echo(EchoString.ToString());

            // Rails Management
            Echo("Current Rail mode: " + mode + "\n" + "Batteries: " + _batteryOn);

            if (mode == "RailsOff")
            {
                if (_batteryOn)
                {
                    batteries.ForEach(battery =>
                    {
                        IMyBatteryBlock bat = battery as IMyBatteryBlock;
                        bat.ChargeMode = ChargeMode.Auto;
                    });
                    _batteryOn = false;
                }
                WeaponsRails.ForEach(w =>
                {
                    if (w.GetValueBool("OnOff"))
                    {
                        w.SetValueBool("OnOff", false);
                    }
                });
            }
            else if (mode == "TargetedStaggerShooting")
            {
                if (WeaponsRails.Count > 0)
                {
                    FireAlternateWeapons();
                }
            } else if (mode == "AIShoot")
            {
                if (WeaponsRails.Count > 0)
                {
                    SetRailsAutofire();
                }
            }


            return;
            /*
            weaponBlock = w;
            EchoString.Append("\nWeapon Info:\n");
            EchoString.Append("-----------------------\n");
            EchoString.Append("Weapon Target Info:\n");
            GetTargetInfo((MyDetectedEntityInfo)api.GetWeaponTarget(weaponBlock));
            EchoString.Append("Ready To Fire:" + api.IsWeaponReadyToFire(weaponBlock) + "\n");
            EchoString.Append("Max Range:" + api.GetMaxWeaponRange(weaponBlock, 0) + "\n");
            List<ITerminalAction> list = new List<ITerminalAction>();

            string a = api.GetHeatLevel(weaponBlock).ToString();
            EchoString.Append(a);

            EchoString.Append("\n");
            if (WeaponDefinitions.Contains(weaponBlock.BlockDefinition))
            {
                Matrix AZ = api.GetWeaponAzimuthMatrix(weaponBlock, 0);
                Matrix EL = api.GetWeaponElevationMatrix(weaponBlock, 0);
                Vector3D ORIGINPOS = weaponBlock.GetPosition();
                var FORWARDPOS = weaponBlock.Position + Base6Directions.GetIntVector(weaponBlock.Orientation.TransformDirection(Base6Directions.Direction.Forward));
                var FORWARD = weaponBlock.CubeGrid.GridIntegerToWorld(FORWARDPOS);
                var FORWARDVECTOR = Vector3D.Normalize(FORWARD - ORIGINPOS);

                Vector3D tmpVECTOR = Vector3D.Rotate(FORWARDVECTOR, AZ);
                Vector3D TURRETVECTOR = Vector3D.Rotate(tmpVECTOR, EL);
                var UPPOS = weaponBlock.Position + Base6Directions.GetIntVector(weaponBlock.Orientation.TransformDirection(Base6Directions.Direction.Up));
                var UP = weaponBlock.CubeGrid.GridIntegerToWorld(UPPOS);
                var UPVECTOR = Vector3D.Normalize(UP - ORIGINPOS);
                Quaternion QUAT_ONE = Quaternion.CreateFromForwardUp(FORWARDVECTOR, UPVECTOR);
                Vector3D TARGETPOS1 = Vector3D.Transform(TURRETVECTOR, QUAT_ONE);
                TARGETPOS1 = Vector3D.Negate(TARGETPOS1);
                EchoString.Append("Aim Vector: " + TARGETPOS1.ToString() + "\n");
            }
            WeaponDefinitions.Clear();
            api.GetAllCoreStaticLaunchers(WeaponDefinitions);
            EchoString.Append("Total Static Weapons registered: " + WeaponDefinitions.Count + "\n");
            WeaponDefinitions.Clear();
            api.GetAllCoreTurrets(WeaponDefinitions);
            EchoString.Append("Total Turret Weapons registered: " + WeaponDefinitions.Count + "\n");
            EchoString.Append("\nGrid Info:\n");
            EchoString.Append("-----------------------\n");
            EchoString.Append("Has GridAi: " + api.HasGridAi(id) + "\n");
            EchoString.Append("GridAi Target Data:\n");
            GetTargetInfo((MyDetectedEntityInfo)api.GetAiFocus(id));
            api.GetSortedThreats(Me, dict);
            
            EchoString.Append("Available Targets: " + dict.Count + "\n");
            Me.CustomData = "";
            Me.CustomData = EchoString.ToString();
            Echo(Me.CustomData);
            */

        }

        //public void GetTargetInfo(MyDetectedEntityInfo info)
        //{
        //    if (info.IsEmpty())
        //    {
        //        EchoString.Append("None\n");
        //        return;
        //    }
        //    EchoString.Append("Id: " + info.EntityId + "\n");
        //    EchoString.Append("Name: " + info.Name + "\n");
        //    EchoString.Append("Type: " + info.Type + "\n");
        //    EchoString.Append("HitPosition: " + (info.HitPosition == null ? "unknown" : info.HitPosition.ToString()) + "\n");
        //    EchoString.Append("Relation: " + info.Relationship.ToString() + "\n");
        //    EchoString.Append("Position: " + info.Position.ToString() + "\n");
        //}
    }
}
