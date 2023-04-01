// Assembler Manager - manages the production of items using assemblers
// Created by xSupeFly
// Discord: xSupeFly#2911

//  Queue items for production using LCD display: Done
//  Fill assemblers with required components and move excess components to available containers: In Progress
//  Display production queue and estimated completion time in dd:hh:mm:ss format on LCD display: Planned
//  Allow for queuing of multiple items simultaneously and assign priority based on need: In Progress
//  Store production statistics in the Storage property: Planned
//  Make an DilithiumMatrix production separate optional method -> This is only useful with DeuteriumRefineryManager script running

//  To set up your grid, follow these basic steps:

//  1.  Build Assemblers and Cargo Containers on your grid.
//  2.  If you have enemies who have built Assemblers on your grid, make sure they are set to share with all players.
//  3.  If enemies have built Assemblers on your grid, you will also need to build a Cargo Container that is connected to all of the enemy Assemblers using conveyors. This Cargo Container should be set to share with all players.
//  4.  Make sure to name the Cargo Containers and Assemblers according to the names listed in the Config section, or change the names in the Config section to match your chosen names.

// Lists
List<IMyCargoContainer> containersList = new List<IMyCargoContainer>();
List<IMyCargoContainer> containerDilMatrixList = new List<IMyCargoContainer>();
List<IMyAssembler> assemblers = new List<IMyAssembler>();
List<LearnedBlueprint> lBlueprints = new List<LearnedBlueprint>();
List<IMyTextPanel> lcdPanelList = new List<IMyTextPanel>();

