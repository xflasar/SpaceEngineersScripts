using Sandbox.Game.Gui;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Web;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        List<IMyCargoContainer> containers = new List<IMyCargoContainer>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            Echo = EchoToLCD;

            // Fetch a log text panel
            _logOutput = GridTerminalSystem.GetBlockWithName("GC-12-Earthshaker.Log LCD") as IMyTextPanel; // Set the name here so the script can get the lcd to put Echo to


            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(containers, c => c.CubeGrid == Me.CubeGrid);

            mainDrivesCompsMin.Add("SteelPlate", 2400);
            mainDrivesCompsMin.Add("Construction", 900);
            mainDrivesCompsMin.Add("MetalGrid", 720);
            mainDrivesCompsMin.Add("Thrust", 720);
            mainDrivesCompsMin.Add("PowerCell", 640);
            mainDrivesCompsMin.Add("FusionCoil", 148);
            mainDrivesCompsMin.Add("Superconductor", 640);
            mainDrivesCompsMin.Add("LargeTube", 360);


        }

        Dictionary<string, MyFixedPoint> mainDrivesCompsMin = new Dictionary<string, MyFixedPoint>();

        Dictionary<string, MyFixedPoint> items = new Dictionary<string, MyFixedPoint>();
        Dictionary<string, int> itemsMaxRepair = new Dictionary<string, int>();

        IMyTextPanel _logOutput;

        public void EchoToLCD(string text)
        {
            // Append the text and a newline to the logging LCD
            // A nice little C# trick here:
            // - The ?. after _logOutput means "call only if _logOutput is not null".
            //_logOutput?.WriteText("");
            _logOutput?.WriteText($"{text}\n", true);
        }

        int iteration = 0;

        public void Main(string argument, UpdateType updateSource)
        {
            iteration++;
            _logOutput?.WriteText("");
            Echo(iteration.ToString());
            items.Clear();
            GetItems();

            Report rep = GetCouterRepairs();
            
            bool notEnougMats = PrintOut();
            
            if (rep != null)
            {
                Echo(rep.PrintOut());
            }

            if(notEnougMats)
            {
                Echo("Not Enough Mats for repair drives!!");
            }
        }

        class Report
        {
            string itemName = "";
            int lowestRepair = int.MaxValue;

            public void ChangeLR (string itemname, int lowestrepair)
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

        // checks if item is in mainDriveCompsMin dictionary and if it has more than value than mainDriveCompsMin[item.Key] then if it has it adds into itemsMaxRepair the item name and its calculated how many times it can repair that drive
        Report GetCouterRepairs()
        {
            Report rep = new Report();
            itemsMaxRepair.Clear();
            foreach (var item in items)
            {
                if (mainDrivesCompsMin.Keys.Contains(item.Key) && (items[item.Key] - mainDrivesCompsMin[item.Key] > mainDrivesCompsMin[item.Key]))
                {
                    int repairTimes = (int)((double)items[item.Key] / (double)mainDrivesCompsMin[item.Key]);
                    
                    if (rep.GetLowestRepair() > repairTimes)
                    {
                        rep.ChangeLR(item.Key ,repairTimes);
                    }
                    
                    itemsMaxRepair.Add(item.Key, repairTimes);
                }
            }
            
            

            return rep;
        }

        // We get all items we have in the grid inventory and set them to items dictionary
        void GetItems ()
        {
            containers.ForEach(c =>
            {
                VRage.Game.ModAPI.Ingame.IMyInventory inventory = c.GetInventory();
                if (inventory != null)
                {

                    for (int slot = 0; slot < inventory.ItemCount; slot++)
                    {
                        if (inventory.IsItemAt(slot))
                        {
                            var item = inventory.GetItemAt(slot);
                            if (item != null && item.HasValue)
                            {
                                if (!items.ContainsKey(item.Value.Type.SubtypeId))
                                {
                                    items.Add(item.Value.Type.SubtypeId, item.Value.Amount);
                                }
                                else
                                {
                                    items[item.Value.Type.SubtypeId] += item.Value.Amount;
                                }
                            }

                        }
                    }
                }
            });
        }

        // Prints out mainDrivesCompsMin Dictionary components in a format of: ItemName: itemAmount/mainDrivesCompsMinAmount can repair itemsMaxRepairTimes
        bool PrintOut()
        {
            bool notEnougMats = false;
            items.Keys.ToList().ForEach(key =>
            {
                if (mainDrivesCompsMin.Keys.Contains(key))
                {
                    Echo(key + ": " + items[key] + "/" + mainDrivesCompsMin[key] + " can repair " + itemsMaxRepair[key]);
                    if (items[key] < mainDrivesCompsMin[key])
                    {
                        Echo("Missing " + (items[key] - mainDrivesCompsMin[key]) + " of " + key);
                        notEnougMats |= true;
                    }
                }
            });

            return notEnougMats;
        }
    }
}
