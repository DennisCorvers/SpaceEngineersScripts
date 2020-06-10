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
    class GasReader : MyGridProgram
    {
        private const string LCDSCREEN = "";

        private List<GasTank> _oxygenTanks;
        private List<GasTank> _hydrogenTanks;

        private IMyTextPanel _debugScreen;

        public Program()
        {
            _oxygenTanks = new List<GasTank>();
            _hydrogenTanks = new List<GasTank>();

            List<IMyGasTank> _allTanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType(_allTanks);

            foreach (var tank in _allTanks)
            {
                GasTank gasTank = new GasTank(tank);
                if (gasTank.MyGasType == GasType.Hydrogen)
                { _hydrogenTanks.Add(gasTank); }
                else
                { _oxygenTanks.Add(gasTank); }
            }

            Print("Oxygen Tanks: " + _oxygenTanks.Count);
            Print("Hydrogen Tanks: " + _hydrogenTanks.Count);

            _debugScreen = GridTerminalSystem.GetBlockWithName(LCDSCREEN) as IMyTextPanel;

            if (_debugScreen != null)
            { _debugScreen.ContentType = ContentType.TEXT_AND_IMAGE; }
        }

        void Main()
        {
            ClearScreen();
            DisplayTankInfo(_hydrogenTanks, GasType.Hydrogen);
            Print("");
            DisplayTankInfo(_oxygenTanks, GasType.Oxygen);
        }

        void DisplayTankInfo(List<GasTank> tanks, GasType gasType)
        {
            if(tanks == null || tanks.Count == 0)
            {
                Print(string.Format("No {0} tanks on this grid", gasType));
                return;
            }

            double volume = 0, fillratio = 0;

            foreach(var tank in tanks)
            {
                volume += tank.MyGasTank.FilledRatio * tank.MyGasTank.Capacity;
                fillratio += tank.MyGasTank.FilledRatio;
            }

            Print(string.Format("{0}: {1}", gasType, DoubleToString(volume)));
            Print(string.Format("Tanks: {0} Fill: {1}%", tanks.Count, Math.Round(fillratio / tanks.Count * 100, 1)));
        }
        void Print(object message)
        {
            if (_debugScreen != null)
            { _debugScreen.WriteText(message + "\n", true); }
            else
            { Echo(message.ToString()); }
        }
        void ClearScreen()
        {
            if (_debugScreen != null)
            { _debugScreen.WriteText("", false); }
        }

        private string DoubleToString(double value, int decimals = 2)
        {
            if (value >= 1000000000)
            { return Math.Round(value / 1000000000d, decimals).ToString() + " GL"; }
            if (value >= 1000000)
            { return Math.Round(value / 1000000d, decimals).ToString() + " ML"; }
            if (value >= 1000)
            { return Math.Round(value / 1000d, decimals).ToString() + " KL"; }
            return Math.Round(value, decimals).ToString() + " L";
        }

        public class GasTank
        {
            public IMyGasTank MyGasTank;
            public GasType MyGasType;

            public GasTank(IMyGasTank gasTank)
            {
                MyGasTank = gasTank;
                if (!Enum.TryParse(gasTank.Components.Get<MyResourceSourceComponent>()
                    .ResourceTypes[0].SubtypeName, out MyGasType))
                { throw new Exception("Unknown gas type"); }
            }
        }
        public enum GasType
        {
            Hydrogen,
            Oxygen
        }
    }
}
