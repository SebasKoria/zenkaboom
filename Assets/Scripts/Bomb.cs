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
        public int explosion_distance = 2;

        public GameObject centerExplosion;
        public GameObject sideExplosion;
        public GameObject tailExplosion;

        private void Start()
        {
            if(isServer) Invoke(nameof(DestroyBomb), 3f);
        }

        public void DestroyBomb()
        {
            GameObject newCenterExplosion = Instantiate(centerExplosion, transform.position, Quaternion.identity);
            NetworkServer.Spawn(newCenterExplosion);

            CheckRay(new Vector2Int((int)transform.position.x, (int)transform.position.y), Vector2Int.up);
            CheckRay(new Vector2Int((int)transform.position.x, (int)transform.position.y), Vector2Int.right);
            CheckRay(new Vector2Int((int)transform.position.x, (int)transform.position.y), Vector2Int.down);
            CheckRay(new Vector2Int((int)transform.position.x, (int)transform.position.y), Vector2Int.left);

            NetworkServer.Destroy(gameObject);
        }

        private void CheckRay(Vector2Int position, Vector2Int direction)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(position, direction, explosion_distance);
            Array.Sort(hits, HitSortComparer);
            int explosionsCount = explosion_distance;

            foreach(RaycastHit2D hit in hits){
                Debug.Log(hit.collider.name);

                if (hit.collider.CompareTag("Wall"))
                {
                    explosionsCount = hit.transform.position.x == transform.position.x ? (int)Mathf.Abs(transform.position.y - hit.transform.position.y) : (int)Mathf.Abs(transform.position.x - hit.transform.position.x);
                    explosionsCount -= 1;
                    break;
                }
                if (hit.collider.CompareTag("Player") && hit.collider.TryGetComponent(out Player player))
                {
                    player.RPC_Die();
                }
            }

            DrawExplosion(explosionsCount, position, direction);
        }

        private void DrawExplosion(int explosionsCount, Vector2Int position, Vector2Int direction)
        {
            float angle;
            if (direction == Vector2Int.up) angle = 180;
            else if (direction == Vector2Int.right) angle = 90;
            else if (direction == Vector2Int.left) angle = -90;
            else angle = 0;

            for(int i = 1; i <= explosionsCount; i++)
            {
                if(i < explosionsCount)
                {
                    GameObject newSideExplosion = Instantiate(sideExplosion, (Vector3Int)(position + (direction * i)), Quaternion.Euler(0,0,angle));
                    NetworkServer.Spawn(newSideExplosion);
                }
                else
                {
                    GameObject newTailExplosion = Instantiate(tailExplosion, (Vector3Int)(position + (direction * i)), Quaternion.Euler(0, 0, angle));
                    NetworkServer.Spawn(newTailExplosion);
                }
            }
        }

        private int HitSortComparer(RaycastHit2D x, RaycastHit2D y)
        {
            return x.distance > y.distance ? 1 : 0;
        }
    }
}
