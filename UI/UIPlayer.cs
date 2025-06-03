using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;

namespace betterTeammateUI
{
    public class UIPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            // 进入世界时刷新所有玩家状态并更新UI
            betterTeammateUISystem modSystem = ModContent.GetInstance<betterTeammateUISystem>();
            modSystem.MyUI.RefreshTeamPlayers();
        }

        public override void PlayerDisconnect()
        {
            // 离开世界时清理UI状态
            betterTeammateUISystem modSystem = ModContent.GetInstance<betterTeammateUISystem>();
            modSystem.MyUI.RefreshTeamPlayers();
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            // 死亡时只更新状态并刷新UI
            int respawnTime = Player.respawnTimer;
            betterTeammateUISystem modSystem = ModContent.GetInstance<betterTeammateUISystem>();
            modSystem.MyUI.OnPlayerDeath(Player.name, respawnTime);
        }

        public override void OnRespawn()
        {
            // 复活时只更新状态并刷新UI
            betterTeammateUISystem modSystem = ModContent.GetInstance<betterTeammateUISystem>();
            modSystem.MyUI.OnPlayerRespawn(Player.name);
        }

        public override void PostUpdate()
        {
            // 实时同步血量和DPS（如有DPS统计逻辑可补充）
            betterTeammateUISystem modSystem = ModContent.GetInstance<betterTeammateUISystem>();
            modSystem.MyUI.OnPlayerHealthChange(Player.name, Player.statLife);
            // modSystem.MyUI.OnPlayerDPSChange(Player.name, 你的DPS计算);
        }
    }
}