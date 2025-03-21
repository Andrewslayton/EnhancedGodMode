using MelonLoader;
using EnhancedGodMode;
using Repo_Library;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(EnhancedGod), "EventRepo", "1.0", "Drew and Kel")]
[assembly: MelonGame("semiwork", "REPO")]

namespace EnhancedGodMode
{
    public class EnhancedGod : MelonMod
    {
        private readonly Library Repo_Lib = new Library();
        private bool hasUpgraded = false;
        private bool god = false;
        private bool isNoClip = false;
        private float flySpeed = 10f;
        private bool durability = false;
        public override void OnUpdate()
        {
            if (!Repo_Lib.IsInGame()) return;
            PlayerController playerController = Repo_Lib.GetPlayerController();
            if (Input.GetKeyDown(KeyCode.F4))
            {
                var pla  = Repo_Lib.GetAllPlayers();
                for(int i = 0; i < pla.Count; i++)
                {

                    if (pla[i].GetComponent<PhotonView>().IsMine == true)
                    {
                        var t1 = Repo_Lib.GetPlayerController();
                        MelonLogger.Msg("Player is mine");
                        if (i != 0)
                        {
                            MelonLogger.Msg("Player moved to: " + pla[i - 1].transform.position);
                            Repo_Lib.TeleportPlayer(t1, pla[i - 1].transform.position);
                        }
                        else
                        {
                            MelonLogger.Msg("Player moved to: " + pla[pla.Count - 1].transform.position);
                            Repo_Lib.TeleportPlayer(t1, pla[pla.Count-1].transform.position);
                        }
                    }
                }
            }
            Repo_Lib.GetPlayerCollision().enabled = false;
            if (Repo_Lib.IsSprinting(playerController))
            {
                Repo_Lib.SetSprintEnergyDrain(playerController, 0f);
                Repo_Lib.SetPlayerCurrentEnergy(playerController, Repo_Lib.GetPlayerMaxEnergy(playerController));
            }
            if (!hasUpgraded)
            {
                hasUpgraded = true;
                for (int i = 0; i < 20; i++)
                {
                    Repo_Lib.UpgradePlayerEnergy();
                    Repo_Lib.UpgradePlayerHealth();
                    Repo_Lib.UpgradePlayerJump();
                    Repo_Lib.UpgradePlayerGrabRange();
                    Repo_Lib.UpgradePlayerGrabStrength();
                }
                for (int i = 0; i < 10; i++)
                {
                    Repo_Lib.UpgradePlayerTumbleLaunch();
                    Repo_Lib.UpgradePlayerSprintSpeed();
                }
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                Repo_Lib.SpawnItem(AssetManager.instance.enemyValuableBig);
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (!god)
                {
                    god = true;
                    Repo_Lib.SetGodMode(true);
                    MelonLogger.Msg("god mode enabled");
                }
                else
                {
                    god = false;
                    Repo_Lib.SetGodMode(false);
                    MelonLogger.Msg("god mode disabled");
                }
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                PlayerAvatar playerAvatar = Repo_Lib.GetPlayerAvatar();
                Repo_Lib.HealPlayerMax(playerAvatar.gameObject);
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                if (!durability)
                {
                    Repo_Lib.DisableItemsDurability(true);
                    MelonLogger.Msg("Item Durability: Disabled");
                    durability = true;
                }
                else
                {
                    Repo_Lib.DisableItemsDurability(false);
                    MelonLogger.Msg("Item Durability: Enabled");
                    durability = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleNoClip(playerController);
            }
            if (isNoClip)
            {
                FlyMovement(playerController);
            }
        }
        private void ToggleNoClip(PlayerController playerController)
        {
            isNoClip = !isNoClip;
            if (isNoClip)
            {
                playerController.PlayerCollision.enabled = false;
                playerController.CollisionController.enabled = false;
                playerController.CollisionController.gameObject.SetActive(false);
                playerController.PlayerCollision.gameObject.SetActive(false);
                playerController.CollisionGrounded.enabled = false;
                playerController.rb.isKinematic = true;
                playerController.rb.useGravity = false;
                playerController.rb.detectCollisions = false;
                playerController.rb.velocity = Vector3.zero;
                MelonLogger.Msg("NoClip enabled");
            }
            else
            {
                playerController.PlayerCollision.enabled = true;
                playerController.CollisionController.enabled = true;
                playerController.CollisionController.gameObject.SetActive(true);
                playerController.PlayerCollision.gameObject.SetActive(true);
                playerController.CollisionGrounded.enabled = true;
                playerController.rb.isKinematic = false;
                playerController.rb.useGravity = true;
                playerController.rb.detectCollisions = true;
                MelonLogger.Msg("NoClip disabled");
            }
        }
        private void FlyMovement(PlayerController playerController)
        {
            Rigidbody rb = playerController.GetComponent<Rigidbody>();
            if (rb != null )
            {
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
            }
            
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            float upDown = 0f;
            if (Input.GetKey(KeyCode.Space))
                upDown = 1f;
            if (Input.GetKey(KeyCode.LeftShift))
                upDown = -1f;
            
            bool isMoving = horizontal != 0f || vertical != 0f || upDown != 0f;

            Vector3 movement = isMoving
                ? (playerController.transform.right * horizontal) +
                  (playerController.transform.forward * vertical) +
                  (playerController.transform.up * upDown)
                : Vector3.zero;

            playerController.transform.position += movement * flySpeed * Time.deltaTime;
        }

        private void AttachTeleportationToPlayers()
        {
            var players = Repo_Lib.GetAllPlayers();
            MelonLogger.Msg($"Found {players.Count()} players to attach Teleportation component to");

            foreach (var player in players)
            {
                if (!player.GetComponent<Teleportation>())
                {
                    MelonLogger.Msg($"Adding Teleportation component to player: {player.name}");
                    player.gameObject.AddComponent<Teleportation>();
                }
                else
                {
                    MelonLogger.Msg($"Player already has Teleportation component: {player.name}");
                }
            }
        }
    }

    public class Teleportation : MonoBehaviourPun
    {
        [PunRPC]
        public void Teleport(Vector3 targetPos)
        {
            MelonLogger.Msg("Received teleport request to: " + targetPos);
            transform.position = targetPos;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = targetPos;
                rb.velocity = Vector3.zero;
            }
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.transform.position = targetPos;
                if (pc.rb != null)
                {
                    pc.rb.position = targetPos;
                    pc.rb.velocity = Vector3.zero;
                }
            }

            MelonLogger.Msg("Teleportation complete to: " + targetPos);
        }
    }
}