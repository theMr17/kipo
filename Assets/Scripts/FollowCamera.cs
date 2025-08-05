using Unity.Cinemachine;
using UnityEngine;

public class FollowCamera : MonoBehaviour {
    public static FollowCamera Instance {get; private set;}

    private CinemachineCamera _cinemachineCamera;

    private void Awake() {
        Instance = this;

        _cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    public void SetFollowTarget(Transform targetTransform) {
        _cinemachineCamera.Follow = targetTransform;
    }
}
