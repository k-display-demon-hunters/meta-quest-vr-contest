# 두 방의 데이터 기반 교육용 필터

ROOM 1-2와 ROOM 2의 입력·레이어 개수·진행·출력 판정 방식을 유지하면서 고정 가중치를 데이터로 적합한 값으로 교체한다. NumPy 최소제곱 회귀로 **교사가 지정한 특징 반응을 학습**한다. TensorFlow 분류 모델을 학습하거나 16개 필터에서 2개를 선택한 결과는 아니다. 이 폴더는 학습 방법과 교육용 제약, 내보낸 가중치의 재현 기록이다.

## 재현

Python 3.11 이상, NumPy 2.3.5, Pillow 12.3.0. 기존 `mnist-color-data/requirements.txt`와 같다. 클라우드에서 저장소 루트 기준으로 실행한다.

```bash
# 원본 마스크를 스트리밍하고 가중치를 다시 적합한다. Unity 파일은 수정하지 않는다.
OPENBLAS_NUM_THREADS=1 python mnist-color-data/learned-filters/fit_and_export.py --fit

# 저장된 가중치로 현재 세 입력의 결과와 미리보기를 다시 내보낸다. 네트워크 없음.
OPENBLAS_NUM_THREADS=1 python mnist-color-data/learned-filters/fit_and_export.py

# 내보낸 결과를 기존 Unity 가중치·데이터 파일에 반영한다.
OPENBLAS_NUM_THREADS=1 python mnist-color-data/learned-filters/fit_and_export.py --apply
```

`--fit`은 이 폴더의 가중치·보고서를 덮어쓴다. 일반 실행은 이 폴더의 미리보기·보고서·`export/` C# 파일만 갱신한다. `export/` 사본은 Git에서 제외한다. `--apply`는 `PictureConvolution.RedWeights` 및 `CNNFeatureMapLab/ExecutionData`의 기존 5개 C# 파일도 갱신한다. UI 연결이나 프리팹을 자동 편집하지 않는다. 같은 라이브러리와 BLAS 구현에서 재현한다. 다른 플랫폼에서는 회귀 계산의 작은 부동소수 오차가 가능하다.

## 자료와 분할

기존 `MnistTraining.bytes`의 숫자 1·2·3, 각 120장만 선택한다. 총 360장의 원본 인덱스로 CVDF의 MNIST 이미지·라벨 IDX 스트림을 읽고 마지막 필요한 인덱스에서 중단한다. 원본 아카이브는 보관하지 않는다.

- https://storage.googleapis.com/cvdf-datasets/mnist/train-images-idx3-ubyte.gz
- https://storage.googleapis.com/cvdf-datasets/mnist/train-labels-idx1-ubyte.gz

원본 라벨과 기존 라벨을 대조한다. 원본 마스크는 28×28에서 16×16으로 bilinear 축소한다. 시드 `20261115`로 숫자별 12장씩 validation에 배정하여 fitting 324장, validation 36장으로 나눈다. `training_indices.npy`, `validation_indices.npy`가 원본 인덱스다. test 파일은 읽지 않는다.

기존 컬러 데이터의 무작위 색상 그대로를 사용한 학습은 아니다. **현재 방의 고정 주황 숫자·청록 배경에 맞춰 색을 재합성**한다. 현재 3개 입력 배열에서 R>0.7 픽셀의 RGB 평균을 숫자색, R<0.6 픽셀 평균을 배경색으로 사용한다. 원본 마스크로 두 색을 혼합하고 RGB마다 표준편차 0.10의 독립 정규 잡음을 더한 뒤 0~1로 제한한다. 이 색 보정은 학습 입력 생성에만 적용하며 실제 Unity 입력 그림은 변경하지 않는다. 색상에 대한 보편적 일반화 성능을 주장하지 않는다.

## ROOM 1-2

R 채널의 noisy 3×3 패치로 잡음 없는 중앙 R 값을 예측하도록 제약 최소제곱을 1,500번의 projected gradient step으로 최적화한다. 가중치는 음수 없이 합이 1인 simplex 위에 둔다. 경계 1픽셀은 fitting에서 제외한다. 학습률은 Gram 행렬 최대 고윳값으로 정한다. 편향 0, G/B 가중치 0, 활성화 없음, valid stride 1이다.

교육용 표시를 위해 학습값을 0.01 단위로 양자화하고 largest-remainder 방식으로 합 1을 보존했다. **표시와 계산 모두 이 양자화한 값을 사용**한다.

```text
0.00  0.12  0.04
0.14  0.42  0.13
0.04  0.11  0.00
```

