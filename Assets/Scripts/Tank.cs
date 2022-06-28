using Assets.Scripts.Core;
using System;
using System.Collections;
using UltimateArcade.Frontend;
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

        private UltimateArcadeGameServerAPI serverApi;
        private UltimateArcadeGameClientAPI clientApi;
        private DateTime joinTime;
        private const int maxTimeMs = 5 * 60 * 1000;
        private int shotsFired = 0;
        private string token;

        private void Start()
        {
            if (base.isServer)
            {
                this.serverApi = new UltimateArcadeGameServerAPI();
                AutoConnect.OnServerReady += AutoConnect_OnServerReady;
            }
            else
            {
                AutoConnect.OnClientReady += AutoConnect_OnClientReady;
            }
        }

        private void AutoConnect_OnClientReady(string token)
        {
            this.clientApi = new UltimateArcadeGameClientAPI(token, ExternalScriptBehavior.BaseApiServerName());
            UADebug.Log("got token: " + token);
            InitPlayerCmd(token);
        }

        private void AutoConnect_OnServerReady(string randomSeed)
        {
            UADebug.Log("For any randomness we need to use this seed: " + randomSeed);
            UnityEngine.Random.InitState(randomSeed.GetHashCode());
            // and we would only allow players to join after we setup all randomness
        }

        [Command]
        private void InitPlayerCmd(string token)
        {
            this.joinTime = DateTime.Now;
            StartCoroutine(this.DeathCountdown());
            this.token = token;
            UADebug.Log("Activating player");
            StartCoroutine(serverApi.ActivatePlayer(token,
                pi =>
                {
                    UADebug.Log("player joined: " + pi.DisplayName);
                },
                err =>
                {
                    UADebug.Log("ERROR player join. KICKING PLAYER: " + err);
                    NetworkServer.Destroy(gameObject);
                }));
        }

        private IEnumerator DeathCountdown()
        {
            yield return new WaitForSeconds(maxTimeMs / 1000.0f);
            UADebug.Log("player didn't finish in time");
            yield return serverApi.ReportPlayerScore(this.token, 0,
                () =>
                {
                    UADebug.Log("player score reported");
                    this.ClientGameOver();
                    StartCoroutine(this.serverApi.Shutdown(
                        () => UADebug.Log("Shutdown requested"),
                        err => UADebug.Log("couldn't request shutdown:" + err)
                        )
                    );
                },
                err =>
                {
                    UADebug.Log("ERROR reporting player score. KICKING PLAYER: " + err);
                    this.ClientGameOver();
                    StartCoroutine(this.serverApi.Shutdown(
                        () => UADebug.Log("Shutdown requested"),
                        err => UADebug.Log("couldn't request shutdown:" + err)
                        )
                    );
                });
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
                // score is time left - a bigger score is always better in the arcade
                var score = maxTimeMs - (int)((DateTime.Now - this.joinTime).TotalMilliseconds);
                StartCoroutine(serverApi.ReportPlayerScore(this.token, score,
                    () =>
                    {
                        UADebug.Log("player score reported");
                        this.ClientGameOver();
                        StartCoroutine(this.serverApi.Shutdown(
                            () => UADebug.Log("Shutdown requested"),
                            err => UADebug.Log("couldn't request shutdown:" + err)
                            )
                        );
                    },
                    err =>
                    {
                        UADebug.Log("ERROR reporting player score. KICKING PLAYER: " + err);
                        this.ClientGameOver();
                        StartCoroutine(this.serverApi.Shutdown(
                            () => UADebug.Log("Shutdown requested"),
                            err => UADebug.Log("couldn't request shutdown:" + err)
                            )
                        );
                    }));
            }
        }

        [TargetRpc]
        void ClientGameOver()
        {
            ExternalScriptBehavior.CloseGame();
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
