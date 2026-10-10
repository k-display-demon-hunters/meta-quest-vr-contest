# CNN 초보자 체험 씬

CNN 체험은 같은 `client` 프로젝트 안의 `CNNFeatureMapExplorer` 씬으로 제공한다. 전용 스크립트는 `Assets/Scripts/CNNFeatureMaps/`의 `CNNFeatureMaps` 네임스페이스에 있으며, 콘텐츠 프리팹은 `Assets/Prefabs/CNNFeatureMaps/CNNFeatureMapWorkspace.prefab`이다. 이전 `FeatureMapLab` 씬과 전용 스크립트·프리팹은 제거했다. 입력 이미지와 재질은 `Assets/Textures/CNNFeatureMaps/`와 `Assets/Materials/CNNFeatureMaps/`에 유지하며, 기존 XRPlayerRig를 재사용한다.

## 열기

1. Unity Hub에서 `C:\Github_local\MetaQuest\meta-quest-vr-contest\client` 프로젝트를 사용한다.
2. 현재 Play 중이면 먼저 정지한다.
3. Project 창에서 `Assets/Scenes/CNNFeatureMapExplorer.unity`를 더블클릭한다.
4. 학습 안내와 필터 도구는 Workspace의 중첩 프리팹으로 배치되어 Play 전에도 편집할 수 있다. 사용자가 Play를 실행하면 안내 내용과 계산 결과가 갱신된다.

처음에는 입력과 첫 번째 맵만 보인다. 아래쪽의 **다음** 버튼으로 단계를 진행한다. 카메라·양손·이동은 기존 XRPlayerRig를 사용하며, 콘텐츠는 VR 컨트롤러로 조작한다.

## 체험 순서

1. **feature map 만들기**: 민트색 필터 손잡이를 입력 위에서 끌거나 입력 픽셀을 클릭한다. 3×3 입력 영역과 해당 결과 뉴런이 연결선으로 표시된다. 방문한 위치가 맵에 채워지고, 곱한 값의 합과 ReLU 결과를 표시한다. **자동 이동**은 64개 위치를 순서대로 보여준다. 이미지 밖은 0으로 채운다.
2. **다른 필터 비교**: 좌우 변화 감지와 위아래 변화 감지 필터의 맵을 펼쳐 비교한다. **필터 바꾸기**로 현재 입력 영역에 적용하는 필터를 바꾼다. 맵 테두리를 잡아 끌어오거나 회전·확대할 수 있다.
3. **한 픽셀의 영향 따라가기**: 입력 픽셀을 클릭하면 수용 영역에 그 픽셀을 포함하는 뉴런들이 각 층에서 노랗게 표시된다. **펼치기 1/2/3**으로 살펴볼 층을 차례로 펼친다. 직접 층 손잡이를 잡고 펼치는 기존 조작도 유지한다.
4. **입력 바꾸기**: 입력 픽셀을 클릭해 흑백을 바꾸면 전체 합성곱을 다시 계산한다. feature map의 계산값과 예시 반응 막대가 바뀐다. **처음 상태**는 원래 입력, 노드·맵 배치와 현재 단계의 표시를 복원한다.

## 콘텐츠 조작

| 동작 | VR 오른손 |
|---|---|
| 안내 버튼 | 레이로 가리키고 Grip |
| 입력 픽셀 선택·칠하기 | 레이로 가리키고 Trigger 또는 Grip |
| 필터·뉴런·맵·층 잡기 | Grip |
| 크기 | 잡은 뒤 A/B 유지 |
| 회전 | 잡은 손 회전 |
| 잡은 물체의 거리 | 잡은 뒤 오른쪽 스틱 |
| 특정 층 펼치기 | 층 손잡이를 잡고 Trigger |
| 배치 복원 | 오른쪽 스틱 클릭 |

필터는 학습용 도구이므로 입력 격자의 평면에 맞춰 움직이고, 임의 회전·확대는 적용하지 않는다. 다른 뉴런·맵·층은 자유롭게 조작할 수 있다. 잡은 물체를 놓은 뒤 체험 단계 버튼을 사용한다. 오른쪽 스틱 클릭은 배치 복원이고, **처음 상태** 버튼은 입력 수정 내용까지 복원한다.

## 계산과 표시의 의미

