using Terraria.UI;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Collections.Generic;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ID;
using betterTeammateUI;

namespace betterTeammateUI
{
    public class PlayerState
    {
        public string Name;
        public bool IsDead;
        // The time it takes to respawn, STATIC
        public int RespawnTime;
        // The global tick count when the player died, used to calculate respawn time
        public uint DeathTime;
        public int Health;
        public int MaxHealth;
        public int DPS;
        public bool HasPotionSickness;
        public int HeldItemType; // 新增：持有物品类型
        // RespawnLeft 字段移除
        // 你可以根据需要扩展更多字段
        public static PlayerState Read(System.IO.BinaryReader reader)
        {
            return new PlayerState
            {
                Name = reader.ReadString(),
                IsDead = reader.ReadBoolean(),
                RespawnTime = 0,
                Health = reader.ReadInt32(),
                MaxHealth = reader.ReadInt32(),
                DPS = reader.ReadInt32(),
                HasPotionSickness = reader.ReadBoolean(),
                HeldItemType = reader.ReadInt32() // 新增
            };
        }

        public void Write(System.IO.BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(IsDead);
            writer.Write(Health);
            writer.Write(MaxHealth);
            writer.Write(DPS);
            writer.Write(HasPotionSickness);
            writer.Write(HeldItemType); // 新增
        }

        public PlayerState Clone()
        {
            return new PlayerState
            {
                Name = Name,
                IsDead = IsDead,
                RespawnTime = RespawnTime,
                DeathTime = DeathTime,
                Health = Health,
                MaxHealth = MaxHealth,
                DPS = DPS,
                HasPotionSickness = HasPotionSickness,
                HeldItemType = HeldItemType // 新增
            };
        }
    }

    public class MyModUI : UIState
    {
        public UIElement panel;
        private Dictionary<string, PlayerState> playerStates = new Dictionary<string, PlayerState>();
        private Dictionary<string, UIPanel> playerPanels = new Dictionary<string, UIPanel>();
        private double respawnUpdateTimer = 0;

        // 玩家面板结构体，便于缓存控件引用
        private class PlayerPanelElements
        {
            public PlayerPanel Panel;
            public UIElement BarContainer;
            public UIImage BarFrame;
            public UIText NameText;
            public UIText HpText;
            public UIText DpsText;
            public UIText RespawnText;
            public UIText PotionSicknessText;
            public UIImage PotionSicknessIcon;
            public ItemImage WeaponIcon; // 新增：武器图标
            public float LastPercent;
            public bool LastIsDead;
            public Color LastFgColor;
            public Color LastBgColor;
        }

