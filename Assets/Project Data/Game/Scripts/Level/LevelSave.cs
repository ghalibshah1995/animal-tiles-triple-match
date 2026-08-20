namespace Watermelon
{
    [System.Serializable]
    public class LevelSave : ISaveObject
    {
        public int MaxReachedLevelIndex = 0;

        public int RealLevelIndex = 0;
        public int DisplayLevelIndex = 0;
        public int SelectedLevelIndex = -1;
        public bool IsPlayingRandomLevel = false;

        public int LastPlayerLevelIndex = -1;

        public int GetSelectedOrDisplayLevelIndex()
        {
            return SelectedLevelIndex >= 0 ? SelectedLevelIndex : DisplayLevelIndex;
        }

        public int ClampPlayableLevelIndex(int requestedLevelIndex, int maxLevelIndex)
        {
            if (maxLevelIndex < 0)
                return 0;

            return UnityEngine.Mathf.Clamp(requestedLevelIndex, 0, maxLevelIndex);
        }

        public void SelectLevel(int levelIndex, int maxLevelIndex)
        {
            int playableLevelIndex = ClampPlayableLevelIndex(levelIndex, maxLevelIndex);
            SelectedLevelIndex = playableLevelIndex;
            DisplayLevelIndex = playableLevelIndex;
        }
        
        public void Flush()
        {

        }
    }
}
