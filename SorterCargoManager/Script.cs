// Sorter Cargo Manager
// Created by xSupeFly
// Discord: xSupeFly#2911
//
/// TODO:


List<IMyCargoContainer> containerList = new List<IMyCargoContainer>();

List<IMyCargoContainer> containerConsumablesAndToolsList = new List<IMyCargoContainer>();
List<IMyCargoContainer> containerComponentList = new List<IMyCargoContainer>();
List<IMyCargoContainer> containerIngotList = new List<IMyCargoContainer>();
List<IMyCargoContainer> containerOreList = new List<IMyCargoContainer>();
List<IMyCargoContainer> containerEmptyList = new List<IMyCargoContainer>();

Dictionary<string, double> itemsVolumes = new Dictionary<string, double>() {
    {"Tech2x.Component", 5},
    {"Tech4x.Component", 5},
    {"Tech8x.Component", 5},
    {"GoldPressedLatinum.Component", 10},
    {"StemBolt.Component", 10},
    {"DeltaGelPack.Component", 10},
    {"AlphaGelPack.Component", 10},
    {"BetaGelPack.Component", 10},
    {"GammaGelPack.Component", 10},
    {"ContinuumTech.Component", 3},
    {"DilithiumMatrix.Component", 3},
    {"DuraniumGrid.Component", 15},
    {"TritaniumPlate.Component", 3},
    {"TransparentAluminumPlate.Component", 3},
    {"IsolinearChip.Component", 3},
    {"Dilithium.Ore", 1.8},
    {"Dilithium.Ingot", 1.8},
    {"Duranium.Ore", 1},
    {"Duranium.Ingot", 1},
    {"TransphasicMatter.Ore", 30.0},
    {"TransphasicMatter.Ingot", 3.2},
    {"Tritanium.Ore", 1.8},
    {"Tritanium.Ingot", 1.8},
    {"Aluminum.Ore", 1},
    {"Aluminum.Ingot", 1},
    {"DeuteriumIntermix.Ingot", 1},
    {"Kemocite.Ore", 1.8},
    {"Kemocite.Ingot", 1.8},
    {"Latinum.Ore", 1.8},
    {"Latinum.Ingot", 1.8},
    {"Neutronium.Ore", 2.8},
    {"Neutronium.Ingot", 2.8},
    {"Deuterium.Ore", 1.8},
    {"AntiDeuterium.Ore", 1.8},
    {"TorpedoCasing.Component", 10},
    {"MatterAntiMatterChamber.Component", 10},
    {"TorpedoThruster.Component", 10},
    {"TransphasicCore.Component", 10},
    {"TransphasicMatterChamber.Component", 10},
    {"STC_TransphasicTorpedoMagazine.AmmoMagazine", 5},
    {"STC_PhotonTorpedoMagazine.AmmoMagazine", 5},
    {"STC_QuantumTorpedoMagazine.AmmoMagazine", 5},
    {"STC_SpatialTorpedoMagazine.AmmoMagazine", 5},
    {"STC_SmallPhotonTorpedoMagazine.AmmoMagazine", 5},
    {"STC_SmallQuantumTorpedoMagazine.AmmoMagazine", 5},
    {"STC_SmallSpatialTorpedoMagazine.AmmoMagazine", 5},
    {"ShieldComponent", 0.85},
    {"Stone.Ore", 0.37},
    {"Iron.Ore", 0.37},
    {"Nickel.Ore", 0.37},
    {"Cobalt.Ore", 0.37},
    {"Magnesium.Ore", 0.37},
    {"Silicon.Ore", 0.37},
    {"Silver.Ore", 0.37},
    {"Gold.Ore", 0.37},
    {"Platinum.Ore", 0.37},
    {"Uranium.Ore", 0.37},
    {"Stone.Ingot", 0.37},
    {"Iron.Ingot", 0.127},
    {"Nickel.Ingot", 0.112},
    {"Cobalt.Ingot", 0.112},
    {"Magnesium.Ingot", 0.575},
    {"Silicon.Ingot", 0.429},
    {"Silver.Ingot", 0.095},
    {"Gold.Ingot", 0.052},
    {"Platinum.Ingot", 0.047},
    {"Uranium.Ingot", 0.052},
    {"SemiAutoPistolItem.PhysicalGunObject", 6},
    {"FullAutoPistolItem.PhysicalGunObject", 8},
    {"ElitePistolItem.PhysicalGunObject", 6},
    {"AutomaticRifleItem.PhysicalGunObject", 20},
    {"PreciseAutomaticRifleItem.PhysicalGunObject", 20},
    {"RapidFireAutomaticRifleItem.PhysicalGunObject", 20},
    {"UltimateAutomaticRifleItem.PhysicalGunObject", 20},
    {"BasicHandHeldLauncherItem.PhysicalGunObject", 125},
    {"AdvancedHandHeldLauncherItem.PhysicalGunObject", 125},
    {"OxygenBottle.OxygenContainerObject", 120},
    {"HydrogenBottle.GasContainerObject", 120},
    {"WelderItem.PhysicalGunObject", 8},
    {"Welder2Item.PhysicalGunObject", 8},
    {"Welder3Item.PhysicalGunObject", 8},
    {"Welder4Item.PhysicalGunObject", 8},
    {"AngleGrinderItem.PhysicalGunObject", 20},
    {"AngleGrinder2Item.PhysicalGunObject", 20},
    {"AngleGrinder3Item.PhysicalGunObject", 20},
    {"AngleGrinder4Item.PhysicalGunObject", 20},
    {"HandDrillItem.PhysicalGunObject", 25},
    {"HandDrill2Item.PhysicalGunObject", 25},
    {"HandDrill3Item.PhysicalGunObject", 25},
    {"HandDrill4Item.PhysicalGunObject", 25},
    {"CubePlacerItem.PhysicalGunObject", 1},
    {"Scrap.Ore", 0.254},
    {"Scrap.Ingot", 0.254},
    {"Ice.Ore", 0.37},
    {"Organic.Ore", 0.37},
    {"DesertTree.TreeObject", 8000},
    {"DesertTreeDead.TreeObject", 8000},
    {"LeafTree.TreeObject", 8000},
    {"PineTree.TreeObject", 8000},
    {"PineTreeSnow.TreeObject", 8000},
    {"LeafTreeMedium.TreeObject", 8000},
    {"DesertTreeMedium.TreeObject", 8000},
    {"DesertTreeDeadMedium.TreeObject", 8000},
    {"PineTreeMedium.TreeObject", 8000},
    {"PineTreeSnowMedium.TreeObject", 8000},
    {"DeadBushMedium.TreeObject", 8000},
    {"DesertBushMedium.TreeObject", 8000},
    {"LeafBushMedium_var1.TreeObject", 8000},
    {"LeafBushMedium_var2.TreeObject", 8000},
    {"PineBushMedium.TreeObject", 8000},
    {"SnowPineBushMedium.TreeObject", 8000},
    {"ClangCola.ConsumableItem", 1},
    {"CosmicCoffee.ConsumableItem", 1}
};

