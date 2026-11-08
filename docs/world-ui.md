# 공용 공간 UI

## 현재 범위

ROOM 1-2의 합성곱 안내와 특징 판독 데스크는 `WorldUIButton` 원본의 중첩 인스턴스를 사용한다. 태블릿 등 화면 내부의 2D UI는 적용 대상이 아니다.

| 파일 | 역할 |
| --- | --- |
| `client/Assets/Scripts/Core/UI/WorldUIButtonController.cs` | ray 진입·이탈·누름 및 사용 가능 여부에 따른 버튼 표시 |
| `client/Assets/Scripts/Core/UI/WorldUIPopupController.cs` | 안내·확인 공용 팝업의 표시와 응답 이벤트 |
| `client/Assets/Prefabs/Common/WorldUIButton.prefab` | 기본 버튼 원본. Button의 onClick으로 기능을 연결한다. |
| `client/Assets/Prefabs/Common/WorldUIPopup.prefab` | 기본 활성 상태의 World Space Canvas. 두 버튼은 공용 버튼의 중첩 프리팹이며 보조 버튼은 기본 비활성이다. |
| `client/Assets/Prefabs/Common/WorldUIPrimaryButton.prefab` | WorldUIButton을 상속한 강조색 버튼 Variant. |
| `client/Assets/Prefabs/Common/WorldUIConfirmPopup.prefab` | WorldUIPopup을 상속하고 보조 버튼과 기본 확인 문구를 설정한 Variant. |

## 버튼 설정

Inspector에서 RectTransform의 위치·회전·크기, Label의 문구·폰트·글자 크기·정렬·여백, Image의 배경 Sprite, Outline의 두께를 수정한다. 배경과 글자의 상태별 색 및 전환 시간은 WorldUIButtonController에 설정한다. 기본 Image·Text 색은 흰색이며 Controller가 CanvasRenderer의 색을 전환한다. Image·Text 색을 별도로 바꾸면 상태 색과 곱해진다.

Button의 Transition은 None으로 유지한다. Controller가 시각 상태를 소유한다. 자식 Label은 raycastTarget을 끈 상태다. 클릭 기능은 표준 UnityEvent인 Button.onClick에 연결한다. Inspector의 On Click()에서 대상과 함수를 지정하거나 기존 Unity API인 AddListener/RemoveListener를 사용할 수 있다. 사용 가능 여부는 표준 Button.interactable로 설정한다. SetText로 문구를 갱신하고 Show()/Hide()로 표시·숨김을 제어한다. 비활성 후 재활성 시 호버·누름 상태를 초기화한다. 전역 검색이나 다른 버튼의 스타일 변경은 수행하지 않는다.

버튼 원본은 World Space Canvas와 TrackedDeviceGraphicRaycaster를 포함한다. 기본 크기는 240×72, 월드 배율은 0.001(0.24m×0.072m)이다. 기존 XR 입력이 구성된 씬의 일반 Transform 아래 또는 씬 루트에 배치하면 Play 시 별도의 Canvas 생성·컴포넌트 추가·호버 이벤트 연결 없이 기본 반응을 받는다. 공간 위치·방향은 사용자가 정한다. 기본·강조 버튼 모두 같은 구성을 상속한다.

팝업 내부의 중첩 버튼은 프리팹 override로 로컬 배율을 1로 설정하고 자체 Canvas·raycaster를 비활성화하여 팝업의 Canvas·raycaster를 사용한다. 피드백과 클릭 컴포넌트는 그대로 상속한다. 별도로 일반 UI Canvas 안에 버튼을 넣을 경우에도 동일한 배율·Canvas 설정이 필요하다. 권장 기본 사용은 공간에 독립 배치하는 방식이다.

EventSystem·XRUIInputModule·UI 입력이 활성화된 XR ray·MainCamera는 기존 씬의 입력 환경을 사용한다. 패키지 raycaster는 Canvas의 Event Camera가 비어 있으면 Camera.main을 사용하므로 기존 MainCamera를 수동으로 연결할 필요가 없다. 빈 씬에 XR 입력 환경까지 자동 생성하는 기능은 없다. Scene 뷰 편집 중의 마우스가 아닌 Play 상태의 XR ray 입력을 대상으로 한다.

## 팝업 설정과 호출

팝업은 기본 800×420, 월드 배율 0.001의 World Space Canvas다. Inspector에서 배경·제목·본문·버튼 배치를 수정할 수 있다. 글꼴 기본값은 Unity 내장 폰트다. 외부 폰트를 지정하는 경우 해당 사용을 ASSET_CREDITS.md에 기록한다.

제목·본문은 Title과 Body의 Text에서, 버튼 문구는 각 버튼 자식 Label의 Text에서 편집한다. 제목 표시 여부는 Title의 활성 상태, 보조 버튼 표시 여부는 Secondary Button의 활성 상태로 설정한다. 기본 팝업은 주 버튼 하나를 표시하고 확인형 Variant는 보조 버튼도 표시한다.

두 팝업 원본은 기본 활성 상태여서 배치 후 Play 시 바로 표시·조작할 수 있다. 나중에 열 팝업은 해당 인스턴스의 루트를 비활성화하고 Show()로 연다. Show()/Hide()는 루트 활성 상태만 바꾸므로 저장된 문구·자식 활성 상태·배치를 덮어쓰지 않는다. 이미 열린 팝업에 Show()를 호출해도 다시 초기화하지 않는다. 주 버튼은 오른쪽, 보조 버튼은 왼쪽이며 한 버튼일 때도 주 버튼의 위치를 유지한다. 본문 영역과 글자 크기는 Inspector에서 조절하며 자동 페이지 분할이나 스크롤은 제공하지 않는다.

