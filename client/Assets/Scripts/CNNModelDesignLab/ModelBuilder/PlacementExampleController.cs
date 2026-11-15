using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ModelDesignScene.ModelBuilder
{
    public sealed class PlacementExampleController : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Color GhostColor = new Color(0.30f, 0.84f, 1f, 0.46f);

        [SerializeField] private Material _outlineMaterial;
        [SerializeField] private Material _ghostMaterial;

        private Coroutine _animation;
        private ModelBlockOutline _outline;
        private MeshRenderer _ghost;
        private MaterialPropertyBlock _ghostProperties;

        internal bool IsPlaying => _animation != null;

        internal void Play((ModelBlockController Block, Renderer Pad) display, int slotIndex,
            ModelConveyorController conveyor)
        {
            _animation = StartCoroutine(Animate(display, slotIndex, conveyor));
        }

        internal void Cancel()
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
            }
            Clear();
        }

        private IEnumerator Animate((ModelBlockController Block, Renderer Pad) display, int slotIndex,
            ModelConveyorController conveyor)
        {
            _outline = new ModelBlockOutline(transform, "Placement example outline", _outlineMaterial);
            _outline.Show(display.Pad);
            yield return new WaitForSecondsRealtime(0.42f);

            CreateGhost(display.Block);
            Transform ghost = _ghost.transform;
            Vector3 start = display.Block.transform.position;
            Vector3 lifted = start + transform.up * 0.48f - transform.forward * 0.20f;
            for (float elapsed = 0f; elapsed < 0.48f; elapsed += Time.unscaledDeltaTime)
            {
                ghost.position = Vector3.Lerp(start, lifted, Mathf.SmoothStep(0f, 1f, elapsed / 0.48f));
                yield return null;
            }
            ghost.position = lifted;

            OutlineSlot(conveyor, slotIndex);
            yield return new WaitForSecondsRealtime(0.36f);

            Pose destination = conveyor.GetSlotPose(slotIndex);
            Quaternion rotation = ghost.rotation;
            Vector3 scale = ghost.localScale;
            for (float elapsed = 0f; elapsed < 1.15f; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / 1.15f);
                ghost.SetPositionAndRotation(
                    Vector3.Lerp(lifted, destination.position, t) + transform.up * (0.42f * Mathf.Sin(t * Mathf.PI)),
                    Quaternion.Slerp(rotation, destination.rotation, t));
                ghost.localScale = Vector3.Lerp(scale, Vector3.one, t);
                yield return null;
            }
            ghost.SetPositionAndRotation(destination.position, destination.rotation);
            ghost.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(0.85f);
            for (float elapsed = 0f; elapsed < 0.40f; elapsed += Time.unscaledDeltaTime)
            {
                SetGhostOpacity(Mathf.Lerp(GhostColor.a, 0f, elapsed / 0.40f));
                yield return null;
            }
            Clear();
        }

        private void OutlineSlot(ModelConveyorController conveyor, int slot)
        {
            _outline.Show(conveyor.GetSlotRenderer(slot));
        }

        private void CreateGhost(ModelBlockController source)
        {
            GameObject visual = new GameObject("Placement example ghost");
            visual.transform.SetParent(transform, false);
            visual.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            visual.transform.localScale = Vector3.one * (source.transform.lossyScale.x / transform.lossyScale.x);
            Mesh mesh = source.ExampleMesh;
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            _ghost = visual.AddComponent<MeshRenderer>();
            Material[] materials = new Material[mesh.subMeshCount];
            for (int index = 0; index < materials.Length; index++)
            {
                materials[index] = _ghostMaterial;
            }
            _ghost.sharedMaterials = materials;
            _ghost.shadowCastingMode = ShadowCastingMode.Off;
            _ghost.receiveShadows = false;
            _ghostProperties = new MaterialPropertyBlock();
            SetGhostOpacity(GhostColor.a);
        }

        private void SetGhostOpacity(float opacity)
        {
            Color color = GhostColor;
            color.a = opacity;
            _ghostProperties.SetColor(BaseColor, color);
            _ghost.SetPropertyBlock(_ghostProperties);
        }

        private void Clear()
        {
            _animation = null;
            if (_outline != null)
            {
                _outline.Remove();
                _outline = null;
            }
            if (_ghost != null)
            {
                _ghost.gameObject.SetActive(false);
                Destroy(_ghost.gameObject);
                _ghost = null;
            }
        }

        private void OnDisable() => Cancel();
    }
}
