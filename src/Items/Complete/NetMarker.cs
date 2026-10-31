using R2API;
using UnityEngine;
using UnityEngine.Networking;

namespace ClassicItemsReturns.Items.Complete
{
    /// <summary>
    /// Networked world markers. The server spawns them with NetworkServer.Spawn, which carries the spawn
    /// position to every client, so static markers need no SyncVars. Prefabs must be made at load time.
    /// </summary>
    public static class NetMarker
    {
        /// <param name="model">If set, the item's own 3D model is used (at modelScale) instead of a glow sprite.</param>
        public static GameObject Make(string name, Color color, float size, float bob = 0f, bool bounce = false, string model = null, float modelScale = 1f, float lightRange = 0f)
        {
            var template = new GameObject("CIC_Marker_" + name);
            template.AddComponent<NetworkIdentity>();
            var visual = model != null ? Modules.RuntimeArt.Spawn(model, modelScale, template.transform) : null;
            if (!visual) visual = CIC.GlowObject(template.transform, color, size);
            else visual.AddComponent<CICSpin>();
            if (bob > 0) { var b = visual.AddComponent<CICBob>(); b.height = bob; b.bounce = bounce; }
            if (lightRange > 0)
            {
                var lg = new GameObject("Light"); lg.transform.SetParent(template.transform, false);
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.range = lightRange; l.intensity = 2.5f;
            }
            var prefab = PrefabAPI.InstantiateClone(template, "CIC_Marker_" + name, true);
            Object.Destroy(template);
            return prefab;
        }

        public static GameObject Spawn(GameObject prefab, Vector3 pos)
        {
            var go = Object.Instantiate(prefab, pos, Quaternion.identity);
            if (NetworkServer.active) NetworkServer.Spawn(go);
            return go;
        }

        public static void Kill(GameObject go)
        {
            if (!go) return;
            if (NetworkServer.active && go.GetComponent<NetworkIdentity>()) NetworkServer.Destroy(go);
            else Object.Destroy(go);
        }
    }

    /// <summary>Slow idle spin, like a pickup on the ground.</summary>
    public class CICSpin : MonoBehaviour
    {
        void Update() { transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World); }
    }

    /// <summary>Client-side bob/bounce animation for marker visuals.</summary>
    public class CICBob : MonoBehaviour
    {
        public float height = 0.3f; public bool bounce; float t;
        void Update()
        {
            t += Time.deltaTime;
            float y = bounce ? Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * height : Mathf.Sin(t * 2f) * height;
            transform.localPosition = new Vector3(0, y, 0);
        }
    }
}
