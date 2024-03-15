using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
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

        // Bugs:
        // - Whenever we reload the pb code it will count from 0  if we add back the sold amount it will start counting from 0 
        // - it takes into account Main first taken Inventory and then each time we check against current but the math is just substracting current from first saved and that difference gets saved -> we should always add onto the saved amount in CustomData each time we sell item

        // TODO:
        // - Refactor whole code to make it more effective current code is fast coded no brain
        IMyStoreBlock _storeBlockBuy;
        IMyStoreBlock _storeBlockSell;
        IMyRadioAntenna _storeAntena;

        MyIni _ini = new MyIni();
        string stringBuilder = "";


        List<IMyCargoContainer> containers = new List<IMyCargoContainer>();

        Dictionary<string, double> inventoryItems = new Dictionary<string, double>();
        Dictionary<string, double> itemsSell = new Dictionary<string, double>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            _storeBlockBuy = GridTerminalSystem.GetBlockWithName("Store Buy") as IMyStoreBlock;
            _storeBlockSell = GridTerminalSystem.GetBlockWithName("Store Sell") as IMyStoreBlock;

            _storeAntena = GridTerminalSystem.GetBlockWithName("Antena") as IMyRadioAntenna;

            var containersGroup = GridTerminalSystem.GetBlockGroupWithName("Containers");

            List<IMyTerminalBlock> containersTerminal = new List<IMyTerminalBlock>();
            containersGroup.GetBlocks(containersTerminal);
            containersTerminal.ForEach(container =>
            {
                containers.Add(container as IMyCargoContainer);
            });

            GetItemsInCargos();

            if (Me.CustomData.Length > 0)
            {
                MyIniParseResult result;
                if (!_ini.TryParse(Me.CustomData, out result))
                    throw new Exception(result.ToString());

                inventoryItems.Keys.ToList().ForEach(item =>
                {
                    int amount = int.Parse(_ini.Get("Main", item).ToString());
                    if (itemsSell.ContainsKey(item))
                    {
                        itemsSell[item] = amount;
                    } else
                    {
                        itemsSell.Add(item, amount);
                    }
                });
            }
            else
            {
                MyIniParseResult result;
                if (!_ini.TryParse(Me.CustomData, out result)) throw new Exception(result.ToString());

                inventoryItems.Keys.ToList().ForEach(item =>
                {
                    Echo(item);

                    _ini.Set("Main", item, itemsSell[item]);
                });

                Me.CustomData = _ini.ToString();
            }
        }

        public void Save()
        {
        }

        List<MyStoreQueryItem> _storeItems = new List<MyStoreQueryItem>();
        int counterItem = 0;
        int runnerTimer = 0; // each 5 seconds it will change item

        static string FormatPrice(long number)
        {
            return string.Format("{0:#,##0}", number);
        }

        void GetItemsInCargos()
        {
            inventoryItems.Clear();
            containers.ForEach(x =>
            {
                var inventory = x.GetInventory();
                if (inventory != null)
                {
                    List<MyInventoryItem> myInventoryItems = new List<MyInventoryItem>();
                    inventory.GetItems(myInventoryItems);
                    foreach (var item in myInventoryItems)
                    {
                        if (item != null)
                        {
                            if (inventoryItems.Keys.ToList().Contains(item.Type.SubtypeId))
                            {
                                inventoryItems[item.Type.SubtypeId] += (double)item.Amount;
                            } else
                            {
                                inventoryItems.Add(item.Type.SubtypeId, (double)item.Amount);
                                if (!itemsSell.ContainsKey(item.Type.SubtypeId))
                                {
                                    Echo(item.Type.SubtypeId);
                                    itemsSell.Add(item.Type.SubtypeId, 0);
                                }
                            }
                        }
                    }
                }
            });
        }

        void GetItemsWithCompare()
        {
            Dictionary<string, double> itemsTemp = new Dictionary<string, double>();
            containers.ForEach(x =>
            {
                var inventory = x.GetInventory();
                if (inventory != null)
                {
                    List<MyInventoryItem> myInventoryItems = new List<MyInventoryItem>();
                    inventory.GetItems(myInventoryItems);
                    foreach (var item in myInventoryItems)
                    {
                        if (item != null)
                        {
                            if (itemsTemp.Keys.ToList().Contains(item.Type.SubtypeId))
                            {
                                itemsTemp[item.Type.SubtypeId] += (double)item.Amount;
                            }
                            else
                            {
                                itemsTemp.Add(item.Type.SubtypeId, (double)item.Amount);
                            }
                        }
                    }
                }
            });
            
            bool sold = false;

            itemsTemp.Keys.ToList().ForEach(x =>
            {
                var diff = inventoryItems[x] - itemsTemp[x];
                Echo(inventoryItems[x] + ":" + diff + ":" + itemsTemp[x]);

                if(diff != 0)
                {
                    itemsSell[x] = diff;
                    sold = true;
                }
            });

            if (sold)
            {
                SaveToCustomData();
            }
        }

        void SaveToCustomData()
        {
            MyIniParseResult result;
            if (!_ini.TryParse(Me.CustomData, out result)) throw new Exception(result.ToString());

            itemsSell.Keys.ToList().ForEach(item =>
            {
                Echo(item);

                _ini.Set("Main", item, itemsSell[item]);
            });

            double totalEarnings = 0;
            
            _storeItems.ForEach(item =>
            {
                if(itemsSell.ContainsKey(item.ItemId.SubtypeId))
                {
                    if (itemsSell[item.ItemId.SubtypeId]> 0)
                    {
                        totalEarnings += itemsSell[item.ItemId.SubtypeId] * item.PricePerUnit;
                    }

                }
            });

            _ini.Set("Statistics", "Earnings", totalEarnings);

            Me.CustomData = _ini.ToString();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            _storeItems.Clear();
            _storeBlockBuy.GetPlayerStoreItems(_storeItems);

            if (argument == "RefreshItems")
            {
                GetItemsInCargos();
            }

            if (_storeItems.Count > 0 && runnerTimer == 5)
            {
                // Just check every 5s for bought items
                GetItemsWithCompare();

                if (counterItem >= _storeItems.Count)
                {
                    counterItem = 0;
                    return;
                }

                MyStoreQueryItem queryItem = _storeItems[counterItem];

                string _stringBuilder = "";
                _stringBuilder += queryItem.ItemId.ToString().Split('/')[1] + " | ";
                _stringBuilder += "Units:" + queryItem.Amount.ToString() + " | ";
                _stringBuilder += "PPU:" + FormatPrice(queryItem.PricePerUnit);

                _storeAntena.HudText = _stringBuilder;
                
                counterItem++;
                runnerTimer = 0;

            } else
            {
                runnerTimer++;
            }
        }
    }
}
