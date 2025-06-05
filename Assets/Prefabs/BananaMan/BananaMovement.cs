using System;
using Unity.Netcode;
using UnityEngine;

public class BananaMovement : NetworkBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            throw new Exception("No animator component found!");
        }
    }

    void Update()
    {
        { // Make sure only the owner moves their player
            if (Game.singleton.isMulti && !IsOwner)
                return;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        animator.SetFloat("horizontal", h);
        animator.SetFloat("vertical", v);

        // Set the magnitude to determine if and how fast we are moving.
        float magnitude = new Vector2(h, v).magnitude;
        animator.SetFloat("magnitude", magnitude);
    }
}
