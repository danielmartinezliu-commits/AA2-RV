using System;
using UnityEngine;

public class Lookat : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (mainCameraTransform != null)
        {
            transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward, mainCameraTransform.rotation * Vector3.up);
        }
    }
}
