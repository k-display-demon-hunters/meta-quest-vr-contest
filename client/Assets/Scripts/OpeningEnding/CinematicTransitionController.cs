using System.Collections;
using UnityEngine;
using UI;
namespace Cinematic
{
    public sealed class CinematicTransitionController : MonoBehaviour
    {
        [SerializeField] private SceneTransitionController _loader;
        [SerializeField] private CinematicTransitionVisualController _visual;
        internal bool IsLoading { get; private set; }
        internal bool Succeeded => _loader.Succeeded;
        internal static CinematicTransitionController Prepare(CinematicTransitionController prefab, Camera camera, Transform rig, string destination)
        {
            CinematicTransitionController transition = Instantiate(prefab, rig);
            transition._visual.Bind(camera.transform);
            transition._loader.Prepare(destination);
            return transition;
        }
        internal static CinematicTransitionController Begin(CinematicTransitionController prefab, Camera camera, Transform rig, string destination)
        {
            CinematicTransitionController transition = Prepare(prefab, camera, rig, destination);
            transition.BeginTransition();
            return transition;
        }
        internal void BeginTransition()
        {
            if (IsLoading) { return; }
            IsLoading = true;
            StartCoroutine(Travel());
        }
        private IEnumerator Travel()
        {
            yield return _visual.Enter();
            yield return _loader.Activate();
            yield return _visual.Exit();
            IsLoading = false;
            if (Succeeded) { Destroy(gameObject); }
        }
    }
}