// Unfortunatelly KEEN doesn't want to give us access to Blueprint.Result to get materials needed to make the blueprint item so this Dictionary is side way to make it possible
List<ItemBlueprint> _itemBlueprintResult = new List<ItemBlueprint> {
    // Components
    // Vanilla
    new ItemBlueprint("Bulletproof Glass", new Dictionary<string, double>(){
        {"Silicon Wafer", 5.00}
    }),
    new ItemBlueprint("Computer", new Dictionary<string, double>(){
        {"Iron Ingot", 0.17},
        {"Silicon Wafer", 0.07}
    }),
    new ItemBlueprint("Construction Component", new Dictionary<string, double>(){
        {"Iron Ingot", 2.67}
    }),
    new ItemBlueprint("Detector Component", new Dictionary<string, double>(){
        {"Iron Ingot", 1.67},
        {"Nickel Ingot", 5.00}
    }),
    new ItemBlueprint("Display", new Dictionary<string, double>(){
        {"Iron Ingot", 0.33},
        {"Silicon Wafer", 1.67}
    }),
    new ItemBlueprint("Explosives", new Dictionary<string, double>(){
        {"Silicon Wafer", 0.17},
        {"Magnesium Powder", 0.67}
    }),
    new ItemBlueprint("Girder", new Dictionary<string, double>(){
        {"Iron Ingot", 2.00}
    }),
    new ItemBlueprint("Gravity Component", new Dictionary<string, double>(){
        {"Silver Ingot", 1.67},
        {"Gold Ingot", 3.33},
        {"Cobalt Ingot", 73.33},
        {"Iron Ingot", 200.00}
    }),
    new ItemBlueprint("Interior Plate", new Dictionary<string, double>(){
        {"Iron Ingot", 1.00}
    }),
    new ItemBlueprint("Large Steel Tube", new Dictionary<string, double>(){
        {"Iron Ingot", 10.00}
    }),
    new ItemBlueprint("Medical Component", new Dictionary<string, double>(){
        {"Iron Ingot", 20.00},
        {"Nickel Ingot", 23.33},
        {"Silver Ingot", 6.67}
    }),
    new ItemBlueprint("Metal Grid", new Dictionary<string, double>(){
        {"Iron Ingot", 4.00},
        {"Nickel Ingot", 1.67},
        {"Cobalt Ingot", 1.00}
    }),
    new ItemBlueprint("Motor", new Dictionary<string, double>(){
        {"Iron Ingot", 6.67},
        {"Nickel Ingot", 1.67}
    }),
    new ItemBlueprint("Power Cell", new Dictionary<string, double>(){
        {"Iron Ingot", 3.33},
        {"Silicon Wafer", 0.33},
        {"Nickel Ingot", 0.67}
    }),
    new ItemBlueprint("Radio-comm Component", new Dictionary<string, double>(){
        {"Iron Ingot", 2.67},
        {"Silicon Wafer", 0.33}
    }),
    new ItemBlueprint("Reactor Component", new Dictionary<string, double>(){
        {"Iron Ingot", 5.00},
        {"Gravel", 6.67},
        {"Silver Ingot", 1.67}
    }),
    new ItemBlueprint("Small Steel Tube", new Dictionary<string, double>(){
        {"Iron Ingot", 1.67}
    }),
    new ItemBlueprint("Solar Cell", new Dictionary<string, double>(){
        {"Nickel Ingot", 1.00},
        {"Silicon Wafer", 2.00}
    }),
    new ItemBlueprint("Steel Plate", new Dictionary<string, double>(){
        {"Iron Ingot", 7.00}
    }),
    new ItemBlueprint("Superconductor", new Dictionary<string, double>(){
        {"Iron Ingot", 3.33},
        {"Gold Ingot", 0.67}
    }),
    new ItemBlueprint("Thruster Component", new Dictionary<string, double>(){
        {"Iron Ingot", 10.00},
        {"Cobalt Ingot", 3.33},
        {"Gold Ingot", 0.33},
        {"Platinum Ingot", 0.13}
    }),
    // Modded Components Star Trek Continuum
    new ItemBlueprint("Duranium Grid", new Dictionary<string, double>(){
        {"Duranium Ingot", 1.67}
    }),
    new ItemBlueprint("Nanovirus Chip", new Dictionary<string, double>(){
        {"Iron Ingot", 6.67},
        {"Nickel Ingot", 6.67},
        {"Silicon Wafer", 6.67},
        {"Gold Ingot", 8.33},
        {"Platinum Ingot", 8.33}
    }),
    new ItemBlueprint("Field Emitter", new Dictionary<string, double>(){
        {"Platinum Ingot", 2.67},
        {"Iron Ingot", 26.67},
        {"Silicon Wafer", 6.67},
        {"Gold Ingot", 5.00}
    }),
    new ItemBlueprint("Gold Pressed Latinum", new Dictionary<string, double>(){
        {"Latinum Ingot", 10.00},
        {"Gold Ingot", 20.00},
        {"Platinum Ingot", 3.33}
    }),
    new ItemBlueprint("Transparent Aluminum Plate", new Dictionary<string, double>(){
        {"Aluminum Ingot", 10.00},
        {"Silver Ingot", 6.67}
    }),
    new ItemBlueprint("Tritanium Plate", new Dictionary<string, double>(){
        {"Tritanium Ingot", 2.33},
        {"Duranium Ingot", 1.00}
    }),
    // Tools Vanilla
    new ItemBlueprint("PRO-1", new Dictionary<string, double>(){
        {"Iron Ingot", 10.00},
        {"Nickel Ingot", 3.33},
        {"Cobalt Ingot", 1.67},
        {"Platinum Ingot", 1.67}
    }),
    new ItemBlueprint("Grinder", new Dictionary<string, double>(){
        {"Iron Ingot", 1.00},
        {"Nickel Ingot", 0.33},
        {"Gravel", 1.67},
        {"Silicon Wafer", 0.33}
    }),
    new ItemBlueprint("MR-20", new Dictionary<string, double>(){
        {"Iron Ingot", 1.00},
        {"Nickel Ingot", 0.33}
    }),
    new ItemBlueprint("RO-1", new Dictionary<string, double>(){
        {"Iron Ingot", 10.00},
        {"Nickel Ingot", 3.33},
        {"Cobal Ingot", 1.67}
    }),
    new ItemBlueprint("Datapad", new Dictionary<string, double>(){
        {"Iron Ingot", 0.33},
        {"Silicon Wafer", 1.67},
        {"Gravel", 0.33}
    }),
    new ItemBlueprint("S-10E", new Dictionary<string, double>(){
        {"Iron Ingot", 0.33},
        {"Nickel Ingot", 0.13},
        {"Platinum Ingot", 0.17},
        {"Silver Ingot", 0.33}
    }),
    new ItemBlueprint("S-20A", new Dictionary<string, double>(){
        {"Iron Ingot", 0.50},
        {"Nickel Ingot", 0.17}
    }),
    new ItemBlueprint("Hand Drill", new Dictionary<string, double>(){
        {"Iron Ingot", 6.67},
        {"Nickel Ingot", 1.00},
        {"Silicon Wafer", 1.00}
    }),
    new ItemBlueprint("Hydrogen Bottle", new Dictionary<string, double>(){
        {"Iron Ingot", 26.67},
        {"Silicon Wafer", 3.33},
        {"Nickel Ingot", 10.00}
    }),
    new ItemBlueprint("Oxygen Bottle", new Dictionary<string, double>(){
        {"Iron Ingot", 26.67},
        {"Silicon Wafer", 3.33},
        {"Nickel Ingot", 10.00}
    }),
    new ItemBlueprint("MR-8P", new Dictionary<string, double>(){
        {"Iron Ingot", 1.00},
        {"Nickel Ingot", 0.33},
        {"Cobalt Ingot", 1.67}
    }),
    new ItemBlueprint("MR-50A", new Dictionary<string, double>(){
        {"Iron Ingot", 1.00},
        {"Nickel Ingot", 2.67}
    }),
    new ItemBlueprint("S-10", new Dictionary<string, double>(){
        {"Iron Ingot", 0.33},
        {"Nickel Ingot", 0.10}
    }),
    new ItemBlueprint("MR-30E", new Dictionary<string, double>(){
        {"Iron Ingot", 1.00},
        {"Nickel Ingot", 0.33},
        {"Platinum Ingot", 1.33},
        {"Silver Ingot", 2.00}
    }),
    new ItemBlueprint("Welder", new Dictionary<string, double>(){
        {"Iron Ingot", 1.67},
        {"Nickel Ingot", 0.33},
        {"Gravel", 1.00}
    }),
    // Modded Tools Star Trek Continuum
    new ItemBlueprint("Enhanced Grinder", new Dictionary<string, double>(){
        {"Iron Ingot", 1.00},
        {"Nickel Ingot", 0.33},
        {"Cobalt Ingot", 0.67},
        {"Silicon Wafer", 2.00}
    }),
    new ItemBlueprint("Proficient Grinder", new Dictionary<string, double>(){
        {"Iron Ingot", 1.00},
        {"Nickel Ingot", 0.33},
        {"Cobalt Ingot", 0.33},
        {"Silicon Wafer", 0.67},
        {"Silver Ingot", 0.67}
    }),
    new ItemBlueprint("Elite Grinder", new Dictionary<string, double>(){
        {"Iron Ingot", 1.00},
        {"Nickel Ingot", 0.33},
        {"Cobalt Ingot", 0.33},
        {"Silicon Wafer", 0.67},
        {"Platinum Ingot", 0.67}
    }),
    new ItemBlueprint("Paint Gun", new Dictionary<string, double>(){
        {"Iron Ingot", 1.33},
        {"Nickel Ingot", 0.33},
        {"Silicon Wafer", 0.67}
    }),
    new ItemBlueprint("Enhanced Hand Drill", new Dictionary<string, double>(){
        {"Iron Ingot", 6.67},
        {"Nickel Ingot", 1.00},
        {"Silicon Wafer", 1.67}
    }),
    new ItemBlueprint("Proficient Hand Drill", new Dictionary<string, double>(){
        {"Iron Ingot", 6.67},
        {"Nickel Ingot", 1.00},
        {"Silicon Wafer", 1.00},
        {"Silver Ingot", 0.67}
    }),
    new ItemBlueprint("Elite Hand Drill", new Dictionary<string, double>(){
        {"Iron Ingot", 6.67},
        {"Nickel Ingot", 1.00},
        {"Silicon Wafer", 1.00},
        {"Platinum Ingot", 0.67}
    }),
    new ItemBlueprint("Enhanced Welder", new Dictionary<string, double>(){
        {"Iron Ingot", 1.67},
        {"Nickel Ingot", 0.33},
        {"Cobalt Ingot", 0.07},
        {"Silicon Wafer", 0.67}
    }),
    new ItemBlueprint("Proficient Welder", new Dictionary<string, double>(){
        {"Iron Ingot", 1.67},
        {"Nickel Ingot", 0.33},
        {"Cobalt Ingot", 0.07},
        {"Silver Ingot", 0.67}
    }),
    new ItemBlueprint("Elite Welder", new Dictionary<string, double>(){
        {"Iron Ingot", 1.67},
        {"Nickel Ingot", 0.33},
        {"Cobalt Ingot", 0.07},
        {"Platinum Ingot", 0.67}
    }),
    // Consumables Vanilla
    new ItemBlueprint("Autocannon Magazine", new Dictionary<string, double>(){
        {"Iron Ingot", 8.33},
        {"Nickel Ingot", 1.00},
        {"Magnesium Powder", 0.67}
    }),
    new ItemBlueprint("MR-20 Magazine", new Dictionary<string, double>(){
        {"Iron Ingot", 0.27},
        {"Nickel Ingot", 0.07},
        {"Magnesium Powder", 0.05}
    }),
    new ItemBlueprint("Canvas", new Dictionary<string, double>(){
        {"Silicon Wafer", 11.67},
        {"Iron Ingot", 0.67}
    }),
    new ItemBlueprint("S-10E Magazine", new Dictionary<string, double>(){
        {"Iron Ingot", 0.10},
        {"Nickel Ingot", 0.03},
        {"Magnesium Powder", 0.03}
    }),
    new ItemBlueprint("S-20A", new Dictionary<string, double>(){
        {"Iron Ingot", 0.17},
        {"Nickel Ingot", 0.03},
        {"Magnesium Powder", 0.07}
    }),
    new ItemBlueprint("Artillery Shell", new Dictionary<string, double>(){
        {"Iron Ingot", 20.00},
        {"Nickel Ingot", 2.67},
        {"Magnesium Powder", 1.67},
        {"Uranium Ingot", 0.03}
    }),
    new ItemBlueprint("Large Railgun Sabot", new Dictionary<string, double>(){
        {"Iron Ingot", 6.67},
        {"Nickel Ingot", 1.00},
        {"Silicon Wafer", 10.00},
        {"Uranium Ingot", 0.33}
    }),
    new ItemBlueprint("Assault Cannon Shell", new Dictionary<string, double>(){
        {"Iron Ingot", 5.00},
        {"Nickel Ingot", 0.67},
        {"Magnesium Powder", 0.40}
    }),
    new ItemBlueprint("Missile", new Dictionary<string, double>(){
        {"Iron Ingot", 18.33},
        {"Nickel Ingot", 2.33},
        {"Silicon Wafer", 0.07},
        {"Uranium Ingot", 0.03},
        {"Platinum Ingot", 0.01},
        {"Magnesium Powder", 0.40}
    }),
    new ItemBlueprint("Gatling Ammo Box", new Dictionary<string, double>(){
        {"Iron Ingot", 13.33},
        {"Nickel Ingot", 1.67},
        {"Magnesium Powder", 1.00}
    }),
    new ItemBlueprint("MR-8P Magazine", new Dictionary<string, double>(){
        {"Iron Ingot", 0.27},
        {"Nickel Ingot", 0.07},
        {"Magnesium Powder", 0.05}
    }),
    new ItemBlueprint("MR-50A Magazine", new Dictionary<string, double>(){
        {"Iron Ingot", 0.67},
        {"Nickel Ingot", 0.17},
        {"Magnesium Powder", 0.13}
    }),
    new ItemBlueprint("S-10 Magazine", new Dictionary<string, double>(){
        {"Iron Ingot", 0.08},
        {"Nickel Ingot", 0.02},
        {"Magnesium Powder", 0.02}
    }),
    new ItemBlueprint("Small Railgun Sabot", new Dictionary<string, double>(){
        {"Iron Ingot", 1.33},
        {"Nickel Ingot", 0.17},
        {"Silicon Wafer", 1.67},
        {"Uranium Ingot", 0.07}
    }),
    new ItemBlueprint("MR-30E Magazine", new Dictionary<string, double>(){
        {"Iron Ingot", 0.40},
        {"Nickel Ingot", 0.13},
        {"Magnesium Powder", 0.08}
    }),
    // Modded Consumables Star Trek Continuum
    new ItemBlueprint("Paint Chemicals", new Dictionary<string, double>(){
        {"Gravel", 0.01}
    }),
    new ItemBlueprint("Photon Torpedo Magazine", new Dictionary<string, double>(){
        {"Tritanium Plate", 3.33},
        {"Torpedo Casing", 1.00},
        {"Matter / Anti-Matter Chamber", 2.00},
        {"Torpedo Thruster", 2.00},
        {"Isolinear Chip", 11.67}
    }),
    new ItemBlueprint("Quantum Torpedo Magazine", new Dictionary<string, double>(){
        {"Tritanium Plate", 10.00},
        {"Torpedo Casing", 2.00},
        {"Matter / Anti-Matter Chamber", 3.00},
        {"Torpedo Thruster", 5.00},
        {"Isolinear Chip", 20.00}
    }),
    new ItemBlueprint("Photon Torpedo Magazine (Small)", new Dictionary<string, double>(){
        {"Tritanium Plate", 1.00},
        {"Torpedo Casing", 1.00},
        {"Matter / Anti-Matter Chamber", 1.00},
        {"Torpedo Thruster", 1.00},
        {"Isolinear Chip", 5.00}
    }),
    new ItemBlueprint("Quantum Torpedo Magazine (Small)", new Dictionary<string, double>(){
        {"Tritanium Plate", 5},
        {"Torpedo Casing", 1.00},
        {"Matter / Anti-Matter Chamber", 1.00},
        {"Torpedo Thruster", 1.00},
        {"Isolinear Chip", 10.00}
    }),
    new ItemBlueprint("Spatial Torpedo Magazine", new Dictionary<string, double>(){
        {"Tritanium Plate", 3.33},
        {"Silicon Wafer", 16.67},
        {"Uranium Ingot", 6.67},
        {"Platinum Ingot", 3.33},
        {"Computer", 6.67},
        {"Gold Ingot", 8.33}
    }),
    new ItemBlueprint("Spatial Torpedo Magazine (Small)", new Dictionary<string, double>(){
        {"Tritanium Plate", 1.67},
        {"Silicon Wafer", 8.33},
        {"Uranium Ingot", 1.67},
        {"Platinum Ingot", 1.33},
        {"Computer", 3.67},
        {"Gold Ingot", 3.33}
    }),
    new ItemBlueprint("Isolinear Chip", new Dictionary<string, double>(){
        {"Tritanium Ingot", 6.67},
        {"Silicon Wafer", 1.67},
        {"Gold Ingot", 0.33},
        {"Silver Ingot", 0.22},
        {"Duranium Ingot", 0.33}
    }),
    new ItemBlueprint("Dilithium Matrix", new Dictionary<string, double>(){
        {"Refined Dilithium", 3.33}
    }),
    new ItemBlueprint("Matter '\' Anti-Matter Chamber", new Dictionary<string, double>(){
        {"Tritanium Ingot", 5.00},
        {"Silicon Wafer", 16.67},
        {"Refined Dilithium", 6.67},
        {"Deuterium Intermix Containment Unit", 3.00},
        {"Magnesium Powder", 10.00},
        {"Duranium Ingot", 3.33},
        {"Gold Ingot", 8.33}
    }),
    new ItemBlueprint("Torpedo Casing", new Dictionary<string, double>(){
        {"Tritanium Ingot", 10.00},
        {"Silicon Wafer", 10.00},
        {"Iron Ingot", 6.67},
        {"Aluminum Ingot", 3.33},
        {"Nickel Ingot", 6.67},
        {"Duranium Ingot", 6.67},
        {"Gold Ingot", 8.33}
    }),
    new ItemBlueprint("Torpedo Thruster", new Dictionary<string, double>(){
        {"Tritanium Ingot", 5.00},
        {"Silicon Wafer", 16.67},
        {"Refined Dilithium", 6.67},
        {"Deuterium Intermix Containment Unit", 6.67},
        {"Platinum Ingot", 3.33},
        {"Duranium Ingot", 3.33},
        {"Gold Ingot", 8.33}
    }),
    new ItemBlueprint("Transphasic Matter Chamber", new Dictionary<string, double>(){
        {"Tritanuim Ingot", 5.00},
        {"Silicon Wafer", 16.67},
        {"Refined Dilithium", 6.67},
        {"Deuterium Intermix Containment Unit", 3.00},
        {"Refined Transphasic Matter", 20.00},
        {"Magnesium Powder", 10.00},
        {"Duranium Ingot", 6.67},
        {"Gold Ingot", 16.67}
    }),
    // Modded Tech Star Trek Continuum
    new ItemBlueprint("Common Tech", new Dictionary<string, double>(){
        {"Tritanium Ingot", 15.00},
        {"Iron Ingot", 500.00},
        {"Silicon Wafer", 250.00},
        {"Cobalt Ingot", 50.00},
        {"Gold Ingot", 25.00},
        {"Aluminum Ingot", 100.00}
    }),
    new ItemBlueprint("Rare Tech", new Dictionary<string, double>(){
        {"Tritanium Ingot", 15.00},
        {"Iron Ingot", 1000.00},
        {"Silicon Wafer", 500.00},
        {"Silver Ingot", 100.00},
        {"Gold Ingot", 30.00},
        {"Uranium Ingot", 20.00},
        {"Kemocite Ingot", 50.00},
        {"Common Tech", 2.00}
    }),
    new ItemBlueprint("Exotic Tech", new Dictionary<string, double>(){
        {"Tritanium Ingot", 15.00},
        {"Iron Ingot", 1000.00},
        {"Silicon Wafer", 500.00},
        {"Duranium Ingot", 66.67},
        {"Refined Dilithium", 10.00},
        {"Platinum Ingot", 30.00},
        {"Neutronium Ingot", 50.00},
        {"Rare Tech", 2.00}
    })
};

