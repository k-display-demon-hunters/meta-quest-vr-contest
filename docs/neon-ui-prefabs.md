# CNN 필터·노드 표시 에셋

필터 패널과 노드의 표시 구성을 제공하는 에셋이다. 프리팹에는 Canvas·텍스트·메시·Collider가 저장되어 있으며, 계산과 조작은 사용하는 씬의 코드에서 연결한다.

## 파일별 역할

| 파일 | 역할과 연결 방식 |
| --- | --- |
| [CNNFilterPanel.prefab](../client/Assets/Prefabs/CNNPixelLab/CNNFilterPanel.prefab) | 월드 공간 Canvas 아래 `Cell_행_열`별 Image와 `Weight` TextMeshPro 텍스트를 배치해 3×3 가중치 표시를 구성한다. 프레임은 MeshRenderer, 셀·테두리는 UI Image로 나뉜다. 숫자는 저장된 텍스트 참조를 통해 갱신하는 구조이며 합성곱 계산은 사용하는 씬 코드가 담당한다. |
| [NeonNode.prefab](../client/Assets/Prefabs/CNNPixelLab/NeonNode.prefab) | `Core`의 MeshFilter는 구형 메시를, MeshRenderer는 `NeonSphere.mat`를 참조하고 SphereCollider가 물리 선택 영역을 제공한다. 별도 링 메시·재질을 쓰는 `NeonRing1`, `NeonRing2` 자식은 기본 비활성이다. 노드의 계산값·선택 이벤트 연결은 씬 코드에서 구성한다. |
| [NeonNodeSmoothSphere.asset](../client/Assets/Meshes/CNNPixelLab/NeonNodeSmoothSphere.asset) | 노드 Core의 MeshFilter에 연결되는 정점·면 데이터다. Core의 Renderer가 이 형상에 `NeonSphere.mat`를 적용하며, 표시 색상과 Collider 구성은 프리팹의 별도 참조로 관리된다. |
| [NeonNodeRing.asset](../client/Assets/Meshes/CNNPixelLab/NeonNodeRing.asset) | 링 자식의 MeshFilter들이 참조하는 메시다. 같은 메시를 각 자식 Transform으로 배치하고 `NeonNodeGlow.mat`로 렌더링한다. 자식 활성 상태를 바꿔 링 표시를 제어할 수 있다. |
| [NeonSphere.mat](../client/Assets/Materials/CNNPixelLab/NeonSphere.mat) | Core의 MeshRenderer와 `Custom/NeonSphere` 셰이더를 연결한다. `_BaseColor`, `_RimColor` 등 셰이더가 읽는 속성을 저장하므로 형상 메시와 표시 설정을 별도로 관리할 수 있다. |
| [NeonSphere.shader](../client/Assets/Shaders/CNNPixelLab/NeonSphere.shader) | URP에서 월드 법선과 카메라 방향의 내적으로 가장자리 비율을 계산한다. 이 비율로 재질의 `_BaseColor`와 `_RimColor`를 혼합해 픽셀 색상을 출력한다. 카메라 방향 변화에 따른 표현은 셰이더 계산으로 처리된다. |
| [NeonUIDemoProfile.asset](../client/Assets/Settings/NeonUIDemoProfile.asset) | Bloom·Tonemapping·Vignette의 Volume 설정값을 보관한다. 이 에셋을 씬 Volume이 참조하고 카메라 후처리가 활성화된 경우 해당 렌더링 경로에 적용된다. 노드 프리팹의 생성·계산이나 상호작용과는 별도로 연결한다. |

## 연결 구조

`NeonNode.Core`는 구형 메시와 `NeonSphere.mat`를 참조한다. 링 자식은 링 메시와 [NeonNodeGlow.mat](../client/Assets/Materials/CNNPixelLab/NeonNodeGlow.mat)를 참조한다. 노드 색상과 가장자리 표현은 재질 속성과 셰이더에서 조정한다.

`CNNFilterPanel`의 `Cell_행_열` 자식은 셀 Image를, `Weight` 자식은 가중치 텍스트를 담는다. 가중치 표시를 갱신하는 코드는 이 텍스트 컴포넌트를 연결해야 한다. 패널 재질은 [Materials/CNNPixelLab](../client/Assets/Materials/CNNPixelLab)에 있다.

Volume Profile의 효과는 사용하는 씬의 Volume과 카메라 후처리 설정에 따라 적용된다. 프리팹 자체에는 Volume이 포함되지 않는다.

필터 패널의 글꼴은 TextMeshPro 폰트 에셋을 참조한다. 함께 배포되는 Liberation Sans의 라이선스는 [OFL](../client/Assets/TextMesh%20Pro/Fonts/LiberationSans%20-%20OFL.txt)에 있다.
