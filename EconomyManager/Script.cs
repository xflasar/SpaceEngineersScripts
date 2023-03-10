// Economy Manager
// Created by xSupeFly
// Discord: xSupeFly#2911
//
///

// Lists
List<IMyCargoContainer> _gridThisContainersList = new List<IMyCargoContainer>();
Dictionary<string, VRage.MyFixedPoint> _itemsList = new Dictionary<string, VRage.MyFixedPoint>();
Dictionary<string, VRage.MyFixedPoint> _itemsEconomyList = new Dictionary<string, VRage.MyFixedPoint>();
List<MySprite> listSprites = new List<MySprite>();
List<string> listPrint = new List<string>();

// Variables
TimeSpan TimeSinceFirstRun = TimeSpan.Zero;
IMyTextPanel lcdPanel;
IEnumerator<bool> _stateMachine;
string data = "";
Vector2 position = new Vector2(0,0);
EconomyPriceEnumValue EconomyPriceEnumV;
IMyTextSurface _drawingSurface;
RectangleF _viewport;

Program(){
    GridTerminalSystem.GetBlocksOfType(_gridThisContainersList);
    lcdPanel = GridTerminalSystem.GetBlockWithName("LCD Economy Manager") as IMyTextPanel;
    _drawingSurface = lcdPanel;
    _viewport = new RectangleF(
        (_drawingSurface.TextureSize - _drawingSurface.SurfaceSize) / 2f,
        _drawingSurface.SurfaceSize
    );
    PrepareTextSurfaceForSprites(_drawingSurface);

    position = new Vector2(256, 0) + _viewport.Position;
    lcdPanel.FontSize = 0.65f;
    lcdPanel.Alignment = TextAlignment.CENTER;

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
        try{

        if(500 > counter){
            counter++;
        }
        else
        {
            counter = 0;
            RunStuffOverTimeS();
            RunStuffOverTimeM();
                
            }
        }catch (Exception e){
            Echo("exception: " + e);
            throw new System.Exception(message: "exit");
        }

        Echo("Performance (Ms): " + Runtime.LastRunTimeMs);
        Echo("Items in itemList: " + _itemsList.Count.ToString());
        Echo("Total cargo containers: " + _gridThisContainersList.Count.ToString());
        Echo("Total items to print: " + listPrint.Count.ToString());
        yield return false;
    }
    yield return true;
}
void RunStuffOverTimeS(){
    FindItemsFromCargoContainers();
}

void RunStuffOverTimeM(){
    //CalculateEconomyOfItems();
    PrintOntoLCD();
}

void FindItemsFromCargoContainers(){
    Echo("FindItems");
    _gridThisContainersList.ForEach(curContainer =>
    {
        var items = new List<MyInventoryItem>();
        curContainer.GetInventory(0).GetItems(items);

        if (items.Count == 0) return;

        items.ForEach(curItem =>
        {
            var itemName = curItem.Type.SubtypeId;
            
            if(curItem.Type.TypeId == "MyObjectBuilder_Ingot" && !itemName.Contains("Ingot"))
            {
                itemName += "Ingot";
            }
            else if(curItem.Type.TypeId == "MyObjectBuilder_Ore" && !itemName.Contains("Ore"))
            {
                itemName += "Ore";
            }
            if(itemName.Contains("ScrapOre") || itemName.Contains("Stone") || itemName.Contains("Datapad") || itemName.Contains("STC_PhotonTorpedoMagazine") || itemName.Contains("STC_QuantumTorpedoMagazine") || itemName.Contains("LatinumOre") || itemName.Contains("LatinumIngot"))
            {
                return;
            }

            if (_itemsList.ContainsKey(itemName))
            {
                _itemsList[itemName] += curItem.Amount;
                CalculateEconomyOfItem(itemName, curItem.Amount);
            }
            else
            {
                _itemsList.Add(itemName, curItem.Amount);
                CalculateEconomyOfItem(itemName, curItem.Amount);
            }
        });
    });
    // Test throw new Exception("Find Items From Cargo");
}
void CalculateEconomyOfItem(string itemName, VRage.MyFixedPoint Amount){
    if(_itemsEconomyList.ContainsKey(itemName)){
        _itemsEconomyList[itemName] = Amount * setEconomyPriceEnumValue(itemName);
    }else{
        _itemsEconomyList.Add(itemName, Amount * setEconomyPriceEnumValue(itemName));
    }
}

void CalculateEconomyOfItems(){
    foreach(KeyValuePair<string, VRage.MyFixedPoint> pair in _itemsList){
        
    }
}

public int GetVisualLength(string str) {
  var length = 0;
  foreach (var c in str) {
    if (Char.IsLetterOrDigit(c)) {
      length++;
    } else {
      length += 2;
    }
  }
  return length;
}