// Dictionary
Dictionary<string, int> lcdAssemblerStreamData = new Dictionary<string, int>();
Dictionary<MyDefinitionId, int> queueItems = new Dictionary<MyDefinitionId, int>();
Dictionary<string, int> currentItems = new Dictionary<string, int>();
Dictionary<string, int> currentIngots = new Dictionary<string, int>();

// variables
int runtimeLagger = 0;
int runtimeLaggerDelay = 5; // This variable is a timer with a default interval of 5 seconds. If the timer detects activity during the 5-second interval, it will remain at that interval. However, if no activity is detected, the timer will switch to a 20-second interval to conserve resources. This timer is used to limit resource drainage.
Logger _logger;

// Config || Replace variable values at your discretion ||
string containerTransferEnemyAssemblersCustomName = "DilithiumMatrix Cargo"; // Every Transfer Enemy Assembler Cargo Container CustomName need to hame this in their name 
string assemblersName = "Assembler"; // Every Assembler CustomName need to have this in their name

public Program(){
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    _logger = new Logger();
    GridTerminalSystem.GetBlocksOfType(assemblers, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(containersList, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(lcdPanelList, b => b.CubeGrid == Me.CubeGrid);

    GridTerminalSystem.GetBlocksOfType(containerDilMatrixList, b => b.CustomName.Contains(containerTransferEnemyAssemblersCustomName));
    // Move to an method lazy mate...
    assemblers = assemblers.Where(assembler => assembler.CustomName.Contains(assemblersName)).ToList();

    LoadDataStream();
    LoadLearnedBlueprints();
}
public void Main(string argument, UpdateType updateSource)
{
    // Check for runtime lags
    if (runtimeLagger < runtimeLaggerDelay)
    {
        runtimeLagger++;
        //Echo("Main method instructions (runtimeLagger): " + Runtime.CurrentInstructionCount.ToString());
    }
    else
    {
        // Reset runtime lagger and increment script runs
        runtimeLagger = 0;
        _logger.SetRuns(_logger.GetRuns() + 1);

        // Handle LCD display
        var lcdPanel = lcdPanelList.Find(panel => panel.CustomName == "LCD Assembler Manager Main");
        ReadLCDData(lcdPanel, lcdAssemblerStreamData);
        SaveLCDData(lcdPanel, lcdAssemblerStreamData);

        // Manage items in containers
        ManageCurrentItems();
        ManageCurrentIngotInventory();

        // Handle assembler queues
        try
        {
            MainAssembly();
        }
        catch (Exception ex)
        {
            Echo("Error: " + ex.ToString());
        }

        // Handle enemy-owned assemblers
        List<IMyAssembler> enemyAssemblers = assemblers.Where(assembler => assembler.OwnerId != Me.OwnerId).ToList();
        enemyAssemblers.ForEach(assembler =>
        {
            
            var items = new List<MyInventoryItem>();
            assembler.GetInventory(1).GetItems(items);

            items.ForEach(item =>
            {
                var container = containerDilMatrixList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type));
                if (container != null)
                {
                    //Echo($"{container.GetInventory(0).TransferItemFrom(assembler.GetInventory(1), 0, null, true, item.Amount)}");
                    container.GetInventory(0).TransferItemFrom(assembler.GetInventory(1), 0, null, true, item.Amount);
                    containersList.Any(cont => {
                        if(cont != container){
                            var itemsCont = new List<MyInventoryItem>();
                            cont.GetInventory(0).GetItems(itemsCont);

                             var itemContFound = itemsCont.Find(itemCont => itemCont.Type == item.Type);

                            var itemsContainer = new List<MyInventoryItem>();
                            container.GetInventory(0).GetItems(itemsContainer);

                            var itemFoundContainer = itemsContainer.Find( itemContainer => itemContainer.Type == item.Type);

                           

                            if(itemContFound != null && cont.GetInventory(0).CanItemsBeAdded(itemFoundContainer.Amount, itemFoundContainer.Type)){
                                var index = itemsContainer.IndexOf(itemFoundContainer);
                                //Echo($"Transfer Completed? {cont.GetInventory(0).TransferItemFrom(container.GetInventory(0), index, null, true, itemFoundContainer.Amount)}");
                                cont.GetInventory(0).TransferItemFrom(container.GetInventory(0), index, null, true, itemFoundContainer.Amount);
                                return true;
                            }
                        } 
                        return false;
                    });
                }
            });
        });

        // Output runtime information to LCD display
        _logger.SetRuntime(Runtime.TimeSinceLastRun);
        Echo(Runtime.MaxInstructionCount.ToString());
        Echo("Main method instructions (Main Loop): " + Runtime.CurrentInstructionCount.ToString());
        Echo(_logger.GetRuntime().ToString());
        Echo("Script runs: " + _logger.GetRuns());

        // Debug output
        Echo("Assemblers: " + assemblers.Count.ToString());
    }
}


