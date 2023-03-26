// Refinery Ore Refiner - manages the refinery of ore
// Created by xSupeFly
// Discord: xSupeFly#2911

// TODO:
// - Show up how much refined in the session and total (This needs storing permanent - can it be done??)

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
    "Dilithium",
    "Tritanium",
    "Duranium"
}; 
// Use this as priority list -> Depending on position it will take the priority if no more ore to be refined will go onto next one -> this probably
// will be better to use from Me.CustomData ( better user manipulation )

// Variables
int programDelay = 5;
int programDelayCounter = 0;

Program()
{
  // Configure this program to run the Main method every 100 update ticks
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    GridTerminalSystem.GetBlocksOfType(refineryList, b => b.CubeGrid == Me.CubeGrid);
    GridTerminalSystem.GetBlocksOfType(containerList, b => b.CubeGrid == Me.CubeGrid);
}

void Main(string argument)
{
    if (programDelayCounter < programDelay)
    {
        programDelayCounter++;
        return;
    }

    programDelayCounter = 0;
    FindNonEmptyOreCargoContainers();

    foreach (IMyRefinery refinery in refineryList)
    {
        IMyCargoContainer container = FindCargoContainerWithOreToRefine();

        if (container != null)
        {
            MoveOreToRefinery(container, refinery, oreToRefine[0]);
            MoveRefinedToContainer(refinery, container);
        }
        var items = new List<MyInventoryItem>();
        refinery.GetInventory(1).GetItems(items);
        if (items.Count > 0)
        {
            IMyCargoContainer emptyContainer = GetEmptyContainer();
            if (emptyContainer != null)
            {
                MoveRefinedToContainer(refinery, emptyContainer);
            }
        }
    }
}

// Define a method to move ore from cargo containers to refineries
void MoveOreToRefinery(IMyCargoContainer container, IMyRefinery refinery, string oreName)
{
    var containerInventory = container.GetInventory(0);
    var containerItems = new List<MyInventoryItem>();
    containerInventory.GetItems(containerItems);

    var oreItem = containerItems.FirstOrDefault(item => item.Type.TypeId == "MyObjectBuilder_Ore" && item.Type.SubtypeId.Contains(oreName));

    if (oreItem != null)
    {
        var refineryInventory = refinery.GetInventory(0);
        var refineryItems = new List<MyInventoryItem>();
        refineryInventory.GetItems(refineryItems);

        foreach (var item in refineryItems)
        {
            if (!item.Type.SubtypeId.Contains(oreName))
            {
                var targetContainer = containerList.FirstOrDefault(containerOut => containerOut.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type));

                if (targetContainer != null)
                {
                    refineryInventory.TransferItemTo(targetContainer.GetInventory(0), 0, null, true, item.Amount);
                }
            }
        }

        containerInventory.TransferItemTo(refineryInventory, containerItems.IndexOf(oreItem), null, true, oreItem.Amount);
    }
}

// Define a method to move refined items to the cargo container
void MoveRefinedToContainer(IMyRefinery refinery, IMyCargoContainer container)
{
    var refineryInventory = refinery.GetInventory(1);
    var refineryItems = new List<MyInventoryItem>();
    refineryInventory.GetItems(refineryItems);

    if (refineryItems.Count > 0)
    {
        var targetContainer = containerList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(refineryItems[0].Amount, refineryItems[0].Type));
        
        var targetInventory = targetContainer.GetInventory(0);
        var amountToTransfer = refineryItems[0].Amount;
        var index = 0;
        var targetSlot = 0;
        var sourceInventory = refineryInventory;

        var transferSuccess = targetInventory.TransferItemFrom(sourceInventory, index, targetSlot);
        
        if (!transferSuccess)
        {
            return;
        }
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

IMyCargoContainer FindCargoContainer()
{
    foreach (var container in filledContainerList)
    {
        var items = new List<MyInventoryItem>();
        container.GetInventory(0).GetItems(items);

        if (items.Count > 0 && items.Any(item => item.Type.SubtypeId.Contains(oreToRefine[0])))
        {
            Echo("Found!");
            return container;
        }
    }

    return null;
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
