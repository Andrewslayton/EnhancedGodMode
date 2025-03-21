using MelonLoader;
using EnhancedGodMode;
using Repo_Library;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Reflection;

[assembly: MelonInfo(typeof(EnhancedGod), "EventRepo", "1.0", "Drew and Kel")]
[assembly: MelonGame("semiwork", "REPO")]

namespace EnhancedGodMode
{
    public static class GlobalItemManager
    {
        // Categorized collections for different item types
        private static readonly List<GameObject> _globalItemPrefabs = new List<GameObject>();
        private static readonly Dictionary<string, GameObject> _weaponPrefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, GameObject> _valuablePrefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, GameObject> _utilityPrefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, GameObject> _miscPrefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, GameObject> _registeredPrefabs = new Dictionary<string, GameObject>();

        private static bool _canRegisterItems = true;
        private static bool _prefabsDiscovered = false;

        // Keywords for categorization
        private static readonly string[] weaponKeywords = {
            "gun", "weapon", "knife", "blade", "sword", "pistol", "rifle", "shotgun",
            "item_gun", "firearm", "revolver", "ammo", "shoot", "projectile", "bullet",
            "magazine", "clip", "ranged", "firing", "trigger", "barrel"
        };
        private static readonly string[] valuableKeywords = { "valuable", "treasure", "money", "gold", "silver", "gem" };
        private static readonly string[] utilityKeywords = { "tool", "utility", "med", "health", "ammo", "battery", "key" };

        // Public accessors for the categorized prefabs
        public static IReadOnlyList<GameObject> AllItemPrefabs => _globalItemPrefabs;
        public static IReadOnlyDictionary<string, GameObject> WeaponPrefabs => _weaponPrefabs;
        public static IReadOnlyDictionary<string, GameObject> ValuablePrefabs => _valuablePrefabs;
        public static IReadOnlyDictionary<string, GameObject> UtilityPrefabs => _utilityPrefabs;
        public static IReadOnlyDictionary<string, GameObject> MiscPrefabs => _miscPrefabs;
        public static IReadOnlyDictionary<string, GameObject> RegisteredPrefabs => _registeredPrefabs;

        /// <summary>
        /// Discovers and registers all item prefabs from AssetManager via reflection
        /// </summary>
        public static void DiscoverAllPrefabs()
        {
            if (_prefabsDiscovered)
            {
                MelonLogger.Msg("Prefabs already discovered, skipping...");
                return;
            }

            ClearAllCollections();

            if (AssetManager.instance == null)
            {
                MelonLogger.Error("AssetManager.instance is null. Cannot discover prefabs.");
                return;
            }

            MelonLogger.Msg("Starting prefab discovery from AssetManager...");

            // Get all fields from AssetManager (including private ones)
            FieldInfo[] fields = typeof(AssetManager).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MelonLogger.Msg($"Found {fields.Length} fields in AssetManager");

            foreach (var field in fields)
            {
                ProcessField(field);
            }

            // Log summary of what we found
            LogPrefabSummary();
            _prefabsDiscovered = true;
        }

        /// <summary>
        /// Manually register an item prefab with a specific name
        /// </summary>
        public static bool RegisterItem(string itemName, GameObject prefab)
        {
            if (prefab == null)
            {
                MelonLogger.Error($"Failed to register item. Prefab for {itemName} is null.");
                return false;
            }

            if (!_canRegisterItems)
            {
                MelonLogger.Error($"Failed to register item {itemName}. Registration period has ended.");
                return false;
            }

            if (_registeredPrefabs.ContainsKey(itemName))
            {
                MelonLogger.Error($"Failed to register item {itemName}. An item with this name is already registered.");
                return false;
            }

            // Add to registered prefabs dictionary
            _registeredPrefabs[itemName] = prefab;

            // Also categorize it in our other collections
            CategorizeAndAddPrefab(itemName, prefab);

            MelonLogger.Msg($"Successfully registered item: {itemName}");
            return true;
        }

        /// <summary>
        /// Lock item registration to prevent further changes
        /// </summary>
        public static void LockRegistration()
        {
            _canRegisterItems = false;
            MelonLogger.Msg("Item registration has been locked. No further items can be registered.");
        }

