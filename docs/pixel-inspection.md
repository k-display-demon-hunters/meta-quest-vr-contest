# 픽셀 관찰과 ROOM 1-2 체험

## 진행과 완료 경계

Core/Learning의 단계는 `Tutorial → ScreenLab → ConvolutionLab → FeatureMap → ModelDesign`이다. `ConvolutionLab` 하나 안에 아래 두 체험을 순서대로 둔다. ROOM 2의 `CNNFeatureMapLab`은 별도의 다음 학습 단계이며 이번 후속 데스크와 서로의 Manager·Controller를 참조하지 않는다.

1. **필터가 지나간 자리**: 0~3 값의 고정 5×5 RGB 입력, 3×3 필터 배치, 첫 R 아홉 칸 계산, G/B 자동 계산, 출력 위치 퀴즈, 아홉 위치 스캔, 결과 요약.
2. **특징 판독 데스크**: 고정 6×6 비교 표본에서 A/B 특징맵 전환, 선택적인 표본 90° 회전과 복원, 체험 마치기.

첫 체험의 요약 버튼은 `특징맵 비교하기`다. 후속 체험을 시작할 때 앞 체험의 UI, 필터 잡기, 출력칸 클릭, 이동 숫자를 정리한다. 같은 씬·같은 관람 방향에서 두 판을 가진 후속 데스크를 보여 준다. 첫 3×3 출력이 뒤의 4×4 맵으로 변하는 것이 아니며, 요약에서 **새 비교 표본**으로 넘어감을 안내한다.

`CNNConvolutionLabManager`는 `ILearningExperience`를 유지한다. 첫 체험의 `Completed`는 파트 완료일 뿐 출구 허용 이벤트가 아니다. 후속 데스크의 A/B 비교가 끝나도 출구는 열리지 않으며, 관람객이 **체험 마치기**를 눌렀을 때만 `Progress=1`, `CanExit=true`와 완료 알림을 보낸다. 회전은 완료 조건에서 제외한다.

진행률은 출력 기록 수에 따라 0~0.75, 첫 결과 확인 0.75, 후속 데스크 진입 0.8, 첫 B 비교 0.95, 최종 종료 1이다. 같은 실행 중 진행률은 감소시키지 않는다. 1-1의 선택 변경이나 재시작은 1-2의 입력·진행 상태에 영향을 주지 않으며, 세션의 누적 최대 기록은 기존 `LearningProgress` 정책으로 유지한다. 공장 문·태블릿 항목·동선의 기존 인터페이스를 그대로 사용한다.

## 독립적인 관찰과 합성곱 입력

`CNNScreenLab`은 사진에서 선택한 3×3 원본 RGB 0~255 관찰을 담당한다. `PixelImageFactory`는 기존 사진에서 128×72 축소·5×5 블러·32×18 샘플링 결과를 만든다. `PixelImageData.CapturePatch`가 만든 `PixelPatchData`는 관찰 내부에서만 사용하며 `Scripts/CNNScreenLab/PixelPatchData.cs`에 둔다. 외부로 선택 데이터를 공개하거나 전달하지 않는다.

`CNNConvolutionLab`은 `Scripts/CNNConvolutionLab/ConvolutionLesson/ConvolutionCalculation.cs`의 `CreateExample()`에 저장된 0~3 값의 고정 5×5 RGB 배열로 시작한다. 순차 입장, 태블릿 직접 이동, 단독 실행 모두 같은 예제를 사용한다. 1-1의 선택 여부나 완료 상태로 입력을 바꾸지 않는다. 계산 결과는 이 배열과 필터로 매번 계산한다.

`FactorySceneLoadController`는 인접 씬을 미리 로드한다. ScreenLab과 ConvolutionLab은 로드 후 각자의 `Start`에서 자체 초기화하며, 공장의 현재 구역이나 실제 입장 여부를 전달받지 않는다. ConvolutionLab은 고정 예제를 준비하고 필터 배치 입력을 기다린다. 공장 내 로드와 단독 실행은 같은 시작 경로를 사용한다. 로드된 상태에서는 구역 이동만으로 체험을 중단하거나 초기화하지 않는다.

`LearningSessionManager`는 구체 체험 Manager를 참조하지 않고 `ILearningExperience`로 단계·진행률·출구 허용 상태를 조회하고 변경 이벤트를 구독한다. 언로드 전에는 같은 인터페이스의 `EndSession`으로 정리를 요청한다. 공장의 기존 문 개방 조건과 학습 순서는 유지한다.

두 체험의 UI 버튼은 공용 `XRPlayerRig`에 구성한 EventSystem·XRUIInputModule을 사용한다. `ScreenLabUIController`와 `CNNConvolutionLabManager`는 입력 환경을 생성하거나 해제하지 않으며, 버튼의 기존 On Click 연결로 각 체험의 요청을 처리한다. 체험 종료·언로드 시 공용 입력 환경은 유지된다. ScreenLab 단독 실행은 씬에 배치된 공용 리그를 사용하고, 자체 리그가 없는 ConvolutionLab의 XR 조작은 공장에 로드된 공용 리그를 사용한다.

