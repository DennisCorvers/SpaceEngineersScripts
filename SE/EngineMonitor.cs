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
    class EngineMonitor : MyGridProgram
    {
        //The maximum amount of charge the batteries can hold before the power producers are turned off.
        private const float UPPERLIMIT = 0.9f;
        //The minimum amount of charge the batteries can hold before the power producers are turned on.
        private const float LOWERLIMIT = 0.2f;


        //Do not alter any code past this point!
        private List<IMyBatteryBlock> m_batteries;
        private List<IMyPowerProducer> m_powerProducers;
        private bool m_isCharging;

        public Program()
        {
            m_batteries = new List<IMyBatteryBlock>();
            m_powerProducers = new List<IMyPowerProducer>();

            GridTerminalSystem.GetBlocksOfType(m_batteries);
            GridTerminalSystem.GetBlocksOfType(m_powerProducers, ValidatePowerProducer);

            Print("Batteries found: " + m_batteries.Count);
            Print("Producers found: " + m_powerProducers.Count);

            TogglePowerProducers(GetPowerPercentage() < UPPERLIMIT);
        }

        void Main()
        {
            float power = GetPowerPercentage();
            Print("Current power: " + (power * 100).ToString("0.0"));

            if (m_isCharging)
            {
                if (power > UPPERLIMIT)
                { TogglePowerProducers(false); }
            }
            else
            {
                if (power < LOWERLIMIT)
                { TogglePowerProducers(true); }
            }
        }

        private float GetPowerPercentage()
        {
            float totalPower = 0;
            float totalCapacity = 0;

            foreach (IMyBatteryBlock battery in m_batteries)
            {
                totalPower += battery.CurrentStoredPower;
                totalCapacity += battery.MaxStoredPower;
            }

            return totalPower / totalCapacity;
        }
        private void TogglePowerProducers(bool isRunning)
        {
            m_isCharging = isRunning;
            if (isRunning)
            {
                Echo("Engines turned on.");
                foreach (var producer in m_powerProducers)
                { producer.ApplyAction("OnOff_On"); }
            }
            else
            {
                Echo("Engines turned off.");
                foreach (var producer in m_powerProducers)
                { producer.ApplyAction("OnOff_Off"); }
            }
        }
        private bool ValidatePowerProducer(IMyPowerProducer powerProducer)
        {
            //Check for the type of power producer required.
            //Reactors, Solar Panels, Hydrogen Engines, etc.
            return powerProducer.BlockDefinition.SubtypeId == "LargeHydrogenEngine";
        }

        private void Print(object message)
        {
            Echo(message.ToString());
        }
    }
}
