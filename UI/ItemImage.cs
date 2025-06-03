using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;
using Terraria.GameContent;
using Terraria.ModLoader.UI.Elements;

namespace betterTeammateUI
{
    public class ItemImage : UIElement
    {
        private int itemType;
        public float ImageScale = 0.8f;
        public ItemImage(int itemType)
        {
            this.itemType = itemType;
            Width.Set(32f * ImageScale, 0f);
            Height.Set(32f * ImageScale, 0f);
        }
        public void SetItemType(int type)
        {
            itemType = type;
        }
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (itemType <= 0) return;
            var texture = TextureAssets.Item[itemType].Value;
            int frameCount = Terraria.Main.itemAnimations[itemType]?.FrameCount ?? 1;
            int frameHeight = texture.Height / frameCount;
            Rectangle sourceRect = new Rectangle(0, 0, texture.Width, frameHeight);
            CalculatedStyle dim = GetDimensions();
            Vector2 pos = new Vector2(dim.X, dim.Y);
            spriteBatch.Draw(texture, pos, sourceRect, Color.White, 0f, Vector2.Zero, ImageScale, SpriteEffects.None, 0f);
        }
    }
}
