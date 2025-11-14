using UnityEngine;

public class SimplePlayer2D : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody2D rb;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public SpriteRenderer spriteRenderer;

    [Header("Controls (map via Inspector)")]
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Movement")]
    public float moveSpeed = 7f;
    public float acceleration = 60f;
    public float deceleration = 60f;

    [Header("Jump")]
    public float jumpForce = 13f;
    public float coyoteTime = 0.1f;
    public float jumpBuffer = 0.1f;
    public float jumpCutMultiplier = 0.5f;
    public int extraJumps = 0;

    [Header("Ground Check")]
    public float groundCheckRadius = 0.2f;

    float _xInput;
    float _targetSpeed;
    float _currentSpeed;
    bool _jumpHeld;
    bool _jumpPressed;
    float _lastOnGroundTime;
    float _lastJumpPressedTime;
    int _jumpsLeft;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        if (GetComponent<Collider2D>() == null) gameObject.AddComponent<BoxCollider2D>();

        rb.gravityScale = 3f;
        rb.freezeRotation = true;
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _jumpsLeft = extraJumps;
    }

    void Update()
    {
        // INPUT MAPEJAT DES DE L’INSPECTOR
        _xInput = 0;
        if (Input.GetKey(moveLeftKey)) _xInput = -1;
        if (Input.GetKey(moveRightKey)) _xInput = 1;

        _jumpPressed = Input.GetKeyDown(jumpKey);
        _jumpHeld = Input.GetKey(jumpKey);

        if (IsGrounded()) _lastOnGroundTime = coyoteTime; else _lastOnGroundTime -= Time.deltaTime;
        if (_jumpPressed) _lastJumpPressedTime = jumpBuffer; else _lastJumpPressedTime -= Time.deltaTime;

        TryJump();

        if (spriteRenderer && Mathf.Abs(_xInput) > 0.01f)
            spriteRenderer.flipX = _xInput < 0;
    }

    void FixedUpdate()
    {
        // MOVIMENT HORIZONTAL (accel/decel)
        _targetSpeed = _xInput * moveSpeed;
        float accelRate = (Mathf.Abs(_targetSpeed) > 0.01f) ? acceleration : deceleration;

        _currentSpeed = Mathf.MoveTowards(rb.linearVelocity.x, _targetSpeed, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(_currentSpeed, rb.linearVelocity.y);

        // JUMP CUT: retalla quan deixes anar el salt
        if (!_jumpHeld && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * (1f - jumpCutMultiplier));
        }

        if (IsGrounded() && rb.linearVelocity.y <= 0.01f)
            _jumpsLeft = extraJumps;
    }

    void TryJump()
    {
        bool canCoyote = _lastOnGroundTime > 0f;
        bool hasBufferedJump = _lastJumpPressedTime > 0f;

        if (hasBufferedJump && (canCoyote || _jumpsLeft > 0))
        {
            _lastJumpPressedTime = 0f;
            _lastOnGroundTime = 0f;

            if (!canCoyote && _jumpsLeft > 0) _jumpsLeft--;

            float v = rb.linearVelocity.y;
            if (v < 0f) v = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, v);

            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    bool IsGrounded()
    {
        if (!groundCheck) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
