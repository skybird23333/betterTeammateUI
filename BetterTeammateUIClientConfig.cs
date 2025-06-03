using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace betterTeammateUI
{
    public class BetterTeammateUIClientConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;
        [DefaultValue(-1)]
        public int CustomRespawnTime;

        [DefaultValue(false)]
        public bool ShowSelfInUI;

        [Label("Show Weapon/Item Icon in UI")]
        [DefaultValue(false)]
        public bool ShowWeaponIcon { get; set; } = false;
    }
}
