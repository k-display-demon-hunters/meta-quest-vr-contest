using UnityEngine;
using UnityEngine.Rendering;

namespace CNNScreen
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PixelGridData), typeof(MeshRenderer))]
    public sealed class PixelShatter : MonoBehaviour
    {
        // Match the shader's fixed array capacity and stay below Unity's per-draw limit.
        private const int BatchSize = 250;
        private static readonly int InstanceColor = Shader.PropertyToID("_PixelColor");
        private static readonly int Coverage = Shader.PropertyToID("_Coverage");
        private static readonly int Highlight = Shader.PropertyToID("_HighlightColor");
        private static readonly int ImageOpacity = Shader.PropertyToID("_Opacity");

        [Tooltip("Fraction of the cell width and height before subtracting the gap.")]
        [Range(0.05f, 1f)] public float cubeSize = 1f;
        [Tooltip("Space between cells, in meters, at full cube size.")]
        [Min(0f)] public float gap = 0.012f;
        [Min(0.001f)] public float thickness = 0.075f;

        [Min(0f)] public float imageFadeDuration = 0.3f;
        [Min(0.01f)] public float burstDuration = 1f;
        [Min(0.01f)] public float returnDuration = 1f;
        public Vector2 burstDistance = new Vector2(0.1f, 0.6f);
        [Min(0f)] public float selectedDistance = 0.8f;
        [Min(0f)] public float bobAmplitude = 0.018f;
        [ColorUsage(false, true)] public Color highlightColor = new Color(0.28f, 0.9f, 1f);

        [SerializeField] private Mesh _cubeMesh;
        [SerializeField] private Material _cubeMaterial;
        [SerializeField] private Shader _imageFadeShader;
        [SerializeField] private PixelSelectionController _selection;
        [SerializeField] private PixelRevealStage _revealStage;
        [SerializeField] private ScreenLabUIController _ui;

        private PixelGridData _grid;
        private MeshRenderer _imageRenderer;
        private bool _previousRenderingOff;
        private bool _shownInPlayMode;
        private Batch[] _batches;
        private int _columns;
        private int _rows;
        private Matrix4x4 _lastBoardMatrix;
        private Vector3 _lastCubeSettings;
        private bool _subscribed;
        private bool _startedThisRun;
        private VisualPhase _phase;
        private float _elapsed;
        private float _transitionElapsed;
        private float _returnStartSpread;
        private float _fadeInStartOpacity;
        private readonly Matrix4x4[] _outlineMatrices = new Matrix4x4[4];
        private MaterialPropertyBlock _outlineProperties;
        private readonly Vector4[] _outlineColors = new Vector4[4];
        private Vector2Int _focus;
        private Material _originalImageMaterial;
        private Material _fadingImageMaterial;

        private enum VisualPhase
        {
            Hidden,
            Preview,
            Burst,
            Returning,
            FadingIn,
            Outline
        }

        private void OnEnable()
        {
            if (!Application.IsPlaying(gameObject)) return;

            _selection.SelectionChanged += HandleSelectionChanged;
            _revealStage.PhaseChanged += HandleRevealPhaseChanged;
            _ui.NextRequested += HandleNextRequested;
            _ui.ResetRequested += HandleResetRequested;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                _selection.SelectionChanged -= HandleSelectionChanged;
                _revealStage.PhaseChanged -= HandleRevealPhaseChanged;
                _ui.NextRequested -= HandleNextRequested;
                _ui.ResetRequested -= HandleResetRequested;
                _subscribed = false;
            }
            HideGrid();
            _startedThisRun = false;
        }

        private void HandleSelectionChanged()
        {
            if (!_selection.HasSelection || _revealStage.Phase != PixelObservationPhase.Inactive) return;

            _grid = GetComponent<PixelGridData>();
            if (!_grid.TryGetPatchCenter(_selection.SelectionWorldPosition, out Vector2Int center)) return;

            if (_startedThisRun)
            {
                if (_phase != VisualPhase.Hidden && _phase != VisualPhase.Preview) SetFocus(center);
                return;
            }

            HideGrid();
            if (!PrepareGrid()) return;

            _startedThisRun = true;
            _phase = VisualPhase.Burst;
            _elapsed = 0f;
            SetFocus(center);
            if (imageFadeDuration > 0f) SetImageOpacity(1f);
            else _imageRenderer.forceRenderingOff = true;
            UpdateMatrices(transform.localToWorldMatrix, 0f);
        }

        private void HandleNextRequested()
        {
            // The first accepted Next after a valid selection advances the first pixel page.
            if (_phase != VisualPhase.Burst || _selection.IsFilterHeld
                || _revealStage.Phase != PixelObservationPhase.Inactive) return;

            _returnStartSpread = BurstSpread();
            _transitionElapsed = 0f;
            _phase = VisualPhase.Returning;
        }

        private void HandleRevealPhaseChanged(PixelObservationPhase phase)
        {
            if (_phase != VisualPhase.Preview && phase == PixelObservationPhase.Revealing) HideGrid();
        }

        private void HandleResetRequested()
        {
            if (_selection.IsFilterHeld) return;

            HideGrid();
            _startedThisRun = false;
        }

        [ContextMenu(nameof(ShowGrid))]
        public void ShowGrid()
        {
            if (!isActiveAndEnabled) return;

            HideGrid();
            if (!PrepareGrid()) return;

            _phase = VisualPhase.Preview;
            UpdateMatrices(transform.localToWorldMatrix);
            _imageRenderer.forceRenderingOff = true;
            RequestEditorPreview();
        }

        private bool PrepareGrid()
        {
            _grid = GetComponent<PixelGridData>();
            _imageRenderer = GetComponent<MeshRenderer>();
            if (!_grid.TryPrepare(out string error))
            {
                Debug.LogWarning(error, this);
                return false;
            }
            if (!SystemInfo.supportsInstancing || _cubeMesh == null || _cubeMaterial == null || !_cubeMaterial.enableInstancing)
            {
                Debug.LogWarning("Pixel Shatter needs GPU instancing, a cube mesh and an instanced material.", this);
                return false;
            }

            _previousRenderingOff = _imageRenderer.forceRenderingOff;
            _shownInPlayMode = Application.IsPlaying(gameObject);
            CreateBatches();
            return true;
        }

        [ContextMenu(nameof(HideGrid))]
        public void HideGrid()
        {
            if (_phase == VisualPhase.Hidden) return;

            _batches = null;
            _phase = VisualPhase.Hidden;
            RestoreImageMaterial();
            _imageRenderer.forceRenderingOff = _previousRenderingOff;
            RequestEditorPreview();
        }

        private void LateUpdate()
        {
            if (_phase == VisualPhase.Hidden) return;
            if (_shownInPlayMode != Application.IsPlaying(gameObject))
            {
                HideGrid();
                return;
            }

            Matrix4x4 boardMatrix = transform.localToWorldMatrix;
            float coverage = UpdateAnimation(boardMatrix);

            RenderParams draw = new RenderParams(_cubeMaterial)
            {
                layer = gameObject.layer,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                lightProbeUsage = LightProbeUsage.Off
            };

            if (_phase == VisualPhase.Outline) DrawOutline(draw, boardMatrix);
            else
            {
                foreach (Batch batch in _batches)
                {
                    batch.Properties.SetFloat(Coverage, coverage);
                    batch.Properties.SetColor(Highlight, highlightColor);
                    draw.matProps = batch.Properties;
                    Graphics.RenderMeshInstanced(draw, _cubeMesh, 0, batch.Matrices, batch.Matrices.Length);
                }
            }

            RequestEditorPreview();
        }

        private float BurstSpread()
        {
            float remaining = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, burstDuration));
            return 1f - remaining * remaining * remaining;
        }

        private float UpdateAnimation(Matrix4x4 boardMatrix)
        {
            switch (_phase)
            {
                case VisualPhase.Preview:
                    Vector3 settings = new Vector3(cubeSize, gap, thickness);
                    if (boardMatrix != _lastBoardMatrix || settings != _lastCubeSettings) UpdateMatrices(boardMatrix);
                    break;
                case VisualPhase.Burst:
                    _elapsed += Time.unscaledDeltaTime;
                    UpdateMatrices(boardMatrix, BurstSpread());
                    return UpdateFadeOut(_elapsed);
                case VisualPhase.Returning:
                    _transitionElapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(_transitionElapsed / Mathf.Max(0.01f, returnDuration));
                    UpdateMatrices(boardMatrix, _returnStartSpread, Mathf.SmoothStep(0f, 1f, t));
                    float coverage = UpdateFadeOut(_elapsed + _transitionElapsed);
                    if (t >= 1f) BeginFadeIn(1f - coverage);
                    return coverage;
                case VisualPhase.FadingIn:
                    _transitionElapsed += Time.unscaledDeltaTime;
                    float fade = Mathf.Clamp01(_transitionElapsed / Mathf.Max(0.001f, imageFadeDuration));
                    float opacity = Mathf.Lerp(_fadeInStartOpacity, 1f, fade);
                    if (fade >= 1f) FinishReturn();
                    else
                    {
                        UpdateMatrices(boardMatrix, _returnStartSpread, 1f);
                        SetImageOpacity(opacity);
                    }
                    return 1f - opacity;
            }
            return 1f;
        }

        private void BeginFadeIn(float startOpacity)
        {
            _imageRenderer.forceRenderingOff = _previousRenderingOff;
            if (imageFadeDuration <= 0f)
            {
                FinishReturn();
                return;
            }

            _phase = VisualPhase.FadingIn;
            _transitionElapsed = 0f;
            _fadeInStartOpacity = startOpacity;
            SetImageOpacity(startOpacity);
        }

        private void FinishReturn()
        {
            RestoreImageMaterial();
            _batches = null;
            _phase = VisualPhase.Outline;
        }

        private void DrawOutline(RenderParams draw, Matrix4x4 boardMatrix)
        {
            Bounds surface = _grid.SurfaceBounds;
            float cellWidth = surface.size.x / _columns;
            float cellHeight = surface.size.y / _rows;
            int left = Mathf.Max(0, _focus.x - 1);
            int right = Mathf.Min(_columns, _focus.x + 2);
            int bottom = Mathf.Max(0, _focus.y - 1);
            int top = Mathf.Min(_rows, _focus.y + 2);
            float width = (right - left) * cellWidth;
            float height = (top - bottom) * cellHeight;
            float scaleX = ((Vector3)boardMatrix.GetColumn(0)).magnitude;
            float scaleY = ((Vector3)boardMatrix.GetColumn(1)).magnitude;
            float scaleZ = ((Vector3)boardMatrix.GetColumn(2)).magnitude;
            Vector3 center = new Vector3(
                surface.min.x + (left + right) * 0.5f * cellWidth,
                surface.min.y + (bottom + top) * 0.5f * cellHeight,
                surface.center.z - 0.006f / scaleZ);
            float edgeX = 0.006f / scaleX;
            float edgeY = 0.006f / scaleY;
            float depth = 0.002f / scaleZ;
            Vector3 horizontal = new Vector3(width + edgeX, edgeY, depth);
            Vector3 vertical = new Vector3(edgeX, height, depth);
            Vector3 halfWidth = Vector3.right * (width * 0.5f);
            Vector3 halfHeight = Vector3.up * (height * 0.5f);
            _outlineMatrices[0] = boardMatrix * Matrix4x4.TRS(center + halfHeight, Quaternion.identity, horizontal);
            _outlineMatrices[1] = boardMatrix * Matrix4x4.TRS(center - halfHeight, Quaternion.identity, horizontal);
            _outlineMatrices[2] = boardMatrix * Matrix4x4.TRS(center - halfWidth, Quaternion.identity, vertical);
            _outlineMatrices[3] = boardMatrix * Matrix4x4.TRS(center + halfWidth, Quaternion.identity, vertical);

            _outlineProperties ??= new MaterialPropertyBlock();
            Color color = QualitySettings.activeColorSpace == ColorSpace.Linear ? highlightColor.linear : highlightColor;
            for (int i = 0; i < _outlineColors.Length; i++)
            {
                _outlineColors[i] = new Vector4(color.r, color.g, color.b, 0f);
            }
            _outlineProperties.SetVectorArray(InstanceColor, _outlineColors);
            _outlineProperties.SetFloat(Coverage, 1f);
            draw.matProps = _outlineProperties;
            Graphics.RenderMeshInstanced(draw, _cubeMesh, 0, _outlineMatrices, _outlineMatrices.Length);
        }

        private void CreateBatches()
        {
            _columns = _grid.gridWidth;
            _rows = _grid.gridHeight;
            int total = _columns * _rows;
            _batches = new Batch[(total + BatchSize - 1) / BatchSize];
            bool toLinear = QualitySettings.activeColorSpace == ColorSpace.Linear && _grid.SourceIsSrgb;
            System.Random pattern = new System.Random(2147);

            for (int batchIndex = 0; batchIndex < _batches.Length; batchIndex++)
            {
                int first = batchIndex * BatchSize;
                int count = Mathf.Min(BatchSize, total - first);
                Batch batch = new Batch(count);
                for (int i = 0; i < count; i++)
                {
                    int index = first + i;
                    Color color = _grid.GetColor(index % _columns, index / _columns);
                    if (toLinear) color = color.linear;
                    batch.Colors[i] = new Vector4(color.r, color.g, color.b, 0f);
                    batch.DepthSamples[i] = (float)pattern.NextDouble();
                    batch.BobPhases[i] = (float)pattern.NextDouble() * (2f * Mathf.PI);
                }
                batch.UploadColors();
                _batches[batchIndex] = batch;
            }
        }

        private void SetFocus(Vector2Int center)
        {
            _focus = center;

            // Keep the departing 3×3 in place if a new selection comes in mid-return.
            if (_phase != VisualPhase.Burst) return;

            for (int batchIndex = 0; batchIndex < _batches.Length; batchIndex++)
            {
                Batch batch = _batches[batchIndex];
                for (int i = 0; i < batch.Colors.Length; i++)
                {
                    int index = batchIndex * BatchSize + i;
                    batch.Colors[i].w = Mathf.Abs(index % _columns - _focus.x) <= 1
                        && Mathf.Abs(index / _columns - _focus.y) <= 1 ? 1f : 0f;
                }
                batch.UploadColors();
            }
        }

        private void SetImageOpacity(float opacity)
        {
            if (_fadingImageMaterial == null)
            {
                _originalImageMaterial = _imageRenderer.sharedMaterial;
                _fadingImageMaterial = new Material(_imageFadeShader) { hideFlags = HideFlags.DontSave };
                _fadingImageMaterial.mainTexture = _originalImageMaterial.mainTexture;
                _imageRenderer.sharedMaterial = _fadingImageMaterial;
            }

            _fadingImageMaterial.SetFloat(ImageOpacity, opacity);
        }

        private float UpdateFadeOut(float elapsed)
        {
            if (_fadingImageMaterial == null) return 1f;

            float coverage = imageFadeDuration > 0f ? Mathf.Clamp01(elapsed / imageFadeDuration) : 1f;
            SetImageOpacity(1f - coverage);
            if (coverage >= 1f)
            {
                _imageRenderer.forceRenderingOff = true;
                RestoreImageMaterial();
            }
            return coverage;
        }

        private void RestoreImageMaterial()
        {
            if (_fadingImageMaterial == null) return;

            _imageRenderer.sharedMaterial = _originalImageMaterial;
            if (Application.IsPlaying(gameObject)) Destroy(_fadingImageMaterial);
            else DestroyImmediate(_fadingImageMaterial);
            _fadingImageMaterial = null;
            _originalImageMaterial = null;
        }

        private void UpdateMatrices(Matrix4x4 boardMatrix, float spread = 1f, float rejoin = 0f)
        {
            Bounds surface = _grid.SurfaceBounds;
            float cellWidth = surface.size.x / _columns;
            float cellHeight = surface.size.y / _rows;
            float scaleX = ((Vector3)boardMatrix.GetColumn(0)).magnitude;
            float scaleY = ((Vector3)boardMatrix.GetColumn(1)).magnitude;
            float scaleZ = ((Vector3)boardMatrix.GetColumn(2)).magnitude;
            float depth = Mathf.Lerp(0.002f, thickness, spread) / scaleZ;
            float openGap = gap * spread;
            float fill = Mathf.Lerp(1f, cubeSize, spread);
            Vector3 size = new Vector3(
                Mathf.Max(cellWidth * 0.01f, cellWidth * fill - openGap / scaleX),
                Mathf.Max(cellHeight * 0.01f, cellHeight * fill - openGap / scaleY),
                depth);
            bool animate = _phase != VisualPhase.Preview;
            float bobWeight = animate ? Mathf.SmoothStep(0f, 1f, (_elapsed - burstDuration) / 0.25f) : 0f;
            Vector3 joinedSize = new Vector3(cellWidth, cellHeight, 0.002f / scaleZ);
            float near = Mathf.Max(0f, Mathf.Min(burstDistance.x, burstDistance.y));
            float far = Mathf.Max(near, Mathf.Max(burstDistance.x, burstDistance.y));

            for (int batchIndex = 0; batchIndex < _batches.Length; batchIndex++)
            {
                Batch batch = _batches[batchIndex];
                Matrix4x4[] matrices = batch.Matrices;
                for (int i = 0; i < matrices.Length; i++)
                {
                    int index = batchIndex * BatchSize + i;
                    float travel = animate
                        ? Mathf.Lerp(near, far, batch.DepthSamples[i])
                        : 0f;
                    float lift = 0f;
                    if (animate)
                    {
                        travel = Mathf.Lerp(travel, selectedDistance, batch.Colors[i].w);
                        float phase = batch.BobPhases[i];
                        travel += Mathf.Sin(_elapsed * 2.15f + phase) * bobAmplitude * bobWeight;
                        lift = Mathf.Sin(_elapsed * 1.7f + phase) * bobAmplitude * bobWeight / scaleY;
                    }
                    Vector3 center = new Vector3(
                        surface.min.x + (index % _columns + 0.5f) * cellWidth,
                        surface.min.y + (index / _columns + 0.5f) * cellHeight,
                        surface.center.z);
                    Vector3 floating = center + new Vector3(0f, lift, -depth * 0.5f - travel * spread / scaleZ);
                    Vector3 joined = center + Vector3.back * (joinedSize.z * 0.5f);
                    float blend = batch.Colors[i].w > 0.5f ? 0f : rejoin;
                    matrices[i] = boardMatrix * Matrix4x4.TRS(
                        Vector3.Lerp(floating, joined, blend), Quaternion.identity, Vector3.Lerp(size, joinedSize, blend));
                }
            }

            _lastBoardMatrix = boardMatrix;
            _lastCubeSettings = new Vector3(cubeSize, gap, thickness);
        }

        private void RequestEditorPreview()
        {
#if UNITY_EDITOR
            if (!Application.IsPlaying(gameObject))
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }

        private sealed class Batch
        {
            internal readonly Matrix4x4[] Matrices;
            internal readonly Vector4[] Colors;
            internal readonly float[] DepthSamples;
            internal readonly float[] BobPhases;
            internal readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();

            internal Batch(int count)
            {
                Matrices = new Matrix4x4[count];
                Colors = new Vector4[count];
                DepthSamples = new float[count];
                BobPhases = new float[count];
            }

            internal void UploadColors() => Properties.SetVectorArray(InstanceColor, Colors);
        }
    }
}
