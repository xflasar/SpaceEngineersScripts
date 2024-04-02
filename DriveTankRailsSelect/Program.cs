using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
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

    partial class Program : MyGridProgram
    {
        static public Program pInstance = null;

        List<IMyShipConnector> ejectConnectors = new List<IMyShipConnector>();
        List<IMyShipConnector> myShipConnectors = new List<IMyShipConnector>();

        MyIni _ini = new MyIni();

        int iteration = 0;
        int mode = 0;

        Program()
        {
            pInstance = this;
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            Echo = EchoToLCD;

            SetUp();
            _logOutput = myTextPanels.Find(t => t.CustomName.Contains("DriveTankLog")); // Set the name here so the script can get the lcd to put Echo to
        }

        Dictionary<string, MyFixedPoint> mainDrivesCompsMin = new Dictionary<string, MyFixedPoint>();
        Dictionary<string, int> itemsMaxRepair = new Dictionary<string, int>();

        IMyTextPanel _logOutput;

        void SetupGridBlocksLists()
        {
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(myTextPanels);
            GridTerminalSystem.GetBlocksOfType<IMyShipGrinder>(grinders);
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(myShipConnectors, b => b.CustomName.Contains("Connector") && !b.CustomName.Contains("Ejector"));

            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(containers, c => c.CubeGrid == Me.CubeGrid);

            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(ejectConnectors, c => c.CustomName == "Connector Ejector");
        }

        void SetupCompsMinList()
        {
            mainDrivesCompsMin.Add("SteelPlate", 60000);
            mainDrivesCompsMin.Add("Construction", 2400);
            mainDrivesCompsMin.Add("MetalGrid", 2400);
            mainDrivesCompsMin.Add("Thrust", 1600);
            mainDrivesCompsMin.Add("PowerCell", 2400);
            mainDrivesCompsMin.Add("FusionComponent", 240);
            mainDrivesCompsMin.Add("Superconductor", 2000);
            mainDrivesCompsMin.Add("LargeTube", 720);
        }

        void InitCustomData()
        {
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

        void SetUp()
        {
            SetupGridBlocksLists();
            SetupCompsMinList();
            InitCustomData();
        }

        void Main(string argument, UpdateType updateSource)
        {
            iteration++;
            _logOutput?.WriteText("");
            Echo(iteration.ToString());

            items.Clear();
            GetItems();
            TransferItemsFromConnectorsToCargo();
            TransferFromGrindersToCargo();

            if(mode == 1)
            {
                ThrowOutItems();
            }

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
    }
}
