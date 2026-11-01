using System;
using System.Collections.Generic;
using UnityEngine;

namespace Parchment
{
    public enum ParchmentIllustrationType
    {
        PixelGrid,
        PaddingGrid,
        VerticalEdgeFilter,
        FeatureMaps,
        ReluGraph
    }

    public sealed class ParchmentPage
    {
        public string Body { get; }
        public ParchmentIllustrationType Illustration { get; }

        public ParchmentPage(string body, ParchmentIllustrationType illustration)
        {
            Body = body;
            Illustration = illustration;
        }
    }

    public sealed class ParchmentLesson
    {
        public int Id { get; }
        public string Title { get; }
        public string Topic { get; }
        public string Department { get; }
        public Pose PickupPose { get; }
        public IReadOnlyList<ParchmentPage> Pages { get; }

        public ParchmentLesson(int id, string title, string topic, string department,
            Vector3 pickupPosition, float pickupYaw, params ParchmentPage[] pages)
        {
            Id = id;
            Title = title;
            Topic = topic;
            Department = department;
            PickupPose = new Pose(pickupPosition, Quaternion.Euler(0f, pickupYaw, 0f));
            Pages = Array.AsReadOnly((ParchmentPage[])pages.Clone());
        }
    }

    public static class ParchmentCatalog
    {
        public static IReadOnlyList<ParchmentLesson> All { get; } = Array.AsReadOnly(new ParchmentLesson[]
        {
            // These three world poses are applied on scene startup without editing the scene asset.
            new ParchmentLesson(0, "이미지와 픽셀", "픽셀 · 밝기 · RGB", "원본 검사 담당",
                new Vector3(-10.5f, 1.45f, 0f), -90f,
                new ParchmentPage("디지털 이미지는 작은 칸이 가로와 세로로 배열된 데이터다. 이미지를 구성하는 가장 작은 단위를 <b>픽셀(Pixel)</b>이라고 한다.\n\n가로 28픽셀, 세로 28픽셀인 이미지는 28×28, 즉 784개의 픽셀로 이루어져 있다. 이미지의 가로와 세로 픽셀 수를 알면 전체 픽셀 수를 구할 수 있다. 컴퓨터는 각 픽셀에 기록된 숫자를 입력으로 사용한다.", ParchmentIllustrationType.PixelGrid),
                new ParchmentPage("<b>흑백 이미지</b>의 픽셀에는 밝기를 나타내는 값 하나가 들어 있다. 밝기를 0~255로 표현할 때 0은 검정, 255는 하양이다. 그 사이의 값은 회색이며 숫자가 클수록 밝다.\n\n예를 들어 30인 픽셀은 어두운 회색이고, 220인 픽셀은 밝은 회색이다. 신경망에 입력할 때는 이 값들을 255로 나누어 0~1 범위로 바꾸기도 한다. 표현 범위가 달라도 밝기의 관계는 유지된다.", ParchmentIllustrationType.PixelGrid),
                new ParchmentPage("<b>컬러 이미지</b>는 보통 빨강(R), 초록(G), 파랑(B)의 세 값으로 색을 표현한다. 0~255 기준에서 (255, 0, 0)은 빨강, (255, 255, 255)는 하양이다.\n\n이미지 전체의 R 값만 모은 배열처럼, 같은 종류의 값을 모아 놓은 것을 <b>채널(Channel)</b>이라고 한다. RGB 이미지는 세 채널을 가진다. 따라서 컴퓨터가 처리하는 이미지는 픽셀의 위치와 각 채널의 숫자로 이루어진 배열이다.", ParchmentIllustrationType.PixelGrid)),
            new ParchmentLesson(1, "특징 맵", "위치별 반응 · 출력 채널", "특징 분석 담당",
                new Vector3(-14.5f, 1.45f, 11.2f), 90f,
                new ParchmentPage("<b>특징 맵(Feature Map)</b>은 필터를 입력의 여러 위치에 적용한 계산 결과를 공간적 배열로 모은 것이다. 특징 맵의 각 칸에는 입력의 해당 영역에서 계산한 값이 들어 있다.\n\n세로 경계를 검출하는 필터를 적용했다면, 각 위치의 좌우 밝기 차이가 결과에 반영된다. 특징 맵은 원본의 색을 그대로 복사한 이미지가 아니다. 특정 필터가 입력의 어느 위치에서 얼마나 반응했는지를 나타낸다.", ParchmentIllustrationType.FeatureMaps),
                new ParchmentPage("일반적인 합성곱 층에서는 <b>필터 하나가 출력 채널 하나</b>를 만든다. 서로 다른 필터를 여러 개 적용하면 여러 채널의 특징 맵을 얻는다. 각 채널은 서로 다른 특징 정보를 담을 수 있다.\n\n특징 맵을 화면에 표시할 때는 숫자를 밝기나 색으로 바꾸기도 한다. 이때 화면의 밝기는 원본 픽셀의 밝기와 구분해야 한다. 또한 합성곱 결과에 활성화 함수를 적용한 배열도 흔히 특징 맵이라고 부른다.", ParchmentIllustrationType.FeatureMaps),
                new ParchmentPage("<b>실습: 특징 맵을 하나씩 껐다 켜며 비교한다.</b>\n\n맵 하나를 숨긴 뒤 어떤 경계나 무늬의 표시가 사라지는지 관찰한다. 다시 켜서 돌아오는 부분을 확인하면 각 맵이 담고 있는 정보를 비교할 수 있다. 같은 원본에서 만들어졌어도 맵마다 반응하는 위치는 다를 수 있다.\n\n이 실습에서 끄는 것은 <b>화면 표시</b>다. 해당 맵을 신경망의 실제 계산에서 제외하거나 신경망의 예측을 바꾸는 동작은 아니다.", ParchmentIllustrationType.FeatureMaps)),
            new ParchmentLesson(2, "활성화 함수와 ReLU", "출력 변환 · 비선형성", "신경망 연산 담당",
                new Vector3(-14.5f, 1.45f, 12.8f), 90f,
                new ParchmentPage("<b>활성화 함수(Activation Function)</b>는 신경망에서 계산한 값에 추가로 적용하는 함수다. 일반적으로 입력에 가중치를 곱하고 편향을 더한 뒤, 그 결과를 활성화 함수에 넣는다.\n\n활성화 함수의 종류에 따라 숫자가 변환되는 규칙도 다르다. 대표적인 함수로 <b>ReLU</b>가 있으며, ‘렐루’라고 읽는다. 합성곱 층에서 ReLU를 사용할 때는 계산된 특징 맵의 각 칸에 같은 규칙을 개별적으로 적용한다.", ParchmentIllustrationType.ReluGraph),
                new ParchmentPage("<b>ReLU의 규칙</b>은 다음과 같다.\n\n입력이 0보다 작으면 0을 출력한다.\n입력이 0 이상이면 입력값을 그대로 출력한다.\n\n입력: −3, −1, 0, 2, 5\n출력:   0,   0, 0, 2, 5\n\n즉, 음수만 0으로 바뀌고 양수는 그대로 남는다. ReLU를 적용한 결과는 모두 0 이상이다. 이 규칙은 값의 크기를 순위대로 골라내는 것이 아니라, 각 값이 음수인지에 따라 출력을 정한다.", ParchmentIllustrationType.ReluGraph),
                new ParchmentPage("활성화 함수의 중요한 역할은 신경망에 <b>비선형성</b>을 부여하는 것이다. 가중치를 곱하고 편향을 더하는 계산만 여러 층에 걸쳐 반복하면, 전체를 하나의 같은 형태의 계산으로 합칠 수 있다.\n\n따라서 그런 연산만으로는 층을 늘려도 표현할 수 있는 관계가 충분히 다양해지지 않는다. ReLU처럼 비선형인 함수를 층 사이에 적용하면, 신경망이 여러 층을 통해 더 복잡한 입력과 출력의 관계를 학습할 수 있다.", ParchmentIllustrationType.ReluGraph))
        });

        public static ParchmentLesson Get(int id)
        {
            foreach (ParchmentLesson lesson in All)
            {
                if (lesson.Id == id) { return lesson; }
            }
            throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown parchment ID.");
        }
    }
}