void PrintOntoLCD(){
    data = "";
    double TotalInventoryValue = 0;

    if(_itemsEconomyList.Count == 0) return;

    listPrint.Clear();

    var maxLength = _itemsEconomyList.Keys.ToList().Max(item => GetVisualLength(item));
    var formatingString = $"{{0,-{maxLength}}}-{{1,{maxLength}}}";
    var output = new StringBuilder();

    foreach (KeyValuePair<string, VRage.MyFixedPoint> item in _itemsEconomyList.OrderByDescending(key => (double)key.Value))
    {
        if (item.Value == 0) continue;

        var amountString = $"{Math.Round((double)item.Value):N0}";
        var namePaddingLength = maxLength - GetVisualLength(item.Key) + 1;
        var amountPaddingLength = maxLength - GetVisualLength(amountString) + 1;
        var namePaddingString = new string('-', namePaddingLength);
        var amountPaddingString = new string('-', amountPaddingLength);
        
        data += string.Format(formatingString, item.Key + namePaddingString, amountPaddingString + amountString + "\n");
        
        TotalInventoryValue += Math.Round((double)item.Value);
        
        listPrint.Add(string.Format(formatingString, item.Key + namePaddingString, amountPaddingString + amountString + "\n"));
    }

    data += $"Total Inventory Value = {TotalInventoryValue, 15:N0} \n";
    listPrint.Add($"Total Inventory Value = {TotalInventoryValue,15:N0} \n");

    TextSurfaceProvider(listPrint);
}

private int setEconomyPriceEnumValue(string EconomyPriceEnumValueCode){
    if(!Enum.IsDefined(typeof(EconomyPriceEnumValue), EconomyPriceEnumValueCode)){
        return 0;
    }else{
        return (int)(EconomyPriceEnumValue)Enum.Parse(typeof(EconomyPriceEnumValue), EconomyPriceEnumValueCode);
    }
}

enum EconomyPriceEnumValue
{
    DuraniumGrid = 91,
    TritaniumPlate = 243,
    InteriorPlate = 3,
    Construction = 6,
    Display = 5,
    Computer = 1,
    SteelPlate = 16,
    MetalGrid = 21,
    Thrust = 175,
    LargeTube = 22,
    RadioCommunication = 7,
    IsolinearChip = 645,
    TransparentAluminumPlate = 613,
    BulletproofGlass = 11,
    Medical = 243,
    HackingChip = 6153,
    Motor = 23,
    Superconductor = 151,
    SmallTube = 4,
    Girder = 5,
    Detector = 26,
    ShieldComponent = 2524,
    Reactor = 35,
    DilithiumMatrix = 180,
    BetaGelPack = 0,
    Canvas = 28,
    MatterAntiMatterChamber = 13647,
    TorpedoThruster = 4607,
    TorpedoCasing = 3113,
    Tech2x = 13778,
    Tech4x = 52708,
    Tech8x = 165939,
    GravityGenerator = 1982,
    PowerCell = 11,
    SolarCell = 9,
    AluminumIngot = 47,
    AluminumOre = 36,
    AntiDeuteriumOre = 58,
    CobaltIngot = 4,
    CobaltOre = 10,
    DeuteriumOre = 58,
    DeuteriumIntermixIngot = 1494,
    DilithiumIngot = 49,
    DilithiumOre = 58,
    DuraniumIngot = 22,
    DuraniumOre = 30,
    GoldIngot = 194,
    GoldOre = 16,
    IronIngot = 2,
    IronOre = 8,
    NickelIngot = 4,
    NickelOre = 8,
    PlatinumIngot = 471,
    PlatinumOre = 17,
    SiliconIngot = 2,
    SiliconOre = 7,
    SilverIngot = 13,
    SilverOre = 11,
    TritaniumIngot = 85,
    TritaniumOre = 92,
    UraniumIngot = 252,
    UraniumOre = 19,
    KemociteIngot = 104,
    KemociteOre = 68,
    NeutroniumIngot = 416,
    NeutroniumOre = 150

}

void TextSurfaceProvider(List<string> listPrint){
    // Begin a new frame
    var frame = _drawingSurface.DrawFrame();

    // All sprites must be added to the frame here
    DrawSprites(frame, listPrint);

    // We are done with the frame, send all the sprites to the text panel
    frame.Dispose();
}

int counterPos = 0;
bool checkFirstRun = true;
Vector2 positionOff = new Vector2(0,0);
bool down = true;

public void DrawSprites(MySpriteDrawFrame frame, List<string> listPrint)
{
    
    position = new Vector2(256, positionOff.Y) + _viewport.Position;
    listPrint.ForEach(stringF =>
    {
        var sprite = new MySprite()
        {
            Type = SpriteType.TEXT,
            Data = stringF,
            Position = position,
            RotationOrScale = 0.9f /* 90 % of the font's default size */,
            Color = Color.Red,
            Alignment = TextAlignment.CENTER /* Center the text on the position */,
            FontId = "White"
        };
        // Add the sprite to the frame
        frame.Add(sprite);
        position += new Vector2(0, 20);
    });
    if(listPrint.Count > 51){
        if(counterPos < (listPrint.Count - 51)){
            if(down){
                positionOff -= new Vector2(0, 20);
            }
            else{
                positionOff += new Vector2(0, 20);
            }
            position = new Vector2(256, positionOff.Y) + _viewport.Position;
            counterPos++;
        }
        else{
            counterPos = 0;
            if(positionOff.Y < 0){
                down = false;
            }else{
                down = true;
            }
        }
    }
    else{
        positionOff = new Vector2(0,0);
        position = new Vector2(256,0) + _viewport.Position;
    }
}

// Auto-setup text surface
public void PrepareTextSurfaceForSprites(IMyTextSurface textSurface)
{
    // Set the sprite display mode
    textSurface.ContentType = ContentType.SCRIPT;
    // Make sure no built-in script has been selected
    textSurface.Script = "";
}