WorldUIPopupController의 Actions에 있는 On Confirm과 On Cancel은 Inspector에서 연결하는 UnityEvent다. 주 버튼은 창을 닫은 뒤 On Confirm을, 보조 버튼은 창을 닫은 뒤 On Cancel을 실행한다. 내부 Button.onClick의 닫기 처리는 Controller가 활성 기간에 자동으로 연결·해제한다. 기능 연결은 팝업 루트의 On Confirm/On Cancel에만 지정하여 내부 버튼과 중복 등록하지 않는다. 기능이 없어도 닫기는 자동으로 수행한다.

Hide()는 응답 이벤트 없이 닫는다. 루트 활성 상태는 표준 gameObject.activeSelf로 조회할 수 있다. 열리는 프레임에는 응답을 받지 않아 동일 프레임의 클릭이 다음 설명까지 넘기지 않게 한다.

호버·누름·비활성 표시와 팝업 버튼을 눌렀을 때 닫히는 기본 동작은 별도 기능 연결 없이 작동하도록 구성되어 있다. 응답에 따른 학습 진행·설명 대기열·취소·실제 실행은 사용하는 모듈의 책임이다. 팝업은 씬 Manager나 기능 데이터를 참조하지 않는다.

배경 Image는 팝업 사각 영역 안에서 뒤쪽 UI로 ray가 통과하는 것을 막는다. 전체 화면의 다른 UI나 물체 조작·이동을 잠그는 기능은 없다. 향후 적용할 모듈이 필요한 입력 제한과 복원을 연결해야 한다. TrackedDeviceGraphicRaycaster는 연결되어 있으며, 실제 사용에는 기존 XR ray와 XRUIInputModule 입력 구성이 필요하다. 이 원본은 입력 시스템을 생성하거나 기존 설정을 변경하지 않는다.

## 에디터 편집과 Variant

Play 전에 프리팹 인스턴스를 원하는 위치에 배치하고 Inspector에서 설정한다. 원본의 공통 속성 변경은 상속되며, Variant 또는 인스턴스에서 override한 속성은 해당 값을 유지한다. 공통 변경은 기본 원본에서, 재사용할 변형은 Variant에서, 특정 배치만의 문구·위치는 인스턴스에서 편집한다. 기존 기본 원본의 GUID는 유지한다.

WorldUIPrimaryButton은 상태별 배경색만 override하며 버튼 구조·피드백 스크립트는 WorldUIButton에서 상속한다. WorldUIConfirmPopup은 제목·본문의 기본 문구와 보조 버튼 활성 상태만 override하며 팝업 구조·응답 처리는 WorldUIPopup에서 상속한다. 두 팝업의 내부 버튼은 동일한 WorldUIButton 원본을 계속 참조한다. 새 형태를 재사용하려면 Project 창에서 원본의 Create > Prefab Variant로 파생시키고 필요한 속성만 수정한다.

표시·숨김은 Inspector의 UnityEvent 대상에서 해당 Controller의 Show/Hide를 선택하거나 사용하는 모듈에서 호출한다. 표준 GameObject.SetActive도 사용할 수 있다. 버튼 기능은 Button의 On Click(), 팝업 기능은 루트 Controller의 On Confirm/On Cancel에서 모듈 내부의 처리 함수에 연결한다. 씬 Manager나 다른 모듈 내부로 직접 연결하지 않고 기존 모듈 경계를 따른다.

## 코드 API

각 공용 Controller가 직접 공개하는 메서드는 Show, Hide, SetText 세 개다.

| Controller | 함수 |
| --- | --- |
| WorldUIButtonController | Show(), Hide(), SetText(string text) |
| WorldUIPopupController | Show(), Hide(), SetText(string title, string body, string primaryLabel, string secondaryLabel) |

SetText는 Text.text만 변경하며 위치·색상·활성 상태를 변경하지 않는다. 팝업의 네 문구는 한 번에 지정한다. 보조 버튼이 숨겨져 있어도 보조 버튼 문구 인수를 전달하며, 문구를 바꾸는 것으로 버튼을 표시하거나 숨기지는 않는다.

```csharp
button.SetText("실행");
button.Show();
popup.SetText("다시 시작", "처음부터 다시 시작할까요?", "다시 시작", "취소");
popup.Show();
popup.Hide(); // 확인·취소 이벤트 없이 숨긴다.
```

버튼의 OnPointerEnter/Exit/Down/Up은 명시적 인터페이스 구현으로 정의한다. Controller의 일반 public 메서드로 노출하지 않으며 Unity EventSystem은 인터페이스를 통해 계속 호출한다. 인터페이스로 캐스팅한 호출까지 차단하는 구조는 아니다.

Unity 기본 API는 사용을 제한하지 않는다. GameObject.SetActive, Button.interactable, Button.onClick.AddListener/RemoveListener와 RectTransform 등의 표준 기능을 그대로 사용할 수 있다. 별도의 SetInteractable, IsOpen 같은 편의 API나 접근 차단 체계는 제공하지 않는다. 팝업에는 별도의 C# 응답 이벤트를 중복 제공하지 않고 private SerializeField UnityEvent를 사용한다.

## 확인 범위

새 파일과 직렬화 참조를 정적으로 검토했다. Unity 실행·임포트·컴파일·Play·테스트·빌드는 수행하지 않았다. 실제 표시·폰트·ray 반응과 프리팹 임포트 결과는 실행 확인 전이다.
