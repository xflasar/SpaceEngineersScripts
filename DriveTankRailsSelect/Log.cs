using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        List<IMyTextPanel> myTextPanels = new List<IMyTextPanel>();
        public void EchoToLCD(string text)
        {
            // Append the text and a newline to the logging LCD
            // A nice little C# trick here:
            // - The ?. after _logOutput means "call only if _logOutput is not null".
            //_logOutput?.WriteText("");
            _logOutput?.WriteText($"{text}\n", true);
        }
    }
}
