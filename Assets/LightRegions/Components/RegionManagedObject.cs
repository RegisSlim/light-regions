using UnityEditor;
using UnityEngine;

namespace LightRegions
{
    public class RegionManagedObject : MonoBehaviour
    {
        public RegionManager regionManager;
        public Region region;
        public Vector3 centerOffset = Vector3.zero;
        public RegionManagedObjectType objectType;
        public Light lightComponent;
        public LightInfluenceMesher influenceMesh;
        public bool isActive;
        public bool debugTrace;

        public Vector3 GetOffsetPoint()
        {
            return transform.TransformDirection(centerOffset) + transform.position;
        }
        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformDirection(centerOffset) + transform.position, 0.1f);
        }
    }

#if UNITY_EDITOR

#endif
    public enum RegionManagedObjectType { GameObject = 0, Light = 1 }
}
