using Unity.VisualScripting;
using UnityEngine;

namespace Mirror.Examples.Chat
{
    public class Player : NetworkBehaviour
    {
        [SyncVar]
        public string playerName;

        private Rigidbody2D rb;
        private float speed = 100f;

        void Start(){
            transform.position = Vector3.zero;
            rb = GetComponent<Rigidbody2D>();
        }

        public override void OnStartServer()
        {
            playerName = (string)connectionToClient.authenticationData;
        }

        public override void OnStartLocalPlayer()
        {
            ChatUI.localPlayerName = playerName;
        }

        void Update(){
            if(!isLocalPlayer) return;

            rb.velocity = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized * (speed * Time.deltaTime);
        }
    }
}
