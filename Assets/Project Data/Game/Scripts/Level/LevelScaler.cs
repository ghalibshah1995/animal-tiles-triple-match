using UnityEngine;

namespace Watermelon
{
    public class LevelScaler: MonoBehaviour
    {
        [SerializeField] Offset levelFieldOffset;
        [SerializeField] float maxTileSize = 2;

        public Vector2 LevelFieldCenter { get; private set; }
        public Vector2 LevelFieldSize { get; private set; }

        public static float TileSize { get; private set; }
        public static Vector2 OddLevelSize { get; private set; }
        public static Vector2 EvenLevelSize { get; private set; }

        public static float SlotSize { get; private set; }

        private void Awake()
        {
            float cameraHeight = Camera.main.orthographicSize * 2;
            float cameraWidth = cameraHeight * Camera.main.aspect;

            float leftBorder = (levelFieldOffset.left - 0.5f) * cameraWidth;
            float rightBorder = (0.5f - levelFieldOffset.right) * cameraWidth;

            float bottomBorder = (levelFieldOffset.bottom - 0.5f) * cameraHeight;
            float topBorder = (0.5f - levelFieldOffset.top) * cameraHeight;

            LevelFieldCenter = new Vector2((leftBorder + rightBorder) / 2, (bottomBorder + topBorder) / 2);
            LevelFieldSize = new Vector2(rightBorder - leftBorder, topBorder - bottomBorder);

            SlotSize = LevelFieldSize.x / 8;

            if(SlotSize > maxTileSize) SlotSize = maxTileSize;
        }

        public void Recalculate()
        {
            var biggerLayerSize = LevelController.IsEvenLayerBigger ? LevelController.EvenLayerSize : LevelController.OddLayerSize;

            var tileSizeX = LevelFieldSize.x / biggerLayerSize.x;
            var tileSizeY = LevelFieldSize.y / biggerLayerSize.y;

            TileSize = Mathf.Clamp(Mathf.Min(tileSizeX, tileSizeY), 0, maxTileSize);

            OddLevelSize = new Vector2(TileSize * LevelController.OddLayerSize.x, TileSize * LevelController.OddLayerSize.y);
            EvenLevelSize = new Vector2(TileSize * LevelController.EvenLayerSize.x, TileSize * LevelController.EvenLayerSize.y);
        }

        public static Vector3 GetPosition(ElementPosition elementPosition)
        {
            int layerID = elementPosition.LayerId;

            var halfSize = TileSize / 2f;

            if ((LevelController.Level.AmountOfLayers + 1 - layerID) % 2 == 0)
            {
                return new Vector3(-EvenLevelSize.x / 2f + elementPosition.X * TileSize + halfSize, -EvenLevelSize.y / 2f + halfSize + elementPosition.Y * TileSize, layerID);
            }
            else
            {
                return new Vector3(-OddLevelSize.x / 2f + elementPosition.X * TileSize + halfSize, -OddLevelSize.y / 2f + halfSize + elementPosition.Y * TileSize, layerID);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying)
            {
                Gizmos.DrawWireCube(LevelFieldCenter, LevelFieldSize);
            }
        }

        [System.Serializable]
        public struct Offset
        {
            [Range(0, 1)] public float left;
            [Range(0, 1)] public float right;
            [Range(0, 1)] public float top;
            [Range(0, 1)] public float bottom;
        }
    }
}