        // 玩家面板类
        private class PlayerPanel : UIPanel
        {
            public PlayerState State;
            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);
                if (State == null) return;
                float percent;
                Color fgColor, bgColor;
                if (State.IsDead)
                {
                    int ticksPassed = (int)(Main.GameUpdateCount - State.DeathTime);
                    percent = MathHelper.Clamp((float)ticksPassed / Math.Max(1, State.RespawnTime), 0f, 1f);
                    fgColor = Color.White;
                    bgColor = new Color(60, 60, 60, 180);
                }
                else
                {
                    int maxHp = State.MaxHealth > 0 ? State.MaxHealth : 400;
                    percent = MathHelper.Clamp(State.Health / (float)Math.Max(1, maxHp), 0f, 1f);
                    fgColor = Color.Red;
                    bgColor = new Color(0, 0, 0, 180);
                }
                Rectangle hitbox = GetInnerDimensions().ToRectangle();
                hitbox.X += 2;
                hitbox.Width -= 4;
                hitbox.Y += 22;
                hitbox.Height = 10;
                int left = hitbox.Left;
                int right = hitbox.Right;
                int steps = (int)((right - left) * percent);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(left, hitbox.Y, right - left, hitbox.Height), bgColor);
                for (int i = 0; i < steps; i++)
                {
                    float grad = (float)i / (right - left);
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(left + i, hitbox.Y, 1, hitbox.Height), fgColor);
                }
            }
        }
        private Dictionary<string, PlayerPanelElements> playerPanelElements = new();

        public override void OnInitialize()
        {
            panel = new UIElement();
            panel.Left.Set(20f, 0f); // 向左移动
            panel.Top.Set(0f, 0.6f);
            panel.Width.Set(240f, 0f); // 加宽
            panel.Height.Set(400f, 0f);

            Append(panel);

            RefreshPlayerStates();
            UpdateUI();
        }

        private PlayerState lastLocalState = null;
        private bool lastDead = false;
        private int lastRespawnTime = 0;
        private int lastHealth = 0;
        private int lastDps = 0;

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // 本地玩家状态变化检测与同步
            var player = Main.LocalPlayer;
            bool curDead = player.dead;
            int curRespawnTime = player.respawnTimer;
            int curHealth = player.statLife;
            int curDps = 0; // 如有DPS统计可补充
            bool hasPotionSickness = player.HasBuff(21); // 21是Potion Sickness的BuffID

            bool needSync = false;
            if (lastLocalState == null)
            {
                needSync = true;
            }
            else if (lastDead != curDead)
            {
                // 死亡或复活瞬间
                needSync = true;
            }
            else if (!curDead && lastHealth != curHealth)
            {
                // 活着时血量变化
                needSync = true;
            }
            // 可根据需要添加DPS变化检测

            if (needSync && betterTeammateUISystem.Instance != null)
            {
                SyncLocalPlayerState();
            }
            lastDead = curDead;
            lastRespawnTime = curRespawnTime;
            lastHealth = curHealth;
            lastDps = curDps;

            respawnUpdateTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (respawnUpdateTimer >= 1.0)
            {
                UpdateUI();
                respawnUpdateTimer = 0;
            }
        }

        public void SyncLocalPlayerState()
        {
            if (betterTeammateUISystem.Instance == null) return;
            var player = Main.LocalPlayer;
            int syncRespawnTime = player.dead && !lastDead ? player.respawnTimer : 0;
            var state = new PlayerState
            {
                Name = player.name,
                IsDead = player.dead,
                RespawnTime = player.respawnTimer,
                DeathTime = player.dead ? Main.GameUpdateCount : 0,
                Health = player.statLife,
                MaxHealth = player.statLifeMax2,
                DPS = 0, // 如有DPS统计可补充
                HasPotionSickness = player.HasBuff(21), // 21是Potion Sickness的BuffID
                HeldItemType = player.HeldItem?.type ?? ItemID.None // 新增：同步持有物品类型
            };
            betterTeammateUISystem.Instance.SendLocalPlayerState(state);
            lastLocalState = state.Clone();
        }

        private void RefreshPlayerStates()
        {
            playerStates.Clear();
            foreach (var player in Main.ActivePlayers)
            {
                // 根据配置决定是否跳过本地玩家
                bool showSelf = false;
                try
                {
                    showSelf = ModContent.GetInstance<BetterTeammateUIClientConfig>().ShowSelfInUI;
                }
                catch { }
                if (!showSelf && player == Main.LocalPlayer) continue;
                playerStates[player.name] = new PlayerState
                {
                    Name = player.name,
                    IsDead = player.dead,
                    RespawnTime = player.respawnTimer,
                    DeathTime = player.dead ? Main.GameUpdateCount : 0,
                    Health = player.statLife,
                    MaxHealth = player.statLifeMax2,
                    DPS = 0, // 你可以根据需要计算DPS
                    HeldItemType = player.HeldItem?.type ?? ItemID.None // 新增
                };
            }
        }

        public void OnPlayerDeath(string name, int respawnTime)
        {
            if (playerStates.TryGetValue(name, out var state))
            {
                state.IsDead = true;
                state.RespawnTime = respawnTime;
                state.DeathTime = Main.GameUpdateCount;
            }
            else
            {
                playerStates[name] = new PlayerState
                {
                    Name = name,
                    IsDead = true,
                    RespawnTime = respawnTime,
                    DeathTime = Main.GameUpdateCount,
                    Health = 0,
                    DPS = 0
                };
            }
            UpdateUI();
        }

        public void OnPlayerRespawn(string name)
        {
            if (playerStates.TryGetValue(name, out var state))
            {
                state.IsDead = false;
                state.RespawnTime = 0;
                state.DeathTime = 0;
            }
            UpdateUI();
        }

        public void OnPlayerHealthChange(string name, int health, int maxHealth)
        {
            if (playerStates.TryGetValue(name, out var state))
            {
                state.Health = health;
                state.MaxHealth = maxHealth;
            }
            UpdateUI();
        }

        public void OnPlayerDPSChange(string name, int dps)
        {
            if (playerStates.TryGetValue(name, out var state))
            {
                state.DPS = dps;
            }
            UpdateUI();
        }

        public void OnPlayerPotionSicknessChange(string name, bool hasPotionSickness)
        {
            if (playerStates.TryGetValue(name, out var state))
            {
                state.HasPotionSickness = hasPotionSickness;
            }
            else
            {
                playerStates[name] = new PlayerState
                {
                    Name = name,
                    HasPotionSickness = hasPotionSickness
                };
            }
            UpdateUI();
        }

        public void RefreshTeamPlayers()
        {
            RefreshPlayerStates();
            UpdateUI();
        }

        public void UpdateUI()
        {
            // 获取自定义复活时间配置
            int customRespawn = 0;
            try
            {
                customRespawn = ModContent.GetInstance<BetterTeammateUIClientConfig>().CustomRespawnTime;
            }
            catch { }
            // 先移除不存在的玩家面板
            var toRemove = playerPanelElements.Keys.Except(playerStates.Keys).ToList();
            foreach (var name in toRemove)
            {
                if (playerPanelElements.TryGetValue(name, out var e))
                    panel.RemoveChild(e.Panel);
                playerPanelElements.Remove(name);
            }

            int y = 10;
            foreach (var state in playerStates.Values)
            {
                PlayerPanelElements elements;
                if (!playerPanelElements.TryGetValue(state.Name, out elements))
                {
                    elements = new PlayerPanelElements();
                    elements.Panel = new PlayerPanel();
                    elements.Panel.State = state;
                    elements.Panel.Width.Set(300, 0f);
                    elements.Panel.Height.Set(60, 0f);
                    elements.Panel.BackgroundColor = Color.Transparent;
                    elements.Panel.Left.Set(10, 0f);
                    elements.Panel.Top.Set(y, 0f);
                    elements.Panel.BorderColor = new Color(0, 0, 0, 200);

                    // 文本
                    elements.NameText = new UIText(state.Name);
                    elements.NameText.Left.Set(0, 0f);
                    elements.NameText.Top.Set(0, 0f);
                    elements.Panel.Append(elements.NameText);

                    elements.HpText = new UIText("");
                    elements.HpText.Left.Set(-20f, 1f);
                    elements.HpText.Top.Set(0, 0f);
                    elements.Panel.Append(elements.HpText);

                    elements.RespawnText = new UIText("");
                    elements.RespawnText.Left.Set(-20f, 1f);
                    elements.RespawnText.Top.Set(0, 0f);
                    elements.Panel.Append(elements.RespawnText);

                    elements.PotionSicknessText = null; // 不再用文本
                    elements.PotionSicknessIcon = new UIImage(Terraria.GameContent.TextureAssets.Buff[BuffID.PotionSickness]);
                    elements.PotionSicknessIcon.Left.Set(-80f, 1f);
                    elements.PotionSicknessIcon.Top.Set(-10f, 0f);
                    elements.PotionSicknessIcon.ImageScale = 0.75f;
                    elements.PotionSicknessIcon.Recalculate();
                    elements.Panel.Append(elements.PotionSicknessIcon);

                    // 新增：武器图标
                    elements.WeaponIcon = new ItemImage(ItemID.None); // 默认空手
                    elements.WeaponIcon.Left.Set(-45f, 1f);
                    elements.WeaponIcon.Top.Set(-5f, 0f);
                    elements.WeaponIcon.ImageScale = 0.65f;
                    elements.WeaponIcon.Recalculate();
                    elements.Panel.Append(elements.WeaponIcon);

                    panel.Append(elements.Panel);

                    playerPanelElements[state.Name] = elements;
                }

                Player player = Main.player.FirstOrDefault(p => p?.name == state.Name);


                elements.Panel.Top.Set(y, 0f);
                ((PlayerPanel)elements.Panel).State = state;
                elements.Panel.BackgroundColor = state.IsDead ? new Color(30, 30, 30, 180) : new Color(90, 90, 90, 180);
                // 更新文本
                elements.NameText.SetText(state.Name);
                elements.NameText.TextColor = state.IsDead ? Color.Red : Color.White;
                if (state.IsDead && state.RespawnTime > 0)
                {
                    int baseRespawn = state.RespawnTime;
                    if (customRespawn > 0) baseRespawn = customRespawn * 60;
                    int remaining = Math.Max(0, (baseRespawn - (int)(Main.GameUpdateCount - state.DeathTime)) / 60);
                    elements.RespawnText.SetText($"({remaining})");
                    elements.RespawnText.TextColor = Color.Yellow;
                    elements.HpText.SetText("");
                }
                else
                {
                    elements.RespawnText.SetText("");
                    elements.HpText.SetText($"{state.Health}");
                }
                // 药水病显示
                if (elements.PotionSicknessIcon != null)
                {
                    if (state.HasPotionSickness)
                    {
                        if (!elements.Panel.HasChild(elements.PotionSicknessIcon))
                            elements.Panel.Append(elements.PotionSicknessIcon);
                    }
                    else
                    {
                        if (elements.Panel.HasChild(elements.PotionSicknessIcon))
                            elements.Panel.RemoveChild(elements.PotionSicknessIcon);
                    }
                }

                // 武器图标显示
                if (elements.WeaponIcon != null)
                {
                    // 获取是否显示武器图标的配置
                    bool showWeaponIcon = false;
                    try
                    {
                        showWeaponIcon = ModContent.GetInstance<BetterTeammateUIClientConfig>().ShowWeaponIcon;
                    }
                    catch { }

                    int itemType = ItemID.None;
                    if (player != null && player.HeldItem != null && player.HeldItem.type != ItemID.None)
                    {
                        itemType = player.HeldItem.type;
                    }
                    else if (state.HeldItemType != ItemID.None)
                    {
                        itemType = state.HeldItemType;
                    }
                    if (showWeaponIcon && itemType != ItemID.None)
                    {
                        elements.WeaponIcon.SetItemType(itemType);
                        elements.WeaponIcon.Width.Set(32f * elements.WeaponIcon.ImageScale, 0f);
                        elements.WeaponIcon.Height.Set(32f * elements.WeaponIcon.ImageScale, 0f);
                        if (!elements.Panel.HasChild(elements.WeaponIcon))
                            elements.Panel.Append(elements.WeaponIcon);
                        // 药水病图标位置（武器图标显示时）
                        if (elements.PotionSicknessIcon != null)
                            elements.PotionSicknessIcon.Left.Set(-80f, 1f);
                    }
                    else
                    {
                        if (elements.Panel.HasChild(elements.WeaponIcon))
                            elements.Panel.RemoveChild(elements.WeaponIcon);
                        // 药水病图标位置（武器图标不显示时）
                        if (elements.PotionSicknessIcon != null)
                            elements.PotionSicknessIcon.Left.Set(-55f, 1f);
                    }
                }

                y -= 64;
            }
        }


        // 网络同步：接收其他玩家状态
        public void OnNetworkPlayerState(PlayerState state)
        {
            if (!playerStates.TryGetValue(state.Name, out var localState))
            {
                localState = new PlayerState { Name = state.Name };
                playerStates[state.Name] = localState;
            }
            bool wasDead = localState.IsDead;
            bool changed =
                localState.IsDead != state.IsDead ||
                localState.Health != state.Health ||
                localState.MaxHealth != state.MaxHealth ||
                localState.DPS != state.DPS ||
                localState.HasPotionSickness != state.HasPotionSickness;
            if (changed)
            {
                if (!wasDead && state.IsDead)
                {
                    // 死亡，记录本地死亡时间
                    localState.DeathTime = Main.GameUpdateCount;
                    // 死亡时，远程玩家的 RespawnTime 采用本地玩家的 respawnTimer
                    localState.RespawnTime = Main.LocalPlayer.respawnTimer;
                }
                if (wasDead && !state.IsDead)
                {
                    // 复活，清零
                    localState.DeathTime = 0;
                    localState.RespawnTime = 0;
                }
                localState.IsDead = state.IsDead;
                localState.Health = state.Health;
                localState.MaxHealth = state.MaxHealth;
                localState.DPS = state.DPS;
                localState.HasPotionSickness = state.HasPotionSickness;
                localState.HeldItemType = state.HeldItemType; // 新增
                UpdateUI();
            }
        }
    }
}