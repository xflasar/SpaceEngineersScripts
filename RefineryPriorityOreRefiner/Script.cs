// Refinery Ore Refiner - manages the refinery of ore
// Created by xSupeFly
// Discord: xSupeFly#2911

// TODO:
// - Show up how much refined in the session and total (This needs storing permanent - can it be done??)
// - 70% - refine 1st ore, 20% - refine 2nd ore, 10% - refine 3rd ore. This will activate if oreToRefine list has more than 1 set ore and there is ore different to the 1st ore in priority.

// Lists
List<IMyRefinery> refineryList = new List<IMyRefinery>();
List<IMyCargoContainer> containerList = new List<IMyCargoContainer>();
List<IMyCargoContainer> filledContainerList = new List<IMyCargoContainer>();
List<IMyCargoContainer> containerDilMatrixList = new List<IMyCargoContainer>();
List<string> oreToRefine = new List<string> {
	"Dilithium",
    "Tritanium",
    "Duranium",
    "Stone",
	"Iron",
	"Nickel",
	"Cobalt",
	"Silicon",
	"Uranium",
	"Silver",
	"Gold",
	"Platinum",
	"Magnesium",
	"Scrap"
    
}; 
// Use this as priority list -> Depending on position it will take the priority if no more ore to be refined will go onto next one -> this probably
// will be better to use from Me.CustomData ( better user manipulation )

// Variables
int programDelay = 5;
int programDelayCounter = 0;
// Config || Replace variable values at your discretion ||
string containerTransferEnemyAssemblersCustomName = "DilithiumMatrix Cargo"; // Every Transfer Enemy Assembler Cargo Container CustomName need to hame this in their name 

Program()
{
  // Configure this program to run the Main method every 100 update ticks
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    GridTerminalSystem.GetBlocksOfType(refineryList, b => !StringSplitter(b.DetailedInfo)[0].Contains("Deuterium"));
    GridTerminalSystem.GetBlocksOfType(containerList, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(containerDilMatrixList, b => b.CustomName.Contains(containerTransferEnemyAssemblersCustomName));
}

// Works only for Type: 
// TODO:
// - All keys will get Substring into result
List<string> StringSplitter(string text){

    if(text.Length == 0) return new List<string>();

    List<string> result = new List<string>();

    List<string> keys = new List<string>(){
        {"Type:"},
        {"Max Required Input:"},
        {"Required Input:"},
        {"Productivity:"},
        {"Effectiveness:"},
        {"Power Efficiency:"},
        {"Used upgrade module slots:"}
    };

    for (int i = 0; i < keys.Count; i++)
    {
        if(text.Substring(0).StartsWith(keys[i]))
        {
            int startIndex = 0 + keys[i].Length;
            int endIndex = 0;
            if(i == keys.Count-1)
            {
                endIndex = text.Length;
            }else{
                endIndex = text.IndexOf(keys[i+1]);
            }
            //Echo($"String: {text.Substring(startIndex, endIndex - startIndex)}");
            result.Add(text.Substring(startIndex, endIndex - startIndex));
        }
        else{
            break;
        }
    }
    return result;
}

void Main(string argument)
{
    Echo($"Refineries: {refineryList.Count}");
    if (programDelayCounter < programDelay)
    {
        programDelayCounter++;
        return;
    }

    programDelayCounter = 0;
    FindNonEmptyOreCargoContainers();

    foreach (IMyRefinery refinery in refineryList)
    {
        IMyCargoContainer container = null;
        container = FindCargoContainerWithOreToRefine();
        
        if (container != null)
        {
            try{
                MoveOreToRefinery(container, refinery, oreToRefine[0]);
                refinery.CustomName = $"Refinery {refinery.IsProducing} - {oreToRefine[0]}";
            }
            catch (Exception ex)
            {
                Echo($"Error with moving ore to refinery: {ex}");
            }

            //MoveRefinedToContainer(refinery);
        }
        var items = new List<MyInventoryItem>();
        refinery.GetInventory(1).GetItems(items);
        if (items.Count > 0)
        {
            MoveRefinedToContainer(refinery);
        }
    }
}

// Define a method to move ore from cargo containers to refineries
void MoveOreToRefinery(IMyCargoContainer container, IMyRefinery refinery, string oreName)
{
    double TransferAmount = 1000;
    var containerInventory = container.GetInventory(0);
    var containerItems = new List<MyInventoryItem>();
    containerInventory.GetItems(containerItems);

    var oreItem = containerItems.FirstOrDefault(item => item.Type.TypeId == "MyObjectBuilder_Ore" && item.Type.SubtypeId.Contains(oreName));

    if (oreItem != null)
    {
        var refineryInventory = refinery.GetInventory(0);
        var refineryItems = new List<MyInventoryItem>();
        refineryInventory.GetItems(refineryItems);
        if((double)oreItem.Amount < TransferAmount){
            TransferAmount = (double)oreItem.Amount;
        }

        foreach (var item in refineryItems)
        {
            if (!item.Type.SubtypeId.Contains(oreName))
            {
                IMyCargoContainer targetContainer = null;
                if(refinery.OwnerId == 144115188075856837){
                    targetContainer = containerDilMatrixList.FirstOrDefault(containerOut => containerOut.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type));
                }
                else
                {
                    targetContainer = containerList.FirstOrDefault(containerOut => containerOut.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type));
                }

                if (targetContainer != null)
                {
                    refineryInventory.TransferItemTo(targetContainer.GetInventory(0), 0, null, true, item.Amount);
                }
            }
        }
        if(refinery.IsProducing){
            return;
        }

        if(refinery.OwnerId == 144115188075856837){
            var tgContainer = containerDilMatrixList.FirstOrDefault(containerTransf => containerTransf.GetInventory(0).CanItemsBeAdded((VRage.MyFixedPoint)TransferAmount, oreItem.Type));
            if(containerInventory.TransferItemTo(tgContainer.GetInventory(0), containerItems.IndexOf(oreItem), null, true, (VRage.MyFixedPoint)TransferAmount)){
                var tgContainerItems = new List<MyInventoryItem>();
                tgContainer.GetInventory(0).GetItems(tgContainerItems);
                var oreItemE = tgContainerItems.FirstOrDefault(item => item.Type.TypeId == "MyObjectBuilder_Ore" && item.Type.SubtypeId.Contains(oreName));
                tgContainer.GetInventory(0).TransferItemTo(refineryInventory, tgContainerItems.IndexOf(oreItemE), null, true, oreItemE.Amount);
            }
        }
        else{
            containerInventory.TransferItemTo(refineryInventory, containerItems.IndexOf(oreItem), null, true, (VRage.MyFixedPoint)TransferAmount);
        }
    }
}

