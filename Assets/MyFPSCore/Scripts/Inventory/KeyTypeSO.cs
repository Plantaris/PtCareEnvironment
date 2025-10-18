using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.Inventory
{
    [CreateAssetMenu(menuName = "MyFPSCore/Key Type", fileName = "KeyType")]
    public class KeyTypeSO : ScriptableObject
    {
        public string id;          // Unique internal name
        public string displayName; // Nice name shown in prompts
    }
}
