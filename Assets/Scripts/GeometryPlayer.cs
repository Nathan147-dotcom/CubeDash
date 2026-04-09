using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GeometryPlayer : MonoBehaviour
{
    [Header("Auto Slide")]
    public float minSpeed = 3f;
    public float normalSpeed = 6f;
    public float maxSpeed = 12f;
    private int speedLevel = 1; 
    private float moveSpeed;

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
    public TextMeshProUGUI resultText;
    public Image chargeBarFill;

    [Header("Spark Prefabs")]
    public GameObject tapSparkPrefab;
    public GameObject swipeSparkPrefab;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isHolding;
    private bool swipeDetected;
    private bool levelFinished = false;

    private float holdStartTime;
    private Vector2 touchStartPos;
    private Vector3 startPosition;

    private float tapThreshold = 0.2f;
    private float swipeThreshold = 50f;

    private int deathCount = 0;
    private float levelStartTime;

    void Start()
    {
        levelStartTime = Time.time;
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();

        speedLevel = 1;
        moveSpeed = normalSpeed;

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
        if (transform.position.y < -10f)
    {
        ResetLevel();
    }
    }

    void FixedUpdate()
    {
        AutoSlide();
    }

    void AutoSlide()
{
    if (levelFinished)
        return;

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

            SetActionText("Charging");
        }
    }
    else if (touch.phase == TouchPhase.Moved)
    {
        if (swipeDetected)
            return;

        float horizontalDistance = touch.position.x - touchStartPos.x;

        if (Mathf.Abs(horizontalDistance) > swipeThreshold)
        {
            swipeDetected = true;
            isHolding = false;

            if (horizontalDistance > 0f)
            {
                IncreaseSpeed();
                SpawnSwipeSpark(touch.position, 8f);
            }
            else
            {
                DecreaseSpeed();
                SpawnSwipeSpark(touch.position, -8f);
            }
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
    speedLevel = Mathf.Clamp(speedLevel + 1, 0, 2);
    ApplySpeedLevel();
    SetActionText("Right Swipe = Faster");
}

void DecreaseSpeed()
{
    speedLevel = Mathf.Clamp(speedLevel - 1, 0, 2);
    ApplySpeedLevel();
    SetActionText("Left Swipe = Slower");
}

void ApplySpeedLevel()
{
    if (speedLevel == 0)
        moveSpeed = minSpeed;
    else if (speedLevel == 1)
        moveSpeed = normalSpeed;
    else
        moveSpeed = maxSpeed;

    UpdateSpeedUI();
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
        worldPos.z = -1f;

        Instantiate(tapSparkPrefab, worldPos, Quaternion.identity);
    }

    void SpawnSwipeSpark(Vector2 screenPos, float direction)
    {
        if (swipeSparkPrefab == null || Camera.main == null)
            return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = -1f;

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
    deathCount++;
    transform.position = startPosition;

    if (rb != null)
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    speedLevel = 1;
moveSpeed = normalSpeed;
UpdateSpeedUI();
    SetActionText("Reset");
    
}

void OnTriggerEnter2D(Collider2D collision)
{
    if (collision.CompareTag("End"))
    {
        FinishLevel();
    }
}

void FinishLevel()
{
    levelFinished = true;
    moveSpeed = 0f;

    if (rb != null)
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    float totalTime = Time.time - levelStartTime;

    SetActionText("Level Complete!");

    if (resultText != null)
    {
        resultText.text = "Level Complete!\nTime: " + totalTime.ToString("F1") + "s\nDeaths: " + deathCount;
    }
}
}