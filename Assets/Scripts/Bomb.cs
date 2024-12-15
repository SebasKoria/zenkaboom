using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

namespace Mirror.Examples.Chat
{
    public class Bomb : NetworkBehaviour
    {
        [Header("Explosion Distance")]
        public float explosion_distance = 2;

        public GameObject centerExplosion;
        public GameObject sideExplosion;
        public GameObject tailExplosion;

        private void Start()
        {
            if(isServer) Invoke(nameof(DestroyBomb), 3f);
        }

        public void DestroyBomb()
        {
            CheckRay(new Vector2Int((int)transform.position.x, (int)transform.position.y), Vector2Int.up);
            GameObject newCenterExplosion = Instantiate(centerExplosion, transform.position, Quaternion.identity);
            NetworkServer.Spawn(newCenterExplosion);

            NetworkServer.Destroy(gameObject);
        }

        private void CheckRay(Vector2Int position, Vector2Int direction)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(position, direction, explosion_distance);
            Array.Sort(hits, HitSortComparer);

            foreach(RaycastHit2D hit in hits){
                Debug.Log(hit.collider.name);

                if (hit.collider.CompareTag("Wall")) break;
                if (hit.collider.CompareTag("Player") && hit.collider.TryGetComponent(out Player player))
                {
                    player.RPC_Die();
                }
            }
        }

        private int HitSortComparer(RaycastHit2D x, RaycastHit2D y)
        {
            return x.distance > y.distance ? 1 : 0;
        }
    }
}
