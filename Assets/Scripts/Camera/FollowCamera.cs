using Unity.Cinemachine;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public static FollowCamera Instance { get; private set; }

    private CinemachineCamera _cinemachineCamera;
    private CinemachineCameraOffset _cameraOffset;

    [Header("Camera Bias")]
    public float forwardOffset = 3f;
    public float biasSmoothTime = 0.2f;

    private Transform _targetTransform;
    private bool _isFacingRight = true;

    private Vector3 _currentOffset;
    private Vector3 _targetOffset;
    private Vector3 _offsetVelocity;

    private void Awake()
    {
        Instance = this;
        _cinemachineCamera = GetComponent<CinemachineCamera>();
        _cameraOffset = GetComponent<CinemachineCameraOffset>();
        _currentOffset = Vector3.zero;
        _targetOffset = new Vector3(forwardOffset, 0, 0);
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

    private void LateUpdate()
    {
        UpdateCameraBias();
    }

    private void UpdateCameraBias(bool instant = false)
    {
        if (_cameraOffset == null) return;
        if (instant)
        {
            _currentOffset = _targetOffset;
        }
        else
        {
            _currentOffset = Vector3.SmoothDamp(_currentOffset, _targetOffset, ref _offsetVelocity, biasSmoothTime);
        }
        _cameraOffset.Offset = _currentOffset;
    }
}