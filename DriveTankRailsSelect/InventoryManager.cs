using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        List<IMyShipGrinder> grinders = new List<IMyShipGrinder>();

        List<MyInventoryItem?> inventoryC = new List<MyInventoryItem?>();

        List<IMyCargoContainer> containers = new List<IMyCargoContainer>();
        Dictionary<string, MyFixedPoint> items = new Dictionary<string, MyFixedPoint>();

        // Items to be automatically thrown out if detected
        List<string> itemsToThrowOut = new List<string>() {
            "Detector",
            "Glass",
            "Gravity",
            "Radio",
            "Display",
            "Computer",
            "Small",
            "Interior",
            "Girder",
            "Motor",
            "Reactor"
        };

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
            "Heavy",
            "Pack",
            "Arms"
        };

        void TransferItemsFromConnectorsToCargo()
        {
            myShipConnectors.ForEach(conn =>
            {
                VRage.Game.ModAPI.Ingame.IMyInventory inventory = conn.GetInventory();
                List<MyInventoryItem> items = new List<MyInventoryItem> ();
                inventory.GetItems(items);
                
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
        }

        void TransferFromGrindersToCargo()
        {
            grinders.ForEach(grinder =>
            {
                var inventory = grinder.GetInventory();
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventory.GetItems(items);

                foreach (var item in items)
                {
                    containers.ForEach(container =>
                    {
                        if (container.GetInventory().CanItemsBeAdded(item.Amount, item.Type))
                        {
                            container.GetInventory().TransferItemFrom(grinder.GetInventory(), item, item.Amount);
                        }
                    });
                }
            });
        }

        void ThrowOutItems()
        {
            containers.ForEach(c =>
            {
                itemsToThrowOut.ForEach(item =>
                {
                    var inventory = c.GetInventory();
                    List<MyInventoryItem> myInventoryItems = new List<MyInventoryItem>();

                    inventory.GetItems(myInventoryItems);

                    foreach (var inventoryItem in myInventoryItems)
                    {
                        bool throwOut = true;
                        specialItems.ForEach(sI =>
                        {
                             if(inventoryItem.Type.SubtypeId.Contains(sI))
                             {
                                 throwOut = false;
                             }
                        });

                        if (!throwOut)
                        {
                            continue;
                        }

                        if (inventoryItem.Type.SubtypeId.Contains(item))
                        {
                            ejectConnectors.ForEach(ej =>
                            {
                                if (ej.GetInventory().CanItemsBeAdded(inventoryItem.Amount, inventoryItem.Type))
                                {
                                    //Echo(inventoryItem.Type.SubtypeId);
                                    ej.GetInventory().TransferItemFrom(inventory, inventoryItem, inventoryItem.Amount);
                                }
                                else
                                {
                                    Echo(inventoryItem.Type.SubtypeId + ":" + inventoryItem.Amount);
                                }
                            });
                        }
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

        void ThrowExcessCargo(string item, int amount)
        {
            bool continueE = true;
            specialItems.ForEach(sI =>
            {
                if (item.Contains(sI))
                {
                    continueE = false;
                    return;
                }
            });

            if (!continueE) return;

            int ejectorCount = ejectConnectors.Count;
            int totalItemsToEject = amount;

            if (ejectorCount == 0) return;

            int actualAmount = amount / ejectorCount / 2;

            if (amount == 1) actualAmount = 1;

            switch (item)
            {
                case "SteelPlate":
                    if (actualAmount > 8000)
                    {
                        actualAmount = 8000;
                    }
                    break;
                case "Thrust":
                    if (actualAmount > 2400)
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
                        }
                        else
                        {
                            ejectConnectors.ForEach(e =>
                            {
                                if (e.GetInventory().CanItemsBeAdded(actualAmount, i.Type))
                                {
                                    if (e.GetInventory().TransferItemFrom(inventoryCc, i, actualAmount))
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


    }
}
