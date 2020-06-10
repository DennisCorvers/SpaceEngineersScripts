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
    partial class CentrifugeScript : MyGridProgram
    {
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts

        //NAMING FORMAT IS THE FOLLOWING:   
        //FillMeCentrifuge ICE:STONE   
        //EXAMPLE: FillMeCentrifuge 50000:500000   
        const string CENTRIFUGENAME = "FillMeCentrifuge";
        const string LCDNAME = "CentrifugeLCD";
        const float DefaultIce = 1000;
        const float DefaultStone = 10000;

        private List<Centrifuge> _centrifuges;
        private List<IMyTerminalBlock> _work;
        private List<IMyTerminalBlock> _containers;
        private IMyTextPanel _debugPanel;

        void Print(object message)
        {
            if (_debugPanel != null)
            {
                _debugPanel.WriteText(message + "\n", true);
            }
            else
            {
                Echo(message.ToString());
            }
        }

        public CentrifugeScript()
        {
            _centrifuges = new List<Centrifuge>();
            _work = new List<IMyTerminalBlock>();
            _containers = new List<IMyTerminalBlock>();

            _debugPanel = GridTerminalSystem.GetBlockWithName(LCDNAME) as IMyTextPanel;
            if (_debugPanel != null)
            { _debugPanel.ContentType = ContentType.TEXT_AND_IMAGE; }

            //Add container blocks   
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(_work);
            _containers.AddRange(_work);
            _work.Clear();

            //Add refineries   
            GridTerminalSystem.GetBlocksOfType<IMyRefinery>(_work);
            _containers.AddRange(_work);

            // Find the centrifuges to autofeed   
            List<IMyTerminalBlock> tempList = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(CENTRIFUGENAME, tempList);

            bool isUnique = false;
            foreach (var cent in tempList)
            {
                isUnique = true;
                foreach (var container in _containers)
                { if (cent.EntityId == container.EntityId) { isUnique = false; break; } }

                if (isUnique) { _centrifuges.Add(new Centrifuge(cent)); }
            }

            if (_centrifuges.Count < 1)
            { Print("No Centrifuges found!"); }
        }

        void Main()
        {
            if (_debugPanel != null) { _debugPanel.WriteText("", false); }

            List<MyInventoryItem> items = new List<MyInventoryItem>();

            // Loop through the centrifuges   
            for (int i = 0; i < _centrifuges.Count; i++)
            {
                // Retrieve centrifuge InventoryOwner, Inventory and Items  
                var centrifuge = _centrifuges[i];
                var inventory = centrifuge.GetInventory();
                if (inventory == null) { continue; }

                items.Clear(); inventory.GetItems(items);

                float totalIce = 0;
                float totalGravel = 0;

                for (int j = items.Count - 1; j >= 0; j--)
                {

                    var item = items[j];
                    if (IsItem(item, ItemType.Stone))
                    { totalGravel += (float)item.Amount; continue; }
                    if (IsItem(item, ItemType.Ice))
                    { totalIce += (float)item.Amount; continue; }
                }

                Print("Ice Quota: " + centrifuge.IceQuota);
                Print("Gravel Quota: " + centrifuge.GravelQuota);
                Print("");

                Print("Current Ice: " + totalIce);
                Print("Current Gravel: " + totalGravel);
                Print("");

                float movedIce = MoveItems(centrifuge.IceQuota - totalIce, ItemType.Ice, inventory);
                float movedStone = MoveItems(centrifuge.GravelQuota - totalGravel, ItemType.Stone, inventory);

                Print("Ice Moved: " + movedIce);
                Print("Gravel Moved: " + movedStone);
            }
        }

        private float MoveItems(float neededAmount, ItemType itemType, IMyInventory centrifugeInventory)
        {
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            float totalMoved = 0;

            // Loop through all objects in the "containers" list.   
            // (Cargo containers and refineries)   
            for (int i = 0; i < _containers.Count; i++)
            {
                if (_containers[i].CustomName.Contains("<Excl>")) { continue; }

                // Get container InventoryOwner   
                var invOwner = _containers[i];
                if (invOwner == null) { continue; }
                IMyInventory inventory;

                if (_containers[i] is IMyRefinery)
                { inventory = invOwner.GetInventory(1); }
                else
                { inventory = invOwner.GetInventory(0); }

                // Get the items in the container's inventory  
                items.Clear(); inventory.GetItems(items);

                // Loop through all items in the inventory   
                for (int j = items.Count - 1; j >= 0; j--)
                {
                    bool isIce = IsItem(items[j], itemType) && itemType == ItemType.Ice;
                    bool isGravel = IsItem(items[j], ItemType.Stone) && itemType == ItemType.Stone;

                    int? index = 0;

                    if (isIce) { index = 0; }
                    if (isGravel) { index = 1; }

                    if (isIce || isGravel)
                    {
                        float foundAmount = Math.Min((float)items[j].Amount, neededAmount);

                        if (!inventory.TransferItemTo(centrifugeInventory, j, index, true, (MyFixedPoint)foundAmount))
                        { continue; }
                        //If the amount of ice/gravel found is as large as the amount required, there was at least the required ice/gravel present in an inventory.  
                        //The smallest of the 2 is chosen. If there is more found, the requirements have been met and 0 can be returned.  
                        //If less is found, the amount of ice/gravel found is smaller than the required amount. Therefor it should return the remainder and search the next inventory.  
                        totalMoved += foundAmount;
                        //We moved everything we needed.
                        if (totalMoved >= neededAmount)
                        { return totalMoved; }
                    }
                }
            }
            return totalMoved;
        }

        private bool IsItem(MyInventoryItem item, ItemType itemType)
        {
            if (itemType == ItemType.Stone)
            { return item.Type.SubtypeId == itemType.ToString() && item.Type.GetItemInfo().IsIngot; }

            return item.Type.SubtypeId == itemType.ToString();
        }

        private class Centrifuge
        {
            public float IceQuota { get; }
            public float GravelQuota { get; }
            public IMyTerminalBlock MyCentrifuge { get; }

            public Centrifuge(IMyTerminalBlock myTerminalBlock)
            {
                string[] InventoryQuantitys = myTerminalBlock.CustomName.Replace(CENTRIFUGENAME, "").Trim().Split(':');

                try
                {
                    float ice;
                    if (float.TryParse(InventoryQuantitys[0], out ice))
                    { IceQuota = ice; }
                    else { IceQuota = DefaultIce; }
                }
                catch { IceQuota = DefaultIce; }
                try
                {
                    float gravel;
                    if (float.TryParse(InventoryQuantitys[1], out gravel))
                    { GravelQuota = gravel; }
                    else { GravelQuota = DefaultStone; }
                }
                catch { GravelQuota = DefaultStone; }

                MyCentrifuge = myTerminalBlock;
            }

            public IMyInventory GetInventory()
            { return MyCentrifuge.GetInventory(0); }
        }

        enum ItemType
        {
            Stone,
            Ice
        }
    }
}
