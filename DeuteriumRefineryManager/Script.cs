//List
List<IMyCargoContainer> containerList = new List<IMyCargoContainer>();
List<IMyCargoContainer> allContainers = new List<IMyCargoContainer>();
List<IMyRefinery> deutRefList = new List<IMyRefinery>();

// Predefined list
List<IMyCargoContainer> deutIntermixContainersList = new List<IMyCargoContainer>();
List<IMyCargoContainer> refineryMatsContainersList = new List<IMyCargoContainer>();

// Variables
int programDelay = 10;
int programDelayCounter = 0;
int programDelayMatrix = 7;
int programDelayMatrixCounter = 0;
int DilithiumMatrixMade = 0;
int DilithiumMatrixAllTimeCountTransfered = 0;
int IntermixAllTimeCountTransfered = 0;
int DilithiumMatrixAllTimeCountTransferedMoney = 0;
int IntermixAllTimeCountTransferedMoney = 0;
TimeSpan TimeSinceFirstRun = TimeSpan.Zero;


Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    GridTerminalSystem.GetBlocksOfType(deutRefList, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(containerList, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(allContainers, b =>
    {
        if(b.CustomName.Contains("Refinery Materials")) return false;
        return true;
        //var items = new List<MyInventoryItem>();
        //b.GetInventory(0).GetItems(items);
        //if (items.Any(item => item.Type.SubtypeId == "DilithiumMatrix")) return true;
        //return false;
    });
    FindAssignedContainers();
    //Echo("FindAssignedContainers - Instruction current: " + Runtime.CurrentInstructionCount.ToString());
    LoadDataFromCD();
}

void Main(string argument){
    TimeSinceFirstRun += Runtime.TimeSinceLastRun;
    try
    {
        if(programDelayMatrixCounter < programDelayMatrix){
            programDelayMatrixCounter++;
        }
        else{
            programDelayMatrixCounter = 0;
            TransferMatrixResourceToRefMatsCargo();
        }
    }catch
    {}

    try
    {
        if (programDelayCounter < programDelay)
        {
            programDelayCounter++;
        }
        else
        {
            programDelayCounter = 0;
            FillDeutProcessors();
            //Echo("FillDeutProcessors - Instruction current: " + Runtime.CurrentInstructionCount.ToString());
            ClearDeutProcessors();
            SaveToCD();
            //Echo("ClearDeutProcessors - Instruction current: " + Runtime.CurrentInstructionCount.ToString());
            RenameCargoContainer();
            //Echo("RenameCargoContainer - Instruction current: " + Runtime.CurrentInstructionCount.ToString());
        }
    }
    catch
    {
        Echo("Script Failed to complete!");
    }
    Echo("Main End - Instruction current: " + Runtime.CurrentInstructionCount.ToString());
    Echo("Matrixes made in 7 seconds: " + DilithiumMatrixMade);
    Echo("Matrixes transfered total: " + DilithiumMatrixAllTimeCountTransfered);
    Echo("Money made for Matrixes: " + DilithiumMatrixAllTimeCountTransferedMoney);
    Echo("Intermix made and transfered: " + IntermixAllTimeCountTransfered);
    Echo("Money made for Intermix: " + IntermixAllTimeCountTransferedMoney);
    Echo("TimeSinceFirstRun: " + TimeSinceFirstRun);
}

void ClearDeutProcessors(){
    deutRefList.ForEach(deutRefinery =>
    {
        var refineryOutputItems = new List<MyInventoryItem>();
        deutRefinery.GetInventory(1).GetItems(refineryOutputItems);
        if(refineryOutputItems.Count == 0) return;
        IntermixAllTimeCountTransfered = (int)refineryOutputItems[0].Amount;
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
    IMyCargoContainer carg = refineryMatsContainersList.Find(container => container.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type));
    if(carg == null) RenameCargoContainer();

    var items = new List<MyInventoryItem>();
    refin.GetInventory(0).GetItems(items);
    refin.GetInventory(0).TransferItemTo(carg.GetInventory(0), items.IndexOf(item), null, true, item.Amount);
}

void FillDeutProcessors(){
    //Echo("Fill Run! " + DilithiumMatrixAmountCur);
    int refCount = 0;
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
        //Echo("Num of Ref: " + enabledRefineries.Count);
    }
    //Echo("Num of Ref: " + enabledRefineries.Count);
    enabledRefineries.ForEach(deutRefinery =>
    {
        //Echo("Passed Refinery!");

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
                //Echo("Item name: " + refineryInputItem.Type.SubtypeId + " Item amount: " + refineryInputItem.Amount.ToString());
                if (refineryInputItem.Type.SubtypeId.Contains("AntiDeuterium"))
                {
                    antiDeuteriumPresent = true;

                    if(refineryInputItem.Amount < 500){
                        if(TransferMaterialsIntoProcessor(deutRefinery, refineryInputItem.Type.SubtypeId, (antiDeuteriumCount - refineryInputItem.Amount))){}
                        //Echo("Transfered " + (antiDeuteriumCount - refineryInputItem.Amount) + "of AntiDeuterium to " + deutRefinery.CustomName);
                    } else if (refineryInputItem.Amount > 2000){
                        BalanceProcessorInput(deutRefinery, refineryInputItem);
                        //Echo("Balancing Refinery input out of AntiDeuterium!");
                    }
                }
                else if (refineryInputItem.Type.SubtypeId.Contains("DilithiumMatrix"))
                {
                    dilMatrixPresent = true;
                    
                    if (refineryInputItem.Amount > 500){
                        BalanceProcessorInput(deutRefinery, refineryInputItem);
                        //Echo("Balancing Refinery input out of DilithiumMatrix!");
                    }
                }
                else if (refineryInputItem.Type.SubtypeId.Contains("Deuterium"))
                {
                    deuteriumPresent = true;

                    if(refineryInputItem.Amount < 500){
                        if(TransferMaterialsIntoProcessor(deutRefinery, refineryInputItem.Type.SubtypeId, (deuteriumCount - refineryInputItem.Amount))){}
                        //Echo("Transfered " + (antiDeuteriumCount - refineryInputItem.Amount) + "of Deuterium to " + deutRefinery.CustomName);
                    } else if (refineryInputItem.Amount > 2000){
                        BalanceProcessorInput(deutRefinery, refineryInputItem);
                        //Echo("Balancing Refinery input out of Deuterium!");
                    }
                }
            });
            if(!dilMatrixPresent){
                if(TransferMaterialsIntoProcessor(deutRefinery, "DilithiumMatrix", (VRage.MyFixedPoint)((int)DilithiumMatrixAmountCur/enabledRefineries.Count))){
                    //Echo("Transfered " + DilithiumMatrixAmountCur+ "/" + (int)DilithiumMatrixAmountCur/deutRefList.Count + "of DilithiumMatrix to " + deutRefinery.CustomName);
                }
                //
            }

            if(!deuteriumPresent){
                if(TransferMaterialsIntoProcessor(deutRefinery, "Deuterium", deuteriumCount)){
                    //Echo("Transfered " + 2000 + "of Deuterium to " + deutRefinery.CustomName);
                }
                
            }
                
            if(!antiDeuteriumPresent){
                if(TransferMaterialsIntoProcessor(deutRefinery, "AntiDeuterium", antiDeuteriumCount)){}
                //Echo("Transfered " + 2000 + "of AntiDeuterium to " + deutRefinery.CustomName);
            }
        }
        refCount++;
        //Echo("Refinery " + refCount + "out of " + deutRefList.Count());
        
    });
}

