// Deuterium Refinery Manager
// Created by xSupeFly
// Discord: xSupeFly#2911
//
/// 
// Issues:
// 
// - TimeActiveTotal is Saving OK but Loading doesn't load the saved value but it resets to TimeSpan.Zero
// - Material UsageTotal is not working. Not saving transfered item amount.


//List
List<IMyCargoContainer> containerList = new List<IMyCargoContainer>();
List<IMyCargoContainer> allContainers = new List<IMyCargoContainer>();
List<IMyRefinery> deutRefList = new List<IMyRefinery>();

// Predefined list
List<IMyCargoContainer> deutIntermixContainersList = new List<IMyCargoContainer>();
List<IMyCargoContainer> refineryMatsContainersList = new List<IMyCargoContainer>();

// Variables
int DilithiumMatrixMade = 0;
int DilithiumMatrixAllTimeCountTransfered = 0;
int IntermixAllTimeCountTransfered = 0;
int DilithiumMatrixAllTimeCountTransferedMoney = 0;
int IntermixAllTimeCountTransferedMoney = 0;
TimeSpan TimeSinceFirstRun = TimeSpan.Zero;
TimeSpan TimeActiveTotal = TimeSpan.Zero;
TimeSpan lastSaveTime = TimeSpan.Zero;
int DeuteriumUsageTotal = 0;
int AntiDeuteriumUsageTotal = 0;
IMyTextPanel lcdPanel;
IEnumerator<bool> _stateMachine;
int activeRefineries = 0;