// Helper for ItemBlueprintResult List
struct ItemBlueprint{
    public ItemBlueprint(string Name, Dictionary<string, double> materialsDic){
        name = Name;
        materials = materialsDic;
    }

    public string name {get; set;}
    public Dictionary<string, double> materials {get; set;}
}

// This logger is still not implemented it will be used for an resource graph to know what and how much was done/created at the given time and total
// Logger
public class Log{
    string logType;
    string logMsg;

    public Log(string logtype, string logmsg){
        logType = logtype;
        logMsg = logmsg;
    }

    public string LogType{
        get { return this.logType; }
        set { 
            if(String.IsNullOrEmpty(value))return;
            else this.logType = value;
        }
    }

    public string LogMsg{
        get { return this.logMsg; }
        set {
            if(String.IsNullOrEmpty(value)) return;
            else this.logMsg = value;
        }
    }
}

public class Logger{
    private List<Log> loggerLog = new List<Log>();
    private TimeSpan runtime = TimeSpan.Zero;
    private int runs = 0;

    public Logger(){
    } 

    public void AddLogItem(Log log){
        this.loggerLog.Add(log);
    }

    public List<Log> GetLoggerLog(){
        return this.loggerLog;
    }

    public TimeSpan GetRuntime(){
        return this.runtime;
    }

