using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.Map
{
    public class MapChunkBehavior : MonoBehaviour
    {
        private static readonly Dictionary<Sprite, Sprite> bottomCapSprites = new Dictionary<Sprite, Sprite>();

        [SerializeField] SpriteRenderer background;
        [SerializeField] GameObject bottom;

        [SerializeField] List<MapLevelBehavior> levels;
        public int LevelsCount => levels.Count;

        public int ChunkId { get; private set; }

        public MapBehavior Map { get; private set; }
        public float Height => background.size.y * transform.localScale.y;
        public float AdjustedHeight => Height / Map.MapVisibleRectHeight;

        public float CurrentLevelPosition { get; private set; }
        public float Position { get; private set; }
        public int StartLevelCount { get; private set; }

        public void SetPosition(float y)
        {
            Position = y;
            transform.SetPositionY(y * Map.MapVisibleRectHeight + Height / 2 + Map.MapViewportBottomWorld);
        }

        public void SetMap(MapBehavior map)
        {
            Map = map;
        }

        public void Init(int chunkId, int startLevelCount)
        {
            ChunkId = chunkId;
            StartLevelCount = startLevelCount;

            CurrentLevelPosition = -1;

            transform.localScale = Vector3.one * Map.MapVisibleRectWidth / background.size.x;

            for (int i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                var levelId = startLevelCount + i;

                level.Init(levelId);

                if(levelId == MapBehavior.MaxLevelReached)
                {
                    CurrentLevelPosition = (level.transform.position.y + Height / 2) / Map.MapVisibleRectHeight;
                }
            }

            background.receiveShadows = true;

            // The legacy bottom object is a flat blue filler. The reskinned map
            // itself fills the screen, so keeping this object creates a blue band.
            if(bottom != null) bottom.SetActive(false);

            if(startLevelCount == 0)
                AddUnstretchedBottomCap();
        }

        private void AddUnstretchedBottomCap()
        {
            if(background == null)
                return;

            SpriteRenderer mapRenderer = null;
            var renderers = background.GetComponentsInChildren<SpriteRenderer>(true);
            for(int i = 0; i < renderers.Length; i++)
            {
                if(renderers[i] != background && renderers[i].sprite != null)
                {
                    mapRenderer = renderers[i];
                    break;
                }
            }

            if(mapRenderer == null || mapRenderer.sprite == null || mapRenderer.sprite.texture == null)
                return;

            var sourceSprite = mapRenderer.sprite;
            if(!bottomCapSprites.TryGetValue(sourceSprite, out var capSprite) || capSprite == null)
            {
                var sourceRect = sourceSprite.rect;
                var capHeight = Mathf.Max(1f, sourceRect.height * 0.08f);
                var capRect = new Rect(sourceRect.x, sourceRect.y, sourceRect.width, capHeight);

                capSprite = Sprite.Create(
                    sourceSprite.texture,
                    capRect,
                    new Vector2(0.5f, 0.5f),
                    sourceSprite.pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                capSprite.name = $"{sourceSprite.name}_BottomCap";
                bottomCapSprites[sourceSprite] = capSprite;
            }

            var capObject = new GameObject("Map Bottom Cap");
            capObject.transform.SetParent(mapRenderer.transform, false);
            capObject.transform.localPosition = new Vector3(
                0f,
                -sourceSprite.bounds.extents.y - capSprite.bounds.extents.y,
                0f);

            var capRenderer = capObject.AddComponent<SpriteRenderer>();
            capRenderer.sprite = capSprite;
            capRenderer.flipY = true;
            capRenderer.color = Color.white;
            capRenderer.sharedMaterial = mapRenderer.sharedMaterial;
            capRenderer.sortingLayerID = mapRenderer.sortingLayerID;
            capRenderer.sortingOrder = mapRenderer.sortingOrder;
        }

        private void CalculateNarrowScreenScale()
        {
            transform.localScale = Vector3.one * Map.MapVisibleRectWidth / background.size.x;
        }

        private void CalculateWideScreenScale()
        {

        }
    }
}

