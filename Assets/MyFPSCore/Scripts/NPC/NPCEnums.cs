using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.NPC
{
    // What is this NPC to the player?
    public enum NPCRole
    {
        Enemy,
        Friendly,
        Neutral
    }

    // What kind of thing is it?
    public enum NPCSpecies
    {
        Human,
        Animal,
        Auto
    }
}