    public void SetRuntime(TimeSpan value){
        this.runtime = value;
    }

    public void SetRuns(int value){
        this.runs = value;
    }

    public int GetRuns(){
        return this.runs;
    }
}

// Assembler handling
int GetPriorityComponentBP(){
    return 1;
}

void TransferMaterialsToAssemblerInputInventory(IMyAssembler assembler, IMyCargoContainer container, int slot, double amountToTransfer, MyInventoryItem item)
{
    try
    {
        var containerE = containerDilMatrixList
            .Find(cont => cont.GetInventory(0).CanItemsBeAdded((VRage.MyFixedPoint)amountToTransfer, item.Type));
        if(containerE == null){
            Echo("Container not found!");
            return;
        }

        Echo("Pass Find Container!");
        container.GetInventory(0).TransferItemTo(containerE.GetInventory(0), slot, null, true, (VRage.MyFixedPoint)amountToTransfer);
        Echo("Pass Transfer Item to Container!");
        // Not Working Properly doesn't execute the transfer to Assemblers Input Inventory
        if(assembler == null)
        {
            Echo("Assembler == null!!!!");
            return;
        }
        var eer1 = new List<MyInventoryItem>();
        containerE.GetInventory(0).GetItems(eer1);
        var itemToTransfer = eer1.Find(itemE => itemE.Type == item.Type);
        var index = eer1.IndexOf(itemToTransfer);
        containerE.GetInventory(0).TransferItemTo(assembler.GetInventory(0), index, null, true, (VRage.MyFixedPoint)((double)itemToTransfer.Amount / (double)assemblers.Count));
        Echo("Pass Transfer from Container to Assembler!");
    }
    catch(Exception ex)
    {
        Echo($"Error: {ex}");
    }
}

