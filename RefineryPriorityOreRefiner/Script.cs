
// TODO:
// - Reformat code - Actual
// - Show up how much refined in the session and total (This needs storing permanent - can it be done??)
// So first thing Make 2 methods 1 for Moving ore to refinery and 2 for moving refined ingots from refinery to cargo containers that can hold the amount of it 
// ( later this gets made to fill up the container and if there is remaining ingots that didnt get transfered then find another container that and repete the stuff) then
//



// Lists
List<IMyRefinery> refineryList = new List<IMyRefinery>();
List<IMyCargoContainer> containerList = new List<IMyCargoContainer>();
List<IMyCargoContainer> filledContainerList = new List<IMyCargoContainer>();
List<string> oreToRefine = new List<string> {
	//"Stone",
	//"Iron",
	//"Nickel",
	//"Cobalt",
	//"Silicon",
	//"Uranium",
	//"Silver",
	//"Gold",
	//"Platinum",
	//"Magnesium",
	//"Scrap",
    "Tritanium",
    "Duranium"
}; 
// Use this as priority list -> Depending on position it will take the priority if no more ore to be refined will go onto next one -> this probably
// will be better to use from Me.CustomData ( better user manipulation )

// Variables
int programDelay = 5;
int programDelayCounter = 0;

void Program()
{
  // Configure this program to run the Main method every 100 update ticks
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    GridTerminalSystem.GetBlocksOfType(refineryList, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(containerList, b => b.CubeGrid == Me.CubeGrid);
}

// Define a method to move ore from cargo containers to refineries
void MoveOreToRefinery(IMyCargoContainer container, IMyRefinery refinery, string oreName)
{
    // Get the list of inventory items in the container
    List<MyInventoryItem> containerItems = new List<MyInventoryItem>();
    container.GetInventory(0).GetItems(containerItems);
    // Find the ore to move to the refinery
    MyInventoryItem oreItem = containerItems.Find(item => item.Type.SubtypeId == oreName && item.Type.TypeId == "MyObjectBuilder_Ore");

    if (oreItem != null)
    {
        List<MyInventoryItem> refItems = new List<MyInventoryItem>();
        refinery.GetInventory(0).GetItems(refItems);
        refItems.ForEach(item => 
        {
            if(item.Type.SubtypeId != oreName){
                refinery.GetInventory(0).TransferItemTo(containerList.Find(containerOut => containerOut.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).GetInventory(0), 0, null, true, item.Amount);
            }
        });
        // Transfer the ore to the refinery
        container.GetInventory(0).TransferItemTo(refinery.GetInventory(0), containerItems.IndexOf(oreItem), null, true, oreItem.Amount);
    }
}

// Define a method to move refined items to the cargo container
void MoveRefinedToContainer(IMyRefinery refinery, IMyCargoContainer container)
{
    // Get the list of inventory items in the refinery
    List<MyInventoryItem> refineryItems = new List<MyInventoryItem>();
    refinery.GetInventory(1).GetItems(refineryItems);

    if (refineryItems.Count > 0)
    {
        // Transfer the refined items to the cargo container
        refinery.GetInventory(1).TransferItemTo(containerList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(refineryItems[0].Amount, refineryItems[0].Type)).GetInventory(0), 0, null, true, refineryItems[0].Amount);
    }
}

void FindNonEmptyOreCargoContainers(){
    filledContainerList = containerList.Where(container => {
        var items = new List<MyInventoryItem>();
        container.GetInventory(0).GetItems(items);
        if(items.Count == 0) return;

        if(items.Any(items => item.Type.TypeId == "MyObjectBuilder_Ore")) return true;
        return false;
    });
}


IMyCargoContainer FindCargoContainer(){
    bool itemFound = false;
    IMyCargoContainer containerF = filledContainerList.Find(container => {
        var items = new List<MyInventoryItem>();
        container.GetInventory(0).GetItems(items);

        if(items.Count == 0) return;
        else if( items.Find(item => item.Type.SubtypeId == oreToRefine[0]) != null){
            Echo("Found!");
            return container;
        }
    });
}
void Main(string argument)
{
    if(programDelayCounter < programDelay){
        programDelayCounter++;
    }
    else{
        programDelayCounter = 0;
        FindNonEmptyOreCargoContainers();
        // Move ore to refineries
        foreach (IMyRefinery refinery in refineryList)
        {
            // Find a cargo container to get the ore from
            IMyCargoContainer container = containerList.Find(item => {
                List<MyInventoryItem> invItems = new List<MyInventoryItem>();
                item.GetInventory(0).GetItems(invItems);
                if(invItems.Count > 0) containerOcupied++;

                if(invItems.Any(oreItems =>
                {
                    if( oreItems.Type.TypeId == "MyObjectBuilder_Ore" && oreItems.Type.SubtypeId.ToString() == oreToRefine){
                        return true;
                    }
                    return false;
                })) {
                    return true;
                    }else{
                return false;
                }
            });
            Echo("Find cargo container and item in it - Instruction current: " + Runtime.CurrentInstructionCount.ToString());
            if (container != null)
            {
                MoveOreToRefinery(container, refinery, oreToRefine);
                MoveRefinedToContainer(refinery, container);
            }
        }
    }
}
