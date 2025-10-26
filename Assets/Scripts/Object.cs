using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    public string InteractMessage => objectInteractMessage;

    public bool cantInteract => _cantInteract;

    [SerializeField]
    string objectInteractMessage;

    public bool isOpen = false;

    [SerializeField]
    bool _cantInteract = false;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Outline>().enabled = false;   
    }

    void Awake()
    {
        _cantInteract = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Interact()
    {
        _cantInteract = true;
        if (!isOpen)
        {
            transform.GetComponent<BoxCollider>().isTrigger = true;
            transform.parent.DORotate(new Vector3(0, 90, 0), 0.4f, RotateMode.WorldAxisAdd).SetEase(Ease.InOutElastic).OnComplete(() =>
            {
                _cantInteract = false;
                isOpen = !isOpen;

            });
        }
        else
        {
            transform.parent.DORotate(new Vector3(0, -90, 0), 0.4f, RotateMode.WorldAxisAdd).SetEase(Ease.InOutElastic).OnComplete(() =>
            {
                transform.GetComponent<BoxCollider>().isTrigger = false;
                _cantInteract = false;
                isOpen = !isOpen;

            });
        }

        //TODO PLAY SFX !!
    }
}
