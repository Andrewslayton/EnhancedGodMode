using MelonLoader;
using EnhancedGodMode;
using Repo_Library;
using UnityEngine;
using Random = UnityEngine.Random;
using System;
using System.Collections.Generic;
using System.Linq;

[assembly: MelonInfo(typeof(EnhancedGod), "EventRepo", "1.0", "semiwork")]
[assembly: MelonGame("semiwork", "REPO")]

namespace EnhancedGodMode
{
    public class EnhancedGod : MelonMod
    {
        private readonly Library Repo_Lib = new Library();
        private bool hasUpgraded = false;
        private bool god = false;
        private bool durability = false;

        public override void OnUpdate()
        {
            if (!Repo_Lib.IsInGame()) return;
            PlayerController playerController = Repo_Lib.GetPlayerController();

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
            if (Input.GetKeyDown(KeyCode.F2))
            {

               Repo_Lib.SpawnItem(AssetManager.instance.enemyValuableBig);
               
            }
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if(!god)
                {
                    god = true;
                    Repo_Lib.SetGodMode(true);
                    MelonLogger.Msg("god mode enabled");
                }
                else
                {
                    god = false;
                    Repo_Lib.SetGodMode(false);
                    MelonLogger.Msg("God mode disabled");
                }
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                PlayerAvatar playerAvatar = Repo_Lib.GetPlayerAvatar();
                Repo_Lib.HealPlayerMax(playerAvatar.gameObject);
            }
            if (Input.GetKeyDown(KeyCode.F4))
            {
                PlayerAvatar playerAvatar = Repo_Lib.GetPlayerAvatar();
                List<PlayerAvatar> values = Repo_Lib.GetAllPlayers();
                foreach (PlayerAvatar playerAv in values)
                {
                    continue;   
                }
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (!durability)
                {
                    Repo_Lib.DisableItemsDurability(true);
                    MelonLogger.Msg("Item Durability: Disabled");
                    durability = true;
                } else
                {
                    Repo_Lib.DisableItemsDurability(false);
                    MelonLogger.Msg("Item Durability: Enabled");
                    durability = false;
                }
            }
        }
    }
}