## 첫 체험의 작업대와 시각 디자인

기존 공장 테마와 세 구역의 역할을 유지하면서, 모서리를 다듬은 금속 몸체·겹친 얕은 테두리·어두운 표시 면·절제된 앰버 포인트로 구성한다. 공용 UI 원본이나 ROOM 2의 재질을 바꾸지 않고 `CNNConvolutionLab` 전용 프리팹·재질·메시를 사용한다. 조명 자체보다 판·문자·타일의 대비로 정보를 구분한다.

| 구역 | 구성 |
| --- | --- |
| 왼쪽 · 01 필터 거치 | 홈이 있는 손잡이와 3×3 금속 필터, 대기 받침 |
| 가운데 · 02 입력 × 가중치 | 현재 채널의 5×5 입력, 아래 고정된 한 칸 곱셈판 |
| 오른쪽 · 03 결과 · 합계 | 3×3 판, 그 아래 작은 합계 표시, 그 아래 고정 합산식 판 |
| 위 | 행동 안내와 R/G/B 상태 카드 |
| 아래 | 짧은 계산 메모·피드백, 단계에 맞는 버튼 |

**두 식 판의 배경·테두리·크기·위치는 작업대 단계 내내 유지**한다. 도입·채널 전환·필터 이동·퀴즈에서는 식의 글자만 비운다. 첫 R의 한 칸 식은 가운데, 모든 채널 합과 RGB 합은 오른쪽에 둔다. 결과 요약으로 전환할 때 작업대를 숨기는 것은 별도 단계다.

오른쪽 결과판 위에 어두운 바탕과 얇은 금속 테두리의 명판을 둔다. `같은 위치의 곱`과 `합성곱 출력 · 3×3`은 같은 명판에서 기존 한글 글꼴로 표시한다.

`PixelCalculationUI`의 필터 자동 배치·결과 확인·특징맵 비교와 `FeatureInspectionDesk`의 필터 A/B·표본 회전/되돌리기·체험 마치기는 공용 `WorldUIButton`의 중첩 인스턴스다. 각 버튼은 부모 Canvas의 레이 입력을 사용한다. 필터 자동 배치·결과 확인·특징맵 비교 인스턴스는 자체 Canvas·레이캐스터를 비활성 상태로 남겨 두지 않고 프리팹 override로 제외하여 부모 Canvas에서 직접 표시한다. 자동 배치 버튼은 배치 단계 진입 시 표시하고 배치 완료·취소 시 숨긴다. 결과 확인 버튼은 필터가 입력판으로 이동한 뒤 비는 작업대 왼쪽에 표시되며, 결과 확인 단계에 들어가면 활성화한 뒤 라벨을 갱신한다. 결과 요약판의 `특징맵 비교하기` 버튼은 요약 표시 시 활성화하고 `RequestAdvance`로 후속 체험 진입을 요청한다. 합성곱의 클릭 이벤트와 특징 판독의 클릭 리스너는 기존 모듈에 연결된다. 위치 퀴즈는 출력 격자에서 칸을 선택한다.

입력 숫자는 타일 중앙, 겹친 필터의 가중치는 오른쪽 위에 둔다. 숫자는 원본을 유지하고 사본만 이동한다. `·`은 미기록 상태이며 유효한 0은 `0`으로 표시한다. 상태 카드도 `대기`, `진행 중`, `완료 · 합 0`을 구분한다. R/G/B는 글자와 빨강/초록/파랑을 함께 사용하며, 합산식에서도 채널 숫자의 색을 유지하고 연산 기호·최종합은 밝은 중성색으로 표시한다.

RGB 입력은 작업대의 같은 위치에서 채널별로 전환하여 표시한다. 잡기 대상은 필터 손잡이이며, RGB 층·이미지 패치 잡기 컴포넌트나 별도의 3층 가중치·재구성 패치 오브젝트는 생성하지 않는다. 자동 배치 버튼은 필터 배치만 보조한다. `PixelCalculationUI.prefab`에는 숫자 키패드·숫자 답 제출 이벤트를 두지 않으며, 퀴즈 입력은 출력판의 칸 선택으로 받는다.

안내 UI의 버튼은 결과 확인·필터 자동 배치·특징맵 비교에 사용한다. 프리팹의 버튼 연결은 `RequestNext`, `RequestSkip`, `RequestAdvance`를 유지하며, 각각 `NextRequested`, `SkipRequested`, `AdvanceRequested` 이벤트를 보낸다. 배치 단계만 자동 배치 요청을 구독하고, 결과 요약 단계만 결과 확인·후속 진행 요청을 구독한다. 계산식과 합계는 작업대의 식 판·합계 표시와 RGB 진행 카드에서 갱신한다. `LessonUIController`는 제목·안내·피드백·진행 카드·결과 요약을 표시하고, `ConvolutionCalculation`은 표시 문자열 없이 입력과 계산값을 제공한다.

