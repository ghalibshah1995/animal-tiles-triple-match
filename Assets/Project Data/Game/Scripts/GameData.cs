using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Game Data", menuName = "Data/Game Data")]
    public class GameData : ScriptableObject
    {
        [SerializeField] bool showTutorial = true;
        public bool ShowTutorial => showTutorial;
    }
}
