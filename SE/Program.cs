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
    partial class Program : MyGridProgram
    {
        private const string CONTAINERNAME = "TestContainer";

        private CargoContainer _hydrogenContainer;
        private List<IMyGasTank> _hydrogenTanks;

        public Program()
        {
            var container = GridTerminalSystem.GetBlockWithName(CONTAINERNAME) as IMyCargoContainer;
            if (container == null)
            { Print("Unable to find container with specified name!"); }
            else
            { _hydrogenContainer = new CargoContainer(container); }



        }

        void Main()
        {
            if (_hydrogenContainer == null) { return; }

            var items = _hydrogenContainer.GetItems();
            foreach(var item in items)
            {
                Print(item.Type.SubtypeId);
                Print(string.Format("Volume: {0}, Mass: {1}", item.Type.GetItemInfo().Volume, item.Type.GetItemInfo().Mass));
                Print(string.Format("Max Stack: {0}", item.Type.GetItemInfo().MaxStackAmount));
                Print("AMOUNT: " + item.Amount);
            }
        }

        private void Print(object message)
        {
            Echo(message.ToString());
        }

        private class CargoContainer
        {
            public IMyCargoContainer MyContainer;
            public IMyInventory MyInventory
            { get { return MyContainer.GetInventory(0); } }
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
