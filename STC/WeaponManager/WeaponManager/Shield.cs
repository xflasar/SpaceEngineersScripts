using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage;
using VRageMath;
using VRageRender.Messages;

namespace IngameScript
{
    partial class Program: MyGridProgram
    {
        public ShieldApi _shield;

        private float _shieldHP = float.MinValue;
        private float _shieldMaxHP = float.MinValue;
        private float _shieldRecharge = float.MinValue;
        private double _shieldPercentage = Double.MinValue;
        private double _shieldHeat = Double.MinValue;
        private float _shieldPowerUsage = Single.MinValue;
        public float _shieldPowerSet = float.MinValue;
        private string _shieldStatus = "";
        public bool _shieldBooted = false;

        public bool _shieldShuntingDefault = false;

        void BootUp()
        {
            if (!_shield.IsShieldUp())
            {
                mainShieldController.SetValue<bool>("DS-C_ToggleShield", true);
            }

            // Always off it's useless
            mainShieldController.SetValueBool("DS-C_AutoManage", false);
            // Used for Batteries fighting for power ether you get recharge or batteries will take the power and no recharge
            mainShieldController.SetValueBool("DS-C_UseBatteries", true);
            // Used for shunting shields
            mainShieldController.SetValueBool("DS-C_SideRedirect", true);
            // Protections
            mainShieldController.SetValueBool("DS-M_ModulateEmpProt", true);
            mainShieldController.SetValueFloat("DS-M_DamageModulation", 3); // not sure its value type
            mainShieldController.SetValueBool("DS-M_ModulateReInforceProt", true); // not sure its value type
            _shieldBooted = true;

        }

        void CheckShield() {
            if (!_shield.IsShieldUp())
            {
                mainShieldController.SetValue<bool>("DS-C_ToggleShield", true);
            }
            
            if (_shieldHeat > 50)
            {
                _antennas.ForEach(ant =>
                {
                    ant.HudText = $"Shield Heat: {_shieldHeat}% ACTIVATE HEATSINK!! || Shield {_shieldPercentage}%";
                });
            }
            else
            {
                _antennas.ForEach(ant =>
                {
                    ant.HudText = $"Shield at {_shieldPercentage}%";
                });
            }
        }

        void GetData()
        {
            // Get Values from Shield
            _shieldHP = _shield.GetCharge() * _shield.HpToChargeRatio();
            _shieldMaxHP = _shield.GetMaxCharge() * _shield.HpToChargeRatio();
            _shieldRecharge = _shield.GetChargeRate();
            _shieldPercentage = _shield.GetShieldPercent();
            _shieldHeat = _shield.GetShieldHeat();
            _shieldPowerUsage = _shield.GetPowerUsed();
            _shieldStatus = _shield.ShieldStatus();
            _shieldPowerSet = mainShieldController.GetValueFloat("DS-C_PowerWatts");
        }

        // Unused in Dedicated servers due to Developer decision
        #region ShieldShunting

        void DefaultShuntingShields()
        {
            Echo("run");
            mainShieldController.ApplyAction("DS-C_TopShield_ShuntToggle");
            Echo("run1 complete");
            mainShieldController.ApplyAction("DS-C_BottomShield_ShuntOff");
            Echo("run2 complete");
            mainShieldController.ApplyAction("DS-C_LeftShield_ShuntOff");
            Echo("run3 complete");
            mainShieldController.ApplyAction("DS-C_RightShield_ShuntOff");
            Echo("run4 complete");
            mainShieldController.ApplyAction("DS-C_FrontShield_ShuntOff");
            Echo("run5 complete");
            mainShieldController.ApplyAction("DS-C_BackShield_ShuntOff");
            Echo("run complete");
            _shieldShuntingDefault = true;

        }
        public enum RelativePosition
        {
            Left,
            Right,
            Forward,
            Backward,
            Top,
            Bottom,
            Aligned
        }

