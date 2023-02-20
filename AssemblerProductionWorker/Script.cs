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
List<IMyAssembler> assemblers = new List<IMyAssembler>();
List<LearnedBlueprint> lBlueprints = new List<LearnedBlueprint>();
List<IMyTextPanel> lcdPanelList = new List<IMyTextPanel>();

// Dictionary
Dictionary<string, int> lcdAssemblerStreamData = new Dictionary<string, int>();
Dictionary<MyDefinitionId, int> queueItems = new Dictionary<MyDefinitionId, int>();
Dictionary<string, int> currentItems = new Dictionary<string, int>();

// variables
int runtimeLagger = 0;

public Program(){
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    GridTerminalSystem.GetBlocksOfType(assemblers);
    GridTerminalSystem.GetBlocksOfType(containersList);
    GridTerminalSystem.GetBlocksOfType(lcdPanelList);
    LoadDataStream();
    LoadLearnedBlueprints();
}

public void Main(string argument, UpdateType updateSource){

    // RuntimeLag + 5sec
    if(runtimeLagger < 5)
    {
        runtimeLagger++;
    }
    else{
        runtimeLagger = 0;
        //LCD handleout
        ReadLCDData(lcdPanelList.Find(panel => panel.CustomName == "LCD Assembler Manager Main"), lcdAssemblerStreamData);
        SaveLCDData(lcdPanelList.Find(panel => panel.CustomName == "LCD Assembler Manager Main"), lcdAssemblerStreamData);

        // Assembler handleout
        MainAssembly();
        //QueueListPrint();
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
            foreach(KeyValuePair<MyDefinitionId, int> pair in queueItems){
                assembler.AddQueueItem(pair.Key, (decimal)pair.Value);
            }
        }else{
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
                else if(currentItems[itemsQueued[0].BlueprintId.SubtypeId.ToString()] > lcdAssemblerStreamData[itemsQueued[0].BlueprintId.SubtypeId. ToString()]){
                    assembler.ClearQueue();
                }
                else{
                    //Echo("None Hit!");
                }
            }
        }
        ClearAssemblyLine(assembler);
    }
}

void ClearAssemblyLine(IMyAssembler assembler)
{
    var items = new List<MyInventoryItem>();

    assembler.GetInventory(1).GetItems(items);

    items.ForEach(item => assembler.GetInventory(1).TransferItemTo(containersList.Find(containerOut => containerOut.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).GetInventory(0), 0, null, true, item.Amount));
    
}

void ManageQueueAssembly(){
    // Add desired Queue Items
    foreach (KeyValuePair<string, int> pair in lcdAssemblerStreamData){

        // First check difference between currentAmount and desiredAmmount
        var currentAmount = GetItemAmountInInventory(pair.Key);

        if(currentItems.ContainsKey(pair.Key)){
            currentItems[pair.Key] = currentAmount;
        }else{
            currentItems.Add(pair.Key, currentAmount);
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
            if(queueItems.ContainsKey(itemDeffId)){
                queueItems[itemDeffId] = queueAmount/assemblers.Count;
            }
            else{
                queueItems.Add(itemDeffId, pair.Value/assemblers.Count);
            }
        }
    }
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
}

// Used for Getting the total current amount of given item
public int GetItemAmountInInventory(string itemName)
{
    int amount = 0;

    containersList.ForEach(container => {

        var containerInventory = container.GetInventory(0);

        var items = new List<MyInventoryItem>();

        containerInventory.GetItems(items);

        items.ForEach(item => {
            if(item.Type.TypeId == "MyObjectBuilder_Component" && item.Type.SubtypeId == itemName)
            {
                amount += (int) item.Amount;
            }
        });
    });
    return amount;
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
    }

    var data = "";
    foreach (var bp in learnedBlueprints)
    {
        data += bp.TypeId + ";" + bp.SubtypeId + "\n";
    }
    Me.CustomData = data;
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
}