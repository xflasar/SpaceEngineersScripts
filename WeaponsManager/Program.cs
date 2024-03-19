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

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            Echo = EchoToLCD;

            // Fetch a log text panel
            _logOutput = GridTerminalSystem.GetBlockWithName("XDR-Weezel.LCD.Log2 LCD") as IMyTextPanel; // Set the name here so the script can get the lcd to put Echo to
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(WeaponsRails, b => b.CustomName.ToLower().Contains("rail"));
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(Weapons, b => b.CustomName.ToLower().Contains("pdc"));
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

        public static WcPbApi api;

        public List<IMyTerminalBlock> Weapons = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> WeaponsRails = new List<IMyTerminalBlock>();
        //public List<string> targetTypes = new List<string>();
        private StringBuilder EchoString = new StringBuilder();
        //IDictionary<string, int> temp = new Dictionary<string, int>();
        int mode = 0;

        // Does it work? Yeah kinda can be better
        static int CalculateDelay(double totalHeat)
        {
            double heatDelayFactor = 30.0 / (44800.0 - 40000.0);
            double delay = heatDelayFactor * (totalHeat - 40000);

            int result = (int)Math.Max(0, Math.Min(30, delay)); // Ensure the result is between 0 and 30
            return result;
        }

        bool weaponFiring = false;
        int firingDelay = 20;
        int currentWeaponIndex = 0;
        int countdown = 0;

        // Main Method for Firing rail
        void FireAlternateWeapons()
        {
            if(!WeaponsRails[currentWeaponIndex].IsFunctional)
            {
                currentWeaponIndex++;
                return;
            }
            if (!WeaponsRails[currentWeaponIndex].GetValueBool("OnOff"))
            {
                WeaponsRails[currentWeaponIndex].SetValueBool("OnOff", true);
                return;
            }

            if (countdown > 0)
            {
                countdown--;
                return;
            }

            if (!api.IsWeaponReadyToFire(WeaponsRails[currentWeaponIndex]) || weaponFiring)
            {
                return;
            }

            if (!CanShootTarget(WeaponsRails[currentWeaponIndex]))
            {
                return;
            }

            // Fire the current weapon
            api.FireWeaponOnce(WeaponsRails[currentWeaponIndex]);

            countdown = firingDelay;

            // Switch to the next weapon
            currentWeaponIndex = (currentWeaponIndex + 1) % WeaponsRails.Count;
        }


        // Method for checking if we can shoot the target with weapon
        bool CanShootTarget(IMyTerminalBlock weapon)
        {
            try
            {
                // Check if the weapon can shoot the target
                return api.CanShootTarget(weapon, api.GetWeaponTarget(weapon).Value.EntityId, 0);
            }
            catch (Exception ex)
            {
                Echo("Error checking if weapon can shoot target: " + ex.Message);
                return false;
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument != "")
            {
                int.TryParse(argument, out mode);
            }


            _logOutput?.WriteText("");
            api = new WcPbApi();
            try
            {
                api.Activate(Me);
            }
            catch
            {
                Echo("WeaponCore Api is failing! \n Make sure WeaponCore is enabled!"); return;
            }

            EchoString.Clear();

            // PDC Management
            Echo("PDC Management: \nPDC count: " + Weapons.Count.ToString() + "\n");
            if (Weapons.Count > 0)
            {
                MyTuple<bool, int, int> projectiles = api.GetProjectilesLockedOn(Me.EntityId);

                Echo(projectiles.Item1 + ":" + projectiles.Item2 + ":" + projectiles.Item3);
                foreach (var w in Weapons)
                {
                    float heatLevel = api.GetHeatLevel(w);
                    int calculatedDelay = 1;
                    string delayStatus = "";
                    w.SetValue<bool>("WC_Shoot", false);

                    if (heatLevel > 40000f && heatLevel < 44000f)
                    {
                        w.SetValue<bool>("WC_Shoot", true);
                        double totalHeat = heatLevel;
                        calculatedDelay = CalculateDelay(totalHeat);
                    }
                    else if (heatLevel >= 43000f)
                    {
                        w.SetValue<bool>("WC_Shoot", true);
                        calculatedDelay = 30;
                        delayStatus = "!!!!!!! OVERHEATING !!!!!!!!";
                    } else if (projectiles.Item2 < 40)
                    {
                        calculatedDelay = 5;
                    }

                    w.SetValue<float>("Burst Delay", calculatedDelay);

                    EchoString.Append($"Weapon {w.CustomName}  =>  Total Heat: {heatLevel} => Delay: {calculatedDelay} | Actual Delay: {w.GetValue<float>("Burst Delay").ToString()} {delayStatus}\n");
                }
            }

            Echo(EchoString.ToString());

            // Rails Management
            Echo("Current Rail mode: " + mode.ToString());
            if (mode == 0)
            {
                WeaponsRails.ForEach(w =>
                {
                    if (w.GetValueBool("OnOff"))
                    {
                        w.SetValueBool("OnOff", false);
                    }
                });
            }
            else if (mode == 1)
            {
                if (Weapons.Count > 0)
                {
                    FireAlternateWeapons();

                }
            } else if (mode == 2)
            {
                if (api.GetAiFocus(Me.CubeGrid.EntityId).Value.Name != null)
                {
                    if (Weapons.Count > 0)
                    {
                        FireAlternateWeapons();

                    }
                }
                else
                {
                    api.ReleaseAiFocus(Me, Me.CubeGrid.EntityId);
                    Echo("No targets focused!!");
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

        public class WcPbApi
        {
            private Action<ICollection<MyDefinitionId>> _getCoreWeapons;
            private Action<ICollection<MyDefinitionId>> _getCoreStaticLaunchers;
            private Action<ICollection<MyDefinitionId>> _getCoreTurrets;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, IDictionary<string, int>, bool> _getBlockWeaponMap;
            private Func<long, MyTuple<bool, int, int>> _getProjectilesLockedOn;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, IDictionary<MyDetectedEntityInfo, float>> _getSortedThreats;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, ICollection<Sandbox.ModAPI.Ingame.MyDetectedEntityInfo>> _getObstructions;
            private Func<long, int, MyDetectedEntityInfo> _getAiFocus;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool> _setAiFocus;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, bool> _releaseAiFocus;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, MyDetectedEntityInfo> _getWeaponTarget;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int> _setWeaponTarget;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool, int> _fireWeaponOnce;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool, bool, int> _toggleWeaponFire;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, bool, bool, bool> _isWeaponReadyToFire;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, float> _getMaxWeaponRange;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, ICollection<string>, int, bool> _getTurretTargetTypes;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, ICollection<string>, int> _setTurretTargetTypes;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _setBlockTrackingRange;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool> _isTargetAligned;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, MyTuple<bool, Vector3D?>> _isTargetAlignedExtended;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, bool> _canShootTarget;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, int, Vector3D?> _getPredictedTargetPos;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _getHeatLevel;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, float> _currentPowerConsumption;
            private Func<MyDefinitionId, float> _getMaxPower;
            private Func<long, bool> _hasGridAi;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, bool> _hasCoreWeapon;
            private Func<long, float> _getOptimalDps;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, string> _getActiveAmmo;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, string> _setActiveAmmo;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<long, int, ulong, long, Vector3D, bool>> _monitorProjectile;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<long, int, ulong, long, Vector3D, bool>> _unMonitorProjectile;
            private Func<ulong, MyTuple<Vector3D, Vector3D, float, float, long, string>> _getProjectileState;
            private Func<long, float> _getConstructEffectiveDps;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long> _getPlayerController;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Matrix> _getWeaponAzimuthMatrix;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Matrix> _getWeaponElevationMatrix;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, long, bool, bool, bool> _isTargetValid;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, MyTuple<Vector3D, Vector3D>> _getWeaponScope;
            private Func<Sandbox.ModAPI.Ingame.IMyTerminalBlock, MyTuple<bool, bool>> _isInRange;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<int, bool>> _monitorEvents;
            private Action<Sandbox.ModAPI.Ingame.IMyTerminalBlock, int, Action<int, bool>> _unmonitorEvents;
            public bool Activate(Sandbox.ModAPI.Ingame.IMyTerminalBlock pbBlock)
            {
                var dict = pbBlock.GetProperty("WcPbAPI")?.As<IReadOnlyDictionary<string, Delegate>>().GetValue(pbBlock);
                if (dict == null) throw new Exception("WcPbAPI failed to activate");
                return ApiAssign(dict);
            }

            public bool ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
            {
                if (delegates == null)
                    return false;

                AssignMethod(delegates, "GetCoreWeapons", ref _getCoreWeapons);
                AssignMethod(delegates, "GetCoreStaticLaunchers", ref _getCoreStaticLaunchers);
                AssignMethod(delegates, "GetCoreTurrets", ref _getCoreTurrets);
                AssignMethod(delegates, "GetBlockWeaponMap", ref _getBlockWeaponMap);
                AssignMethod(delegates, "GetProjectilesLockedOn", ref _getProjectilesLockedOn);
                AssignMethod(delegates, "GetSortedThreats", ref _getSortedThreats);
                AssignMethod(delegates, "GetObstructions", ref _getObstructions);
                AssignMethod(delegates, "GetAiFocus", ref _getAiFocus);
                AssignMethod(delegates, "SetAiFocus", ref _setAiFocus);
                AssignMethod(delegates, "ReleaseAiFocus", ref _releaseAiFocus);
                AssignMethod(delegates, "GetWeaponTarget", ref _getWeaponTarget);
                AssignMethod(delegates, "SetWeaponTarget", ref _setWeaponTarget);
                AssignMethod(delegates, "FireWeaponOnce", ref _fireWeaponOnce);
                AssignMethod(delegates, "ToggleWeaponFire", ref _toggleWeaponFire);
                AssignMethod(delegates, "IsWeaponReadyToFire", ref _isWeaponReadyToFire);
                AssignMethod(delegates, "GetMaxWeaponRange", ref _getMaxWeaponRange);
                AssignMethod(delegates, "GetTurretTargetTypes", ref _getTurretTargetTypes);
                AssignMethod(delegates, "SetTurretTargetTypes", ref _setTurretTargetTypes);
                AssignMethod(delegates, "SetBlockTrackingRange", ref _setBlockTrackingRange);
                AssignMethod(delegates, "IsTargetAligned", ref _isTargetAligned);
                AssignMethod(delegates, "IsTargetAlignedExtended", ref _isTargetAlignedExtended);
                AssignMethod(delegates, "CanShootTarget", ref _canShootTarget);
                AssignMethod(delegates, "GetPredictedTargetPosition", ref _getPredictedTargetPos);
                AssignMethod(delegates, "GetHeatLevel", ref _getHeatLevel);
                AssignMethod(delegates, "GetCurrentPower", ref _currentPowerConsumption);
                AssignMethod(delegates, "GetMaxPower", ref _getMaxPower);
                AssignMethod(delegates, "HasGridAi", ref _hasGridAi);
                AssignMethod(delegates, "HasCoreWeapon", ref _hasCoreWeapon);
                AssignMethod(delegates, "GetOptimalDps", ref _getOptimalDps);
                AssignMethod(delegates, "GetActiveAmmo", ref _getActiveAmmo);
                AssignMethod(delegates, "SetActiveAmmo", ref _setActiveAmmo);
                AssignMethod(delegates, "MonitorProjectile", ref _monitorProjectile);
                AssignMethod(delegates, "UnMonitorProjectile", ref _unMonitorProjectile);
                AssignMethod(delegates, "GetProjectileState", ref _getProjectileState);
                AssignMethod(delegates, "GetConstructEffectiveDps", ref _getConstructEffectiveDps);
                AssignMethod(delegates, "GetPlayerController", ref _getPlayerController);
                AssignMethod(delegates, "GetWeaponAzimuthMatrix", ref _getWeaponAzimuthMatrix);
                AssignMethod(delegates, "GetWeaponElevationMatrix", ref _getWeaponElevationMatrix);
                AssignMethod(delegates, "IsTargetValid", ref _isTargetValid);
                AssignMethod(delegates, "GetWeaponScope", ref _getWeaponScope);
                AssignMethod(delegates, "IsInRange", ref _isInRange);
                AssignMethod(delegates, "RegisterEventMonitor", ref _monitorEvents);
                AssignMethod(delegates, "UnRegisterEventMonitor", ref _unmonitorEvents);
                return true;
            }

            private void AssignMethod<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field) where T : class
            {
                if (delegates == null)
                {
                    field = null;
                    return;
                }

                Delegate del;
                if (!delegates.TryGetValue(name, out del))
                    throw new Exception($"{GetType().Name} :: Couldn't find {name} delegate of type {typeof(T)}");

                field = del as T;
                if (field == null)
                    throw new Exception(
                        $"{GetType().Name} :: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
            }

            public void GetAllCoreWeapons(ICollection<MyDefinitionId> collection) => _getCoreWeapons?.Invoke(collection);

            public void GetAllCoreStaticLaunchers(ICollection<MyDefinitionId> collection) =>
                _getCoreStaticLaunchers?.Invoke(collection);

            public void GetAllCoreTurrets(ICollection<MyDefinitionId> collection) => _getCoreTurrets?.Invoke(collection);

            public bool GetBlockWeaponMap(Sandbox.ModAPI.Ingame.IMyTerminalBlock weaponBlock, IDictionary<string, int> collection) =>
                _getBlockWeaponMap?.Invoke(weaponBlock, collection) ?? false;

            public MyTuple<bool, int, int> GetProjectilesLockedOn(long victim) =>
                _getProjectilesLockedOn?.Invoke(victim) ?? new MyTuple<bool, int, int>();

            public void GetSortedThreats(Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock, IDictionary<MyDetectedEntityInfo, float> collection) =>
                _getSortedThreats?.Invoke(pBlock, collection);
            public void GetObstructions(Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock, ICollection<Sandbox.ModAPI.Ingame.MyDetectedEntityInfo> collection) =>
                _getObstructions?.Invoke(pBlock, collection);
            public MyDetectedEntityInfo? GetAiFocus(long shooter, int priority = 0) => _getAiFocus?.Invoke(shooter, priority);

            public bool SetAiFocus(Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock, long target, int priority = 0) =>
                _setAiFocus?.Invoke(pBlock, target, priority) ?? false;
            public bool ReleaseAiFocus(Sandbox.ModAPI.Ingame.IMyTerminalBlock pBlock, long playerId) =>
                _releaseAiFocus?.Invoke(pBlock, playerId) ?? false;
            public MyDetectedEntityInfo? GetWeaponTarget(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId = 0) =>
                _getWeaponTarget?.Invoke(weapon, weaponId);

            public void SetWeaponTarget(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long target, int weaponId = 0) =>
                _setWeaponTarget?.Invoke(weapon, target, weaponId);

            public void FireWeaponOnce(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, bool allWeapons = true, int weaponId = 0) =>
                _fireWeaponOnce?.Invoke(weapon, allWeapons, weaponId);

            public void ToggleWeaponFire(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, bool on, bool allWeapons, int weaponId = 0) =>
                _toggleWeaponFire?.Invoke(weapon, on, allWeapons, weaponId);

            public bool IsWeaponReadyToFire(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId = 0, bool anyWeaponReady = true,
                bool shootReady = false) =>
                _isWeaponReadyToFire?.Invoke(weapon, weaponId, anyWeaponReady, shootReady) ?? false;

            public float GetMaxWeaponRange(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
                _getMaxWeaponRange?.Invoke(weapon, weaponId) ?? 0f;

            public bool GetTurretTargetTypes(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, IList<string> collection, int weaponId = 0) =>
                _getTurretTargetTypes?.Invoke(weapon, collection, weaponId) ?? false;

            public void SetTurretTargetTypes(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, IList<string> collection, int weaponId = 0) =>
                _setTurretTargetTypes?.Invoke(weapon, collection, weaponId);

            public void SetBlockTrackingRange(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, float range) =>
                _setBlockTrackingRange?.Invoke(weapon, range);

            public bool IsTargetAligned(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetEnt, int weaponId) =>
                _isTargetAligned?.Invoke(weapon, targetEnt, weaponId) ?? false;

            public MyTuple<bool, Vector3D?> IsTargetAlignedExtended(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetEnt, int weaponId) =>
                _isTargetAlignedExtended?.Invoke(weapon, targetEnt, weaponId) ?? new MyTuple<bool, Vector3D?>();

            public bool CanShootTarget(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetEnt, int weaponId) =>
                _canShootTarget?.Invoke(weapon, targetEnt, weaponId) ?? false;

            public Vector3D? GetPredictedTargetPosition(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetEnt, int weaponId) =>
                _getPredictedTargetPos?.Invoke(weapon, targetEnt, weaponId) ?? null;

            public float GetHeatLevel(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) => _getHeatLevel?.Invoke(weapon) ?? 0f;
            public float GetCurrentPower(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) => _currentPowerConsumption?.Invoke(weapon) ?? 0f;
            public float GetMaxPower(MyDefinitionId weaponDef) => _getMaxPower?.Invoke(weaponDef) ?? 0f;
            public bool HasGridAi(long entity) => _hasGridAi?.Invoke(entity) ?? false;
            public bool HasCoreWeapon(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) => _hasCoreWeapon?.Invoke(weapon) ?? false;
            public float GetOptimalDps(long entity) => _getOptimalDps?.Invoke(entity) ?? 0f;

            public string GetActiveAmmo(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
                _getActiveAmmo?.Invoke(weapon, weaponId) ?? null;

            public void SetActiveAmmo(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId, string ammoType) =>
                _setActiveAmmo?.Invoke(weapon, weaponId, ammoType);

            public void MonitorProjectileCallback(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId, Action<long, int, ulong, long, Vector3D, bool> action) =>
                _monitorProjectile?.Invoke(weapon, weaponId, action);

            public void UnMonitorProjectileCallback(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId, Action<long, int, ulong, long, Vector3D, bool> action) =>
                _unMonitorProjectile?.Invoke(weapon, weaponId, action);

            public MyTuple<Vector3D, Vector3D, float, float, long, string> GetProjectileState(ulong projectileId) =>
                _getProjectileState?.Invoke(projectileId) ?? new MyTuple<Vector3D, Vector3D, float, float, long, string>();

            public float GetConstructEffectiveDps(long entity) => _getConstructEffectiveDps?.Invoke(entity) ?? 0f;

            public long GetPlayerController(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon) => _getPlayerController?.Invoke(weapon) ?? -1;

            public Matrix GetWeaponAzimuthMatrix(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
                _getWeaponAzimuthMatrix?.Invoke(weapon, weaponId) ?? Matrix.Zero;

            public Matrix GetWeaponElevationMatrix(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
                _getWeaponElevationMatrix?.Invoke(weapon, weaponId) ?? Matrix.Zero;

            public bool IsTargetValid(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, long targetId, bool onlyThreats, bool checkRelations) =>
                _isTargetValid?.Invoke(weapon, targetId, onlyThreats, checkRelations) ?? false;

            public MyTuple<Vector3D, Vector3D> GetWeaponScope(Sandbox.ModAPI.Ingame.IMyTerminalBlock weapon, int weaponId) =>
                _getWeaponScope?.Invoke(weapon, weaponId) ?? new MyTuple<Vector3D, Vector3D>();
            // terminalBlock, Threat, Other, Something 
            public MyTuple<bool, bool> IsInRange(Sandbox.ModAPI.Ingame.IMyTerminalBlock block) =>
                _isInRange?.Invoke(block) ?? new MyTuple<bool, bool>();
            public void MonitorEvents(Sandbox.ModAPI.Ingame.IMyTerminalBlock entity, int partId, Action<int, bool> action) =>
                _monitorEvents?.Invoke(entity, partId, action);

            public void UnMonitorEvents(Sandbox.ModAPI.Ingame.IMyTerminalBlock entity, int partId, Action<int, bool> action) =>
                _unmonitorEvents?.Invoke(entity, partId, action);

        }
    }
}