IMyTextPanel lcdPanel;
IEnumerator<bool> _stateMachine;
TimeSpan TimeSinceFirstRun = TimeSpan.Zero;
TimeSpan TimeActiveTotal = TimeSpan.Zero;
TimeSpan lastSaveTime = TimeSpan.Zero;
MyLogger logger;

string[] itemTypes = new string[] { "Component", "Ingot", "Ore", "ConsumablesAndTools"};
string[] ConsumablesAndTools = new string[] {"PhysicalGunObject", "OxygenContainerObject", "GasContainerObject", "AmmoMagazine", "MissileAmmo", "NATO_5p56x45mm", "NATO_25x184mm", "NATO_5p56x45mm", "NATO_25x184mm", "Missile200mm"};

// Config
List<string> ignoreContainerNames = new List<string>(){ "DilithiumMatrix Cargo", "Deuterium Intermix", "Refinery Materials" }; // Containers with these names will be ignored
Program()
{
    GridTerminalSystem.GetBlocksOfType(containerList, b => b.CubeGrid == Me.CubeGrid);

    lcdPanel = GridTerminalSystem.GetBlockWithName("LCD Deuterium Processor Manager") as IMyTextPanel;

    logger = new MyLogger();
    SetupFirstRunContainers();

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
        logger.GetLoggerLog().ForEach(l => Echo(l));
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
    SortInventory();
}

public class MyLogger
{
    private List<string> _logData;
    private int _maxLogs = 10;

    public MyLogger(int maxLogs = 10)
    {
        _logData = new List<string>();
        _maxLogs = maxLogs;
    }

    public void Log(string message)
    {
        _logData.Add(message);
        if (_logData.Count > _maxLogs)
        {
            _logData.RemoveAt(0);
        }
    }

    public List<string> GetLoggerLog()
    {
        return _logData;
    }
}