Program()
{
    GridTerminalSystem.GetBlocksOfType(deutRefList, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(containerList, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(allContainers, b =>
    {
        if(b.CustomName.Contains("Refinery Materials")) return false;
        return true;
    });
    lcdPanel = GridTerminalSystem.GetBlockWithName("LCD Deuterium Processor Manager") as IMyTextPanel;
    LoadDataFromCD();
    FindAssignedContainers();

    _stateMachine = RunStuffOverTime();
    Runtime.UpdateFrequency |= UpdateFrequency.Once;
}

void Main(string argument, UpdateType updateType){
    TimeSinceFirstRun += Runtime.TimeSinceLastRun;
    if ((updateType & UpdateType.Once) == UpdateType.Once)
    {
        RunStateMachine();
    }
}

public void RunStateMachine()
{
    if (_stateMachine != null) 
    {
        bool hasMoreSteps = _stateMachine.MoveNext();

        if (hasMoreSteps)
        {
            Runtime.UpdateFrequency |= UpdateFrequency.Once;
        } 
        else 
        {
            _stateMachine.Dispose();
            _stateMachine = null;
        }
    }
}

public IEnumerator<bool> RunStuffOverTime() 
{
    yield return true;

    int counter = 0;

    while (true) 
    {
        Echo("Performance (Ms): " + Runtime.LastRunTimeMs);
        Echo("Active Refineries: " + activeRefineries);
        Echo("DilithiumMatrixAmount: " + DilithiumMatrixAmountCur);
        if(500 > counter){
            counter++;
        }else
        {
            counter = 0;
            RunStuffOverTimeT();
        }

        yield return true;
    }
}

public void RunStuffOverTimeT()
{
    TransferMatrixResourceToRefMatsCargo();
    FillDeutProcessors();
    ClearDeutProcessors();
    RenameCargoContainer();
    SaveToCD();
    SaveLCDData();
}


void ClearDeutProcessors(){
    deutRefList.ForEach(deutRefinery =>
    {
        var refineryOutputItems = new List<MyInventoryItem>();
        deutRefinery.GetInventory(1).GetItems(refineryOutputItems);
        if(refineryOutputItems.Count == 0) return;
        IntermixAllTimeCountTransfered += (int)refineryOutputItems[0].Amount;
        IntermixAllTimeCountTransferedMoney = IntermixAllTimeCountTransfered * 1494;

        deutRefinery.GetInventory(1).TransferItemTo(deutIntermixContainersList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(refineryOutputItems[0].Amount, refineryOutputItems[0].Type)).GetInventory(0), 0, null, true, refineryOutputItems[0].Amount);
    });
}
int DilithiumMatrixAmountCur = 0;
IMyCargoContainer FindItemInRefineryMaterialsContainer(string itemName, out int pos){
    int position = 0;
    IMyCargoContainer carg = refineryMatsContainersList.Find(refMats =>
    {
        var refMatsItems = new List<MyInventoryItem>();
        refMats.GetInventory(0).GetItems(refMatsItems);

        return refMatsItems.Any(refMatsItem => {
            if(refMatsItem.Type.SubtypeId == itemName) {
                position = refMatsItems.IndexOf(refMatsItem);
                if(itemName == "DilithiumMatrix") DilithiumMatrixAmountCur = (int)refMatsItem.Amount;
                return true;
            }
            return false;
        });
    });
    pos = position;
    return carg;
}

bool TransferMaterialsIntoProcessor(IMyRefinery refinery, string type, VRage.MyFixedPoint amount){
    int pos = 0;
    IMyCargoContainer contFound = FindItemInRefineryMaterialsContainer(type, out pos);
    if (contFound != null)
    {
        contFound.GetInventory(0).TransferItemTo(refinery.GetInventory(0), pos, null, true, amount);
        return true;
    }
    else{
        return false;
    }
}

void TransferMatrixResourceToRefMatsCargo(){
    DilithiumMatrixMade = 0;
    allContainers.ForEach(container =>
    {
        int pos = -1;
        // Get items from the current container and get the Matrix position and Amount
        var items = new List<MyInventoryItem>();
        container.GetInventory(0).GetItems(items);

        var itemFound = items.Find(item => item.Type.SubtypeId == "DilithiumMatrix");
        pos = items.IndexOf(itemFound);
        DilithiumMatrixMade += (int)itemFound.Amount;
        DilithiumMatrixAllTimeCountTransfered += (int)itemFound.Amount;
        DilithiumMatrixAllTimeCountTransferedMoney = DilithiumMatrixAllTimeCountTransfered * 180;

        // Now we transfer all of it into one of Refinery Materials cargo container
        refineryMatsContainersList.Find(containerR => containerR.GetInventory(0).CanItemsBeAdded(itemFound.Amount, itemFound.Type)).GetInventory(0).TransferItemFrom(container.GetInventory(), pos, null, true, itemFound.Amount);
    });
}

void BalanceProcessorInput(IMyRefinery refin, MyInventoryItem item){
    if( refin == null || item == null) return;
    IMyCargoContainer carg = refineryMatsContainersList.Find(container => container.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type));
    if(carg == null) RenameCargoContainer();
    
    var items = new List<MyInventoryItem>();
    refin.GetInventory(0).GetItems(items);
    items.Find(itemf => itemf.Type.SubtypeId == item.Type.SubtypeId);
    try
    {
        refin.GetInventory(0).TransferItemTo(carg.GetInventory(0), items.IndexOf(item), null, true, item.Amount);
    }
    catch (Exception e)
    {
        
        Echo("Failed to balance processor: " + e);
    }
    
}

