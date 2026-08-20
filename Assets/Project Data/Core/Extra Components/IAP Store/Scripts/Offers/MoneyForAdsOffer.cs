 using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.IAPStore
{
    public class MoneyForAdsOffer : MonoBehaviour, IIAPStoreOffer
    {
        [SerializeField] int coinsAmount;
        [SerializeField] TMP_Text coinsAmountText;

        [Space]
        [SerializeField] Button button;

        [Space]
        [SerializeField] RectTransform cloudSpawnRectTransform;
        [SerializeField] int floatingElementsAmount = 10;

        public GameObject GameObject => gameObject;

        private RectTransform rect;
        public float Height => rect.sizeDelta.y;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
        }

        public void Init()
        {
            gameObject.SetActive(false);
        }

        private void OnAdButtonClicked()
        {
            gameObject.SetActive(false);
        }
    }
}