        /// <summary>
        /// Spawn an item by name at a specific position
        /// </summary>
        public static GameObject SpawnItem(string itemName, Vector3 position)
        {
            // Try direct lookup first in registered items
            if (_registeredPrefabs.TryGetValue(itemName, out GameObject prefab))
            {
                return UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            }

            // Try partial name match in registered items
            foreach (var kvp in _registeredPrefabs)
            {
                if (kvp.Key.ToLower().Contains(itemName.ToLower()))
                {
                    return UnityEngine.Object.Instantiate(kvp.Value, position, Quaternion.identity);
                }
            }

            // Try weapons dictionary if not found in registered items
            if (_weaponPrefabs.TryGetValue(itemName, out GameObject weaponPrefab))
            {
                return UnityEngine.Object.Instantiate(weaponPrefab, position, Quaternion.identity);
            }

            // Try partial match in weapons
            foreach (var kvp in _weaponPrefabs)
            {
                if (kvp.Key.ToLower().Contains(itemName.ToLower()))
                {
                    return UnityEngine.Object.Instantiate(kvp.Value, position, Quaternion.identity);
                }
            }

            // Try valuables
            if (_valuablePrefabs.TryGetValue(itemName, out GameObject valuablePrefab))
            {
                return UnityEngine.Object.Instantiate(valuablePrefab, position, Quaternion.identity);
            }

            // Try utilities
            if (_utilityPrefabs.TryGetValue(itemName, out GameObject utilityPrefab))
            {
                return UnityEngine.Object.Instantiate(utilityPrefab, position, Quaternion.identity);
            }

            // Try misc items
            if (_miscPrefabs.TryGetValue(itemName, out GameObject miscPrefab))
            {
                return UnityEngine.Object.Instantiate(miscPrefab, position, Quaternion.identity);
            }

            MelonLogger.Error($"Item not found: {itemName}");
            return null;
        }

        /// <summary>
        /// Spawn an item by name at the player's position
        /// </summary>
        public static GameObject SpawnItemAtPlayer(string itemName, float forwardDistance = 2f)
        {
            PlayerController playerController = UnityEngine.Object.FindObjectOfType<PlayerController>();
            if (playerController == null)
            {
                MelonLogger.Error("Cannot spawn item at player. Player controller not found.");
                return null;
            }

            Vector3 spawnPos = playerController.transform.position +
                               playerController.transform.forward * forwardDistance;

            return SpawnItem(itemName, spawnPos);
        }

        /// <summary>
        /// Spawn a networked item in multiplayer games
        /// </summary>
        public static GameObject SpawnItemNetworked(string itemName, Vector3 position)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                MelonLogger.Error("Only the host can spawn networked items");
                return null;
            }

            // Try to find the item
            GameObject prefab = null;

            if (_registeredPrefabs.TryGetValue(itemName, out GameObject registeredPrefab))
            {
                prefab = registeredPrefab;
            }
            else if (_weaponPrefabs.TryGetValue(itemName, out GameObject weaponPrefab))
            {
                prefab = weaponPrefab;
            }
            else if (_valuablePrefabs.TryGetValue(itemName, out GameObject valuablePrefab))
            {
                prefab = valuablePrefab;
            }
            else if (_utilityPrefabs.TryGetValue(itemName, out GameObject utilityPrefab))
            {
                prefab = utilityPrefab;
            }
            else if (_miscPrefabs.TryGetValue(itemName, out GameObject miscPrefab))
            {
                prefab = miscPrefab;
            }

            if (prefab == null)
            {
                MelonLogger.Error($"Networked item not found: {itemName}");
                return null;
            }

