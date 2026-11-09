using System;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace FeatureMapCorridor
{
    [System.Serializable]
    internal sealed class CorridorVisuals : IDisposable
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private MaterialPropertyBlock _properties;
        private List<UnityEngine.Object> _assets = new List<UnityEngine.Object>();
        [SerializeField] private List<Transform> _billboards = new List<Transform>();
        [SerializeField] private Material _opaque;
        [SerializeField] private Sprite _rounded;
        [SerializeField] private Material _transparent;
        [SerializeField] private Material _slab;
        [SerializeField] private TMP_FontAsset _korean;
        [SerializeField] private TMP_FontAsset _mono;
        [SerializeField] private GameObject _button;

        [SerializeField] private CorridorTheme _theme;
        internal CorridorTheme Theme => _theme;
        internal TMP_FontAsset DebugFont => _korean;
        internal CorridorPointer Pointer { get; set; }

        private readonly List<Action> _subscribe = new List<Action>();
        private readonly List<Action> _unsubscribe = new List<Action>();
#if UNITY_EDITOR
        private readonly Dictionary<(Material, Color), Material> _colored = new Dictionary<(Material, Color), Material>();
#endif

        private CorridorVisuals() { }

        internal void InitializeRuntime()
        {
            _properties = new MaterialPropertyBlock();
            _assets.Clear();
            _subscribe.Clear();
            _unsubscribe.Clear();
        }

        internal void Listen(Collider collider, Action<Vector3> clicked,
            Action<Vector3> doubleClicked = null, Action<Vector3?> hovered = null)
        {
            Pointer.Register(collider, clicked, doubleClicked, hovered);
        }

        internal void Listen(Button button, Action clicked)
        {
            UnityEngine.Events.UnityAction listener = () =>
            {
                Pointer.ButtonPressed(button.GetComponentInChildren<Text>(true).text);
                clicked();
            };
            _subscribe.Add(() => button.onClick.AddListener(listener));
            _unsubscribe.Add(() => button.onClick.RemoveListener(listener));
        }

        internal void EnableInput()
        {
            foreach (Action subscribe in _subscribe) { subscribe(); }
        }

        internal void DisableInput()
        {
            foreach (Action unsubscribe in _unsubscribe) { unsubscribe(); }
        }

#if UNITY_EDITOR
        internal CorridorVisuals(CorridorTheme theme, GameObject button)
        {
            _properties = new MaterialPropertyBlock();
            _theme = theme;
            _button = button;

            _opaque = Own(new Material(theme.Unlit) { enableInstancing = true });
            _transparent = Own(new Material(theme.Unlit));
            _transparent.SetFloat("_Surface",1);
            _transparent.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
            _transparent.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            _transparent.SetFloat("_ZWrite",0);
            _transparent.SetFloat("_Cull",0);
            _transparent.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _transparent.renderQueue = (int)RenderQueue.Transparent;
            _slab = Own(new Material(theme.Lit));
            _slab.SetFloat("_Metallic",.1f);
            _slab.SetFloat("_Smoothness",.25f);
            _korean = Own(TMP_FontAsset.CreateFontAsset(theme.Korean, 52, 7, GlyphRenderMode.SDFAA, 2048, 2048));
            _mono = Own(TMP_FontAsset.CreateFontAsset(theme.Mono, 44, 6, GlyphRenderMode.SDFAA, 1024, 1024));
            _mono.fallbackFontAssetTable = new List<TMP_FontAsset> { _korean };
        }
#endif

#if UNITY_EDITOR
        internal Sprite RoundedSprite()
        {
            if (_rounded != null) { return _rounded; }
            const int size = 80;
            const int radius = 14;
            var texture = Own(new Texture2D(size,size,TextureFormat.RGBA32,false) { wrapMode = TextureWrapMode.Clamp });
            var pixels = new Color32[size*size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0,radius-Mathf.Min(x+.5f,size-x-.5f));
                float dy = Mathf.Max(0,radius-Mathf.Min(y+.5f,size-y-.5f));
                byte alpha = (byte)Mathf.RoundToInt(255*Mathf.Clamp01(radius+.5f-Mathf.Sqrt(dx*dx+dy*dy)));
                pixels[y*size+x] = new Color32(255,255,255,alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false,true);
            _rounded = Own(Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,100,0,
                SpriteMeshType.FullRect,Vector4.one*radius));
            return _rounded;
        }
#endif

        internal T Own<T>(T asset) where T : UnityEngine.Object
        {
            _assets.Add(asset);
            return asset;
        }

#if UNITY_EDITOR
        internal static Transform Group(Transform parent, string name, Vector3 position)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = position;
            return group;
        }
#endif

#if UNITY_EDITOR
        internal Renderer Shape(Transform parent, string name, Vector3 position, Vector3 scale, Color color,
            PrimitiveType type = PrimitiveType.Cube, bool collider = false, bool transparent = false, bool lit = false)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            Collider hitbox = shape.GetComponent<Collider>();
            if (!collider && hitbox != null)
            {
                hitbox.enabled = false;
                UnityEngine.Object.DestroyImmediate(hitbox);
            }
            Renderer renderer = shape.GetComponent<Renderer>();
            renderer.sharedMaterial = lit ? _slab : transparent ? _transparent : _opaque;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Paint(renderer, color);
            return renderer;
        }
