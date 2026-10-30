# 공용 양피지 열람

공장 pickup과 태블릿 양피지 목록은 `client/Assets/Prefabs/Common/ParchmentReader.prefab` 하나를 사용한다. 배경·제목·본문·버튼·삽화는 프리팹에 배치되어 있다.

FactoryEnvironment에 양피지 Pickup 5개를 각 방의 자식 씬 오브젝트로 배치했다. 문서 ID 0~4와 공용 열람 프리팹을 연결하며, 수집 전 문서는 태블릿 목록에 표시하지 않고 수집한 문서만 같은 열람창으로 다시 읽는다.

## 소유 관계

`ParchmentPage`, `ParchmentLesson`, `ParchmentCatalog`와 삽화 enum은 모두 `Core/Parchment/ParchmentLesson.cs` 한 파일에 정의한다.

- `Core/Parchment/ParchmentLesson.cs`: 한 페이지의 본문과 `ParchmentIllustrationType`. 삽화 종류를 페이지 데이터에 명시한다.
- `Core/Parchment/ParchmentLesson.cs`: 한 문서의 Id·제목·주제·부서·읽기 전용 페이지 목록. 번호에서 방 위치를 추론하지 않는다.
- `Core/Parchment/ParchmentLesson.cs`: 기존 다섯 문서의 실제 데이터와 읽기 전용 전체 목록, `Get(id)` 조회. 기존 pickup·태블릿과 연결되는 ID 0~4를 유지한다.
- `Core/Parchment/ParchmentRoom.cs`: 양피지 배치 방 enum. `None=0`, `PixelLab=1`, `FeatureMapLab=2`, `ModelDesignLab=3`이며 공장 동선 enum과 독립적이다.
- `Scripts/FactoryEnvironment/ParchmentPickupVisualController.cs`: 양피지의 외형 초기화·부유·발광·숨김·복원. 수집 판단이나 세션 조회는 하지 않는다.
- `Core/Parchment/ParchmentManager.cs`: 독립 루트의 MonoBehaviour. Pickup 등록·배치 방 조회·거리와 수집 여부에 따른 열람 판단을 담당한다. 수집 상태·현재 열람·코루틴·수명을 직접 관리한다. 첫 씬 로드 전 `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`에서 공용 세션을 생성한다. 태블릿 존재나 초기화에 의존하지 않는다. 리그 컴포넌트나 별도 관리 프리팹은 추가하지 않는다.
- `Core/Parchment/ParchmentReaderController.cs`: 공용 프리팹의 페이지·화면 버튼·확대 연출. FactoryEnvironment 폴더에서 이동했고 기존 GUID를 유지했다.
- `Core/Parchment/ParchmentIllustrationController.cs`: 현재 페이지에 지정된 삽화 패널 표시. 삽화는 항상 보이며 토글·포커스·입력 처리는 없다.
- `Core/Tablet/TabletParchmentController.cs`: 아이템함과 선택 항목. 공통 수집 상태 조회 함수를 사용한다.
- `Core/Tablet/TabletInputController.cs`: 기존 컨트롤러 입력으로 태블릿을 조작한다. 양피지 열람 중에는 TabletManager가 이 입력을 비활성화한다. 별도 양피지 입력 컴포넌트는 없다.

## 공장 pickup

ParchmentPickupController는 Awake에서 공용 Manager와 XRI 컴포넌트를 연결한다. OnEnable에서 문서 ID·ParchmentRoom·Transform·열람 프리팹과 표시 콜백을 RegisterPickup으로 등록하고 XRI 이벤트를 구독한다. 선택·활성화 입력은 RequestPickup으로 전달하며 OnDisable에서 구독과 등록을 해제한다. 자체 Update, 수집 상태, 거리 판정, 열람 코루틴, Renderer 조작은 없다.

