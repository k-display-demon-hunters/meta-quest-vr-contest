# MNIST 컬러 데이터 600장

클라우드 Python에서 생성한 0, 1, 2, 3 각 150장 데이터다. 통합 원본 600장과 이를 분리한 training 480장, test 120장을 제공한다. 원본 정답과 손글씨 형태를 보존한다.

## 파일과 실행

- 압축하지 않은 `.npy` 파일: `images.npy` `(600,28,28,3)` uint8 RGB, `labels.npy` `(600,)` uint8. 행 순서가 서로 대응한다. `source_indices.npy`는 원본의 0 기반 인덱스, `style_seeds.npy`는 행별 합성 시드다.
- `MnistColor.bytes`: 동일한 600장 RGB 픽셀·정답·원본 인덱스를 담은 압축 없는 바이너리. 1,414,224바이트. 아래 CMNT v2 형식으로 저장한다.
- `MnistTraining.bytes`: training 480장(숫자별 120장), 1,131,384바이트.
- `MnistTest.bytes`: test 120장(숫자별 30장), 282,864바이트.
- `preview.png`: 전체 600장을 원래 순서대로 표시. 각 타일은 실제 28×28을 nearest-neighbor로 4배 확대한 112×112이며 아래 표기는 `행 번호: 정답`이다. 회색 타일 간격과 표기는 데이터에 포함되지 않는다.
- `generation-report.txt`: 실제 생성 결과의 크기·자료형·개수·중복 확인.
- `generate.py`, `requirements.txt`: 생성 코드와 재현용 의존성. 생성 시 `.npy`와 `.bytes`를 함께 저장한다.
- `export_bytes.py`: 기존 `.npy`에서 `.bytes`만 저장하는 변환 코드. 다운로드나 이미지 재합성 없이 실행할 수 있다.

저장소 루트에서 **클라우드 환경**으로 실행한다. Python 3.11 이상을 사용한다.

```bash
python -m pip install -r mnist-color-data/requirements.txt
python mnist-color-data/generate.py
```

실행하면 같은 폴더의 산출물을 다시 쓴다. 서버는 필요 없다.

```python
import numpy as np
images = np.load('mnist-color-data/images.npy', allow_pickle=False)
labels = np.load('mnist-color-data/labels.npy', allow_pickle=False)
normalized = images.astype(np.float32) / 255.0
```

## Unity용 바이너리 저장 형식

기존 배열을 그대로 변환하려면 저장소 루트에서 실행한다.

```bash
python mnist-color-data/export_bytes.py
```

세 `.bytes` 파일은 Unity에서 TextAsset의 `bytes`로 접근할 수 있는 일반 바이너리 파일이다. 기존 CMNT v1의 레코드 방식(원본 인덱스·정답·픽셀)을 유지하고, RGB 데이터를 표현하는 **CMNT v2** 헤더를 사용한다. 현재 ModelDesign 로더는 v2 RGB 형식을 읽는다. training/test 파일은 기존 Unity 런타임 데이터 경로에 적용되며 입력 3채널·출력 4클래스로 연결한다. 런타임 상세는 `../docs/CNN_TRAINING.md`를 참고한다.

모든 int32는 little-endian이다. 헤더는 24바이트이며 split 필드는 없다.

| 오프셋 | 자료형 | 값 |
| --- | --- | --- |
| 0 | ASCII 4바이트 | `CMNT` |
| 4 | int32 | version = 2 |
| 8 | int32 | height = 28 |
| 12 | int32 | width = 28 |
| 16 | int32 | channels = 3 |
| 20 | int32 | count = 통합 600 / training 480 / test 120 |

헤더 뒤에는 다음 레코드 count개가 원본 `.npy`의 상대 순서를 유지하며 연속 저장된다. 레코드당 2,357바이트이며 패딩이나 압축, 파일 끝의 추가 데이터는 없다.

| 레코드 내 오프셋 | 자료형 | 내용 |
| --- | --- | --- |
| 0 | int32 | 원본 MNIST training 파일 내 0 기반 인덱스 |
| 4 | uint8 | 정답 0, 1, 2, 3 |
| 5 | uint8 × 2,352 | 28×28×3 RGB 픽셀, 0~255 |

픽셀은 위쪽 행부터 왼쪽→오른쪽, 픽셀마다 R·G·B 순서(HWC)다. 채널 위치는 `(y * width + x) * channels + c`이며 c=0/1/2는 R/G/B다. 나중에 C# `BinaryReader`로 헤더 정수를 읽고, 각 레코드를 `ReadInt32()`, `ReadByte()`, `ReadBytes(height * width * channels)` 순서로 읽으면 된다. 학습 입력을 만들 때 픽셀을 `/255f`로 정규화한다. 스타일 시드는 재현용 `.npy`에만 보관한다. 파일 이름으로 training/test 용도를 구분한다.