Dictionary<IMyCargoContainer, double> FindContainersWithMaterials(Dictionary<string, double> materials, double amount, IMyAssembler assembler)
{
    var containersWithMaterials = new Dictionary<IMyCargoContainer, double>();
    var materialsTemp = materials.ToDictionary(kvp => kvp.Key.Replace(" ", ""), kvp => kvp.Value * amount);

    foreach (var container in containersList)
    {
        if (container.GetInventory(0).CurrentVolume == 0)
        {
            break;
        }

        var items = new List<MyInventoryItem>();
        container.GetInventory(0).GetItems(items);

        foreach (var item in items)
        {
            if (item.Type.TypeId == "MyObjectBuilder_Ingot")
            {
                var materialName = materialsTemp.Keys.FirstOrDefault(k => k.Contains(item.Type.SubtypeId));
                    double amountNeeded = 0.00;
                if (materialName != null && materialsTemp.TryGetValue(materialName, out amountNeeded))
                {
                    Echo($"{item.Type} Item Found");
                    Echo($"{item.Amount}/{amountNeeded}");

                    var itemSlotId = items.IndexOf(item);

                    if ((double)item.Amount >= amountNeeded)
                    {
                        Echo("Hit AmountNeeded!");
                        TransferMaterialsToAssemblerInputInventory(assembler, container, itemSlotId, amountNeeded, item);
                        containersWithMaterials.Add(container, amountNeeded);
                        return containersWithMaterials;
                    }
                    else
                    {
                        Echo("Hit item.Amount!");
                        TransferMaterialsToAssemblerInputInventory(assembler, container, itemSlotId, (double)item.Amount, item);
                        containersWithMaterials.Add(container, (double)item.Amount);
                        materialsTemp[materialName] -= (double)item.Amount;
                        amountNeeded -= (double)item.Amount;
                    }
                }
            }
        }
    }

    return containersWithMaterials;
}

void TransferItemsNeededForQueue(IMyAssembler assembler)
{
    if (assembler.IsProducing)
    {
        return;
    }
    
    var assemblerInventory = assembler.GetInventory(0);
    var assemblerInventoryMax = assemblerInventory.MaxVolume;
    var assemblerInventoryCurrent = assemblerInventory.CurrentVolume;
    
    var queueItemsL = new List<MyProductionItem>();
    assembler.GetQueue(queueItemsL);
    
    if (queueItemsL.Count == 0)
    {
        return;
    }
    
    var itemFromQueue = _itemBlueprintResult.FirstOrDefault(IB => IB.name.Replace(" ", "").Contains(queueItemsL[0].BlueprintId.SubtypeId.ToString()));
    
    if (itemFromQueue.name == null)
    {
        return;
    }
    
    var totalSpaceTakes = 500.00 * assemblers.Count; // It should be worth 500 items times amount of assemblers
    if (totalSpaceTakes > (double)queueItemsL[0].Amount)
    {
        totalSpaceTakes = (double)queueItemsL[0].Amount;
    }
    
    if ((double)assemblerInventoryCurrent < (double)assemblerInventoryMax)
    {
        FindContainersWithMaterials(itemFromQueue.materials, totalSpaceTakes, assembler);
    }
}

bool CheckForAbilityToCraftWithMaterials(string key){
    
    var itemFromQueue = _itemBlueprintResult.FirstOrDefault(IB => IB.name.Replace(" ", "").Contains(key));
    //Echo($"{itemFromQueue.name} / {itemFromQueue.materials.Count}");
    if(itemFromQueue.name == null) return false;
    //Echo("Passed Check!");
    if(currentItems.Count == 0) ManageCurrentItems();
    //Echo(currentItems.Count.ToString());
    int count = 0;
    foreach (var cI in currentIngots){
        try{
            //Echo(cI.Key);
            var mat = itemFromQueue.materials.FirstOrDefault(IB => IB.Key.Replace(" ", "").Contains(cI.Key));
            //Echo($"{mat.Key}/{mat.Value}");
            count++;
            if(mat.Key == null) continue;
            if(!mat.Key.Replace(" ", "").Contains(cI.Key)) continue;
            //Echo("Found Material Return True!");
            return true;
        }
        catch (Exception ex)
        {
            Echo($"Checking for mat avai for queue failed: {ex}");
            Echo(count.ToString());
        }
    }
    //Echo(count.ToString());
    return false;
            
}

