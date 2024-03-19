using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.WorldEnvironment.Modules;
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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        /*
         * This script is used for having idea how many times can Eipstien drives be repaired depending on current amount of components in the ship.
         * If ship doesn't have enough materials it will warn with basic message of Not enough components to repair drives!.
         * Each Component that is found depending on mainDrivesCompsMin which is Dictionary of Items needed for building one Scirroco Eipstein Drive will show how many times you can repair/build 1 whole Eipstein.
         * 
         * Second feature is listing SpecialItems which comes from hunting like MCRN,UNN and other items.
         * This is only just getting the item amount and showing them on screen
         * 
         * TODO:
         * - We can just do one loop of items and find and assign wanted components and special items in one go no need for own forEach loop
         * - Optimize Method for transfering items to be ejected to connectors for ejection ( Right now it just mostly overshoots and ejects more than needed items
         */
        List<IMyCargoContainer> containers = new List<IMyCargoContainer>();
        List<IMyShipConnector> ejectConnectors = new List<IMyShipConnector>();
        List<IMyShipConnector> myShipConnectors = new List<IMyShipConnector>();
        List<MyInventoryItem?> inventoryC = new List<MyInventoryItem?>();

        MyIni _ini = new MyIni();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            Echo = EchoToLCD;

            // Fetch a log text panel
            _logOutput = GridTerminalSystem.GetBlockWithName("XDR-Weezel.LCD.Log LCD") as IMyTextPanel; // Set the name here so the script can get the lcd to put Echo to

            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(myShipConnectors, b => b.CustomName.Contains("Connector"));

            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(containers, c => c.CubeGrid == Me.CubeGrid);

            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(ejectConnectors, c => c.CustomName == "Connector Ejector");

            mainDrivesCompsMin.Add("SteelPlate", 60000);
            mainDrivesCompsMin.Add("Construction", 2400);
            mainDrivesCompsMin.Add("MetalGrid", 2400);
            mainDrivesCompsMin.Add("Thrust", 1600);
            mainDrivesCompsMin.Add("PowerCell", 2400);
            mainDrivesCompsMin.Add("FusionComponent", 240);
            mainDrivesCompsMin.Add("Superconductor", 2000);
            mainDrivesCompsMin.Add("LargeTube", 720);

            if (Me.CustomData.Length > 0)
            {
                MyIniParseResult result;
                if (!_ini.TryParse(Me.CustomData, out result))
                    throw new Exception(result.ToString());

                mainDrivesCompsMin.Keys.ToList().ForEach(item =>
                {
                    int amount = int.Parse(_ini.Get("Main", item).ToString());

                    mainDrivesCompsMin[item] = amount;
                });
            }
            else
            {
                MyIniParseResult result;
                if (!_ini.TryParse(Me.CustomData, out result)) throw new Exception(result.ToString());

                mainDrivesCompsMin.Keys.ToList().ForEach(item =>
                {
                    Echo(item);

                    _ini.Set("Main", item, mainDrivesCompsMin[item].ToString());
                });

                Me.CustomData = _ini.ToString();
            }
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
        int mode = 0;

        public void Main(string argument, UpdateType updateSource)
        {
            iteration++;
            _logOutput?.WriteText("");
            Echo(iteration.ToString());
            items.Clear();
            GetItems();
            TransferItemsFromConnectorsToCargo();

            if (argument == "0")
            {
                mode = 0;
            }
            else if (argument == "1")
            {
                mode = 1;
            }

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

            PrintOutSpecialItems();
        }

        void ThrowExcessCargo(string item, int amount)
        {
            int ejectorCount = ejectConnectors.Count;
            int totalItemsToEject = amount;

            if (ejectorCount == 0) return;

            int actualAmount = amount / ejectorCount / 2;

            if(amount == 1) actualAmount = 1;

            switch(item)
            {
                case "SteelPlate":
                    if(actualAmount > 8000) {
                        actualAmount = 8000;
                    }
                    break;
                case "Thrust":
                    if(actualAmount > 2400)
                    {
                        actualAmount = 2400;
                    }
                    break;
                case "PowerCell":
                    if (actualAmount > 600)
                    {
                        actualAmount = 600;
                    }
                    break;
                case "LargeTube":
                    if (actualAmount > 631)
                    {
                        actualAmount = 631;
                    }
                    break;
                case "MetalGrid":
                    if (actualAmount > 1600)
                    {
                        actualAmount = 1600;
                    }
                    break;
                case "Superconductor":
                    if (actualAmount > 3000)
                    {
                        actualAmount = 3000;
                    }
                    break;
                case "Construction":
                    if (actualAmount > 12000)
                    {
                        actualAmount = 12000;
                    }
                    break;
            }

            containers.ForEach(c =>
            {
                var inventoryCc = c.GetInventory();

                List<MyInventoryItem> items = new List<MyInventoryItem>();

                inventoryCc.GetItems(items);

                items.ForEach(i =>
                {
                    if (i.Type.SubtypeId == item)
                    {
                        if (actualAmount <= 10)
                        {
                            // ejectConnectors[0].GetInventory().TransferItemFrom(inventoryCc, i, actualAmount);
                            return;
                        } else
                        {
                            ejectConnectors.ForEach(e =>
                            {
                                if(e.GetInventory().CanItemsBeAdded(actualAmount, i.Type))
                                {
                                    if(e.GetInventory().TransferItemFrom(inventoryCc, i, actualAmount))
                                    {
                                        totalItemsToEject -= actualAmount;
                                        Echo($"Thrown {actualAmount} units of {i.Type.SubtypeId} out of {totalItemsToEject}");
                                    }
                                }
                            });
                        }
                    }
                });
            });
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

        // Special Items from Hunting ships
        List<string> specialItems = new List<string> {
            "MCRN",
            "UNN",
            "PDC",
            "BlackBox",
            "Inner",
            "Lidar",
            "High",
            "Chip",
            "Belter",
            "Slug",
            "Guidance",
            "Tracking",
            "IceBox",
            "Water_Tank",
            "HeavyMotor",
            "ToolPack",
            "SmallArms"
        };

        // Loops items and finds specialItems in it and echoes it
        void PrintOutSpecialItems()
        {
            Echo("\n Special Items:");
            foreach (var item in items)
            {
                specialItems.ForEach(sI => {
                    if (item.Key.Contains(sI)) {
                        Echo(item.Key + ": " + item.Value);
                    }
                });
            }
        }

        // checks if item is in mainDriveCompsMin dictionary and if it has more than value than mainDriveCompsMin[item.Key] then if it has it adds into itemsMaxRepair the item name and its calculated how many times it can repair that drive
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
                            rep.ChangeLR(item.Key ,repairTimes);
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

        void TransferItemsFromConnectorsToCargo()
        {
            myShipConnectors.ForEach(conn =>
            {
                VRage.Game.ModAPI.Ingame.IMyInventory inventory = conn.GetInventory();
                List<MyInventoryItem> items = new List<MyInventoryItem> ();
                inventory.GetItems(items);
                itemsToThrowOut.ForEach(item =>
                {
                    foreach (var itemD in items)
                    {
                        containers.ForEach(con =>
                        {
                            if (con.GetInventory().CanItemsBeAdded(itemD.Amount, itemD.Type) && !(itemD.Type.SubtypeId.Contains("PDCBox") || itemD.Type.SubtypeId.Contains("Slug")))
                            {
                                con.GetInventory().TransferItemFrom(conn.GetInventory(), itemD, itemD.Amount);
                            }
                        });
                    }
                });

            });
        }

        // We get all items we have in the grid inventory and set them to items dictionary
        void GetItems()
        {
            try
            {
                inventoryC.Clear();
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
                                inventoryC.Add(item);
                                // This is yayks no idea why tho won't let me save the Aryx component as SubTypeId
                                if (item.Value.Type.SubtypeId.Contains("FusionComponent"))
                                {
                                    if(items.ContainsKey("FusionComponent"))
                                    {
                                        items["FusionComponent"] += item.Value.Amount;
                                    } else
                                    {
                                        items.Add("FusionComponent", item.Value.Amount);
                                    }
                                }
                                if (item != null && item.HasValue && !item.Value.Type.SubtypeId.Contains("FusionComponent"))
                                {
                                    string subtypeId = item.Value.Type.SubtypeId;

                                    if (!items.ContainsKey(subtypeId))
                                    {
                                        items.Add(subtypeId, item.Value.Amount);
                                    }
                                    else
                                    {
                                        items[subtypeId] += item.Value.Amount;
                                    }
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Echo(ex.Message);
            }
        }

        List<string> itemsToThrowOut = new List<string>() {
            "Detector",
            "Glass",
            "Gravity",
            "Radio",
            "Display",
            "Computer",
            "Small",
            "Interior",
            "Girder"
        };

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
                } else
                {
                    Echo($"{comp}: {itemsValue}/{mainDrivesCompsMinValue} Missing: {missingAmount}");
                }
            });

            if (mode == 1)
            {
                items.Keys.ToList().ForEach(key =>
                {
                    itemsToThrowOut.ForEach(k =>
                    {
                        if (key.Contains(k))
                        {
                            ThrowExcessCargo(key, (int)items[key]);
                        }
                    });
                });
            }
            
            if (counterCompsPresent < requiredComps)
            {
                notEnoughMats = true;
            }

            return notEnoughMats;
        }
    }
}
