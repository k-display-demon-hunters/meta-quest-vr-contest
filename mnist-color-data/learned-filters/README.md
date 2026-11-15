# 숫자 분류 학습에서 가져온 필터

ROOM 1-2와 ROOM 2의 고정 가중치를 **숫자 정답만으로 학습한 CNN의 실제 float32 값**으로 교체한다. 이전의 사람이 정한 방향별 목표 맵·마스크 회귀·잡음 제거 학습은 제거했다. 손으로 만든 필터, 목표 피처맵, 학습 후 계수 보정·반전·양자화를 사용하지 않는다. 시각화 이름도 세로·가로·대각선 대신 실제 채널 번호를 사용한다.

Unity에는 가중치와 그에 종속된 저장 결과만 반영한다. 입력 그림, 레이어 개수, 연산 방식, 진행, 출력 판정 방식은 유지한다. 전체 분류 모델은 이 폴더에 재현 근거로 보관하며 Unity에서 모델을 실행하지 않는다.

## 실행

클라우드 환경에서 저장소 루트를 기준으로 실행한다. Python 3.12.14, NumPy 2.3.5, Pillow 12.3.0, TensorFlow CPU 2.20.0, Keras 3.15.1을 사용했다.

```bash
python -m venv /tmp/cnn-classification-env
/tmp/cnn-classification-env/bin/python -m pip install -r mnist-color-data/learned-filters/requirements.txt

# 정답 분류 학습부터 가중치·모델·미리보기·보고서를 다시 생성한다.
/tmp/cnn-classification-env/bin/python mnist-color-data/learned-filters/fit_and_export.py --train --check-model

# 저장 모델과 대조하고 기존 Unity 가중치·결과 배열에 반영한다. 재학습하지 않는다.
/tmp/cnn-classification-env/bin/python mnist-color-data/learned-filters/fit_and_export.py --check-model --apply

# 저장된 npy 가중치에서 결과만 재생성할 때는 NumPy와 Pillow만 필요하다.
python mnist-color-data/learned-filters/fit_and_export.py
```

`--train`은 이 폴더의 모델·가중치·학습 기록을 덮어쓴다. `--apply`가 없으면 Unity 소스를 수정하지 않는다. `--apply`는 `PictureConvolution.RedWeights`와 `CNNFeatureMapLab/ExecutionData`의 기존 5개 C# 파일에 반영한다. 중간 `export/` C# 사본은 Git에서 제외한다. 원본 다운로드나 네트워크 접속은 필요 없다.

CPU 단일 스레드, TensorFlow 결정적 연산, oneDNN 비활성화를 사용한다. 데이터 분할과 각 모델 초기화·배치 섞기의 시드를 고정했다. 같은 라이브러리·CPU 환경에서 재현하며 다른 환경의 부동소수 계산에는 작은 차이가 가능하다. 저장된 모델·npy 파일은 이번 실행의 원본 결과다.

## 자료, 학습과 선택

기존 `MnistTraining.bytes`에서 숫자 1·2·3 각 120장, 총 360장의 **실제 컬러 RGB**를 읽는다. 원본 인덱스순으로 정렬하고, 시드 `20261115`로 클래스별 12장씩 검증에 배정한다. 학습 324장, 검증 36장이다. `training_indices.npy`, `validation_indices.npy`가 MNIST 원본 인덱스다.

Pillow bilinear로 28×28을 16×16으로 축소하고 float32 `/255`만 적용한다. 색 재합성, 마스크 입력, 방향별 목표, 데이터 증강은 사용하지 않는다. 기존 컬러 데이터와 정답 대응을 유지한다.

- 두 모델 모두 손실은 숫자 라벨 1·2·3에 대한 sparse categorical cross-entropy 하나다.
- 무작위 Glorot 초기화, 초기 bias 0, Adam, batch 32, 200 epochs. 학습률은 1~120 epoch에 0.003, 121~200에 0.001이다.
- 초기화·셔플 시드 `20261115`, `20261116`, `20261117`의 후보를 학습한다. 검증 손실이 가장 작은 체크포인트를 선택하며 동률이면 먼저 나온 것을 유지한다.
- 두 모델과 ROOM 1-2 필터를 확정한 후에만 `MnistTest.bytes`의 숫자 1·2·3 각 30장, 총 90장을 평가한다. 모델·필터 선택에 test를 쓰지 않는다. 원본 인덱스 중복도 확인한다.

## ROOM 1-2

R 채널만 쓰는 별도 분류 모델을 학습한다.

`16×16×1 → Conv(3×3, 8, same, bias 없음)+ReLU → MaxPool(2×2) → Conv(3×3, 8, same)+ReLU → Flatten → Dense(3)`

선택된 모델의 첫 Conv에서 검증 이미지의 내부 14×14 반응에 대한 공간 표준편차를 이미지별로 계산하고 평균이 가장 큰 채널을 고른다. 죽은 채널을 피하고 위치별 반응이 보이는 필터를 선택하는 기준이며 방향별 목표와 비교하지 않는다. 선택 결과는 0부터 센 **채널 7**이다. 이 필터의 실제 값은 다음과 같다(아래 표만 소수 6자리로 표시).

```text
 0.128475   0.555714  -0.165759
 0.620212   0.274826  -0.777433
 0.205255  -0.356444  -0.506978
```