void MainAssembly()
{
    ManageQueueAssembly();

    foreach (var assembler in assemblers)
    {
        LearnBlueprint(assembler, lBlueprints);

        if (assembler.IsQueueEmpty)
        {
            if (queueItems.Count == 0)
            {
                runtimeLaggerDelay = 20;
                return;
            }
            foreach (var pair in queueItems)
            {
                //Echo($"{pair.Key}/{pair.Value}");
                if(CheckForAbilityToCraftWithMaterials(pair.Key.SubtypeId.ToString()) && pair.Value > 0){
                    //Echo("Passed he checs");
                    assembler.AddQueueItem(pair.Key, (decimal)pair.Value);
                }
            }
        }
        else
        {
            CheckAndRefreshAssemblerQueue(assembler);
            TransferItemsNeededForQueue(assembler);
            if (assembler.IsProducing)
            {
                runtimeLaggerDelay = 5;
                var queItems = new List<MyProductionItem>();
                assembler.GetQueue(queItems);
                assembler.CustomName = $"{assemblersName} - {(assembler.IsProducing? "Active" : "Inactive")} - {queItems[0].BlueprintId.SubtypeId.ToString()}";
            }
            else
            {
                runtimeLaggerDelay = 20;
                assembler.CustomName = $"{assemblersName} - {(assembler.IsProducing? "Active" : "Inactive")}";
            }

            ClearAssemblyLine(assembler);
        }
    }
}

void CheckAndRefreshAssemblerQueue(IMyAssembler assembler)
{
    var itemsQueued = new List<MyProductionItem>();
    assembler.GetQueue(itemsQueued);

    if (itemsQueued.Count == 0)
    {
        return;
    }

    var item = itemsQueued[0];

    if (currentItems.ContainsKey(item.BlueprintId.SubtypeId.ToString()) &&
        queueItems.ContainsKey(item.BlueprintId))
    {
        var queuedAmount = queueItems[item.BlueprintId];
        var currentAmount = currentItems[item.BlueprintId.SubtypeId.ToString()];

        if (item.Amount <= queuedAmount - 250 || item.Amount >= queuedAmount + 250 ||
            currentAmount > lcdAssemblerStreamData[item.BlueprintId.SubtypeId.ToString()])
        {
            assembler.ClearQueue();

            if (queueItems.Count == 0)
            {
                queueItems.Remove(queueItems.First().Key);
            }
        }
    }
}

void ClearAssemblyLine(IMyAssembler assembler)
{
    // Get all items in the assembler's output inventory
    var items = new List<MyInventoryItem>();
    assembler.GetInventory(1).GetItems(items);

    // Transfer each item to the first container that can receive it
    foreach (var item in items)
    {
        containersList.Any(cont => {
            var itemsCont = new List<MyInventoryItem>();
            cont.GetInventory(0).GetItems(itemsCont);
            var itemContFound = itemsCont.Find(itemCont => itemCont.Type == item.Type);
            if(itemContFound != null && cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)){
                var index = items.IndexOf(item);
                cont.GetInventory(0).TransferItemFrom(assembler.GetInventory(1), index, null, true, item.Amount);
                //Echo($"Clearing {assembler.CustomName} Status: {cont.GetInventory(0).TransferItemFrom(assembler.GetInventory(1), index, null, true, item.Amount)}");
                return true;
            }
            return false;
        });
    }

    // Log instructions count
    //Echo("Clear Assembly Line method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

void ManageQueueAssembly(){
    // Add desired Queue Items
    foreach (var pair in lcdAssemblerStreamData){

        // First check difference between currentAmount and desiredAmmount
        var currentAmount = 0;
        if(!currentItems.TryGetValue(pair.Key, out currentAmount))
        {
            currentAmount = 0;
        }

        var queueAmount = 0;

        if(currentAmount < pair.Value){

            queueAmount = pair.Value - currentAmount;

            // Make MyDefinitionId
            var _bp = new LearnedBlueprint{
                TypeId = "MyObjectBuilder_BlueprintDefinition",
                SubtypeId = pair.Key
            };

            var itemDeffId = GetBlueprintBack(_bp);
            
            if(assemblers.Count != 0 && assemblers[0].CanUseBlueprint(itemDeffId)){
                //Echo("Item: " + pair.Key + " Usage: True!");
                if(queueItems.ContainsKey(itemDeffId)){
                    queueItems[itemDeffId] = queueAmount/assemblers.Count;
                }
                else{
                    queueItems.Add(itemDeffId, pair.Value/assemblers.Count);
                }
            } 
            else if(queueItems.ContainsKey(itemDeffId)) {
                queueItems.Remove(itemDeffId);
            }
        }
    }
    //Echo("Manage Queue Assembly method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

void PrintQueueList()
{
    foreach(var pair in queueItems)
    {
        Echo($"{pair.Key}/{pair.Value}");
    }
}

// LCD handling
public void ReadLCDData(IMyTextPanel lcdPanel, Dictionary<string, int> dataStream)
{
    var lcdData = lcdPanel.GetPublicText();

    if(string.IsNullOrEmpty(lcdData))
    {
        return;
    }

    var lcdLines = lcdData.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);

    foreach (var line in lcdLines)
    {
        var values = line.Split(':');
        if(values.Length == 2)
        {
            var itemName = values[0].Trim();
            int desiredAmount = 0;
            int.TryParse(values[1].Substring(1 + values[1].IndexOf("/")), out desiredAmount);

            if (!dataStream.ContainsKey(itemName))
            {
                dataStream.Add(itemName, desiredAmount);
            }
            else
            {
                dataStream[itemName] = desiredAmount;
            }
        }
    }
    //Echo($"Read LCD Data method instructions: {Runtime.CurrentInstructionCount}");
}

