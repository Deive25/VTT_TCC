using UnityEngine;

public class SpriteCleanup : MonoBehaviour
{
    public Sprite spriteToDestroy;

    private void OnDestroy()
    {
        if (spriteToDestroy == null || spriteToDestroy == VTTLayout.GetCircleSprite()) return;

        Texture2D texture = spriteToDestroy.texture;
        Destroy(spriteToDestroy);
        if (texture != null) Destroy(texture);
    }
}