            // This assumes your prefabs are registered with Photon
            // You'll need to adapt this based on how your game registers prefabs with Photon
            try
            {
                string prefabPath = GetPrefabPath(itemName, prefab);
                return PhotonNetwork.Instantiate(prefabPath, position, Quaternion.identity);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to network spawn item {itemName}: {ex.Message}");
                // Fall back to local spawn
                MelonLogger.Msg($"Falling back to local spawn for {itemName}");
                return UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            }
        }

        // Existing methods for backwards compatibility
        public static GameObject SpawnWeapon(string weaponName)
        {
            return SpawnItem(weaponName, Vector3.zero);
        }

        /// <summary>
        /// Log all discovered weapon prefabs
        /// </summary>
        public static void LogAllWeapons()
        {
            if (_weaponPrefabs.Count > 0)
            {
                MelonLogger.Msg($"=== WEAPON PREFABS ({_weaponPrefabs.Count}) ===");
                foreach (var kvp in _weaponPrefabs)
                {
                    MelonLogger.Msg($"- {kvp.Key}");
                }
            }
            else
            {
                MelonLogger.Msg("No weapon prefabs found.");
            }
        }

        /// <summary>
        /// Log all registered prefabs
        /// </summary>
        public static void LogAllRegisteredItems()
        {
            if (_registeredPrefabs.Count > 0)
            {
                MelonLogger.Msg($"=== REGISTERED PREFABS ({_registeredPrefabs.Count}) ===");
                foreach (var kvp in _registeredPrefabs)
                {
                    MelonLogger.Msg($"- {kvp.Key}");
                }
            }
            else
            {
                MelonLogger.Msg("No manually registered prefabs found.");
            }
        }

        //
        // Private helper methods
        //

        private static void ClearAllCollections()
        {
            _globalItemPrefabs.Clear();
            _weaponPrefabs.Clear();
            _valuablePrefabs.Clear();
            _utilityPrefabs.Clear();
            _miscPrefabs.Clear();
            // Don't clear registered prefabs during discovery
        }

        private static void ProcessField(FieldInfo field)
        {
            string fieldName = field.Name.ToLower();
            Type fieldType = field.FieldType;

            try
            {
                // Process GameObject fields
                if (fieldType == typeof(GameObject))
                {
                    ProcessGameObjectField(field, fieldName);
                }
                // Process ItemGun or weapon component fields
                else if (fieldType.Name.Contains("ItemGun") ||
                         fieldType.Name.Contains("Weapon") ||
                         weaponKeywords.Any(kw => fieldType.Name.ToLower().Contains(kw)))
                {
                    ProcessWeaponComponentField(field, fieldName);
                }
                // Process arrays of GameObjects or components
                else if (fieldType.IsArray)
                {
                    ProcessArrayField(field);
                }
                // Process Lists of GameObjects or components
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    ProcessListField(field);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error processing field {field.Name}: {ex.Message}");
            }
        }

        private static void ProcessGameObjectField(FieldInfo field, string fieldName)
        {
            try
            {
                GameObject prefab = (GameObject)field.GetValue(AssetManager.instance);
                if (prefab != null)
                {
                    CategorizeAndAddPrefab(field.Name, prefab);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error processing GameObject field {field.Name}: {ex.Message}");
            }
        }

        private static void ProcessWeaponComponentField(FieldInfo field, string fieldName)
        {
            try
            {
                var component = field.GetValue(AssetManager.instance);
                if (component != null)
                {
                    // Use reflection to get the gameObject
                    PropertyInfo gameObjectProp = component.GetType().GetProperty("gameObject");
                    if (gameObjectProp != null)
                    {
                        GameObject prefab = (GameObject)gameObjectProp.GetValue(component);
                        if (prefab != null)
                        {
                            _globalItemPrefabs.Add(prefab);
                            _weaponPrefabs[field.Name] = prefab;
                            MelonLogger.Msg($"Found weapon via component: {field.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error processing weapon component field {field.Name}: {ex.Message}");
            }
        }

        private static void ProcessArrayField(FieldInfo field)
        {
            try
            {
                var array = field.GetValue(AssetManager.instance) as Array;
                if (array != null && array.Length > 0)
                {
                    Type elementType = field.FieldType.GetElementType();

                    // Handle GameObject[] arrays
                    if (elementType == typeof(GameObject))
                    {
                        for (int i = 0; i < array.Length; i++)
                        {
                            GameObject obj = (GameObject)array.GetValue(i);
                            if (obj != null)
                            {
                                string keyName = $"{field.Name}_{i}";
                                CategorizeAndAddPrefab(keyName, obj);
                            }
                        }
                    }
                    // Handle weapon component arrays
                    else if (elementType.Name.Contains("ItemGun") ||
                             elementType.Name.Contains("Weapon") ||
                             weaponKeywords.Any(kw => elementType.Name.ToLower().Contains(kw)))
                    {
                        for (int i = 0; i < array.Length; i++)
                        {
                            var component = array.GetValue(i);
                            if (component != null)
                            {
                                PropertyInfo gameObjectProp = component.GetType().GetProperty("gameObject");
                                if (gameObjectProp != null)
                                {
                                    GameObject prefab = (GameObject)gameObjectProp.GetValue(component);
                                    if (prefab != null)
                                    {
                                        _globalItemPrefabs.Add(prefab);
                                        _weaponPrefabs[$"{field.Name}_{i}"] = prefab;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error processing array field {field.Name}: {ex.Message}");
            }
        }

        private static void ProcessListField(FieldInfo field)
        {
            try
            {
                var list = field.GetValue(AssetManager.instance);
                if (list != null)
                {
                    Type listType = field.FieldType;
                    Type elementType = listType.GetGenericArguments()[0];

                    // Get Count property via reflection
                    PropertyInfo countProp = listType.GetProperty("Count");
                    int count = (int)countProp.GetValue(list);

                    if (count > 0)
                    {
                        // Get Item indexer via reflection
                        PropertyInfo itemProp = listType.GetProperty("Item");

                        // Handle List<GameObject>
                        if (elementType == typeof(GameObject))
                        {
                            for (int i = 0; i < count; i++)
                            {
                                GameObject obj = (GameObject)itemProp.GetValue(list, new object[] { i });
                                if (obj != null)
                                {
                                    string keyName = $"{field.Name}_{i}";
                                    CategorizeAndAddPrefab(keyName, obj);
                                }
                            }
                        }
                        // Handle List<WeaponComponent>
                        else if (elementType.Name.Contains("ItemGun") ||
                                 elementType.Name.Contains("Weapon") ||
                                 weaponKeywords.Any(kw => elementType.Name.ToLower().Contains(kw)))
                        {
                            for (int i = 0; i < count; i++)
                            {
                                var component = itemProp.GetValue(list, new object[] { i });
                                if (component != null)
                                {
                                    PropertyInfo gameObjectProp = component.GetType().GetProperty("gameObject");
                                    if (gameObjectProp != null)
                                    {
                                        GameObject prefab = (GameObject)gameObjectProp.GetValue(component);
                                        if (prefab != null)
                                        {
                                            _globalItemPrefabs.Add(prefab);
                                            _weaponPrefabs[$"{field.Name}_{i}"] = prefab;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error processing list field {field.Name}: {ex.Message}");
            }
        }

        private static void CategorizeAndAddPrefab(string keyName, GameObject prefab)
        {
            // Store in global list
            if (!_globalItemPrefabs.Contains(prefab))
            {
                _globalItemPrefabs.Add(prefab);
            }

            string lowerName = keyName.ToLower();

            // Categorize based on name keywords
            if (weaponKeywords.Any(kw => lowerName.Contains(kw)))
            {
                if (!_weaponPrefabs.ContainsKey(keyName))
                {
                    _weaponPrefabs[keyName] = prefab;
                }
            }
            else if (valuableKeywords.Any(kw => lowerName.Contains(kw)))
            {
                if (!_valuablePrefabs.ContainsKey(keyName))
                {
                    _valuablePrefabs[keyName] = prefab;
                }
            }
            else if (utilityKeywords.Any(kw => lowerName.Contains(kw)))
            {
                if (!_utilityPrefabs.ContainsKey(keyName))
                {
                    _utilityPrefabs[keyName] = prefab;
                }
            }
            else
            {
                if (!_miscPrefabs.ContainsKey(keyName))
                {
                    _miscPrefabs[keyName] = prefab;
                }
            }

            // Also check if it contains weapon components
            CheckForWeaponComponents(prefab, keyName);
        }

        private static void CheckForWeaponComponents(GameObject prefab, string keyName)
        {
            // Check if this GameObject has any weapon-related components
            Component[] components = prefab.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component != null)
                {
                    string componentName = component.GetType().Name.ToLower();
                    if (weaponKeywords.Any(kw => componentName.Contains(kw)))
                    {
                        if (!_weaponPrefabs.ContainsKey(keyName))
                        {
                            _weaponPrefabs[keyName] = prefab;
                            MelonLogger.Msg($"Found weapon via component inspection: {keyName} (Component: {component.GetType().Name})");
                        }
                        break;
                    }
                }
            }
        }

        private static string GetPrefabPath(string itemName, GameObject prefab)
        {
            return $"Items/{prefab.name}";
        }

        private static void LogPrefabSummary()
        {
            MelonLogger.Msg($"=== PREFAB REGISTRATION SUMMARY ===");
            MelonLogger.Msg($"Total Prefabs: {_globalItemPrefabs.Count}");
            MelonLogger.Msg($"Weapons: {_weaponPrefabs.Count}");
            MelonLogger.Msg($"Valuables: {_valuablePrefabs.Count}");
            MelonLogger.Msg($"Utilities: {_utilityPrefabs.Count}");
            MelonLogger.Msg($"Misc Items: {_miscPrefabs.Count}");
        }
    }
}

public class EnhancedGod : MelonMod
    {
        private readonly Library Repo_Lib = new Library();
        private bool hasUpgraded = false;
        private bool god = false;
        private bool isNoClip = false;
        private float flySpeed = 10f;
        private bool durability = false;
        private bool wasInGameLastFrame = false;
        private bool globalItemsRegistered = false;
        private bool globalEnemiesRegistered = false;

        private bool prefabsRegistered = false;

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"=== OnSceneWasInitialized: {sceneName} (build index: {buildIndex}) ===");

            // Register all prefabs
            if (!prefabsRegistered && AssetManager.instance != null)
            {
            GlobalItemManager.DiscoverAllPrefabs();
            GlobalItemManager.LogAllWeapons();
                prefabsRegistered = true;
            }
        }
        public override void OnLateUpdate()
        {
            // Try to register prefabs if it failed earlier
            if (!prefabsRegistered && AssetManager.instance != null)
            {
                MelonLogger.Msg("Late registering prefabs...");
            GlobalItemManager.DiscoverAllPrefabs();
            GlobalItemManager.LogAllWeapons();
                prefabsRegistered = true;
            }
        }
        public override void OnUpdate()
        {
            bool inGame = Repo_Lib.IsInGame();

            if (inGame && !wasInGameLastFrame)
            {
                hasUpgraded = false;
            }
            AssetManager assetManager = AssetManager.instance;
        wasInGameLastFrame = inGame;

            if (!inGame)
                return;

            PlayerController playerController = Repo_Lib.GetPlayerController();

            if (playerController == null)
            {
                return;
            }

            Repo_Lib.GetPlayerCollision().enabled = false;
            if (Repo_Lib.IsSprinting(playerController))
            {
                Repo_Lib.SetSprintEnergyDrain(playerController, 0f);
                Repo_Lib.SetPlayerCurrentEnergy(
                    playerController,
                    Repo_Lib.GetPlayerMaxEnergy(playerController)
                );
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

            //noclip
            if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleNoClip(playerController);
            }

            if (isNoClip)
            {
                FlyMovement(playerController);
            }


            //item spawn 7k val
            if (Input.GetKeyDown(KeyCode.F3))
            {
                Repo_Lib.SpawnItem(AssetManager.instance.enemyValuableBig);
            }


            //tp closest (further testing- userbase :D)
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


            //heal player idk 
            if (Input.GetKeyDown(KeyCode.F5))
            {
                PlayerAvatar playerAvatar = Repo_Lib.GetPlayerAvatar();
                Repo_Lib.HealPlayerMax(playerAvatar.gameObject);
            }

            //disable item durability
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

        //stuff we prolly wont use again
        
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

    //stuff we prolly wont use again
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