public void SaveLCDData(IMyTextPanel lcdPanel, Dictionary<string, int> dataStream)
{
    string lcdData = "";

    foreach (var pair in dataStream)
    {
        int amount = 0;
        var currentAmount = currentItems.TryGetValue(pair.Key, out amount) ? amount : 0;
        lcdData += $"{pair.Key}:{currentAmount}/{pair.Value}\n";
    }

    lcdPanel.WritePublicText(lcdData);
    //Echo("Save LCD Data method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

void LoadDataStream(){
    containersList.ForEach(container => {
        var items = new List<MyInventoryItem>(); 
        container.GetInventory(0).GetItems(items);
        items.ForEach(item => {
            if(item.Type.TypeId == "MyObjectBuilder_Component")
            {
                int currentAmount = 0;
                if (lcdAssemblerStreamData.TryGetValue(item.Type.SubtypeId, out currentAmount))
                {
                    lcdAssemblerStreamData[item.Type.SubtypeId] = (int) item.Amount;
                }
                else
                {
                    lcdAssemblerStreamData.Add(item.Type.SubtypeId, (int) item.Amount);
                }
            }
        });
    });
    Echo("Load Data Stream method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

public void ManageCurrentItems()
{
    currentItems.Clear();
    foreach (var container in containersList)
    {
        var items = new List<MyInventoryItem>();
        container.GetInventory(0).GetItems(items);
        if (items.Count == 0)
            continue;
        foreach (var item in items)
        {
            if (item.Type.TypeId == "MyObjectBuilder_Component")
            {
                int amount = 0;
                if (currentItems.TryGetValue(item.Type.SubtypeId, out amount))
                {
                    currentItems[item.Type.SubtypeId] = amount + (int)item.Amount;
                }
                else
                {
                    currentItems[item.Type.SubtypeId] = (int)item.Amount;
                }
            }
        }
    }
    //Echo("Manage Current Items method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

public void ManageCurrentIngotInventory()
{
    currentIngots.Clear();
    foreach (var container in containersList)
    {
        var items = new List<MyInventoryItem>();
        container.GetInventory(0).GetItems(items);
        if (items.Count == 0)
            continue;
        foreach (var item in items)
        {
            if (item.Type.TypeId == "MyObjectBuilder_Ingot")
            {
                int amount = 0;
                if (currentIngots.TryGetValue(item.Type.SubtypeId, out amount))
                {
                    currentIngots[item.Type.SubtypeId] = amount + (int)item.Amount;
                }
                else
                {
                    currentIngots[item.Type.SubtypeId] = (int)item.Amount;
                }
            }
        }
    }
    //Echo("Manage Current Items method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

// Blueprint handling class
// Probably use the {GetBlueprintBack, LearnBlueprint, LoadLearnedBlueprints} and add it into this class
public class LearnedBlueprint
{
    public string TypeId { get; set; }
    public string SubtypeId { get; set; }
}

MyDefinitionId GetBlueprintBack(LearnedBlueprint bpTP)
{
    var blueprintId = MyDefinitionId.Parse($"{bpTP.TypeId}/{bpTP.SubtypeId}");
    return blueprintId;
}

public void LearnBlueprint(IMyAssembler assembler, List<LearnedBlueprint> learnedBlueprints)
{
    var items = new List<MyProductionItem>();
    assembler.GetQueue(items);

    if (items.Count == 0) return;

    var currentItem = items[0].BlueprintId;
    if (currentItem.TypeId.ToString() != "MyObjectBuilder_BlueprintDefinition" && !learnedBlueprints.Any(x => x.SubtypeId == currentItem.SubtypeId.ToString()))
    {
        var blueprint = items[0].BlueprintId;

        var learnedBlueprint = new LearnedBlueprint
        {
            TypeId = blueprint.TypeId.ToString(),
            SubtypeId = blueprint.SubtypeId.ToString()
        };

        learnedBlueprints.Add(learnedBlueprint);

        var data = "";
        foreach (var bp in learnedBlueprints)
        {
            data += $"{bp.TypeId};{bp.SubtypeId}\n";
        }

        Me.CustomData = data;
    }
    //Echo("Learn Blueprint method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

public void LoadLearnedBlueprints()
{
    // Load learned blueprints from LCD data
    if (lcdAssemblerStreamData.Count > 0)
    {
        foreach (var pair in lcdAssemblerStreamData)
        {
            if (!lBlueprints.Any(bp => bp.SubtypeId == pair.Key))
            {
                var learnedBlueprint = new LearnedBlueprint
                {
                    TypeId = "MyObjectBuilder_BlueprintDefinition",
                    SubtypeId = pair.Key
                };
                lBlueprints.Add(learnedBlueprint);
            }
        }
    }

    // Load learned blueprints from CustomData
    var data = Me.CustomData;
    if (string.IsNullOrEmpty(data))
    {
        return;
    }

    var lines = data.Split('\n');
    foreach (var line in lines)
    {
        if (string.IsNullOrEmpty(line))
        {
            continue;
        }

        var values = line.Split(';');
        if (values.Length != 2)
        {
            continue;
        }

        if (lBlueprints.Any(bp => bp.SubtypeId == values[1]))
        {
            break;
        }

        var learnedBlueprint = new LearnedBlueprint
        {
            TypeId = values[0],
            SubtypeId = values[1]
        };
        lBlueprints.Add(learnedBlueprint);
    }

    //Echo("Load Learned Blueprints method instructions: " + Runtime.CurrentInstructionCount.ToString());
}