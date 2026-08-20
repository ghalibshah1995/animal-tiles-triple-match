using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.IAPStore;

namespace Watermelon
{
    public class UINoAdsPopUp : MonoBehaviour
    {
        [SerializeField] UIScaleAnimation panelScalable;
        [SerializeField] UIFadeAnimation backFade;
        [SerializeField] Button bigCloseButton;
        [SerializeField] Button smallCloseButton;
        [SerializeField] IAPButton removeAdsButton;

        public bool IsOpened => gameObject.activeSelf;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void ForcedAdDisabled()
        {
            ClosePanel();
        }

        public void Show()
        {
            gameObject.SetActive(false);
        }

        private void ClosePanel()
        {
            bigCloseButton.interactable = false;
            smallCloseButton.interactable = false;
            
            backFade.Hide(0.2f);
            panelScalable.Hide(immediately: false, duration: 0.4f, onCompleted: () =>
            {
                gameObject.SetActive(false);
            });

        }
    }
}
