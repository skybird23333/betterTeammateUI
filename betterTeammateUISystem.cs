using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria.ID;

namespace betterTeammateUI
{
    public class betterTeammateUISystem : ModSystem
    {
        public static betterTeammateUISystem Instance;
        internal UserInterface MyInterface;
        internal MyModUI MyUI;
        private double syncTimer = 0;
        private const double SyncInterval = 0.5; // 每0.5秒同步一次
        private GameTime lastGameTime;

        private PlayerState lastSentState = null;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                MyUI = new MyModUI();
                MyUI.Activate();
                MyInterface = new UserInterface();
                MyInterface.SetState(MyUI);
            }
        }

        public override void OnModLoad()
        {
            Instance = this;
        }

        public override void OnModUnload()
        {
            Instance = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            lastGameTime = gameTime;
            MyInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryLayerIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryLayerIndex != -1)
            {
                layers.Insert(inventoryLayerIndex, new LegacyGameInterfaceLayer(
                    "MyMod: UI",
                    delegate
                    {
                        MyInterface.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        public override void PostUpdatePlayers()
        {
            // 客户端定时广播本地玩家状态的逻辑已移除，由UI主动同步
        }

        // 修正netmode判断
        public void SendLocalPlayerState(PlayerState currentState)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient) return;
            lastSentState = currentState.Clone();
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)0); // 0: 玩家状态同步
            // 只写入 Name, IsDead, Health, MaxHealth, DPS, HasPotionSickness
            currentState.Write(packet);
            packet.Send();
        }
    }
}