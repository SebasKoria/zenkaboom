using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Mirror.Examples.Chat
{
    public class Player : NetworkBehaviour
    {
        [SyncVar] public string playerName;

        private Rigidbody2D rb;
        private Animator animator;
        private NetworkAnimator nAnimator;
        private float horizontalInput = 0;
        private float verticalInput = 0;

        [SerializeField] private float speed = 1f;

        void Start(){
            transform.position = new Vector2(1f, 1f);

            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
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

        // me tardé un pinche día haciendo esto
        void FixedUpdate(){
            if(!isLocalPlayer) return;

            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");

            RaycastHit2D horizontalRay = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y + 0f), Vector2.right * horizontalInput, 0.501f * Mathf.Abs(horizontalInput));
            RaycastHit2D verticalRay = Physics2D.Raycast(new Vector2(transform.position.x + 0f, transform.position.y), Vector2.up * verticalInput, 0.501f * Mathf.Abs(verticalInput));
            RaycastHit2D diagonalRay = Physics2D.Raycast(transform.position, new(horizontalInput, verticalInput), Mathf.Sqrt(Mathf.Pow(Mathf.Abs(horizontalInput * 0.51f), 2) + Mathf.Pow(Mathf.Abs(verticalInput * 0.51f), 2)));

            Vector2 velocity = new(horizontalInput * speed, verticalInput * speed);
            if (verticalRay) velocity.y = 0;
            if (horizontalRay) velocity.x = 0;

            if (diagonalRay)
            {
                Bounds bounds = diagonalRay.collider.bounds;
                Vector2 hitPoint = diagonalRay.point;

                float distanceToTop = Mathf.Abs(bounds.max.y - hitPoint.y);
                float distanceToBottom = Mathf.Abs(bounds.min.y - hitPoint.y);
                float distanceToRight = Mathf.Abs(bounds.max.x - hitPoint.x);
                float distanceToLeft = Mathf.Abs(bounds.min.x - hitPoint.x);

                float minDistance = Mathf.Min(distanceToTop, distanceToBottom, distanceToRight, distanceToLeft);

                if (minDistance == distanceToTop || minDistance == distanceToBottom) velocity.y = 0;
                else if (minDistance == distanceToLeft || minDistance == distanceToRight) velocity.x = 0;
            }

            
            Vector2 fixedPos = Vector2.zero;
            if(velocity.x == 0 && velocity.y != 0)
            {
                int targetPos = Mathf.RoundToInt(transform.position.x);
                float dif = targetPos - transform.position.x;
                if (dif < 0) fixedPos.x += Mathf.Max(-0.05f, dif);
                else fixedPos.x += Mathf.Min(0.05f, dif);
            }
            if(velocity.y == 0 && velocity.x != 0)
            {
                int targetPos = Mathf.RoundToInt(transform.position.y);
                float dif = targetPos - transform.position.y;
                if (dif < 0) fixedPos.y += Mathf.Max(-0.05f, dif);
                else fixedPos.y += Mathf.Min(0.05f, dif);
            }
            transform.position = new Vector3(transform.position.x + fixedPos.x, transform.position.y + fixedPos.y);
            

            rb.velocity = velocity;

            //animator.SetFloat("horizontal-input", rb.velocity.x > 0 ? 1 : (rb.velocity.x < 0 ? -1 : 0));
            //animator.SetFloat("vertical-input", rb.velocity.y > 0 ? 1 : (rb.velocity.y < 0 ? -1 : 0));

            if (rb.velocity.x > 0) nAnimator.SetTrigger("right");
            //if (rb.velocity.x == 0) animator.SetTrigger("idle");
            else if (rb.velocity.x < 0) nAnimator.SetTrigger("left");

            else if (rb.velocity.y > 0) nAnimator.SetTrigger("up");
            //if (rb.velocity.y == 0) animator.SetTrigger("idle");
            else if (rb.velocity.y < 0) nAnimator.SetTrigger("down");

            else if (rb.velocity.x == 0 && rb.velocity.y == 0) nAnimator.SetTrigger("idle");
        }

        /*
        private void OnDrawGizmos()
        {
            Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y + 0f), new Vector3(transform.position.x + (0.55f * horizontalInput), transform.position.y + 0f));
            Gizmos.DrawLine(new Vector3(transform.position.x + 0f, transform.position.y), new Vector3(transform.position.x + 0f, transform.position.y + (0.55f * verticalInput)));
            Gizmos.DrawLine(transform.position, new(transform.position.x + horizontalInput * 0.5f, transform.position.y + verticalInput * 0.5f));
        }
        */
    }
}
