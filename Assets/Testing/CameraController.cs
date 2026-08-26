using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Transform target;
    [SerializeField]
    private float lerp = 200f;
    [SerializeField]
    private float minLerp = 0.5f;
    [SerializeField]
    private float maxLerp = 200f;
    [SerializeField]
    private float lerpTime = 1f;

    private InputAction jumpAction;

    private void Start()
    {
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        if (jumpAction.WasPerformedThisFrame())
        {
            lerp = minLerp;
        }

        var actualLerp = lerp * Time.deltaTime;
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, target.position, actualLerp),
            Quaternion.Slerp(transform.rotation, target.rotation, actualLerp)
        );

        lerp = Mathf.Min(lerp + (maxLerp / lerpTime) * Time.deltaTime, maxLerp);
    }
}
