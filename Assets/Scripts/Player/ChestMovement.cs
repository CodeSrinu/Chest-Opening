using System.Collections;
using UnityEngine;

public class ChestMovement : MonoBehaviour
{
    [Header("Swim Settings")]
    public float swimSpeed = 8f;
    public float driftSpeed = 1f;
    public float lateralSpeed = 5f;
    public float lateralBounds = 5f;
    public KeyCode chestOpenKey = KeyCode.Space;

    [Header("Visual Feedback")]
    public Transform chestVisual;
    public Vector3 chestOpenScale = new Vector3(1.2f, 1.2f, 0.8f);
    public Vector3 chestClosedScale = Vector3.one;
    public float scaleTransitionSpeed = 5f;

    [Header("Speed Progression")]
    public float maxSpeedMultiplier = 3f;
    public float timeToMaxSpeed = 120f;

    private Rigidbody rb;
    private bool isChestOpen;
    private bool wasChestOpen;
    private float currentSpeedMultiplier = 1f;
    private float elapsedTime;
    private float lateralInput;

    private Animator playerAnimator;

    public float CurrentSpeed => swimSpeed * currentSpeedMultiplier;
    public float SpeedMultiplier => currentSpeedMultiplier;
    public bool IsChestOpen => isChestOpen;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        playerAnimator = GetComponent<Animator>();
    }

    private void Update()
    {
        isChestOpen = Input.GetKey(chestOpenKey);

        if (Input.GetKeyDown(KeyCode.X))
        {
            StartCoroutine(StartDiveAndRiseUp());
        }

        if (isChestOpen) playerAnimator.SetBool("isFast", true);
        else playerAnimator.SetBool("isFast", false);

            lateralInput = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            lateralInput = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            lateralInput = 1f;

        if (isChestOpen && !wasChestOpen)
            AudioManager.Instance?.PlaySwimLoop();
        wasChestOpen = isChestOpen;

        UpdateChestVisual();
    }


    private IEnumerator StartDiveAndRiseUp()
    {
        yield return StartCoroutine(RotateTo(Quaternion.Euler(45f, 0f, 0f), 1f));
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(RotateTo(Quaternion.identity, 1f));
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(RotateTo(Quaternion.Euler(-45f, 0f, 0f), 1f));
        yield return new WaitForSeconds(0.05f);
        yield return StartCoroutine(RotateTo(Quaternion.identity, 1f));
    }

    private IEnumerator RotateTo(Quaternion target, float duration)
    {
        Quaternion start = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Lerp(start, target, elapsed / duration);
            yield return null; 
        }

        transform.rotation = target;
    }


    private void FixedUpdate()
    {
        elapsedTime += Time.fixedDeltaTime;
        currentSpeedMultiplier = Mathf.Lerp(1f, maxSpeedMultiplier, elapsedTime / timeToMaxSpeed);

        float speed = isChestOpen ? CurrentSpeed : driftSpeed;
        Vector3 forwardMovement = transform.forward * speed * Time.fixedDeltaTime;

        Vector3 lateralMovement =
            transform.right * lateralInput * lateralSpeed * Time.fixedDeltaTime;

        Vector3 targetPos = rb.position + forwardMovement + lateralMovement;
        targetPos.x = Mathf.Clamp(targetPos.x, -lateralBounds, lateralBounds);

        rb.MovePosition(targetPos);
    }

    private void UpdateChestVisual()
    {
        if (chestVisual == null)
            return;

        Vector3 targetScale = isChestOpen ? chestOpenScale : chestClosedScale;
        chestVisual.localScale = Vector3.Lerp(
            chestVisual.localScale,
            targetScale,
            scaleTransitionSpeed * Time.deltaTime
        );
    }

    public void ResetSpeed()
    {
        elapsedTime = 0f;
        currentSpeedMultiplier = 1f;
    }
}
