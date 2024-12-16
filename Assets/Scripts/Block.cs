using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Block : NetworkBehaviour
{
    public void DestroyBlock()
    {
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}
