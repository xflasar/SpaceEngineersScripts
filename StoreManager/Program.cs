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
        IMyStoreBlock _storeBlockBuy;
        IMyStoreBlock _storeBlockSell;
        IMyRadioAntenna _storeAntena;
        
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            _storeBlockBuy = GridTerminalSystem.GetBlockWithName("Store Buy") as IMyStoreBlock;
            _storeBlockSell = GridTerminalSystem.GetBlockWithName("Store Sell") as IMyStoreBlock;

            _storeAntena = GridTerminalSystem.GetBlockWithName("Antena") as IMyRadioAntenna;
        }

        List<MyStoreQueryItem> _storeItems = new List<MyStoreQueryItem>();
        int counterItem = 0;
        int runnerTimer = 0; // each 5 seconds it will change item

        static string FormatPrice(long number)
        {
            return string.Format("{0:#,##0}", number);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            _storeItems.Clear();
            _storeBlockBuy.GetPlayerStoreItems(_storeItems);

            if (_storeItems.Count > 0 && runnerTimer == 5)
            {
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
