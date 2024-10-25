using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightRegions
{
    public class ActiveShadowUpdater : MonoBehaviour
    {
        public Vector3 captureOffset = Vector3.up;
        public float captureRadius = 1.25f;
        public bool castIfStill = false;
        private bool update = false;
        [HideInInspector] public int instanceID = -1;
        [HideInInspector] Vector3 lastUpdatePos = Vector3.zero;
        [HideInInspector] Quaternion lastUpdateRot = Quaternion.identity;
        private Collider[] hitColliders;
        private List<LightInfluenceMesher> influenceMeshers = new List<LightInfluenceMesher>();
        private void OnEnable()
        {
            hitColliders = new Collider[16];
            if (ShadowUpdateManager.Instance == null)
            {
                ShadowUpdateManager.Initialize();
            }
            ShadowUpdateManager.Instance.SubscribeUpdater(this);
        }
        private void OnDisable()
        {
            ShadowUpdateManager.Instance.UnSubscribeUpdater(this);
        }
        public void Update()
        {
            if (update)
            {
                UpdateShadows();
            }
        }
        public void UpdateShadows()
        {
            for(int i = 0; i < influenceMeshers.Count; i++)
            {
                influenceMeshers[i].RenderShadows();
            }
        }
        public void CheckForInfluence()
        {
            if (RegionManager.ActiveRegionManager == null)
            {
                update = false;
                return;
            }
            if (transform.position != lastUpdatePos || transform.rotation != lastUpdateRot || castIfStill)
            {
                ClearBuffer();
                Physics.OverlapSphereNonAlloc(transform.position + captureOffset, captureRadius, hitColliders, RegionManager.ActiveRegionManager.lightInfluenceMaskInclude);
                GetInfluenceMeshers();
                lastUpdatePos = transform.position;
                lastUpdateRot = transform.rotation;
                update = true;
            }
            else
            {
                update = false;
            }
        }
        public void ClearBuffer()
        {
            for (int i = 0; i < hitColliders.Length; i++)
            {
                hitColliders[i] = null;
            }
        }
        public void GetInfluenceMeshers()
        {
            influenceMeshers.Clear();
            for (int i = 0; i < hitColliders.Length; i++)
            {
                if (hitColliders[i] != null)
                {
                    influenceMeshers.Add(hitColliders[i].gameObject.GetComponent<LightInfluenceMesher>());
                }
            }
        }
    }

}
