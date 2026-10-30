# 공용 양피지 열람

공장 pickup과 태블릿 아이템함은 `client/Assets/Prefabs/Common/ParchmentReader.prefab` 하나를 사용한다. 배경·제목·본문·버튼·삽화는 프리팹에 배치되어 있다.

## 소유 관계

`ParchmentPage`, `ParchmentLesson`, `ParchmentCatalog`와 삽화 enum은 모두 `Core/Parchment/ParchmentLesson.cs` 한 파일에 정의한다.

- `Core/Parchment/ParchmentLesson.cs`: 한 페이지의 본문과 `ParchmentIllustrationType`. 삽화 종류를 페이지 데이터에 명시한다.
- `Core/Parchment/ParchmentLesson.cs`: 한 문서의 Id·제목·주제·부서·LocationHint·읽기 전용 페이지 목록. 번호에서 방 위치를 추론하지 않는다.
- `Core/Parchment/ParchmentLesson.cs`: 기존 다섯 문서의 실제 데이터와 읽기 전용 전체 목록, `Get(id)` 조회. 기존 pickup·태블릿과 연결되는 ID 0~4를 유지한다.
- `Core/Parchment/ParchmentManager.cs`: 독립 루트의 MonoBehaviour. 수집 상태·현재 열람·코루틴·수명을 직접 관리한다. 첫 씬 로드 전 `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`에서 공용 세션을 생성한다. 태블릿 존재나 초기화에 의존하지 않는다. 리그 컴포넌트나 별도 관리 프리팹은 추가하지 않는다.
- `Core/Parchment/ParchmentReaderController.cs`: 공용 프리팹의 페이지·화면 버튼·확대 연출. FactoryEnvironment 폴더에서 이동했고 기존 GUID를 유지했다.
- `Core/Parchment/ParchmentIllustrationController.cs`: 현재 페이지에 지정된 삽화 패널 표시. 삽화는 항상 보이며 토글·포커스·입력 처리는 없다.
- `Core/Tablet/TabletParchmentController.cs`: 아이템함과 선택 항목. 공통 수집 상태 조회 함수를 사용한다.
- `Core/Tablet/TabletInputController.cs`: 기존 컨트롤러 입력으로 태블릿을 조작한다. 양피지 열람 중에는 TabletManager가 이 입력을 비활성화한다. 별도 양피지 입력 컴포넌트는 없다.

## 공장 pickup

ParchmentPickupController는 Start에서 공용 ParchmentManager를 한 번 찾아 보유 여부를 확인한다. 3m 이내 입력을 받으면 이 Manager에 공용 프리팹·카메라·문서 인덱스·시작 위치와 종료 콜백을 직접 전달한다. Tablet 네임스페이스·참조·탐색·중계 호출은 없다. 열기에 성공한 경우에만 원본을 숨긴다. 마지막 페이지에서 완료하면 공통 수집 상태가 기록되고 원본이 비활성화된다. 중간에 닫으면 원본을 복원한다. Pickup이 비활성화될 때 진행 중인 해당 수집 열람을 취소한다.

Pickup의 `_placementRoom`은 기존 `FactoryDestinationId` enum이며 `PlacementRoom`으로 조회한다. 현재 씬에서 01은 `PixelLab`, 02~05는 `ModelDesignLab`으로 지정한다. 정확한 위치·회전은 Pickup 자신의 Transform을 사용하며 별도 좌표 필드는 두지 않는다. 배치 방은 씬 로딩 조건에 사용하지 않으며, ParchmentLesson.LocationHint는 안내 문구로만 사용한다.

FactoryEnvironment 씬의 기존 다섯 Pickup에 `_readerPrefab` 참조와 `_placementRoom` enum 값을 지정한다. 기존 ParchmentSceneSetup 도구도 열람 프리팹과 배치 대상 방 enum을 설정한다. FactoryLearningFlowManager는 양피지 수집 여부를 검사하지 않는다. 기존 체험 진행 순서와 구역 진입 조건만으로 씬을 로드한다.

## 태블릿 재열람과 수명

공장에서 열람한 프리팹과 카메라를 세션이 기억한다. 수집한 아이템을 선택하면 그 항목 위치에서 같은 열람 프리팹을 생성한다. 두 경우 모두 첫 페이지부터 0.3초 동안 플레이어 앞 1.55m 위치로 확대된다. 완료와 닫기는 재열람창만 닫고 수집 상태를 변경하지 않는다.

