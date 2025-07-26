using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public PlayerMovementStats movementStats;
    [SerializeField] private Collider2D _feetCollider;
    [SerializeField] private Collider2D _headCollider;

    private Rigidbody2D _rigidbody;

    private Vector2 _moveVelocity;
    private bool _isFacingRight;

    private RaycastHit2D _groundHit;
    private RaycastHit2D _headHit;
    private bool _isGrounded;
    private bool _bumpedHead;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _isFacingRight = true;
    }

    private void FixedUpdate()
    {
        CollisionChecks();

        if (_isGrounded)
        {
            Move(movementStats.groundAcceleration, movementStats.groundDeceleration, InputManager.movement);
        }
        else
        {
            Move(movementStats.airAcceleration, movementStats.airDeceleration, InputManager.movement);
        }
    }

    private void Move(float acceleration, float deceleration, Vector2 moveInput)
    {
        if (moveInput != Vector2.zero)
        {
            CheckTurn(moveInput);

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

    private void CheckTurn(Vector2 moveInput)
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
            Color rayColor;
            if (_isGrounded)
            {
                rayColor = Color.green;
            }
            else
            {
                rayColor = Color.red;
            }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * movementStats.groundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * movementStats.groundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - movementStats.groundDetectionRayLength), Vector2.right * boxCastSize.x, rayColor);
        }
    }

    private void CollisionChecks()
    {
        IsGrounded();
    }
}