프리팹에는 기존 GUID와 기준점 연결을 유지한다. `_inputAnchor`, `_kernelHome`, `_kernelTarget`, `_resultAnchor`, `_sumAnchor`, `_equationAnchor`와 새 `_sumEquationAnchor`가 런타임 표시 위치를 정한다. 첫 목표는 **입력의 맨 왼쪽 위 3×3**이다. 필터 이동은 입력판 로컬 격자를 기준으로 하므로 공장의 회전과 무관하게 관람객 기준 오른쪽·아래 방향을 유지한다.

ROOM 1-2의 공통 콘텐츠 루트는 균일 배율 1.5를 사용한다. 기존 배율 2의 75%로 두 작업대, 표시 UI와 잡기 대상에 함께 적용한다. 공장에서는 기준점 `(-28.55, -0.375, -2.0)`에 배치한다. 발 받침이 바닥 Y=0 바로 위에 오도록 높이를 맞추고, 남쪽 연구 데스크와 거리를 두도록 기준점을 북쪽으로 0.75m 옮겼다. 계산 메모는 높이를 줄여 바닥 위, 작업대 명판 아래에 배치한다. 필터의 배치 허용 반경은 목표 로컬 좌표로 계산하여 축소 비율을 따른다.

### 계산 규칙

입력 5×5×3, 공간 필터 3×3, 전체 가중치 3×3×3, stride 1, 패딩 없음, 편향 0이다. 세 채널의 연습용 가중치는 모두 `1 0 0 / 0 1 0 / 0 0 1`이다. 각 위치에서 채널별 9개 곱을 모두 더하고 세 채널 합을 더해 출력 한 칸을 만든다. 짧은 합산식에서는 가중치 0 항만 생략한다.

필터 왼쪽 위 `(row,column)`은 출력판의 같은 `(row,column)`에 대응한다. 값이 필터 중앙에서 출발하는 연출은 입력 중앙 픽셀을 수정하지 않는다. 입력 좌표·필터 내부 좌표·출력 좌표는 구분한다.

### 진행과 애니메이션

1. **배치**: R 입력과 두 빈 식 판을 표시한다. 손잡이를 Grip으로 잡아 맨 왼쪽 위 점선에 놓는다. 자동 배치도 같은 목표·정렬 경로를 사용한다. 이후 다시 잡도록 요구하지 않는다.
2. **첫 R 아홉 칸**: 행 순서로 아홉 칸을 모두 개별 계산한다. 입력·가중치 강조 → 두 숫자 사본을 가운데로 이동 → 큰 곱셈식 → 결과 숫자만 오른쪽 대응 칸에 기록한다. 0도 같은 순서로 기록한다. 한 칸을 곱할 때마다 합계로 옮기는 이전 동작은 실행하지 않는다.
3. **첫 R 합계**: 아홉 곱의 사본을 오른쪽 결과판 자체의 가운데로 모은다. 중앙 곱도 포함한다. 별도 합계 숫자가 나타난 뒤 바로 아래 작은 합계 표시로 내려간다. 원래 곱 결과는 보존한다. 이 모임·낙하 연출은 처음 한 번만 실행한다.
4. **첫 G/B**: 필터를 고정하고 G→B로 자동 전환한다. 채널의 아홉 곱과 합을 짧게 보여준다. 합산식은 오른쪽 고정판에 바로 표시하고 모임 연출은 반복하지 않는다. 완료한 채널 합은 상단 카드에 남긴다.
5. **첫 출력**: 오른쪽 제목을 `같은 위치의 곱`에서 `합성곱 출력 · 3×3`으로 바꾸고 중간 곱을 초기화한다. RGB 색을 유지한 합산식 뒤에 출력값을 필터 중앙에서 오른쪽 1행 1열로 보낸다.
6. **위치 퀴즈**: 필터를 오른쪽 한 칸 옮기고 `이번 출력값은 어느 칸에 들어갈까요?`라고 묻는다. 기존 XR 레이로 출력칸을 직접 선택한다. 첫 출력은 유지하고 두 식 판의 글자는 비운다. 오답은 힌트와 재시도, 정답 1행 2열은 입력을 즉시 닫고 그 위치의 R→G→B 계산을 한 번 시작한다.
7. **계속 이동**: 나머지 일곱 위치 모두에서 `한 칸 이동 → 정렬 → R → G → B → RGB 합 → 출력 기록`을 보여준다. 출력판으로 전환된 뒤 기존 출력값을 중간 곱으로 덮어쓰지 않는다.
8. **요약**: 아홉 출력 뒤 `결과 확인`을 기다린다. 완성된 3×3 결과와 첫 위치 RGB 식을 표시하고 `특징맵 비교하기`로 이어진다. 고정 입력으로 계산한 값을 표시하며 출력값 자체를 하드코딩하지 않는다.

출력 순서는 `(1,1)→(1,2)→(1,3)→(2,3)→(2,2)→(2,1)→(3,1)→(3,2)→(3,3)`이다. 배치·퀴즈·결과 확인·후속 진입은 관람객을 기다리고, 영상의 시간 때문에 강제로 넘기지 않는다.

