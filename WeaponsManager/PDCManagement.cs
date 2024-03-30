using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using VRage;
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {

        List<long> pdcHeat10To30k = new List<long>();
        List<long> pdcHeat30To40k = new List<long>();
        List<long> pdcHeat40kOver = new List<long>();

        float CalculateDelay(double totalHeat)
        {
            float result = (float)(60 / (1 + Math.Exp(Math.Pow((-totalHeat + 44800) / 1000, Math.Sqrt(2)))));
            // Ensure the result is between 0 and 30
            if (result <= 1) return 1;
            return result;
        }

        void PDCMain()
        {
            MyTuple<bool, int, int> projectiles = api.GetProjectilesLockedOn(Me.EntityId);

            // Torpedos in AO
            Echo("===Torpedo Management===");

            if (projectiles.Item1)
            {
                Echo("== !! Torpedos in AO !! ==");
                Echo($"Torpedos: {projectiles.Item2}");
            }

            // PDC HeatLevel render
            Echo("\n===PDC Heat Level===");
            Echo($"PDC 10k - 30k: {pdcHeat10To30k.Count} || PDC 30k - 40k: {pdcHeat30To40k.Count} || PDC 40k+: {pdcHeat40kOver.Count}\n=======================\n");


            foreach (var w in Weapons)
            {
                float heatLevel = api.GetHeatLevel(w);
                float calculatedDelay = 1;
                string delayStatus = "";
                w.SetValue<bool>("WC_Shoot", true);
                double totalHeat = heatLevel;
                if (totalHeat > 0)
                {
                    calculatedDelay = CalculateDelay(totalHeat);
                }
                w.SetValue<float>("Burst Delay", calculatedDelay);

                if (Vector3D.Distance(api.GetAiFocus(Me.EntityId).Value.Position, Me.GetPosition()) < 3000 && projectiles.Item1)
                {
                    w.SetValueBool("WC_Grids", false);
                }
                //else
                //{
                //    w.SetValueBool("WC_Grids", true);
                //}

                AssignWeaponToList(totalHeat, w);

                if (totalHeat > 30000)
                {
                    EchoString.Append(
                        $"Weapon {w.CustomName}  =>  Total Heat: {heatLevel} => Delay: {calculatedDelay} | Actual Delay: {w.GetValue<float>("Burst Delay")} {delayStatus} | Time to Overheat: {Math.Ceiling((44800 - totalHeat) / 4000)}s\n");
                }
            }
        }

        void RemoveEntityFromAllLists(long entityId)
        {
            pdcHeat10To30k.Remove(entityId);
            pdcHeat30To40k.Remove(entityId);
            pdcHeat40kOver.Remove(entityId);
        }

        void AssignWeaponToList(double totalHeat, IMyTerminalBlock w)
        {
            RemoveEntityFromAllLists(w.EntityId);

            if (totalHeat >= 10000 && totalHeat < 30000)
            {
                pdcHeat10To30k.Add(w.EntityId);
            }
            else if (totalHeat >= 30000 && totalHeat < 40000)
            {
                if (!pdcHeat10To30k.Contains(w.EntityId))
                {
                    pdcHeat10To30k.Remove(w.EntityId);
                }
                pdcHeat30To40k.Add(w.EntityId);
            }
            else if (totalHeat >= 40000)
            {
                if (pdcHeat10To30k.Contains(w.EntityId))
                {
                    pdcHeat10To30k.Remove(w.EntityId);
                }
                pdcHeat40kOver.Add(w.EntityId);
            }
            else
            {
                if (pdcHeat10To30k.Contains(w.EntityId))
                {
                    pdcHeat10To30k.Remove(w.EntityId);
                }
            }
        }

    }
}
