using System;
using System.Collections;
using System.Collections.Generic;
using Learning;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FeatureMapCorridor
{
    public sealed class CorridorDirector : MonoBehaviour, ILearningExperience
    {
        private enum Phase { Start, Tour, AwaitRound, Round, Recap, AwaitOff, OffResult, AwaitOn, OnResult, Complete }
        [SerializeField] private CorridorTheme _theme;
        [SerializeField] private CnnModelProvider _provider;
        [SerializeField] private GameObject _button;

        [SerializeField] private GameObject _popup;
        [SerializeField] private Transform _root;

        private static readonly Vector3[][] MapPositions = {
            new[] { new Vector3(-2.5f,1.5f,-4),new Vector3(2.5f,1.5f,-2.4f) },
            new[] { new Vector3(-2.5f,.9f,1),new Vector3(2.5f,.9f,2.6f),new Vector3(-2.5f,.9f,4.2f) },
            new[] { new Vector3(2.5f,.3f,7.6f),new Vector3(-2.5f,.3f,9.2f),new Vector3(2.5f,.3f,10.8f),
                new Vector3(-2.5f,.3f,12.4f),new Vector3(2.5f,.3f,14) }
        };

        private readonly HashSet<string> _off = new HashSet<string>();
        private ICnnModel _model;
        private LayerOutput[][] _forward;
        private float[][] _values;
        [SerializeField] private CorridorVisuals _visuals;
        [SerializeField] private CorridorBuilder _builder;
        [SerializeField] private InputPanel _input;
        private FeatureMapPanel[][] _maps;
        [SerializeField] private MapLinks _links;
        [SerializeField] private ReceptiveFieldTour _tour;
        [SerializeField] private RoundPlayer _round;
        [SerializeField] private OutputStage _output;
        [SerializeField] private SubtitlePanel _subtitle;
        [SerializeField] private HintBubbles _hints;
        [SerializeField] private CompletionPopup _completion;
        [SerializeField] private CorridorDoor _door;
        private Phase _phase;
        private Phase _optionalReturn;
        private int _digit = 2;
        private int _tourStage;
        private int _tourIndex;
        private int _mandatoryLayer = 1;
        private bool _mandatoryRound;
        private readonly int[] _savedRoundIndices = new int[4];
        private int _switchLayer;
        private int _switchMap;
        private bool _tourIntroduced;
        private bool _tourIntroThisVisit;
        private bool _waitingForFilter;
        private bool _filterButtonPending;
        private bool _roomVisible;
        [System.Serializable]
        private sealed class MapLayerView
        {
            public FeatureMapPanel[] Panels;
        }

        [SerializeField] private MapLayerView[] _layers;

        [SerializeField] private Volume _volume;
        private UniversalAdditionalCameraData _renderCamera;
        private bool _previousPostProcessing;
        private bool _standalone;
        private Camera _camera;

        public LearningStageId StageId => LearningStageId.FeatureMap;
        public float Progress { get; private set; }
        public bool CanExit => _phase == Phase.Complete;
        public event Action<float> ProgressChanged;
        public event Action<bool> ExitAvailabilityChanged;

        private void Awake()
        {
            _provider.Initialize();
            _model = _provider.Model;
            _visuals.InitializeRuntime();
            _input.Bind(_visuals, InputSelected, DigitSelected);
            _maps = new FeatureMapPanel[_layers.Length][];
            for (int l = 0; l < _layers.Length; l++)
            {
                int layer = l + 1;
                _maps[l] = _layers[l].Panels;
                for (int m = 0; m < _maps[l].Length; m++)
                {
                    int map = m;
                    _maps[l][m].Bind(_visuals, index => MapSelected(layer, map, index));
                    _maps[l][m].BindSwitch(() => Toggle(layer, map));
                }
            }
            _builder.Bind(_visuals);
            _output.Bind(_visuals, _model, _input, _maps[2], TinSelected);
            _links.Bind(_visuals, _root, _input, _maps, _output);
            _tour.Bind(_visuals, _input.Picture, _maps, _model.Size);
            _round.Bind(_visuals, _model, _input, _maps, _output);
            _subtitle.Bind(_visuals, Previous, Next);
            _hints.Bind(_visuals);
            _completion.Bind();
            _door.Bind(_visuals);
            Recalculate();
            ShowStart();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) { return; }
            _builder.ZonePressed += ToggleZone;
            _round.FilterPressed += FilterChanged;
            _round.EnableInput();
            _visuals.EnableInput();
        }

#if UNITY_EDITOR
        public void BuildLayoutForEditor()
        {
            _provider.Initialize();
            _model = _provider.Model;
            _visuals = new CorridorVisuals(_theme,_button);
            _input = new InputPanel(_visuals,_root,_model.Size,InputSelected,DigitSelected);
            _maps = new FeatureMapPanel[_model.MapsPerLayer.Length][];
            for (int l = 0; l < _maps.Length; l++)
            {
                int layer = l+1;
                _maps[l] = new FeatureMapPanel[_model.MapsPerLayer[l]];
                for (int m = 0; m < _maps[l].Length; m++)
                {
                    int map = m;
                    Vector3 position = MapPositions[l][m];
                    var panel = new FeatureMapPanel(_visuals,_root,position,Mathf.Sign(position.x)*41,
                        _model.Size,CorridorDialogue.MapName(layer,map),CorridorDialogue.Sources[l],
                        index => MapSelected(layer,map,index));
                    panel.AddSwitch(() => Toggle(layer,map));
                    _maps[l][m] = panel;
                }
            }
            _builder = new CorridorBuilder(_visuals,_root,_model.MapsPerLayer);
            _output = new OutputStage(_visuals,_root,_model,_input,_maps[2],TinSelected);
            _links = new MapLinks(_visuals,_root,_input,_maps,_output);
            _tour = new ReceptiveFieldTour(_visuals,_root,_input.Picture,_maps,_model.Size);
            _round = new RoundPlayer(_visuals,_root,_model,_input,_maps,_output);
            _subtitle = new SubtitlePanel(_visuals,transform,Previous,Next);
            _hints = new HintBubbles(_visuals,_root);
            _completion = new CompletionPopup(_popup,transform,_visuals);
            _door = new CorridorDoor(_visuals,_root);
            _layers = new MapLayerView[_maps.Length];
            for (int i = 0; i < _maps.Length; i++) { _layers[i] = new MapLayerView { Panels = _maps[i] }; }
            _volume = gameObject.AddComponent<Volume>();
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Corridor post processing";
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 1;
            bloom.intensity.value = .6f;
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = .14f;
            _volume.isGlobal = true;
            _volume.priority = 10;
            _volume.sharedProfile = profile;
            _volume.enabled = false;
            Recalculate();
            ShowStart();
        }

#endif

        private void Start()
        {
            _standalone = gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (_standalone)
            {
#if UNITY_EDITOR
                if (!UnityEngine.XR.XRSettings.isDeviceActive)
                {
                    var rig = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                    float height = rig.Camera.transform.position.y - rig.Origin.transform.position.y;
                    if (height < .5f)
                        rig.CameraFloorOffsetObject.transform.localPosition += Vector3.up * (rig.CameraYOffset - height);
                }
#endif
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 16;
                RenderSettings.fogEndDistance = 46;
                RenderSettings.fogColor = _theme.Void;
            }
            _volume.enabled = false;
        }

        private void LateUpdate()
        {
            if (_camera == null) { _camera = Camera.main; }
            if (_camera == null) { return; }
            Vector3 view = _root.InverseTransformPoint(_camera.transform.position);
            bool inside = Mathf.Abs(view.x) <= 5.7f && view.z >= -11.3f && view.z <= 26.8f;
            if (_roomVisible != inside)
            {
                _roomVisible = inside;
                _subtitle.SetVisible(inside);
                _volume.enabled = inside;
                if (inside)
                {
                    _renderCamera = _camera.GetUniversalAdditionalCameraData();
                    _previousPostProcessing = _renderCamera.renderPostProcessing;
                    _renderCamera.renderPostProcessing = true;
                    if (_standalone) { _camera.backgroundColor = _theme.Void; }
                }
                else { RestoreCamera(); }
            }
            if (!_roomVisible) { return; }
            float now = Time.unscaledTime;
            float delta = Time.unscaledDeltaTime;
            _input.Tick(now);
            _builder.DrawFlow(now);
            _links.Tick(now,_off);
            _visuals.UpdateBillboards(_camera);
            _hints.Tick(_camera,now);
            _subtitle.Track(_camera,delta);
            _completion.Tick(delta);
            _door.Tick(delta);
            _output.Tick(now);
            for (int layer = 0; layer < _maps.Length; layer++)
            for (int map = 0; map < _maps[layer].Length; map++)
                _maps[layer][map].AnimateSwitch(delta,!_forward[layer][map].dead);
            if (_phase == Phase.Tour) { _tour.Tick(now); }
            if (_phase != Phase.Round) { return; }
            _round.Tick(now);
            if (_filterButtonPending && now >= _round.ButtonReadyAt)
            {
                _filterButtonPending = false;
                RenderRound();
            }
        }

        private void Recalculate()
        {
            _values = _model.Input(_digit);
            _forward = _model.Forward(_digit,_off);
            _input.Draw(_values);
            for (int layer = 0; layer < _maps.Length; layer++)
            for (int map = 0; map < _maps[layer].Length; map++)
            {
                LayerOutput value = _forward[layer][map];
                _maps[layer][map].UpdateMap(value.act,_model.Scale(layer+1,map),value.dead);
            }
            _output.SetData(_forward);
        }

        private void DigitSelected(int digit)
        {
            if (_digit == digit || CanExit) { return; }
            _digit = digit;
            if (_phase == Phase.Round || _phase == Phase.Recap)
            {
                _round.Close();
                _links.Dimmed = false;
                _waitingForFilter = false;
                _filterButtonPending = false;
                if (_mandatoryRound) { AwaitRound(); }
                else { RestoreOptional(); }
            }
            Recalculate();
            if (_phase == Phase.AwaitRound) { AwaitRound(); }
        }

        private void ToggleZone(int zone) => Highlight(_builder.HighlightedZone == zone ? -1 : zone);
        private void Highlight(int zone)
        {
            _builder.Highlight(zone);
            for (int l = 0; l < _maps.Length; l++)
            foreach (FeatureMapPanel panel in _maps[l])
                panel.Frame(l+1 == zone ? _theme.Cyan : (Color?)null);
        }

        private void SetProgress(float progress)
        {
            Progress = progress;
            ProgressChanged?.Invoke(progress);
        }

        private void ShowStart()
        {
            _phase = Phase.Start;
            Highlight(-1);
            _subtitle.Show("",CorridorDialogue.StartBody,false,null);
            _subtitle.SetVisible(_roomVisible);
            _hints.Show(CorridorDialogue.StartHint,_input.Picture.Position+Vector3.up*1.1f);
        }

        private void InputSelected(int index)
        {
            if (_phase != Phase.Start && _phase != Phase.Tour) { return; }
            _tourIndex = index;
            _tourIntroThisVisit = !_tourIntroduced;
            _tourIntroduced = true;
            ShowTour(0);
        }

        private void ShowTour(int stage)
        {
            _phase = Phase.Tour;
            _tourStage = stage;
            _hints.Hide();
            Highlight(stage);
            _tour.Show(stage,_tourIndex);
            string prefix = stage == 0 && _tourIntroThisVisit ? CorridorDialogue.TourIntro : "";
            _subtitle.Show(CorridorDialogue.TourHeader,prefix+CorridorDialogue.Tour[stage],true,
                stage == 3 ? CorridorDialogue.Understood : CorridorDialogue.Next);
        }

        private void AwaitRound()
        {
            _phase = Phase.AwaitRound;
            Highlight(_mandatoryLayer);
            _subtitle.Show("",CorridorDialogue.WaitingBody(_mandatoryLayer,_digit),true,null);
            Vector3 location = _mandatoryLayer == 4 ? _output.Tin(_digit)+Vector3.up*.9f
                : _maps[_mandatoryLayer-1][0].Position+Vector3.up*1.75f;
            _hints.Show(CorridorDialogue.WaitingHint(_mandatoryLayer,_digit),location);
        }

        private void MapSelected(int layer, int map, int index)
        {
            if (_phase == Phase.Round || _phase == Phase.Recap || CanExit) { return; }
            if (map == 0)
            {
                if (_phase != Phase.AwaitRound || layer != _mandatoryLayer) { return; }
                BeginRound(layer,map,index,true);
                return;
            }
            _optionalReturn = _phase;
            BeginRound(layer,map,index,false);
        }

        private void TinSelected(int digit)
        {
            if (_phase == Phase.AwaitRound && _mandatoryLayer == 4 && digit == _digit) { BeginRound(4,0,0,true); }
        }

        private void BeginRound(int layer, int map, int index, bool mandatory)
        {
            _tour.Clear();
            _phase = Phase.Round;
            _mandatoryRound = mandatory;
            _waitingForFilter = true;
            _filterButtonPending = false;
            _links.Dimmed = true;
            Highlight(layer);
            _round.Start(layer,map,_digit,index,_values,_forward);
            RenderRound();
        }

        private void FilterChanged(bool expanded)
        {
            if (_phase != Phase.Round || _round.Stage > 1) { return; }
            _waitingForFilter = !expanded;
            _filterButtonPending = expanded;
            _round.Show(expanded ? 1 : 0,true);
            RenderRound();
        }

        private void RenderRound()
        {
            if (_waitingForFilter) { _hints.Show(CorridorDialogue.FilterHint,_round.FilterPosition+Vector3.up*.8f); }
            else { _hints.Hide(); }
            string next = _waitingForFilter || _filterButtonPending ? null :
                _round.Stage == _round.LastStage ? CorridorDialogue.Understood : CorridorDialogue.Next;
            _subtitle.Show(CorridorDialogue.RoundHeader(_round.Layer),(_filterButtonPending ? "" : _round.Body()),true,next);
        }

        private void ShowRoundStage(int stage)
        {
            _phase = Phase.Round;
            _waitingForFilter = stage == 0;
            _filterButtonPending = false;
            _round.Show(stage);
            RenderRound();
        }

        private void ShowRecap()
        {
            _phase = Phase.Recap;
            if (_round.IsOutput) { _output.Complete(); }
            _round.Recap(_hints,_mandatoryRound);
            _round.Close();
            _links.Dimmed = false;
            _hints.Hide();
            if (_mandatoryRound)
            {
                _savedRoundIndices[_round.Layer - 1] = _round.Index;
                _links.Celebrate(_round.Layer);
                SetProgress((_round.Layer+1)/6f);
            }
            int mapCount = _round.IsOutput ? 0 : _maps[_round.Layer-1].Length;
            _subtitle.Show("",CorridorDialogue.Recap(_round.Layer,_round.Map,mapCount,_mandatoryRound),true,CorridorDialogue.Next);
        }

        private void AwaitOff()
        {
            _phase = Phase.AwaitOff;
            Highlight(-1);
            foreach (FeatureMapPanel[] layer in _maps)
            foreach (FeatureMapPanel panel in layer)
                panel.ShowSwitch(true);
            _hints.Show(CorridorDialogue.SwitchHint,SwitchPosition(1,0));
            _subtitle.Show("",CorridorDialogue.SwitchBody,true,null);
        }

        private Vector3 SwitchPosition(int layer, int map) =>
            _root.InverseTransformPoint(_maps[layer-1][map].Switch.position)+new Vector3(0,-.34f,-.2f);

        private void Toggle(int layer, int map)
        {
            if (_phase != Phase.AwaitOff && _phase != Phase.OffResult && _phase != Phase.AwaitOn
                && _phase != Phase.OnResult && !CanExit) { return; }
            string key = CorridorMath.MapKey(layer,map);
            bool turningOn = _off.Remove(key);
            if (!turningOn) { _off.Add(key); }
            Recalculate();
            if (!turningOn && (_phase == Phase.AwaitOff || _phase == Phase.OnResult))
            {
                _switchLayer = layer;
                _switchMap = map;
                ShowOffResult();
            }
            else if (turningOn && layer == _switchLayer && map == _switchMap)
            {
                if (_phase == Phase.AwaitOn) { ShowOnResult(); }
                else if (_phase == Phase.OffResult) { AwaitOff(); }
            }
        }

        private void ShowOffResult()
        {
            _phase = Phase.OffResult;
            _hints.Hide();
            _subtitle.Show("",CorridorDialogue.FirstOff(_switchLayer,_switchMap),true,CorridorDialogue.Next);
        }

        private void AwaitOn()
        {
            _phase = Phase.AwaitOn;
            _hints.Show(CorridorDialogue.RestoreSwitchHint,SwitchPosition(_switchLayer,_switchMap));
            _subtitle.Show("",CorridorDialogue.RestoreSwitchBody,true,null);
        }

        private void ShowOnResult()
        {
            _phase = Phase.OnResult;
            _hints.Hide();
            _subtitle.Show("",CorridorDialogue.RestoredSwitch,true,CorridorDialogue.Next);
        }

        private void Finish()
        {
            _phase = Phase.Complete;
            _door.Open();
            _subtitle.Show("",CorridorDialogue.CompleteBody,false,null);
            _hints.Show(CorridorDialogue.DoorOpenHint,_root.InverseTransformPoint(_door.GuidePosition));
            _completion.Open(_camera);
            SetProgress(1);
            ExitAvailabilityChanged?.Invoke(true);
        }

        private void Next()
        {
            switch (_phase)
            {
                case Phase.Tour:
                    _tour.Finish();
                    if (_tourStage < 3) { ShowTour(_tourStage+1); }
                    else { _tour.Clear(); SetProgress(1/6f); AwaitRound(); }
                    break;
                case Phase.Round:
                    if (_waitingForFilter || _filterButtonPending) { return; }
                    if (_round.Stage == _round.LastStage) { ShowRecap(); }
                    else { ShowRoundStage(_round.IsOutput && _round.Stage == 3 ? 5 : _round.Stage+1); }
                    break;
                case Phase.Recap:
                    if (!_mandatoryRound) { RestoreOptional(); }
                    else if (_round.IsOutput) { AwaitOff(); }
                    else { _mandatoryLayer++; AwaitRound(); }
                    break;
                case Phase.OffResult: AwaitOn(); break;
                case Phase.OnResult: Finish(); break;
            }
        }

        private void Previous()
        {
            switch (_phase)
            {
                case Phase.Tour:
                    if (_tourStage > 0) { ShowTour(_tourStage-1); }
                    else { _tour.Clear(); ShowStart(); }
                    break;
                case Phase.AwaitRound:
                    if (_mandatoryLayer == 1) { ShowTour(3); }
                    else
                    {
                        _mandatoryLayer--;
                        BeginRound(_mandatoryLayer,0,_savedRoundIndices[_mandatoryLayer - 1],true);
                        ShowRecap();
                    }
                    break;
                case Phase.Round:
                    if (_round.Stage > 0)
                        ShowRoundStage(_round.IsOutput && _round.Stage == 5 ? 3 : _round.Stage-1);
                    else
                    {
                        _round.Close();
                        _links.Dimmed = false;
                        if (_mandatoryRound) { AwaitRound(); }
                        else { RestoreOptional(); }
                    }
                    break;
                case Phase.Recap:
                    BeginRound(_round.Layer,_round.Map,_round.Index,_mandatoryRound);
                    if (_round.IsOutput)
                    {
                        _output.StartMatch();
                        _output.FinishMatch();
                        _output.StartFill(true);
                    }
                    ShowRoundStage(_round.LastStage);
                    break;
                case Phase.AwaitOff:
                    foreach (FeatureMapPanel[] layer in _maps)
                    foreach (FeatureMapPanel panel in layer)
                        panel.ShowSwitch(false);
                    BeginRound(4,0,0,true);
                    _output.StartMatch();
                    _output.FinishMatch();
                    _output.StartFill(true);
                    _output.FinishFill();
                    _output.Fly(true);
                    ShowRecap();
                    break;
                case Phase.OffResult:
                    _off.Remove(CorridorMath.MapKey(_switchLayer,_switchMap));
                    Recalculate();
                    AwaitOff();
                    break;
                case Phase.AwaitOn: ShowOffResult(); break;
                case Phase.OnResult:
                    _off.Add(CorridorMath.MapKey(_switchLayer,_switchMap));
                    Recalculate();
                    AwaitOn();
                    break;
            }
        }

        private void RestoreOptional()
        {
            switch (_optionalReturn)
            {
                case Phase.Start: ShowStart(); break;
                case Phase.Tour: ShowTour(_tourStage); break;
                case Phase.AwaitOff: AwaitOff(); break;
                case Phase.OffResult: ShowOffResult(); break;
                case Phase.AwaitOn: AwaitOn(); break;
                case Phase.OnResult: ShowOnResult(); break;
                default: AwaitRound(); break;
            }
        }

        public IEnumerator EndSession() { yield break; }
        private void RestoreCamera()
        {
            if (_renderCamera == null) { return; }
            _renderCamera.renderPostProcessing = _previousPostProcessing;
            _renderCamera = null;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) { return; }
            _builder.ZonePressed -= ToggleZone;
            _round.FilterPressed -= FilterChanged;
            _visuals.DisableInput();
            _round.DisableInput();
            RestoreCamera();
            _roomVisible = false;
            _volume.enabled = false;
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying) { return; }
            _visuals.Dispose();
        }
    }
}
