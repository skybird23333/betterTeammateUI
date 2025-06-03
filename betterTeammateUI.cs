using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria;
using Terraria.UI;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria.ID;

namespace betterTeammateUI
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class betterTeammateUI : Mod
	{
		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			byte msgType = reader.ReadByte();
			if (msgType == 0)
			{
				// 你可以扩展更多字段
				if (Main.netMode != NetmodeID.Server && betterTeammateUISystem.Instance?.MyUI != null)
				{
					var state = PlayerState.Read(reader);
					betterTeammateUISystem.Instance.MyUI.OnNetworkPlayerState(state);
				}
				// 服务器转发给其他客户端
				if (Main.netMode == NetmodeID.Server)
				{
					var state = PlayerState.Read(reader);
					ModPacket packet = GetPacket();
					packet.Write((byte)0);
					// 只转发 Name, IsDead, Health, MaxHealth, DPS, HasPotionSickness
					state.Write(packet);
					packet.Send(-1, whoAmI);
				}
			}
		}
	}
}