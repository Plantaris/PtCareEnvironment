using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.Inventory
{
    public class PlayerInventory : MonoBehaviour
    {
        private readonly HashSet<string> keys = new HashSet<string>();

        public bool HasKey(KeyTypeSO key) => key != null && keys.Contains(key.id);
        public void AddKey(KeyTypeSO key) { if (key != null) keys.Add(key.id); }
    }
}