## Training / test 분리

`export_bytes.py`는 시드 `20261108`의 NumPy default_rng(PCG64)로 숫자별 행을 독립적으로 섞고, 앞 120개는 training, 뒤 30개는 test에 배정한다. 저장할 때는 원본 행 순서로 정렬한다. 두 파일의 원본 인덱스는 겹치지 않고 합치면 전체 600장이 된다. 재합성·픽셀 변경 없이 기존 RGB와 정답을 그대로 기록한다. 통합 `.npy`와 `MnistColor.bytes`는 보존한다.

별도 validation 파일은 만들지 않는다. Unity RGB·4클래스 로더는 숫자별 매 10번째 validation 분리 정책을 유지하므로, training 파일의 480장은 실제 학습 432장(숫자별 108), validation 48장(숫자별 12)이 된다. test 120장은 이 과정과 별개다. 현재 로더는 v2·RGB·4클래스 기준으로 동작한다. 이 전환의 Unity 실행·컴파일·테스트·빌드는 아직 수행하지 않았다.

## 원본과 선택

원출처: http://yann.lecun.com/exdb/mnist/
배포 안내: https://github.com/cvdfoundation/mnist

공식 CVDF 배포의 다음 두 IDX gzip 스트림을 urllib/gzip으로 메모리에서 읽는다.

- https://storage.googleapis.com/cvdf-datasets/mnist/train-images-idx3-ubyte.gz
- https://storage.googleapis.com/cvdf-datasets/mnist/train-labels-idx1-ubyte.gz

원본 training 파일은 원천의 이름일 뿐 이 결과의 용도 지정이 아니다. 인덱스 순서로 읽으며 0~3의 원본 픽셀 배열이 서로 다른 첫 150장씩 선택한다. 네 숫자가 채워지면 연결을 닫는다. gzip/HTTP 버퍼의 선행 읽기는 있을 수 있다. 전체 아카이브나 원본 픽셀 사본은 저장하지 않는다. 원본 인덱스와 픽셀 바이트 중복을 모두 제외한다. 작성자가 같은지 여부는 MNIST에서 제공하지 않으므로 판별하지 않는다.

## 합성 및 재현

시드 `20261107`, NumPy default_rng(PCG64). 150개의 스타일 시드를 생성하여 각 클래스에 같은 집합을 독립적으로 순열 배정한다. 네 클래스의 색·패턴 파라미터 분포가 정확히 같고, 원본 배열 순서로 저장한다.

스타일마다 HSV 색상 H를 [0,1)에서 선택하고 상대 색상은 0.38~0.62만큼 이동한다. 채도 0.64, 기본 밝기 0.73. 숫자와 배경 모두 2.3 픽셀 간격의 점 중심에 ±0.65 픽셀 위치 변화, 반지름 0.55~1.15 픽셀, 밝기 ±0.17 변화를 적용한다. 픽셀 중심에서 원 가장자리를 부드럽게 처리한다. 색 영역 차이를 점무늬보다 크게 유지한다.

원본 회색값/255를 연속 마스크로 사용하여 foreground*mask + background*(1-mask)를 합성하고 반올림하여 uint8로 저장한다. 회전·크기 변경·원형 검사판 없이 전체 28×28 정사각형을 사용한다. 원본의 흐린 가장자리도 유지한다. 모든 행은 동일한 알고리즘과 설정을 거친다. 확대 미리보기의 가독성은 시각적으로 확인했으며 모델 학습이나 정확도 평가는 수행하지 않았다.

## 출처와 권리

Keras 공식 문서 https://keras.io/api/datasets/mnist/ 는 MNIST 권리자를 Yann LeCun과 Corinna Cortes, 라이선스를 CC BY-SA 3.0으로 명시한다(2026-11-07 확인). 컬러 합성 데이터와 미리보기도 같은 조건으로 제공한다: https://creativecommons.org/licenses/by-sa/3.0/

Codex 보조로 Python/NumPy/Pillow 코드를 작성하고 원본에 RGB 색과 점무늬를 합성했다. 생성형 이미지 AI로 숫자를 생성하지 않았다. 합성 부분의 별도 권리자는 미확인. 프로젝트 출처 기록은 `../docs/ASSET_CREDITS.md` 참조.
