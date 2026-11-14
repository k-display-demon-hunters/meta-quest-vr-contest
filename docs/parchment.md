# 공용 양피지

## 배분과 위치

`client/Assets/Scripts/Core/Learning/LearningStageCatalog.cs`가 체험별 문서 배분의 원본이다. 각 단계는 문서 ID 배열 하나를 받는다. `EntranceParchmentIds`는 바깥문 입장 조건에, `ParchmentIds`는 태블릿 이동 시 지급과 문서 소속 조회에 사용하며 두 프로퍼티는 같은 읽기 전용 목록을 참조한다. `ParchmentCatalog`는 문서 ID와 제목·본문·페이지·삽화만 소유한다.

ScreenLab과 ConvolutionLab은 독립된 학습 단계다. 첫 공간은 픽셀 선택·RGB 전개 관찰, 두 번째 공간은 R/G 계산 학습·B 퀴즈를 담당한다. FactorySceneLoadController가 지역 진입 이벤트로 씬을 전환하고, FactoryEnvironmentManager가 준비 결과와 양피지 조건으로 문을 제어한다. Screen과 Convolution의 입장 문서는 모두 ID 0(01)이다. FeatureMap과 ModelDesign은 ID 1(02)를 요구한다. 내부 공유 문은 관찰 완료·콘텐츠 준비·문서 수집 후 열린다. 태블릿은 두 체험을 별도 항목으로 제공한다. 상세 책임과 수명은 [픽셀 관찰과 합성곱 체험](pixel-inspection.md)을 따른다.

양피지 2개는 씬의 `Factory Environment/Parchments` 아래에 문서 번호 순서로 저장한다. 부모의 위치·회전은 0, Scale은 1이다. 위치·회전은 씬 Transform이 소유하며 런타임 좌표 적용이나 여분 Pickup 삭제는 하지 않는다.

| ID / 표시 번호 | 내용 | 위치 | 월드 좌표 | Y 회전 |
| --- | --- | --- | --- | --- |
| 0 / 01 | 이미지와 픽셀 | ROOM 1 입구 앞 복도 | (-10.5, 1.45, 0) | -90° |
| 1 / 02 | 특징 맵 | ROOM 2 입구 앞 복도 | (-14.5, 1.45, 11.2) | 90° |

걸어서 이동할 때 튜토리얼 완료 후 01을 수집해 픽셀 관찰에 입장한다. 합성곱 입장에는 같은 01을 사용하고, 특징 맵 입장 전에 02를 수집한다. 모델 제작 입장에는 같은 02를 사용한다. 문서 수집 외에 각 체험 완료·콘텐츠 준비 등 기존 전환 조건도 충족해야 한다.

`HasCollectedAll(documentIds)`는 전달된 문서를 모두 보유했는지 확인한다. 빈 요구 목록은 충족된 것으로 처리한다. 튜토리얼과 후문에 양피지 조건을 추가하지 않는다.

## 수집과 열람

Core/Parchment의 Pickup은 씬에 저장된 위치·방향을 유지하며 활성/비활성 등록과 XRI 입력을 전달한다. Manager가 3m 거리와 중복 수집·열람을 확인하고 원본을 잠시 숨긴 뒤 공용 Reader를 연다. 마지막 페이지의 [학습 완료]를 눌렀을 때 수집을 기록하고 `Collected(documentId)`를 알린다. 완료 전에 닫으면 미수집 상태로 원본을 다시 표시한다. 이미 수집한 문서의 재열람은 보유 상태를 유지한다.

수집 상태는 기존 공용 `ParchmentManager`의 영속 메모리에 유지한다. 공장 씬 종료 시 폐기되는 학습 진행 기록과는 별개다. 디스크에 저장하지 않는다. `ParchmentInventoryItem`은 문서 ID로 학습 카탈로그의 배분을 역조회해 `Stage`와 `LocationText`를 제공하므로 Pickup이 사라져도 위치 안내를 유지한다. 입장 문서는 해당 체험 입구 앞, 문서 0은 “픽셀 관찰 체험 입구 앞”, 문서 1은 “특징 맵 체험 입구 앞”으로 표시한다. 같은 문서를 여러 단계가 요구하면 카탈로그 순서에서 첫 소속 단계로 안내한다.

태블릿 양피지 앱은 `HasCollected`로 수집 여부를 조회해 보유한 문서의 제목·주제만 목록에 표시하고, 현재 XR 카메라와 시작 위치를 전달해 재열람을 요청한다. Manager가 이동 차단과 열람창 수명을 관리한다. 현장에서 처음 여는 문서는 학습 완료 후 태블릿 재열람 목록에 추가된다. 태블릿 단계 이동으로 자동 지급된 문서는 현장 열람 없이도 재열람할 수 있다.

공장의 B 텔레포트는 `ParchmentManager.HasRead`로 열람 완료를 확인한다. 현장 열람과 태블릿 재열람 모두 마지막 페이지의 학습 완료로 기록된다. 태블릿 단계 이동에 따른 자동 지급은 보유 기록에만 반영된다.

## 두루마리 01 페이지 표시