R 값의 학습된 가중 평균이므로 기존 0~255 밝기와 R-only 설명을 유지한다. 중심에 큰 비중을 주면서 주변 값을 혼합하는 잡음 완화 필터다. 숫자 전체를 인식하거나 방향 특징을 분류하는 필터는 아니다. 0 가중치의 곱도 기존 아홉 항 연출에 포함된다. `red.npy`는 실제 적용한 float32 가중치다.

## ROOM 2

구조: 16×16×3 → Conv(3×3, 2채널, same, stride 1)+ReLU → MaxPooling(2×2, stride 2) → Conv(3×3, 4채널, same, stride 1)+ReLU. 기존 숫자 1·2·3 입력 배열을 그대로 사용한다.

첫 Conv의 목표는 Gaussian 3×3 `[[1,2,1],[2,4,2],[1,2,1]]/16`으로 부드럽게 한 마스크와 그 보수(배경)다. RGB 패치에서 두 목표를 예측하도록 bias 포함 ridge regression을 푼다. 정규화 계수는 0.001, bias는 정규화하지 않는다.

첫 층의 실제 예측을 ReLU·Pooling한 결과로 두 번째 층을 적합한다. 목표는 원본 마스크를 Pooling한 뒤 아래 네 커널로 얻은 **활성화 전 반응**이다. 정규화 계수 0.003, bias 비정규화. 최종 예측에는 ReLU를 적용한다.

| 맵 | 교육용 목표 커널 |
| --- | --- |
| 0: 세로 | `[-.5,1,-.5; -.5,1,-.5; -.5,1,-.5]` |
| 1: 가로 | `[-.5,-.5,-.5; 1,1,1; -.5,-.5,-.5]` |
| 2: 형태 | `[[1,2,1],[2,4,2],[1,2,1]]/16` |
| 3: 대각선 / | `[-.5,-.5,1; -.5,1,-.5; 1,-.5,-.5]` |

이 표는 **학습 목표를 만든 커널**이고 실제 적용 가중치는 회귀로 얻은 `conv2.npy`다. 첫 층의 실제 잡음·Pooling 반응에 맞춰 목표를 근사한다. 자동으로 방향 의미를 발견한 분류 모델이라고 설명하지 않는다. Conv1/2 가중치 shape는 `[9, 입력채널, 출력채널]`이며 9개 칸은 위에서 아래·왼쪽에서 오른쪽이다. `bias1.npy`, `bias2.npy`도 실제 적용값이다. 세 숫자는 동일한 가중치를 공유한다.

## 기존 결과 재계산

`Digit1/2/3ExecutionData.cs`에서 기존 inputR/G/B 선언을 읽어 원문 그대로 보존한다. 고정 가중치 변경에 따라 다음 값만 재계산한다.

- Conv 전후 값, 입력 채널별 부분합, 대표 칸(맵 최대 반응)의 인덱스·기여도·bias
- 채널별 실제 MaxPooling, 대표 2×2 창과 최댓값 위치
- 세 입력에 공통으로 적용하는 표시 스케일
- 기존 방식의 숫자별 기준 틀, 점수와 `softmax(8×score)`, 출력 기여도

최종 틀은 각 입력의 마지막 맵을 공통 채널 최대값으로 나눈 값이다. 기존 점수 `dot(current/scale, template) / sum(template²)`를 유지한다. **시연 입력 자체가 기준 틀에 포함**되므로 점수·확률은 미지 데이터 정확도가 아니다. Dense 학습으로 바꾸지 않았다. 개별 피처맵 픽셀을 임의로 수정하지 않는다.

## 확인 범위와 파일

- `fit-report.md`: 별도 validation 이미지의 합성 잡음 제거 오차와 특징 목표 근사 오차. 분류 정확도·실제 색점판 성능이 아니다.
- `export-report.md`: 세 시연의 점수와 확률, 내보내기 중 수치 일관성 확인.
- `preview.png`: 현재 방의 입력과 새 피처맵을 같은 채널별 스케일로 시각화. Pooling 앞 16×16, 뒤 8×8을 같은 픽셀 배율로 표시한다. Unity 화면 캡처가 아니다.

Python 가중치 적합 및 결과 생성을 실행했다. C# 배열을 다시 읽어 원본 입력 보존·필터와 저장 결과의 일치·출력 기여도 합을 확인한다. Unity 실행·컴파일·빌드·테스트는 수행하지 않았다. 렌더링 및 VR 가독성은 실행 검증하지 않았다.

MNIST 및 기존 입력 이미지·파생 시각화의 출처는 `docs/ASSET_CREDITS.md`의 해당 항목을 따른다. Codex 보조로 NumPy 회귀·Pillow 시각화·C# 내보내기 코드를 작성했다.
