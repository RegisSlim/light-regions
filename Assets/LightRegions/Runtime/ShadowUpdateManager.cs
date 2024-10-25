using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightRegions
{
    public class ShadowUpdateManager : MonoBehaviour
    {
        public static ShadowUpdateManager Instance;
        public static ShadowUpdateManager Initialize()
        {
            GameObject go = new GameObject("ShadowUpdateManager");
            Instance = go.AddComponent<ShadowUpdateManager>();
            return Instance;
        }
        public List<ActiveShadowUpdater> activeShadowUpdaters = new List<ActiveShadowUpdater>();

        private void OnEnable()
        {
            StartCoroutine(RefreshShadowUpdaters());
        }
        public void SubscribeUpdater(ActiveShadowUpdater activeShadowUpdater)
        {
            activeShadowUpdaters.Add(activeShadowUpdater);
            activeShadowUpdater.instanceID = activeShadowUpdaters.Count - 1;
        }
        public void UnSubscribeUpdater(ActiveShadowUpdater activeShadowUpdater)
        {
            activeShadowUpdaters.RemoveAt(activeShadowUpdater.instanceID);
            AssignIDs();
        }
        private void AssignIDs()
        {
            for (int i = 0; i < activeShadowUpdaters.Count; i++)
            {
                activeShadowUpdaters[i].instanceID = i;
            }
        }
        private IEnumerator RefreshShadowUpdaters()
        {
            while (true)
            {
                for (int i = 0; i < activeShadowUpdaters.Count; i++)
                {
                    activeShadowUpdaters[i].CheckForInfluence();
                    yield return null;
                }
                yield return null;
            }
        }
    }
}
