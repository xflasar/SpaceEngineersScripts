﻿static double UpdateInvervalAssemblerQueues = 0.1; //Update every x seconds (0=as fast as possible, 0.5=every 500ms, ..)
static double UpdateInvervalGrinding = 0.1; //Update every x seconds (0=as fast as possible, 0.5=every 500ms, ..)
/* <summary>
Configure the groups with there block names and/or group names.
You can access screens from TextSurfaceProvider like Cockpits with there name followed by [ScreenIndex] e.g. "Cockpit[0]"
</summary> */
static BuildAndRepairSystemQueuingGroup[] BuildAndRepairSystemQueuingGroups = {
  new BuildAndRepairSystemQueuingGroup() {
    BuildAndRepairSystemGroupName = "PMCC 1",
    AssemblerGroupName = "Assemblers",
    Displays = new [] {
      new DisplayDefinition {
        DisplayNames = new [] { "PMCC Status Panel 1", "Programmable Block PMCC Control 1[0]" },
        DisplayKinds = new [] { DisplayKind.Status},
        DisplayMaxLines = 19,
        DisplaySwitchTime = 4
      }
    }
  }
};
/* <summary>
Kind of Display
</summary> */
public enum DisplayKind
{
    ShortStatus,
    Status,
    WeldTargets,
    GrindTargets,
    CollectTargets,
    MissingItems,
    BlockWeldPriority,
    BlockGrindPriority
}
static BuildAndRepairAutoQueuing _AutoQueuing;
public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    _AutoQueuing = new BuildAndRepairAutoQueuing(this);
}
void Main(string arg)
{
    _AutoQueuing.Handle();
}
/* <summary>
Group configuration.
Grouped Systems/Assembler could be defined either by list
their Names (BuildAndRepairSystemNames\AssemblerNames) and or by giving
a group name (BuildAndRepairSystemGroupName\AssemblerGroupName)
</summary> */
public class BuildAndRepairSystemQueuingGroup
{
    public string Name { get; set; }
    public string[] BuildAndRepairSystemNames { get; set; }
    public string BuildAndRepairSystemGroupName { get; set; }
    public string[] AssemblerNames { get; set; }
    public string AssemblerGroupName { get; set; }
    public DisplayDefinition[] Displays { get; set; }
}
/* <summary>
Definition for multiple Displays
</summary> */
public class DisplayDefinition
{
    /* <summary>
    List of Display Names
    </summary> */
    public string[] DisplayNames { get; set; }
    /* <summary>
    You can List(?) the Display pages you need from Enum DisplayKind. They will be switched(?) every DisplaySwitchTime seconds
    </summary> */
    public DisplayKind[] DisplayKinds { get; set; } = new[] { DisplayKind.Status };
    /* <summary>
    The maximum of lines that should be displayed in case of list items (Blocks to build, grind, missing, ..)
    </summary> */
    public int DisplayMaxLines { get; set; } = 19;
    /* <summary>
    AutoSwitch time [seconds]
    </summary> */
    public double DisplaySwitchTime { get; set; } = 5;
}
/* <summary>
Build and repair system automatic queuing of missing components
</summary> */
public class BuildAndRepairAutoQueuing
{
    private Program _Program;
    private bool _IsInit;
    private double _ElapsedTime;
    private double _ReInit;
    private double _NextUpdateAssemblerQueues;
    private double _NextUpdateGrinding;
    private BuildAndRepairSystemQueuingGroupData[] _GroupData;
    public string InitializationResultMessage { get; private set; }
    public BuildAndRepairAutoQueuing(Program program)
    {
        _Program = program;
        _ElapsedTime = 0;
    }
    /* <summary>
    Auto-repair
    </summary> */
    public void Handle()
    {
        _ElapsedTime += _Program.Runtime.TimeSinceLastRun.TotalSeconds;
        if (!_IsInit)
        {
            Initialize();
            _ReInit = _ElapsedTime + 120; //Refresh every 2 Minutes
            _NextUpdateAssemblerQueues = _ElapsedTime - 1; //Next refresh now
            _NextUpdateGrinding = _NextUpdateAssemblerQueues;
            if (!string.IsNullOrWhiteSpace(InitializationResultMessage))
            {
                _Program.Echo(InitializationResultMessage);
            }
        }
        if (_IsInit)
        {
            if (_ElapsedTime > _NextUpdateGrinding)
            {
                ScriptControlledGrinding();
                _NextUpdateGrinding = _ElapsedTime + UpdateInvervalGrinding;
            }
            if (_ElapsedTime > _NextUpdateAssemblerQueues)
            {
                CheckAssemblerQueues();
                _NextUpdateAssemblerQueues = _ElapsedTime + UpdateInvervalAssemblerQueues;
            }
            RefreshDisplays();
            if (_ElapsedTime > _ReInit)
            {
                _IsInit = false; //Refresh
            }
        }
    }
    /* <summary>
    Initialize lists with blocks to manage
    </summary> */
    private void Initialize()
    {
        _IsInit = false;
        InitializationResultMessage = string.Empty;
        _GroupData = new BuildAndRepairSystemQueuingGroupData[BuildAndRepairSystemQueuingGroups.Length];
        var idx = 0;
        foreach (var queuingGroup in BuildAndRepairSystemQueuingGroups)
        {
            _GroupData[idx] = new BuildAndRepairSystemQueuingGroupData(queuingGroup.Displays.Length);
            _GroupData[idx].Settings = queuingGroup;
            _GroupData[idx].RepairSystems = InitHandler<RepairSystemHandler>(queuingGroup);
            _GroupData[idx].Assemblers = InitAssemblerList(queuingGroup);
            _GroupData[idx].StatusDisplays = new List<StatusAndLogDisplay>();
            foreach (var displayDef in queuingGroup.Displays)
            {
                var statusDisplay = new StatusAndLogDisplay(_Program, string.IsNullOrEmpty(queuingGroup.Name) ? "BaR Group " + idx : queuingGroup.Name, displayDef.DisplayNames, null);
                _GroupData[idx].StatusDisplays.Add(statusDisplay);
                statusDisplay.Clear();
                statusDisplay.UpdateDisplay();
            }
            idx++;
        }
        _IsInit = true;
        return;
    }
    /* <summary>
    Initialize the group/list of items handler
    </summary> */
    public T InitHandler<T>(BuildAndRepairSystemQueuingGroup queuingGroup) where T : EntityHandler, new()
    {
        T handler = null;
        if (!string.IsNullOrWhiteSpace(queuingGroup.BuildAndRepairSystemGroupName))
        {
            var group = _Program.GridTerminalSystem.GetBlockGroupWithName(queuingGroup.BuildAndRepairSystemGroupName);
            if (group != null)
            {
                handler = new T();
                handler.Init(group);
            }
        }
        if (queuingGroup.BuildAndRepairSystemNames != null)
        {
            foreach (var name in queuingGroup.BuildAndRepairSystemNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var entity = _Program.GridTerminalSystem.GetBlockWithName(name);
                    if (entity != null)
                    {
                        if (handler == null) handler = new T();
                        handler.Init(entity);
                    }
                }
            }
        }
        if (handler == null || handler.Count == 0)
        {
            InitializationResultMessage += string.Format("\nFatalError: Group RepairSystems group empty/wrong types!");
            handler = null;
        }
        return handler;
    }
    /* <summary>
    Build list of assemblers
    </summary> */
    public List<long> InitAssemblerList(BuildAndRepairSystemQueuingGroup queuingGroup)
    {
        List<long> assemblers = null;
        if (!string.IsNullOrWhiteSpace(queuingGroup.AssemblerGroupName))
        {
            var group = _Program.GridTerminalSystem.GetBlockGroupWithName(queuingGroup.AssemblerGroupName);
            if (group != null)
            {
                assemblers = new List<long>();
                var entities = new List<IMyAssembler>();
                group.GetBlocksOfType(entities);
                foreach (var entity in entities)
                {
                    assemblers.Add(entity.EntityId);
                }
            }
        }
        if (queuingGroup.AssemblerNames != null)
        {
            foreach (var name in queuingGroup.AssemblerNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var entity = _Program.GridTerminalSystem.GetBlockWithName(name);
                    if (entity != null && entity is IMyAssembler)
                    {
                        if (assemblers == null) assemblers = new List<long>();
                        assemblers.Add(entity.EntityId);
                    }
                }
            }
        }
        if (assemblers == null || assemblers.Count == 0)
        {
            InitializationResultMessage += string.Format("Warning: Group Assemblers group empty/wrong types!");
            assemblers = null;
        }
        return assemblers;
    }
    private void RefreshDisplays()
    {
        foreach (var groupData in _GroupData)
        {
            groupData.RefreshDisplay(_ElapsedTime);
        }
    }
    /* <summary>
    This the basic algorithm and spread the items over the list of assemblers.
    </summary> */
    private void CheckAssemblerQueues()
    {
        foreach (var groupData in _GroupData)
        {
            groupData.CheckAssemblerQueues();
        }
    }
    /* <summary>
    Place your code here to handle specialized Grind handling
    </summary> */
    private void ScriptControlledGrinding()
    {
        // Simple example of Script controlled grind handling
        foreach (var groupData in _GroupData)
        {
            groupData.RepairSystems.ScriptControlled = true;
            var listGrindable = groupData.RepairSystems.PossibleGrindTargets();
            //If nothing to grind or current grinding object no longer in list (already ground)
            if (groupData.RepairSystems.CurrentPickedGrindTarget == null || listGrindable.IndexOf(groupData.RepairSystems.CurrentPickedGrindTarget) < 0)
            {
                foreach (var entry in listGrindable)
                {
                    try
                    {
                        if (entry.BlockDefinition.ToString().Contains("Tritanium"))
                        {
                            //Grind Tritanium
                            groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            break;
                        }
                        else if (entry.BlockDefinition.ToString().Contains("Glowing"))
                        {
                            //Grind Tritanium
                            groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            break;
                        }
                        else if (entry.BlockDefinition.ToString().Contains("Armor"))
                        {
                            //Skip over normal Armor blocks
                            continue;
                        }
                        else if (entry.BlockDefinition.ToString().Contains("Inhibitor"))
                        {
                            //Inhibitors are special Beacons
                            groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            break;
                        }
                        else if (entry.BlockDefinition.ToString().Contains("Beacon"))
                        {
                            //Skip over normal Beacons
                            continue;
                        }
                        else if (entry.BlockDefinition.ToString().Contains("T3"))
                        {
                            //Grind T3 Phasers
                            groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            break;
                        }
                        else if (entry.BlockDefinition.ToString().Contains("8x"))
                        {
                            //Grind Elite Blocks
                            groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            break;
                        }
                        else if (entry.BlockDefinition.ToString().Contains("T2"))
                        {
                            //Grind T3 Phasers
                            groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            break;
                        }
                        else if (entry.BlockDefinition.ToString().Contains("4x"))
                        {
                            //Grind Elite Blocks
                            groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            break;
                        }
                        else if (entry.BlockDefinition.ToString().Contains("T1"))
                        {
                            //Grind T3 Phasers
                            groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            break;
                        }
                        else if (entry.BlockDefinition.ToString().Contains("2x"))
                        {
                            //Grind Elite Blocks
                            groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            break;
                        }
                          else if (entry.BlockDefinition.ToString().Contains("Power_Coil"))
                        {
                            //Grind Auxiliary_Power_Coil_MK1
                            groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            break;
                        }
                          else if (entry.BlockDefinition.ToString().Contains("Quantum"))
                        {
                            //Grind Quantum Torpedoes
                            groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            break;
                        }
                          else if (entry.BlockDefinition.ToString().Contains("VoyagerCore"))
                        {
                            //Grind VoyagerCore
                            groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            break;
                        }
                        else
                        {
                            //Grind everything else
                            //groupData.RepairSystems.CurrentPickedGrindTarget = entry;
                            //break;
                            continue;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
public class BuildAndRepairSystemQueuingGroupData
{
    public BuildAndRepairSystemQueuingGroup Settings { get; set; }
    public RepairSystemHandler RepairSystems { get; set; }
    public List<long> Assemblers { get; set; }
    public List<StatusAndLogDisplay> StatusDisplays { get; set; }
    private int[] DisplayKindIdx { get; set; }
    private double[] NextSwitchTime { get; set; }
    private int _listTechNum = 0;
    private int _countRunForCheckTech = 0;
    public BuildAndRepairSystemQueuingGroupData(int count)
    {
        DisplayKindIdx = new int[count];
        NextSwitchTime = new double[count];
    }
    public void CheckAssemblerQueues()
    {
        if (RepairSystems != null && Assemblers != null)
        {
            var missingItems = RepairSystems.MissingComponents();
            foreach (var item in missingItems)
            {
                /* Test ->
                var blueprintDefinition = tryGetblueprintDefinitionByResultId(materialId);
                if (blueprintDefinition == null) return 0;
                <- end test */
                RepairSystems.EnsureQueued(Assemblers, item.Key, item.Value);
            }
        }
    }
    /* <summary>
    Refresh the status display
    </summary> */
    public void RefreshDisplay(double elapsedTime)
    {
        for (var idx = 0; idx < StatusDisplays.Count; idx++)
        {
            var display = StatusDisplays[idx];
            var settings = Settings.Displays[idx];
            if (display != null && settings != null)
            {
                display.Clear();
                if (settings.DisplayKinds != null && RepairSystems != null)
                {
                    if (elapsedTime >= NextSwitchTime[idx])
                    {
                        DisplayKindIdx[idx] = (DisplayKindIdx[idx] + 1) % settings.DisplayKinds.Length;
                        NextSwitchTime[idx] = elapsedTime + settings.DisplaySwitchTime;
                    }
                    switch (settings.DisplayKinds[DisplayKindIdx[idx]])
                    {
                        case DisplayKind.Status:
                            DisplayStatus(settings, display);
                            break;
                        case DisplayKind.ShortStatus:
                            DisplayShortStatus(settings, display);
                            break;
                        case DisplayKind.WeldTargets:
                            DisplayWeldTargets(settings, display);
                            break;
                        case DisplayKind.GrindTargets:
                            DisplayGrindTargets(settings, display);
                            break;
                        case DisplayKind.CollectTargets:
                            DisplayCollectTargets(settings, display);
                            break;
                        case DisplayKind.MissingItems:
                            DisplayMissingItems(settings, display);
                            break;
                        case DisplayKind.BlockWeldPriority:
                            DisplayBlockWeldPriorityList(settings, display);
                            break;
                        case DisplayKind.BlockGrindPriority:
                            DisplayBlockGrindPriorityList(settings, display);
                            break;
                    }
                    display.UpdateDisplay();
                }
            }
        }
    }
    /* <summary>
    Show the short status of the BaR-System
    </summary> */
    private void DisplayShortStatus(DisplayDefinition settings, StatusAndLogDisplay display)
    {
        display.AddStatus(string.Format("Online            : {0}", RepairSystems.CountOfWorking > 0));
        display.AddStatus(string.Format("CurrentWelding    : {0}", StatusAndLogDisplay.BlockName(RepairSystems.CurrentTarget)));
        var listB = RepairSystems.PossibleTargets();
        display.AddStatus(string.Format("Blocks to weld    : {0}", listB != null ? listB.Count : 0));
        display.AddStatus(string.Format("LastGrindTarget  : {0}", StatusAndLogDisplay.BlockName(RepairSystems.CurrentPickedGrindTarget)));
        listB = RepairSystems.PossibleGrindTargets();
        display.AddStatus(string.Format("Blocks grindable  : {0}", listB != null ? listB.Count : 0));
        display.AddStatus(string.Format("Grind Tech Targets: Count {0}", listB != null ? _listTechNum : 0));
        if(_countRunForCheckTech < 1){
            _countRunForCheckTech++;
        }
        else{
            GetTechTargetCount(listB);
            _countRunForCheckTech = 0;
        }
        var listF = RepairSystems.PossibleCollectTargets();
        display.AddStatus(string.Format("Floating items    : {0}", listF != null ? listF.Count : 0));
        display.AddStatus(string.Format("Missing item kinds: {0}", StatusAndLogDisplay.BlockName(RepairSystems.MissingComponents().Count)));
    }
    /* <summary>
    Show the detailed status of the BaR-System
    </summary> */
    private void DisplayStatus(DisplayDefinition settings, StatusAndLogDisplay display)
    {
        DisplayShortStatus(settings, display);
        display.AddStatus(string.Format("Search mode      : {0}", StatusAndLogDisplay.BlockName(RepairSystems.SearchMode)));
        display.AddStatus(string.Format("Work mode        : {0}", StatusAndLogDisplay.BlockName(RepairSystems.WorkMode)));
        display.AddStatus(string.Format("Build projected  : {0}", StatusAndLogDisplay.BlockName(RepairSystems.AllowBuild)));
        display.AddStatus(string.Format("UseIgnoreColor    : {0}", StatusAndLogDisplay.BlockName(RepairSystems.UseIgnoreColor)));
        display.AddStatus(string.Format("Script Controlled : {0}", RepairSystems.ScriptControlled));
    }
    /* <summary>
    Show the List of blocks to weld
    </summary> */
    private void DisplayWeldTargets(DisplayDefinition settings, StatusAndLogDisplay display)
    {
        var list = RepairSystems.PossibleTargets();
        display.AddStatus(string.Format("Weld Targets: Count {0}", list != null ? list.Count : 0));
        if (list == null) return;
        var iI = 2;
        foreach (var entry in list)
        {
            if (iI >= settings.DisplayMaxLines)
            {
                display.AddStatus(" ..");
                break;
            }
            display.AddStatus(string.Format(" {0}", StatusAndLogDisplay.BlockName(entry)));
            iI++;
        }
    }
    /* <summary>
    Show the List of blocks to grind
    </summary> */
    public enum TypesOfTech {Elite, Proficient, Common};

    // This stuff is broken -> too complex ??? THE FUCK KEEEN!!!
    private void GetTechTargetCount(List<IMySlimBlock> list)
    {
        /*
        List<IMySlimBlock> listTech = list.Where(item => 
        {
            var itemName = StatusAndLogDisplay.BlockName(item);
            var match = new[] { "Elite", "Proficient", "Common" };
            return match.Any(m => itemName.Contains(m));
        }).ToList();
        
        _listTechNum = listTech.Count;*/
        /*list.Where(item => {
            var itemName = StatusAndLogDisplay.BlockName(item);
            if (itemName == "Elite" || itemName == "Proficient" || itemName == "Common") _listTechNum++;
            return;
        });*/
        var tempCount = 0;
        list.ForEach(item => {
            if(item.BlockDefinition.ToString().Contains("8x"))
            {
                tempCount++;

            }
            else if(item.BlockDefinition.ToString().Contains("4x"))
            {
                tempCount++;

            }
            else if(item.BlockDefinition.ToString().Contains("2x"))
            {
                tempCount++;

            }
            else if(item.BlockDefinition.ToString().Contains("T1"))
            {
                tempCount++;

            }
            else if(item.BlockDefinition.ToString().Contains("T2"))
            {
                tempCount++;

            }
            else if(item.BlockDefinition.ToString().Contains("T3"))
            {
                tempCount++;

            }
            else if(item.BlockDefinition.ToString().Contains("Power_Coil"))
            {
                tempCount++;
                
            }
            else if(item.BlockDefinition.ToString().Contains("Quantum"))
            {
                tempCount++;
 
            }
            else if(item.BlockDefinition.ToString().Contains("VoyagerCore"))
            {
                tempCount++;

            }
            else if (item.BlockDefinition.ToString().Contains("Inhibitor"))
            {
                tempCount++;
            }
            else if (item.BlockDefinition.ToString().Contains("Tritanium"))
            {
                tempCount++;
            }
            else if (item.BlockDefinition.ToString().Contains("Glowing"))
            {
                tempCount++;
            }
            });
            _listTechNum = tempCount;
    } 

    private void DisplayGrindTargets(DisplayDefinition settings, StatusAndLogDisplay display)
    {
        var list = RepairSystems.PossibleGrindTargets();
        display.AddStatus(string.Format("Grind Targets: Count {0}", list != null ? list.Count : 0));
        //display.AddStatus(string.Format("Grind Tech Targets: Count {0}", list != null ? GetTechTargetCount(list) : 0));
        if (list == null) return;
        var iI = 2;
        foreach (var entry in list)
        {
            if (iI >= settings.DisplayMaxLines)
            {
                display.AddStatus(" ..");
                break;
            }
            display.AddStatus(string.Format(" {0}", StatusAndLogDisplay.BlockName(entry)));
            iI++;
        }
    }
    /* <summary>
    Show the List of collectible floating objects
    </summary> */
    private void DisplayCollectTargets(DisplayDefinition settings, StatusAndLogDisplay display)
    {
        var list = RepairSystems.PossibleCollectTargets();
        display.AddStatus(string.Format("Collect Targets: Count {0}", list != null ? list.Count : 0));
        if (list == null) return;
        var iI = 2;
        foreach (var entry in list)
        {
            if (iI >= settings.DisplayMaxLines)
            {
                display.AddStatus(" ..");
                break;
            }
            display.AddStatus(string.Format(" {0}", StatusAndLogDisplay.BlockName(entry)));
            iI++;
        }
    }
    /* <summary>
    Show the List of missing materials
    </summary> */
    private void DisplayMissingItems(DisplayDefinition settings, StatusAndLogDisplay display)
    {
        var list = RepairSystems.MissingComponents();
        display.AddStatus(string.Format("Missing Items: Count {0}", list != null ? list.Count : 0));
        if (list == null) return;
        var iI = 2;
        foreach (var entry in list)
        {
            if (iI >= settings.DisplayMaxLines)
            {
                display.AddStatus(" ..");
                break;
            }
            display.AddStatus(string.Format(" {0}: Amount={1}", entry.Key.SubtypeName, entry.Value));
            iI++;
        }
    }
    /* <summary>
    Show the List of block classes and there enabled state
    </summary> */
    private void DisplayBlockWeldPriorityList(DisplayDefinition settings, StatusAndLogDisplay display)
    {
        display.AddStatus("Weld Priority:");
        var list = RepairSystems.WeldPriorityList();
        foreach (var entry in list)
        {
            display.AddStatus(string.Format(" {0}/{1}", entry.ItemClass, entry.Enabled));
        }
    }
    /* <summary>
    Show the List of block classes and there enabled state
    </summary> */
    private void DisplayBlockGrindPriorityList(DisplayDefinition settings, StatusAndLogDisplay display)
    {
        display.AddStatus("Grind Priority:");
        var list = RepairSystems.GrindPriorityList();
        foreach (var entry in list)
        {
            display.AddStatus(string.Format(" {0}/{1}", entry.ItemClass, entry.Enabled));
        }
    }
    /* <summary>
    Show the List of component classes and there enabled state
    </summary> */
    private void DisplayComponentClassesList(DisplayDefinition settings, StatusAndLogDisplay display)
    {
        display.AddStatus("ComponentClassList:");
        var list = RepairSystems.ComponentClassList();
        foreach (var entry in list)
        {
            display.AddStatus(string.Format(" {0}/{1}", entry.ItemClass, entry.Enabled));
        }
    }
}
/* <summary>
Class to handle the RepairSystems
</summary> */
public class RepairSystemHandler : EntityHandler<IMyShipWelder>
{
    private Func<IEnumerable<long>, VRage.Game.MyDefinitionId, int, int> _EnsureQueued;
    private Func<IMyProjector, Dictionary<VRage.Game.MyDefinitionId, VRage.MyFixedPoint>, int> _NeededComponents4Blueprint;
    /* <summary>
    The block classes the system distinguish
    </summary> */
    public enum BlockClass
    {
        AutoRepairSystem = 1,
        ShipController,
        Thruster,
        Gyroscope,
        CargoContainer,
        Conveyor,
        ControllableGun,
        PowerBlock,
        ProgrammableBlock,
        Projector,
        FunctionalBlock,
        ProductionBlock,
        Door,
        ArmorBlock
    }
    /* <summary>
    The component classes the system distinguish
    </summary> */
    public enum ComponentClass
    {
        Material = 1,
        Ingot,
        Ore,
        Stone,
        Gravel
    }
    /* <summary>
    The search modes supported by the block
    </summary> */
    public enum SearchModes
    {
        Grids = 0x0001,
        BoundingBox = 0x0002
    }
    /* <summary>
    The work modes supported by the block
    </summary> */
    public enum WorkModes
    {
        /* <summary>
        Grind only if nothing to weld
        </summary> */
        WeldBeforeGrind = 0x0001,
        /* <summary>
        Weld only if nothing to grind
        </summary> */
        GrindBeforeWeld = 0x0002,
        /* <summary>
        Grind only if nothing to weld or build waiting for missing items
        </summary> */
        GrindIfWeldGetStuck = 0x0004,
        /* <summary>
        Only welding is allowed
        </summary> */
        WeldOnly = 0x0008,
        /* <summary>
        Only grinding is allowed
        </summary> */
        GrindOnly = 0x0010
    }
    /* <summary>
    Block/Component class and it's state
    </summary> */
    public class ClassState<T> where T : struct
    {
        public T ItemClass { get; }
        public bool Enabled { get; }
        public ClassState(T itemClass, bool enabled)
        {
            ItemClass = itemClass;
            Enabled = enabled;
        }
    }
    /* <summary>
    Set the Help Others state
    </summary> */
    public bool HelpOther
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].HelpOthers : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.HelpOthers = value;
        }
    }
    /* <summary>
    Set AllowBuild (projected blocks)
    </summary> */
    public bool AllowBuild
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueBool("BuildAndRepair.AllowBuild") : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueBool("BuildAndRepair.AllowBuild", value);
        }
    }
    /* <summary>
    Set the search mode of the block
    </summary> */
    public SearchModes SearchMode
    {
        get
        {
            return _Entities.Count > 0 ? (SearchModes)_Entities[0].GetValue<long>("BuildAndRepair.Mode") : SearchModes.Grids;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValue<long>("BuildAndRepair.Mode", (long)value);
        }
    }
    /* <summary>
    Set the search mode of the block
    </summary> */
    public WorkModes WorkMode
    {
        get
        {
            return _Entities.Count > 0 ? (WorkModes)_Entities[0].GetValue<long>("BuildAndRepair.WorkMode") : WorkModes.WeldBeforeGrind;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValue<long>("BuildAndRepair.WorkMode", (long)value);
        }
    }
    /* <summary>
    Enable/Disable the use of the Ignore Color
    If enabled block's with with color 'IgnoreColor' will be ignored.
    You could use this do have intentionally unfinished blocks, and still use AutoRepairSystem on the rest.
    </summary> */
    public bool UseIgnoreColor
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueBool("BuildAndRepair.UseIgnoreColor") : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueBool("BuildAndRepair.UseIgnoreColor", value);
        }
    }
    /* <summary>
    Set the ignore color
    X=Hue        0 .. 1 -> * 360 -> Displayed value
    Y=Saturation -1 .. 1 -> * 100 -> Displayed value
    Z=Value      -1 .. 1 -> * 100 -> Displayed value
    </summary> */
    public Vector3 IgnoreColor
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValue<Vector3>("BuildAndRepair.IgnoreColor") : Vector3.Zero;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValue<Vector3>("BuildAndRepair.IgnoreColor", value);
        }
    }
    /* <summary>
    Enable/Disable the use of the Grind Color
    If enabled block's with with color 'GrindColor' will be ground down.
    </summary> */
    public bool UseGrindColor
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueBool("BuildAndRepair.UseGrindColor") : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueBool("BuildAndRepair.UseGrindColor", value);
        }
    }
    /* <summary>
    Set the grind color
    X=Hue        0 .. 1 -> * 360 -> Displayed value
    Y=Saturation -1 .. 1 -> * 100 -> Displayed value
    Z=Value      -1 .. 1 -> * 100 -> Displayed value
    </summary> */
    public Vector3 GrindColor
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValue<Vector3>("BuildAndRepair.GrindColor") : Vector3.Zero;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValue<Vector3>("BuildAndRepair.GrindColor", value);
        }
    }
    /* <summary>
    If set: AutoGrind enemy blocks in range
    </summary> */
    public bool GrindJanitorEnemies
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueBool("BuildAndRepair.GrindJanitorEnemies") : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueBool("BuildAndRepair.GrindJanitorEnemies", value);
        }
    }
    /* <summary>
    If set: AutoGrind not owned blocks in range
    </summary> */
    public bool GrindJanitorNotOwned
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueBool("BuildAndRepair.GrindJanitorNotOwned") : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueBool("BuildAndRepair.GrindJanitorNotOwned", value);
        }
    }
    /* <summary>
    If set: AutoGrind blocks owned by neutrals in range
    </summary> */
    public bool GrindJanitorNeutrals
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueBool("BuildAndRepair.GrindJanitorNeutrals") : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueBool("BuildAndRepair.GrindJanitorNeutrals", value);
        }
    }
    /* <summary>
    If set: AutoGrind grinds blocks only down to the 'Out of order' level
    </summary> */
    public bool GrindJanitorOptionDisableOnly
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueBool("BuildAndRepair.GrindJanitorOptionDisableOnly") : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueBool("BuildAndRepair.GrindJanitorOptionDisableOnly", value);
        }
    }
    /* <summary>
    If set: AutoGrind grinds blocks only down to the 'Hack' level
    </summary> */
    public bool GrindJanitorOptionHackOnly
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueBool("BuildAndRepair.GrindJanitorOptionHackOnly") : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueBool("BuildAndRepair.GrindJanitorOptionHackOnly", value);
        }
    }
    /* <summary>
    If set block are only welded to functional level
    </summary> */
    public bool WeldOptionFunctionalOnly
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueBool("BuildAndRepair.WeldOptionFunctionalOnly") : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueBool("BuildAndRepair.WeldOptionFunctionalOnly", value);
        }
    }
    /* <summary>
    Set the with of the working area
    </summary> */
    public float AreaWidth
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueFloat("BuildAndRepair.AreaWidth") : 0;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueFloat("BuildAndRepair.AreaWidth", value);
        }
    }
    /* <summary>
    Set the left/right offset of the working area from block center
    </summary> */
    public float AreaOffsetLeftRight
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueFloat("BuildAndRepair.AreaOffsetLeftRight") : 0;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueFloat("BuildAndRepair.AreaOffsetLeftRight", value);
        }
    }
    /* <summary>
    Set the height of the working area
    </summary> */
    public float AreaHeight
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueFloat("BuildAndRepair.AreaHeight") : 0;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueFloat("BuildAndRepair.AreaHeight", value);
        }
    }
    /* <summary>
    Set the up/down offset of the working area from block center
    </summary> */
    public float AreaOffsetUpDown
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueFloat("BuildAndRepair.AreaOffsetUpDown") : 0;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueFloat("BuildAndRepair.AreaOffsetUpDown", value);
        }
    }
    /* <summary>
    Set the depth of the working area
    </summary> */
    public float AreaDepth
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueFloat("BuildAndRepair.AreaDepth") : 0;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueFloat("BuildAndRepair.AreaDepth", value);
        }
    }
    /* <summary>
    Set the depth of the working area
    </summary> */
    public float AreaOffsetFrontBack
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueFloat("BuildAndRepair.AreaOffsetFrontBack") : 0;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueFloat("BuildAndRepair.AreaOffsetFrontBack", value);
        }
    }
    /* <summary>
    Get a list with all known block classes and there
    weld enabled state in descending order of priority.
    </summary> */
    public List<ClassState<BlockClass>> WeldPriorityList()
    {
        if (_Entities.Count > 0)
        {
            var list = _Entities[0].GetValue<List<string>>("BuildAndRepair.WeldPriorityList");
            var blockList = new List<ClassState<BlockClass>>();
            foreach (var item in list)
            {
                var values = item.Split(';');
                BlockClass blockClass;
                bool enabled;
                if (Enum.TryParse<BlockClass>(values[0], out blockClass) && bool.TryParse(values[1], out enabled))
                {
                    blockList.Add(new ClassState<BlockClass>(blockClass, enabled));
                }
            }
            return blockList;
        }
        return null;
    }
    /* <summary>
    Get the weld priority of the given block class
    </summary> */
    public int GetWeldPriority(BlockClass blockClass)
    {
        if (_Entities.Count > 0)
        {
            var getPriority = _Entities[0].GetValue<Func<int, int>>("BuildAndRepair.GetWeldPriority");
            return getPriority((int)blockClass);
        }
        else return int.MaxValue;
    }
    /* <summary>
    Set the weld priority of the given block class (lower number higher priority)
    </summary> */
    public void SetWeldPriority(BlockClass blockClass, int prio)
    {
        foreach (var entity in _Entities)
        {
            var setPriority = entity.GetValue<Action<int, int>>("BuildAndRepair.SetWeldPriority");
            setPriority((int)blockClass, prio);
        }
    }
    /* <summary>
    Get the weld enabled state of the given block class
    Enabled=True: Block of that class will be repaired/constructed
    Enabled=False: Block's of that class will be ignored
    </summary> */
    public bool GetWeldEnabled(BlockClass blockClass)
    {
        if (_Entities.Count > 0)
        {
            var getEnabled = _Entities[0].GetValue<Func<int, bool>>("BuildAndRepair.GetWeldEnabled");
            return getEnabled((int)blockClass);
        }
        else return false;
    }
    /* <summary>
    Set the weld enabled state of the given block class (see GetEnabled)
    </summary> */
    public void SetWeldEnabled(BlockClass blockClass, bool enabled)
    {
        foreach (var entity in _Entities)
        {
            var setEnabled = entity.GetValue<Action<int, bool>>("BuildAndRepair.SetWeldEnabled");
            setEnabled((int)blockClass, enabled);
        }
    }
    /* <summary>
    Get a list with all known block classes and their grind enabled state in descending order of priority.
    </summary> */
    public List<ClassState<BlockClass>> GrindPriorityList()
    {
        if (_Entities.Count > 0)
        {
            var list = _Entities[0].GetValue<List<string>>("BuildAndRepair.GrindPriorityList");
            var blockList = new List<ClassState<BlockClass>>();
            foreach (var item in list)
            {
                var values = item.Split(';');
                BlockClass blockClass;
                bool enabled;
                if (Enum.TryParse<BlockClass>(values[0], out blockClass) &&
                  bool.TryParse(values[1], out enabled))
                {
                    blockList.Add(new ClassState<BlockClass>(blockClass, enabled));
                }
            }
            return blockList;
        }
        return null;
    }
    /* <summary>
    Get the grind priority of the given block class
    </summary> */
    public int GetGrindPriority(BlockClass blockClass)
    {
        if (_Entities.Count > 0)
        {
            var getPriority = _Entities[0].GetValue<Func<int, int>>("BuildAndRepair.GetGrindPriority");
            return getPriority((int)blockClass);
        }
        else return int.MaxValue;
    }
    /* <summary>
    Set the grind priority of the given block class (lower number higher priority)
    </summary> */
    public void SetGrindPriority(BlockClass blockClass, int prio)
    {
        foreach (var entity in _Entities)
        {
            var setPriority = entity.GetValue<Action<int, int>>("BuildAndRepair.SetGrindPriority");
            setPriority((int)blockClass, prio);
        }
    }
    /* <summary>
    Get the grind enabled state of the given block class
    Enabled=True: Blocks of that class will be ground down
    Enabled=False: Blocks of that class will be ignored
    </summary> */
    public bool GetGrindEnabled(BlockClass blockClass)
    {
        if (_Entities.Count > 0)
        {
            var getEnabled = _Entities[0].GetValue<Func<int, bool>>("BuildAndRepair.GetGrindEnabled");
            return getEnabled((int)blockClass);
        }
        else return false;
    }
    /* <summary>
    Set the grind enabled state of the given block class (see GetEnabled)
    </summary> */
    public void SetGrindEnabled(BlockClass blockClass, bool enabled)
    {
        foreach (var entity in _Entities)
        {
            var setEnabled = entity.GetValue<Action<int, bool>>("BuildAndRepair.SetGrindEnabled");
            setEnabled((int)blockClass, enabled);
        }
    }
    /* <summary>
    Get a list with all known component classes and their enabled state in descending order of priority.
    </summary> */
    public List<ClassState<ComponentClass>> ComponentClassList()
    {
        if (_Entities.Count > 0)
        {
            var list = _Entities[0].GetValue<List<string>>("BuildAndRepair.ComponentClassList");
            var compList = new List<ClassState<ComponentClass>>();
            foreach (var item in list)
            {
                var values = item.Split(';');
                ComponentClass compClass;
                bool enabled;
                if (Enum.TryParse<ComponentClass>(values[0], out compClass) &&
                  bool.TryParse(values[1], out enabled))
                {
                    compList.Add(new ClassState<ComponentClass>(compClass, enabled));
                }
            }
            return compList;
        }
        return null;
    }
    /* <summary>
    Get the priority of the given component class
    </summary> */
    public int GetCollectPriority(ComponentClass compClass)
    {
        if (_Entities.Count > 0)
        {
            var getPriority = _Entities[0].GetValue<Func<int, int>>("BuildAndRepair.GetCollectPriority");
            return getPriority((int)compClass);
        }
        else return int.MaxValue;
    }
    /* <summary>
    Set the priority of the given component class (lower number higher priority)
    </summary> */
    public void SetCollectPriority(ComponentClass compClass, int prio)
    {
        foreach (var entity in _Entities)
        {
            var setPriority = entity.GetValue<Action<int, int>>("BuildAndRepair.SetCollectPriority");
            setPriority((int)compClass, prio);
        }
    }
    /* <summary>
    Get the enabled state of the given component class
    Enabled=True: Component of that class will be collected
    Enabled=False: Component's of that class will be ignored
    </summary> */
    public bool GetCollectEnabled(ComponentClass compClass)
    {
        if (_Entities.Count > 0)
        {
            var getEnabled = _Entities[0].GetValue<Func<int, bool>>("BuildAndRepair.GetCollectEnabled");
            return getEnabled((int)compClass);
        }
        else return false;
    }
    /* <summary>
    Set the enabled state of the given component class (see GetEnabled)
    </summary> */
    public void SetCollectEnabled(ComponentClass compClass, bool enabled)
    {
        foreach (var entity in _Entities)
        {
            var setEnabled = entity.GetValue<Action<int, bool>>("BuildAndRepair.SetCollectEnabled");
            setEnabled((int)compClass, enabled);
        }
    }
    /* <summary>
    Set if the Block should only collect floating items (ore/ingot/material) if nothing else to do (no welding, no grinding, no material for welding)
    </summary> */
    public bool CollectIfIdle
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueBool("BuildAndRepair.CollectIfIdle") : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueBool("BuildAndRepair.CollectIfIdle", value);
        }
    }
    /* <summary>
    Set if the Block should push all ore/ingot immediately out of its inventory,
    else this will happen only if no more room to store the next items to be picked.
    </summary> */
    public bool PushIngotOreImmediately
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueBool("BuildAndRepair.PushIngotOreImmediately") : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueBool("BuildAndRepair.PushIngotOreImmediately", value);
        }
    }
    /* <summary>
    Get the block that is currently being repaired/build.
    </summary> */
    public IMySlimBlock CurrentTarget
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValue<IMySlimBlock>("BuildAndRepair.CurrentTarget") : null;
        }
    }
    /* <summary>
    Get the block that is currently being ground.
    </summary> */
    public IMySlimBlock CurrentGrindTarget
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValue<IMySlimBlock>("BuildAndRepair.CurrentGrindTarget") : null;
        }
    }
    /* <summary>
    Set if the Block if controlled by script. (If controlled by script use PossibleTargets and CurrentPickedTarget to set the block that should be built/repaired)
    </summary> */
    public bool ScriptControlled
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValueBool("BuildAndRepair.ScriptControlled") : false;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValueBool("BuildAndRepair.ScriptControlled", value);
        }
    }
    /* <summary>
    Get a list of missing components.
    </summary> */
    public Dictionary<VRage.Game.MyDefinitionId, int> MissingComponents()
    {
        var missingItems = new Dictionary<VRage.Game.MyDefinitionId, int>();
        foreach (var entity in _Entities)
        {
            var dict = entity.GetValue<Dictionary<VRage.Game.MyDefinitionId, int>>("BuildAndRepair.MissingComponents");
            //Merge dictionaries but only first report of an item or higher amount
            //(do not add up the missing items, as overlapping systems report same missing items)
            if (dict != null && dict.Count > 0)
            {
                int value;
                foreach (var newItem in dict)
                {
                    if (missingItems.TryGetValue(newItem.Key, out value))
                    {
                        if (newItem.Value > value) missingItems[newItem.Key] = newItem.Value;
                    }
                    else
                    {
                        missingItems.Add(newItem.Key, newItem.Value);
                    }
                }
            }
        }
        return missingItems;
    }
    /* <summary>
    Get a list of possible repair/build targets (Contains only damaged/deformed/new block's in range of the system)
    </summary> */
    public List<IMySlimBlock> PossibleTargets()
    {
        if (_Entities.Count > 0)
        {
            return _Entities[0].GetValue<List<IMySlimBlock>>("BuildAndRepair.PossibleTargets");
        }
        return null;
    }
    /* <summary>
    Get the Block that should currently built/repaired.
    In order to build the given block the property 'ScriptControlled' has to be true and the block has to be in the list of 'PossibleTargets'.
    If 'ScriptControlled' is true and the block is not in the 'PossibleTargets' the system will do nothing.
    </summary> */
    public IMySlimBlock CurrentPickedTarget
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValue<IMySlimBlock>("BuildAndRepair.CurrentPickedTarget") : null;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValue("BuildAndRepair.CurrentPickedTarget", value);
        }
    }
    /* <summary>
    Get a list of possible grind targets.
    </summary> */
    public List<IMySlimBlock> PossibleGrindTargets()
    {
        if (_Entities.Count > 0)
        {
            return _Entities[0].GetValue<List<IMySlimBlock>>("BuildAndRepair.PossibleGrindTargets");
        }
        return null;
    }
    /* <summary>
    Get the Block that should currently ground.
    In order to grind the given block the property 'ScriptControlled' has to be true and the block has to be in the list of 'PossibleGrindTargets'.
    If 'ScriptControlled' is true and the block is not in the 'PossibleGrindTargets' the system will do nothing.
    </summary> */
    public IMySlimBlock CurrentPickedGrindTarget
    {
        get
        {
            return _Entities.Count > 0 ? _Entities[0].GetValue<IMySlimBlock>("BuildAndRepair.CurrentPickedGrindTarget") : null;
        }
        set
        {
            foreach (var entity in _Entities) entity.SetValue("BuildAndRepair.CurrentPickedGrindTarget", value);
        }
    }
    /* <summary>
    Get a list of Possible Collect Targets.
    </summary> */
    public List<IMyEntity> PossibleCollectTargets()
    {
        if (_Entities.Count > 0)
        {
            return _Entities[0].GetValue<List<IMyEntity>>("BuildAndRepair.PossibleCollectTargets");
        }
        return null;
    }
    /* <summary>
    Ensures that the given amount is either in inventory or the production queue of the given production blocks
    </summary> */
    public int EnsureQueued(IEnumerable<long> productionBlockIds, VRage.Game.MyDefinitionId materialId, int amount)
    {
        if (_Entities.Count > 0)
        {
            if (_EnsureQueued == null)
            {
                _EnsureQueued = _Entities[0].GetValue<Func<IEnumerable<long>, VRage.Game.MyDefinitionId, int, int>>("BuildAndRepair.ProductionBlock.EnsureQueued");
            }
            if (_EnsureQueued != null)
            {
                return _EnsureQueued(productionBlockIds, materialId, amount);
            }
            return -3;
        }
        return -2;
    }
    /* <summary>
    Retrieve the total components amount needed to build the projected blueprint
    </summary> */
    /* <param name="projector"></param>
    <param name="componentList"></param>*/
    public int NeededComponents4Blueprint(IMyProjector projector, Dictionary<VRage.Game.MyDefinitionId, VRage.MyFixedPoint> componentList)
    {
        if (_Entities.Count > 0)
        {
            if (_NeededComponents4Blueprint == null)
            {
                _NeededComponents4Blueprint = _Entities[0].GetValue<Func<IMyProjector, Dictionary<VRage.Game.MyDefinitionId, VRage.MyFixedPoint>, int>>("BuildAndRepair.Inventory.NeededComponents4Blueprint");
            }
            if (_NeededComponents4Blueprint != null)
            {
                return _NeededComponents4Blueprint(projector, componentList);
            }
            return -3;
        }
        return -2;
    }
}
/* <summary>
Class to handle Entities
</summary> */
public class EntityHandler<T> : EntityHandler where T : class, IMyTerminalBlock
{
    protected readonly List<T> _Entities = new List<T>();
    protected readonly HashSet<MyDefinitionId> _DefinitionIdsInclude = new HashSet<MyDefinitionId>();
    protected readonly HashSet<MyDefinitionId> _DefinitionIdsExclude = new HashSet<MyDefinitionId>();
    public IEnumerable<T> Entities
    {
        get
        {
            return _Entities;
        }
    }
    public HashSet<MyDefinitionId> DefinitionIdsInclude
    {
        get
        {
            return _DefinitionIdsInclude;
        }
    }
    public HashSet<MyDefinitionId> DefinitionIdsExclude
    {
        get
        {
            return _DefinitionIdsExclude;
        }
    }
    public bool AreEnabled { get; private set; }
    /* <summary>
    Count of Working Entities (on and functional)
    </summary> */
    public int CountOfWorking
    {
        get
        {
            var res = 0;
            foreach (var entity in _Entities) if (entity.IsWorking && entity.IsFunctional) res++;
            return res;
        }
    }
    /* <summary>
    Get total count
    </summary> */
    protected override int GetCount()
    {
        return _Entities.Count;
    }
    /* <summary>
    Load entities from group
    </summary> */
    public override void Init(IMyBlockGroup group, bool add = false)
    {
        if (!add) _Entities.Clear();
        var entities = new List<T>();
        group.GetBlocksOfType(entities);
        foreach (var entity in entities)
        {
            AddEntity(entity);
        }
        CheckEnabled();
    }
    /* <summary>
    Load entity by name
    </summary> */
    /* <param name="blocks"></param>
    <param name="name"></param> */
    public override void Init(VRage.Game.ModAPI.Ingame.IMyEntity newEntity, bool add = false)
    {
        if (!add) _Entities.Clear();
        var entity = newEntity as T;
        if (AddEntity(entity))
        {
            CheckEnabled();
        }
    }
    /* <summary>
    Load entity filtered by given collect function
    </summary> */
    /* <param name="blocks"></param>
    <param name="name"></param> */
    public void Init(IMyGridTerminalSystem gridTerminalSystem, Func<T, bool> collect = null, bool add = false)
    {
        if (!add) _Entities.Clear();
        if (gridTerminalSystem != null)
        {
            var entities = new List<T>();
            gridTerminalSystem.GetBlocksOfType<T>(entities, collect);
            foreach (var entity in entities)
            {
                AddEntity(entity);
            }
            CheckEnabled();
        }
    }
    /* <summary>
    Starting/Stopping
    </summary> */
    protected virtual bool AddEntity(T entity)
    {
        if (entity == null || _Entities.IndexOf(entity) >= 0) return false;
        var newDefId = entity.BlockDefinition;
        var allowed = DefinitionIdsInclude.Count <= 0;
        foreach (var defId in DefinitionIdsInclude)
        {
            if (defId.TypeId == newDefId.TypeId && (string.IsNullOrEmpty(defId.SubtypeName) || defId.SubtypeName.Equals(newDefId.SubtypeName)))
            {
                allowed = true;
                break;
            }
        }
        if (!allowed) return false;
        foreach (var defId in DefinitionIdsExclude)
        {
            if (defId.TypeId == newDefId.TypeId && (string.IsNullOrEmpty(defId.SubtypeName) || defId.SubtypeName.Equals(newDefId.SubtypeName)))
            {
                return false;
            }
        }
        _Entities.Add(entity);
        return true;
    }
    /* <summary>
    Starting/Stopping
    </summary> */
    public void Enabled(bool enabled)
    {
        foreach (var entity in _Entities)
        {
            var funcBlock = entity as IMyFunctionalBlock;
            if (funcBlock != null && funcBlock.Enabled != enabled) funcBlock.Enabled = enabled;
        }
        AreEnabled = enabled;
    }
    private void CheckEnabled()
    {
        foreach (var entity in _Entities)
        {
            if (entity.IsWorking && entity.IsFunctional)
            {
                AreEnabled = true;
                break;
            }
        }
    }
}
public abstract class EntityHandler
{
    public int Count { get { return GetCount(); } }
    public abstract void Init(IMyBlockGroup group, bool add = false);
    public abstract void Init(VRage.Game.ModAPI.Ingame.IMyEntity entity, bool add = false);
    protected abstract int GetCount();
    public static string GetCustomData(string customData, string startTag, string endTag)
    {
        var start = customData.IndexOf(startTag);
        var end = customData.LastIndexOf(endTag);
        if (start < 0 || end < 0 || end < start) return null;
        return customData.Substring(start + startTag.Length, end - start - startTag.Length);
    }
    public static string GetCustomValue(string customData, string name)
    {
        var tag = "<" + name + "=";
        var start = customData.IndexOf(tag);
        if (start < 0) return null;
        var end = customData.IndexOf("/>", start + tag.Length);
        if (end < 0) return null;
        return customData.Substring(start + tag.Length, end - start - tag.Length);
    }
}
/* <summary>
Status and Log functions
</summary> */
public class StatusAndLogDisplay
{
    private readonly MyGridProgram _Program;
    private readonly List<IMyTextSurface> _StatusPanels = new List<IMyTextSurface>();
    private readonly List<IMyTextSurface> _LogPanels = new List<IMyTextSurface>();
    private readonly string _AIName;
    private string _LogText = "";
    private string _StatusText = "";
    private string _ErrorText = "";
    private int _RefreshDelay;
    private readonly string[] _LcdStatusPanels;
    private readonly string[] _LcdLogPanels;
    /* <summary>
    /// Count of Lines in Log Display
    </summary> */
    public int MaxLogLines { get; set; }
    public bool ShowHeader { get; set; }
    public StatusAndLogDisplay(MyGridProgram caller, string name, string[] lcdStatusPanels, string[] lcdLogPanels)
    {
        ShowHeader = true;
        _Program = caller;
        _AIName = name;
        _LcdStatusPanels = lcdStatusPanels;
        _LcdLogPanels = lcdLogPanels;
        MaxLogLines = 20; //Default
        ReloadDisplays();
    }
    /* <summary>
    Reload the displays (after renaming, adding)
    </summary> */
    public string ReloadDisplays()
    {
        var res = FindPanels(_Program, _LcdStatusPanels, _StatusPanels);
        res += FindPanels(_Program, _LcdLogPanels, _LogPanels);
        return res;
    }
    /* <summary>
    Cyclic tries to reload the DisplayPanels (so the LCD could be added dynamically)
    </summary> */
    public void CyclicReloadDisplays()
    {
        _RefreshDelay--;
        if (_RefreshDelay > 0) return;
        ReloadDisplays();
        _RefreshDelay = 20;
    }
    /* <summary>
    Write a text Log
    </summary> */
    public void Log(string msg)
    {
        var useHadline = !string.IsNullOrEmpty(_AIName);
        var maxlines = MaxLogLines + (useHadline ? 0 : 1);
        if (!string.IsNullOrEmpty(msg))
        {
            _LogText += "\n" + msg;
            var lines = _LogText.Split('\n');
            if (lines.Length >= maxlines)
            {
                _LogText = "";
                for (var a = maxlines; a > 0; a--) _LogText += "\n" + lines[lines.Length - a];
            }
        }
    }
    /* <summary>
    Clears "Status Undefined" Error
    </summary> */
    public void Clear()
    {
        _StatusText = "";
        _ErrorText = "";
    }
    internal void AddStatus(string line)
    {
        _StatusText += line + "\n";
    }
    internal void AddError(string line)
    {
        _ErrorText = line + "\n";
    }
    /* <summary>
    Write Status, Error, Log to the configured panels
    </summary> */
    public void UpdateDisplay()
    {
        var text = string.Empty;
        if (ShowHeader) text = _AIName + " (" + DateTime.Now + "):\n";
        if (_ErrorText.Length > 0) text += _ErrorText;
        text += _StatusText;
        foreach (var panel in _StatusPanels) SetPanelText(panel, text);
        _Program.Echo(!string.IsNullOrEmpty(_ErrorText) ? _ErrorText : text);
        text = !string.IsNullOrEmpty(_AIName) ? _AIName + _LogText : _LogText;
        foreach (var panel in _LogPanels) SetPanelText(panel, text);
    }
    /* <summary>
    Finds TextPanels with the given names
    </summary> */
    private static string FindPanels(MyGridProgram caller, IReadOnlyList<string> names, ICollection<IMyTextSurface> list)
    {
        string res = string.Empty;
        if (names != null && names.Count > 0)
        {
            foreach (var name in names)
            {
                string blockName;
                int index;
                GetNameAndIndex(name, out blockName, out index);
                var block = caller.GridTerminalSystem.GetBlockWithName(blockName);
                if (block == null)
                {
                    res += string.Format("LCD {0} not found\n", blockName);
                    continue;
                }
                var textSurface = block as IMyTextSurface;
                if (textSurface != null)
                {
                    list.Add(textSurface);
                    continue;
                }
                var textSurfaceProvider = block as IMyTextSurfaceProvider;
                if (textSurfaceProvider != null)
                {
                    if (textSurfaceProvider.SurfaceCount > index)
                    {
                        list.Add(textSurfaceProvider.GetSurface(index));
                        continue;
                    }
                    res += string.Format("LCD {0} index {1} out of range (allowed 0..{2})\n", blockName, index, textSurfaceProvider.SurfaceCount - 1);
                    continue;
                }
                res += string.Format("{0} is not an LCD.\n", blockName);
            }
        }
        if (!string.IsNullOrEmpty(res)) caller.Echo(res);
        return res;
    }
    private static void GetNameAndIndex(string name, out string blockName, out int index)
    {
        index = 0;
        var idxStart = name.LastIndexOf('[');
        if (idxStart >= 0)
        {
            var idxEnd = name.LastIndexOf(']');
            if (idxEnd >= 0 && idxEnd > idxStart)
            {
                if (int.TryParse(name.Substring(idxStart + 1, idxEnd - idxStart - 1), out index))
                {
                    blockName = name.Substring(0, idxStart);
                }
                else blockName = name;
            }
            else blockName = name;
        }
        else blockName = name;
    }
    /* <summary>
    Sets panel text if its title is either default or our name.  
    </summary> */
    public static void SetPanelText(IMyTextSurface panel, string text)
    {
        panel.ContentType = ContentType.TEXT_AND_IMAGE;
        panel.WriteText(text, false);
    }
    /* <summary>
    Convert displayed values (Terminal) with correct units -> MW
    </summary> */
    public static float PowerUnitMultiple(string unit)
    {
        if (unit.StartsWith("W")) return 0.000001f;
        if (unit.StartsWith("kW")) return 0.001f;
        if (unit.StartsWith("MW")) return 1f;
        return unit.StartsWith("GW") ? 1000f : 1f;
    }
    public static string DisplayPowerValueUnit(float value)
    {
        if (Math.Abs(value) < 0.001) return Math.Round(value * 1000000f) + "W";
        if (Math.Abs(value) < 1) return Math.Round(value * 1000f) + "kW";
        if (Math.Abs(value) < 1000) return Math.Round(value) + "MW";
        return Math.Round(value / 1000f) + "GW";
    }
    public static string DisplayPowerRate(float current, float max, string ext = "")
    {
        return string.Format("{0:0.00}% {1}{3}/{2}{3}", max > 0 ? current * 100 / max : 0, DisplayPowerValueUnit(current), DisplayPowerValueUnit(max), ext);
    }
    /* <param name="rad"></param>
    <returns></returns> */
    public static double ToDegree(double rad)
    {
        return rad * 180 / Math.PI;
    }
    /* <summary>
    Get Name of Block
    </summary> */
    /* <param name="block"></param>
    <returns></returns> */
    public static string BlockName(object block, bool includeGrid = false)
    {
        var inventory = block as IMyInventory;
        if (inventory != null)
        {
            block = inventory.Owner;
        }
        var slimBlock = block as IMySlimBlock;
        if (slimBlock != null)
        {
            if (slimBlock.FatBlock != null) block = slimBlock.FatBlock;
            else
            {
                if (includeGrid) return string.Format("{0}.{1}", slimBlock.CubeGrid != null ? slimBlock.CubeGrid.DisplayName : "Unknown Grid", slimBlock.BlockDefinition.SubtypeName);
                return string.Format("{0}", slimBlock.BlockDefinition.SubtypeName);
            }
        }
        var terminalBlock = block as IMyTerminalBlock;
        if (terminalBlock != null)
        {
            if (includeGrid) return string.Format("{0}.{1}", terminalBlock.CubeGrid != null ? terminalBlock.CubeGrid.DisplayName : "Unknown Grid", terminalBlock.CustomName);
            return string.Format("{0}", terminalBlock.CustomName);
        }
        var cubeBlock = block as IMyCubeBlock;
        if (cubeBlock != null)
        {
            if (includeGrid) return string.Format("{0} [{1}/{2}]", cubeBlock.CubeGrid != null ? cubeBlock.CubeGrid.DisplayName : "Unknown Grid", cubeBlock.BlockDefinition.TypeIdString, cubeBlock.BlockDefinition.SubtypeName);
            return string.Format("[{0}/{1}]", cubeBlock.BlockDefinition.TypeIdString, cubeBlock.BlockDefinition.SubtypeName);
        }
        var entity = block as IMyEntity;
        if (entity != null)
        {
            return string.Format("{0} ({1})", entity.DisplayName, entity.EntityId);
        }
        var cubeGrid = block as IMyCubeGrid;
        if (cubeGrid != null) return cubeGrid.DisplayName;
        return block != null ? block.ToString() : "NULL";
    }
}
