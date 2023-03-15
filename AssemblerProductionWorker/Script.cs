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

// Dictionary
Dictionary<string, int> lcdAssemblerStreamData = new Dictionary<string, int>();
Dictionary<MyDefinitionId, int> queueItems = new Dictionary<MyDefinitionId, int>();
Dictionary<string, int> currentItems = new Dictionary<string, int>();

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