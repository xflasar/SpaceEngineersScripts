using System;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        private bool _batteryOn = false;
        private double _countdown = 0;
        private int _currentWeaponIndex = 0;
        private double _firingDelay = 18;
        private bool _weaponFiring = false;
        private bool _weaponsRailsSetToAI = true;
        private int _weaponTargetFails = 0;

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
                _weaponTargetFails++;
                return false;
            }
        }

        double CountFiringDelay()
        {
            double distance = Vector3D.Distance(api.GetWeaponTarget(WeaponsRails[_currentWeaponIndex]).Value.Position, Me.GetPosition());
            if (distance < 10000f)
            {
                _firingDelay = Math.Round(12 + (distance / 10000) * 6);
            }

            return _firingDelay;
        }

        // Main Method for Firing rail
        void FireAlternateWeapons()
        {
            if (_weaponsRailsSetToAI)
            {
                _weaponsRailsSetToAI = false;
                _weaponTargetFails = 0;
                WeaponsRails.ForEach(weap =>
                {
                    weap.SetValue<Int64>("WC_Shoot Mode", 2);
                });
            }

            if (_weaponTargetFails > 0)
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
                Echo("Weapon Failed to fire: " + _weaponTargetFails + "/50");
            }

            Echo("Weapon #: " + _currentWeaponIndex + " Cooldown:" + _countdown);
            if (_countdown > 0)
            {
                _countdown -= 1f;
                return;
            }

            if (_weaponTargetFails > 50)
            {
                mode = "AIShoot";
            }

            if (!WeaponsRails[_currentWeaponIndex].GetValueBool("OnOff"))
            {
                WeaponsRails[_currentWeaponIndex].SetValueBool("OnOff", true);
                return;
            }

            if (WeaponsRails[_currentWeaponIndex].GetValueBool("WC_FocusFire"))
            {
                if (api.GetAiFocus(Me.EntityId) == null)
                {
                    _currentWeaponIndex = (_currentWeaponIndex + 1) % WeaponsRails.Count;
                    return;
                }
            }

            if (!WeaponsRails[_currentWeaponIndex].IsFunctional)
            {
                _currentWeaponIndex = (_currentWeaponIndex + 1) % WeaponsRails.Count;
                return;
            }

            if (!api.IsWeaponReadyToFire(WeaponsRails[_currentWeaponIndex]) || _weaponFiring)
            {
                _currentWeaponIndex = (_currentWeaponIndex + 1) % WeaponsRails.Count;
                return;
            }

            if (!CanShootTarget(WeaponsRails[_currentWeaponIndex]))
            {
                _currentWeaponIndex = (_currentWeaponIndex + 1) % WeaponsRails.Count;
                return;
            }

            if (!api.IsTargetAligned(WeaponsRails[_currentWeaponIndex],
                    api.GetWeaponTarget(WeaponsRails[_currentWeaponIndex]).Value.EntityId, 0))
            {
                _currentWeaponIndex = (_currentWeaponIndex + 1) % WeaponsRails.Count;
                return;
            }

            if (!_batteryOn)
            {
                batteries.ForEach(battery =>
                {
                    IMyBatteryBlock bat = battery as IMyBatteryBlock;
                    bat.ChargeMode = ChargeMode.Discharge;
                });
            }

            // Fire the current weapon
            _weaponTargetFails = 0;
            api.FireWeaponOnce(WeaponsRails[_currentWeaponIndex]);

            _countdown = CountFiringDelay();

            // Switch to the next weapon
            _currentWeaponIndex = (_currentWeaponIndex + 1) % WeaponsRails.Count;
        }

        void SetRailsAutofire()
        {
            if (_weaponsRailsSetToAI) return;

            WeaponsRails.ForEach(w =>
            {
                w.SetValue<Int64>("WC_Shoot Mode", 0);
            });
            _weaponsRailsSetToAI = true;
        }
    }
}
