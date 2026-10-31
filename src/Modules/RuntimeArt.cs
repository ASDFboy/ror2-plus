using R2API;
using RoR2;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ClassicItemsReturns.Modules
{
    /// <summary>
    /// Loads icons embedded in the DLL (art/icons/*.png) and builds lightweight billboard pickup
    /// models from them, so new items don't need a Unity asset bundle.
    /// </summary>
    public static class RuntimeArt
    {
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>();
        private static GameObject prefabRoot;

        public static Sprite Icon(string name)
        {
            if (spriteCache.TryGetValue(name, out var cached)) return cached;
            Sprite sprite = null;
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("icons." + name + ".png"))
            {
                if (s != null)
                {
                    var bytes = new byte[s.Length];
                    s.Read(bytes, 0, bytes.Length);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    tex.LoadImage(bytes);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    tex.name = "texIconCIC" + name;
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width / 1.5f);
                    sprite.name = tex.name;
                }
            }
            if (!sprite)
            {
                ClassicItemsReturnsPlugin.ModLogger.LogWarning("RuntimeArt: no embedded icon for " + name);
                sprite = Assets.NullSprite;
            }
            spriteCache[name] = sprite;
            return sprite;
        }

        public static bool HasIcon(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceInfo("icons." + name + ".png") != null;
        }

        /// <summary>
        /// Pickup model for a ror2-plus item: the exported 3D mesh (models/NAME.rpm + NAME.jpg next to the DLL) when
        /// present and "Use 3d Models" is on, otherwise a billboard built from the icon.
        /// </summary>
        public static GameObject PickupModel(string name)
        {
            if (modelCache.TryGetValue(name, out var cached)) return cached;
            if (!prefabRoot)
            {
                prefabRoot = new GameObject("CIC_PrefabRoot");
                prefabRoot.SetActive(false);
                Object.DontDestroyOnLoad(prefabRoot);
            }
            var root = new GameObject("mdlCIC" + name);
            root.transform.SetParent(prefabRoot.transform, false);

            Renderer renderer = null;
            if (ClassicItemsReturnsPlugin.use3dModels)
            {
                try { renderer = BuildMesh(name, root.transform); }
                catch (System.Exception e) { ClassicItemsReturnsPlugin.ModLogger.LogWarning($"RuntimeArt: 3D model for {name} failed ({e.Message}); using billboard."); }
            }
            bool billboard = !renderer;
            if (billboard)
            {
                var child = new GameObject("Sprite");
                child.transform.SetParent(root.transform, false);
                var sr = child.AddComponent<SpriteRenderer>();
                sr.sprite = Icon(name);
                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                child.AddComponent<CICBillboard>();
                renderer = sr;
            }

            var display = root.AddComponent<ItemDisplay>();
            var shadows = billboard ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            display.rendererInfos = new CharacterModel.RendererInfo[]
            {
                new CharacterModel.RendererInfo { renderer = renderer, defaultMaterial = renderer.sharedMaterial, ignoreOverlays = billboard, defaultShadowCastingMode = shadows }
            };
            var panel = root.AddComponent<ModelPanelParameters>();
            var focus = new GameObject("Focus"); focus.transform.SetParent(root.transform, false);
            var cam = new GameObject("Camera"); cam.transform.SetParent(root.transform, false); cam.transform.localPosition = new Vector3(0, 0, -3f);
            panel.focusPointTransform = focus.transform;
            panel.cameraPositionTransform = cam.transform;
            panel.minDistance = 1.5f; panel.maxDistance = 4f;

            modelCache[name] = root;
            return root;
        }

        /// <summary>A free-standing copy of an item's own model (no pickup/display components), for in-world use.</summary>
        public static GameObject Spawn(string name, float scale, Transform parent = null)
        {
            var p = PickupModel(name);
            var src = p.transform.Find("Mesh") ?? p.transform.Find("Sprite");
            if (!src) return null;
            var go = Object.Instantiate(src.gameObject, parent);
            go.transform.localPosition = Vector3.zero; go.transform.localScale = Vector3.one * scale;
            go.SetActive(true);
            return go;
        }

        static string modelDir;

        /// <summary>Reads an RPM2 mesh (see tools/export_models.py) and its baked texture. Returns null if absent.</summary>
        static Renderer BuildMesh(string name, Transform parent)
        {
            if (modelDir == null) modelDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "models");
            string meshPath = System.IO.Path.Combine(modelDir, name + ".rpm"), texPath = System.IO.Path.Combine(modelDir, name + ".jpg");
            if (!File.Exists(meshPath) || !File.Exists(texPath)) return null;

            Mesh mesh;
            using (var r = new BinaryReader(File.OpenRead(meshPath)))
            {
                if (new string(r.ReadChars(4)) != "RPM2") throw new IOException("bad header");
                int vc = r.ReadInt32(), ic = r.ReadInt32();
                var pos = new Vector3[vc]; var nrm = new Vector3[vc]; var uv = new Vector2[vc]; var tris = new int[ic];
                for (int i = 0; i < vc; i++) pos[i] = new Vector3(r.ReadInt16(), r.ReadInt16(), r.ReadInt16()) / 32767f;
                for (int i = 0; i < vc; i++) nrm[i] = new Vector3(r.ReadSByte(), r.ReadSByte(), r.ReadSByte()) / 127f;
                for (int i = 0; i < vc; i++) uv[i] = new Vector2(r.ReadUInt16(), r.ReadUInt16()) / 65535f;
                for (int i = 0; i < ic; i++) tris[i] = r.ReadUInt16();
                mesh = new Mesh { name = "mesh" + name };
                mesh.vertices = pos; mesh.normals = nrm; mesh.uv = uv; mesh.triangles = tris;
                mesh.RecalculateBounds();
                mesh.UploadMeshData(true);
            }

            var albedo = LoadTex(texPath, "tex" + name, false);
            var normal = LoadTex(System.IO.Path.Combine(modelDir, name + "_n.png"), "tex" + name + "Normal", true);
            var emission = LoadTex(System.IO.Path.Combine(modelDir, name + "_e.jpg"), "tex" + name + "Emission", false);

            var go = new GameObject("Mesh");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MakeMaterial(name, albedo, normal, emission);
            return mr;
        }

        static Texture2D LoadTex(string path, string texName, bool linear)
        {
            if (!File.Exists(path)) return null;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear) { name = texName };
            t.LoadImage(File.ReadAllBytes(path), true);
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }

        static Material template;

        /// <summary>
        /// Clones a real vanilla item material (Crowbar: matte HGStandard with dithering) so lighting, rim and specular
        /// match every other item, then swaps in our albedo, normal and emission textures.
        /// </summary>
        static Material MakeMaterial(string name, Texture2D albedo, Texture2D normal, Texture2D emission)
        {
            if (!template) template = Items.Complete.CIC.Load<Material>("RoR2/Base/Crowbar/matCrowbar.mat");
            var m = template ? new Material(template) : new Material(Shader.Find("Standard"));
            m.name = "mat" + name;
            m.mainTexture = albedo;
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            if (normal && m.HasProperty("_NormalTex")) { m.SetTexture("_NormalTex", normal); m.SetFloat("_NormalStrength", 1f); }
            if (m.HasProperty("_EmColor"))
            {
                if (emission) { m.SetTexture("_EmTex", emission); m.SetColor("_EmColor", Color.white); m.SetFloat("_EmPower", 1f); }
                else m.SetColor("_EmColor", Color.black);
            }
            return m;
        }
    }

    /// <summary>Keeps a sprite facing the active camera. No per-frame allocations.</summary>
    public class CICBillboard : MonoBehaviour
    {
        private void OnWillRenderObject()
        {
            var cam = Camera.current;
            if (cam) transform.rotation = cam.transform.rotation;
        }
    }
}
