using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.World
{
    public enum SurfaceKind { Default, Concrete, Wood, Metal, Grass, Dirt, Carpet }

    // Put this on any collider you want custom sounds for.
    public class SurfaceType : MonoBehaviour
    {
        public SurfaceKind kind = SurfaceKind.Default;
    }
}
