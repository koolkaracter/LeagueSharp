using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace JeonJunglePlay
{
    public class Program
    {
        public static Obj_AI_Hero Player = ObjectManager.Player;
        public static Spell Q, W, E, R;
        private static Vector3 spawn;
        private static Vector3 enemy_spawn;
        public static Menu JeonAutoJungleMenu;
        public static float gamestart = 0, pastTime = 0, pastTimeAFK, afktime = 0, fix = 0;
        public static List<MonsterINFO> MonsterList = new List<MonsterINFO>();
        public static int now = 1, max = 20, num = 0, next = -1;
        public static float recallhp = 0;

        public static bool recall = false,
            IsOVER = false,
            IsAttackedByTurret = false,
            IsAttackStart = false,
            IsCastW = false;

        public static bool canBuyItems = true, IsBlueTeam, IsStart = true, IsFind = false;
        public static SpellSlot smiteSlot = SpellSlot.Unknown;
        public static Spell smite;
        public static SpellDataInst Qdata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q);
        public static SpellDataInst Wdata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W);
        public static SpellDataInst Edata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E);
        public static SpellDataInst Rdata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R);
        public static List<Spell> cast2mob = new List<Spell>();
        public static List<Spell> cast2hero = new List<Spell>();
        public static List<Spell> cast4laneclear = new List<Spell>();

        public class MonsterINFO
        {
            public Vector3 Position;
            public string ID;
            public string name;
            public int order;
            public int respawntime;
            public int Range = 1000;

            public MonsterINFO()
            {
                MonsterList.Add(this);
            }
        }

        public class ItemToShop
        {
            public int Price, index;
            public ItemId item;
            public ItemId needItem;

            public ItemToShop()
            {
                num += 1;
            }
        }

        #region 몬스터

        public static MonsterINFO Baron = new MonsterINFO
        {
            ID = "Baron",
            Position = new Vector3(4910f, 10268f, -71.24f),
            name = "SRU_BaronSpawn",
            respawntime = 420
        };

        public static MonsterINFO Dragon = new MonsterINFO
        {
            ID = "Dragon",
            Position = new Vector3(9836f, 4408f, -71.24f),
            name = "SRU_Dragon",
            respawntime = 360
        };

        public static MonsterINFO top_crab = new MonsterINFO
        {
            ID = "top_crab",
            Position = new Vector3(4266f, 9634f, -67.87f),
            name = "Sru_Crab",
            respawntime = 180,
            Range = 3000
        };

        public static MonsterINFO BLUE_MID = new MonsterINFO
        {
            ID = "blue_MID",
            Position = new Vector3(5294.531f, 5537.924f, 50.46155f),
            name = "noneuses",
            respawntime = 180,
            Range = 3000
        };

        public static MonsterINFO PURPLE_MID = new MonsterINFO
        {
            ID = "purple_MID",
            Position = new Vector3(9443.35f, 9339.06f, 53.30994f),
            name = "noneuses",
            respawntime = 180,
            Range = 3000
        };

        public static MonsterINFO down_crab = new MonsterINFO
        {
            ID = "down_crab",
            Position = new Vector3(10524f, 5116f, -62.81f),
            name = "Sru_Crab",
            respawntime = 180,
            Range = 3000
        };

        public static MonsterINFO bteam_Razorbeak = new MonsterINFO
        {
            ID = "bteam_Razorbeak",
            Position = new Vector3(6974f, 5460f, 54f),
            name = "SRU_Razorbeak"
        };

        public static MonsterINFO bteam_Red = new MonsterINFO
        {
            ID = "bteam_Red",
            Position = new Vector3(7796f, 4028f, 54f),
            name = "SRU_Red",
            respawntime = 300
        };

        public static MonsterINFO bteam_Krug = new MonsterINFO
        {
            ID = "bteam_Krug",
            Position = new Vector3(8394f, 2750f, 50f),
            name = "SRU_Krug"
        };

        public static MonsterINFO bteam_Blue = new MonsterINFO
        {
            ID = "bteam_Blue",
            Position = new Vector3(3832f, 7996f, 52f),
            name = "SRU_Blue",
            respawntime = 300
        };

        public static MonsterINFO bteam_Gromp = new MonsterINFO
        {
            ID = "bteam_Gromp",
            Position = new Vector3(2112f, 8372f, 51.7f),
            name = "SRU_Gromp"
        };

        public static MonsterINFO bteam_Wolf = new MonsterINFO
        {
            ID = "bteam_Wolf",
            Position = new Vector3(3844f, 6474f, 52.46f),
            name = "SRU_Murkwolf"
        };

        public static MonsterINFO pteam_Razorbeak = new MonsterINFO
        {
            ID = "pteam_Razorbeak",
            Position = new Vector3(7856f, 9492f, 52.33f),
            name = "SRU_Razorbeak"
        };

        public static MonsterINFO pteam_Red = new MonsterINFO
        {
            ID = "pteam_Red",
            Position = new Vector3(7124f, 10856f, 56.34f),
            name = "SRU_Red",
            respawntime = 300
        };

        public static MonsterINFO pteam_Krug = new MonsterINFO
        {
            ID = "pteam_Krug",
            Position = new Vector3(6495f, 12227f, 56.47f),
            name = "SRU_Krug"
        };

        public static MonsterINFO pteam_Blue = new MonsterINFO
        {
            ID = "pteam_Blue",
            Position = new Vector3(10850f, 6938f, 51.72f),
            name = "SRU_Blue",
            respawntime = 300
        };

        public static MonsterINFO pteam_Gromp = new MonsterINFO
        {
            ID = "pteam_Gromp",
            Position = new Vector3(12766f, 6464f, 51.66f),
            name = "SRU_Gromp"
        };

        public static MonsterINFO pteam_Wolf = new MonsterINFO
        {
            ID = "pteam_Wolf",
            Position = new Vector3(10958f, 8286f, 62.46f),
            name = "SRU_Murkwolf"
        };

        #endregion

        #region 아이템

        #region ap

        public static List<ItemToShop> buyThings_AP = new List<ItemToShop>
        {
            new ItemToShop()
            {
                Price = 450,
                needItem = ItemId.Hunters_Machete,
                item = ItemId.Rangers_Trailblazer,
                index = 1
            },
            new ItemToShop() { Price = 450, needItem = ItemId.Rangers_Trailblazer, item = ItemId.Dagger, index = 2 },
            new ItemToShop()
            {
                Price = 1050,
                needItem = ItemId.Dagger,
                item = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
                index = 3
            },
            new ItemToShop()
            {
                Price = 325,
                needItem = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
                item = ItemId.Boots_of_Speed,
                index = 4
            },
            new ItemToShop()
            {
                Price = 775,
                needItem = ItemId.Boots_of_Speed,
                item = ItemId.Ionian_Boots_of_Lucidity,
                index = 5
            },
            new ItemToShop()
            {
                Price = 400,
                needItem = ItemId.Ionian_Boots_of_Lucidity,
                item = ItemId.Sapphire_Crystal,
                index = 6
            },
            new ItemToShop()
            {
                Price = 140 + 180,
                needItem = ItemId.Sapphire_Crystal,
                item = ItemId.Tear_of_the_Goddess,
                index = 7
            },
            new ItemToShop()
            {
                Price = 435,
                needItem = ItemId.Tear_of_the_Goddess,
                item = ItemId.Amplifying_Tome,
                index = 8
            },
            new ItemToShop()
            {
                Price = 300 + 465,
                needItem = ItemId.Amplifying_Tome,
                item = ItemId.Seekers_Armguard,
                index = 9
            },
            new ItemToShop()
            {
                Price = 1600,
                needItem = ItemId.Seekers_Armguard,
                item = ItemId.Needlessly_Large_Rod,
                index = 10
            },
            new ItemToShop()
            {
                Price = 500,
                needItem = ItemId.Needlessly_Large_Rod,
                item = ItemId.Zhonyas_Hourglass,
                index = 11
            },
            new ItemToShop()
            {
                Price = 860,
                needItem = ItemId.Zhonyas_Hourglass,
                item = ItemId.Blasting_Wand,
                index = 12
            },
            new ItemToShop()
            {
                Price = 1600,
                needItem = ItemId.Blasting_Wand,
                item = ItemId.Needlessly_Large_Rod,
                index = 13
            },
            new ItemToShop()
            {
                Price = 840,
                needItem = ItemId.Needlessly_Large_Rod,
                item = ItemId.Rabadons_Deathcap,
                index = 14
            },
            new ItemToShop()
            {
                Price = 860,
                needItem = ItemId.Rabadons_Deathcap,
                item = ItemId.Blasting_Wand,
                index = 15
            },
            new ItemToShop()
            {
                Price = 860,
                needItem = ItemId.Blasting_Wand,
                item = ItemId.Archangels_Staff,
                index = 16
            },
            new ItemToShop() { Price = 2295, needItem = ItemId.Archangels_Staff, item = ItemId.Void_Staff, index = 17 }
        };

        #endregion

        #region ad = default

        public static List<ItemToShop> buyThings = new List<ItemToShop>
        {
            new ItemToShop()
            {
                Price = 450,
                needItem = ItemId.Hunters_Machete,
                item = ItemId.Rangers_Trailblazer,
                index = 1
            },
            new ItemToShop() { Price = 450, needItem = ItemId.Rangers_Trailblazer, item = ItemId.Dagger, index = 2 },
            new ItemToShop()
            {
                Price = 1050,
                needItem = ItemId.Dagger,
                item = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
                index = 3
            },
            new ItemToShop()
            {
                Price = 325,
                needItem = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
                item = ItemId.Boots_of_Speed,
                index = 4
            },
            new ItemToShop()
            {
                Price = 675,
                needItem = ItemId.Boots_of_Speed,
                item = ItemId.Berserkers_Greaves,
                index = 5
            },
            new ItemToShop()
            {
                Price = 1400,
                needItem = ItemId.Berserkers_Greaves,
                item = ItemId.Bilgewater_Cutlass,
                index = 6
            },
            new ItemToShop()
            {
                Price = 1800,
                needItem = ItemId.Bilgewater_Cutlass,
                item = ItemId.Blade_of_the_Ruined_King,
                index = 7
            },
            new ItemToShop() { Price = 1100, needItem = ItemId.Blade_of_the_Ruined_King, item = ItemId.Zeal, index = 8 },
            new ItemToShop() { Price = 1700, needItem = ItemId.Zeal, item = ItemId.Phantom_Dancer, index = 9 },
            new ItemToShop() { Price = 1550, needItem = ItemId.Phantom_Dancer, item = ItemId.B_F_Sword, index = 10 },
            new ItemToShop() { Price = 2250, needItem = ItemId.B_F_Sword, item = ItemId.Infinity_Edge, index = 11 },
            new ItemToShop() { Price = 2900, needItem = ItemId.Infinity_Edge, item = ItemId.Last_Whisper, index = 12 }
        };

        #endregion

        #region as

        public static List<ItemToShop> buyThings_AS = new List<ItemToShop>
        {
            new ItemToShop()
            {
                Price = 450,
                needItem = ItemId.Hunters_Machete,
                item = ItemId.Rangers_Trailblazer,
                index = 1
            },
            new ItemToShop() { Price = 450, needItem = ItemId.Rangers_Trailblazer, item = ItemId.Dagger, index = 2 },
            new ItemToShop()
            {
                Price = 1050,
                needItem = ItemId.Dagger,
                item = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
                index = 3
            },
            new ItemToShop()
            {
                Price = 325,
                needItem = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
                item = ItemId.Boots_of_Speed,
                index = 4
            },
            new ItemToShop()
            {
                Price = 675,
                needItem = ItemId.Boots_of_Speed,
                item = ItemId.Boots_of_Swiftness,
                index = 5
            },
            new ItemToShop()
            {
                Price = 1400,
                needItem = ItemId.Boots_of_Swiftness,
                item = ItemId.Bilgewater_Cutlass,
                index = 6
            },
            new ItemToShop()
            {
                Price = 1800,
                needItem = ItemId.Bilgewater_Cutlass,
                item = ItemId.Blade_of_the_Ruined_King,
                index = 7
            },
            new ItemToShop()
            {
                Price = 1100,
                needItem = ItemId.Blade_of_the_Ruined_King,
                item = ItemId.Recurve_Bow,
                index = 8
            },
            new ItemToShop()
            {
                Price = 550 + 450 + 450,
                needItem = ItemId.Recurve_Bow,
                item = ItemId.Wits_End,
                index = 9
            },
            new ItemToShop() { Price = 1900, needItem = ItemId.Wits_End, item = ItemId.Tiamat_Melee_Only, index = 10 },
            new ItemToShop()
            {
                Price = 800 + 600,
                needItem = ItemId.Tiamat_Melee_Only,
                item = ItemId.Ravenous_Hydra_Melee_Only,
                index = 11
            },
            new ItemToShop()
            {
                Price = 2900,
                needItem = ItemId.Ravenous_Hydra_Melee_Only,
                item = ItemId.Last_Whisper,
                index = 12
            }
        };

        #endregion

        #region tanky

        public static List<ItemToShop> buyThings_TANK = new List<ItemToShop>
        {
            new ItemToShop()
            {
                Price = 450,
                needItem = ItemId.Hunters_Machete,
                item = ItemId.Rangers_Trailblazer,
                index = 1
            },
            new ItemToShop() { Price = 450, needItem = ItemId.Rangers_Trailblazer, item = ItemId.Dagger, index = 2 },
            new ItemToShop()
            {
                Price = 1050,
                needItem = ItemId.Dagger,
                item = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
                index = 3
            },
            new ItemToShop()
            {
                Price = 325,
                needItem = ItemId.Rangers_Trailblazer_Enchantment_Devourer,
                item = ItemId.Boots_of_Speed,
                index = 4
            },
            new ItemToShop()
            {
                Price = 675,
                needItem = ItemId.Boots_of_Speed,
                item = ItemId.Ionian_Boots_of_Lucidity,
                index = 5
            },
            new ItemToShop()
            {
                Price = 400,
                needItem = ItemId.Ionian_Boots_of_Lucidity,
                item = ItemId.Ruby_Crystal,
                index = 6
            },
            new ItemToShop()
            {
                Price = 500 + 180 + 820,
                needItem = ItemId.Ruby_Crystal,
                item = ItemId.Aegis_of_the_Legion,
                index = 7
            },
            new ItemToShop()
            {
                Price = 900,
                needItem = ItemId.Aegis_of_the_Legion,
                item = ItemId.Locket_of_the_Iron_Solari,
                index = 8
            },
            new ItemToShop()
            {
                Price = 950,
                needItem = ItemId.Locket_of_the_Iron_Solari,
                item = ItemId.Glacial_Shroud,
                index = 9
            },
            new ItemToShop()
            {
                Price = 1050 + 450,
                needItem = ItemId.Glacial_Shroud,
                item = ItemId.Frozen_Heart,
                index = 10
            },
            new ItemToShop()
            {
                Price = 500,
                needItem = ItemId.Frozen_Heart,
                item = ItemId.Null_Magic_Mantle,
                index = 11
            },
            new ItemToShop()
            {
                Price = 400 + 1150,
                needItem = ItemId.Null_Magic_Mantle,
                item = ItemId.Banshees_Veil,
                index = 12
            },
            new ItemToShop() { Price = 1200, needItem = ItemId.Banshees_Veil, item = ItemId.Sheen, index = 13 },
            new ItemToShop() { Price = 1325, needItem = ItemId.Sheen, item = ItemId.Phage, index = 14 },
            new ItemToShop() { Price = 1178, needItem = ItemId.Phage, item = ItemId.Trinity_Force, index = 15 },
        };

        #endregion

        #endregion

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            ////////////////////////////////////////////////
            JeonAutoJungleMenu = new Menu("JeonAutoJungle", "JeonAutoJungle", true);
            JeonAutoJungleMenu.AddItem(new MenuItem("isActive", "Activate")).SetValue(true);
            JeonAutoJungleMenu.AddItem(new MenuItem("maxstacks", "Max Stacks").SetValue(new Slider(30, 1, 150)));
            JeonAutoJungleMenu.AddItem(new MenuItem("autorecallheal", "Recall[for heal]")).SetValue(true);
            JeonAutoJungleMenu.AddItem(new MenuItem("hpper", "Recall on HP(%)").SetValue(new Slider(50, 0, 100)));
            JeonAutoJungleMenu.AddItem(new MenuItem("autorecallitem", "Recall[for item]")).SetValue(true);
            JeonAutoJungleMenu.AddItem(new MenuItem("evading", "Detect TurretAttack")).SetValue(true);
            JeonAutoJungleMenu.AddItem(new MenuItem("Invade", "InvadeEnemyJungle?")).SetValue(true);
            JeonAutoJungleMenu.AddItem(
                new MenuItem("k_dragon", "Add Dragon to Route on Lv").SetValue(new Slider(10, 1, 18)));
            if (Player.ChampionName == "MasterYi")
            {
                JeonAutoJungleMenu.AddItem(new MenuItem("yi_W", "Cast MasterYi-W(%)").SetValue(new Slider(85, 0, 100)));
            }
            JeonAutoJungleMenu.AddItem(new MenuItem("Gank", "Gank Lanes")).SetValue(true);
            JeonAutoJungleMenu.AddItem(
                new MenuItem("GankRange", "   Seeking range").SetValue(new Slider(6800, 0, 20000)));
            JeonAutoJungleMenu.AddItem(new MenuItem("Minlevel", "   Min level").SetValue(new Slider(3, 0, 18)));
            JeonAutoJungleMenu.AddToMainMenu();
            setSmiteSlot();

            #region 스펠설정

            SetSpells();

            #endregion

            #region 지점 설정

            if (Player.Team.ToString() == "Chaos")
            {
                spawn = new Vector3(14318f, 14354, 171.97f);
                enemy_spawn = new Vector3(415.33f, 453.38f, 182.66f);
                Game.PrintChat("Set PurpleTeam Spawn");
                IsBlueTeam = false;
                MonsterList.First(temp => temp.ID == pteam_Gromp.ID).order = 1;
                MonsterList.First(temp => temp.ID == pteam_Blue.ID).order = 2;
                MonsterList.First(temp => temp.ID == pteam_Wolf.ID).order = 3;
                MonsterList.First(temp => temp.ID == pteam_Razorbeak.ID).order = 4;
                MonsterList.First(temp => temp.ID == pteam_Red.ID).order = 5;
                MonsterList.First(temp => temp.ID == pteam_Krug.ID).order = 6;
                MonsterList.First(temp => temp.ID == bteam_Gromp.ID).order = 7;
                MonsterList.First(temp => temp.ID == bteam_Blue.ID).order = 8;
                MonsterList.First(temp => temp.ID == bteam_Wolf.ID).order = 9;
                MonsterList.First(temp => temp.ID == top_crab.ID).order = 10;
                MonsterList.First(temp => temp.ID == PURPLE_MID.ID).order = 11;
                MonsterList.First(temp => temp.ID == down_crab.ID).order = 12;
                MonsterList.First(temp => temp.ID == bteam_Razorbeak.ID).order = 13;
                MonsterList.First(temp => temp.ID == bteam_Red.ID).order = 14;
                MonsterList.First(temp => temp.ID == bteam_Krug.ID).order = 15;
            }
            else
            {
                spawn = new Vector3(415.33f, 453.38f, 182.66f);
                enemy_spawn = new Vector3(14318f, 14354, 171.97f);
                Game.PrintChat("Set BlueTeam Spawn");
                IsBlueTeam = true;
                MonsterList.First(temp => temp.ID == bteam_Gromp.ID).order = 6;
                MonsterList.First(temp => temp.ID == bteam_Blue.ID).order = 5;
                MonsterList.First(temp => temp.ID == bteam_Wolf.ID).order = 4;
                MonsterList.First(temp => temp.ID == bteam_Razorbeak.ID).order = 3;
                MonsterList.First(temp => temp.ID == bteam_Red.ID).order = 2;
                MonsterList.First(temp => temp.ID == bteam_Krug.ID).order = 1;
                MonsterList.First(temp => temp.ID == pteam_Razorbeak.ID).order = 7;
                MonsterList.First(temp => temp.ID == pteam_Red.ID).order = 8;
                MonsterList.First(temp => temp.ID == pteam_Krug.ID).order = 9;
                MonsterList.First(temp => temp.ID == top_crab.ID).order = 10;
                MonsterList.First(temp => temp.ID == BLUE_MID.ID).order = 11;
                MonsterList.First(temp => temp.ID == down_crab.ID).order = 12;
                MonsterList.First(temp => temp.ID == pteam_Gromp.ID).order = 13;
                MonsterList.First(temp => temp.ID == pteam_Blue.ID).order = 14;
                MonsterList.First(temp => temp.ID == pteam_Wolf.ID).order = 15;
            }
            max = MonsterList.OrderByDescending(h => h.order).First().order;

            #endregion

            #region 챔피언 설정

            if (Player.ChampionName.ToUpper() == "NUNU")
            {
                GetItemTree("AP");
                Game.PrintChat("NUNU BOT ACTIVE");
                Readini.GetSpelltree(new int[] { 1, 3, 2, 1, 1, 4, 1, 3, 1, 3, 4, 2, 2, 2, 2, 4, 3, 3 });
            }
            else if (Player.ChampionName.ToUpper() == "WARWICK")
            {
                GetItemTree("AS");
                Game.PrintChat("WARWICK BOT ACTIVE");
                Readini.GetSpelltree(new int[] { 1, 2, 3, 1, 1, 4, 1, 3, 1, 3, 4, 2, 2, 2, 2, 4, 3, 3 });
            }
            else if (Player.ChampionName.ToUpper() == "MASTERYI")
            {
                GetItemTree("AD");
                Game.PrintChat("MASTER YI BOT ACTIVE");
                Readini.GetSpelltree(new int[] { 1, 2, 3, 1, 1, 4, 1, 3, 1, 3, 4, 2, 2, 2, 2, 4, 3, 3 });
            }
            else if (Player.ChampionName.ToUpper() == "CHOGATH")
            {
                GetItemTree("AP");
                Game.PrintChat("CHOGATH BOT ACTIVE");
                Readini.GetSpelltree(new int[] { 3, 2, 1, 3, 3, 4, 3, 1, 3, 1, 4, 2, 2, 2, 2, 4, 1, 1 });
            }
            else if (Player.ChampionName.ToUpper() == "MAOKAI")
            {
                GetItemTree("AP");
                Game.PrintChat("MAOKAI BOT ACTIVE");
                Readini.GetSpelltree(new int[] { 1, 2, 3, 1, 1, 4, 1, 3, 1, 3, 4, 2, 2, 2, 2, 4, 3, 3 });
            }
            else if (Player.ChampionName.ToUpper() == "NASUS")
            {
                GetItemTree("TANK");
                Game.PrintChat("NASUS BOT ACTIVE");
                Readini.GetSpelltree(new int[] { 1, 3, 3, 2, 3, 4, 3, 1, 3, 1, 4, 1, 1, 2, 2, 4, 2, 2 });
            }
            else
            {
                #region Read ini

                Game.PrintChat("Read ini file");
                Readini.GetSpelltree(new int[] { 1, 3, 2, 1, 1, 4, 1, 3, 1, 3, 4, 2, 2, 2, 2, 4, 3, 3 });
                GetItemTree("AD");
                //Readini.GetSpells(setFile.FullName, ref cast2mob, ref cast2hero, ref cast4laneclear);

                #endregion readini
            }

            #endregion

            #region 현재 아이템 단계 설정 - 도중 리로드시 필요

            if (buyThings.Any(h => Items.HasItem(Convert.ToInt32(h.needItem))))
            {
                if (buyThings.First().needItem !=
                    buyThings.Last(h => Items.HasItem(Convert.ToInt32(h.needItem))).needItem)
                {
                    var lastitem = buyThings.Last(h => Items.HasItem(Convert.ToInt32(h.needItem)));
                    Game.PrintChat("Find new ItemList");
                    List<ItemToShop> newlist = buyThings.Where(t => t.index >= lastitem.index).ToList();
                    buyThings.Clear();
                    buyThings = newlist;
                }
            }

            #endregion

            gamestart = Game.Time; // 시작시간 설정
            Game.OnUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += OnCreate;
            Obj_AI_Base.OnProcessSpellCast += OnSpell;
            if (smiteSlot == SpellSlot.Unknown)
            {
                Game.PrintChat("YOU ARE NOT JUNGLER(NO SMITE)");
            }
        }


        private static void SetSpells()
        {
            if (Player.ChampionName.ToUpper() == "NUNU")
            {
                Q = new Spell(SpellSlot.Q, 125);
                Q.SetTargetted(0.5f, float.MaxValue);
                W = new Spell(SpellSlot.W, 700);
                W.SetTargetted(0.5f, float.MaxValue);
                E = new Spell(SpellSlot.E, 550);
                E.SetTargetted(0.5f, 1200f);
                R = new Spell(SpellSlot.R, 650);
            }
            else if (Player.ChampionName.ToUpper() == "CHOGATH")
            {
                Q = new Spell(SpellSlot.Q, 950);
                Q.SetSkillshot(0.5f, 175f, 625f, false, SkillshotType.SkillshotCircle);
                W = new Spell(SpellSlot.W, 650);
                W.SetSkillshot(0.25f, 250f, float.MaxValue, false, SkillshotType.SkillshotCone);
                E = new Spell(SpellSlot.E, 500);
                E.SetSkillshot(
                    E.Instance.SData.SpellCastTime, E.Instance.SData.LineWidth, E.Speed, false,
                    SkillshotType.SkillshotLine);
                R = new Spell(SpellSlot.R, 175);
                R.SetTargetted(0.5f, float.MaxValue);
            }
            else if (Player.ChampionName.ToUpper() == "WARWICK")
            {
                Q = new Spell(SpellSlot.Q, 400, TargetSelector.DamageType.Magical);
                Q.SetTargetted(0.5f, float.MaxValue);
                W = new Spell(SpellSlot.W, 1250);
                E = new Spell(SpellSlot.E, GetSpellRange(Edata));
                R = new Spell(SpellSlot.R, 700, TargetSelector.DamageType.Magical);
                R.SetTargetted(0.5f, float.MaxValue);
            }
            else if (Player.ChampionName.ToUpper() == "MASTERYI")
            {
                Q = new Spell(SpellSlot.Q, 600);
                Q.SetTargetted(0.5f, float.MaxValue);
                W = new Spell(SpellSlot.W, GetSpellRange(Wdata));
                E = new Spell(SpellSlot.E, GetSpellRange(Edata));
                R = new Spell(SpellSlot.R, GetSpellRange(Rdata));
            }
            else if (Player.ChampionName.ToUpper() == "MAOKAI")
            {
                Q = new Spell(SpellSlot.Q, 600);
                Q.SetSkillshot(0.50f, 110f, 1200f, false, SkillshotType.SkillshotLine);
                W = new Spell(SpellSlot.W, 500);
                W.SetTargetted(0.5f, float.MaxValue);
                E = new Spell(SpellSlot.E, 1100);
                E.SetSkillshot(1f, 250f, 1500f, false, SkillshotType.SkillshotCircle);
                R = new Spell(SpellSlot.R, 450);
            }
            else if (Player.ChampionName.ToUpper() == "NASUS")
            {
                Q = new Spell(SpellSlot.Q);
                W = new Spell(SpellSlot.W, 550);
                W.SetTargetted(0.5f, float.MaxValue);
                E = new Spell(SpellSlot.E, 600);
                E.SetSkillshot(
                    E.Instance.SData.SpellCastTime, E.Instance.SData.LineWidth, E.Speed, false,
                    SkillshotType.SkillshotCircle);
                R = new Spell(SpellSlot.R, 350f);
            }
            else
            {
                Q = new Spell(SpellSlot.Q, GetSpellRange(Qdata));
                W = new Spell(SpellSlot.W, GetSpellRange(Wdata));
                E = new Spell(SpellSlot.E, GetSpellRange(Edata));
                R = new Spell(SpellSlot.R, GetSpellRange(Rdata));
            }
        }

        public static List<Vector3> GankPos = new List<Vector3>()
        {
            new Vector3(2918f, 11142f, -71.2406f),
            //TopRiverBush
            new Vector3(2247.295f, 9706.15f, 56.8484f), //TopTriBush
            new Vector3(4462f, 11764f, 51.9751f), //TopTriBushD
            new Vector3(6538f, 8312f, -71.2406f), //MidTopBush
            new Vector3(8502f, 6548f, -71.2406f), //MidBottomBush
            new Vector3(11900f, 3898f, -67.15347f), //BotRiverBush
            new Vector3(10418f, 3050f, 50.23584f), //BotTriBush
            new Vector3(9227.038f, 2201.226f, 54.70776f), //BotSideBushDown
            new Vector3(5739.739f, 12759.03f, 52.83813f), //TopBushSide
            new Vector3(12483.09f, 5221.66f, 51.72937f) //BotupperTriBush
        };

        public static float junglingTime;

        private static void Game_OnGameUpdate(EventArgs args)
        {
            setSmiteSlot();
            Readini.UpdateLvl();
            if (Player.InFountain() && (Player.Health < Player.MaxHealth - 50 || Player.Mana < Player.MaxMana - 50))
            {
                return;
            }
            if (Player.Spellbook.IsChanneling)
            {
                return;
            }
            if (!JeonAutoJungleMenu.Item("isActive").GetValue<Boolean>() || smiteSlot == SpellSlot.Unknown)
            {
                return;
            }

            #region detect afk

            if (Game.Time - pastTimeAFK >= 1 && !Player.IsDead && !Player.IsRecalling())
            {
                afktime += 1;
                if (afktime > 10) // 잠수 10초 경과
                {
                    if (Player.InShop())
                    {
                        Player.IssueOrder(GameObjectOrder.AttackTo, new Vector3(4910f, 10268f, -71.24f));
                    }
                    else
                    {
                        Player.Spellbook.CastSpell(SpellSlot.Recall);
                    }
                    afktime = 0;
                }
                pastTimeAFK = Game.Time;
            }

            #endregion

            #region 0.5초마다 발동 // 오류 없애줌

            if (Environment.TickCount - pastTime <= 500)
            {
                return;
            }
            pastTime = Environment.TickCount;

            #endregion

            #region InvadeEnemyJungle

            if (!IsBlueTeam)
            {
                if (!JeonAutoJungleMenu.Item("Invade").GetValue<Boolean>())
                {
                    MonsterList.First(temp => temp.ID == bteam_Gromp.ID).order = 0;
                    MonsterList.First(temp => temp.ID == bteam_Blue.ID).order = 0;
                    MonsterList.First(temp => temp.ID == bteam_Wolf.ID).order = 0;
                    MonsterList.First(temp => temp.ID == top_crab.ID).order = 0;
                    MonsterList.First(temp => temp.ID == PURPLE_MID.ID).order = 0;
                    MonsterList.First(temp => temp.ID == down_crab.ID).order = 0;
                    MonsterList.First(temp => temp.ID == bteam_Razorbeak.ID).order = 0;
                    MonsterList.First(temp => temp.ID == bteam_Red.ID).order = 0;
                    MonsterList.First(temp => temp.ID == bteam_Krug.ID).order = 0;
                }
                else
                {
                    MonsterList.First(temp => temp.ID == bteam_Gromp.ID).order = 7;
                    MonsterList.First(temp => temp.ID == bteam_Blue.ID).order = 8;
                    MonsterList.First(temp => temp.ID == bteam_Wolf.ID).order = 9;
                    MonsterList.First(temp => temp.ID == top_crab.ID).order = 10;
                    MonsterList.First(temp => temp.ID == PURPLE_MID.ID).order = 11;
                    MonsterList.First(temp => temp.ID == down_crab.ID).order = 12;
                    MonsterList.First(temp => temp.ID == bteam_Razorbeak.ID).order = 13;
                    MonsterList.First(temp => temp.ID == bteam_Red.ID).order = 14;
                    MonsterList.First(temp => temp.ID == bteam_Krug.ID).order = 15;
                }
            }
            else
            {
                if (!JeonAutoJungleMenu.Item("Invade").GetValue<Boolean>())
                {
                    MonsterList.First(temp => temp.ID == pteam_Razorbeak.ID).order = 0;
                    MonsterList.First(temp => temp.ID == pteam_Red.ID).order = 0;
                    MonsterList.First(temp => temp.ID == pteam_Krug.ID).order = 0;
                    MonsterList.First(temp => temp.ID == top_crab.ID).order = 0;
                    MonsterList.First(temp => temp.ID == BLUE_MID.ID).order = 0;
                    MonsterList.First(temp => temp.ID == down_crab.ID).order = 0;
                    MonsterList.First(temp => temp.ID == pteam_Gromp.ID).order = 0;
                    MonsterList.First(temp => temp.ID == pteam_Blue.ID).order = 0;
                    MonsterList.First(temp => temp.ID == pteam_Wolf.ID).order = 0;
                }
                else
                {
                    MonsterList.First(temp => temp.ID == pteam_Razorbeak.ID).order = 7;
                    MonsterList.First(temp => temp.ID == pteam_Red.ID).order = 8;
                    MonsterList.First(temp => temp.ID == pteam_Krug.ID).order = 9;
                    MonsterList.First(temp => temp.ID == top_crab.ID).order = 10;
                    MonsterList.First(temp => temp.ID == BLUE_MID.ID).order = 11;
                    MonsterList.First(temp => temp.ID == down_crab.ID).order = 12;
                    MonsterList.First(temp => temp.ID == pteam_Gromp.ID).order = 13;
                    MonsterList.First(temp => temp.ID == pteam_Blue.ID).order = 14;
                    MonsterList.First(temp => temp.ID == pteam_Wolf.ID).order = 15;
                }
            }
            max = MonsterList.OrderByDescending(h => h.order).First().order;

            #endregion

            #region detect reload

            if (IsStart && Player.Level > 1)
            {
                Game.PrintChat("You did reload");
                IsStart = false;
                var last = MonsterList.OrderBy(temp => temp.Position.Distance(Player.Position)).FirstOrDefault();
                if (last != null)
                {
                    now = last.order;
                }
                if (Player.InFountain())
                {
                    now = 1;
                }
            }

            #endregion

            #region check somethings about dragon

            if (Player.Level > JeonAutoJungleMenu.Item("k_dragon").GetValue<Slider>().Value)
            {
                if (MonsterList.First(temp => temp.ID == down_crab.ID).order == 12)
                {
                    MonsterList.First(temp => temp.ID == down_crab.ID).order = 0;
                    MonsterList.First(temp => temp.ID == Dragon.ID).order = 12;
                }
            }

            #endregion

            #region 오토 플레이 - auto play

            if (Player.IsMoving)
            {
                afktime = 0;
            }
            if (!IsOVER)
            {
                if (IsStart) // start
                {
                    if (Game.Time - gamestart >= 0)
                    {
                        Player.IssueOrder(GameObjectOrder.MoveTo, MonsterList.First(t => t.order == 1).Position);
                        afktime = 0;
                    }
                    if (Player.Distance(MonsterList.First(t => t.order == 1).Position) <= 100)
                    {
                        if (CheckMonster(
                            MonsterList.First(t => t.order == 1).name, MonsterList.First(t => t.order == 1).Position,
                            MonsterList.First(t => t.order == 1).Range))
                        {
                            IsStart = false;
                            now = 1;
                            Game.PrintChat("START!");
                        }
                    }
                }
                else
                {
                    if (Player.IsDead && now >= 7 && now <= 9)
                    {
                        now = 5;
                    }
                    if (Player.IsDead && now > 12)
                    {
                        now = 12;
                    }
                    MonsterINFO target = MonsterList.FirstOrDefault(t => t.order == now);
                    if (Player.IsMoving || Player.IsWindingUp || Player.IsRecalling() || Player.Level == 1)
                    {
                        fix = 0;
                    }
                    else
                    {
                        fix++;
                    }
                    if (fix > 30)
                    {
                        now++;
                        fix = 0;
                    }
                    var anyMonsterCampAroundMe = MonsterList.Any(m => m.Position.Distance(Player.Position) < 1000);
                    if (Environment.TickCount - junglingTime > 2000 && anyMonsterCampAroundMe && !Player.InFountain() &&
                        !recall && Player.CountEnemiesInRange(1500) == 0)
                    {
                        if (Player.HealthPercentage() < JeonAutoJungleMenu.Item("hpper").GetValue<Slider>().Value &&
                            !Player.IsDead //hpper
                            && JeonAutoJungleMenu.Item("autorecallheal").GetValue<Boolean>()) // HP LESS THAN 25%
                        {
                            Game.PrintChat("YOUR HP IS SO LOW. RECALL!");
                            Player.Spellbook.CastSpell(SpellSlot.Recall);
                            recall = true;
                            recallhp = Player.Health;
                        }
                        else if (Player.Gold > buyThings.First().Price &&
                                 JeonAutoJungleMenu.Item("autorecallitem").GetValue<Boolean>() &&
                                 Player.InventoryItems.Length < 8) // HP LESS THAN 25%
                        {
                            Game.PrintChat("CAN BUY " + buyThings.First().item.ToString() + ". RECALL!");
                            Player.Spellbook.CastSpell(SpellSlot.Recall);
                            recall = true;
                            recallhp = Player.Health;
                        }
                    }
                    if ((Dragon.Position.CountEnemiesInRange(800) > 0 || Dragon.Position.CountAlliesInRange(800) > 0))
                    {
                        var drake = GetNearest_big(Dragon.Position);
                        if (drake != null && drake.Health < drake.MaxHealth - 300)
                        {
                            if (Player.Distance(Dragon.Position) > 500)
                            {
                                Player.IssueOrder(GameObjectOrder.MoveTo, Dragon.Position);
                                return;
                            }
                            else
                            {
                                Player.IssueOrder(GameObjectOrder.AttackUnit, drake);
                                DoCast();
                                DoSmite();
                                return;
                            }
                        }
                    }
                    if ((Baron.Position.CountEnemiesInRange(800) > 0 || Baron.Position.CountAlliesInRange(800) > 0))
                    {
                        var baron = GetNearest_big(Baron.Position);
                        if (baron != null && baron.Health < baron.MaxHealth - 300)
                        {
                            if (Player.Distance(Baron.Position) > 500)
                            {
                                Player.IssueOrder(GameObjectOrder.MoveTo, Baron.Position);
                                return;
                            }
                            else
                            {
                                Player.IssueOrder(GameObjectOrder.AttackUnit, baron);
                                DoCast();
                                DoSmite();
                                return;
                            }
                        }
                    }
                    if (JeonAutoJungleMenu.Item("Gank").GetValue<Boolean>() &&
                        Player.Level >= JeonAutoJungleMenu.Item("Minlevel").GetValue<Slider>().Value && !recall &&
                        Environment.TickCount - junglingTime > 2000 && Player.CountEnemiesInRange(2000) == 0 &&
                        Player.Mana > R.ManaCost)
                    {
                        var enemies = Player.CountEnemiesInRange(1500);
                        Obj_AI_Hero gankTarget = null;
                        foreach (var possibleTarget in
                            HeroManager.Enemies.Where(
                                e =>
                                    e.Distance(Player) < JeonAutoJungleMenu.Item("GankRange").GetValue<Slider>().Value &&
                                    e.HealthPercent < 90 && e.IsValidTarget() && !e.UnderTurret(true))
                                .OrderBy(e => e.Distance(Player)))
                        {
                            var myDmg = GetComboDMG(Player, possibleTarget);
                            if (Player.Level + 1 <= possibleTarget.Level && myDmg < possibleTarget.Health)
                            {
                                continue;
                            }
                            if (
                                ObjectManager.Get<Obj_AI_Turret>()
                                    .FirstOrDefault(
                                        t => t.IsEnemy && t.IsValidTarget() && t.Distance(possibleTarget) < 1200) !=
                                null)
                            {
                                continue;
                            }
                            if (possibleTarget.CountEnemiesInRange(1500) > possibleTarget.CountAlliesInRange(1500) + 1)
                            {
                                continue;
                            }
                            if (GetComboDMG(possibleTarget, Player) > Player.Health)
                            {
                                continue;
                            }
                            var ally =
                                HeroManager.Allies.Where(a => !a.IsDead && a.Distance(possibleTarget) < 2000)
                                    .OrderBy(a => a.Distance(possibleTarget))
                                    .FirstOrDefault();
                            var hp = possibleTarget.Health - myDmg;
                            if (ally != null && hp > 0)
                            {
                                hp -= GetComboDMG(ally, possibleTarget);
                            }
                            if (hp < 0)
                            {
                                gankTarget = possibleTarget;
                            }
                        }
                        if (gankTarget != null)
                        {
                            var gankPosition = GankPos.OrderBy(p => p.Distance(gankTarget.Position)).FirstOrDefault();
                            if (gankTarget.Distance(Player) > 2000 && gankPosition.IsValid() && GoodPath(gankPosition) &&
                                gankPosition.Distance(gankTarget.Position) < 2000 &&
                                Player.Distance(gankTarget) > gankPosition.Distance(gankTarget.Position))
                            {
                                Player.IssueOrder(GameObjectOrder.MoveTo, gankPosition);
                                return;
                            }
                            else if (gankTarget.Distance(Player) > 2000 && GoodPath(gankTarget.Position))
                            {
                                Player.IssueOrder(GameObjectOrder.MoveTo, gankTarget.Position);
                                return;
                            }
                        }
                    }
                    if (Player.CountEnemiesInRange(2000) > 0)
                    {
                        var tar =
                            HeroManager.Enemies.Where(
                                e => e.Distance(Player.Position) < 2000 && e.IsValidTarget() && !e.UnderTurret(true))
                                .OrderBy(e => e.Health)
                                .FirstOrDefault();
                        if (tar != null)
                        {
                            var ally =
                                HeroManager.Allies.Where(a => !a.IsDead && a.Distance(tar) < 1500)
                                    .OrderBy(a => a.Distance(tar))
                                    .FirstOrDefault();
                            var myhp = Player.Health - GetComboDMG(tar, Player);
                            var enemyhp = tar.Health - GetComboDMG(Player, tar);
                            if (ally != null && enemyhp > 0)
                            {
                                enemyhp -= GetComboDMG(ally, tar);
                            }
                            if (myhp > enemyhp)
                            {
                                Player.IssueOrder(GameObjectOrder.AttackUnit, tar);
                                DoCast_Hero(tar);
                                return;
                            }
                            else if (myhp < 0)
                            {
                                Player.IssueOrder(GameObjectOrder.MoveTo, spawn);
                                return;
                            }
                        }
                    }
                    if (Player.CountEnemiesInRange(1500) == 0 && Player.CountAlliesInRange(1500) == 0 &&
                        !anyMonsterCampAroundMe)
                    {
                        var mini = AtLane();
                        if (mini != null)
                        {
                            Player.IssueOrder(GameObjectOrder.AttackUnit, mini);
                            DoCast();
                            return;
                        }
                    }
                    var crab =
                        ObjectManager.Get<Obj_AI_Base>()
                            .FirstOrDefault(m => m.Name.Contains("Crab") && m.IsValidTarget(1000));
                    var attackCrab = false;
                    if (crab != null && target != null && !target.name.Contains("SRU_Dragon"))
                    {
                        Player.IssueOrder(GameObjectOrder.AttackUnit, crab);
                        DoCast();
                        if (smite.Slot != SpellSlot.Unknown && smite.IsReady())
                        {
                            DoSmite();
                        }
                        attackCrab = true;
                    }
                    if (Player.Position.Distance(target.Position) >= 300)
                    {
                        if (!recall)
                        {
                            if (target != null && Player.Position.Distance(target.Position) > Player.AttackRange)
                            {
                                if (target.name.Contains("Crab") && crab != null)
                                {
                                    Player.IssueOrder(GameObjectOrder.AttackUnit, crab);
                                    DoCast();
                                }
                                else if (!attackCrab)
                                {
                                    if (!GoodPath(target.Position))
                                    {
                                        //Console.WriteLine("Skipped" + target.name);
                                        now++;
                                        return;
                                    }
                                    Player.IssueOrder(GameObjectOrder.MoveTo, target.Position);
                                }

                                afktime = 0;
                            }
                            DoCast_Hero();
                        }
                    }
                    else
                    {
                        if (CheckMonster(target.name, target.Position, 500)) //해당지점에 몬스터가 있는지
                        {
                            DoCast();
                            Player.IssueOrder(GameObjectOrder.AttackUnit, GetNearest(Player.Position));
                            afktime = 0;
                            if (smite.Slot != SpellSlot.Unknown && smite.IsReady())
                            {
                                DoSmite();
                            }
                        }
                        else
                        {
                            if (next > 0)
                            {
                                now = next;
                                next = -1;
                            }
                            now += 1;
                            if (now > max)
                            {
                                now = 1;
                            }
                        }
                    }
                }
                if (Player.InShop())
                {
                    recall = false;
                }
            }

            #endregion

            #region 스택이 넘는지 체크 - check ur stacks

            foreach (var buff in Player.Buffs.Where(b => b.DisplayName == "Enchantment_Slayer_Stacks"))
            {
                int maxstacks = JeonAutoJungleMenu.Item("maxstacks").GetValue<Slider>().Value;
                if (buff.Count >= maxstacks && !IsOVER) //--테스트
                {
                    IsOVER = true;
                    Game.PrintChat("Stacks Over " + maxstacks + ". Now Going to be offense.");
                }
                if (buff.Count < maxstacks && IsOVER) //-- I don't speak korean :D
                {
                    Game.PrintChat("Stacks under " + maxstacks + ". Going back to farm.");
                    IsOVER = false;
                    IsAttackStart = false;
                }
            }

            #endregion

            #region 공격 모드 - offensive mode

            if (IsOVER)
            {
                if (!IsAttackStart)
                {
                    if (!ObjectManager.Get<Obj_AI_Turret>().Any(t => t.Name == "Turret_T2_C_05_A") && IsBlueTeam)
                    {
                        IsAttackStart = true;
                    }
                    else if (!ObjectManager.Get<Obj_AI_Turret>().Any(t => t.Name == "Turret_T1_C_05_A") && !IsBlueTeam)
                    {
                        IsAttackStart = true;
                    }
                    else
                    {
                        if (IsBlueTeam)
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, BLUE_MID.Position);
                            if (Player.Distance(BLUE_MID.Position) <= 100)
                            {
                                IsAttackStart = true;
                            }
                        }
                        else
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, PURPLE_MID.Position);
                            if (Player.Distance(PURPLE_MID.Position) <= 100)
                            {
                                IsAttackStart = true;
                            }
                        }
                    }
                }
                else
                {
                    var turret =
                        ObjectManager.Get<Obj_AI_Turret>()
                            .OrderBy(t => t.Distance(Player.Position))
                            .First(t => t.IsEnemy);
                    if (IsOVER && !IsAttackedByTurret)
                    {
                        DoCast_Hero();
                        DoLaneClear();
                        if (turret.Distance(Player.Position) > 1200)
                        {
                            Player.IssueOrder(GameObjectOrder.AttackTo, enemy_spawn);
                        }

                        else if (GetMinions(turret) > 2)
                        {
                            Player.IssueOrder(GameObjectOrder.AttackTo, enemy_spawn);
                        }

                        else
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(spawn, 855));
                        }

                        afktime = 0;
                    }
                    if (turret.Distance(Player.Position) > 800)
                    {
                        IsAttackedByTurret = false;
                    }
                    if (Player.IsDead)
                    {
                        IsAttackedByTurret = false;
                    }
                }
            }

            #endregion

            #region 상점이용가능할때 // when you are in shop range or dead

            #region 시작아이템 사기 // startup

            if (Utility.InShop(Player) || Player.IsDead)
            {
                if (
                    !(Items.HasItem(Convert.ToInt32(ItemId.Hunters_Machete)) ||
                      Items.HasItem(Convert.ToInt32(ItemId.Rangers_Trailblazer)) ||
                      Items.HasItem(Convert.ToInt32(ItemId.Rangers_Trailblazer_Enchantment_Devourer))))
                {
                    if (smiteSlot != SpellSlot.Unknown)
                    {
                        Player.BuyItem(ItemId.Hunters_Machete);
                        Player.BuyItem(ItemId.Warding_Totem_Trinket);
                    }
                }

                #endregion

                //Game.PrintChat("Gold:" + Player.Gold);
                //Game.PrintChat("NeedItem:" + buyThings.First().needItem.ToString());
                //Game.PrintChat("BuyItem:" + buyThings.First().item.ToString());

                #region 아이템트리 올리기 // item build up

                if (buyThings.Any(t => t.item != ItemId.Unknown))
                {
                    if (Items.HasItem(Convert.ToInt32(buyThings.First().needItem)))
                    {
                        if (Player.Gold > buyThings.First().Price)
                        {
                            Player.BuyItem(buyThings.First().item);
                            buyThings.Remove(buyThings.First());
                        }
                    }
                }

                #endregion

                #region 포션 구매 - buy potions

                if (Player.Gold > 35f && !IsOVER && !Player.InventoryItems.Any(t => t.Id == ItemId.Health_Potion) &&
                    Player.Level <= 6)
                {
                    Player.BuyItem(ItemId.Health_Potion);
                }
                if (Player.InventoryItems.Any(t => t.Id == ItemId.Health_Potion))
                {
                    if (Player.InventoryItems.First(t => t.Id == ItemId.Health_Potion).Stacks <= 2 && Player.Level <= 6)
                    {
                        Player.BuyItem(ItemId.Health_Potion);
                    }
                    if (Player.Level > 6)
                    {
                        Player.SellItem(Player.InventoryItems.First(t => t.Id == ItemId.Health_Potion).Slot);
                    }
                }
                if (Player.Level > 6 && Items.HasItem(2010))
                {
                    Player.SellItem(Player.InventoryItems.First(t => Convert.ToInt32(t.Id) == 2010).Slot);
                }

                #endregion
            }

            #endregion

            #region 자동포션사용 - auto use potions

            if (Player.HealthPercentage() <= 60 && !Player.InShop())
            {
                ItemId item = ItemId.Health_Potion;
                if (Player.InventoryItems.Any(t => Convert.ToInt32(t.Id) == 2010))
                {
                    item = ItemId.Unknown;
                }
                if (Player.InventoryItems.Any(t => (t.Id == ItemId.Health_Potion || Convert.ToInt32(t.Id) == 2010)))
                {
                    if (!Player.HasBuff("ItemMiniRegenPotion") && item == ItemId.Unknown)
                    {
                        Player.Spellbook.CastSpell(
                            Player.InventoryItems.First(t => Convert.ToInt32(t.Id) == 2010).SpellSlot);
                    }
                    if (!Player.HasBuff("RegenerationPotion") && item == ItemId.Health_Potion)
                    {
                        Player.Spellbook.CastSpell(
                            Player.InventoryItems.First(t => t.Id == ItemId.Health_Potion).SpellSlot);
                    }
                }
            }

            #endregion
        }

        private static Obj_AI_Base AtLane()
        {
            return
                MinionManager.GetMinions(Player.Position, 800, MinionTypes.All, MinionTeam.Enemy)
                    .Where(m => m.IsValidTarget() && !m.UnderTurret())
                    .OrderByDescending(m => m.Health < Player.GetAutoAttackDamage(m))
                    .ThenBy(m => m.Distance(Player.Position))
                    .FirstOrDefault();
        }

        private static bool GoodPath(Vector3 gankPosition)
        {
            return
                Player.GetPath(gankPosition)
                    .All(
                        point =>
                            !point.UnderTurret(true) && gankPosition.CountEnemiesInRange(1200) == 0 &&
                            MinionManager.GetMinions(gankPosition, 1200, MinionTypes.All, MinionTeam.Enemy).Count == 0);
        }

        private static void Jungling()
        {
            junglingTime = Environment.TickCount;
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsValid<Obj_SpellMissile>())
            {
                var m = (Obj_SpellMissile) sender;
                if (m.SpellCaster.IsValid<Obj_AI_Turret>() && m.SpellCaster.IsEnemy && m.Target.IsValid<Obj_AI_Hero>() &&
                    m.Target.IsMe && JeonAutoJungleMenu.Item("evading").GetValue<Boolean>())
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, spawn);
                    Game.PrintChat("OOPS YOU ARE ATTACKED BY TURRET!");
                    Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(spawn, 855));
                    IsAttackedByTurret = true;
                }
            }
        }

        private static void OnSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs spell)
        {
            if (spell.Target.IsValid<Obj_AI_Hero>())
            {
                if (spell.Target.IsMe && sender.IsEnemy)
                {
                    string[] turrest =
                    {
                        "Turret_T2_C_01_A", "Turret_T2_C_02_A", "Turret_T2_L_01_A", "Turret_T2_C_03_A",
                        "Turret_T2_R_01_A", "Turret_T1_C_01_A", "Turret_T1_C_02_A", "Turret_T1_C_06_A",
                        "Turret_T1_C_03_A", "Turret_T1_C_07_A"
                    };
                    if (turrest.Contains(sender.Name) && JeonAutoJungleMenu.Item("evading").GetValue<Boolean>())
                    {
                        Player.IssueOrder(GameObjectOrder.MoveTo, spawn);
                        Game.PrintChat("OOPS YOU ARE ATTACKED BY INHIBIT TURRET!");
                        Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(spawn, 855));
                        IsAttackedByTurret = true;
                    }
                }
            }
        }

        #region getminions around turret

        public static int GetMinions(Obj_AI_Turret Turret)
        {
            int i = 0;
            foreach (var minion in
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(t => t.Name.Contains("Minion") && t.Distance(Turret.Position) <= 855 && !t.IsEnemy))
            {
                i++;
            }
            return i;
        }

        #endregion

        #region spell methods

        public static void DoSmite()
        {
            var mob1 = GetNearest_big(Player.Position);
            if (mob1 != null && mob1.Health < mob1.MaxHealth - 50)
            {
                if (((mob1.Name.Contains("Dragon") || mob1.Name.Contains("Baron") || mob1.Name.Contains("Crab") ||
                      (mob1.Name.Contains("SRU_Red") && Player.HealthPercent > 15) ||
                      Player.CountEnemiesInRange(1000) > 0) && smiteDamage(mob1) < mob1.Health))
                {
                    return;
                }
                smite.CastOnUnit(mob1);
            }
        }

        public static void DoLaneClear()
        {
            var mob1 =
                MinionManager.GetMinions(700, MinionTypes.All, MinionTeam.NotAlly)
                    .Where(t => t.IsValidTarget())
                    .OrderBy(t => t.MaxHealth)
                    .FirstOrDefault();
            //if (Player.ChampionName.ToUpper() == "NUNU" && Q.IsReady()) // 누누 Q버그수정 - Fix nunu Q bug
            Player.IssueOrder(GameObjectOrder.MoveTo, mob1.ServerPosition.Extend(Player.ServerPosition, 10));
            if (mob1 != null)
            {
                castspell_laneclear(mob1);
            }
        }

        public static void DoCast()
        {
            var mob1 = GetNearest_big(Player.Position);
            if (mob1 == null)
            {
                mob1 =
                    MinionManager.GetMinions(700, MinionTypes.All, MinionTeam.NotAlly)
                        .Where(t => t.IsValidTarget())
                        .OrderByDescending(t => t.MaxHealth - t.Health)
                        .FirstOrDefault();
            }
            //if (Player.ChampionName.ToUpper() == "NUNU" && Q.IsReady()) // 누누 Q버그수정 - Fix nunu Q bug
            //Player.IssueOrder(GameObjectOrder.MoveTo, mob1.ServerPosition.Extend(Player.ServerPosition, 10));
            if (mob1 != null)
            {
                castspell(mob1);
            }
            Jungling();
        }

        public static void DoCast_Hero(Obj_AI_Hero trg = null)
        {
            if (trg != null)
            {
                castspell_hero(trg);
                return;
            }
            if (ObjectManager.Get<Obj_AI_Hero>().Any(t => t.IsEnemy & !t.IsDead && Player.Distance(t.Position) <= 700))
            {
                var target =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .OrderBy(t => t.Distance(Player.Position))
                        .Where(tar => tar.IsEnemy && !tar.IsMe && !tar.IsDead)
                        .First(); // 플레이어와 가장 가까운타겟
                var turret =
                    ObjectManager.Get<Obj_AI_Turret>()
                        .OrderBy(t => t.Distance(target.Position))
                        .Where(tur => tur.IsEnemy && !tur.IsDead)
                        .First(); // 타겟과 가장 가까운터렛
                if (turret.Distance(target.Position) > 755) // 터렛 사정거리 밖에있어야만 공격함.
                {
                    castspell_hero(target);
                }
            }
        }

        public static void castspell(Obj_AI_Base mob1)
        {
            if (Player.IsWindingUp)
            {
                return;
            }
            if (Player.ChampionName.ToUpper() == "NUNU")
            {
                if (Q.IsReady())
                {
                    Q.CastOnUnit(mob1);
                }
                if (E.IsReady())
                {
                    E.CastOnUnit(mob1);
                }
                if (W.IsReady())
                {
                    W.Cast();
                }
            }
            else if (Player.ChampionName.ToUpper() == "CHOGATH")
            {
                if (Q.IsReady())
                {
                    Q.Cast(mob1.Position);
                }
                if (W.IsReady())
                {
                    W.Cast(mob1.Position);
                }
                if (R.IsReady() && R.GetDamage(mob1) >= mob1.Health)
                {
                    R.CastOnUnit(mob1);
                }
            }
            else if (Player.ChampionName.ToUpper() == "WARWICK")
            {
                if (Q.IsReady())
                {
                    Q.CastOnUnit(mob1);
                }
                if (W.IsReady() && Player.Distance(mob1) < 300)
                {
                    W.Cast();
                }
                if (R.IsReady())
                {
                    R.CastOnUnit(mob1);
                }
            }
            else if (Player.ChampionName.ToUpper() == "MASTERYI")
            {
                if (Q.IsReady())
                {
                    Q.CastOnUnit(mob1);
                }

                if (W.IsReady() && Player.HealthPercent < JeonAutoJungleMenu.Item("yi_W").GetValue<Slider>().Value)
                {
                    W.Cast();
                }
                if (E.IsReady())
                {
                    E.Cast();
                }
                if (R.IsReady())
                {
                    R.Cast();
                }
            }
            else if (Player.ChampionName.ToUpper() == "MAOKAI")
            {
                if (Q.IsReady())
                {
                    Q.Cast(mob1.Position);
                }
                if (E.IsReady())
                {
                    E.Cast(mob1.Position);
                }
                if (W.IsReady())
                {
                    W.CastOnUnit(mob1);
                }
            }
            else if (Player.ChampionName.ToUpper() == "NASUS")
            {
                if (Q.IsReady() && CheckNasusQDamage(mob1))
                {
                    Q.Cast();
                }
                if (W.IsReady() && mob1.IsValid<Obj_AI_Hero>())
                {
                    W.CastOnUnit(mob1);
                }
                if (E.IsReady())
                {
                    E.Cast(mob1.Position);
                }
            }
            else
            {
                foreach (var spell in cast2mob)
                {
                    if (spell.IsReady())
                    {
                        spell.CastOnUnit(mob1);
                    }
                    if (spell.IsReady())
                    {
                        spell.Cast();
                    }
                    if (spell.IsReady())
                    {
                        spell.Cast(mob1.Position);
                    }
                }
            }
        }

        public static void castspell_hero(Obj_AI_Base mob1)
        {
            if (Player.ChampionName.ToUpper() == "NUNU")
            {
                if (Q.IsReady())
                {
                    Q.CastOnUnit(mob1);
                }
                if (E.IsReady())
                {
                    E.CastOnUnit(mob1);
                }
                if (W.IsReady())
                {
                    W.Cast();
                }
            }
            else if (Player.ChampionName.ToUpper() == "CHOGATH")
            {
                if (Q.IsReady())
                {
                    Q.Cast(mob1.Position);
                }
                if (W.IsReady())
                {
                    W.Cast(mob1.Position);
                }
                if (R.IsReady() && R.GetDamage(mob1) >= mob1.Health)
                {
                    R.CastOnUnit(mob1);
                }
            }
            else if (Player.ChampionName.ToUpper() == "WARWICK")
            {
                if (Q.IsReady())
                {
                    Q.CastOnUnit(mob1);
                }
                if (W.IsReady() && Player.Distance(mob1) < 300)
                {
                    W.Cast();
                }
                if (R.IsReady())
                {
                    R.CastOnUnit(mob1);
                }
            }
            else if (Player.ChampionName.ToUpper() == "MASTERYI")
            {
                if (Q.IsReady())
                {
                    Q.CastOnUnit(mob1);
                }
                if (W.IsReady() && Player.HealthPercentage() < JeonAutoJungleMenu.Item("yi_W").GetValue<Slider>().Value)
                {
                    W.Cast();
                }
                if (E.IsReady())
                {
                    E.Cast();
                }
                if (R.IsReady())
                {
                    R.Cast();
                }
            }
            else if (Player.ChampionName.ToUpper() == "MAOKAI")
            {
                if (Q.IsReady())
                {
                    Q.Cast(mob1.Position);
                }
                if (E.IsReady())
                {
                    E.Cast(mob1.Position);
                }
                if (W.IsReady())
                {
                    W.CastOnUnit(mob1);
                }
            }
            else if (Player.ChampionName.ToUpper() == "NASUS")
            {
                if (Q.IsReady())
                {
                    Q.Cast();
                }
                if (W.IsReady() && mob1.IsValid<Obj_AI_Hero>())
                {
                    W.CastOnUnit(mob1);
                }
                if (E.IsReady())
                {
                    E.Cast(mob1.Position);
                }
                if (R.IsReady())
                {
                    R.Cast();
                }
            }
            else
            {
                foreach (var spell in cast2hero)
                {
                    if (spell.IsReady())
                    {
                        spell.CastOnUnit(mob1);
                    }
                    if (spell.IsReady())
                    {
                        spell.Cast();
                    }
                    if (spell.IsReady())
                    {
                        spell.Cast(mob1.Position);
                    }
                }
            }
        }

        public static void castspell_laneclear(Obj_AI_Base mob1)
        {
            if (Player.ChampionName.ToUpper() == "NUNU")
            {
                if (Q.IsReady())
                {
                    Q.CastOnUnit(mob1);
                }
                if (E.IsReady())
                {
                    E.CastOnUnit(mob1);
                }
                if (W.IsReady())
                {
                    W.Cast();
                }
            }
            else if (Player.ChampionName.ToUpper() == "CHOGATH")
            {
                if (Q.IsReady())
                {
                    Q.Cast(mob1.Position);
                }
                if (W.IsReady())
                {
                    W.Cast(mob1.Position);
                }
                if (R.IsReady() && R.GetDamage(mob1) >= mob1.Health)
                {
                    R.CastOnUnit(mob1);
                }
            }
            else if (Player.ChampionName.ToUpper() == "WARWICK")
            {
                if (W.IsReady())
                {
                    W.Cast();
                }
            }
            else if (Player.ChampionName.ToUpper() == "MASTERYI")
            {
                if (Q.IsReady())
                {
                    Q.CastOnUnit(mob1);
                }
                if (E.IsReady())
                {
                    E.Cast();
                }
            }
            else if (Player.ChampionName.ToUpper() == "MAOKAI")
            {
                if (Q.IsReady())
                {
                    Q.Cast(mob1.Position);
                }
                if (E.IsReady())
                {
                    E.Cast(mob1.Position);
                }
            }
            else if (Player.ChampionName.ToUpper() == "NASUS")
            {
                if (Q.IsReady() && CheckNasusQDamage(mob1))
                {
                    Q.Cast();
                }
                if (W.IsReady() && mob1.IsValid<Obj_AI_Hero>())
                {
                    W.CastOnUnit(mob1);
                }
                if (E.IsReady())
                {
                    E.Cast(mob1.Position);
                }
            }
            else
            {
                foreach (var spell in cast4laneclear)
                {
                    if (spell.IsReady())
                    {
                        spell.CastOnUnit(mob1);
                    }
                    if (spell.IsReady())
                    {
                        spell.Cast();
                    }
                    if (spell.IsReady())
                    {
                        spell.Cast(mob1.Position);
                    }
                }
            }
        }

        public static bool CheckNasusQDamage(Obj_AI_Base target)
        {
            float QDmg =
                Convert.ToSingle(
                    Q.GetDamage(target) +
                    Player.CalcDamage(
                        target, Damage.DamageType.Physical, Player.BaseAttackDamage + Player.FlatPhysicalDamageMod));
            if (QDmg >= target.Health)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static float GetSpellRange(SpellDataInst targetSpell, bool IsChargedSkill = false)
        {
            if (targetSpell.SData.CastRangeDisplayOverride <= 0)
            {
                if (targetSpell.SData.CastRange <= 0)
                {
                    return targetSpell.SData.CastRadius;
                }
                else
                {
                    if (!IsChargedSkill)
                    {
                        return targetSpell.SData.CastRange;
                    }
                    else
                    {
                        return targetSpell.SData.CastRadius;
                    }
                }
            }
            else
            {
                return targetSpell.SData.CastRangeDisplayOverride;
            }
        }

        #endregion spell methods

        #region 스마이트함수 - Smite Function

        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3724, 3723, 3933 };
        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719, 3932 };
        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714, 3931 };
        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707, 3930 };

        private static readonly string[] MinionNames =
        {
            "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak",
            "SRU_Red", "SRU_Krug", "SRU_Dragon", "SRU_BaronSpawn", "Sru_Crab"
        };

        public static void setSmiteSlot()
        {
            foreach (var spell in
                Player.Spellbook.Spells.Where(
                    spell => String.Equals(spell.Name, smitetype(), StringComparison.CurrentCultureIgnoreCase)))
            {
                smiteSlot = spell.Slot;
                smite = new Spell(smiteSlot, 700);
                return;
            }
        }

        public static string smitetype()
        {
            if (Player.InventoryItems.Any(item => SmiteBlue.Any(t => t == Convert.ToInt32(item.Id))))
            {
                return "s5_summonersmiteplayerganker";
            }
            if (Player.InventoryItems.Any(item => SmiteRed.Any(t => t == Convert.ToInt32(item.Id))))
            {
                return "s5_summonersmiteduel";
            }
            if (Player.InventoryItems.Any(item => SmiteGrey.Any(t => t == Convert.ToInt32(item.Id))))
            {
                return "s5_summonersmitequick";
            }
            if (Player.InventoryItems.Any(item => SmitePurple.Any(t => t == Convert.ToInt32(item.Id))))
            {
                return "itemsmiteaoe";
            }
            return "summonersmite";
        }

        public static double setSmiteDamage()
        {
            int level = Player.Level;
            int[] damage = { 20 * level + 370, 30 * level + 330, 40 * level + 240, 50 * level + 100 };
            return damage.Max();
        }

        public static double smiteDamage(Obj_AI_Base target)
        {
            return Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Smite);
        }

        public static Obj_AI_Base GetNearest(Vector3 pos)
        {
            var minions =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(
                        minion =>
                            minion.IsValid && minion.IsEnemy && !minion.IsDead &&
                            MinionNames.Any(
                                name => minion.Name.StartsWith(name) && Player.Distance(minion.Position) <= 1000))
                    .OrderByDescending(m => m.MaxHealth);
            return minions.FirstOrDefault();
        }

        public static Obj_AI_Base GetNearest_big(Vector3 pos)
        {
            return
                MinionManager.GetMinions(1000, MinionTypes.All, MinionTeam.NotAlly)
                    .Where(
                        minion =>
                            minion.IsValid && minion.IsEnemy && !minion.IsDead &&
                            MinionNames.Any(name => minion.Name.StartsWith(name)) &&
                            !MinionNames.Any(name => minion.Name.Contains("Mini")))
                    .FirstOrDefault();
        }

        public static bool CheckMonster(String TargetName, Vector3 BasePosition, int Range = 500)
        {
            var minion =
                MinionManager.GetMinions(Player.Position, Range, MinionTypes.All, MinionTeam.NotAlly)
                    .FirstOrDefault(mini => mini.IsValidTarget() && mini.Name.StartsWith(TargetName));
            if (minion != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region GetItemTree

        public static void GetItemTree(string type)
        {
            if (type == "AP")
            {
                buyThings.Clear();
                buyThings = buyThings_AP;
                Game.PrintChat("Set ItemTree for AP - Finished");
            }
            else if (type == "AS")
            {
                buyThings.Clear();
                buyThings = buyThings_AS;
                Game.PrintChat("Set ItemTree for AS - Finished");
            }
            else if (type == "TANK")
            {
                buyThings.Clear();
                buyThings = buyThings_TANK;
                Game.PrintChat("Set ItemTree for TANK - Finished");
            }
            else if (type == "X")
            {
                Game.PrintChat("PLZ TYPE VALID VALUE, SET AD ITEMTREE - ERROR");
            }
            else
            {
                Game.PrintChat("Set ItemTree for AD - Finished");
            }
        }

        #endregion

        public static float GetComboDMG(Obj_AI_Hero source, Obj_AI_Hero target)
        {
            double result = 0;
            double basicDmg = 0;
            int attacks = (int) Math.Floor(source.AttackSpeedMod * 5);
            for (int i = 0; i < attacks; i++)
            {
                if (source.Crit > 0)
                {
                    basicDmg += source.GetAutoAttackDamage(target) * (1 + source.Crit / attacks);
                }
                else
                {
                    basicDmg += source.GetAutoAttackDamage(target);
                }
            }
            result += basicDmg;
            var spells = source.Spellbook.Spells;
            foreach (var spell in spells)
            {
                var t = spell.CooldownExpires - Game.Time;
                if (t < 0.5)
                {
                    switch (source.SkinName)
                    {
                        case "Ahri":
                            if (spell.Slot == SpellSlot.Q)
                            {
                                result += (Damage.GetSpellDamage(source, target, spell.Slot));
                                result += (Damage.GetSpellDamage(source, target, spell.Slot, 1));
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Akali":
                            if (spell.Slot == SpellSlot.R)
                            {
                                result += (Damage.GetSpellDamage(source, target, spell.Slot) * spell.Ammo);
                            }
                            else if (spell.Slot == SpellSlot.Q)
                            {
                                result += (Damage.GetSpellDamage(source, target, spell.Slot));
                                result += (Damage.GetSpellDamage(source, target, spell.Slot, 1));
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Amumu":
                            if (spell.Slot == SpellSlot.W)
                            {
                                result += (Damage.GetSpellDamage(source, target, spell.Slot) * 5);
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Cassiopeia":
                            if (spell.Slot == SpellSlot.Q || spell.Slot == SpellSlot.E || spell.Slot == SpellSlot.W)
                            {
                                result += (Damage.GetSpellDamage(source, target, spell.Slot) * 2);
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Fiddlesticks":
                            if (spell.Slot == SpellSlot.W || spell.Slot == SpellSlot.E)
                            {
                                result += (Damage.GetSpellDamage(source, target, spell.Slot) * 5);
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Garen":
                            if (spell.Slot == SpellSlot.E)
                            {
                                result += (Damage.GetSpellDamage(source, target, spell.Slot) * 3);
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Irelia":
                            if (spell.Slot == SpellSlot.W)
                            {
                                result += (Damage.GetSpellDamage(source, target, spell.Slot) * attacks);
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Karthus":
                            if (spell.Slot == SpellSlot.Q)
                            {
                                result += (Damage.GetSpellDamage(source, target, spell.Slot) * 4);
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "KogMaw":
                            if (spell.Slot == SpellSlot.W)
                            {
                                result += (Damage.GetSpellDamage(source, target, spell.Slot) * attacks);
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "LeeSin":
                            if (spell.Slot == SpellSlot.Q)
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                                result += Damage.GetSpellDamage(source, target, spell.Slot, 1);
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Lucian":
                            if (spell.Slot == SpellSlot.R)
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot) * 4;
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Nunu":
                            if (spell.Slot != SpellSlot.R && spell.Slot != SpellSlot.Q)
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "MasterYi":
                            if (spell.Slot == SpellSlot.E)
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot) * attacks;
                            }
                            else if (spell.Slot == SpellSlot.R)
                            {
                                result += basicDmg * 0.6f;
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "MonkeyKing":
                            if (spell.Slot == SpellSlot.R)
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot) * 4;
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Pantheon":
                            if (spell.Slot == SpellSlot.E)
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot) * 3;
                            }
                            else if (spell.Slot == SpellSlot.R)
                            {
                                result += 0;
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }

                            break;
                        case "Rammus":
                            if (spell.Slot == SpellSlot.R)
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot) * 6;
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Riven":
                            if (spell.Slot == SpellSlot.Q)
                            {
                                result += RivenDamageQ(spell, source, target);
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Viktor":
                            if (spell.Slot == SpellSlot.R)
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                                result += Damage.GetSpellDamage(source, target, spell.Slot, 1) * 5;
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        case "Vladimir":
                            if (spell.Slot == SpellSlot.E)
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot) * 2;
                            }
                            else
                            {
                                result += Damage.GetSpellDamage(source, target, spell.Slot);
                            }
                            break;
                        default:
                            result += Damage.GetSpellDamage(source, target, spell.Slot);
                            break;
                    }
                }
            }
            if (source.Spellbook.CanUseSpell(target.GetSpellSlot("summonerdot")) == SpellState.Ready)
            {
                result += source.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            }
            return (float) result;
        }

        private static float RivenDamageQ(SpellDataInst spell, Obj_AI_Hero src, Obj_AI_Hero dsc)
        {
            double dmg = 0;
            if (spell.IsReady())
            {
                dmg += src.CalcDamage(
                    dsc, Damage.DamageType.Physical,
                    (-10 + (spell.Level * 20) +
                     (0.35 + (spell.Level * 0.05)) * (src.FlatPhysicalDamageMod + src.BaseAttackDamage)) * 3);
            }
            return (float) dmg;
        }
    }
}