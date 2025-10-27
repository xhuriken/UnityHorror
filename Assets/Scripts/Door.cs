using DG.Tweening;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    public string InteractMessage => objectInteractMessage;
    public bool cantInteract => _cantInteract;

    [SerializeField] string objectInteractMessage;
    [SerializeField] bool _cantInteract = false;

    [Header("SoundFX")]
    [SerializeField] AudioClip openSFX, closeSFX;
    [SerializeField, Range(0f, 1f)] float openVolume = 1f;
    [SerializeField, Range(0f, 1f)] float closeVolume = 1f;
    [SerializeField] Vector2 openPitchRange = new Vector2(0.98f, 1.04f);
    [SerializeField] Vector2 closePitchRange = new Vector2(0.96f, 1.02f);
    [SerializeField] bool randomizePitch = true;

    private AudioSource _audioSource;

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
        _audioSource = GetComponent<AudioSource>();


        _closedRot = transform.parent.rotation;
        _openRot = _closedRot * Quaternion.Euler(0f, -90f, 0f); // -90°
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

        // SFX
        if (!isOpen)
        {
            //Open
            PlayOneShotVar(openSFX, openVolume, randomizePitch ? openPitchRange : (Vector2?)null);
        }
        else
        {
            //close
            PlayOneShotVar(closeSFX, closeVolume, randomizePitch ? closePitchRange : (Vector2?)null);
        }
    }

    void PlayOneShotVar(AudioClip clip, float volume = 1f, Vector2? pitchRange = null)
    {
        if (_audioSource == null || clip == null) return;

        float oldPitch = _audioSource.pitch;

        if (pitchRange.HasValue)
        {
            var r = pitchRange.Value;
            _audioSource.pitch = Random.Range(r.x, r.y); 
        }

        _audioSource.PlayOneShot(clip, volume);

        _audioSource.pitch = oldPitch; //reset pitch
    }
}