Core의 ParchmentManager가 등록된 위치를 기준으로 3m 거리와 중복 열람·수집 여부를 확인하고 XR 카메라를 연결한다. XRI 콜백이 끝난 다음 프레임에 표시 콜백으로 원본을 숨기고 공용 열람창을 연다. 완료하면 수집 상태를 기록하고, 닫으면 수집 결과를 표시 콜백에 전달한다. 공장 전용 ParchmentPickupVisualController가 완료 시 원본을 다음 프레임에 비활성화하고 취소 시 외형과 상호작용을 복원한다. Core는 공장 Controller 타입을 참조하지 않는다.

Pickup의 `_placementRoom`은 Core/Parchment의 ParchmentRoom이며 PlacementRoom으로 조회한다. 공용 Manager는 위치 등록값과 LocationsChanged 이벤트를 유지한다. 태블릿 목록은 HasCollected로 수집 여부만 조회하고 보유한 문서의 제목·주제를 표시한다. TabletManager는 Collected 이벤트와 재활성화 시 목록을 갱신하며, 수집 전 위치 힌트 행은 표시하지 않는다. 문서 데이터에는 위치 문자열을 저장하지 않는다.

기존 ParchmentSceneSetup 도구도 공용 enum과 열람 프리팹을 사용하며 새 표시 Controller의 Renderer·Light 참조와 Pickup의 `_visuals`를 연결한다. 이 도구의 배치 좌표는 여전히 이전 공장 기준이므로 현재 공간의 배치 도구가 아니다.

FactoryEnvironment 씬에는 기존 Pickup의 외형·상호작용 설정을 재사용하고 현재 방에 맞게 위치·회전·배치 방을 지정했다. FactoryLearningFlowManager는 양피지 수집을 진행 조건으로 사용하지 않으며, 현재 튜토리얼·체험 완료와 구역 진입 조건으로 학습 씬과 셔터를 조율한다. ROOM #1의 스크린·합성곱 공간 전환과 후문 엔딩 흐름은 [공장 공간과 체험 연결](factory-environment.md)을 따른다.

## 씬 배치

각 항목은 Hierarchy의 해당 방 아래 `Parchment 1`~`Parchment 5` 이름으로 직접 이동할 수 있다. 위치는 월드 X, Y, Z이며 Y 회전은 도 단위다. 기존 방·가구·문·체험 기준점은 유지한다.

| 문서 | 방 | 위치 X, Y, Z | Y 회전 |
| --- | --- | --- | --- |
| 01 이미지와 픽셀 | ROOM #1 동쪽 | -15, 1.45, 1 | 0 |
| 02 합성곱과 패딩 | ROOM #1 서쪽 | -26, 1.45, -6 | -90 |
| 03 필터와 특징 추출 | ROOM #2 서쪽 | -8, 1.45, 12 | -90 |
| 04 특징 맵 | ROOM #2 남동쪽 | 4, 1.45, 8 | 180 |
| 05 활성화 함수와 ReLU | ROOM #3 남서쪽 | 17, 1.45, 8 | 180 |

FactoryMapController의 기존 하위 Pickup 탐색으로 지도에 게시한다. 태블릿 미니맵은 게시된 위치를 사용하고 수집 목록은 보유 문서만 표시한다. 씬 직렬화 참조와 배치 좌표를 정적으로 확인했으며 Unity 실행·컴파일·Play·테스트·빌드는 수행하지 않았다.

## 태블릿 재열람과 수명

공장에서 열람한 프리팹과 카메라를 세션이 기억한다. 수집한 아이템을 선택하면 그 항목 위치에서 같은 열람 프리팹을 생성한다. 두 경우 모두 첫 페이지부터 0.3초 동안 플레이어 앞 1.55m 위치로 확대된다. 완료와 닫기는 재열람창만 닫고 수집 상태를 변경하지 않는다.

