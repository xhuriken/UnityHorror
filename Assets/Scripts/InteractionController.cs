using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionController : MonoBehaviour
{
    [SerializeField] Camera playerCamera;

    [SerializeField] TextMeshProUGUI interactionText;

    [SerializeField] float interactionDistance = 3f;

    IInteractable currentTargetedInteractable;
    GameObject lastOutlinedGO;   // last highlighted
    Outline lastOutline;       // cached component

    public void Update()
    {
        UpdateCurrentInteractable();

        UpdateInteractionText();

        CheckForInteractionInput();
    }



    void UpdateCurrentInteractable()
    {
        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        GameObject hitGO = null;
        IInteractable it = null;

        if (Physics.Raycast(ray, out var hit, interactionDistance) && hit.collider != null)
        {
            hitGO = hit.collider.gameObject;
            hit.collider.TryGetComponent(out it);

            // can't interact?
            if (hit.collider.TryGetComponent(out it))
            {
                if (it.cantInteract)
                {
                    return;
                    //Debug.Log("Cant interact with " + hitGO.name);
                }
            }
        }


        // swap highlight if target changed
        if (hitGO != lastOutlinedGO)
        {
            // turn off old
            if (lastOutline) lastOutline.enabled = false;

            // set new
            lastOutlinedGO = hitGO;
            lastOutline = null;
            if (lastOutlinedGO && lastOutlinedGO.TryGetComponent(out Outline o))
            {
                lastOutline = o;
                lastOutline.enabled = true;
            }
        }

        currentTargetedInteractable = it;
    }

    void UpdateInteractionText()
    {
        if (!interactionText) return;

        if (currentTargetedInteractable == null)
        {
            interactionText.text = string.Empty;
            return;
        }

        interactionText.text = currentTargetedInteractable.InteractMessage;
    }


    private void CheckForInteractionInput()
    {
        if(Keyboard.current.fKey.wasPressedThisFrame && currentTargetedInteractable != null)
        {
            currentTargetedInteractable.Interact();
        }
    }
}