`red.npy`와 C# 상수에는 원래 float32를 그대로 보관한다. 9자리 유효숫자로 내보내 float32로 다시 읽으면 같은 값이다. G/B는 기존처럼 0, bias 0이다. 원래 방의 32×18 RGB(0~255), valid stride 1, **ReLU 전 합산**과 30×16 출력 크기를 유지한다. 학습 모델의 첫 층에 비해 입력 단위가 255배이며 외곽 패딩을 제외한 계산이다. ROOM 1-2 자체가 전체 분류 모델이라는 의미는 아니다.

양수·음수 가중치가 함께 있으므로 가중 평균이나 0~255로 제한된 결과가 아니다. 기존 UI의 가중치 라벨은 소수 둘째 자리까지, 합은 첫째 자리까지 반올림해 표시하지만 실제 합산은 원본 float32로 한다. 기존 밝기 표시 `value/255`도 유지하여 음수는 검게, 255 초과는 흰색으로 포화되고 숫자 표시에는 원래 부호 있는 출력값이 남는다. 필터를 보기 좋게 만들기 위해 계수를 바꾸지 않았다.

## ROOM 2

실제 분류 모델은 다음 구조다.

`16×16×3 → Conv(3×3, 2, same)+ReLU → MaxPool(2×2) → Conv(3×3, 4, same)+ReLU → Flatten → Dense(3)`

첫 Conv 2개와 두 번째 Conv 4개, bias 전부를 **선택한 체크포인트 그대로** 사용한다. 큰 모델에서 일부 채널을 떼어 의존 관계가 달라진 모델이 아니다. 세 숫자는 동일한 가중치를 공유한다. `conv1.npy`, `conv2.npy`의 shape는 `[9, 입력 채널, 출력 채널]`이고 9칸은 위에서 아래·왼쪽에서 오른쪽 순이다. `bias1.npy`, `bias2.npy`도 원본 학습값이다.

기존 `Digit1/2/3ExecutionData.cs`의 inputR/G/B 선언은 원문 그대로 보존한다. 이 세 그림은 기존 방의 직접 제작 입력이며, 학습·검증·평가 데이터에서 가져온 MNIST 이미지가 아니다. 이 그림에 대한 다음 결과를 다시 계산한다.

- Conv 부분합·bias·ReLU 결과와 대표 칸(최댓값)의 값·위치
- 채널별 실제 MaxPooling 값과 대표 2×2 창
- 세 숫자에 공통으로 적용하는 채널별 밝기 스케일
- 기존 숫자별 기준 틀, 점수, `softmax(8×score)`, 출력 기여도

**Unity의 최종 출력은 기존 기준 틀 비교 방식이다.** 저장 모델의 학습된 Dense를 Unity에 이식하지 않았다. 기준 틀은 각 시연 입력의 마지막 맵을 채널별 최대값으로 나눈 것으로, 점수는 `dot(current/scale, template) / sum(template²)`이다. 시연 입력 자체가 기준 틀에 포함되므로 Unity의 출력 비율은 미지 데이터의 분류 정확도가 아니다. 개별 피처맵 픽셀은 임의로 수정하지 않는다.

## 결과와 확인 범위

| 모델 | 선택 시드 / epoch | 검증 정확도 | 별도 평가 정확도 |
| --- | --- | --- | --- |
| ROOM 2 전체 분류 모델 | 20261117 / 197 | 35/36 (97.22%) | 83/90 (92.22%) |
| ROOM 1-2 필터의 원본 R 분류 모델 | 20261116 / 77 | 33/36 (91.67%) | 81/90 (90.00%) |

정확도는 이 작은 컬러 데이터 부분집합의 **전체 분류 모델** 수치다. 단일 필터, 기존 방의 그림, Unity의 기준 틀 판정 성능을 나타내지 않는다.

- `corridor.keras`, `red-classifier.keras`: 선택한 전체 분류 모델, optimizer 상태 포함.
- `train_classifiers.py`, `training-history.csv`, `fit-report.md`: 실제 학습 코드, 후보별 모든 epoch 기록, 선택·평가 결과.
- `fit_and_export.py`, `export-report.md`: 결과 재계산과 저장 모델 대조. Conv/Pool 결과를 TensorFlow 독립 순전파와 대조하고 C# 배열의 float32 왕복 보존을 확인한다.
- `preview.png`: 기존 세 입력, Conv1 두 채널, Pooling 두 채널, Conv2 네 채널 순. 밝을수록 해당 채널의 ReLU 후 반응이 크다. Conv1은 16×16, 이후는 8×8이다. Pooling 앞뒤는 같은 픽셀 배율로, Conv2는 2배 더 크게 표시한다. 실제 학습 이미지나 Unity 화면 캡처가 아니라 **계산 결과의 확대 미리보기**다.

미리보기에서 첫 채널은 획 주변 반응이, 두 번째 채널은 배경 쪽 반응이 두드러지지만 이는 학습 후 현재 입력에서 관찰한 결과다. 특정 방향의 전용 필터라고 단정하지 않는다. 깊은 층은 여러 입력 채널의 반응을 조합하며 숫자 전체가 선명하게 복원될 필요는 없다. Padding 경계에도 반응할 수 있다.

클라우드 Python 학습·내보내기·수치 대조를 실행했다. Unity 실행·컴파일·빌드·테스트는 하지 않았다. 따라서 VR 화면의 실제 가독성은 실행 확인하지 않았다. 출처·권리는 `docs/ASSET_CREDITS.md`의 MNIST와 기존 입력 항목을 따른다. Codex 보조로 TensorFlow 학습, NumPy 내보내기, Pillow 시각화 코드를 작성했다.
