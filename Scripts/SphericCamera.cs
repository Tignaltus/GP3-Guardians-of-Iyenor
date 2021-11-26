using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

public class SphericCamera : MonoBehaviour
{
    public PlayerController playerC;
    [SerializeField] public Transform target;
    
    [SerializeField] private float offsetFromTarget;
    [SerializeField] private float cameraColliderRadius;
    
    [SerializeField][Range(0.1f, 5f)] private float sensitivity;
    
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private float yTargetOffset = 5;

    [SerializeField] private Camera cameraReference;
    
    private Vector3 newTarget;

    [Space]
    
    [SerializeField] private float targetRotationSpeed = 100f;
    [SerializeField] private float targetRotationOffset = 1f;
    //[SerializeField] private Vector3 heightOffset;
    
    [Space]
    
    [SerializeField] private Transform rigPole;

    private Vector3 origin;
    private Vector3 myDirection;

    private float currentHitDistance;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        cameraReference = FindObjectOfType<Camera>();
    }

    private void LateUpdate()
    {
        PlayerInput();
        CheckSpherecast();
        RotateTowardsTarget();
        RotateTarget();
    }
    
    private void PlayerInput()
    {
        //Place this object at the same position as target
        newTarget = new Vector3(target.position.x, target.position.y + yTargetOffset, target.position.z);
        transform.position = newTarget;

        //If the player is moving the mouse
        if(UnityEngine.Input.GetAxis("Mouse X") != 0)
        {
            SphericMovement(new Vector3(0, Input.GetAxis("Mouse X"), 0));
        }
        
        if(UnityEngine.Input.GetAxis("Mouse Y") != 0)
        {
            SphericMovement(new Vector3(-Input.GetAxis("Mouse Y"), 0, 0));
        }
    }

    private void SphericMovement(Vector3 direction)
    {
        direction *= sensitivity;
        
        //Rotate this object with how the Mouse moves
        Quaternion rotation = Quaternion.Euler(direction.x, direction.y, 0);
        transform.rotation *= rotation;
    }
    
    private void CheckSpherecast()
    {
        origin = newTarget;
        myDirection = -transform.forward;
        
        RaycastHit hit;
        
        if (Physics.SphereCast(origin, cameraColliderRadius, myDirection, out hit, offsetFromTarget, collisionLayer))
        {
            //Debug.Log("Collided with " + hit.collider.gameObject);
            currentHitDistance = hit.distance;
        }
        else
        {
            currentHitDistance = offsetFromTarget;
        }
        
        //Push this object back
        this.transform.position = origin + myDirection * currentHitDistance;
    }

    private void RotateTowardsTarget()
    {
        Vector3 lookDir = newTarget - transform.position;

        transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
        //rigPole.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
    }

    private void RotateTarget()
    {
        if (playerC.IsPlayerAlive())
        {
            //Rotation of character
            Vector3 cameraDirection = new Vector3(transform.forward.x, 0f, transform.forward.z);
            Vector3 playerDirection = new Vector3(target.forward.x, 0f, target.forward.z);
    
            if (Vector3.Angle(cameraDirection, playerDirection) > targetRotationOffset)
            {
                var targetRotation = Quaternion.LookRotation(cameraDirection, transform.up);
                target.rotation = Quaternion.RotateTowards(target.rotation, targetRotation, targetRotationSpeed * Time.deltaTime);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin + myDirection * currentHitDistance, cameraColliderRadius);
    }

    public Camera GetCamera()
    {
        return cameraReference;
    }
}
