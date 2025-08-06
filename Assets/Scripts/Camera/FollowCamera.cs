using Unity.Cinemachine;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public static FollowCamera Instance { get; private set; }

    private CinemachineCamera _cinemachineCamera;
    private CinemachineCameraOffset _cinemachineCameraOffset;
    private CinemachinePositionComposer _cinemachineCameraPositionComposer;

    [Header("Forward Bias")]
    public float forwardOffset = 0.8f;
    public float biasSmoothTime = 0.2f;

    [Header("Vertical Bias")]
    public float verticalFallBias = -0.9f;
    public float fallBiasSmoothTime = 0.1f;
    private float _defaultBiasSmoothTime;

    private Transform _targetTransform;
    private bool _isFacingRight = true;

    private Vector3 _currentOffset;
    private Vector3 _targetOffset;
    private Vector3 _offsetVelocity;
    private Vector3 _defaultDamping = new Vector3(0.75f, 2, 0);

    private Vector3 _currentDamping;
    private Vector3 _targetDamping;
    public float dampingLerpSpeed = 5f;

    private void Awake()
    {
        Instance = this;
        _cinemachineCamera = GetComponent<CinemachineCamera>();
        _cinemachineCameraOffset = GetComponent<CinemachineCameraOffset>();
        _cinemachineCameraPositionComposer = GetComponent<CinemachinePositionComposer>();
        _currentOffset = Vector3.zero;
        _targetOffset = new Vector3(forwardOffset, 0, 0);
        _defaultBiasSmoothTime = biasSmoothTime;
        _currentDamping = _defaultDamping;
        _targetDamping = _defaultDamping;
    }

    public void SetFollowTarget(Transform targetTransform)
    {
        _targetTransform = targetTransform;
        _isFacingRight = true;
        if (_cinemachineCamera != null) _cinemachineCamera.Follow = _targetTransform;
        _targetOffset = new Vector3(forwardOffset, 0, 0);
        _currentOffset = _targetOffset;
        UpdateCameraBias(true); // instant update on spawn
    }

    public void SetBiasOffset(Vector3 offset)
    {
        _targetOffset = offset;
    }

    public void OnPlayerTurn(bool facingRight)
    {
        _isFacingRight = facingRight;
        _targetOffset = new Vector3(_isFacingRight ? forwardOffset : -forwardOffset, 0, 0);
        // Do not call UpdateCameraBias here, let LateUpdate handle smooth transition
    }

    public void OnPlayerFastFall()
    {
        biasSmoothTime = fallBiasSmoothTime;
        _targetDamping = Vector3.zero;
        SetBiasOffset(new Vector3(0, verticalFallBias, 0));
    }

    public void OnPlayerStopFastFall()
    {
        biasSmoothTime = _defaultBiasSmoothTime;
        _targetDamping = _defaultDamping;
        SetBiasOffset(new Vector3(_isFacingRight ? forwardOffset : -forwardOffset, 0, 0));
    }

    private void LateUpdate()
    {
        UpdateCameraBias();
        UpdateCameraDamping();
    }

    private void UpdateCameraBias(bool instant = false)
    {
        if (_cinemachineCameraOffset == null) return;
        if (instant)
        {
            _currentOffset = _targetOffset;
        }
        else
        {
            _currentOffset = Vector3.SmoothDamp(_currentOffset, _targetOffset, ref _offsetVelocity, biasSmoothTime);
        }
        _cinemachineCameraOffset.Offset = _currentOffset;
    }

    private void UpdateCameraDamping()
    {
        if (_cinemachineCameraPositionComposer == null) return;
        _currentDamping = Vector3.Lerp(_currentDamping, _targetDamping, dampingLerpSpeed * Time.deltaTime);
        _cinemachineCameraPositionComposer.Damping = _currentDamping;
    }
}