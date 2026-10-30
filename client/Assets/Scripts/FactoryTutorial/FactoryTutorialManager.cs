using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using XR;

namespace FactoryTutorial
{
    public sealed class FactoryTutorialManager : MonoBehaviour
    {
        private static readonly string[] Titles = { "당신의 미션", "태블릿이 필요한 이유", "진행률과 지도", "양피지를 수집하세요!", "이동과 둘러보기", "버튼 선택", "물체 잡기", "정면 복원", "태블릿 조작 시작", "태블릿 항목 이동", "X · 선택", "Y · 뒤로", "Menu · 설정", "첫 번째 목적지" };
        private static readonly string[] Instructions =
        {
            "당신은 이미지 복구 공장의 새 담당자입니다.\n양피지로 원리를 배우고 세 방의 체험을 완료하세요.\n픽셀·합성곱 → 피처맵 → 모델 제작 순서로 CNN을 이해합니다.",
            "태블릿에서 학습 진행률을 확인하고 앱을 선택해요.\n지도는 내 주변, 학습 단계는 네 체험으로 이동하는 앱이에요.\n양피지 앱에서는 모은 문서만 다시 읽을 수 있어요.",
            "픽셀 · 합성곱 · 특징 맵 · 모델 제작, 총 네 단계예요.\n한 단계를 완료할 때마다 진행률이 25% 올라가요.\n지도는 내 주변을 따라가며 위치와 방향을 보여줍니다.",
            "방으로 가는 길에서 양피지를 찾아보세요.\n3m 안에서 오른손 Trigger로 열고 마지막 페이지에서 완료하면 모을 수 있어요.\n놓쳐도 입장할 수 있어요. 지도에서 찾아 다시 돌아오세요.",
            "왼손 스틱을 움직여 걸어보세요.\n고개를 돌려 주변을 보고, 오른손 스틱으로 방향을 조절하세요.",
            "오른손 앞쪽 검지 버튼이 Trigger예요.\n레이로 화면 버튼을 가리키고 눌러 선택하세요.",
            "오른손 옆면 버튼이 Grip이에요.\nGrip을 누른 채 물체를 가리켜 잡고, 손을 움직여 옮기세요.\n놓으면 물체를 놓습니다.",
            "오른손 A를 누르면 현재 위치에서 정면을 복원해요.\nB는 레이를 켜거나 끕니다.\n안내 중에는 버튼 선택을 위해 레이가 유지됩니다.",
            "왼손 Trigger를 누르고 있어 보세요.\n태블릿을 조작하는 동안에는 이동 대신 항목을 고릅니다.",
            "왼손 Trigger를 누른 채 스틱을 움직이세요.\n노란 선택 표시가 다른 항목으로 이동하는지 확인하세요.",
            "왼손 X를 눌러 선택한 항목을 여세요.\n실제 태블릿과 왼쪽 연습 그림의 반응을 확인하세요.",
            "왼손 Y를 누르면 이전 화면으로 돌아갑니다.\n설정이나 조절 모드를 닫고 체험을 계속할 때 사용하세요.",
            "왼손 Menu로 설정을 열 수 있어요.\n밝기와 음량을 조절한 뒤 Y로 돌아오세요.",
            "첫 미션: Pixel Lab으로 이동하세요.\n가는 길에서 ‘이미지와 픽셀’, ‘합성곱과 패딩’ 양피지를 수집하세요.\n방 입구에서 역할 설명을 읽고 체험을 시작합니다."
        };
        private FactoryTutorialVisualController _visuals;
        [SerializeField] private FactoryTutorialPanelController _panel;
        [SerializeField] private Font _font;

        private XRPlayerInputController _playerInput;
        private bool _initialized;
        private bool _ownsRay;
        private bool _previousForcedRay;
        private int _page;
        private bool _finishing;

        public bool IsComplete { get; private set; }

        private void OnEnable()
        {
            if (_initialized && !IsComplete)
            {
                HoldGuideRay();
            }
        }

        public void Initialize(XROrigin rig, Transform entrance)
        {
            if (_initialized)
            {
                return;
            }
            _playerInput = rig.GetComponent<XRPlayerInputController>();

            _panel.transform.SetPositionAndRotation(
                entrance.position + entrance.forward * 3f + Vector3.up * 1.65f, entrance.rotation);
            _panel.Initialize(_font, rig.Camera);
            _panel.ConfigureVisualGuide();
            _visuals = _panel.gameObject.AddComponent<FactoryTutorialVisualController>();
            _visuals.Initialize(rig.GetComponent<PersistentXRPlayerRig>(), _font);
            _panel.OnPrevious += Previous;
            _panel.OnNext += Next;
            _panel.OnFinish += Finish;
            _initialized = true;
            HoldGuideRay();
            Refresh();
        }

        private void Previous()
        {
            if (_finishing || IsComplete)
            {
                return;
            }
            _page = Mathf.Max(0, _page - 1);
            Refresh();
        }

        private void Next()
        {
            if (_finishing || IsComplete)
            {
                return;
            }
            if (_page == Titles.Length - 1)
            {
                Finish();
                return;
            }
            _page++;
            Refresh();
        }

        private void Refresh()
        {
            _panel.ShowPage(_page, Titles.Length, Titles[_page], Instructions[_page]);
            _visuals.ShowPage(_page);
        }

        private void Finish()
        {
            if (_finishing || IsComplete)
            {
                return;
            }
            _finishing = true;
            StartCoroutine(FinishAfterClick());
        }

        private IEnumerator FinishAfterClick()
        {
            // Let the XR UI callback finish before the factory unloads this scene.
            yield return null;
            ReleaseGuideRay();
            _panel.gameObject.SetActive(false);
            IsComplete = true;
        }

        private void HoldGuideRay()
        {
            if (_ownsRay)
            {
                return;
            }
            _playerInput.EnsureRayReady();
            _previousForcedRay = _playerInput.TutorialRayForcedOn;
            _playerInput.TutorialRayForcedOn = true;
            _ownsRay = true;
        }

        private void ReleaseGuideRay()
        {
            if (!_ownsRay)
            {
                return;
            }
            if (_playerInput != null)
            {
                _playerInput.TutorialRayForcedOn = _previousForcedRay;
            }
            _ownsRay = false;
        }

        private void OnDisable()
        {
            ReleaseGuideRay();
            if (_finishing && !IsComplete)
            {
                StopAllCoroutines();
                _finishing = false;
            }
        }

        private void OnDestroy()
        {
            if (_panel != null)
            {
                _panel.OnPrevious -= Previous;
                _panel.OnNext -= Next;
                _panel.OnFinish -= Finish;
            }
        }
    }
}
