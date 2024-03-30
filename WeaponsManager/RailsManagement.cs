using System;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        private bool weaponFiring = false;
        private double firingDelay = 18;
        private int currentWeaponIndex = 0;
        private double countdown = 0;
        private int weaponTargetFails = 0;
        private bool weaponsRailsSetToAI = true;

        // Main Method for Firing rail
        void FireAlternateWeapons()
        {
            if (weaponsRailsSetToAI)
            {
                weaponsRailsSetToAI = false;
                weaponTargetFails = 0;
                WeaponsRails.ForEach(weap =>
                {
                    weap.SetValue<Int64>("WC_Shoot Mode", 2);
                });
            }

            if (weaponTargetFails > 0) Echo("Weapon Failed to fire: " + weaponTargetFails + "/50");

            Echo("Weapon #: " + currentWeaponIndex + " Cooldown:" + countdown);
            if (countdown > 0)
            {
                countdown -= 1f;
                return;
            }

            if (weaponTargetFails > 50)
            {
                mode = "AIShoot";
            }

            if (!WeaponsRails[currentWeaponIndex].GetValueBool("OnOff"))
            {
                WeaponsRails[currentWeaponIndex].SetValueBool("OnOff", true);
                return;
            }

            if (WeaponsRails[currentWeaponIndex].GetValueBool("WC_FocusFire"))
            {
                if (api.GetAiFocus(Me.EntityId) == null)
                {
                    currentWeaponIndex = (currentWeaponIndex + 1) % WeaponsRails.Count;
                    return;
                }
            }

            if (!WeaponsRails[currentWeaponIndex].IsFunctional)
            {
                currentWeaponIndex = (currentWeaponIndex + 1) % WeaponsRails.Count;
                return;
            }

            if (!api.IsWeaponReadyToFire(WeaponsRails[currentWeaponIndex]) || weaponFiring)
            {
                currentWeaponIndex = (currentWeaponIndex + 1) % WeaponsRails.Count;
                return;
            }

            if (!CanShootTarget(WeaponsRails[currentWeaponIndex]))
            {
                currentWeaponIndex = (currentWeaponIndex + 1) % WeaponsRails.Count;
                return;
            }

            if (!api.IsTargetAligned(WeaponsRails[currentWeaponIndex],
                    api.GetWeaponTarget(WeaponsRails[currentWeaponIndex]).Value.EntityId, 0))
            {
                currentWeaponIndex = (currentWeaponIndex + 1) % WeaponsRails.Count;
                return;
            }

            // Fire the current weapon
            weaponTargetFails = 0;
            api.FireWeaponOnce(WeaponsRails[currentWeaponIndex]);

            countdown = CountFiringDelay();

            // Switch to the next weapon
            currentWeaponIndex = (currentWeaponIndex + 1) % WeaponsRails.Count;
        }

        double CountFiringDelay()
        {
            double distance = Vector3D.Distance(api.GetWeaponTarget(WeaponsRails[currentWeaponIndex]).Value.Position, Me.GetPosition());
            if (distance < 10000f)
            {
                firingDelay = Math.Round(12 + (distance / 10000) * 6);
            }

            return firingDelay;
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
                weaponTargetFails++;
                return false;
            }
        }

        void SetRailsAutofire()
        {
            if(weaponsRailsSetToAI) return;

            WeaponsRails.ForEach(w =>
            {
                w.SetValue<Int64>("WC_Shoot Mode", 0);
            });
            weaponsRailsSetToAI = true;
        }
    }
}
