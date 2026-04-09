using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GeometryPlayer : MonoBehaviour
{
    [Header("Auto Slide")]
    public float moveSpeed = 6f;
    public float minSpeed = 3f;
    public float maxSpeed = 12f;
    public float speedStep = 1f;

    [Header("Jump")]
    public float tapJumpForce = 8f;
    public float maxChargeJumpForce = 16f;
    public float maxChargeTime = 1.2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Feedback References")]
    public SpriteRenderer playerSprite;
    public Color normalColor = Color.white;
    public Color chargingColor = Color.yellow;

    public TextMeshProUGUI speedText;
    public TextMeshProUGUI actionText;
    public Image chargeBarFill;

    [Header("Spark Prefabs")]
    public GameObject tapSparkPrefab;
    public GameObject swipeSparkPrefab;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isHolding;
    private bool swipeDetected;

    private float holdStartTime;
    private Vector2 touchStartPos;
    private Vector3 startPosition;

    private float tapThreshold = 0.2f;
    private float swipeThreshold = 80f;

    void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        if (chargeBarFill != null)
        {
            chargeBarFill.fillAmount = 0f;
        }

        UpdateSpeedUI();
        SetActionText("Ready");
    }

    void Update()
    {
        CheckGround();
        HandleTouchInput();
        UpdateChargeBar();
    }

    void FixedUpdate()
    {
        AutoSlide();
    }

    void AutoSlide()
    {
        transform.position += Vector3.right * moveSpeed * Time.fixedDeltaTime;
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void HandleTouchInput()
    {
        if (Input.touchCount <= 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            touchStartPos = touch.position;
            holdStartTime = Time.time;
            isHolding = true;
            swipeDetected = false;

            if (isGrounded)
            {
                if (playerSprite != null)
                    playerSprite.color = chargingColor;

                SetActionText("Charging...");
            }
        }
        else if (touch.phase == TouchPhase.Moved)
        {
            float horizontalDistance = touch.position.x - touchStartPos.x;

            if (Mathf.Abs(horizontalDistance) > swipeThreshold)
            {
                swipeDetected = true;
                isHolding = false;

                if (horizontalDistance > 0f)
                {
                    IncreaseSpeed();
                    SpawnSwipeSpark(touch.position, 3f);
                }
                else
                {
                    DecreaseSpeed();
                    SpawnSwipeSpark(touch.position, -3f);
                }

                touchStartPos = touch.position;
            }
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            if (!swipeDetected && isGrounded)
            {
                float holdTime = Time.time - holdStartTime;

                if (holdTime < tapThreshold)
                {
                    TapJump(touch.position);
                }
                else
                {
                    ChargeJump();
                }
            }

            isHolding = false;

            if (playerSprite != null)
                playerSprite.color = normalColor;

            if (chargeBarFill != null)
                chargeBarFill.fillAmount = 0f;
        }
    }

    void TapJump(Vector2 screenPos)
    {
        if (rb != null)
            rb.velocity = new Vector2(rb.velocity.x, 0f);

        rb.AddForce(Vector2.up * tapJumpForce, ForceMode2D.Impulse);
        SetActionText("Tap Jump");

        SpawnTapSpark(screenPos);
    }

    void ChargeJump()
    {
        float holdTime = Time.time - holdStartTime;
        float chargePercent = Mathf.Clamp01(holdTime / maxChargeTime);
        float jumpForce = Mathf.Lerp(tapJumpForce, maxChargeJumpForce, chargePercent);

        if (rb != null)
            rb.velocity = new Vector2(rb.velocity.x, 0f);

        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        SetActionText("Charge Jump");
    }

    void IncreaseSpeed()
    {
        moveSpeed = Mathf.Clamp(moveSpeed + speedStep, minSpeed, maxSpeed);
        UpdateSpeedUI();
        SetActionText("Swipe Right = Faster");
    }

    void DecreaseSpeed()
    {
        moveSpeed = Mathf.Clamp(moveSpeed - speedStep, minSpeed, maxSpeed);
        UpdateSpeedUI();
        SetActionText("Swipe Left = Slower");
    }

    void UpdateChargeBar()
    {
        if (isHolding && isGrounded && chargeBarFill != null)
        {
            float holdTime = Time.time - holdStartTime;
            chargeBarFill.fillAmount = Mathf.Clamp01(holdTime / maxChargeTime);
        }
    }

    void UpdateSpeedUI()
    {
        if (speedText != null)
        {
            speedText.text = "Speed: " + moveSpeed.ToString("F1");
        }
    }

    void SetActionText(string message)
    {
        if (actionText != null)
        {
            actionText.text = message;
        }
    }

    void SpawnTapSpark(Vector2 screenPos)
    {
        if (tapSparkPrefab == null || Camera.main == null)
            return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        Instantiate(tapSparkPrefab, worldPos, Quaternion.identity);
    }

    void SpawnSwipeSpark(Vector2 screenPos, float direction)
    {
        if (swipeSparkPrefab == null || Camera.main == null)
            return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        GameObject spark = Instantiate(swipeSparkPrefab, worldPos, Quaternion.identity);
        TouchSparkEffect effect = spark.GetComponent<TouchSparkEffect>();

        if (effect != null)
        {
            effect.moveSpeed = direction;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
{
    if (collision.gameObject.CompareTag("Spike"))
    {
        ResetLevel();
    }
}

void ResetLevel()
{
    transform.position = startPosition;

    if (rb != null)
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    moveSpeed = 6f;
    UpdateSpeedUI();
    SetActionText("Reset");
}
}