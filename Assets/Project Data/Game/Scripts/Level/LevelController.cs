using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class LevelController : MonoBehaviour
    {
        private static LevelController instance;

        [SerializeField] LevelDatabase database;
        [SerializeField] LevelSpawnAnimation levelSpawnAnimation;

        [Space]
        [SerializeField] LevelScaler levelScaler;
        [SerializeField] GameObject levelObject;
        [SerializeField] GameObject layersParentObject;
        [SerializeField] DockBehavior dock;

        private static bool isLevelLoaded;
        public static bool IsLevelLoaded => isLevelLoaded;

        private static LevelData level;
        public static LevelData Level => level;

        private static LevelSave levelSave;

        public static LevelDatabase Database => instance.database;

        public static int MaxReachedLevelIndex => levelSave.MaxReachedLevelIndex;
        public static int DisplayedLevelIndex => levelSave.DisplayLevelIndex;

        private static int loadedLevelIndex;

        public static GameObject LevelObject => instance.levelObject;

        private static LevelRepresentation levelRepresentation;
        public static LevelRepresentation LevelRepresentation => levelRepresentation;

        private static Dictionary<TileEffectType, TileEffect> effectsLink;

        public static Vector2Int EvenLayerSize => new Vector2Int(Level.GetLayer(Level.AmountOfLayers - 1).GetRow(0).AmountOfCells, Level.GetLayer(Level.AmountOfLayers - 1).AmountOfRows);
        public static Vector2Int OddLayerSize => new Vector2Int(Level.GetLayer(Level.AmountOfLayers - 2).GetRow(0).AmountOfCells, Level.GetLayer(Level.AmountOfLayers - 2).AmountOfRows);
        public static bool IsEvenLayerBigger => EvenLayerSize.x > OddLayerSize.x;

        public static int CurrentReward => GetCurrentLevelReward();
        public static DockBehavior Dock => instance.dock;

        public static BackgroundBehavior Background { get; private set; }

        private static bool isCustomLevel;
        public static bool IsCustomLevel => isCustomLevel;

        public static bool IsRaycastEnabled { get; set; } = true;

        private static bool firstTimeCompletedLevel = false;

        private static bool isBusy;
        public static bool IsBusy => isBusy;

        private static int TotalTileCount => levelRepresentation == null || levelRepresentation.Tiles == null ? 0 : levelRepresentation.Tiles.Count;

        private void Awake()
        {
            instance = this;
        }

        public void Initialise()
        {
            database.Initialise();
            dock.Initialise(this);

            levelSave = SaveController.GetSaveObject<LevelSave>("level");

            RaycastController raycastController = gameObject.AddComponent<RaycastController>();
            raycastController.Initialise();

            // Initialise special effects
            effectsLink = new Dictionary<TileEffectType, TileEffect>();
            TileEffect[] availableEffects = database.TileEffects;
            for (int i = 0; i < availableEffects.Length; i++)
            {
                if (effectsLink.ContainsKey(availableEffects[i].EffectType))
                {
                    Debug.LogError(string.Format("Tile effect with type {0} has duplicates in the database!", availableEffects[i].EffectType));

                    continue;
                }

                effectsLink.Add(availableEffects[i].EffectType, availableEffects[i]);
            }

            LoadBackground();
        }

        public void LoadCustomLevel(LevelData levelData, PreloadedLevelData preloadedLevelData, BackgroundData backgroundData, bool animateDock, SimpleCallback onLevelLoaded = null)
        {
            level = levelData;

            loadedLevelIndex = -1;
            firstTimeCompletedLevel = false;
            isCustomLevel = true;
            isBusy = true;

            UIGame gameUI = UIController.GetPage<UIGame>();
            gameUI.PowerUpsUIController.OnLevelStarted(0);
            gameUI.ActivateTutorial();

            levelObject.SetActive(true);

            levelScaler.Recalculate();

            layersParentObject.transform.position = levelScaler.LevelFieldCenter;

            // Initing level representation
            levelRepresentation = new LevelRepresentation(level, layersParentObject);
            levelRepresentation.SpawnObjects(preloadedLevelData);

            RaycastController.Disable();

            levelSpawnAnimation.Play(levelRepresentation, () =>
            {
                isLevelLoaded = true;

                RaycastController.Enable();

                isBusy = false;

                onLevelLoaded?.Invoke();
            });

            if(animateDock)
                dock.PlayAppearAnimation();

            LoadBackground(backgroundData);
        }

        public static void CompleteCustomLevel()
        {
            if (levelRepresentation != null)
            {
                UnloadLevel();
            }

            isCustomLevel = false;
        }

        public void LoadLevel(int levelIndex, SimpleCallback onLevelLoaded = null)
        {
            if (levelRepresentation != null)
            {
                UnloadLevel();
            }

            int requestedLevelIndex = levelIndex;
            int maxPlayableLevelIndex = levelSave.MaxReachedLevelIndex;
            levelIndex = levelSave.ClampPlayableLevelIndex(levelIndex, maxPlayableLevelIndex);
            if (requestedLevelIndex != levelIndex)
            {
                LogGameplayInitDiagnostics(
                    "Requested level was outside playable range and was clamped.",
                    requestedLevelIndex,
                    levelIndex,
                    -1,
                    null);
            }

            bool canResumeRandomLevel = levelSave.IsPlayingRandomLevel &&
                levelIndex == levelSave.DisplayLevelIndex &&
                levelSave.RealLevelIndex != -1;

            levelSave.SelectLevel(levelIndex, maxPlayableLevelIndex);
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true);

            int realLevelIndex;
            if (canResumeRandomLevel)
            {
                realLevelIndex = levelSave.RealLevelIndex;
            }
            else
            {
                levelSave.IsPlayingRandomLevel = false;
                realLevelIndex = database.GetRandomLevelIndex(levelIndex, levelSave.LastPlayerLevelIndex, false);

                levelSave.LastPlayerLevelIndex = realLevelIndex;
                levelSave.RealLevelIndex = realLevelIndex;

                if(realLevelIndex != levelIndex)
                {
                    levelSave.IsPlayingRandomLevel = true;
                }
            }

            levelSave.DisplayLevelIndex = levelIndex;
            levelSave.SelectedLevelIndex = levelIndex;
            loadedLevelIndex = levelIndex;
            firstTimeCompletedLevel = false;
            isBusy = true;
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true);

            level = database.GetLevel(realLevelIndex);
            if (level == null)
            {
                LogGameplayInitDiagnostics(
                    "Level data was missing for the resolved level index.",
                    requestedLevelIndex,
                    levelIndex,
                    realLevelIndex,
                    null);

                realLevelIndex = Mathf.Clamp(levelIndex, 0, Mathf.Max(0, database.AmountOfLevels - 1));
                levelSave.RealLevelIndex = realLevelIndex;
                level = database.GetLevel(realLevelIndex);

                if (level == null)
                {
                    isBusy = false;
                    isLevelLoaded = false;
                    return;
                }
            }

            UIGame gameUI = UIController.GetPage<UIGame>();
            gameUI.PowerUpsUIController.OnLevelStarted(levelIndex);
            gameUI.UpdateLevelNumber(levelSave.DisplayLevelIndex + 1);

            levelObject.SetActive(true);

            levelScaler.Recalculate();
            layersParentObject.transform.position = levelScaler.LevelFieldCenter;

            // Preparing objects to be placed on the level
            TileData[] availableObjects = database.AvailableForLevel(level);

            // Initing level representation
            levelRepresentation = new LevelRepresentation(level, layersParentObject);
            levelRepresentation.SpawnObjects(availableObjects);

            RaycastController.Disable();

            levelSpawnAnimation.Play(levelRepresentation, () =>
            {
                isLevelLoaded = true;

                RaycastController.Enable();

                isBusy = false;

                SaveController.MarkAsSaveIsRequired();
                SaveController.Save(true);

                LogGameplayInitDiagnostics(
                    "Gameplay initialized.",
                    requestedLevelIndex,
                    levelIndex,
                    realLevelIndex,
                    level);

                onLevelLoaded?.Invoke();
            });

            dock.PlayAppearAnimation();

            LoadBackground();
        }

        private void LoadBackground(BackgroundData backgroundData = null)
        {
            if (Background != null)
                Destroy(Background.gameObject);

            if(backgroundData == null)
                backgroundData = database.GetLastAvailableBackgroundData();

            if (backgroundData != null)
            {
                Background = Instantiate(backgroundData.BackgroundPrefab).GetComponent<BackgroundBehavior>();
            }
        }

        public static void OnTileSubmitted(TileBehavior tileBehavior)
        {
            TileEffect effect = tileBehavior.Effect;
            if (effect != null)
            {
                effect.OnTileSubmitted();
            }

            List<TileBehavior> activeTiles = levelRepresentation.Tiles;
            foreach (TileBehavior tiles in activeTiles)
            {
                effect = tiles.Effect;
                if (effect != null)
                {
                    effect.OnAnyTileSubmitted();
                }
            }
        }

        public static void UnloadLevel()
        {
            PUController.ResetBehaviors();
            isLevelLoaded = false;
            isBusy = false;

            if (Background != null)
            {
                Destroy(Background.gameObject);
                Background = null;
            }

            if (levelRepresentation != null)
            {
                levelRepresentation.Clear();
                levelRepresentation = null;
            }

            instance.levelSpawnAnimation.Clear();

            instance.dock.DisposeQuickly();
            instance.dock.HideSlots();
        }

        public static void RecoverActiveLevelAfterResume()
        {
            if (instance == null)
                return;

            IsRaycastEnabled = true;

            if (Background == null)
            {
                instance.LoadBackground();
            }
            else
            {
                Background.gameObject.SetActive(true);
            }

            if (levelRepresentation != null && TotalTileCount > 0)
            {
                instance.levelObject.SetActive(true);
                instance.layersParentObject.SetActive(true);
                instance.levelScaler.Recalculate();
                instance.layersParentObject.transform.position = instance.levelScaler.LevelFieldCenter;
                levelRepresentation.UpdateStates(true);

                isLevelLoaded = true;
                isBusy = false;
                RaycastController.Enable();

                return;
            }

            if (loadedLevelIndex >= 0)
            {
                Debug.LogWarning($"GAMEPLAY_RESUME_RECOVERY Reloading active level after app resume. displayedLevel={DisplayedLevelIndex} loadedLevel={loadedLevelIndex}");

                isLevelLoaded = false;
                isBusy = false;
                instance.LoadLevel(loadedLevelIndex);
            }
        }

        public void OnMatchCompleted()
        {
            if (isCustomLevel) return;

            if (levelRepresentation.Tiles.Count == 0 && dock.IsEmpty)
            {
                levelSave.IsPlayingRandomLevel = false;

                levelSave.DisplayLevelIndex++;
                levelSave.SelectedLevelIndex = levelSave.DisplayLevelIndex;

                if (levelSave.DisplayLevelIndex > levelSave.MaxReachedLevelIndex)
                {
                    levelSave.MaxReachedLevelIndex = levelSave.DisplayLevelIndex;
                    firstTimeCompletedLevel = true;
                }

                SaveController.MarkAsSaveIsRequired();
                SaveController.Save(true);

                GameController.OnLevelCompleted();

                AudioController.PlaySound(AudioController.Sounds.levelComplete);
            }
        }

        public void OnSlotsFilled()
        {
            if (!GameController.IsGameActive)
                return;

            GameController.OnLevelFailed();

            AudioController.PlaySound(AudioController.Sounds.levelFailed);
        }

        public static bool SubmitIsAllowed()
        {
            return !instance.dock.IsFilled;
        }

        public static TileBehavior SpawnDockTile(int tileID)
        {
            TileData tileData = Database.GetTile(tileID);
            ElementPosition elementPosition = new ElementPosition(-1, -1);

            TileBehavior tileBehavior = tileData.Pool.GetPooledObject().GetComponent<TileBehavior>();
            tileBehavior.Initialise(tileData, elementPosition);
            tileBehavior.transform.localScale = Vector3.one;
            tileBehavior.SetScale(LevelScaler.SlotSize);
            tileBehavior.MarkAsSubmitted();

            Dock.SubmitToSlot(tileBehavior, true);

            return tileBehavior;
        }

        public static void SubmitElement(TileBehavior tileBehavior)
        {
            tileBehavior.MarkAsSubmitted();

            instance.dock.SubmitToSlot(tileBehavior, false);

            levelRepresentation.RemoveObject(tileBehavior);
            levelRepresentation.UpdateStates(true);
        }

        public static void RevertElement(TileBehavior tileBehavior)
        {
            tileBehavior.ResetSubmitState();

            levelRepresentation.AddObject(tileBehavior);
            levelRepresentation.UpdateStates(true);
        }

        public static void RevertElements(List<TileBehavior> tileBehaviors)
        {
            foreach (TileBehavior tileBehavior in tileBehaviors)
            {
                tileBehavior.ResetSubmitState();

                levelRepresentation.AddObject(tileBehavior);
            }

            levelRepresentation.UpdateStates(true);
        }

        public static void SubmitElements(List<TileBehavior> tileBehaviors)
        {
            for (int i = 0; i < tileBehaviors.Count; i++)
            {
                var tileBehavior = tileBehaviors[i];

                tileBehavior.MarkAsSubmitted();

                instance.dock.SubmitToSlot(tileBehavior, false);

                levelRepresentation.RemoveObject(tileBehavior);
            }

            levelRepresentation.UpdateStates(true);
        }

        public static TileEffect GetTileEffect(TileEffectType tileEffectType)
        {
            if (effectsLink.ContainsKey(tileEffectType))
                return effectsLink[tileEffectType];

            return null;
        }

        public static List<TileBehavior> GetActiveTiles(bool ignoreEffects)
        {
            List<TileBehavior> tempTiles = new List<TileBehavior>();
            List<TileBehavior> activeTiles = levelRepresentation.Tiles;

            for (int i = 0; i < activeTiles.Count; i++)
            {
                if (!activeTiles[i].IsSubmitted)
                {
                    if (ignoreEffects)
                    {
                        if (activeTiles[i].Effect == null)
                        {
                            tempTiles.Add(activeTiles[i]);
                        }
                    }
                    else
                    {
                        tempTiles.Add(activeTiles[i]);
                    }
                }
            }

            return tempTiles;
        }

        public static List<TileBehavior> GetClickableTiles()
        {
            List<TileBehavior> tempTiles = new List<TileBehavior>();
            List<TileBehavior> activeTiles = levelRepresentation.Tiles;

            for (int i = 0; i < activeTiles.Count; i++)
            {
                if (!activeTiles[i].IsSubmitted && activeTiles[i].IsClickable)
                {
                    tempTiles.Add(activeTiles[i]);
                }
            }

            return tempTiles;
        }

        public static List<TileBehavior> GetTilesByType(TileData tileData, int amount = int.MaxValue)
        {
            List<TileBehavior> tempTiles = new List<TileBehavior>();
            List<TileBehavior> activeTiles = levelRepresentation.Tiles;

            for (int i = 0; i < activeTiles.Count; i++)
            {
                if (!activeTiles[i].IsSubmitted)
                {
                    if (activeTiles[i].TileData == tileData)
                    {
                        tempTiles.Add(activeTiles[i]);

                        if (tempTiles.Count >= amount)
                            break;
                    }
                }
            }

            return tempTiles;
        }

        public static List<TileBehavior> GetNeighbourTiles(ElementPosition elementPosition)
        {
            List<TileBehavior> neighbourTiles = new List<TileBehavior>();

            ElementPosition[] neighbourPositions = new ElementPosition[] { elementPosition.UpNeighbourPos, elementPosition.RightNeighbourPos, elementPosition.BottomNeighbourPos, elementPosition.LeftNeighbourPos };
            for(int i = 0; i < neighbourPositions.Length; i++)
            {
                if(levelRepresentation.IsTileExists(neighbourPositions[i]))
                {
                    neighbourTiles.Add(levelRepresentation.Layers[neighbourPositions[i]].Tile);
                }
            }

            return neighbourTiles;
        }

        public static TileBehavior GetTile(ElementPosition elementPosition)
        {
            if (levelRepresentation.IsTileExists(elementPosition))
            {
                return levelRepresentation.Layers[elementPosition].Tile;
            }

            return null;
        }

        public static bool HasNeighbourTiles(ElementPosition elementPosition)
        {
            ElementPosition[] neighbourPositions = new ElementPosition[] { elementPosition.UpNeighbourPos, elementPosition.RightNeighbourPos, elementPosition.BottomNeighbourPos, elementPosition.LeftNeighbourPos };
            for (int i = 0; i < neighbourPositions.Length; i++)
            {
                if (levelRepresentation.IsTileExists(neighbourPositions[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetCurrentLevelReward()
        {
            if (Level != null)
            {
                if (firstTimeCompletedLevel)
                {
                    return level.CoinsReward;
                }
                else
                {
                    return (int)(level.CoinsReward * 0.25f);
                }
            }

            return 5;
        }

        public static bool ReturnTiles(int count, SimpleCallback callback)
        {
            List<TileBehavior> removedTiles = DockBehavior.RemoveObjects(count).ConvertAll((slotable) => (TileBehavior)slotable);
            if (removedTiles.IsNullOrEmpty())
            {
                callback?.Invoke();
                return false;
            } 

            int revertedTiles = 0;
            foreach (TileBehavior tile in removedTiles)
            {
                Vector3 returnPosition = LevelScaler.GetPosition(tile.ElementPosition);

                Transform parentTransform = tile.transform.parent;
                if (parentTransform != null)
                {
                    returnPosition = parentTransform.TransformPoint(returnPosition);
                }

                tile.SubmitMove(returnPosition, Vector3.one * LevelScaler.TileSize, () =>
                {
                    revertedTiles++;

                    callback?.Invoke();

                    tile.SetPosition(tile.ElementPosition);

                    if (revertedTiles >= removedTiles.Count)
                    {
                        RevertElements(removedTiles);
                    }
                });
            }

            return true;
        }

        public static bool IsLevelCompletable()
        {
            int clickableTilesCount = 0;

            List<TileBehavior> tiles = levelRepresentation.Tiles;
            foreach(TileBehavior tile in tiles)
            {
                if (tile.IsClickable)
                {
                    clickableTilesCount++;

                    if (clickableTilesCount > 2)
                        return true;
                }
            }

            return false;
        }

        public static void SetBusyState(bool state)
        {
            isBusy = state;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void LogGameplayInitDiagnostics(string message, int requestedLevelIndex, int selectedLevelIndex, int realLevelIndex, LevelData selectedLevelData)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string boardActive = instance != null && instance.levelObject != null ? instance.levelObject.activeSelf.ToString() : "missing";
            string boardScale = instance != null && instance.levelObject != null ? instance.levelObject.transform.localScale.ToString() : "missing";
            string selectedNull = selectedLevelData == null ? "true" : "false";
            string saveSelected = levelSave != null ? levelSave.SelectedLevelIndex.ToString() : "missing";
            string current = levelSave != null ? levelSave.DisplayLevelIndex.ToString() : "missing";
            string maxReached = levelSave != null ? levelSave.MaxReachedLevelIndex.ToString() : "missing";
            string dataCount = instance != null && instance.database != null ? instance.database.AmountOfLevels.ToString() : "missing";

            Debug.Log($"GAMEPLAY_INIT_DIAGNOSTICS message=\"{message}\" scene={sceneName} requestedLevel={requestedLevelIndex} " +
                      $"selectedLevel={selectedLevelIndex} saveSelected={saveSelected} currentLevel={current} highestUnlocked={maxReached} " +
                      $"realLevelIndex={realLevelIndex} levelDataCount={dataCount} selectedLevelDataNull={selectedNull} " +
                      $"boardContainerActive={boardActive} boardContainerScale={boardScale} generatedTileCount={TotalTileCount} " +
                      $"isLevelLoaded={isLevelLoaded} isBusy={isBusy}");
        }
    }
}
