// Assembler Manager
// Created by xSupeFly
// Discord: xSupeFly#2911
// 
// Assemblers will be filled with queue of required compenents by setting them in LCD with format -> Name: (current amount) / (desired amount) - Done
// Assemblers will be Cleaned of components ( will be moved to available containers that can hold the amount) - Done
// LCD will show queue and time of completion in dd:hh:mm:ss - In Progress
// Reformat code
// Queue multiple items at same time and change priority depending on need -> This we can use priority to assign free assemblers to each items to be created
//
// Producement Will be stored in Storage property for statistics -> Total made


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

// Unfortunatelly KEEN doesn't want to give us access to Blueprint.Result to get materials needed to make the blueprint item so this Dictionary is side way to make it possible
// variables
int runtimeLagger = 0;
int runtimeLaggerDelay = 5; // Default 5 seconds Non-Active 20 seconds -> This works as limiter for resource drainer 
                            // ( if there was an activity in the 5 sec timer it will be left at 5 second interval if not then it 
                            // will switch to 20 sec interval of checking)
Logger _logger;

public Program(){
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    _logger = new Logger();
    GridTerminalSystem.GetBlocksOfType(assemblers, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(containersList, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(lcdPanelList, b => b.CubeGrid == Me.CubeGrid);

    GridTerminalSystem.GetBlocksOfType(containerDilMatrixList, b => b.CustomName.Contains("DilithiumMatrix Cargo"));
    // Move to an method lazy mate...
    assemblers = assemblers.Where(assembler => assembler.CustomName.Contains("Assembler")).ToList();

    LoadDataStream();
    LoadLearnedBlueprints();
}

public void Main(string argument, UpdateType updateSource)
{
    // RuntimeLag + 5sec
    if (runtimeLagger < runtimeLaggerDelay)
    {
        runtimeLagger++;
        //Echo("Main method instructions (runtimeLagger): " + Runtime.CurrentInstructionCount.ToString());
    }
    else
    {
        runtimeLagger = 0;
        _logger.SetRuns(_logger.GetRuns() + 1);
        //LCD handleout
        ReadLCDData(lcdPanelList.Find(panel => panel.CustomName == "LCD Assembler Manager Main"), lcdAssemblerStreamData);
        SaveLCDData(lcdPanelList.Find(panel => panel.CustomName == "LCD Assembler Manager Main"), lcdAssemblerStreamData);

        // Search the containers for items
        ManageCurrentItems();

        // Assembler handleout
        try
        {
            MainAssembly();
        }
        catch (Exception ex)
        {
            Echo("Error: " + ex.ToString());
        }

        // This is for EnemyOwned Assemblers -> The cargo and assemblers have to be connected!!! and set to share with all to work!!!
        List<IMyAssembler> asemblyEnemy = assemblers.Where(assembler => assembler.OwnerId != Me.OwnerId).ToList();
        asemblyEnemy.ForEach(asse => {
            var items = new List<MyInventoryItem>();
            asse.GetInventory(1).GetItems(items);

            items.ForEach(item => {

                Echo(containerDilMatrixList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).GetInventory(0).TransferItemFrom(asse.GetInventory(1), 0, null, true, item.Amount).ToString());
            });
        });

        // Logs
        //QueueListPrint();
        _logger.SetRuntime(Runtime.TimeSinceLastRun);
        Echo(Runtime.MaxInstructionCount.ToString());
        Echo("Main method instructions (Main Loop): " + Runtime.CurrentInstructionCount.ToString());
        Echo(_logger.GetRuntime().ToString());
        Echo("Script runs: " + _logger.GetRuns());

        Echo("Assemblers: " + assemblers.Count.ToString());
        //_logger.GetLoggerLog().ForEach(log => Echo(log.LogType + " : " + log.LogMsg));
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
/*
// This Method will be for finding and transfering correct amount of needed materials for Current Queue Item
// NEEDS REWRITING!!!!
// Get the amount of needed materials for queue item
void TransferItemsNeededForQueue(){
    foreach (var assembler in assemblers)
    {
        if (!assembler.IsQueueEmpty)
        {
            // Get the required materials for the current item in queue
            var queue = new List<MyProductionItem>();
            assembler.GetQueue(queue);
            if(queue.Count == 0) return;

            var currentItem = queue.FirstOrDefault();
            var blueprintDefinition = Sandbox.Definitions.MyDefinitionManager.Static.GetBlueprintDefinition(currentItem.BlueprintId);
            var requiredMaterials = blueprintDefinition.Results;

            // Find the required materials in the containers
            var availableMaterials = new Dictionary<VRage.Game.MyDefinitionId, VRage.MyFixedPoint>();
            foreach (var material in requiredMaterials)
            {
                var amountNeeded = material.Amount;
                foreach (var container in containersList)
                {
                    var amountAvailable = container.GetInventory().GetItemAmount(material.Id);
                    if (amountAvailable >= amountNeeded)
                    {
                        availableMaterials.Add(material.Id, amountNeeded);
                        break;
                    }
                    else if (amountAvailable > 0)
                    {
                        availableMaterials.Add(material.Id, amountAvailable);
                        amountNeeded -= amountAvailable;
                    }
                }
            }

            // Send the required materials to the assembler's input inventory
            var availableInventoryVolume = assembler.InputInventory.MaxVolume - assembler.InputInventory.CurrentVolume;
            foreach (var material in availableMaterials)
            {
                var amountPerSlot = assembler.InputInventory.MaxStackVolume(material.Key);
                var numSlotsNeeded = Math.Min((int)Math.Floor((double)availableInventoryVolume / amountPerSlot), (int)Math.Ceiling((double)material.Value / amountPerSlot));
                foreach (var container in containers)
                {
                    var amountTaken = container.GetInventory().TakeItemsById(material.Key, amountPerSlot * numSlotsNeeded);
                    if (amountTaken > 0)
                    {
                        assembler.InputInventory.InsertItems(material.Key, amountTaken);
                        availableInventoryVolume -= amountTaken;
                    }
                }
            }

            // Split the amount of materials to create whole item amount
            var currentItemName = currentItem.Blueprint.Id.SubtypeId.ToString();
            assembler.InputInventory.Split(currentItemName);
        }
    }
}
*/
void MainAssembly(){
    ManageQueueAssembly();

    foreach( var assembler in assemblers){        
        LearnBlueprint(assembler, lBlueprints);
        if(assembler.IsQueueEmpty)
        {
            if(queueItems.Count == 0){
                runtimeLaggerDelay = 20;
                return;
            }
            foreach(KeyValuePair<MyDefinitionId, int> pair in queueItems){
                assembler.AddQueueItem(pair.Key, (decimal)pair.Value);
            }
            //Echo("Main method instructions (Queue Empty - Yes): " + Runtime.CurrentInstructionCount.ToString());
        }else{
            //
            /// Make method for checking and refreshing Assembler queue
            //
            if(assembler.IsProducing)
            {
                runtimeLaggerDelay = 5;
            }
            else{
                runtimeLaggerDelay = 20;
            }

            var itemsQueued = new List<MyProductionItem>();

            assembler.GetQueue(itemsQueued);

            if(currentItems.ContainsKey(itemsQueued[0].BlueprintId.SubtypeId.ToString()) && queueItems.ContainsKey(itemsQueued[0].BlueprintId) && itemsQueued.Count != 0)
            {
                //Echo("ItemsQueued amount: " + itemsQueued[0].Amount + "\nQueueItems amount: " + queueItems[itemsQueued[0].BlueprintId] + "\nCurrentItems: " + currentItems[itemsQueued[0].BlueprintId.SubtypeId.ToString()] + "\nQueueItems: " + queueItems[itemsQueued[0].BlueprintId]);
                if(itemsQueued[0].Amount <= (queueItems[itemsQueued[0].BlueprintId] - 150)){
                    assembler.ClearQueue();
                }
                else if(itemsQueued[0].Amount >= (queueItems[itemsQueued[0].BlueprintId] + 150)){
                    assembler.ClearQueue();
                }
                else if(currentItems[itemsQueued[0].BlueprintId.SubtypeId.ToString()] > lcdAssemblerStreamData[itemsQueued[0].BlueprintId.SubtypeId.ToString()]){
                    //_logger.AddLogItem(new Log("Queue removal" ,"Removed " + queueItems.First().Key.ToString() + " from queue and assemblers"));
                    assembler.ClearQueue();
                    if(queueItems.Count <= 0)
                        queueItems.Remove(queueItems.First().Key);
                }
                else{
                    //Echo("None Hit!");
                }
            }
            //Echo("Main method instructions (Queue Empty - False): " + Runtime.CurrentInstructionCount.ToString());
        }
        ClearAssemblyLine(assembler);
    }
}

void ClearAssemblyLine(IMyAssembler assembler)
{
    var items = new List<MyInventoryItem>();

    assembler.GetInventory(1).GetItems(items);

    items.ForEach(item => assembler.GetInventory(1).TransferItemTo(containersList.Find(containerOut => containerOut.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).GetInventory(0), 0, null, true, item.Amount));
    //Echo("Clear Assembly Line method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

void ManageQueueAssembly(){
    // Add desired Queue Items
    foreach (KeyValuePair<string, int> pair in lcdAssemblerStreamData){

        // First check difference between currentAmount and desiredAmmount
        var currentAmount = 0;
        if(!currentItems.TryGetValue(pair.Key, out currentAmount))
        {
            currentAmount = 0;
        }

        int queueAmount = 0;

        if(currentAmount < pair.Value){

            queueAmount = pair.Value - currentAmount;

            // Make MyDefinitionId
            var _bp = new LearnedBlueprint{
                TypeId = "MyObjectBuilder_BlueprintDefinition",
                SubtypeId = pair.Key
            };

            var itemDeffId = GetBlueprintBack(_bp);
            
            if(assemblers.Count != 0){
                if(assemblers[0].CanUseBlueprint(itemDeffId)){
                    //Echo("Item: " + pair.Key + " Usage: True!");
                    if(queueItems.ContainsKey(itemDeffId)){
                        queueItems[itemDeffId] = queueAmount/assemblers.Count;
                    }
                    else{
                        queueItems.Add(itemDeffId, pair.Value/assemblers.Count);
                    }
                }
                else{
                    if(queueItems.ContainsKey(itemDeffId)) queueItems.Remove(itemDeffId);
                }
            } 
        }
    }
    //Echo("Manage Queue Assembly method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

void QueueListPrint(){
    foreach(KeyValuePair<MyDefinitionId, int> pair in queueItems){
        Echo(pair.Key.ToString() + "/" + pair.Value);
    }
}

// LCD handling
public void ReadLCDData(IMyTextPanel lcdPanel, Dictionary<string, int> dataStream){

    string lcdData = lcdPanel.GetPublicText();

    if(lcdData.Length == 0) return;

    string[] lcdLines = lcdData.Split(new char[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);

    foreach (string line in lcdLines)
    {
        string[] values = line.Split(':');
        if(values.Length == 2){

            string itemName = values[0].Trim();
            int desiredAmount = 0;

            int.TryParse(values[1].Substring(1 + values[1].IndexOf("/")), out desiredAmount);

            if (!dataStream.ContainsKey(itemName)){
                dataStream.Add(itemName, desiredAmount);
            }
            else{
                dataStream[itemName] = desiredAmount;
            }
        }
    }
    //Echo("Read LCD Data method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

public void SaveLCDData(IMyTextPanel lcdPanel, Dictionary<string, int> dataStream){

    string lcdData = "";
    int currentAmount = 0;

    foreach (KeyValuePair<string, int> pair in dataStream){
        if(currentItems.ContainsKey(pair.Key)){
            currentAmount = currentItems[pair.Key];
        }
        else{
            currentAmount = 0;
        }
        lcdData += pair.Key + ":" + currentAmount.ToString()  + "/" + pair.Value.ToString() + "\n";
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
                if(!lcdAssemblerStreamData.ContainsKey(item.Type.SubtypeId)){
                    lcdAssemblerStreamData.Add(item.Type.SubtypeId, (int) item.Amount);
                }
                else{
                    lcdAssemblerStreamData[item.Type.SubtypeId] = (int) item.Amount;
                }
            }
        });
    });
    Echo("Load Data Stream method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

public void ManageCurrentItems(){
    currentItems.Clear();
    containersList.ForEach(container => {
        var items = new List<MyInventoryItem>();
        container.GetInventory(0).GetItems(items);
        if(items.Count == 0) return;

        items.ForEach(item => {
            if(item.Type.TypeId == "MyObjectBuilder_Component")
            {
                if(currentItems.ContainsKey(item.Type.SubtypeId)){
                    currentItems[item.Type.SubtypeId] += (int) item.Amount;
                }
                else{
                    currentItems.Add(item.Type.SubtypeId, (int) item.Amount);
                }
                /// Find if clearing the array and then adding items is faster then just rewriting the value
            }
        });
    });
    //Echo("Manage Current Items method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

// Blueprint handling class
// Probably use the {GetBlueprintBack, LearnBlueprint, LoadLearnedBlueprints} and add it into this class
public class LearnedBlueprint
{
    public string TypeId {get; set;}
    public string SubtypeId {get; set;}
}

MyDefinitionId GetBlueprintBack(LearnedBlueprint bpTP)
{
    MyDefinitionId blueprintId = new MyDefinitionId();
    blueprintId = MyDefinitionId.Parse(bpTP.TypeId + "/" + bpTP.SubtypeId);

    return blueprintId;
}

public void LearnBlueprint(IMyAssembler assembler, List<LearnedBlueprint> learnedBlueprints){

    List<MyProductionItem> Items = new List<MyProductionItem>();

    assembler.GetQueue(Items);
    
    if(Items.Count == 0) return;
    
    var currentItem = Items[0].BlueprintId;
    
    if(currentItem.TypeId.ToString() != "MyObjectBuilder_BlueprintDefinition" && !learnedBlueprints.Any(x => x.SubtypeId == currentItem.SubtypeId.ToString())){
        var blueprint = Items[0].BlueprintId;

        var learnedBlueprint = new LearnedBlueprint{
            TypeId = blueprint.TypeId.ToString(),
            SubtypeId = blueprint.SubtypeId.ToString()
        };

        learnedBlueprints.Add(learnedBlueprint);

        var data = "";
        foreach (var bp in learnedBlueprints)
        {
            data += bp.TypeId + ";" + bp.SubtypeId + "\n";
        }
        Me.CustomData = data;
    }
    //Echo("Learn Blueprint method instructions: " + Runtime.CurrentInstructionCount.ToString());
}

public void LoadLearnedBlueprints()
{
    if(lcdAssemblerStreamData.Count > 0){
        foreach ( KeyValuePair<string, int> pair in lcdAssemblerStreamData){
            var _LearnedBlueprint = new LearnedBlueprint{
                TypeId = "MyObjectBuilder_BlueprintDefinition",
                SubtypeId = pair.Key
            };
            lBlueprints.Add(_LearnedBlueprint);
        }
    }

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

        if(lBlueprints.Any(bp => bp.SubtypeId == values[1])) break;
        
        var learnedBlueprint = new LearnedBlueprint
        {
            TypeId = values[0],
            SubtypeId = values[1]
        };
        lBlueprints.Add(learnedBlueprint);
    }
    //Echo("Load Learned Blueprints method instructions: " + Runtime.CurrentInstructionCount.ToString());
}