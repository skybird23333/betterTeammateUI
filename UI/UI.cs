using Terraria.UI;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Collections.Generic;
using Terraria.GameContent;

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
        public int DPS;
        // RespawnLeft 字段移除
        // 你可以根据需要扩展更多字段
        public static PlayerState Read(System.IO.BinaryReader reader)
        {
            return new PlayerState
            {
                Name = reader.ReadString(),
                IsDead = reader.ReadBoolean(),
                RespawnTime = reader.ReadInt32(),
                Health = reader.ReadInt32(),
                DPS = reader.ReadInt32()
            };
        }

        public void Write(System.IO.BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(IsDead);
            writer.Write(RespawnTime);
            writer.Write(Health);
            writer.Write(DPS);
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
                DPS = DPS
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
                    bgColor = new Color(60, 60, 60);
                }
                else
                {
                    Player player = Main.player.FirstOrDefault(p => p?.name == State.Name);
                    int maxHp = player?.statLifeMax2 ?? 400;
                    percent = MathHelper.Clamp(State.Health / (float)Math.Max(1, maxHp), 0f, 1f);
                    fgColor = Color.Red;
                    bgColor = Color.Black;
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
                // 死亡瞬间才同步respawnTimer
                int syncRespawnTime = curDead && !lastDead ? curRespawnTime : 0;
                var state = new PlayerState
                {
                    Name = player.name,
                    IsDead = curDead,
                    RespawnTime = syncRespawnTime,
                    DeathTime = curDead ? Main.GameUpdateCount : 0,
                    Health = curHealth,
                    DPS = curDps
                };
                betterTeammateUISystem.Instance.SendLocalPlayerState(state);
                lastLocalState = state.Clone();
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

        private void RefreshPlayerStates()
        {
            playerStates.Clear();
            foreach (var player in Main.ActivePlayers)
            {
                playerStates[player.name] = new PlayerState
                {
                    Name = player.name,
                    IsDead = player.dead,
                    RespawnTime = player.respawnTimer,
                    DeathTime = player.dead ? Main.GameUpdateCount : 0,
                    Health = player.statLife,
                    DPS = 0 // 你可以根据需要计算DPS
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

        public void OnPlayerHealthChange(string name, int health)
        {
            if (playerStates.TryGetValue(name, out var state))
            {
                state.Health = health;
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

        public void RefreshTeamPlayers()
        {
            RefreshPlayerStates();
            UpdateUI();
        }

        public void UpdateUI()
        {
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
                // 跳过本地玩家
                if (state.Name == Main.LocalPlayer?.name)
                    continue;
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

                    // 文本
                    elements.NameText = new UIText(state.Name);
                    elements.NameText.Left.Set(10, 0f);
                    elements.NameText.Top.Set(0, 0f);
                    elements.Panel.Append(elements.NameText);

                    elements.HpText = new UIText("");
                    elements.HpText.Left.Set(-20f, 1f);
                    elements.HpText.Top.Set(0, 0f);
                    elements.Panel.Append(elements.HpText);

                    // elements.DpsText = new UIText("");
                    // elements.DpsText.Left.Set(200, 0f);
                    // elements.DpsText.Top.Set(0, 0f);
                    // elements.Panel.Append(elements.DpsText);

                    elements.RespawnText = new UIText("");
                    elements.RespawnText.Left.Set(-20f, 1f);
                    elements.RespawnText.Top.Set(0, 0f);
                    elements.Panel.Append(elements.RespawnText);

                    panel.Append(elements.Panel);

                    playerPanelElements[state.Name] = elements;
                }

                Player player = Main.player.FirstOrDefault(p => p?.name == state.Name);


                elements.Panel.Top.Set(y, 0f);
                ((PlayerPanel)elements.Panel).State = state;
                elements.Panel.BackgroundColor = state.IsDead ? new Color(30, 30, 30) : new Color(90, 90, 90);
                // 更新文本
                elements.NameText.SetText(state.Name);
                elements.NameText.TextColor = state.IsDead ? Color.Red : Color.White;
                if (state.IsDead && state.RespawnTime > 0)
                {
                    int remaining = Math.Max(0, (state.RespawnTime - (int)(Main.GameUpdateCount - state.DeathTime)) / 60);
                    elements.RespawnText.SetText($"({remaining})");
                    elements.RespawnText.TextColor = Color.Yellow;
                    elements.HpText.SetText("");
                }
                else
                {
                    elements.RespawnText.SetText("");
                    elements.HpText.SetText($"{state.Health}");
                    // elements.DpsText.SetText($"DPS: {state.DPS}");
                }
                y -= 60;
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
            // 只在本地活着→死亡时，记录本地死亡时间
            bool wasDead = localState.IsDead;
            bool changed =
                localState.IsDead != state.IsDead ||
                localState.RespawnTime != state.RespawnTime ||
                localState.Health != state.Health ||
                localState.DPS != state.DPS;
            if (changed)
            {
                if (!wasDead && state.IsDead)
                {
                    // 死亡，记录本地死亡时间（只在本地活着→死亡时设置）
                    localState.DeathTime = Main.GameUpdateCount;
                }
                if (wasDead && !state.IsDead)
                {
                    // 复活，清零
                    localState.DeathTime = 0;
                }
                localState.IsDead = state.IsDead;
                localState.RespawnTime = state.RespawnTime;
                localState.Health = state.Health;
                localState.DPS = state.DPS;
                // 不要用网络包的 DeathTime 覆盖本地
                UpdateUI();
            }
        }
    }
}