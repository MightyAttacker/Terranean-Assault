using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using NUnit.Framework;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterPathfindingMovementHandler : MonoBehaviour {

    private const float speed = 5f; // Changed to 5f for smoother units
    private int currentPathIndex;
    private List<Vector3> pathVectorList;
    private Rigidbody2D rb;
    [SerializeField] private float maxMoveDistance = 6f;

    public float GetMaxMoveDistance()
    {
        return maxMoveDistance;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        HandleMovement();
    }

private void HandleMovement() {
    if (pathVectorList != null && currentPathIndex < pathVectorList.Count) {
        Vector3 targetPosition = pathVectorList[currentPathIndex];
        Vector3 currentPosition = transform.position;

        Vector3 moveDir = (targetPosition - currentPosition);
        float distanceToTarget = moveDir.magnitude;

        if (distanceToTarget > 0f) {
            moveDir /= distanceToTarget; // normalize

            float moveDistance = speed * Time.deltaTime;

            // Clamp to not overshoot
            float distanceToMove = Mathf.Min(moveDistance, distanceToTarget);

            transform.position += moveDir * distanceToMove;

            // If we've reached or passed the target position, move to next node
            if (distanceToMove >= distanceToTarget) {
                currentPathIndex++;
                if (currentPathIndex >= pathVectorList.Count) {
                    StopMoving();
                }
            }
        } else {
            currentPathIndex++;
            if (currentPathIndex >= pathVectorList.Count) {
                StopMoving();
            }
        }
    }
}


    private void StopMoving() {
        pathVectorList = null;
    }

    public Vector3 GetPosition() {
        return transform.position;
    }

    public void SetTargetPosition(Vector3 targetPosition) {
    currentPathIndex = 0;
    pathVectorList = Pathfinding.Instance.FindPath(GetPosition(), targetPosition);

    if (pathVectorList != null && pathVectorList.Count > 1) {
        pathVectorList.RemoveAt(0); // remove current position node, optional
    }

    // Use the last node in the path as the exact target position
if (pathVectorList != null && pathVectorList.Count > 0) {
    // No need to add targetPosition because path ends there
    // Optionally snap targetPosition to last node for smooth movement
    Vector3 lastNodePos = pathVectorList[pathVectorList.Count - 1];
    pathVectorList[pathVectorList.Count - 1] = lastNodePos;
}

}

}
