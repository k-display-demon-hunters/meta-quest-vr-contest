using FactoryEnvironment;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

// Attached only during editor Play mode, after the factory scene loads.
[DefaultExecutionOrder(10000)]
public sealed class FactorySimulatorFloorController : MonoBehaviour
{
    private const float StepHeight = 0.2f;
    private const float ContactTolerance = 0.06f;
    private FactoryPlayerController _player;
    private FactoryAreaController _areas;
    private BoxCollider _roomFloor;
    private XRBodyTransformer _body;
    private XR.XREditorSimulator _simulator;

    [InitializeOnLoadMethod]
    private static void Register()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Application.isPlaying) { Attach(scene); }
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) { return; }
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Attach(SceneManager.GetSceneAt(i));
        }
    }

    private static void Attach(Scene scene)
    {
        if (UnityEngine.XR.XRSettings.isDeviceActive) { return; }
        XR.XREditorSimulator simulator = FindFirstObjectByType<XR.XREditorSimulator>();
        if (simulator == null) { return; }
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            FactoryPlayerController player = root.GetComponentInChildren<FactoryPlayerController>();
            if (player == null || player.GetComponent<FactorySimulatorFloorController>() != null) { continue; }
            FactorySimulatorFloorController controller = player.gameObject.AddComponent<FactorySimulatorFloorController>();
            controller.hideFlags = HideFlags.DontSave;
            controller._player = player;
            controller._areas = player.GetComponent<FactoryAreaController>();
            controller._roomFloor = player.GetComponent<FactoryLayoutData>()
                .GetArea(FactoryAreaId.FeatureMapRoom).FloorVolumes[0];
            controller._body = player.Rig.GetComponent<XRBodyTransformer>();
            controller._simulator = simulator;
        }
    }

    private void LateUpdate()
    {
        if (_simulator == null || !_simulator.isActiveAndEnabled || UnityEngine.XR.XRSettings.isDeviceActive) { return; }
        FactoryAreaId area = _areas.CurrentArea.Id;
        if (area != FactoryAreaId.FeatureMapRoom && area != FactoryAreaId.FeatureMapRoomToModelDesignRoom) { return; }

        Vector3 feet = _body.bodyPositionEvaluator.GetBodyGroundWorldPosition(_player.Rig);
        float floorY = _roomFloor.bounds.max.y;
        float probeY = Mathf.Max(feet.y, floorY) + StepHeight;
        Vector3 origin = new Vector3(feet.x, probeY, feet.z);
        float range = probeY - floorY + ContactTolerance;
        float supportY = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, range,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            ? Mathf.Max(hit.point.y, floorY)
            : floorY;
        _player.Rig.Origin.transform.position += Vector3.up * (supportY - feet.y);
    }
}
