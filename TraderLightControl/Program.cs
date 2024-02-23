using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        List<IMyLightingBlock> l1 = new List<IMyLightingBlock>();
        List<IMyLightingBlock> l2 = new List<IMyLightingBlock>();
        List<IMyLightingBlock> l3 = new List<IMyLightingBlock>();
        List<IMyLightingBlock> l4 = new List<IMyLightingBlock>();
        List<IMyLightingBlock> l5 = new List<IMyLightingBlock>();
        List<IMyLightingBlock> l6 = new List<IMyLightingBlock>();
        List<IMyLightingBlock> l7 = new List<IMyLightingBlock>();
        List<IMyLightingBlock> l8 = new List<IMyLightingBlock>();
        List<IMyLightingBlock> l9 = new List<IMyLightingBlock>();
        List<IMyLightingBlock> l10 = new List<IMyLightingBlock>();
        List<IMyLightingBlock> l11 = new List<IMyLightingBlock>();
        List<IMyLightingBlock> l12 = new List<IMyLightingBlock>();

        List<IMyBlockGroup> lGroupBool = new List<IMyBlockGroup>();
        List<IMyLightingBlock> lights = new List<IMyLightingBlock>();

        IMyShipConnector connector;

        string currentLPhase = "L1";
        int phase = 0;
        int runs = 0;
        bool reversed = false;

        int mode = 0;

        float speed = 1.5f;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            lGroupBool.Add(GridTerminalSystem.GetBlockGroupWithName("L1"));
            lGroupBool.Add(GridTerminalSystem.GetBlockGroupWithName("L2"));
            lGroupBool.Add(GridTerminalSystem.GetBlockGroupWithName("L3"));
            lGroupBool.Add(GridTerminalSystem.GetBlockGroupWithName("L4"));
            lGroupBool.Add(GridTerminalSystem.GetBlockGroupWithName("L5"));
            lGroupBool.Add(GridTerminalSystem.GetBlockGroupWithName("L6"));
            lGroupBool.Add(GridTerminalSystem.GetBlockGroupWithName("L7"));
            lGroupBool.Add(GridTerminalSystem.GetBlockGroupWithName("L8"));
            lGroupBool.Add(GridTerminalSystem.GetBlockGroupWithName("L9"));
            lGroupBool.Add(GridTerminalSystem.GetBlockGroupWithName("L10"));
            lGroupBool.Add(GridTerminalSystem.GetBlockGroupWithName("L11"));
            lGroupBool.Add(GridTerminalSystem.GetBlockGroupWithName("L12"));

            connector = GridTerminalSystem.GetBlockWithName("Connector TRADE") as IMyShipConnector;

            foreach (var lightGroup in lGroupBool)
            {
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                lightGroup.GetBlocks(blocks);

                foreach (var block in blocks)
                {
                    lights.Add(block as IMyLightingBlock);
                }
            }
        }

        int counterLightsColorR = 0;
        int counterLightsColorG = 0;
        int counterLightsColorB = 0;
        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == "Lights:1")
            {
                mode = 1;
            }
            else if (argument == "Lights:2")
            {
                mode = 2;
            }

            if (connector != null)
            {
                if (connector.IsConnected)
                {
                    counterLightsColorR = 0;
                    if (counterLightsColorG > 0) counterLightsColorG--;
                    if (counterLightsColorB < 255) counterLightsColorB++;

                    foreach (var light in lights)
                    {
                        light.SetValue<Color>("Color", new Color(counterLightsColorR, counterLightsColorG, counterLightsColorB, 255));
                    }
                } 
                else
                {
                    counterLightsColorR = 0;
                    if (counterLightsColorG < 100) counterLightsColorG++;
                    if (counterLightsColorG > 100) counterLightsColorG--;
                    if (counterLightsColorB < 255) counterLightsColorB++;

                    foreach (var light in lights)
                    {
                        light.SetValue<Color>("Color", new Color(counterLightsColorR, counterLightsColorG, counterLightsColorB, 255));
                    }
                    
                }
            }

            if (speed > 10)
            {
                speed = 1.5f;
                runs = 0;
                lGroupBool.Reverse();
                reversed = !reversed;
            }
            switch(mode)
            {
                case 0:
                    break;
                case 1:
                    RandomTurnOnThenOff();
                    break;
                case 2:
                    RunLineFR();
                    break;
            }
        }

        void RunLineFR ()
        {
            foreach (var lGroup in lGroupBool)
            {
                // Check conditions
                if (currentLPhase == lGroup.Name)
                {
                    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                    lGroup.GetBlocks(blocks);

                    bool finished = LightsBrightness(blocks);

                    if (finished)
                    {
                        // Get the next key in the dictionary
                        var enumerator = lGroupBool.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current == lGroup && enumerator.MoveNext())
                            {
                                currentLPhase = enumerator.Current.Name;
                                break;
                            }
                        }
                        if (reversed)
                        {

                            if (currentLPhase == "L1" && phase == 0)
                            {
                                Echo(currentLPhase);
                                phase = 1;
                                currentLPhase = "L12";
                                Echo(currentLPhase);
                            }
                            else if (currentLPhase == "L1" && phase == 1)
                            {
                                Echo(currentLPhase);
                                phase = 0;
                                currentLPhase = "L12";
                                Echo(currentLPhase);
                                runs++;
                                if ((runs % 5) == 0) speed++;
                            }
                        }
                        else
                        {

                            if (currentLPhase == "L12" && phase == 0)
                            {
                                Echo(currentLPhase);
                                phase = 1;
                                currentLPhase = "L1";
                                Echo(currentLPhase);
                            }
                            else if (currentLPhase == "L12" && phase == 1)
                            {
                                Echo(currentLPhase);
                                phase = 0;
                                currentLPhase = "L1";
                                Echo(currentLPhase);
                                runs++;
                                if ((runs % 5) == 0) speed++;
                            }
                        }
                    }

                    // You may want to add a break; statement here to exit the loop after processing one group.
                }
            }
        }

        List<IMyLightingBlock> lOnline = new List<IMyLightingBlock>();

        void RandomTurnOnThenOff()
        {
            Random random = new Random();
            int r = random.Next(0, lights.Count());

            for (int i = lOnline.Count - 1; i >= 0; i--)
            {
                var l = lOnline[i];

                bool increased = l.CustomName == "increased";

                if (!increased && l.Intensity < 10f && l.CustomName != "decreasing")
                {
                    l.Intensity += 0.5f;

                    if (l.Intensity == 10f)
                    {
                        l.CustomName = "increased";
                    }
                }
                else
                {
                    float step = 0.5f;

                    bool decreasing = l.CustomName == "decreasing";

                    if (decreasing && l.Intensity > 0.5f)
                    {
                        l.Intensity -= step;
                    }
                    else if (!decreasing)
                    {
                        l.CustomName = "decreasing";
                    }
                    else
                    {
                        l.Intensity = 0.5f;
                        l.CustomName = "Light";
                        lOnline.RemoveAt(i);
                    }
                }
            }

            if (!lOnline.Contains(lights[r]))
            {
                lights[r].Intensity += 1f;
                lOnline.Add(lights[r]);
            }
        }

        bool LightsBrightness(List<IMyTerminalBlock> lights)
        {
            bool finished = false;
            int counter = 0;
            foreach (var light in lights)
            {
                float intensity = light.GetValue<float>("Intensity");
                if (phase == 0)
                {
                    if (intensity > 0.5f)
                    {
                        light.SetValue<float>("Intensity", intensity - speed);
                    }
                    else
                    {
                        counter++;
                    }
                } else if (phase == 1)
                {
                    if (intensity < 10f)
                    {
                        light.SetValue<float>("Intensity", intensity + 5f);
                    }
                    else
                    {
                        counter++;
                    }
                }
            }

            if (counter == lights.Count()) return true;
            return finished;
        }
    }
}