첫 위치의 R에서 피연산자·곱의 사본과 합계 숫자가 움직이는 구간은 기본 속도의 0.75배로 재생한다. 두 번째부터 아홉 번째 위치로 옮기는 필터 슬라이드는 1.5배속으로, 한 칸에 0.30초가 걸린다.

위치 퀴즈가 시작되면 상단 안내판에 앰버색 `위치 퀴즈` 제목과 오른쪽 3×3 출력판에서 한 칸을 고르라는 안내를 표시한다. 아래에는 필터 이동 힌트와 재시도 안내를 표시하고, 정답을 고르면 상단에 초록색 `정답`을 표시한다.

### 기본 예제의 대조값

```text
R 입력             G 입력             B 입력
0 1 3 0 2          1 2 1 3 0          2 1 0 1 3
1 2 3 1 0          2 2 0 3 1          0 3 2 2 1
3 2 1 2 3          1 0 3 2 2          1 2 1 0 3
0 2 0 2 1          3 1 2 1 0          0 3 1 3 2
1 0 1 3 0          2 3 0 2 1          2 0 3 1 0

첫 R 곱            최종 출력
0 0 0              15 13 18
0 2 0              10 18 12
0 0 1              15 13 12
```

기본 예제의 첫 RGB 식은 `R 3 + G 6 + B 6 = 15`, 퀴즈 다음은 `R 6 + G 4 + B 3 = 13`이다.

## 후속 특징 판독 데스크

하나의 상판에 **왼쪽 6×6 컬러 표본, 오른쪽 4×4 특징맵**을 놓는다. 두 격자는 비슷한 물리적 크기와 중심 높이로 맞추고, 표본과 결과를 한자리에서 비교한다. 앞 작업대와 같은 금속·표시 면·명판 계열을 사용한다. 프레임·받침·버튼은 고정하며, 표본 격자의 내용만 회전한다.

고정 데스크·판·52개 타일·버튼·캡션·필수 참조는 `FeatureInspectionDesk.prefab`에 저장한다. 활성 모듈 루트에 Manager와 Controller를 놓고 시각 루트는 진입 전 비활성 상태다. 런타임에는 데이터, 표시, 버튼 상태와 표본 회전만 갱신한다. XR Ray와 World Space UI 입력을 재사용한다.

필터 A·필터 B·표본 회전/되돌리기·체험 마치기 네 버튼은 중첩 `WorldUIButton` 인스턴스의 자체 Canvas·추적 레이캐스터를 프리팹 override로 제외하여 데스크의 부모 Canvas·레이캐스터를 사용한다. 비활성 Canvas가 버튼에 남지 않도록 구성하며, `FeatureInspectionController`의 네 Button 참조와 활성 기간의 클릭 리스너 연결은 유지한다. 회전 버튼은 필터 B에서 표시하고, 체험 마치기는 첫 B 비교 후 사용 가능해지는 기존 조건을 따른다.

| 상태 | 표본·결과 | 조작 |
| --- | --- | --- |
| 시작 A | 세로 이음새 표본, 왼쪽 넓은 적색 반응이 오른쪽으로 약해짐 | 필터 B 선택, 종료 비활성 |
| B | 표본 유지, 4열 중 **세 번째 열**에 밝은 세로 줄 | A/B 반복 전환, 선택 회전, 체험 마치기 |
| 회전 중 | 표본 격자만 시계 방향으로 회전 | 모든 조작 잠금, 회전 중 안내 |
| 회전 B | 적갈색 위 3행·청회색 아래 3행, 밝은 줄 소멸 | B 고정, 표본 되돌리기 또는 체험 마치기 |
| 복원 B | 원래 세로 표본과 B 줄 복원 | A/B 전환 재개, 비교 완료 기록 유지 |
| 완료 | 맵과 최종 특징맵 정의를 유지 | 조작 잠금, ROOM 1-2 완료·출구 허용 |

안내는 `A · 빨강 반응`, `B · 세로 경계`, `선택 실험 · 필터 B 고정`을 구분한다. 회전은 0.4초이며 전환 중 중복 입력을 막는다. 중간 각도에서는 표본만 잠시 축소하여 모서리를 프레임 안에 유지하고, 회전 완료 시 원래 크기로 복원한다. B 맵이 어두워져도 격자·이름·선택등을 유지하여 전원 꺼짐과 구분한다. A/B 전환은 전체 맵을 함께 갱신하고 첫 체험의 필터 스캔·RGB 합산·위치 퀴즈를 반복하지 않는다.

첫 B 선택은 로컬 비교 완료를 한 번 알리고 종료 버튼을 활성화한다. 선택 회전 없이 끝낼 수 있다. 후속 데스크 재진입은 세로 표본+A에서 시작하고, 이미 얻은 전체 학습 완료·누적 기록은 보존한다.

### 표본·필터·표시값

