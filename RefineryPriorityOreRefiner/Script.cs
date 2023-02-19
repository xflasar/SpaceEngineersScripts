
// TODO:
// - Reformat code - Actual
// - Show up how much refined in the session and total (This needs storing permanent - can it be done??)
// So first thing Make 2 methods 1 for Moving ore to refinery and 2 for moving refined ingots from refinery to cargo containers that can hold the amount of it 
// ( later this gets made to fill up the container and if there is remaining ingots that didnt get transfered then find another container that and repete the stuff) then
//



// Define your lists to store the refineries and containers
List<IMyRefinery> refineryList = new List<IMyRefinery>();
List<IMyCargoContainer> containerList = new List<IMyCargoContainer>();
string oreToRefine;
public Program()
{
  // Configure this program to run the Main method every 100 update ticks
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    GridTerminalSystem.GetBlocksOfType(refineryList);
    GridTerminalSystem.GetBlocksOfType(containerList);
    oreToRefine = "Duranium";

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
int counter = 0;
int containerOcupied = 0;
void Main(string argument)
{
    if(counter < 10){
        counter++;
    }
    else{

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
        if (container != null)
        {
            MoveOreToRefinery(container, refinery, oreToRefine);
            MoveRefinedToContainer(refinery, container);
        }
    }
    counter = 0;
    containerOcupied = 0;
    }
}
