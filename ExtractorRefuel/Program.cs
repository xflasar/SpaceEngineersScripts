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
        IMyTerminalBlock fuelExtractor;
        List<IMyCargoContainer> myCargoContainers = new List<IMyCargoContainer>();
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            fuelExtractor = GridTerminalSystem.GetBlockWithName("Extractor");
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(myCargoContainers);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if(fuelExtractor == null)
            {
                Echo("FuelExtractor not found!");
                return;
            }
            
            try
            {
                if(fuelExtractor.GetInventory().ItemCount == 0)
                {
                    myCargoContainers.ForEach(container =>
                    {
                        var inventory = container.GetInventory();

                        List<MyInventoryItem> items = new List<MyInventoryItem>();
                        inventory.GetItems(items);

                        foreach (var item in items)
                        {
                            if(item.Type.SubtypeId.Contains("SG_Fuel_Tank"))
                            {
                                if(fuelExtractor.GetInventory().CanItemsBeAdded(1, item.Type))
                                {
                                    fuelExtractor.GetInventory().TransferItemFrom(inventory, item, 1);
                                    Echo("Loaded fuel!");
                                }
                            }
                        }
                    });
                }
            } catch (Exception ex)
            {
            }
        }
    }
}