표본 왼쪽 세 열은 `RGB(223,109,81)`, 오른쪽 세 열은 `RGB(51,68,101)`이며 여섯 행에 반복한다. 원래 0~255 색을 0~1로 정규화해 3×3 창·stride 1·패딩 없음·편향 0으로 계산한다. 각 맵은 4×4다. 이 표본은 앞 체험의 고정 5×5 입력과 구분되는 고정 비교 표본이다.

- **A**: 각 공간 위치의 채널 가중치는 R `+1/9`, G/B 각각 `−1/18`. 부호 있는 점수를 보존한다. 표시 강도는 `Clamp01(Max(0,score)×2.9)`이며 적색으로 나타낸다. 각 행의 점수는 약 `[+0.502,+0.291,+0.080,−0.131]`이다.
- **B**: 밝기 `0.2126R+0.7152G+0.0722B`, 각 행의 열 가중치 `[-1,+1,0]`, 세 행의 평균. 부호 있는 점수를 보존하고 `Clamp01(Abs(score)×4.5)`로 중성 밝기를 표시한다. 세로 표본의 각 행은 세 번째 열만 약 `−0.253`, 나머지는 0이다. 일반적인 Sobel이나 `[-1,0,+1]`로 대체하지 않는다.
- **회전 B**: 표본만 시계 방향 90° 돌아가면 각 행의 좌우 밝기가 같아져 모든 결과가 0에 가깝다. 원래 두 색, 필터, 결과판 위치·방향을 유지한다. 되돌리면 원래 결과가 복원된다.

세로+A, 세로+B, 가로+B의 세 부호 있는 결과를 실제 수식으로 계산하여 보관한다. 색 강도 변환은 계산 결과와 분리한다. 서로 다른 필터의 표시 강도를 동일한 원점수 크기라고 해석하지 않는다.

## 구현 책임과 수명주기

`client/Assets/Scripts/CNNConvolutionLab/`의 35개 소스는 씬 Manager 1개, 첫 체험 모듈 30개, 특징 판독 모듈 4개로 나눈다. 모든 소스의 namespace는 `CNNConvolution`으로 유지한다.

```text
CNNConvolutionLab/
├── CNNConvolutionLabManager.cs
├── ConvolutionLesson/
│   ├── ConvolutionLessonManager.cs
│   ├── ConvolutionCalculation.cs
│   ├── LessonUIController.cs
│   ├── Workbench/
│   │   ├── InputGridController.cs
│   │   ├── WorkbenchKernelController.cs
│   │   ├── ResultGridController.cs
│   │   ├── EquationDisplayController.cs
│   │   ├── FlyingNumberController.cs
│   │   ├── SideNumberController.cs
│   │   ├── WorkbenchColors.cs
│   │   └── Construction/
│   │       ├── WorkbenchBuilder.cs
│   │       └── WorkbenchPrimitiveFactory.cs
│   ├── KernelPlacement/
│   │   ├── KernelInteractionController.cs
│   │   └── KernelPlacementDisplayController.cs
│   ├── Multiplication/
│   │   ├── MultiplicationController.cs
│   │   ├── MultiplicationAnimationController.cs
│   │   ├── MultiplicationContent.cs
│   │   └── MultiplicationDisplayController.cs
│   ├── ChannelSum/
│   │   ├── ChannelSumController.cs
│   │   ├── ChannelSumAnimationController.cs
│   │   ├── ChannelSumContent.cs
│   │   └── ChannelSumDisplayController.cs
│   ├── OutputRecording/
│   │   ├── OutputRecordingController.cs
│   │   ├── OutputRecordingAnimationController.cs
│   │   └── OutputRecordingContent.cs
│   ├── WindowMovement/
│   │   ├── WindowMovementController.cs
│   │   └── WindowMovementContent.cs
│   ├── Quiz/
│   │   ├── ConvolutionQuizController.cs
│   │   └── OutputSelectionController.cs
│   └── ResultSummary/
│       └── ResultSummaryController.cs
└── FeatureInspection/
    ├── FeatureInspectionManager.cs
    ├── FeatureInspectionCalculation.cs
    ├── FeatureInspectionContent.cs
    └── FeatureInspectionController.cs
```

`KernelPlacement`·`Multiplication`·`ChannelSum`·`OutputRecording`·`WindowMovement`·`Quiz`·`ResultSummary`는 첫 체험의 하위 기능이다. 각 기능은 다른 기능의 클래스나 상위 Manager를 참조하지 않는다. `ConvolutionLessonManager`가 일곱 진입점을 연결하며 첫 위치 계산·반복 계산을 private 코루틴으로 구성한다. 첫 R 상세 계산, G/B 요약 합산, 출력 기록, 이동과 퀴즈 순서를 이 Manager에서 읽을 수 있다.

두 최상위 모듈은 서로의 입력·계산 결과를 전달하거나 내부 클래스를 참조하지 않는다. 씬 Manager가 `ConvolutionLessonManager`와 `FeatureInspectionManager`를 통해 시작·종료를 요청하고 이벤트를 받아 진행 순서를 조율한다. 폴더 구분은 책임과 소유 관계를 나타내며, 별도 asmdef나 새로운 접근 제한 체계는 추가하지 않는다.

