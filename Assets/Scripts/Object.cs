using DG.Tweening;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    public string InteractMessage => objectInteractMessage;
    public bool cantInteract => _cantInteract;

    [SerializeField] string objectInteractMessage;
    [SerializeField] bool _cantInteract = false;

    public bool isOpen = false;

    Tween _tween;

    Quaternion _closedRot;
    Quaternion _openRot;

    void Awake()
    {
        _cantInteract = false;
    }

    void Start()
    {
        GetComponent<Outline>().enabled = false;

        _closedRot = transform.parent.rotation;
        _openRot = _closedRot * Quaternion.Euler(0f, 90f, 0f); // +90°
    }

    public void Interact()
    {
        if (_cantInteract) return;

        _cantInteract = true;
        var col = transform.GetComponent<BoxCollider>();
        col.isTrigger = true;

        if (_tween != null && _tween.IsActive()) _tween.Kill();

        var target = isOpen ? _closedRot : _openRot;

        _tween = transform.parent
            .DORotateQuaternion(target, 0.4f)
            .SetEase(Ease.InOutElastic) 
            .OnComplete(() =>
            {
                isOpen = !isOpen;
                col.isTrigger = false;
                _cantInteract = false;
            });

        // TODO: PLAY SFX
    }
}
