using UnityEngine;

namespace Mirror.Examples.Chat
{
    public class Player : NetworkBehaviour
    {
        [SyncVar] public string playerName;

        [SerializeField] private GameObject bomb_go;
        [SerializeField] private LayerMask walls_layer;
        [SerializeField] private float speed = 1f;
        [SerializeField] private float timeScale = 1f;

        private Rigidbody2D rb;
        private NetworkAnimator nAnimator;
        private Vector2 previousVelocity;
        private float horizontal_input = 0;
        private float vertical_input = 0;
        private string lastTriggerSent = "idle";

        void Start(){
            Time.timeScale = timeScale;

            transform.position = new Vector2(1f, 1f);
            previousVelocity = Vector2.zero;

            rb = GetComponent<Rigidbody2D>();
            nAnimator = GetComponent<NetworkAnimator>();
        }

        public override void OnStartServer()
        {
            playerName = (string)connectionToClient.authenticationData;
        }

        public override void OnStartLocalPlayer()
        {
            ChatUI.localPlayerName = playerName;
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                Debug.Log("Sent SPAWN BOMB");
                SpawnBomb(transform.position);
            }
        }

        // me tardé un pinche día haciendo esto
        void FixedUpdate(){
            if(!isLocalPlayer) return;

            horizontal_input = Input.GetAxisRaw("Horizontal");
            vertical_input = Input.GetAxisRaw("Vertical");

            Vector2 new_velocity = Get_Velocity();
            rb.velocity = new_velocity;

            Fix_Position(new_velocity);
            Set_Animation_Triggers(new_velocity);

            previousVelocity = new_velocity;

        }

        private Vector2 Get_Velocity()
        {
            RaycastHit2D horizontal_ray = Physics2D.Raycast
                (
                new Vector2(transform.position.x, transform.position.y + 0f),
                Vector2.right * horizontal_input, 0.501f * Mathf.Abs(horizontal_input),
                walls_layer
                );
            
            RaycastHit2D vertical_ray = Physics2D.Raycast
                (
                new Vector2(transform.position.x + 0f, transform.position.y),
                Vector2.up * vertical_input, 0.501f * Mathf.Abs(vertical_input),
                walls_layer
                );

            RaycastHit2D diagonal_ray = Physics2D.Raycast
                (
                transform.position, new(horizontal_input, vertical_input),
                Mathf.Sqrt(Mathf.Pow(Mathf.Abs(horizontal_input * 0.51f), 2) + Mathf.Pow(Mathf.Abs(vertical_input * 0.51f), 2)),
                walls_layer
                );

            Vector2 new_velocity = new(horizontal_input * speed, vertical_input * speed);
            if (vertical_ray.collider != null) new_velocity.y = 0;
            if (horizontal_ray.collider != null) new_velocity.x = 0;

            if (diagonal_ray.collider != null)
            {
                Bounds bounds = diagonal_ray.collider.bounds;
                Vector2 hitPoint = diagonal_ray.point;

                float distanceToTop = Mathf.Abs(bounds.max.y - hitPoint.y);
                float distanceToBottom = Mathf.Abs(bounds.min.y - hitPoint.y);
                float distanceToRight = Mathf.Abs(bounds.max.x - hitPoint.x);
                float distanceToLeft = Mathf.Abs(bounds.min.x - hitPoint.x);

                float minDistance = Mathf.Min(distanceToTop, distanceToBottom, distanceToRight, distanceToLeft);

                if (minDistance == distanceToTop || minDistance == distanceToBottom) new_velocity.y = 0;
                else if (minDistance == distanceToLeft || minDistance == distanceToRight) new_velocity.x = 0;
            }

            return new_velocity;
        }

        private void Fix_Position(Vector2 velocity)
        {
            Vector2 fixedPos = Vector2.zero;
            if (velocity.x == 0 && velocity.y != 0)
            {
                int targetPos = Mathf.RoundToInt(transform.position.x);
                float dif = targetPos - transform.position.x;
                if (dif < 0) fixedPos.x += Mathf.Max(-0.05f, dif);
                else fixedPos.x += Mathf.Min(0.05f, dif);
            }
            if (velocity.y == 0 && velocity.x != 0)
            {
                int targetPos = Mathf.RoundToInt(transform.position.y);
                float dif = targetPos - transform.position.y;
                if (dif < 0) fixedPos.y += Mathf.Max(-0.05f, dif);
                else fixedPos.y += Mathf.Min(0.05f, dif);
            }
            transform.position = new Vector3(transform.position.x + fixedPos.x, transform.position.y + fixedPos.y);
        }

        private void Set_Animation_Triggers(Vector2 velocity)
        {
            if (velocity != previousVelocity || (horizontal_input == 0 && vertical_input == 0))
            {
                if (velocity.x > 0 && lastTriggerSent != "right")
                {
                    nAnimator.SetTrigger("right");
                    lastTriggerSent = "right";
                    //Debug.Log("Sent RIGHT");
                }
                else if (velocity.x < 0 && lastTriggerSent != "left")
                {
                    nAnimator.SetTrigger("left");
                    lastTriggerSent = "left";
                    //Debug.Log("Sent LEFT");
                }
                else if (velocity.y > 0 && lastTriggerSent != "up")
                {
                    nAnimator.SetTrigger("up");
                    lastTriggerSent = "up";
                    //Debug.Log("Sent UP");
                }
                else if (velocity.y < 0 && lastTriggerSent != "down")
                {
                    nAnimator.SetTrigger("down");
                    lastTriggerSent = "down";
                    //Debug.Log("Sent DOWN");
                }
                else if (velocity.x == 0 && velocity.y == 0 && lastTriggerSent != "idle")
                {
                    nAnimator.SetTrigger("idle");
                    lastTriggerSent = "idle";
                    //Debug.Log("Sent IDLE");
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y + 0f), new Vector3(transform.position.x + (0.55f * horizontal_input), transform.position.y + 0f));
            Gizmos.DrawLine(new Vector3(transform.position.x + 0f, transform.position.y), new Vector3(transform.position.x + 0f, transform.position.y + (0.55f * vertical_input)));
            Gizmos.DrawLine(transform.position, new(transform.position.x + horizontal_input * 0.5f, transform.position.y + vertical_input * 0.5f));
        }

        [Command(requiresAuthority = false)]
        public void SpawnBomb(Vector2 player_position)
        {
            Vector2 spawn_position = new(Mathf.RoundToInt(player_position.x), Mathf.RoundToInt(player_position.y));
            GameObject bomb_clone = Instantiate(bomb_go, spawn_position, Quaternion.identity);
            NetworkServer.Spawn(bomb_clone);
        }

        [ClientRpc]
        public void RPC_Die()
        {
            if(isLocalPlayer) Debug.Log("You Died!");
        }
    }
}
