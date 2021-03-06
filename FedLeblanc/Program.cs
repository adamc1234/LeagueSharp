﻿#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace Leblanc
{
    internal class Program
    {
        public const string ChampionName = "Leblanc";

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static SpellSlot IgniteSlot;
        public static Items.Item DFG;

        public static Menu Config;

        private static Obj_AI_Hero Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 700);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 950);
            R = new Spell(SpellSlot.R, 600);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            DFG = Utility.Map.GetMap() == Utility.Map.MapType.TwistedTreeline ? new Items.Item(3188, 750) : new Items.Item(3128, 750);

            W.SetSkillshot(0.25f, 240, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 60f, 1200f, true, SkillshotType.SkillshotLine);

            SpellList.AddRange(new[] { Q, W, E, R });

            Config = new Menu(ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("harassToggleQ", "Use Q (toggle)").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Toggle)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuItem("UseQFarm", "Use Q").SetValue(
                        new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 1)));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuItem("UseWFarm", "Use W").SetValue(
                        new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 3)));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuItem("UseEFarm", "Use E").SetValue(
                        new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 3)));
            Config.SubMenu("Farm")
                .AddItem(new MenuItem("ManaFarm", "Min Mana").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Farm")
                .AddItem(new MenuItem("waveNumW", "Minions to hit with W").SetValue<Slider>(new Slider(4, 1, 10)));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuItem("FreezeActive", "Freeze!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after a rotation").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit += hero => (float)(DamageLib.getDmg(hero, DamageLib.SpellType.Q) + DamageLib.getDmg(hero, DamageLib.SpellType.W) + DamageLib.getDmg(hero, DamageLib.SpellType.E) + DamageLib.getDmg(hero, DamageLib.SpellType.W));
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(dmgAfterComboItem);
            Config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            Orbwalking.BeforeAttack += OrbwalkingOnBeforeAttack;
            Game.PrintChat(ChampionName + " Loaded!");
        }

        private static void OrbwalkingOnBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
                args.Process = !(Q.IsReady() || W.IsReady() || E.IsReady() || R.IsReady() || Player.Distance(args.Target) >= 600);
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            E.Cast(gapcloser.Sender);
        }
        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            E.Cast(unit);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                {
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();

                if (Config.Item("harassToggleQ").GetValue<bool>())
                    ToggleHarass();

                var lc = Config.Item("LaneClearActive").GetValue<KeyBind>().Active;
                if (lc || Config.Item("FreezeActive").GetValue<KeyBind>().Active)
                    Farm(lc);

                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                    JungleFarm();

            }
        }

        private static void Combo()
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (target != null)
            {
                if (Player.Distance(target) <= 600)
                {
                    if (Q.IsReady())
                    {
                        Q.CastOnUnit(target);
                    }
                    else if (DFG.IsReady() && W.IsReady() && R.IsReady())
                    {
                        DFG.Cast(target);
                    }
                    else if (W.IsReady())
                    {
                        W.CastOnUnit(target);
                    }
                    else if (R.IsReady())
                    {
                        R.CastOnUnit(target);
                    }
                    else if (E.IsReady())
                    {
                        PredictionOutput ePred = E.GetPrediction(target);
                        if (ePred.Hitchance >= HitChance.High)
                            E.Cast(ePred.CastPosition);
                    }
                    //Ignite
                    if (Player.Spellbook.GetSpell(Player.GetSpellSlot("SummonerDot")).State == SpellState.Ready)
                    {
                        var igniteDmg = DamageLib.getDmg(target, DamageLib.SpellType.IGNITE);
                        if (igniteDmg > target.Health)
                        {
                            Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
                        }
                    }

                }
                else if (DamageLib.getDmg(target, DamageLib.SpellType.Q) > target.Health)
                {
                    Q.CastOnUnit(target);
                }

            }
        }

        private static void ToggleHarass()
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (target != null && Q.IsReady())
            {
                Q.CastOnUnit(target);
            }

        }

        private static void Harass()
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (target != null && Config.Item("UseQHarass").GetValue<bool>())
            {
                Q.CastOnUnit(target);
            }
            if (target != null && Config.Item("UseWHarass").GetValue<bool>())
            {
                W.CastOnUnit(target);
            }
            if (target != null && Config.Item("UseEHarass").GetValue<bool>())
            {
                PredictionOutput ePred = E.GetPrediction(target);
                if (ePred.Hitchance >= HitChance.High)
                    E.Cast(ePred.CastPosition);
            }
        }

        private static void Farm(bool laneClear)
        {
            if (!Orbwalking.CanMove(40)) return;
            var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);
            var minions = MinionManager.GetMinions(Player.Position, W.Range + W.Width / 2);
            var useQi = Config.Item("UseQFarm").GetValue<StringList>().SelectedIndex;
            var useWi = Config.Item("UseWFarm").GetValue<StringList>().SelectedIndex;
            var useEi = Config.Item("UseEFarm").GetValue<StringList>().SelectedIndex;

            var FMana = Config.Item("ManaFarm").GetValue<Slider>().Value;
            var MPercent = Player.Mana * 100 / Player.MaxMana;

            var useQ = (laneClear && (useQi == 1 || useQi == 2)) || (!laneClear && (useQi == 0 || useQi == 2));
            var useW = (laneClear && (useWi == 1 || useWi == 2)) || (!laneClear && (useWi == 0 || useWi == 2));
            var useE = (laneClear && (useEi == 1 || useEi == 2)) || (!laneClear && (useEi == 0 || useEi == 2));

            if (useQ && Q.IsReady() && MPercent >= FMana)
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget() &&
                        HealthPrediction.GetHealthPrediction(minion,
                            (int)(Player.Distance(minion) * 1000 / 1400)) <
                         DamageLib.getDmg(minion, DamageLib.SpellType.Q) - 10)
                    {
                        Q.CastOnUnit(minion);
                        return;
                    }
                }
            }
            else if (useW && W.IsReady() && MPercent >= FMana)
            {

                var farmLocation = MinionManager.GetBestCircularFarmLocation(minions.Select(minion => minion.ServerPosition.To2D()).ToList(), W.Width, W.Range);

                if (farmLocation.MinionsHit >= Config.Item("waveNumW").GetValue<Slider>().Value)
                    W.Cast(farmLocation.Position);
            }
            else if (useE && E.IsReady() && MPercent >= FMana)
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget(E.Range) &&
                        HealthPrediction.GetHealthPrediction(minion,
                            (int)(Player.Distance(minion) * 1000 / 1000)) <
                        DamageLib.getDmg(minion, DamageLib.SpellType.E) - 10)
                    {
                        E.CastOnUnit(minion);
                        return;
                    }
                }
            }

            if (laneClear)
            {
                foreach (var minion in allMinions)
                {
                    if (useQ)
                        Q.CastOnUnit(minion);

                    if (useW)
                        W.CastOnUnit(minion);

                    if (useE)
                        E.CastOnUnit(minion);
                }
            }
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                Q.CastOnUnit(mob);
                W.CastOnUnit(mob);
                E.CastOnUnit(mob);
            }
        }
    }
}