        public static RelativePosition GetRelativePosition(Vector3D shipPosition, Vector3D pointPosition)
        {
            Vector3D relativePosition = pointPosition - shipPosition;

            if (Math.Abs(relativePosition.X) < double.Epsilon &&
                Math.Abs(relativePosition.Y) < double.Epsilon &&
                Math.Abs(relativePosition.Z) < double.Epsilon)
            {
                return RelativePosition.Aligned;
            }

            if (Math.Abs(relativePosition.X) > Math.Abs(relativePosition.Y) &&
                Math.Abs(relativePosition.X) > Math.Abs(relativePosition.Z))
            {
                return relativePosition.X > 0 ? RelativePosition.Right : RelativePosition.Left;
            }
            else if (Math.Abs(relativePosition.Y) > Math.Abs(relativePosition.X) &&
                     Math.Abs(relativePosition.Y) > Math.Abs(relativePosition.Z))
            {
                return relativePosition.Y > 0 ? RelativePosition.Top : RelativePosition.Bottom;
            }
            else
            {
                return relativePosition.Z > 0 ? RelativePosition.Forward : RelativePosition.Backward;
            }
        }
        public void ShuntShields(Vector3D shipPosition, IMyTerminalBlock mainShieldController)
        {
            List<Vector3D> enemyPositions = new List<Vector3D>();

            EchoString.Append("Enemy: " + enemies.Count());
            enemies.Keys.ToList().ForEach(targetEnemies =>
            {
                enemyPositions.Add(targetEnemies.Position);
            });

            bool enemyDetectedTop = CheckForEnemyInDirection(RelativePosition.Top, shipPosition, enemyPositions);
            bool enemyDetectedBottom = CheckForEnemyInDirection(RelativePosition.Bottom, shipPosition, enemyPositions);
            bool enemyDetectedLeft = CheckForEnemyInDirection(RelativePosition.Left, shipPosition, enemyPositions);
            bool enemyDetectedRight = CheckForEnemyInDirection(RelativePosition.Right, shipPosition, enemyPositions);
            bool enemyDetectedFront = CheckForEnemyInDirection(RelativePosition.Forward, shipPosition, enemyPositions);
            bool enemyDetectedBack = CheckForEnemyInDirection(RelativePosition.Backward, shipPosition, enemyPositions);

            // Printout
            if (enemyDetectedFront) {Echo("Shunting Front Shield!");}
            if (enemyDetectedBack) Echo("Shunting Back Shield!");
            if (enemyDetectedLeft) Echo("Shunting Left Shield!");
            if (enemyDetectedRight) Echo("Shunting Right Shield!");
            if (enemyDetectedTop) Echo("Shunting Top Shield!");
            if (enemyDetectedBottom) Echo("Shunting Bottom Shield!");

            mainShieldController.SetValueBool("DS-C_TopShield", !enemyDetectedTop);
            mainShieldController.SetValueBool("DS-C_BottomShield", !enemyDetectedBottom);
            mainShieldController.SetValueBool("DS-C_LeftShield", !enemyDetectedLeft);
            mainShieldController.SetValueBool("DS-C_RightShield", !enemyDetectedRight);
            mainShieldController.SetValueBool("DS-C_FrontShield", !enemyDetectedFront);
            mainShieldController.SetValueBool("DS-C_BackShield", !enemyDetectedBack);
        }

        private static bool CheckForEnemyInDirection(RelativePosition direction, Vector3D shipPosition, List<Vector3D> enemyPositions)
        {
            foreach (Vector3D enemyPosition in enemyPositions)
            {
                RelativePosition enemyRelativePosition = GetRelativePosition(shipPosition, enemyPosition);
                if (enemyRelativePosition == direction || enemyRelativePosition == RelativePosition.Aligned)
                {
                    return true; // Enemy detected in the specified direction
                }
            }
            return false; // No enemy detected in the specified direction
        }
        #endregion

        void PrintProperties()
        {
            List<ITerminalProperty> props = new List<ITerminalProperty>();
            mainShieldController.GetProperties(props);
            foreach (var terminalProperty in props)
            {
                EchoString.Append(terminalProperty.Id + ":" + terminalProperty.TypeName + "\n");
            }
        }

        void PrintShieldStatus()
        {
            string shieldStatus = "";
            if (_shield.IsShieldUp() && _shieldHP != 0) shieldStatus = "ONLINE";

            _shieldRecharge = 5;

            if (_shieldHeat >= 50) shieldStatus = "OVERHEATING";

            EchoString.Append($"==== Shield {shieldStatus} ==> {_shieldStatus} ==== {Helper_Functions.FormatText(_shieldPercentage)}% ====\n");

            if (_shieldHeat > 0)
                EchoString.Append($"\n==== Shield Heat {_shieldHeat}% ====\n");

            EchoString.Append($"\nShield Amount: {Helper_Functions.FormatText(_shieldHP)}/{Helper_Functions.FormatText(_shieldMaxHP)}\n\n");

            // Shield Info print
            EchoString.Append("---------- Shield Info ----------\n");

            bool shieldRecharge = _shieldRecharge > 0;

            EchoString.Append($"Shield Recharging: {shieldRecharge}\n");
            
            EchoString.Append($"Shield Power Usage: {Helper_Functions.FormatText(_shieldPowerUsage)} || PowerScale: {_shieldPowerSet} GW\n");
            
            string shieldModulationState = "";

            if (_shieldModulator.GetValueFloat("DS-M_DamageModulation") > 0)
            {
                shieldModulationState = "Kinetic"; 
            } else if (_shieldModulator.GetValueFloat("DS-M_DamageModulation") == 0)
            {
                shieldModulationState = "Balanced";
            }
            else
            {
                shieldModulationState = "Energy";
            }

            EchoString.Append($"Shield Modulation: {shieldModulationState} || Aggreation: {_shieldModulator.GetValueBool("DS-M_AggreateModulation")}");
        }
    }
}