`KernelInteractionController.cs`와 해당 `.meta`를 `KernelPlacement/`로 함께 이동하고 GUID를 보존했다. 곱셈 Controller·AnimationController와 기능 폴더는 `.meta` GUID를 보존하여 이동했다. 이전 작업대 스크립트의 GUID와 직렬화 필드는 WorkbenchBuilder에 이어서 사용한다. 중간 CalculationLesson 폴더와 전용 Manager는 제거하고 순서 조율은 ConvolutionLessonManager에 통합했다. 나머지 기존 스크립트의 경로·GUID는 유지한다. 씬·프리팹의 직렬화 필드와 버튼 콜백 이름은 유지하며, 에디터 구성 도구와 프리팹·재질 등은 각 에셋 종류의 기존 위치에 둔다. 아래 표에서 두 모듈의 경로는 위 스크립트 루트를 기준으로 한다.

| 파일·모듈 | 책임 |
| --- | --- |
| `CNNScreenLabManager`, `PixelImageData` | 3×3 관찰용 스냅샷 생성 |
| `CNNScreenLab/PixelPatchData` | 1-1 내부 관찰용 9픽셀 데이터 |
| `LearningSessionManager` | 공용 `ILearningExperience`를 통한 상태 구독·종료와 누적 진행 기록 |
| `CNNConvolutionLabManager` | 두 파트의 시작·순서·종료와 하나의 전체 완료 계약 |
| `ConvolutionLesson/ConvolutionLessonManager` | 단계 진입점 연결, 현재 계산 위치 선택, 진행률·파트 완료·후속 진행 알림 |
| `ConvolutionLesson/ConvolutionCalculation` | 고정 입력과 읽기 전용 윈도 위치, 가중치·곱·채널 합·출력 계산 |
| `ConvolutionLesson/KernelPlacement/KernelInteractionController` | 배치 단계 진입점. Grip 상태·목표 판정·자동 배치·복귀 순서와 안내 |
| `ConvolutionLesson/Multiplication/MultiplicationController` | 아홉 칸 반복, 안내 선택, 연출 호출과 기록 완료 안내 |
| `ConvolutionLesson/Multiplication/MultiplicationAnimationController` | 한 칸의 강조·피연산자 이동·곱셈식 표시·결과 기록 |
| `ConvolutionLesson/Multiplication/MultiplicationContent` | 칸별 제목, 곱셈 설명, 가중치별 안내와 완료 문구 |
| `ConvolutionLesson/ChannelSum/ChannelSumController` | 상세 합산과 첫 G/B·이후 RGB 요약 진행, 완료 카드 갱신 |
| `ConvolutionLesson/ChannelSum/ChannelSumAnimationController` | 아홉 사본 모으기와 합계 이동 |
| `ConvolutionLesson/ChannelSum/ChannelSumContent` | 상세·요약 합산 문구와 합산식 구성 |
| `ConvolutionLesson/OutputRecording/OutputRecordingController` | 첫 출력판 전환, RGB 합 표시, 출력 기록 진행 |
| `ConvolutionLesson/OutputRecording/OutputRecordingAnimationController` | 출력값 이동·기록 확정·강조 해제 |
| `ConvolutionLesson/OutputRecording/OutputRecordingContent` | 출력 안내와 RGB 합산식 구성 |
| `ConvolutionLesson/WindowMovement/WindowMovementController` | 이전 표시 정리, 이동 안내, 필터 이동과 도착 처리 |
| `ConvolutionLesson/WindowMovement/WindowMovementContent` | 이동 안내 문구 |
| `ConvolutionLesson/Quiz/ConvolutionQuizController` | 퀴즈 진입점. 전달받은 정답 인덱스로 판정·재시도·입력 종료 처리 |
| `ConvolutionLesson/ResultSummary/ResultSummaryController` | 요약 진입점. 결과 확인 대기·작업대 숨김·요약 표시·후속 진행 요청 |
| `ConvolutionLesson/Workbench/InputGridController` | RGB 입력 셀 생성·값·채널 표시, 입력 셀과 필터 목표 위치 |
| `ConvolutionLesson/Workbench/WorkbenchKernelController` | 필터 본체·가중치, XR 잡기·놓기, 위치와 잡기 해제 |
| `ConvolutionLesson/Workbench/ResultGridController` | 공유 3×3 셀의 값·표시·강조와 곱셈 결과판·출력판 전환 |
| `ConvolutionLesson/Workbench/EquationDisplayController` | 곱셈식·합산식·합계판 표시와 위치 조회 |
| `ConvolutionLesson/Workbench/FlyingNumberController` | 이동 숫자의 값·색·크기·위치·가시성 |
| `ConvolutionLesson/Workbench/SideNumberController` | 입력·결과 셀의 측면 숫자 동기화와 가시성 |
| `ConvolutionLesson/Workbench/WorkbenchColors` | RGB 채널색과 계산 강조색 |
| `ConvolutionLesson/Workbench/Construction/WorkbenchBuilder` | 프리팹 기준점으로 공유 오브젝트 최초 생성, 측면 숫자 갱신, 런타임 자원 해제 |
| `ConvolutionLesson/Workbench/Construction/WorkbenchPrimitiveFactory` | 타일·라벨·프레임 생성, 공용 좌표 변환과 런타임 재질 |
| `ConvolutionLesson/KernelPlacement/KernelPlacementDisplayController` | 목표 윤곽·체크·잡기 화살표·힌트 표시 |
| `ConvolutionLesson/Multiplication/MultiplicationDisplayController` | 가중치 사본, 입력·가중치 강조, 연결선, 곱셈 표시 조합 |
| `ConvolutionLesson/ChannelSum/ChannelSumDisplayController` | 아홉 합산 사본과 원래 결과 숫자의 표시·복원 |
| `ConvolutionLesson/Quiz/OutputSelectionController` | 출력 셀 선택 영역·버튼·레이 입력과 셀 인덱스 전달 |
| `ConvolutionLesson/LessonUIController` | 공유 안내 UI 표시, RGB 카드, 메모, 고정 버튼과 결과 요약 표시·클릭 이벤트 |
| `FeatureInspection/FeatureInspectionManager` | A/B·회전·복원·로컬 완료 상태와 내부 동작 조율 |
| `FeatureInspection/FeatureInspectionCalculation` | 고정 비교 표본과 부호 있는 필터 점수 계산 |
| `FeatureInspection/FeatureInspectionContent` | 상태별 안내·라벨·버튼 데이터 |
| `FeatureInspection/FeatureInspectionController` | 고정 프리팹의 표시·입력·표본 회전 |
| `ConvolutionLabBuilder` | 저장된 세 전용 프리팹과 같은 참조를 사용하는 씬 구성 경로 |

