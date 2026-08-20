using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Watermelon.IAPStore
{
    public class StartedPackOffer : IAPStoreOffer
    {
        [SerializeField, Tooltip("In hours")] int infiniteLifeDuration;

        [Space]

        [SerializeField] List<PUType> powerUps;
        [SerializeField] int powerUpsAmount;

        [SerializeField] int coinsAmount;

        [Space]

        [SerializeField] TMP_Text powerUpsText;

        [SerializeField] TMP_Text livesText;
        [SerializeField] TMP_Text coinsText;

        protected override void Awake()
        {
            base.Awake();

            powerUpsText.text = $"x{powerUpsAmount}";

            coinsText.text = $"x{coinsAmount}";
            livesText.text = $"{infiniteLifeDuration}hrs";
        }

        protected override void ApplyOffer()
        {
            LivesManager.StartInfiniteLives(infiniteLifeDuration * 60 * 60);

            for (int i = 0; i < powerUps.Count; i++)
            {
                if(System.Enum.IsDefined(typeof(PUType), powerUps[i]))
                {
                    var type = powerUps[i];

                    PUController.AddPowerUp(type, powerUpsAmount);
                }
            }

            UIIAPStore iapStore = UIController.GetPage<UIIAPStore>();
            iapStore.SpawnCurrencyCloud((RectTransform)transform, CurrencyType.Coins, 15, () =>
            {
                CurrenciesController.Add(CurrencyType.Coins, coinsAmount);
            });

        }

        protected override void ReapplyOffer()
        {
        }
    }
}
