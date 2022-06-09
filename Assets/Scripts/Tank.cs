using Assets.Scripts.Core;
using System;
using UltimateArcade.Server;
using UnityEngine;
using UnityEngine.AI;

namespace Mirror.Examples.Tanks
{
    public class Tank : NetworkBehaviour
    {
        [Header("Components")]
        public NavMeshAgent agent;
        public Animator animator;
        public TextMesh healthBar;
        public Transform turret;

        [Header("Movement")]
        public float rotationSpeed = 100;

        [Header("Firing")]
        public KeyCode shootKey = KeyCode.Space;
        public GameObject projectilePrefab;
        public Transform projectileMount;

        [Header("Stats")]
        [SyncVar] public int health = 4;

        private UltimateArcadeGameServerAPI api;
        private DateTime joinTime;
        private int shotsFired = 0;
        private string token;

        private void Start()
        {
            this.api = new UltimateArcadeGameServerAPI();
            InitPlayerCmd(ExternalScriptBehavior.Token());
        }

        [Command]
        private void InitPlayerCmd(string token)
        {
            this.joinTime = DateTime.Now;
            this.token = token;
            StartCoroutine(api.ActivatePlayer(token,
                () => UADebug.Log("player joined"),
                err => UADebug.Log("ERROR player join. TODO KICK PLAYER: " + err)));
        }

        void Update()
        {
            // always update health bar.
            // (SyncVar hook would only update on clients, not on server)
            healthBar.text = new string('-', health);

            // movement for local player
            if (isLocalPlayer)
            {
                // rotate
                float horizontal = Input.GetAxis("Horizontal");
                transform.Rotate(0, horizontal * rotationSpeed * Time.deltaTime, 0);

                // move
                float vertical = Input.GetAxis("Vertical");
                Vector3 forward = transform.TransformDirection(Vector3.forward);
                agent.velocity = forward * Mathf.Max(vertical, 0) * agent.speed;
                animator.SetBool("Moving", agent.velocity != Vector3.zero);

                // shoot
                if (Input.GetKeyDown(shootKey))
                {
                    CmdFire();
                }

                RotateTurret();
            }
        }

        // this is called on the server
        [Command]
        void CmdFire()
        {
            GameObject projectile = Instantiate(projectilePrefab, projectileMount.position, projectileMount.rotation);
            NetworkServer.Spawn(projectile);
            RpcOnFire();
            this.shotsFired++;
            if (this.shotsFired == 5)
            {
                //TODO player should automatically lose when they take longer than the max time

                // score is time left - a bigger score is always better in the arcade
                var maxTime = 5 * 60 * 1000;
                var score = maxTime - (DateTime.Now - this.joinTime).Milliseconds;
                StartCoroutine(api.ReportPlayerScore(this.token, score,
                    () => UADebug.Log("player joined"),
                    err => UADebug.Log("ERROR player join. TODO KICK PLAYER: " + err)));
            }
        }

        // this is called on the tank that fired for all observers
        [ClientRpc]
        void RpcOnFire()
        {
            animator.SetTrigger("Shoot");
        }

        [ServerCallback]
        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Projectile>() != null)
            {
                --health;
                if (health == 0)
                    NetworkServer.Destroy(gameObject);
            }
        }

        void RotateTurret()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                Debug.DrawLine(ray.origin, hit.point);
                Vector3 lookRotation = new Vector3(hit.point.x, turret.transform.position.y, hit.point.z);
                turret.transform.LookAt(lookRotation);
            }
        }
    }
}