void FillDeutProcessors(){
    var pos = 0;
    var cTemp = FindItemInRefineryMaterialsContainer("DilithiumMatrix", out pos);
    List<IMyRefinery> enabledRefineries = new List<IMyRefinery>();
    for (int i = 1; i < (deutRefList.Count + 1); i++)
    {
        if (((int)DilithiumMatrixAmountCur / i) < 5)
        {
            break;
        }
        enabledRefineries.Add(deutRefList[i-1]);
    }
    activeRefineries = enabledRefineries.Count;

    enabledRefineries.ForEach(deutRefinery =>
    {
        var refineryInputItems = new List<MyInventoryItem>();
        deutRefinery.GetInventory(0).GetItems(refineryInputItems);
        
        bool dilMatrixPresent = false;
        bool deuteriumPresent = false;
        bool antiDeuteriumPresent = false;

        VRage.MyFixedPoint dilMatrixCount = 500*deutRefList.Count;
        VRage.MyFixedPoint deuteriumCount = 2000;
        VRage.MyFixedPoint antiDeuteriumCount = 2000;

        if (refineryInputItems.Count <= 3)
        {
            refineryInputItems.ForEach(refineryInputItem =>
            {
                if (refineryInputItem.Type.SubtypeId.Contains("AntiDeuterium"))
                {
                    antiDeuteriumPresent = true;

                    if(refineryInputItem.Amount < 500){
                        if(TransferMaterialsIntoProcessor(deutRefinery, refineryInputItem.Type.SubtypeId, (antiDeuteriumCount - refineryInputItem.Amount))){}
                    } else if (refineryInputItem.Amount > 2000){
                        BalanceProcessorInput(deutRefinery, refineryInputItem);
                    }
                }
                else if (refineryInputItem.Type.SubtypeId.Contains("DilithiumMatrix"))
                {
                    dilMatrixPresent = true;
                }
                else if (refineryInputItem.Type.SubtypeId.Contains("Deuterium"))
                {
                    deuteriumPresent = true;

                    if(refineryInputItem.Amount < 500){
                        if(TransferMaterialsIntoProcessor(deutRefinery, refineryInputItem.Type.SubtypeId, (deuteriumCount - refineryInputItem.Amount))){}
                    } else if (refineryInputItem.Amount > 2000){
                        BalanceProcessorInput(deutRefinery, refineryInputItem);
                    }
                }
            });
            if(!dilMatrixPresent){
                if(TransferMaterialsIntoProcessor(deutRefinery, "DilithiumMatrix", (VRage.MyFixedPoint)((int)DilithiumMatrixAmountCur/enabledRefineries.Count))){
                }
            }

            if(!deuteriumPresent){
                if(TransferMaterialsIntoProcessor(deutRefinery, "Deuterium", deuteriumCount)){
                    DeuteriumUsageTotal += (int)deuteriumCount;
                }
            }
                
            if(!antiDeuteriumPresent){
                if(TransferMaterialsIntoProcessor(deutRefinery, "AntiDeuterium", antiDeuteriumCount)){
                    AntiDeuteriumUsageTotal += (int)antiDeuteriumCount;
                }
            }
        }
    });
}

void FindAssignedContainers(){
    containerList.ForEach(container =>
    {
        var items = new List<MyInventoryItem>();
        container.GetInventory(0).GetItems(items);
        if (container.CustomName.Contains("Deuterium Intermix"))
        {
            deutIntermixContainersList.Add(container);
        }
        else if (container.CustomName.Contains("Refinery Materials"))
        {
            refineryMatsContainersList.Add(container);
        }
    });
}