ParchmentManager의 Opened·Closed·Collected 이벤트는 TabletManager 내부에서 연결·해제한다. 열람 시작·종료 시 ParchmentManager가 Core/XR의 XRPlayerInputController.SetParchmentMovementBlocked를 호출한다. XR 코드는 ContinuousMoveProvider.moveSpeed를 0으로 설정하고 닫을 때 저장한 속도로 복원한다. 열람 중에는 Trigger 여부와 무관하게 왼쪽 스틱 이동이 멈춘다. 양피지 UI 조작을 위해 이동 입력 참조를 교체하지 않는다. 열람 중 태블릿 입력은 비활성화하며, 닫으면 기존 태블릿 조작 상태와 포커스를 복원하고 다음 프레임부터 입력을 받는다. 양피지는 기존 오른손 XRI UI ray와 Trigger로 화면 버튼을 눌러 조작한다. 왼손 스틱·X·Y를 통한 열람 조작은 없다. 공통 양피지 코드는 XR 입력 Controller의 차단 메서드를 호출하며 이동 Provider의 속도 변경은 Core/XR이 담당한다.

화면 버튼에서 발생한 Reader의 Completed·CloseRequested 이벤트를 ParchmentManager가 처리한다. Collect와 Close는 내부 메서드이며 외부 탐색·선택 메서드는 없다. 최초 수집 열람에서 완료 버튼을 눌렀을 때 Manager 내부에서 Collect를 호출해 수집 상태를 기록하고 Collected 이벤트로 알린다. 태블릿은 수집 상태를 조회하고 이벤트를 받아 표시를 갱신한다. UnregisterPickup은 해당 원본에서 진행 중인 수집 열람을 닫고 위치 등록을 해제한다. 수집 상태는 세션에 유지한다.

수집 상태는 공용 ParchmentManager에 보관되어 씬 이동과 태블릿 비활성화 후에도 유지되며 앱 재시작 시 초기화된다. Core/Parchment가 최초 세션을 생성하며, Pickup·TabletManager는 생성된 공용 세션을 찾아 사용한다. 태블릿에는 세션 생성이나 외부용 수집 조회 중계 메서드가 없다. 아이템함과 미니맵은 ParchmentManager.HasCollected 조회 함수를 직접 전달받아 표시한다. Manager는 태블릿을 참조하거나 태블릿의 코루틴을 사용하지 않는다. 세션 루트에 DontDestroyOnLoad를 적용하고 열람창은 그 자식으로 생성한다. 리그나 태블릿의 자식이 아니므로 화면은 열 때 지정한 월드 위치에 유지된다. 닫기 또는 공용 세션 비활성화 시 열람창을 정리한다. 태블릿은 자신의 이벤트 구독과 입력만 관리하고 공용 세션을 종료하지 않는다. 전역 Instance 프로퍼티·관리용 프리팹은 없다. XRPlayerRig 프리팹과 Intro/Ending Manager에는 변경이 없다. 공장 진행 Manager에는 양피지 참조나 수집 조건이 없다. 이동 차단 메서드는 Core/XR/XRPlayerInputController.cs에 있다.

엔딩의 `처음으로`는 시작 씬으로 전환하며 공용 수집 세션을 초기화하지 않는다. 따라서 같은 앱 실행 안에서는 처음으로 돌아가도 수집 상태가 유지된다.

## 화면 에셋

기존 LearningParchment.png와 Pretendard 폰트를 프리팹에서 참조한다. 특징 맵은 기존 세 필터의 절댓값 응답을 정규화한 UI 셀 색으로 배치하고 세 맵과 고정 캡션을 항상 표시한다. 런타임 모델 계산 데이터에는 영향을 주지 않는다.

## 페이지와 삽화 데이터

본문·페이지별 삽화 지정은 ParchmentCatalog의 문서 데이터에서, 배치 방은 씬 Pickup의 PlacementRoom에서 편집한다. Reader는 현재 ParchmentPage.Body와 Illustration을 읽어 표시한다. IllustrationController는 명시된 종류를 프리팹의 해당 패널로 연결한다. 현재는 기존 화면을 유지하도록 한 문서의 세 페이지에 기존 삽화를 각각 지정했다. 페이지를 갱신할 때 해당 삽화 패널을 표시한다. 이전·다음(마지막 페이지는 완료)·닫기는 화면 버튼 클릭으로 처리하며 별도 포커스 순회는 없다. 문서와 페이지 목록은 외부에서 수정할 수 없는 형태로 제공한다.
