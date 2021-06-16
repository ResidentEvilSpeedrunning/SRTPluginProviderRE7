using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SRTPluginProviderRE7.Structs
{

    [DebuggerDisplay("{_DebuggerDisplay,nq}")]
    [StructLayout(LayoutKind.Sequential)]
    public class InventoryEntry
    {
        /// <summary>
        /// Debugger display message.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string _DebuggerDisplay
        {
            get
            {
                if (IsItem)
                    return string.Format("[#{0}] Item {1} Quantity {2} SlotCount {3}", SlotPosition, DebugItemName, Quantity, SlotCount);
                if (IsWeapon)
                    return string.Format("[#{0}] Weapon {1} SlotCount {2}", SlotPosition, DebugItemName, SlotCount);
                else
                    return string.Format("[#{0}] Empty Slot", SlotPosition);
                //return string.Format("[#{0}] Item {1} Quantity {2}", SlotPosition, ItemName, Quantity);
            }
        }

        public int SlotPosition { get => _slotPosition; set => _slotPosition = value; }
        internal int _slotPosition;
        public int SlotCount { get; set; }
        public string DebugItemName { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public bool IsItem => ItemName != null && (Items.ItemTypes[ItemName] == Items.ItemType.ITEM || Items.ItemTypes[ItemName] == Items.ItemType.STACKABLE);
        public bool IsWeapon => ItemName != null && Items.ItemTypes[ItemName] == Items.ItemType.WEAPON;
        public bool IsStackable => ItemName != null && Items.ItemTypes[ItemName] == Items.ItemType.STACKABLE;
        public bool IsEmptySlot => !IsItem && !IsWeapon;

        public InventoryEntry()
        {
            this.SlotPosition = -1;
            this.SlotCount = -1;
            this.DebugItemName = null;
            this.ItemName = null;
            this.Quantity = -1;
        }

        public int GetSlotCount(string name)
        {
            return Items.ItemSlots[name];
        }

        public void SetValues(int slotPosition, string name, int quantity)
        {
            this.SlotPosition = slotPosition;
            this.SlotCount = Items.ItemSlots[name];
            this.DebugItemName = Items.ItemDictionary[name];
            this.ItemName = name;
            this.Quantity = quantity;
        }

    }
}
