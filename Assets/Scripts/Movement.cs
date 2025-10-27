using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

public class Movement : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform playerCamera;
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundMask;

    [Header("Look")]
    [SerializeField, Range(0f, 0.5f)] float mouseSmoothTime = 0.03f;
    [SerializeField] bool cursorLock = true;
    [SerializeField] float mouseSensitivity = 1f;     // souris: "par pixel" (ou par unité)
    [SerializeField] float gamepadSensitivity = 220f; // manette: degrés PAR SECONDE

    [Header("Move")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField, Range(0f, 0.5f)] float moveSmoothTime = 0.3f;
    [SerializeField] float sprintMultiplier = 1.5f;
    [SerializeField] float sprintSlowMultiplier = 0.5f;

    [Header("Stamina")]
    [SerializeField] float staminaMax = 5f;
    [SerializeField] float staminaDrain = 1f;
    [SerializeField] float staminaRegen = 1f;
    [SerializeField, Range(0f, 1f)] float sprintRestartPercent = 0.5f; // 50% by default
    float RestartStamina => staminaMax * sprintRestartPercent;

    [Header("Jump / Gravity")]
    [SerializeField] float jumpHeight = 2.5f;
    [SerializeField] float groundRadius = 0.22f;
    [SerializeField] float gravityScale = 3f;

    [Header("PostFX")]
    [SerializeField] float vignetteFull = 0f;    // au max stamina
    [SerializeField] float vignetteEmpty = 0.6f; // à 0 stamina
    [SerializeField] float vertResFull = 480f;
    [SerializeField] float vertResEmpty = 320f;
    private PostProcessVolume ppVolume; // sur l'enfant cam
    private Vignette vignette;
    private RetroPostProcessEffect retro;

    [Header("Anim")]
    [SerializeField] Animator camAnimator; // animator on camera
    static readonly int ID_IsWalking = Animator.StringToHash("isWalking");
    static readonly int ID_IsRunning = Animator.StringToHash("isRunning");
    static readonly int ID_MaxStam = Animator.StringToHash("maxStam");     // trigger
    static readonly int ID_ExitMaxStam = Animator.StringToHash("exitMaxStam"); // trigger

    [Header("SoundFX")]
    [SerializeField] AudioClip[] footSteps;
    [SerializeField] AudioClip   outOfBreathe, jump;
    private AudioSource _audioSource;

    [Header("Footsteps")]
    [SerializeField] float stepIntervalWalk = 0.50f;
    [SerializeField] float stepIntervalSprint = 0.33f;
    [SerializeField] float minHorizSpeedForStep = 0.15f; // movement threshold
    [SerializeField, Range(0f, 1f)] float stepVolume = 0.9f;
    [SerializeField] Vector2 stepPitchRange = new Vector2(0.95f, 1.05f);


    bool wasFull;   // edge for maxStam
    bool wasReady;  // edge for exitMaxStam

    int _lastStepIndex = -1;
    float _stepTimer = 0f;

    // state
    Rigidbody rb;
    PlayerInput playerInput;
    bool grounded;
    float stamina;

    // look
    float camPitch;
    Vector2 lookInput, lookSmooth, lookVel;

    bool usingPad;

    // move
    Vector2 moveInput, moveSmooth, moveVel;

    // jump
    bool jumpPressed;

    // sprint
    const float EPS = 0.001f;   // tol
    bool isSprinting;           // sprint state
    bool sprintHeld;
    bool mustRelease;     // block until release

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        _audioSource = GetComponent<AudioSource>();
        rb.freezeRotation = true;
        stamina = staminaMax;

        if (!ppVolume && playerCamera)
            ppVolume = playerCamera.GetComponentInChildren<PostProcessVolume>(true);
        if (ppVolume) ppVolume.profile.TryGetSettings(out vignette);

        if (ppVolume && retro == null)
            ppVolume.profile.TryGetSettings(out retro);

        if (cursorLock) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }


        if (!camAnimator && playerCamera)
            camAnimator = playerCamera.GetComponentInChildren<Animator>(true);

        wasFull = true;                    // start full
        wasReady = !mustRelease;            // ready at start
    }

    void OnEnable()
    {
        playerInput.onActionTriggered += OnActionTriggered;
        playerInput.onControlsChanged += OnControlsChanged;
    }

    void OnDisable()
    {
        playerInput.onActionTriggered -= OnActionTriggered;
        playerInput.onControlsChanged -= OnControlsChanged;
    }

    void OnActionTriggered(InputAction.CallbackContext ctx)
    {
        switch (ctx.action.name)
        {
            case "Move":
                moveInput = ctx.ReadValue<Vector2>();
                break;

            case "Look":
                lookInput = ctx.ReadValue<Vector2>();
                usingPad = ctx.control.device is Gamepad;
                break;

            case "Jump":
                if (ctx.performed) jumpPressed = true;
                break;

            case "Sprint":
                if (ctx.performed)
                {
                    sprintHeld = true;

                    if (!isSprinting)
                    {
                        if (!mustRelease) // not blocked
                        {
                            isSprinting = true;
                            Debug.Log("[SPRINT] start");
                        }
                        else
                        {
                            Debug.Log($"[SPRINT] blocked, need {sprintRestartPercent * 100f:0}% (stam {stamina:0.00}/{staminaMax:0.00})");
                        }
                    }
                }
                else if (ctx.canceled)
                {
                    sprintHeld = false;
                    if (isSprinting)
                    {
                        isSprinting = false;
                        Debug.Log("[SPRINT] stop (release)");
                    }
                    // do NOT clear mustRelease here; wait for threshold
                }
                break;
        }
    }

    void OnControlsChanged(PlayerInput input)
    {
        usingPad = input.currentControlScheme != null && input.currentControlScheme.Contains("Gamepad");
    }

    void Update()
    {
        // look
        Vector2 lookFrame = usingPad
            // stick: taux -> deg/s -> deg/frame
            ? lookInput * (gamepadSensitivity * Time.unscaledDeltaTime)
            // souris: déjà delta/frame -> applique juste la sensi
            : lookInput * mouseSensitivity;

        // lissage temporel
        lookSmooth = Vector2.SmoothDamp(lookSmooth, lookFrame, ref lookVel, mouseSmoothTime);

        // applique
        transform.Rotate(Vector3.up, lookSmooth.x, Space.World);
        camPitch = Mathf.Clamp(camPitch - lookSmooth.y, -90f, 90f);
        playerCamera.localEulerAngles = Vector3.right * camPitch;

        // jump
        if (grounded && jumpPressed)
        {
            float vy = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            var v = rb.velocity; v.y = vy; rb.velocity = v;

            //SFX
            PlayOneShotVar(jump, 0.4f, stepPitchRange);
        }
        jumpPressed = false;
    }

    void FixedUpdate()
    {
        // ground
        grounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask, QueryTriggerInteraction.Ignore);

        // move smooth
        Vector2 dir = moveInput;
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        moveSmooth = Vector2.SmoothDamp(moveSmooth, dir, ref moveVel, moveSmoothTime);

        // anim bools
        bool moving = moveSmooth.sqrMagnitude > 0.0001f;
        if (camAnimator)
        {
            camAnimator.SetBool(ID_IsRunning, isSprinting && moving);
            camAnimator.SetBool(ID_IsWalking, !isSprinting && moving);
        }

        // footsteps
        // only when grounded + moving above a small threshold
        float horizSpeed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
        bool shouldStep = grounded && horizSpeed >= minHorizSpeedForStep;

        // Sprint  intervalle court
        // Slow    intervalle de marche * (1 / sprintSlowMultiplier)  -> plus long
        // Marche  intervalle normal
        float currentInterval;
        if (isSprinting)
        {
            currentInterval = stepIntervalSprint;
        }
        else if (mustRelease) //Slow
        {
            // sprintSlowMultiplier < 1, donc on DIVISE pour AGRANDIR l'intervalle
            currentInterval = stepIntervalWalk / Mathf.Max(0.0001f, sprintSlowMultiplier);
        }
        else
        {
            currentInterval = stepIntervalWalk;
        }

        // Gestion du timer
        if (!shouldStep)
        {
            // reset doux pour éviter un "step" instantané à la reprise
            _stepTimer = Mathf.Min(_stepTimer, currentInterval * 0.5f);
        }
        else
        {
            _stepTimer -= Time.fixedDeltaTime;
            if (_stepTimer <= 0f)
            {
                var clip = GetRandomFootstep();
                if (clip != null)
                {
                    // Ajuster le pitch selon l'état (subtil)
                    // sprint  : un peu plus aigu
                    // slow    : un peu plus grave
                    // marche  : neutre
                    float centerMul =
                        isSprinting ? 1.04f :
                        (mustRelease ? 0.96f : 1.00f);

                    var ranged = ScalePitchRange(stepPitchRange, centerMul);

                    // Optionnel: volume un poil plus faible en slow
                    float vol = mustRelease ? stepVolume * 0.9f : stepVolume;

                    PlayOneShotVar(clip, vol, ranged);
                }

                _stepTimer = currentInterval;
            }
        }


        // keep previous stamina for edges
        float prevStamina = stamina;

        // stamina
        if (isSprinting)
        {
            stamina -= staminaDrain * Time.fixedDeltaTime; // drain
            if (stamina <= 0f)
            {
                stamina = 0f;
                isSprinting = false;
                mustRelease = true; // need release + threshold

                if (prevStamina > 0f)
                    camAnimator?.SetTrigger(ID_MaxStam);

                //SFX
                PlayOneShotVar(outOfBreathe, 0.4f, new Vector2(0.95f, 1.0f));

                //Debug.Log("[SPRINT] stop (empty, blocked) -> maxStam trigger");
            }
        }
        else
        {
            stamina += staminaRegen * Time.fixedDeltaTime; // regen
            if (stamina > staminaMax) stamina = staminaMax;
        }

        // ready trigger (exitMaxStam) when crossing threshold upward AND unblocked
        bool crossedReadyUp = prevStamina < RestartStamina - EPS && stamina >= RestartStamina - EPS;
        if (mustRelease && crossedReadyUp)
        {
            mustRelease = false; // unlock
            camAnimator?.SetTrigger(ID_ExitMaxStam);
            //Debug.Log($"[SPRINT] ready TRIGGER (>= {sprintRestartPercent * 100f:0}%)");
        }

        // post fx
        float t = 1f - Stamina01; // 0 full, 1 empty
        if (vignette) vignette.intensity.value = Mathf.Lerp(vignetteFull, vignetteEmpty, t);
        if (retro)
        {
            float vres = Mathf.Lerp(vertResFull, vertResEmpty, t);
            retro.FixedVerticalResolution.value = Mathf.RoundToInt(vres);
        }

        // velocity
        float baseMul = mustRelease ? sprintSlowMultiplier : 1f;      // slow while blocked
        float speedMul = isSprinting ? sprintMultiplier : baseMul;    // sprint > slow > normal

        Vector3 wish = (transform.right * moveSmooth.x + transform.forward * moveSmooth.y) * (moveSpeed * speedMul);
        Vector3 v = rb.velocity; v.x = wish.x; v.z = wish.z; rb.velocity = v;

        // extra gravity
        if (gravityScale > 1f)
        {
            Vector3 extra = Physics.gravity * (gravityScale - 1f);
            rb.AddForce(extra, ForceMode.Acceleration);
        }
    }

    public float Stamina01 => staminaMax > 0f ? Mathf.Clamp01(stamina / staminaMax) : 0f;

    #region Audio Helpers

    Vector2 ScalePitchRange(Vector2 range, float centerMul)
    {
        return new Vector2(range.x * centerMul, range.y * centerMul);
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
        _audioSource.pitch = oldPitch;
    }

    AudioClip GetRandomFootstep()
    {
        if (footSteps == null || footSteps.Length == 0) return null;
        if (footSteps.Length == 1) return footSteps[0];

        int idx;
        do idx = Random.Range(0, footSteps.Length);
        while (idx == _lastStepIndex);

        _lastStepIndex = idx;
        return footSteps[idx];
    }



    #endregion //Audio Helpers

}