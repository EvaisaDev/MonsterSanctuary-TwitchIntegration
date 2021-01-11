using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TwitchIntegration
{

    public class DontdestroyOnLoadAccessor : MonoBehaviour
    {
        private static DontdestroyOnLoadAccessor _instance;
        public static DontdestroyOnLoadAccessor Instance
        {
            get
            {
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null) Destroy(this);
            this.gameObject.name = this.GetType().ToString();
            _instance = this;
            DontDestroyOnLoad(this);
        }

        public GameObject[] GetAllRootsOfDontDestroyOnLoad()
        {
            return this.gameObject.scene.GetRootGameObjects();
        }
    }

    [BepInPlugin("evaisa.twitchintegration", "TwitchIntegration", "1.1.0")]
    public class TwitchIntegration : BaseUnityPlugin
    {

        public static TwitchIRC IRC;

        public static ConfigEntry<string> TwitchChannel;

        public static ConfigEntry<string> ChestRewardID;

        public static ConfigEntry<string> DebuffRewardID;

        public static ConfigEntry<string> BuffRewardID;

        public static ConfigEntry<string> CutHealthRewardID;

        public static ConfigEntry<string> FullHealRewardID;

        public static ConfigEntry<string> RandomShieldRewardID;

        public static ConfigEntry<string> UnequipItemsRewardID;

        public static Dictionary<string, List<GameObject>> referenceables = new Dictionary<string, List<GameObject>>();

        public static AssetBundle myAssetBundle;

        public static GameObject chest_prefab;
        public TwitchIntegration()
        {
            referenceables.Add("loot", new List<GameObject>());

            referenceables.Add("all", new List<GameObject>());

            chest_prefab = FindAllRootGameObjects().ToList().FirstOrDefault(item => {
                if (item.gameObject.name == "Chest" && item.GetComponent<Chest>() != null)
                {
                    return true;
                }
                return false;
            });

            TwitchChannel = Config.Bind("General", "TwitchChannel", "", "The channel to connect to.");
            ChestRewardID = Config.Bind("General", "ChestRewardID", "", "The reward ID of the chest reward.");
            BuffRewardID = Config.Bind("General", "BuffRewardID", "", "The reward ID of the buff reward.");
            DebuffRewardID = Config.Bind("General", "DebuffRewardID", "", "The reward ID of the debuff reward.");
            CutHealthRewardID = Config.Bind("General", "CutHealthRewardID", "", "The reward ID of the cut health reward.");
            FullHealRewardID = Config.Bind("General", "FullHealRewardID", "", "The reward ID of the full heal reward.");
            RandomShieldRewardID = Config.Bind("General", "RandomShieldRewardID", "", "The reward ID of the random shield reward.");
            UnequipItemsRewardID = Config.Bind("General", "UnequipItemsRewardID", "", "The reward ID of the unequip items reward.");

            myAssetBundle = LoadAssetBundle("TwitchIntegration.twitchbundle");
            //AssetBundle myAssetBundle = LoadAssetBundle(Properties.Resources.twitchbundle);

            var thingy = new GameObject();

            thingy.AddComponent<DontdestroyOnLoadAccessor>();

            DontDestroyOnLoad(thingy);

            var MainThreadObject = new GameObject("MainThread");

            DontDestroyOnLoad(MainThreadObject);

            MainThreadObject.AddComponent<MainThread>();

            var IRCObject = new GameObject("TwitchIRC");

            DontDestroyOnLoad(IRCObject);

            IRC = IRCObject.AddComponent<TwitchIRC>();

            IRC.newChatMessageEvent.AddListener(NewMessage);

            On.PlayerController.LoadGame += PlayerController_LoadGame; ;
            On.GameController.InitPlayerStartSetup += GameController_InitPlayerStartSetup;

        }


        private void GameController_InitPlayerStartSetup(On.GameController.orig_InitPlayerStartSetup orig, GameController self)
        {
            GameController.Instance.WorldData.Referenceables.ForEach(referenceable =>
            {
                if (referenceable != null)
                {
                    if (referenceable.GetComponent<BaseItem>())
                    {
                        if (!referenceable.GetComponent<KeyItem>() && !referenceable.GetComponent<UniqueItem>())
                        {
                            referenceables["loot"].Add(referenceable.gameObject);
                        }
                    }
                    referenceables["all"].Add(referenceable.gameObject);
                }
            });

            /*
            foreach (GameObject item in DontdestroyOnLoadAccessor.Instance.GetAllRootsOfDontDestroyOnLoad())
            {
                Debug.Log(item.name);
                AddDescendantsWithTag(item.transform, 0);

            }
            */

            orig(self);
        }

        private void LogDescendantsWithTag(Transform parent, int iteration)
        {
            iteration += 1;
            foreach (Transform child in parent)
            {
                string printString = "";
                for(int i = 0; i < iteration; i++)
                {
                    printString += "-";
                }
                printString += ">";
                printString += child.gameObject.name;
                Debug.Log(printString);
                LogDescendantsWithTag(child, iteration);
            }
        }

        private void PlayerController_LoadGame(On.PlayerController.orig_LoadGame orig, PlayerController self, SaveGameData saveGameData)
        {
            GameController.Instance.WorldData.Referenceables.ForEach(referenceable =>
            {
                if (referenceable != null)
                {
                    if (referenceable.GetComponent<BaseItem>())
                    {
                        if (!referenceable.GetComponent<KeyItem>() && !referenceable.GetComponent<UniqueItem>())
                        {
                            referenceables["loot"].Add(referenceable.gameObject);
                        }
                    }
                    referenceables["all"].Add(referenceable.gameObject);
                }
            });
            orig(self, saveGameData);
        }

        public void NewMessage(Chatter chatter)
        {
            //Color nameColor = chatter.GetRGBAColor();

            Debug.Log("Custom reward ID: \""+chatter.tags.customReward+"\"");

            var DontRunReward = false;

            if(chatter.tags.displayName.ToLower() == IRC.details.channel)
            {
                if (chatter.message == "link_chest")
                {
                    DontRunReward = true;
                    ChestRewardID.Value = chatter.tags.customReward;
                }
                else if (chatter.message == "link_buff")
                {
                    DontRunReward = true;
                    BuffRewardID.Value = chatter.tags.customReward;
                }
                else if (chatter.message == "link_debuff")
                {
                    DontRunReward = true;
                    DebuffRewardID.Value = chatter.tags.customReward;
                }
                else if (chatter.message == "link_cuthealth")
                {
                    DontRunReward = true;
                    CutHealthRewardID.Value = chatter.tags.customReward;
                }
                else if (chatter.message == "link_fullheal")
                {
                    DontRunReward = true;
                    FullHealRewardID.Value = chatter.tags.customReward;
                }
                else if (chatter.message == "link_randomshield")
                {
                    DontRunReward = true;
                   RandomShieldRewardID.Value = chatter.tags.customReward;
                }
                else if (chatter.message == "link_unequip")
                {
                    DontRunReward = true;
                    UnequipItemsRewardID.Value = chatter.tags.customReward;
                }


                DebuffRewardID.ConfigFile.Save();
                BuffRewardID.ConfigFile.Save();
                ChestRewardID.ConfigFile.Save();
            }

            if (!DontRunReward)
            {
                if (chatter.tags.customReward == ChestRewardID.Value)
                {
                    if (GameStateManager.Instance.State.IsState(GameStateManager.GameStates.Exploring))
                    {

                        var chest_object = GameObject.Instantiate(chest_prefab, PlayerController.Instance.PlayerPosition + new Vector3(0, -10, 0), Quaternion.identity);

                        chest_object.GetComponent<Chest>().ID = UnityEngine.Random.Range(1000, 10000000);
                        //chest_object.GetComponent<MeshRenderer>().material.shader = Shader.Find("16bitMonsters/AdditiveVertexColor");

                        chest_object.GetComponent<Chest>().Item = referenceables["loot"][UnityEngine.Random.Range(0, referenceables["loot"].Count)];

                    }
                }
                else if (chatter.tags.customReward == BuffRewardID.Value)
                {
                    if (GameStateManager.Instance.State.IsState(GameStateManager.GameStates.Combat))
                    {
                        var allies = CombatController.Instance.GetCurrentTurnMonsters();
                        var enemies = CombatController.Instance.GetOppositeMonsters(allies[0]);

                        if(UnityEngine.Random.Range(1, 101) <= 50)
                        {
                            Monster random_monster = allies[UnityEngine.Random.Range(0, allies.Count)];

                            random_monster.BuffManager.AddRandomBuff(new BuffSourceChain(random_monster));
                        }
                        else
                        {
                            Monster random_monster = enemies[UnityEngine.Random.Range(0, enemies.Count)];

                            random_monster.BuffManager.AddRandomBuff(new BuffSourceChain(random_monster));
                        }
                    }
                }
                else if (chatter.tags.customReward == DebuffRewardID.Value)
                {
                    if (GameStateManager.Instance.State.IsState(GameStateManager.GameStates.Combat))
                    {
                        var allies = CombatController.Instance.GetCurrentTurnMonsters();
                        var enemies = CombatController.Instance.GetOppositeMonsters(allies[0]);

                        if (UnityEngine.Random.Range(1, 101) <= 50)
                        {
                            Monster random_monster = allies[UnityEngine.Random.Range(0, allies.Count)];

                            random_monster.BuffManager.AddRandomDebuff(random_monster, new BaseAction());
                        }
                        else
                        {
                            Monster random_monster = enemies[UnityEngine.Random.Range(0, enemies.Count)];

                            random_monster.BuffManager.AddRandomDebuff(random_monster, new BaseAction());
                        }
                    }
                }
                else if (chatter.tags.customReward == CutHealthRewardID.Value)
                {
                    if (GameStateManager.Instance.State.IsState(GameStateManager.GameStates.Combat))
                    {
                        var allies = CombatController.Instance.GetCurrentTurnMonsters();
                        var enemies = CombatController.Instance.GetOppositeMonsters(allies[0]);

                        if (UnityEngine.Random.Range(1, 101) <= 50)
                        {
                            Monster random_monster = allies[UnityEngine.Random.Range(0, allies.Count)];

                            random_monster.CurrentHealth = random_monster.CurrentHealth / 2;
                        }
                        else
                        {
                            Monster random_monster = enemies[UnityEngine.Random.Range(0, enemies.Count)];

                            random_monster.CurrentHealth = random_monster.CurrentHealth / 2;
                        }
                    }
                }
                else if (chatter.tags.customReward == FullHealRewardID.Value)
                {
                    if (GameStateManager.Instance.State.IsState(GameStateManager.GameStates.Combat))
                    {
                        var allies = CombatController.Instance.GetCurrentTurnMonsters().FindAll(monster => monster.CurrentHealth < monster.MaxHealth);
                        var enemies = CombatController.Instance.GetOppositeMonsters(allies[0]).FindAll(monster => monster.CurrentHealth < monster.MaxHealth);

                        if (UnityEngine.Random.Range(1, 101) <= 50)
                        {
                            Monster random_monster = allies[UnityEngine.Random.Range(0, allies.Count)];

                            random_monster.CurrentHealth = random_monster.MaxHealth;
                        }
                        else
                        {
                            Monster random_monster = enemies[UnityEngine.Random.Range(0, enemies.Count)];

                            random_monster.CurrentHealth = random_monster.MaxHealth;
                        }
                    }
                }
                else if (chatter.tags.customReward == RandomShieldRewardID.Value)
                {
                    if (GameStateManager.Instance.State.IsState(GameStateManager.GameStates.Combat))
                    {
                        var allies = CombatController.Instance.GetCurrentTurnMonsters();
                        var enemies = CombatController.Instance.GetOppositeMonsters(allies[0]);

                        if (UnityEngine.Random.Range(1, 101) <= 50)
                        {
                            Monster random_monster = allies[UnityEngine.Random.Range(0, allies.Count)];

                            random_monster.Shield = UnityEngine.Random.Range(1, random_monster.MaxHealth);
                        }
                        else
                        {
                            Monster random_monster = enemies[UnityEngine.Random.Range(0, enemies.Count)];

                            random_monster.Shield = UnityEngine.Random.Range(1, random_monster.MaxHealth);
                        }
                    }
                }
                else if (chatter.tags.customReward == UnequipItemsRewardID.Value)
                {
                    if (GameStateManager.Instance.State.IsState(GameStateManager.GameStates.Combat) || GameStateManager.Instance.State.IsState(GameStateManager.GameStates.Exploring))
                    {
                        var myMonsters = PlayerController.Instance.Monsters.Active;

                        myMonsters[UnityEngine.Random.Range(0, myMonsters.Count)].Equipment.UnequipAll();
                    }
                }
            }
        }
        static AssetBundle LoadAssetBundle(string resourceString)
        {
            var execAssembly = Assembly.GetExecutingAssembly();
            using (var stream = execAssembly.GetManifestResourceStream(resourceString))
            {
                var bundle = AssetBundle.LoadFromStream(stream);

                return bundle;
            }
        }

        public static IEnumerable<GameObject> FindAllRootGameObjects()
        {
            return Resources.FindObjectsOfTypeAll<Transform>()
                .Where(t => t.parent == null)
                .Select(x => x.gameObject);
        }
    }

}