- 입력 8×8, 3개의 합성곱 층, 채널 수 2→3→5. 모두 3×3, stride 1, zero padding, ReLU이며 이 버전에는 pooling과 학습 과정이 없다.
- 첫 층의 좌우 필터는 각 행이 `[-1/3, 0, +1/3]`, 위아래 필터는 각 행이 `[-1/3, -1/3, -1/3]`, `[0, 0, 0]`, `[+1/3, +1/3, +1/3]`이다. 안내의 합과 실제 첫 층 계산은 같은 가중치를 사용한다.
- 뒤의 층은 이전 층 모든 채널을 합성곱하고 입력 채널 수로 정규화한다. 미리 지정한 교육용 필터이며 훈련된 이미지 분류 모델이 아니다.
- **노란색**은 구조적 수용 영역이다. 가중치가 0이거나 ReLU로 값이 0이 되는 연결도 포함하므로 실제 값 변화나 중요도의 증거가 아니다.
- 3개 층의 입력 기준 수용 영역은 최대 3×3, 5×5, 7×7이다. 경계에서는 유효한 이미지 영역만 표시한다. 입력 픽셀에서 영향을 받는 출력 위치는 경계에 따라 더 적을 수 있다.
- 입력의 노란 강조는 흑백 차이가 남도록 색을 섞는다. 계산되지 않은 것처럼 어둡게 표시한 뉴런은 첫 단계의 시각적 진행 표시이며, 내부 계산은 전체 결과를 미리 보유한다.
- 뉴런·맵을 옮기는 것은 배치 변경이다. CNN의 인덱스와 계산 연결은 바뀌지 않는다.
- A/B 막대는 마지막 맵으로 만든 예시 반응이며 고양이·강아지 등 학습된 클래스 확률이 아니다.

## 코드 위치

- `client/Assets/Scripts/CNNFeatureMaps/CNNFeatureMapGuide.cs`: 4단계 안내, 필터 도구, 진행 표시, 버튼 처리.
- `CNNFeatureMapExampleData.cs`: 실제 합성곱·ReLU·수용 영역·입력 수정 계산.
- `CNNFeatureMapNode.cs`: 계산값과 강조색.
- `CNNFeatureMapGrabTarget.cs`, `CNNFeatureMapLayer.cs`: VR 잡기, 이동·회전·확대, 펼치기.
- `client/Assets/Prefabs/CNNFeatureMaps/CNNFeatureMapWorkspace.prefab`: CNN 배치와 학습 컴포넌트 참조.

## 프리팹 구성

`Assets/Prefabs/CNNFeatureMaps/`의 Workspace는 전체 배치와 실행 문맥을 연결하는 조립용 프리팹이다. 각 부품은 아래 원본 프리팹에서 편집한다.

| 프리팹 | 역할 |
|---|---|
| `CNNFeatureMapWorkspace` | 입력·층·출력·안내 UI 조립, 씬 컨트롤러와 가이드 |
| `CNNFeatureMapInput` | 입력 픽셀 64개, 입력 손잡이, 캡션과 필터 도구 |
| `CNNFeatureMapInputPixel` | 개별 입력 픽셀의 모양·콜라이더·잡기 |
| `CNNFeatureMapNeuron` | 개별 은닉 뉴런의 모양·콜라이더·잡기 |
| `CNNFeatureMapPanel` | 공통 뉴런 64개와 맵 테두리 |
| `CNNFeatureMapLayer2Channels` | 맵 2개와 첫 층 손잡이·캡션 |
| `CNNFeatureMapLayer3Channels` | 맵 3개와 두 번째 층 손잡이·캡션 |
| `CNNFeatureMapLayer5Channels` | 맵 5개와 세 번째 층 손잡이·캡션 |
| `CNNFeatureMapOutput` | 출력 막대와 캡션. Workspace에서 A/B 두 개 사용 |
| `CNNFeatureMapLessonControls` | 단계 제목·설명·결과와 6개 안내 버튼 |
| `CNNFeatureMapFilter` | 3×3 필터 격자와 민트색 손잡이 |
| `CNNFeatureMapFilterCard` | 필터 이름·가중치·패딩 안내 |

맵은 동일한 뉴런 원본을, 각 층은 동일한 맵 원본을 중첩 사용한다. 행·열과 배치는 인스턴스에 저장하며, 채널은 맵의 `_channel`, 층 인덱스는 층의 `_layerIndex`에서 설정한다. 입력 픽셀과 은닉 뉴런 수, 계산 방식과 잡기 동작은 기존 구성을 유지한다.

`CNNFeatureMapScene.Awake()`가 하위 부품에 현재 Workspace를 연결한다. 맵이 자기 노드에 층·채널 문맥을 전달하므로 재사용하는 원본 프리팹에 특정 Workspace 참조를 저장하지 않는다. 가이드의 UI·필터 참조는 Workspace에 연결되어 있고 버튼 콜백은 초기화 시 연결한다. 필터와 결과 뉴런을 잇는 연결선만 실행 중 생성한다.

에셋은 Unity를 실행하지 않고 YAML로 분리했다. 파일 내부와 중첩 원본의 참조를 정적으로 확인했으며, Unity 임포트·Play·기기 동작은 실행 확인 전이다.

한글 표시는 `Assets/Fonts/NotoSansCJKkr-Regular.otf`의 [Noto Sans CJK KR](https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/Korean) Regular를 포함하며, `Assets/Fonts/OFL.txt`에 [SIL OFL 원문](https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE)을 보존한다.

## 확인 상태

소스와 에셋 참조를 정적으로 검토했다. 저장소 `AGENTS.md`의 사전 허가 규칙에 따라 Play·테스트·컴파일·빌드는 실행하지 않았다. 실제 화면 배치와 Quest 조작은 아직 실행 확인 전이다. Build Settings와 프로젝트 패키지 설정은 변경하지 않았다.
