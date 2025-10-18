using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyFPSCore.Interaction
{
    public interface IInteractable
    {
        string PromptText { get; }
        void Interact(GameObject interactorRoot);
    }
}