씬 Manager는 두 모듈의 진입점·이벤트만 사용한다. 후속 모듈은 ROOM 2 씬 전용 코드에 의존하지 않는다. 공용 UI 원본·ROOM 2 체험 순서·공장 전체 디자인은 이 변경에 포함되지 않는다.

현재 위치는 `ConvolutionLessonManager`가 선택한다. `ConvolutionCalculation.ForWindow`는 같은 고정 입력을 공유하는 새 읽기 전용 계산 객체를 반환한다. 기존 객체의 위치를 바꾸지 않으며 입력 배열도 외부로 공개하지 않는다. 필터 이동 연출이 끝난 뒤 Manager가 새 계산 객체를 선택해 계산 기능에 전달한다. 공유 작업대는 현재 계산 객체를 보관하지 않으며 초기 격자 설정 외에는 전달받은 값만 표시한다. 계산 기능은 해당 객체를 읽고, 퀴즈는 정답 인덱스만 받으며, 요약은 새로 계산한 출력 배열과 첫 RGB 합 배열만 받는다. 단계마다 계산 객체를 수정하거나 다음 단계를 호출하지 않는다.

공유 작업대는 `ConvolutionLesson/Workbench/`에 둔다. `ConvolutionLessonManager`가 첫 실행에서 WorkbenchBuilder로 공유 컴포넌트를 만들고 각 기능에 필요한 컴포넌트만 연결한다. 의존 방향은 `ConvolutionLessonManager → 기능 Controller → 기능 내부 표시·입력 → 공유 작업대 컴포넌트`다. 공유 작업대는 기능의 진입점이나 진행 순서를 참조하지 않는다.

결과판은 곱셈 결과와 합성곱 출력을 같은 3×3 셀에 표시한다. 퀴즈의 OutputSelectionController는 초기 생성 시 기존 셀 Canvas에 버튼과 XR Raycaster를 연결해 선택 위치를 유지한다. 퀴즈 내부에서만 선택 입력을 켜고 끄며, 필터 이동은 퀴즈를 조작하지 않는다. 곱셈과 합산의 전용 사본은 각각의 표시 Controller가 생성·관리한다. 이동 숫자 하나는 곱셈·합산·출력 기록에서 공유한다.

`ConvolutionLessonManager`는 `Awake`에서 요약 기능을 만들고 첫 체험 시작 때 나머지 기능을 연결한다. `OnEnable`/`OnDisable`에서 요약의 후속 진행 이벤트를 연결·해제한다. 배치·퀴즈·요약의 입력 구독은 각 단계의 실행 구간에만 유지한다. 배치와 퀴즈는 코루틴 반환으로 완료를 알리고, 요약의 결과 확인 이벤트를 받은 Manager가 작업대를 정리하고 숨긴다. 요약은 표시를 갱신한 뒤 반환하여 파트 완료를 알리고 후속 버튼의 `ContinueRequested`를 기다린다. `RequestNext`는 클릭만 알리며 결과 확인 대기 상태는 요약 단계가 소유한다.