// Define a method to move refined items to the cargo container
void MoveRefinedToContainer(IMyRefinery refinery)
{
    var refineryInventory = refinery.GetInventory(1);
    var refineryItems = new List<MyInventoryItem>();
    refineryInventory.GetItems(refineryItems);

    if (refineryItems.Count > 0)
    {
        refineryItems.ForEach(item => {
            Echo($"{refineryItems[0].Type}/{refineryItems[0].Amount}");
            IMyCargoContainer targetContainer = null;
            if(refinery.OwnerId == 144115188075856837){
                targetContainer = containerDilMatrixList.Find(container => container.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type));
            }
            else{
                targetContainer = containerList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(refineryItems[0].Amount, refineryItems[0].Type));
            }
            targetContainer.GetInventory(0).TransferItemFrom(refineryInventory, 0, null, true, item.Amount);
            Echo(targetContainer.GetInventory(0).TransferItemFrom(refineryInventory, refineryItems.IndexOf(item), null, true, item.Amount).ToString());
            //Echo(refineryInventory.TransferItemTo(targetContainer.GetInventory(0), refineryItems.IndexOf(item), null, true, item.Amount).ToString());
            Echo($"{refinery.OwnerId + "/" + Me.OwnerId} output {refineryItems[0].Amount} moved to {targetContainer.CustomName}");
        });
    }
}

void FindNonEmptyOreCargoContainers()
{
    filledContainerList = containerList.Where(container =>
    {
        List<MyInventoryItem> items = new List<MyInventoryItem>();
        container.GetInventory(0).GetItems(items);
        return items.Any(item => item.Type.TypeId == "MyObjectBuilder_Ore");
    }).ToList();
}

IMyCargoContainer FindCargoContainerWithOreToRefine()
{
    return filledContainerList.Find(container =>
        {
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            container.GetInventory(0).GetItems(items);

            return items.Any(item => item.Type.TypeId == "MyObjectBuilder_Ore" && item.Type.SubtypeId.Contains(oreToRefine[0]));
    });
}

IMyCargoContainer GetEmptyContainer()
{
    return containerList.Find(container =>
    {
        List<MyInventoryItem> items = new List<MyInventoryItem>();
        container.GetInventory(0).GetItems(items);

        return items.Count == 0 && container.GetInventory(0).CurrentVolume < container.GetInventory(0).MaxVolume;
    });
}
