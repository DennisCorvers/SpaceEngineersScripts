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
    class AutoFillerScript : MyGridProgram
    {
        private List<CargoContainer> _containers = new List<CargoContainer>();
        private List<IMyTerminalBlock> _processors = new List<IMyTerminalBlock>();
        private IMyTextPanel _lcd;

        //CHANGE THESE TO MATCH YOUR SPECIFIC ITEM
        const float FILLPERCENTAGE = 0.01f;
        const string ITEMNAME = "Ice";

        //CHANGE THESE TO RESEMBLE THE NAME OF YOUR PROCESSING UNITS AND LCD SCREEN
        const string PROCESSORNAME = "Small Cargo Container 4";
        const string LCDNAME = "FillMeLCD";
        const bool USELCD = true;

        private float _volumePerKG = -1f;

        public Program()
        {
            _lcd = GridTerminalSystem.GetBlockWithName(LCDNAME) as IMyTextPanel;
            if (_lcd != null) { InitLCD(_lcd); }

            //Grab all containers in this grid. 
            List<IMyCargoContainer> tempContainers = new List<IMyCargoContainer>();

            GridTerminalSystem.GetBlocksOfType(tempContainers);
            if (tempContainers.Count == 0)
            {
                Print("No containers found!");
            }

            //Grab all processor 
            GridTerminalSystem.SearchBlocksOfName(PROCESSORNAME, _processors);
            if (_processors.Count == 0)
            {
                Print("No processors found!");
            }

            bool isUnique = false;
            foreach (var container in tempContainers)
            {
                isUnique = true;
                foreach (var processor in _processors)
                { if (processor.EntityId == container.EntityId) { isUnique = false; break; } }

                if (isUnique) { _containers.Add(new CargoContainer(container)); }
            }
        }

        void Main()
        {
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            //Loop through all processors. 
            for (int i = 0; i < _processors.Count; i++)
            {
                if (_processors[i].HasInventory == false) { continue; }

                var inventory = _processors[i].GetInventory(0);
                inventory.GetItems(items, null);

                //Convert remaining volume to kg 
                float remainingVolume = (float)(inventory.MaxVolume - (inventory.MaxVolume * (1f - FILLPERCENTAGE)) - inventory.CurrentVolume) * 1000;
                float addedVolume = 0;
                float remainingAmount = 0;

                if (remainingVolume <= 0)
                { Print(string.Format("{0} is currently full!", _processors[i].CustomName)); continue; }
                else
                {
                    if (_volumePerKG < 0)
                    {
                        _volumePerKG = GetMassDensity();
                        if (_volumePerKG < 0)
                        { Print(string.Format("Item with name {0} was not found!", ITEMNAME)); return; }
                    }

                    remainingAmount = remainingVolume / _volumePerKG;

                    Print(string.Format("{0} needs {1} kg of {2}", _processors[i].CustomName, remainingAmount, ITEMNAME));
                    addedVolume = MoveItems(remainingVolume, inventory);
                }

                //Keep adding until inventory is full. 


                remainingVolume -= addedVolume;
                Print("kg left: " + remainingVolume);

                if (remainingVolume > 0) { break; }

                Print("");
            }

        }

        private float MoveItems(float amount, IMyInventory inventory)
        {
            float totalMoved = 0;
            for (int i = 0; i < _containers.Count; i++)
            {
                if (_containers[i].HasInventory == false) { continue; }

                var itemList = _containers[i].GetItems();
                for (int j = 0; j < itemList.Count; j++)
                {
                    if (itemList[j].Type.SubtypeId.ToLower() != ITEMNAME.ToLower()) { continue; }
                    float foundAmount = Math.Min((float)itemList[j].Amount, amount);
                    if (!inventory.TransferItemTo(inventory, j, null, true, (MyFixedPoint)foundAmount))
                    {
                        Print("FAILED TO MOVE");
                        continue;
                    }


                    totalMoved += foundAmount;
                    if (totalMoved >= amount)
                    { return totalMoved; }

                    Print(string.Format("Added {0} kg {1} from ", foundAmount, _containers[i].MyContainer.CustomName));
                }
            }
            return totalMoved;
        }
        private float GetMassDensity()
        {
            float massDensity = -1;

            foreach (var container in _containers)
            {
                if (container.HasInventory == false) { continue; }

                var itemList = container.GetItems();
                foreach (var item in itemList)
                {
                    if (item.Type.SubtypeId.ToLower() != ITEMNAME.ToLower()) { continue; }
                    return item.Type.GetItemInfo().Volume * 1000 / item.Type.GetItemInfo().Mass;
                }
            }
            return massDensity;
        }

        private void Print(object obj)
        {
            if (!USELCD || _lcd == null) { Echo(obj.ToString()); return; }

            //Add to already existing text on screen. 
            _lcd.WriteText(GetTime() + " -> " + obj.ToString() + "\n", true);
        }
        private void InitLCD(IMyTextPanel lcd)
        {
            if (!USELCD) { return; }
            if (lcd == null) { Echo("LCD not found"); return; }

            lcd.ContentType = ContentType.TEXT_AND_IMAGE;
            lcd.WriteText("");
        }
        private string GetTime()
        {
            return DateTime.Now.ToString(" H:mm:ss");
        }

        private class CargoContainer
        {
            public IMyCargoContainer MyContainer;
            public IMyInventory MyInventory
            { get { return MyContainer.GetInventory(0); } }
            public bool HasInventory
            { get { return MyContainer.HasInventory; } }
            private List<MyInventoryItem> _myItemList;

            public CargoContainer(IMyCargoContainer container)
            {
                MyContainer = container;
                _myItemList = new List<MyInventoryItem>();
            }

            public List<MyInventoryItem> GetItems()
            {
                _myItemList.Clear();
                MyInventory.GetItems(_myItemList, null);
                return _myItemList;
            }
        }
    }
}
