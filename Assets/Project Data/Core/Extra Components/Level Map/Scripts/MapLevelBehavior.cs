using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.Map
{
    public class MapLevelBehavior : MapLevelAbstractBehavior
    {
        private static readonly Color32 CompletedCircleColor = new Color32(255, 207, 37, 255);
        private static readonly Color32 CompletedTextColor = new Color32(143, 101, 0, 255);
        private static readonly Color32 PlayableCircleColor = new Color32(83, 221, 105, 255);
        private static readonly Color32 PlayableTextColor = new Color32(23, 112, 48, 255);
        private static readonly Color32 LockedCircleColor = new Color32(247, 247, 247, 255);
        private static readonly Color32 LockedTextColor = new Color32(145, 145, 145, 255);

        [SerializeField] Image innerCircle;

        [Space]
        [SerializeField] Color reachedText;
        [SerializeField] Color reachedCircle;
        [Space]
        [SerializeField] Color openedText;
        [SerializeField] Color openedCircle;
        [Space]
        [SerializeField] Color closedText;
        [SerializeField] Color closedCircle;

        protected override void InitOpen()
        {
            // Already completed levels are yellow.
            levelNumber.color = CompletedTextColor;
            innerCircle.color = CompletedCircleColor;

            button.gameObject.SetActive(true);
        }

        protected override void InitClose() 
        {
            // Future/locked levels stay white and cannot be clicked.
            levelNumber.color = LockedTextColor;
            innerCircle.color = LockedCircleColor;

            button.gameObject.SetActive(false);
        }

        protected override void InitCurrent()
        {
            // The next playable, not-yet-completed level is green.
            levelNumber.color = PlayableTextColor;
            innerCircle.color = PlayableCircleColor;

            button.gameObject.SetActive(true);
        }
    }
}
