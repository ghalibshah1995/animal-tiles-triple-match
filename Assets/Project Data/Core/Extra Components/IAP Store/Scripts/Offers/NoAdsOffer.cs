using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon.IAPStore
{
    public class NoAdsOffer : IAPStoreOffer
    {
        protected override void Awake()
        {
            base.Awake();
            gameObject.SetActive(false);
        }

        protected override void ApplyOffer()
        {
        }

        protected override void ReapplyOffer()
        {
        }

        private void OnPurchaseComplete(ProductKeyType productKeyType)
        {
            if(productKeyType == ProductKeyType.StarterPack) gameObject.SetActive(false);
        }
    }
}
