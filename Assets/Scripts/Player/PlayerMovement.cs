using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    public static PlayerMovement LocalInstance { get; private set; }

    [Header("References")]
    public PlayerMovementStats movementStats;
    [SerializeField] private Collider2D _feetCollider;
    [SerializeField] private Collider2D _headCollider;
    [SerializeField] private Animator _animator;

    private Rigidbody2D _rigidbody;

    private Vector2 _moveVelocity;
    private bool _isFacingRight;

    private RaycastHit2D _groundHit;
    private RaycastHit2D _headHit;
    private bool _isGrounded;
    private bool _bumpedHead;

    public float verticalVelocity { get; private set; }
    private bool _isJumping;
    private bool _isFastFalling;
    private bool _isFalling;
    private float _fastFallTime;
    private float _fastFallReleaseSpeed;
    private int _numberOfJumpsUsed;

    private float _apexPoint;
    private float _timePastApexThreshold;
    private bool _isPastApexThreshold;

    private float _jumpBufferTime;
    private bool _jumpReleasedDuringBuffer;

    private float _coyoteTimer;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;
            FollowCamera.Instance.SetFollowTarget(transform);
        }

        int playerIndex = 0;
        transform.position = SpawnManager.Instance.GetSpawnPoint(playerIndex).position;
        SpawnManager.Instance.SetSpawnPointOccupancy(playerIndex, true);
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _isFacingRight = true;
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        CollisionChecks();
        Jump();

        if (_isGrounded)
        {
            Move(movementStats.groundAcceleration, movementStats.groundDeceleration, InputManager.movement);
            _animator.SetBool("isMoving", Mathf.Abs(_moveVelocity.x) > 0.1f);
        }
        else
        {
            Move(movementStats.airAcceleration, movementStats.airDeceleration, InputManager.movement);
        }
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }
        
        CountTimers();
        JumpChecks();

        _animator.SetBool("isFalling", _isFalling || _isFastFalling);
    }

    private void JumpChecks()
    {
        // when we press the jump button
        if (InputManager.jumpWasPressed)
        {
            _jumpBufferTime = movementStats.jumpBufferTime;
            _jumpReleasedDuringBuffer = false;
        }

        // when we release the jump button
        if (InputManager.jumpWasReleased)
        {
            if (_jumpBufferTime > 0f)
            {
                _jumpReleasedDuringBuffer = true;
            }

            if (_isJumping && verticalVelocity > 0f)
            {
                if (_isPastApexThreshold)
                {
                    _isPastApexThreshold = false;
                    _isFastFalling = true;
                    _fastFallTime = movementStats.timeforUpwardsCancel;
                    verticalVelocity = 0f;
                }
                else
                {
                    _isFastFalling = true;
                    _fastFallReleaseSpeed = verticalVelocity;
                }
            }
        }

        // initiate jump with jump buffering and coyote time
        if (_jumpBufferTime > 0f && !_isJumping && (_isGrounded || _coyoteTimer > 0f))
        {
            InitiateJump(1);

            _isFalling = false;
            _animator.SetBool("isFalling", false);

            if (_jumpReleasedDuringBuffer)
            {
                _isFastFalling = true;
                _fastFallReleaseSpeed = verticalVelocity;
            }

            _animator.SetTrigger("jump");
        }
        // double jump
        else if (_jumpBufferTime > 0f && _isJumping && _numberOfJumpsUsed < movementStats.numberOfJumpsAllowed)
        {
            InitiateJump(1);
            _isFastFalling = false;

            _animator.SetTrigger("doubleJump");
        }
        // air jump after coyote time lapsed
        else if (_jumpBufferTime > 0f && _isFalling && _numberOfJumpsUsed < movementStats.numberOfJumpsAllowed - 1)
        {
            InitiateJump(2);
            _isFastFalling = false;
        }

        // landed
        if ((_isJumping || _isFalling) && _isGrounded && verticalVelocity <= 0f)
        {
            _isJumping = false;
            _isFalling = false;
            _isFastFalling = false;
            _fastFallTime = 0f;
            _isPastApexThreshold = false;
            _numberOfJumpsUsed = 0;

            verticalVelocity = Physics2D.gravity.y;

            _animator.SetBool("isFalling", false);
        }
    }

    private void InitiateJump(int numberOfJumpsUsed)
    {
        if (!_isJumping)
        {
            _isJumping = true;
        }

        _jumpBufferTime = 0f;
        _numberOfJumpsUsed += numberOfJumpsUsed;
        verticalVelocity = movementStats.initialJumpVelocity;
    }

    private void Jump()
    {
        // apply gravity while jumping
        if (_isJumping)
        {
            // check for head bump
            if (_bumpedHead)
            {
                _isFastFalling = true;
            }

            // gravity on ascending
            if (verticalVelocity >= 0f)
            {
                // apex controls
                _apexPoint = Mathf.InverseLerp(movementStats.initialJumpVelocity, 0f, verticalVelocity);

                if (_apexPoint > movementStats.apexThreshold)
                {
                    if (!_isPastApexThreshold)
                    {
                        _isPastApexThreshold = true;
                        _timePastApexThreshold = 0f;
                    }
                    else
                    {
                        _timePastApexThreshold += Time.fixedDeltaTime;
                        if (_timePastApexThreshold < movementStats.apexHangTime)
                        {
                            verticalVelocity = 0f;
                        }
                        else
                        {
                            verticalVelocity = -0.01f;
                        }
                    }
                }
                else
                {
                    verticalVelocity += movementStats.gravity * Time.fixedDeltaTime;
                    if (_isPastApexThreshold)
                    {
                        _isPastApexThreshold = false;
                    }
                }
            }
            // gravity on descending
            else if (!_isFastFalling)
            {
                verticalVelocity += movementStats.gravity * movementStats.gravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else if (verticalVelocity < 0f)
            {
                if (!_isFalling)
                {
                    _isFalling = true;
                }
            }
        }

        // jump cut
        if (_isFastFalling)
        {
            if (_fastFallTime >= movementStats.timeforUpwardsCancel)
            {
                verticalVelocity += movementStats.gravity * movementStats.gravityOnReleaseMultiplier * Time.fixedDeltaTime;
            }
            else
            {
                verticalVelocity = Mathf.Lerp(_fastFallReleaseSpeed, 0f, _fastFallTime / movementStats.timeforUpwardsCancel);
            }

            _fastFallTime += Time.fixedDeltaTime;
        }

        // normal gravity while falling
        if (!_isGrounded && !_isJumping)
        {
            if (!_isFalling)
            {
                _isFalling = true;
            }

            verticalVelocity += movementStats.gravity * Time.fixedDeltaTime;
        }

        // clamp fall speed
        verticalVelocity = Mathf.Clamp(verticalVelocity, -movementStats.maxFallSpeed, 50f);

        if (_isJumping && !_isGrounded && verticalVelocity < 0f)
        {
            _isFalling = true;
        }

        _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocityX, verticalVelocity);
    }

    private void CountTimers()
    {
        _jumpBufferTime -= Time.deltaTime;

        if (!_isGrounded)
        {
            _coyoteTimer -= Time.deltaTime;
        }
        else
        {
            _coyoteTimer = movementStats.jumpCoyoteTime;
        }
    }

    private void Move(float acceleration, float deceleration, Vector2 moveInput)
    {
        if (moveInput != Vector2.zero)
        {
            TurnChecks(moveInput);

            Vector2 targetVelocity;
            if (InputManager.runIsHeld)
            {
                targetVelocity = new Vector2(moveInput.x, 0f) * movementStats.maxRunSpeed;
            }
            else
            {
                targetVelocity = new Vector2(moveInput.x, 0f) * movementStats.maxWalkSpeed;
            }

            _moveVelocity = Vector2.Lerp(_moveVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            _rigidbody.linearVelocity = new Vector2(_moveVelocity.x, _rigidbody.linearVelocityY);
        }
        else
        {
            _moveVelocity = Vector2.Lerp(_moveVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
            _rigidbody.linearVelocity = new Vector2(_moveVelocity.x, _rigidbody.linearVelocityY);
        }
    }

    private void TurnChecks(Vector2 moveInput)
    {
        if (_isFacingRight && moveInput.x < 0f)
        {
            _isFacingRight = false;
            transform.Rotate(0f, 180f, 0f);
        }
        else if (!_isFacingRight && moveInput.x > 0f)
        {
            _isFacingRight = true;
            transform.Rotate(0f, -180f, 0f);
        }
    }

    private void IsGrounded()
    {
        Vector2 boxCastOrigin = new Vector2(_feetCollider.bounds.center.x, _feetCollider.bounds.min.y);
        Vector2 boxCastSize = new Vector2(_feetCollider.bounds.size.x, movementStats.groundDetectionRayLength);

        _groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, movementStats.groundDetectionRayLength, movementStats.groundLayer);
        _isGrounded = _groundHit.collider != null;

        if (movementStats.debugShowIsGroundedBox)
        {
            Color rayColor = _isGrounded ? Color.red : Color.green;

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * movementStats.groundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * movementStats.groundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - movementStats.groundDetectionRayLength), Vector2.right * boxCastSize.x, rayColor);
        }
    }

    private void HeadBumpCheck()
    {
        Vector2 boxCastOrigin = new Vector2(_headCollider.bounds.center.x, _headCollider.bounds.max.y);
        Vector2 boxCastSize = new Vector2(_headCollider.bounds.size.x * movementStats.headWidth, movementStats.headDetectionRayLength);

        _headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up, movementStats.headDetectionRayLength, movementStats.groundLayer);
        _bumpedHead = _headHit.collider != null;

        if (movementStats.debugShowHeadBumpedBox)
        {
            Color rayColor = _bumpedHead ? Color.red : Color.green;

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.up * movementStats.headDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y), Vector2.up * movementStats.headDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y + movementStats.headDetectionRayLength), Vector2.right * boxCastSize.x, rayColor);
        }
    }
    private void CollisionChecks()
    {
        IsGrounded();
        HeadBumpCheck();
    }

    private void DrawJumpArc(float moveSpeed, Color gizmoColor)
    {
        Vector2 startPosition = new Vector2(_feetCollider.bounds.center.x, _feetCollider.bounds.min.y);
        Vector2 previousPosition = startPosition;

        Vector2 velocity = new Vector2(moveSpeed, movementStats.initialJumpVelocity);
        Gizmos.color = gizmoColor;

        float timeStep = 2 * movementStats.timeTillJumpApex / movementStats.arcResolution;

        for (int i = 0; i < movementStats.visualizationSteps; i++)
        {
            float simulationTime = i * timeStep;
            Vector2 displacement;
            Vector2 drawPoint;

            if (simulationTime < movementStats.timeTillJumpApex) // Ascending
            {
                displacement = velocity * simulationTime + 0.5f * new Vector2(0, movementStats.gravity) * simulationTime * simulationTime;
            }
            else if (simulationTime < movementStats.timeTillJumpApex + movementStats.apexHangTime) // Apex hang time
            {
                float apexTime = movementStats.timeTillJumpApex + 0.5f * movementStats.apexHangTime;
                displacement = velocity * movementStats.timeTillJumpApex + 0.5f * new Vector2(0, movementStats.gravity) * movementStats.timeTillJumpApex * movementStats.timeTillJumpApex;
                displacement += new Vector2(0, movementStats.apexHangTime) * apexTime;
            }
            else // Descending
            {
                float descendTime = simulationTime - (movementStats.timeTillJumpApex + movementStats.apexHangTime);
                displacement = velocity * movementStats.timeTillJumpApex + 0.5f * new Vector2(0, movementStats.gravity) * movementStats.timeTillJumpApex * movementStats.timeTillJumpApex;
                displacement += new Vector2(moveSpeed, 0) * movementStats.apexHangTime; // Horizontal movement during hang time
                displacement += new Vector2(moveSpeed, 0) * descendTime + 0.5f * new Vector2(0, movementStats.gravity) * descendTime * descendTime;
            }

            drawPoint = startPosition + displacement;

            if (movementStats.stopOnCollision)
            {
                RaycastHit2D hit = Physics2D.Raycast(previousPosition, drawPoint - previousPosition, Vector2.Distance(previousPosition, drawPoint), movementStats.groundLayer);
                if (hit.collider != null)
                {
                    // If a hit is detected, stop drawing the arc at the hit point
                    Gizmos.DrawLine(previousPosition, hit.point);
                    break;
                }
            }

            Gizmos.DrawLine(previousPosition, drawPoint);
            previousPosition = drawPoint;
        }
    }

    private void OnDrawGizmos()
    {
        if (movementStats.showWalkJumpArc)
        {
            DrawJumpArc(movementStats.maxWalkSpeed, Color.white);
        }

        if (movementStats.showRunJumpArc)
        {
            DrawJumpArc(movementStats.maxRunSpeed, Color.red);
        }
    }
}
