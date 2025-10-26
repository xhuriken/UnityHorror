using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    public string InteractMessage { get; }
    public void Interact();

    public bool cantInteract { get; }
}