void RenameCargoContainer(){
    containerList.ForEach(container =>
    {
        if (container.CustomName.Contains("Deuterium Intermix")){
            double volume = ((double)container.GetInventory(0).CurrentVolume / (double)container.GetInventory(0).MaxVolume) * 100;
            if(volume > 101){
                IMyCargoContainer contair = containerList.Find(containerE => !containerE.CustomName.Contains("Deuterium Intermix") || !containerE.CustomName.Contains("Refinery Materials"));
                contair.CustomName = "Deuterium Intermix";
            }
            else{
                container.CustomName = "Deuterium Intermix (" + volume + "%)";
            }
        }
        else if(container.CustomName.Contains("Refinery Materials")){
            double volume = ((double)container.GetInventory(0).CurrentVolume / (double)container.GetInventory(0).MaxVolume) * 100;
            if(volume > 101){
                IMyCargoContainer contair = containerList.Find(containerE => !containerE.CustomName.Contains("Deuterium Intermix") || !containerE.CustomName.Contains("Refinery Materials"));
                contair.CustomName = "Refinery Materials";
            }
            else{
                container.CustomName = "Refinery Materials (" + volume + "%)";
            }
        }
        else{
            if(deutIntermixContainersList.Count != 0 || refineryMatsContainersList.Count != 0) return;

            var items = new List<MyInventoryItem>();
            container.GetInventory(0).GetItems(items);

            if(deutIntermixContainersList.Count == 0 && (items.Any(item => {
                if(item.Type.SubtypeId == "AntiDeuterium" || item.Type.SubtypeId == "DilithiumMatrix" || item.Type.SubtypeId == "Deuterium"){
                    return false;
                }
                return true;
            }) || items.Count == 0)){
                container.CustomName = "Deuterium Intermix";
                deutIntermixContainersList.Add(container);
                return;
            }
            else if(refineryMatsContainersList.Count == 0 && (!items.Any(item => item.Type.SubtypeId == "DeuteriumIntermix") || items.Count == 0)){
                container.CustomName = "Refinery Materials";
                refineryMatsContainersList.Add(container);
                return;
            }
        }
    });
}
void SaveToCD(){
    var data = "";
    // Saving TimeActiveTotal is bonked it always starts from TimeSpan.Zero
    if (TimeSinceFirstRun.Milliseconds > TimeSpan.Zero.Milliseconds)
    {
        TimeSpan timeSinceLastSave = TimeSinceFirstRun - lastSaveTime;
        TimeActiveTotal += timeSinceLastSave;
    }
    data = "DilithiumMatrixAllTimeCountTransfered:" + DilithiumMatrixAllTimeCountTransfered + "\n" + 
    "DilithiumMatrixAllTimeCountTransferedMoney:" + DilithiumMatrixAllTimeCountTransferedMoney + "\n" + 
    "IntermixAllTimeCountTransfered:" + IntermixAllTimeCountTransfered + "\n" + 
    "IntermixAllTimeCountTransferedMoney:" + IntermixAllTimeCountTransferedMoney + "\n" + 
    "DeuteriumUsageTotal:" + DeuteriumUsageTotal + "\n" + 
    "AntiDeuteriumUsageTotal:" + AntiDeuteriumUsageTotal + "\n"+ 
    "TimeActiveTotal:" + TimeActiveTotal + "\n";
    Me.CustomData = data;
    lastSaveTime = TimeSinceFirstRun;
}
void LoadDataFromCD(){
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

        var values = line.Split(':');
        if (values.Length != 2)
        {
            continue;
        }

        if(values[0] == "DilithiumMatrixAllTimeCountTransfered"){
            DilithiumMatrixAllTimeCountTransfered = int.Parse(values[1]);
        }
        else if(values[0] == "DilithiumMatrixAllTimeCountTransferedMoney"){
            DilithiumMatrixAllTimeCountTransferedMoney = int.Parse(values[1]);
        }
        else if (values[0] == "IntermixAllTimeCountTransfered"){
            IntermixAllTimeCountTransfered = int.Parse(values[1]);
        }
        else if (values[0] == "IntermixAllTimeCountTransferedMoney"){
            IntermixAllTimeCountTransferedMoney = int.Parse(values[1]);
        }
        else if( values[0] == "TimeActiveTotal")
        {
            try{

            TimeActiveTotal = TimeSpan.Parse(values[1]);
            Echo("total: " + TimeActiveTotal);
            string ex = "total: " + TimeActiveTotal;
            throw new Exception(ex);
            }catch
            {

            }
        }
        else if( values[0] == "DeuteriumUsageTotal")
        {
            DeuteriumUsageTotal = int.Parse(values[1]);
        }
        else if( values[0] == "AntiDeuteriumUsageTotal")
        {
            AntiDeuteriumUsageTotal = int.Parse(values[1]);
        }
    }
}

void SaveLCDData(){

    string lcdData = "";
    lcdData = "Matrixes made in 7 seconds: " + DilithiumMatrixMade + "\n" + "Matrixes transfered total: " + DilithiumMatrixAllTimeCountTransfered + "\n" + "Money made for Matrixes: " + DilithiumMatrixAllTimeCountTransferedMoney + "\n" + "Intermix made and transfered: " + IntermixAllTimeCountTransfered + "\n" + "Money made for Intermix: " + IntermixAllTimeCountTransferedMoney + "\n" + "Deuterium used: " + DeuteriumUsageTotal + "\n" + "Anti-Deuterium used: " + AntiDeuteriumUsageTotal + "\n" + "TimeSinceFirstRun: " + TimeSinceFirstRun + "\n" + "TimeActiveTotal: " + TimeActiveTotal + "\n" + "Performance (Ms): " + Runtime.LastRunTimeMs;

    lcdPanel.WritePublicText(lcdData);
}