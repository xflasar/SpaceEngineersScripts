using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program: MyGridProgram
    {
        class Report
        {
            string itemName = "";
            int lowestRepair = int.MaxValue;

            public void ChangeLR(string itemname, int lowestrepair)
            {
                itemName = itemname;
                lowestRepair = lowestrepair;
            }

            public int GetLowestRepair()
            {
                return lowestRepair;
            }

            public string GetItemName()
            {
                return itemName;
            }

            public string PrintOut()
            {
                return "Effectively can repair " + lowestRepair + " due to " + itemName + " being low!!";
            }
        }

        // Loops items and finds specialItems in it and echoes it
        void PrintOutSpecialItems()
        {
            Echo("\n Special Items:");
            foreach (var item in items)
            {
                specialItems.ForEach(sI =>
                {
                    if (item.Key.Contains(sI))
                    {
                        Echo(item.Key + ": " + item.Value);
                    }
                });
            }
        }

        Report GetCouterRepairs()
        {
            Report rep = new Report();
            itemsMaxRepair.Clear();
            foreach (var item in items)
            {
                if (mainDrivesCompsMin.Keys.Contains(item.Key))
                {
                    if (items[item.Key] - mainDrivesCompsMin[item.Key] >= 0)
                    {
                        int repairTimes = (int)Math.Floor((double)items[item.Key] / (double)mainDrivesCompsMin[item.Key]);

                        if (rep.GetLowestRepair() > repairTimes)
                        {
                            rep.ChangeLR(item.Key, repairTimes);
                        }

                        itemsMaxRepair.Add(item.Key, repairTimes);
                    }
                    else
                    {
                        itemsMaxRepair.Add(item.Key, 0);
                    }
                }
            }



            return rep;
        }
        
        // Prints out mainDrivesCompsMin Dictionary components in a format of: ItemName: itemAmount/mainDrivesCompsMinAmount can repair itemsMaxRepairTimes
        bool PrintOut()
        {
            int requiredComps = mainDrivesCompsMin.Count;
            int counterCompsPresent = 0;
            bool notEnoughMats = false;

            mainDrivesCompsMin.Keys.ToList().ForEach(comp =>
            {
                int itemsValue = 0;
                int mainDrivesCompsMinValue = (int)mainDrivesCompsMin[comp];
                int missingAmount = Math.Max(0, mainDrivesCompsMinValue - itemsValue);

                if (items.Keys.Contains(comp) && !items.Keys.Contains("MCRN"))
                {
                    counterCompsPresent++;

                    // Convert VRage.MyFixedPoint to numeric types
                    itemsValue = (int)items[comp];
                    mainDrivesCompsMinValue = (int)mainDrivesCompsMin[comp];

                    // Use Math.Max to calculate positive difference
                    missingAmount = Math.Max(0, mainDrivesCompsMinValue - itemsValue);

                    if (itemsValue > mainDrivesCompsMinValue * 1.5 && mode == 1)
                    {
                        Echo(itemsValue.ToString());
                        Echo(mainDrivesCompsMinValue.ToString());
                        Echo((itemsValue - mainDrivesCompsMinValue).ToString());
                        int amountToThrow = itemsValue - mainDrivesCompsMinValue - 2;
                        ThrowExcessCargo(comp, amountToThrow);
                    }

                    Echo($"{comp}: {itemsValue}/{mainDrivesCompsMinValue} can repair {itemsMaxRepair[comp]}");

                    if (missingAmount > 0)
                    {
                        Echo($"Missing {missingAmount} of {comp}");
                        notEnoughMats = true;
                    }
                }
                else
                {
                    Echo($"{comp}: {itemsValue}/{mainDrivesCompsMinValue} Missing: {missingAmount}");
                }
            });

            if (counterCompsPresent < requiredComps)
            {
                notEnoughMats = true;
            }

            return notEnoughMats;
        }
    }
}