ParchmentManager의 Opened·Closed·Collected 이벤트는 TabletManager 내부에서 연결·해제한다. 열람 시작·종료 시 ParchmentManager가 Core/XR의 XRPlayerInputController.SetParchmentMovementBlocked를 호출한다. XR 코드는 ContinuousMoveProvider.moveSpeed를 0으로 설정하고 닫을 때 저장한 속도로 복원한다. 열람 중에는 Trigger 여부와 무관하게 왼쪽 스틱 이동이 멈춘다. 양피지 UI 조작을 위해 이동 입력 참조를 교체하지 않는다. 열람 중 태블릿 입력은 비활성화하며, 닫으면 기존 태블릿 조작 상태와 포커스를 복원하고 다음 프레임부터 입력을 받는다. 양피지는 기존 오른손 XRI UI ray와 Trigger로 화면 버튼을 눌러 조작한다. 왼손 스틱·X·Y를 통한 열람 조작은 없다. 공통 양피지 코드는 XR 입력 Controller의 차단 메서드를 호출하며 이동 Provider의 속도 변경은 Core/XR이 담당한다.

화면 버튼에서 발생한 Reader의 Completed·CloseRequested 이벤트를 ParchmentManager가 처리한다. Collect와 Close는 내부 메서드이며 외부 탐색·선택 메서드는 없다. 최초 수집 열람에서 완료 버튼을 눌렀을 때 Manager 내부에서 Collect를 호출해 수집 상태를 기록하고 Collected 이벤트로 알린다. 태블릿은 수집 상태를 조회하고 이벤트를 받아 표시를 갱신한다. CancelPickup은 Pickup 비활성화 시 해당 Pickup에서 시작한 수집 열람을 정리하는 공개 메서드로 유지한다.

수집 상태는 공용 ParchmentManager에 보관되어 씬 이동과 태블릿 비활성화 후에도 유지되며 앱 재시작 시 초기화된다. Core/Parchment가 최초 세션을 생성하며, Pickup·TabletManager는 생성된 공용 세션을 찾아 사용한다. 태블릿에는 세션 생성이나 외부용 수집 조회 중계 메서드가 없다. 아이템함과 미니맵은 ParchmentManager.HasCollected 조회 함수를 직접 전달받아 표시한다. Manager는 태블릿을 참조하거나 태블릿의 코루틴을 사용하지 않는다. 세션 루트에 DontDestroyOnLoad를 적용하고 열람창은 그 자식으로 생성한다. 리그나 태블릿의 자식이 아니므로 화면은 열 때 지정한 월드 위치에 유지된다. 닫기 또는 공용 세션 비활성화 시 열람창을 정리한다. 태블릿은 자신의 이벤트 구독과 입력만 관리하고 공용 세션을 종료하지 않는다. 전역 Instance 프로퍼티·관리용 프리팹은 없다. XRPlayerRig 프리팹과 Intro/Ending Manager에는 변경이 없다. 공장 진행 Manager에는 양피지 참조나 수집 조건이 없다. 이동 차단 메서드는 Core/XR/XRPlayerInputController.cs에 있다.

## 화면 에셋

기존 LearningParchment.png와 Pretendard 폰트를 프리팹에서 참조한다. 특징 맵은 기존 세 필터의 절댓값 응답을 정규화한 UI 셀 색으로 배치하고 세 맵과 고정 캡션을 항상 표시한다. 런타임 모델 계산 데이터에는 영향을 주지 않는다.

## 페이지와 삽화 데이터

본문·위치 안내·페이지별 삽화 지정은 ParchmentCatalog의 문서 데이터에서 편집한다. Reader는 현재 ParchmentPage.Body와 Illustration을 읽어 표시한다. IllustrationController는 명시된 종류를 프리팹의 해당 패널로 연결한다. 현재는 기존 화면을 유지하도록 한 문서의 세 페이지에 기존 삽화를 각각 지정했다. 페이지를 갱신할 때 해당 삽화 패널을 표시한다. 이전·다음(마지막 페이지는 완료)·닫기는 화면 버튼 클릭으로 처리하며 별도 포커스 순회는 없다. 문서와 페이지 목록은 외부에서 수정할 수 없는 형태로 제공한다.
