using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    class PowerReader : MyGridProgram
    {
        private const string LCDSCREEN = "";

        private List<IMyBatteryBlock> _batteries;
        private IMyTextPanel _debugScreen;

        private double _lastPower = -1;

        public Program()
        {
            _batteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(_batteries);
            _debugScreen = GridTerminalSystem.GetBlockWithName(LCDSCREEN) as IMyTextPanel;

            if (_debugScreen != null)
            { _debugScreen.ContentType = ContentType.TEXT_AND_IMAGE; }
        }

        void Main()
        {
            ClearScreen();

            double totalPower = 0;
            double maxStored = 0;

            foreach (IMyBatteryBlock battery in _batteries)
            {
                maxStored += battery.MaxStoredPower;
                totalPower += battery.CurrentStoredPower;
            }

            if (_lastPower == -1) { _lastPower = totalPower; }

            Print(string.Format("Total Power: {1}/{0}", DoubleToString(maxStored), DoubleToString(totalPower)));
            Print(string.Format("Fill % {0}", Math.Round(totalPower / maxStored * 100f, 1)));
            Print("");
            Print(string.Format("Power Usage: " + DoubleToString(totalPower - _lastPower)));

            _lastPower = totalPower;
        }

        void Print(object message)
        {
            if (_debugScreen != null)
            {
                _debugScreen.WriteText(message + "\n", true);
            }
            else
            {
                Echo(message.ToString());
            }
        }
        void ClearScreen()
        {
            if (_debugScreen != null)
            {
                _debugScreen.WriteText("", false);
            }
        }

        private string DoubleToString(double value)
        {
            if (value >= 1000000)
            { return Math.Round(value / 1000000d, 2).ToString() + " TW"; }
            if (value >= 1000)
            { return Math.Round(value / 1000d, 2).ToString() + " GW"; }
            return Math.Round(value, 2).ToString() + " MW";
        }
    }
}