공용 Reader는 문서의 본문을 하나의 TMP 컴포넌트에 표시한다. 페이지 제목이 있으면 해당 제목을, 없으면 문서 제목을 표시한다. 01의 숫자 그림과 라벨은 프리팹에 미리 배치한 아틀라스를 사용한다. 그림은 UI 레이 대상이 아니다.

픽셀화와 RGB 분리 효과는 배치된 그림의 투명도와 RGB 판의 위치를 바꾼다. 첫 페이지의 애니메이션은 두루마리가 다 펼쳐지면 시작하고, 페이지 이동으로 해당 그림을 다시 열면 처음부터 재생한다. 흑백 밝기 그림은 정적이다. 현장 안내 문구와 방향은 씬에서 설정한다.

## 학습 세션과 공장 연결

`FactoryEnvironmentManager`가 `Collected` 이벤트를 받아 공통 학습 카탈로그의 `EntranceParchmentIds`와 `HasCollectedAll`로 준비된 다음 체험의 입구 조건을 갱신한다. 수집 기록은 복제하지 않는다. 문 개방 조건은 공장에서 처리하며 로더는 `AreaChanged`만 구독한다. 문·기준점 참조는 `FactoryLayoutData`, 영역 정의는 `FactoryAreaController`에 둔다. `LearningSessionManager`는 태블릿 이동 요청에 따른 문서 지급을 담당한다.

## 태블릿 단계 이동 요청에 따른 지급

`LearningSessionManager`는 `TabletManager.StageMoveRequested(LearningStageId)`를 받으면 공통 카탈로그의 순서대로 선택한 체험까지의 `ParchmentIds`를 `ParchmentManager.CollectDocuments`에 전달한다. enum 숫자나 문서 번호로 진행 순서를 추론하지 않는다. 문서 소속 조회는 같은 전체 목록을 사용하는 `GetParchmentStage(documentId)`로 수행한다.

| 선택한 체험 | 현재 배분 기준 보유하게 되는 문서 |
| --- | --- |
| 픽셀 관찰 | 01 |
| 합성곱 | 01 |
| 특징 맵 | 01, 02 |
| 모델 제작 | 01, 02 |

지급은 이동 요청을 받았을 때 수행한다. 이동 거절·로딩 실패·이전 단계로 복귀해도 이미 수집한 문서를 회수하지 않는다. 처음 얻는 문서만 기존 Collect 경로로 기록하고 Collected 이벤트를 발생시켜 태블릿 목록과 입장 조건을 갱신한다. 등록된 Pickup은 기존 수집 표시 콜백으로 숨기며, 아직 등록되지 않은 Pickup은 나중에 등록될 때 보유 상태를 반영한다. 자동 지급은 열람창을 열지 않고 체험 완료 기록도 변경하지 않는다.

## 태블릿 재열람과 편집 도구

Pickup과 태블릿은 `client/Assets/Prefabs/Common/ParchmentReader.prefab`을 공유한다. TabletManager는 공용 ParchmentManager의 Opened·Closed·Collected 이벤트를 연결·해제한다. 열람 중 태블릿 입력을 중단하고 닫으면 이전 조작 상태와 포커스를 복원하며 다음 프레임부터 입력을 받는다. 수집 상태는 태블릿 비활성화나 씬 이동으로 초기화하지 않는다. 양피지 페이지는 오른손 UI 레이와 Trigger로 조작한다.

ParchmentSceneSetup의 Refresh Learning Parchment Labels 메뉴는 열린 씬의 기존 Pickup 라벨을 문서 데이터에 맞춘다. 이전 공간 좌표로 씬을 열고 전체 재생성하던 메뉴는 제거했다. 위치 변경은 씬 Transform에서 수행한다. Pickup/Visual 스크립트는 Core/Parchment에 있고 기존 GUID를 유지한다.

이번 ScreenLab·ConvolutionLab 독립 체험 분리와 문서 배분은 소스 기준으로 정리했다. Unity 실행·컴파일·테스트·빌드는 수행하지 않았다.

## 말린 양피지와 펼침 연출

현장 양피지는 기존 이미지의 양끝을 재사용한 말린 형태로 표시한다. Reader는 약 0.8초 동안 사용자 앞으로 이동하며 펼쳐지고, 펼침 후반에 본문이 나타난다. 펼침 완료 후 페이지 버튼과 UI 레이를 활성화한다. 이동·펼침 중 입력은 받지 않는다. 생성한 Sprite는 VisualController 종료 시 정리한다. 배치 Transform은 실행 시 문서 좌표로 덮어쓰지 않는다.


## 두 문서 배치

학습 카탈로그와 Pickup·Reader·태블릿은 동일한 ID 0~1을 사용한다. 별도 ID 변환은 없다. 수집·열람·자동 지급 처리는 기존 구현을 유지하며 요구 목록만 카탈로그에서 직접 지정한다. 씬에는 Pickup 2개와 갱신한 제목 라벨을 저장해 에디터 라벨 갱신 도구도 같은 카탈로그를 사용한다. 이번 변경은 소스·씬 직렬화 참조를 정적으로 확인했으며 Unity 실행·컴파일·테스트·빌드는 수행하지 않았다.
