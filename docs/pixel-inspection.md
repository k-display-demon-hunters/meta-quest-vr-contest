# CNN 픽셀 관찰

## 체험 방향

Quest의 3D 공간에서 이미지판과 필터를 관찰한다. 필터에는 손잡이가 없으며 컨트롤러 레이로 가리키고 트리거를 누른 채 이동한다. 주변은 단색이고 요청하지 않은 조명선·장식·제목·설명은 추가하지 않는다. PC 마우스는 개발용 보조 입력이다.

시작 씬은 Assets/01_Scenes/CNNPixelLab.unity다. 이전 2D 메뉴와 화면 전환용 씬·스크립트는 제거했다.

## 이용 순서

1. 컬러 사진이 0.8초 동안 표시되고, 2.2초 동안 가로 조각들이 어긋나고 내려가며 색을 잃는다. 흑백 화면이 남으면 필터와 버튼이 나타난다. 그 위로 필터를 드래그한다.
2. 필터 안에서 선택 영역의 3×3 픽셀이 확대된다.
3. ‘더 자세히 보기’에서 같은 영역의 밝기 값 하나씩을 본다.
4. ‘다음으로’를 누르면 이미지판이 0.65초 동안 두 배로 확대된다.
5. 확대된 이미지를 클릭하거나 Quest 레이와 트리거로 선택하면 약 2초 동안 왼쪽에서 오른쪽으로 픽셀과 숫자가 나타난다.
6. ‘처음부터’는 이미지와 버튼 위치를 복원하고 컬러 붕괴 도입부부터 다시 재생한다.

## 숫자의 의미

입력 사진은 Assets/Textures/CNN/ColorPhoto.png다. 런타임에 128×72로 축소하고 주변을 평균 내어 부드럽게 표시한다. 각 픽셀을 0.299R + 0.587G + 0.114B로 흑백 변환한다. 관찰과 전체 전환에 쓰는 32×18 데이터는 이 흑백 이미지에서 샘플링한다. 원본 사진의 모든 픽셀을 그대로 표시하는 것은 아니다.

각 숫자는 0–255 범위의 흑백 밝기다. 0은 검정, 255는 흰색이다. 표시용 텍스처는 R=G=B로 저장하므로 코드에서는 r 값을 읽어 숫자 하나로 표시한다. 원본 컬러 이미지의 빨간색 채널만 쓰는 의미가 아니다.

이 흑백 변환은 합성곱과 구분되는 전처리다. 컬러 CNN은 일반적으로 RGB 3채널을 유지한다. 이번 체험은 단일 채널 합성곱을 설명하기 위한 구성이다.

## 다음 단계 연결

PixelRevealStage.IsReadyForConvolution은 전환 완료 시 true다. On Pixels Revealed 이벤트에 합성곱 단계를 연결할 수 있다. 합성곱 계산은 아직 구현하지 않았다.
기존 씬에 연결하려면 Edit Mode에서 CNNPixelLab을 열고 Tools > CNN > Install Pixel Reveal Stage를 실행한다.
실제 Quest 기기 조작은 별도 확인이 필요하다.

## 이미지 제작 기록

ImageGen으로 새 흑백 사진을 생성했다. 저장 위치: client/Assets/Textures/CNN/GrayscalePhoto.png.
프롬프트: Create a single photorealistic grayscale landscape photograph used as the input image for an educational convolution exercise. Wide 16:9 composition. A simple real mountain landscape with a lake, broad recognizable light sky, mid-gray mountain slopes and dark shoreline shapes. Ordinary soft daylight, natural smooth continuous tones, slightly soft focus but recognizable photographic detail. Black and white only. This must look like a normal camera photograph, absolutely no pixel art, no blocky mosaic, no visible pixel grid, no numbers, no text, no logos, no border or interface. No extra graphic elements. It will be blurred and sampled programmatically in Unity.

## 컬러 도입부 이미지

Built-in ImageGen edit로 기존 흑백 사진을 컬러화했다.
저장: client/Assets/Textures/CNN/ColorPhoto.png
최종 프롬프트: Colorize this grayscale lake photograph with natural blue sky and water, green forests and neutral rocky mountains. Preserve the composition and all objects. No text or added elements.

도입부 붕괴는 이야기 연출이며 RGB→흑백 전처리와 합성곱은 별개다. 최종 밝기 데이터는 같은 컬러 원본에서 계산한다. 카메라와 실제 이미지판은 고정하고 이미지 내부만 변형한다. 이번 변경은 컴파일 완료했으며 Play Mode와 Quest 실기기 동작 확인은 수행하지 않았다.