종료·컴포넌트 비활성화에서는 Manager가 먼저 소유 코루틴을 중단하고 단계의 `Cancel`로 입력 구독을 해제한 뒤 공유 작업대의 숫자 사본, 퀴즈 입력, 필터 잡기와 강조를 정리한다. 자동 배치·비활성화·폐기에 의한 잡기 취소는 작업대가 사용자 손 놓기 이벤트로 전달하지 않으므로 별도 복귀 동작을 시작하지 않는다. 첫 합계 중간에 중단해도 중앙 결과 글자의 숨김 상태를 복원한다. 재시작은 새 계산 객체와 출력판·카드 상태로 시작한다. 작업대 내부 XR 리스너는 생성부터 폐기까지 유지하고 폐기 시 해제한다.

## 확인 범위

2026-11-09 단계 분리: 코드 의존 관계, 기존 학습 순서·수식·대기 시간·진행률·버튼 연결, 이동 파일의 GUID와 문서 경로를 정적으로 대조했다. 이번 변경에서 씬·프리팹을 재생성하거나 직렬화 참조를 변경하지 않았다.

2026-11-09 후속 버튼 표시 수정: `PixelCalculationUI.prefab`의 필터 자동 배치 인스턴스에서 비활성 Canvas·추적 레이캐스터를 제외했다. 부모 Canvas·레이캐스터, 버튼의 위치·크기·문구·`RequestSkip` 연결과 단계별 표시 코드는 유지했다. 프리팹 차이와 참조를 정적으로 확인했으며 실행 중 표시·클릭은 검증하지 않았다.

같은 결과 요약판의 `특징맵 비교하기` 인스턴스에도 동일한 수정을 적용했다. `PixelCalculationUI`의 세 버튼 모두 부모 Canvas에서 표시하도록 구성되어 있으며 기존 `RequestNext`·`RequestSkip`·`RequestAdvance` 연결을 유지한다. 요약 버튼의 위치·크기·문구와 요약 이후 체험 전환 로직은 변경하지 않았다.

`FeatureInspectionDesk.prefab`의 네 버튼에서도 같은 비활성 Canvas 설정을 확인하여 모두 제외했다. 부모 Canvas·추적 레이캐스터, 버튼 참조·위치·크기·라벨과 단계별 활성 조건을 정적으로 대조했다. 공용 버튼 원본과 런타임 코드는 변경하지 않았으며 Unity 실행·클릭 검증은 수행하지 않았다.

이번 변경은 코드, 수식, 호출 관계, YAML 참조·GUID, 프리팹 배치와 색상 값의 **정적 확인** 범위다. Unity 에디터 실행·프로젝트 열기·컴파일·Play Mode·자동 테스트·빌드·Quest 착용 검증은 수행하지 않았다. 이전 버전의 실행 결과를 이번 버전의 검증 결과로 사용하지 않는다.

실제 Quest에서 확인할 항목은 기본 관람 위치의 글자·약한 반응 셀 시인성, Grip과 XR 레이 선택, 그림자와 가림, 전체 진행, 실기기 프레임 시간이다. A·B·회전 B를 각각 확인해야 한다. 저장된 치수와 참조를 확인한 결과와 실제 렌더·실행 결과는 구분한다.

### 합성곱 수업의 기능별 변경 위치

의존 방향은 `ConvolutionLessonManager → 기능 Controller → 해당 Content·AnimationController`다. Controller는 공유 UI·작업대에 표시를 요청하며 AnimationController는 기능 내부 표시 담당 또는 필요한 공유 작업대 컴포넌트를 참조한다. Content는 Unity 컴포넌트를 참조하지 않는다. 기능 간 직접 참조와 상위 Manager 역참조는 없다. 단순한 필터 이동은 별도 AnimationController 없이 WindowMovementController에서 처리한다.

문구·수식 변경은 해당 Content, 이동 경로·시간 변경은 해당 AnimationController, 기능 내부 진행은 해당 Controller, 기능 사이 순서는 ConvolutionLessonManager에서 수정한다. 현재 위치·전체 진행률·퀴즈 시점은 ConvolutionLessonManager가 소유한다. 기존 코루틴 소유자와 중단 후 공유 작업대 정리 순서는 유지한다.

2026-11-09 계산 기능 분리: 기존 첫 R 상세 계산, 첫 G/B 곱 표시, 이후 RGB 요약, 출력 순서와 대기 시간, GUID·호출 관계를 코드로 대조했다. Unity 실행·컴파일·빌드·테스트는 수행하지 않았다.

중간 계층 제거: 네 계산 기능 폴더를 ConvolutionLesson 바로 아래로 이동하고 전체 실행 순서를 ConvolutionLessonManager에 모았다. 기능 내부의 연출·문구·계산값·대기 시간과 중단 정리 방식은 유지한다.

작업대 분리 후 코드·프리팹·씬의 스크립트 참조와 기능별 의존 관계를 정적으로 대조했다. 런타임 생성 방식, 기준점, 기존 버튼 콜백과 수업 순서를 유지한다.
