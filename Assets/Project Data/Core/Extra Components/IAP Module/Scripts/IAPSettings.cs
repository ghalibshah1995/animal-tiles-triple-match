using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "IAP Settings", menuName = "Settings/IAP Settings")]
    public class IAPSettings : ScriptableObject
    {
        [SerializeField] IAPItem[] storeItems;
        public IAPItem[] StoreItems => storeItems;
    }
}
