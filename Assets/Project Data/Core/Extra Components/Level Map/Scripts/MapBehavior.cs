using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.Map
{
    public class MapBehavior : MonoBehaviour
    {
        private static MapBehavior instance;

        [SerializeField] MapData data;

        [Header("Usable Viewport")]
        [Tooltip("Bottom screen area reserved for the fixed Play button, banner ad, and safe spacing. The map is laid out above this area.")]
        [SerializeField, Range(0f, 0.45f)] float bottomViewportInset = 0f;

        [Tooltip("Top screen area reserved for the fixed HUD/safe area. The map is laid out below this area.")]
        [SerializeField, Range(0f, 0.25f)] float topViewportInset = 0.08f;

        public List<MapChunkBehavior> loadedChunks;

        public MapChunkBehavior LowestLoadedChunk => loadedChunks[0];
        public MapChunkBehavior HighestLoadedChunk => loadedChunks[^1];

        public float MapVisibleRectWidth { get; private set; }
        public float MapVisibleRectHeight { get; private set; }
        public float CameraVisibleRectHeight { get; private set; }
        public float MapViewportBottomNormalized => bottomViewportInset;
        public float MapViewportTopNormalized => 1f - topViewportInset;
        public float MapViewportBottomWorld => Camera.main.transform.position.y - CameraVisibleRectHeight / 2f + CameraVisibleRectHeight * MapViewportBottomNormalized;

        public static int MaxLevelReached => MapLinker.MaxLevelReached;
        
        private bool isMouseDown = false;

        private float mousePressPosY;
        private float mouseReleasePosY;
        
        private float currentLowestChunkPosY;
        private float mousePrevFramePosY;
        private float mouseMoveDeltaY;

        TweenCase rubberCase;

        private void Awake()
        {
            instance = this;
            loadedChunks = new List<MapChunkBehavior>();

            // The height of the orthographic camera in default units
            CameraVisibleRectHeight = Camera.main.orthographicSize * 2;
            MapVisibleRectHeight = CameraVisibleRectHeight * Mathf.Clamp01(MapViewportTopNormalized - MapViewportBottomNormalized);

            if(Camera.main.aspect < 9f / 16f)
            {
                // Real width of rge orthographic camera
                MapVisibleRectWidth = CameraVisibleRectHeight * Camera.main.aspect;
            } else
            {
                // Constraind width for correct scaling on wide screenes
                MapVisibleRectWidth = CameraVisibleRectHeight * 9f / 16f;
            }

            enabled = false;
        }

        public void Show()
        {
            StopAllCoroutines();
            CleanupLoadedChunks();

            enabled = true;
            isMouseDown = false;

            int lastReachedChunkId = GetLastReachedChunkId(out int totalLevelsCount);

            var lastReachedChunk = Instantiate(data.chunks[lastReachedChunkId % data.chunks.Count]).GetComponent<MapChunkBehavior>();

            lastReachedChunk.SetMap(this);
            lastReachedChunk.Init(lastReachedChunkId, totalLevelsCount - lastReachedChunk.LevelsCount);
            loadedChunks.Add(lastReachedChunk);

            // The initial Y position of the lastReachedChunk is 0. we scroll down to the position of the last reached level, and then scrolling up to the desired position
            var lastReachedLevelPos = -lastReachedChunk.CurrentLevelPosition + data.currentLevelVerticalOffset;

            // Keep the first biome anchored to the bottom of the screen. Without
            // this, progressing from level 1 to level 2 recentres the chunk and
            // exposes a blue/cyan strip below the map.
            if(lastReachedChunkId == 0)
                lastReachedLevelPos = data.firstChunkMaxLevelVerticalOffset;

            // We just reseting lastReachedChunks position, populaing parameters to let ScrollMap method do all the work of moving the map to the position we calculated above 
            lastReachedChunk.SetPosition(0);

            currentLowestChunkPosY = 0;
            mouseMoveDeltaY = lastReachedLevelPos;
            ScrollMap();

            // Populaing the map to fill the whole screen
            CheckBottomChunks();
            CheckTopChunks();
        }

        public void Hide()
        {
            enabled = false;
            for(int i = 0; i < loadedChunks.Count; i++) 
            {
                loadedChunks[i].gameObject.SetActive(false);
            }

            StartCoroutine(DisableCoroutine());
        }

        private void CleanupLoadedChunks()
        {
            if (loadedChunks == null)
            {
                loadedChunks = new List<MapChunkBehavior>();
                return;
            }

            for (int i = loadedChunks.Count - 1; i >= 0; i--)
            {
                if (loadedChunks[i] != null)
                    Destroy(loadedChunks[i].gameObject);
            }

            loadedChunks.Clear();
        }

        // Little optimization trick
        private IEnumerator DisableCoroutine()
        {
            while(loadedChunks.Count > 0)
            {
                yield return new WaitForSeconds(0.5f);

                Destroy(loadedChunks[^1].gameObject);
                loadedChunks.RemoveAt(loadedChunks.Count - 1);
            }
        }

        #region Movement

        public static void EnableScroll()
        {
            instance.enabled = true;
            instance.isMouseDown = false;
        }

        public static void DisableScroll()
        {
            instance.enabled = false;
            instance.isMouseDown = false;
        }

        /// <param name="totalLevelsCount">The amount of levels from all previous chunks up to and including last reached chunk</param>
        private int GetLastReachedChunkId(out int totalLevelsCount)
        {
            int lastReachedChunkId = -1;

            totalLevelsCount = 0;
            while (totalLevelsCount <= MaxLevelReached)
            {
                lastReachedChunkId++;

                var chunk = data.chunks[lastReachedChunkId % data.chunks.Count].GetComponent<MapChunkBehavior>();
                totalLevelsCount += chunk.LevelsCount;
            }

            return lastReachedChunkId;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!IsScreenPointInsideVisibleViewport(Input.mousePosition))
                    return;

                // mouse press y position mapped on 0-1 scale. 0 is the bottom of the screen, 1 is the top)
                mousePressPosY = Input.mousePosition.y / Camera.main.pixelHeight;
                mousePrevFramePosY = mousePressPosY;
                currentLowestChunkPosY = LowestLoadedChunk.Position;

                isMouseDown = true;

                rubberCase.KillActive();
            }
            else if(Input.GetMouseButtonUp(0) && isMouseDown)
            {
                isMouseDown = false;

                if (LowestLoadedChunk.ChunkId == 0 && LowestLoadedChunk.Position > data.firstChunkMaxLevelVerticalOffset)
                {
                    // Scrolled to much down, need to return back up a little bit
                    BottomRubber();
                } else
                {
                    mouseReleasePosY = Input.mousePosition.y / Camera.main.pixelHeight;
                    var dif = mouseReleasePosY - mousePrevFramePosY;

                    // There was a swipe movement, need to scroll a little bit more for a little bit of time to feel natural
                    if(Mathf.Abs(dif) > 0.001f)
                    {
                        ContinuousScroll(dif);
                    }
                }
            } 
            else if (isMouseDown)
            {
                var mousePosY = Input.mousePosition.y / Camera.main.pixelHeight;
                mousePrevFramePosY = mousePosY;

                mouseMoveDeltaY = mousePosY - mousePressPosY;

                ScrollMap();
            }
        }

        private void ContinuousScroll(float scrollFrameDistance)
        {
            float scrollDuration = Mathf.Clamp(Mathf.Abs(scrollFrameDistance / 0.1f), 0, 1);

            rubberCase = Tween.DoFloat(scrollFrameDistance, 0, scrollDuration, (value) => {
                mouseMoveDeltaY += value;

                var cachedPos = currentLowestChunkPosY;

                ScrollMap();

                if(Mathf.Approximately(cachedPos, currentLowestChunkPosY))
                {
                    rubberCase.KillActive();
                    rubberCase.InvokeCompleteEvent();
                }
            }).SetEasing(Ease.Type.SineOut).OnComplete(() => {
                if (LowestLoadedChunk.ChunkId == 0 && LowestLoadedChunk.Position > data.firstChunkMaxLevelVerticalOffset)
                {
                    BottomRubber();
                }
            });
        }

        private void BottomRubber()
        {
            rubberCase = Tween.DoFloat(LowestLoadedChunk.Position, data.firstChunkMaxLevelVerticalOffset, 0.3f, (value) => {
                SetChunksPosition(value);
            }).SetEasing(Ease.Type.QuadOut);
        }

        private void ScrollMap()
        {
            var pos = currentLowestChunkPosY + mouseMoveDeltaY;

            if (pos > data.firstChunkMaxLevelVerticalOffset && LowestLoadedChunk.ChunkId == 0)
            {
                // There are some math that kinda works

                // The overshoot distance from the end of the map
                var rubberDistance = pos - data.firstChunkMaxLevelVerticalOffset;
                // Adding Easing for rubber effect
                var interpolatedRubberDistance = Ease.Interpolate(rubberDistance, Ease.Type.SineOut);
                // smoothing position depending on mouseDelta. If the mouse is not moving, we're just sticking to the actual position
                var smoothedPos = Mathf.Lerp(pos, data.firstChunkMaxLevelVerticalOffset + interpolatedRubberDistance, mouseMoveDeltaY);
                // Clamping position in order not to overshoot too far
                pos = Mathf.Clamp(smoothedPos, data.firstChunkMaxLevelVerticalOffset, data.firstChunkMaxLevelVerticalOffset + 0.1f);
            }

            SetChunksPosition(pos);

            CheckTopChunks();
            CheckBottomChunks();
        }

        private void SetChunksPosition(float pos)
        {
            for (int i = 0; i < loadedChunks.Count; i++)
            {
                var chunk = loadedChunks[i];

                chunk.SetPosition(pos);
                pos += chunk.AdjustedHeight;
            }
        }

        private void CheckBottomChunks()
        {
            // Checking for the chunks that are bellow the camera and not visible to the player anymore
            while (LowestLoadedChunk.Position + LowestLoadedChunk.AdjustedHeight < -0.05f)
            {
                Destroy(LowestLoadedChunk.gameObject);
                loadedChunks.RemoveAt(0);
            }

            
            while (LowestLoadedChunk.Position >= 0 && LowestLoadedChunk.ChunkId != 0)
            {
                var newLowestChunk = Instantiate(data.chunks[(LowestLoadedChunk.ChunkId - 1) % data.chunks.Count]).GetComponent<MapChunkBehavior>();
                newLowestChunk.SetMap(this);
                newLowestChunk.Init(LowestLoadedChunk.ChunkId - 1, LowestLoadedChunk.StartLevelCount - newLowestChunk.LevelsCount);
                newLowestChunk.SetPosition(LowestLoadedChunk.Position - newLowestChunk.AdjustedHeight);

                loadedChunks.Insert(0, newLowestChunk);
            }

            // Reseting movement parameters in order to preserve scroll smoothness
            mousePressPosY = Input.mousePosition.y / Camera.main.pixelHeight;
            currentLowestChunkPosY = LowestLoadedChunk.Position;

            mouseMoveDeltaY = 0;
        }

        private void CheckTopChunks()
        {
            // Checking for the chunks that are above the camera and not visible to the player anymore
            while (HighestLoadedChunk.Position > 1.05f)
            {
                Destroy(HighestLoadedChunk.gameObject);
                loadedChunks.RemoveAt(loadedChunks.Count - 1);
            }

            // Checking if there is the need to spawn a new chunk at the top of the screen
            while (HighestLoadedChunk.Position + HighestLoadedChunk.AdjustedHeight <= 1)
            {
                var newHighestChunk = Instantiate(data.chunks[(HighestLoadedChunk.ChunkId + 1) % data.chunks.Count]).GetComponent<MapChunkBehavior>();

                newHighestChunk.SetMap(this);
                newHighestChunk.Init(HighestLoadedChunk.ChunkId + 1, HighestLoadedChunk.StartLevelCount + HighestLoadedChunk.LevelsCount);
                newHighestChunk.SetPosition(HighestLoadedChunk.Position + HighestLoadedChunk.AdjustedHeight);

                loadedChunks.Add(newHighestChunk);
            }
        }

        public static bool IsScreenPointInsideVisibleViewport(Vector2 screenPosition)
        {
            if (instance == null || Camera.main == null)
                return true;

            var normalizedY = screenPosition.y / Camera.main.pixelHeight;
            return normalizedY >= instance.MapViewportBottomNormalized && normalizedY <= instance.MapViewportTopNormalized;
        }
    }

    #endregion
}