#endif

        internal void Paint(Renderer target, Color color)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var key = (target.sharedMaterial, color);
                if (!_colored.TryGetValue(key, out Material material))
                {
                    material = Own(new Material(target.sharedMaterial) { name = target.name + " color" });
                    material.SetColor(BaseColor, color);
                    _colored.Add(key, material);
                }
                target.sharedMaterial = material;
                return;
            }
#endif
            target.GetPropertyBlock(_properties);
            _properties.SetColor(BaseColor, color);
            target.SetPropertyBlock(_properties);
        }

        internal void Texture(Renderer target, Texture2D texture)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Material material = Own(new Material(target.sharedMaterial) { name = target.name + " texture" });
                material.SetTexture(BaseMap, texture);
                target.sharedMaterial = material;
                return;
            }
#endif
            target.GetPropertyBlock(_properties);
            _properties.SetTexture(BaseMap,texture);
            target.SetPropertyBlock(_properties);
        }

#if UNITY_EDITOR
        internal Texture2D Pixels(int size)
        {
            return Own(new Texture2D(size,size,TextureFormat.RGBA32,false) {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            });
        }
#endif

#if UNITY_EDITOR
        internal TMP_Text Label(Transform parent, string text, Vector3 position, float size, float width, Color color,
            bool billboard = true, bool mono = false)
        {
            var holder = new GameObject("Label",typeof(RectTransform));
            var labelTransform = (RectTransform)holder.transform;
            labelTransform.SetParent(parent,false);
            labelTransform.localPosition = position;
            var label = holder.AddComponent<TextMeshPro>();
            label.font = mono ? _mono : _korean;
            label.fontSize = size * 10;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(width,1);
            label.text = text;
            if (billboard) { _billboards.Add(label.transform); }
            return label;
        }
#endif

#if UNITY_EDITOR
        internal TMP_Text CanvasText(Transform parent, string name, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var holder = new GameObject(name,typeof(RectTransform));
            var rect = (RectTransform)holder.transform;
            rect.SetParent(parent,false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = holder.AddComponent<TextMeshProUGUI>();
            text.font = _korean;
            text.color = color;
            text.fontSize = fontSize;
            text.fontSizeMin = 27;
            text.fontSizeMax = fontSize;
            text.enableAutoSizing = true;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }
#endif

#if UNITY_EDITOR
        internal void FaceCamera(Transform target) => _billboards.Add(target);
#endif

        internal void UpdateBillboards(Camera camera)
        {
            for (int i = _billboards.Count - 1; i >= 0; i--)
            {
                Transform billboard = _billboards[i];
                if (billboard == null)
                {
                    _billboards.RemoveAt(i);
                    continue;
                }
                if (!billboard.gameObject.activeInHierarchy) { continue; }
                Vector3 forward = billboard.position - camera.transform.position;
                if (forward.sqrMagnitude > .0001f)
                    billboard.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
        }

#if UNITY_EDITOR
        internal WorldUIButtonController Button(Transform parent, string caption, Vector3 position, Vector2 size,
            Action action, bool primary = false, bool nested = false)
        {
            var button = UnityEngine.Object.Instantiate(_button, parent);
            var rect = (RectTransform)button.transform;
            rect.localPosition = position;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * (nested ? 1 : .001f);
            rect.sizeDelta = size;
            if (nested) { EnableNestedButton(button,1); }
            button.AddComponent<CorridorMouseRaycaster>().ignoreReversedGraphics = false;
            var controller = button.GetComponent<WorldUIButtonController>();
            var text = button.GetComponentInChildren<Text>();
            text.font = Theme.Korean;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 37;
            if (primary)
            {
                text.fontStyle = FontStyle.Bold;
                var outline = button.GetComponent<Outline>();
                outline.effectDistance = new Vector2(2,2);
            }
            controller.SetText(caption);
            button.GetComponent<Button>().onClick.AddListener(() => action());
            return controller;
        }
#endif

#if UNITY_EDITOR
        internal static void EnableNestedButton(GameObject button, int order)
        {
            Canvas canvas = button.GetComponent<Canvas>();
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
            button.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>().enabled = true;
        }
#endif

#if UNITY_EDITOR
        internal static void Click(Collider collider, Action<Vector3> clicked, bool trigger = true)
        {
            collider.gameObject.layer = LayerMask.NameToLayer("CorridorInteractable");
            // XRI's collider discovery skips triggers. Register the collider before enabling trigger mode.
            var interactable = collider.gameObject.AddComponent<XRSimpleInteractable>();
            interactable.colliders.Add(collider);
            collider.isTrigger = trigger;

            interactable.activated.AddListener(args =>
            {
                if (args.interactorObject is XRRayInteractor ray && ray.TryGetCurrent3DRaycastHit(out RaycastHit hit)
                    && hit.collider == collider)
                    clicked(hit.point);
            });
        }
#endif

#if UNITY_EDITOR
        internal void Line(LineRenderer line, Color color, float width)
        {
            line.sharedMaterial = _transparent;
            line.startWidth = width;
            line.endWidth = width;
            Paint(line,color);
        }
#endif

        internal void InstanceLights(Mesh mesh, Matrix4x4[] transforms, int count, Color color, int layer)
        {
            _properties.Clear();
            _properties.SetColor(BaseColor,color);
            if (!SystemInfo.supportsInstancing) { return; }
            Graphics.DrawMeshInstanced(mesh,0,_opaque,transforms,count,_properties,ShadowCastingMode.Off,false,layer);
        }

        public void Dispose()
        {
            foreach (UnityEngine.Object asset in _assets) { UnityEngine.Object.Destroy(asset); }
        }
    }
}
