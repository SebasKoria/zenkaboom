using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ExplosionDestroyObject : NetworkBehaviour
{
    public void Finish()
    {
        NetworkServer.Destroy(gameObject);
    }
}
