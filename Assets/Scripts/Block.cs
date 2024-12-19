using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Block : NetworkBehaviour
{
    private NetworkAnimator network_animator;

    public void Start()
    {
        network_animator = GetComponent<NetworkAnimator>();
    }

    public void DestroyAnimation()
    {
        network_animator.SetTrigger("Explode");
    }

    public void DestroyBlock()
    {
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}