void Debug(){

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
            //Echo("Container filled " + volume + "%");
            if(volume > 99){
                containerList.Find(containerE => !containerE.CustomName.Contains("Deuterium Intermix") || !containerE.CustomName.Contains("Refinery Materials")).CustomName = "Deuterium Intermix";
                //Echo("Allocating new container!");
            }
            else{
                container.CustomName = "Deuterium Intermix (" + volume + "%)";
                //Echo("Container Name % fill change!");
            }
        }
        else if(container.CustomName.Contains("Refinery Materials")){
            double volume = ((double)container.GetInventory(0).CurrentVolume / (double)container.GetInventory(0).MaxVolume) * 100;
            //Echo("Container filled " + volume + "%");
            if(volume > 99){
                containerList.Find(containerE => !containerE.CustomName.Contains("Deuterium Intermix") || !containerE.CustomName.Contains("Refinery Materials")).CustomName = "Refinery Materials";
                //Echo("Allocating new container!");
            }
            else{
                container.CustomName = "Refinery Materials (" + volume + "%)";
                //Echo("Container Name % fill change!");
            }
        }
        else{
            if(deutIntermixContainersList.Count != 0 || refineryMatsContainersList.Count != 0) return;

            //Echo("Container is default named!");
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
                //Echo("No deutIntermixContainers found! Allocating one!");
                return;
            }
            else if(refineryMatsContainersList.Count == 0 && (!items.Any(item => item.Type.SubtypeId == "DeuteriumIntermix") || items.Count == 0)){
                container.CustomName = "Refinery Materials";
                refineryMatsContainersList.Add(container);
                //Echo("No refineryMatsContainers found! Allocating one!");
                return;
            }
        }
    });
}
void SaveToCD(){
    var data = "";
    data = "DilithiumMatrixAllTimeCountTransfered:" + DilithiumMatrixAllTimeCountTransfered + "\n" + "DilithiumMatrixAllTimeCountTransferedMoney:" + DilithiumMatrixAllTimeCountTransferedMoney + "\n" + "IntermixAllTimeCountTransfered:" + IntermixAllTimeCountTransfered + "\n" + "IntermixAllTimeCountTransferedMoney:" + IntermixAllTimeCountTransferedMoney + "\n" + "TimeSinceFirstRun" + TimeSinceFirstRun;
    Me.CustomData = data;
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
        else if( values[0] == "TimeSinceFirstRun")
        {
            TimeSinceFirstRun = TimeSpan.Parse(values[1]);
        }
    }
}