void SetupFirstRunContainers()
{
    foreach (var container in containerList)
    {
        var inventory = container.GetInventory(0);
        var items = new List<MyInventoryItem>();
        inventory.GetItems(items);

        if (items.Count == 0)
        {
            container.CustomName = "Cargo Container - Empty";
            logger.Log($"Renamed container {container.CustomName} to {container.CustomName}");
            containerEmptyList.Add(container);
            continue;
        }

        var itemTypesCount = new Dictionary<string, int>();

        foreach (var itemType in itemTypes)
        {
            itemTypesCount.Add(itemType, 0);
        }

        foreach (var item in items)
        {
            if(ConsumablesAndTools.Any(consu => item.Type.TypeId.EndsWith(consu))){
                itemTypesCount["ConsumablesAndTools"] += (int)item.Amount;
            }
            foreach (var itemType in itemTypes)
            {
                if (item.Type.TypeId.EndsWith(itemType))
                {
                    itemTypesCount[itemType] += 1;
                }
            }

            var max = itemTypesCount.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            container.CustomName = "Cargo Container - " + max;
            logger.Log($"Renamed container {container.CustomName} to {container.CustomName}");
        }
        foreach (var itemType in itemTypes)
        {  
            logger.Log($"Item: {itemType} - {itemTypesCount[itemType]}");
        }

        if (ignoreContainerNames.Any(ignoreName => container.CustomName.Contains(ignoreName)))
            continue;
            
        switch (container.CustomName.Substring(container.CustomName.IndexOf("-") + 1).Trim())
        {
            case "Component":
                containerComponentList.Add(container);
                break;
            case "Ingot":
                containerIngotList.Add(container);
                break;
            case "ConsumablesAndTools":
                containerConsumablesAndToolsList.Add(container);
                break;
            case "Ore":
                containerOreList.Add(container);
                break;
        }
    }
}

void CheckIfItemTypeIsSameAsContainerType(IMyCargoContainer container)
{
    var inventory = container.GetInventory(0);
    var items = new List<MyInventoryItem>();
    inventory.GetItems(items);

    string containerName = container.CustomName.Substring(container.CustomName.IndexOf("-") + 1).Trim();

    foreach (var item in items)
    {
        if (!item.Type.TypeId.Contains(containerName))
        {
            try{
                switch (item.Type.TypeId.Substring(item.Type.TypeId.IndexOf("_") + 1).Trim())
                {
                    case "Component":
                        if(containerComponentList.Any(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type))){
                            inventory.TransferItemTo(containerComponentList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).GetInventory(0), items.IndexOf(item), null, true);
                            logger.Log($"Transferring {item.Amount} {item.Type.TypeId} from {container.CustomName} to {containerComponentList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).CustomName}");
                        }
                        else
                        {
                            containerEmptyList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).GetInventory(0).TransferItemFrom(inventory, items.IndexOf(item), null, true);
                        }
                        break;
                    case "Ingot":
                        if (containerIngotList.Any(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)))
                        {
                            inventory.TransferItemTo(containerIngotList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).GetInventory(0), items.IndexOf(item), null, true);
                            logger.Log($"Transferring {item.Amount} {item.Type.TypeId} from {container.CustomName} to {containerComponentList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).CustomName}");
                        }
                        else
                        {
                            containerEmptyList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).GetInventory(0).TransferItemFrom(inventory, items.IndexOf(item), null, true);
                        }
                        break;
                    case "Ore":
                        if (containerOreList.Any(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)))
                        {
                            inventory.TransferItemTo(containerOreList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).GetInventory(0), items.IndexOf(item), null, true);
                            logger.Log($"Transferring {item.Amount} {item.Type.TypeId} from {container.CustomName} to {containerComponentList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).CustomName}");
                        }
                        else
                        {
                            containerEmptyList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).GetInventory(0).TransferItemFrom(inventory, items.IndexOf(item), null, true);
                        }
                        break;
                    case "ConsumablesAndTools":
                        if (containerConsumablesAndToolsList.Any(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)))
                        {
                            inventory.TransferItemTo(containerConsumablesAndToolsList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).GetInventory(0), items.IndexOf(item), null, true);
                            logger.Log($"Transferring {item.Amount} {item.Type.TypeId} from {container.CustomName} to {containerComponentList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).CustomName}");
                        }
                        else
                        {
                            containerEmptyList.Find(cont => cont.GetInventory(0).CanItemsBeAdded(item.Amount, item.Type)).GetInventory(0).TransferItemFrom(inventory, items.IndexOf(item), null, true);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                logger.Log($"Data: {container.CustomName}, {item.Type.TypeId}, {item.Type.SubtypeId} Error: {e.Message}");
            }
        }
    }
}

public void SortInventory()
{
    logger.Log("Component containers: " + containerComponentList.Count.ToString());
    logger.Log("Ingot containers: " +  containerIngotList.Count.ToString());
    logger.Log("CustomablesAndTools containers: " + containerConsumablesAndToolsList.Count.ToString());
    logger.Log("Ore containers: " + containerOreList.Count.ToString());
    logger.Log("Empty containers: " + containerEmptyList.Count.ToString());
    foreach (var container in containerList)
    {
        CheckIfItemTypeIsSameAsContainerType(container);
    }
    logger.Log("Container run: " + Runtime.CurrentInstructionCount.ToString());
}