# Unity / Meta Quest 프로젝트 코딩 컨벤션

본 규칙은 팀원뿐 아니라 GPT, Claude 등 AI가 생성하거나 수정하는 모든 C# 코드에도 동일하게 적용한다.

이 문서의 목적은 규칙 자체를 지키는 것이 아니라 다음을 달성하는 것이다.

- 코드를 쉽게 읽고 이해할 수 있도록 한다.
- 팀원 간 코드 스타일을 일관되게 유지한다.
- AI가 기존 코드의 의도를 쉽게 파악하도록 한다.
- 유지보수와 수정이 쉬운 구조를 만든다.
- Meta Quest 환경에서 불필요한 성능 비용을 줄인다.

---

# 0. 최우선 원칙

본 문서의 모든 규칙은 **절대적인 금지 사항이 아니라 기본 원칙**으로 적용한다.

특정 규칙을 기계적으로 따르는 것이 오히려 가독성, 유지보수성, 명확성을 떨어뜨린다면 합리적인 범위에서 예외를 허용한다.

즉 다음 우선순위를 따른다.

```text
1. 코드의 명확성
2. 가독성
3. 유지보수성
4. 프로젝트 전체의 일관성
5. 코딩 규칙
```

규칙을 지키기 위해 코드를 더 복잡하게 만들지 않는다.

예외가 필요한 경우에는 단순한 개인 취향이 아니라 코드의 이해도나 구조가 실제로 개선되는지를 기준으로 판단한다.

AI 역시 본 규칙을 절대적인 제약으로 해석하지 않는다.

규칙을 따르는 것보다 더 명확하고 단순한 코드가 존재하는 경우 해당 방법을 사용할 수 있다.

---

# 1. 기본 작성 원칙

## 1.1 명확한 이름을 사용한다

변수, 메서드, 클래스 이름만 보고 가능한 한 역할을 이해할 수 있도록 작성한다.

```csharp
float movementSpeed;
GameObject selectedNeuron;
int convolutionKernelSize;
```

의미를 파악하기 어려운 이름은 피한다.

```csharp
float spd;
GameObject obj;
int val;
```

다만 반복문 인덱스 등 의미가 명백한 경우 짧은 이름을 허용한다.

```csharp
for (int i = 0; i < neurons.Count; i++)
{
    ...
}
```

`UI`, `XR`, `CNN`, `RGB`, `FPS`, `API`처럼 프로젝트에서 널리 사용되는 약어는 사용할 수 있다.

---

# 2. `var` 사용

명시적인 타입을 작성하는 것을 기본으로 한다.

```csharp
GameObject targetObject = GetTarget();
float distance = CalculateDistance();
List<float> activationValues = new List<float>();
```

반환 타입을 코드만 보고 파악하기 어려운 경우에는 특히 명시적 타입을 우선한다.

```csharp
GameObject selectedNeuron = GetSelectedNeuron();
```

다만 `var`를 사용하는 것이 오히려 가독성을 높이는 경우에는 사용할 수 있다.

예:

```csharp
var activationValues = new List<float>();
var neuron = GetComponent<NeuronController>();
```

오른쪽 표현만으로 타입이 매우 명확하거나 타입 이름이 지나치게 길어 코드 가독성을 떨어뜨리는 경우 `var` 사용을 허용한다.

익명 타입처럼 명시적 타입 작성이 불가능한 경우에는 `var`를 사용한다.

```csharp
var result = neurons.Select(neuron => new
{
    neuron.name,
    neuron.ActivationValue
});
```

따라서 다음을 기준으로 판단한다.

```text
타입을 명시했을 때 더 읽기 쉬움
→ 명시적 타입 사용

var를 사용했을 때 더 읽기 쉬움
→ var 사용 가능
```

단순히 타이핑을 줄이기 위한 목적으로 무조건 `var`를 사용하지 않는다.

---

# 3. Naming Convention

## 3.1 클래스

`PascalCase`

```csharp
public class ConvolutionController
public class NeuralNetworkVisualizer
public class PixelInteractionHandler
```

가능하면 클래스가 담당하는 역할이 이름에 드러나도록 한다.

```csharp
CNNTrainingController
NeuronInteractionController
FeatureMapVisualizer
```

단순히 `Manager`, `Controller`, `System`처럼 지나치게 추상적인 이름만 사용하는 것은 피한다.

## 3.2 인터페이스

`I + PascalCase`

```csharp
public interface IInteractable
public interface IResettable
public interface IDataProvider
```

## 3.3 메서드

`PascalCase`

가능하면 동사 또는 동사구를 사용한다.

```csharp
public void StartTraining()
private void UpdateFeatureMap()
private float CalculateActivationValue()
```

무엇을 처리하는지 알기 어려운 이름은 피한다.

```csharp
Process()
Run()
Execute()
```

대신 구체적인 역할을 표현한다.

```csharp
ProcessConvolution()
RunInference()
ExecuteTrainingStep()
```

## 3.4 private 필드

`_camelCase`

```csharp
private int _currentLayerIndex;
private GameObject _selectedNeuron;
private float _movementSpeed;
```

Unity Inspector에 노출되는 private 필드도 동일하게 작성한다.

```csharp
[SerializeField] private GameObject _neuronPrefab;
[SerializeField] private float _interactionDistance;
```

## 3.5 지역 변수

`camelCase`

```csharp
float activationValue;
int neuronIndex;
Vector3 targetPosition;
```

## 3.6 매개변수

`camelCase`

```csharp
private void HighlightNeuron(GameObject targetNeuron, float intensity)
```

## 3.7 Property

`PascalCase`

```csharp
public int CurrentLayerIndex { get; private set; }
public bool IsTraining { get; private set; }
public float ActivationValue { get; set; }
```

public field를 직접 노출하는 것보다 Property 사용을 우선한다.

```csharp
public float ActivationValue { get; private set; }
```

단, Unity 또는 사용하는 라이브러리의 구조상 public field가 더 자연스러운 경우에는 예외를 허용한다.

## 3.8 상수

`PascalCase`

```csharp
private const int MaxLayerCount = 10;
private const float DefaultInteractionDistance = 2.0f;
```

## 3.9 bool

가능하면 상태를 자연스럽게 읽을 수 있도록 작성한다.

```csharp
bool isTraining;
bool isSelected;
bool hasCompletedMission;
bool canInteract;
bool shouldHighlight;
```

`is`, `has`, `can`, `should` 등의 접두사를 권장한다.

다만 이름 자체가 충분히 명확하다면 반드시 특정 접두사를 강제하지 않는다.

## 3.10 Collection

가능하면 복수형을 사용한다.

```csharp
List<GameObject> neurons;
GameObject[] featureMaps;
Dictionary<int, float> activationValues;
```

## 3.11 Enum

Enum 이름과 값은 `PascalCase`를 사용한다.

```csharp
public enum ActivationType
{
    ReLU,
    Sigmoid,
    Tanh
}
```

---

# 4. 파일 이름

C# 파일명은 대표 클래스 이름과 동일하게 작성하는 것을 기본으로 한다.

```text
ConvolutionController.cs
```

```csharp
public class ConvolutionController : MonoBehaviour
{
}
```

가능하면 하나의 `.cs` 파일에는 하나의 주요 클래스를 둔다.

다만 매우 작은 보조 타입이나 해당 클래스와 강하게 결합된 내부 타입까지 반드시 별도 파일로 분리할 필요는 없다.

---

# 5. Namespace

프로젝트 코드는 기능 영역의 이름을 namespace로 사용하는 것을 기본으로 한다. 프로젝트 이름을 앞에 붙이지 않는다.

예:

```csharp
namespace NeuralNetwork
{
    public class ConvolutionController : MonoBehaviour
    {
    }
}
```

기능 구분이 필요할 때만 하위 namespace를 사용할 수 있다.

```text
Core
Networking
XR
UI
NeuralNetwork
NeuralNetwork.MLP
NeuralNetwork.CNN
Mission
Audio
```

불필요하게 너무 세분화된 namespace 구조는 만들지 않는다.

---

# 6. Unity Inspector 변수

Inspector에서 설정해야 하는 값은 가능한 경우 다음 형태를 사용한다.

```csharp
[SerializeField] private GameObject _neuronPrefab;
[SerializeField] private float _movementSpeed;
```

Inspector에 노출하기 위해 단순히 `public`으로 선언하는 것은 피한다.

외부 코드에서 읽어야 하는 경우 Property를 사용할 수 있다.

```csharp
[SerializeField] private float _activationValue;

public float ActivationValue => _activationValue;
```

단, 사용 중인 Unity API나 외부 라이브러리의 요구사항 때문에 public field가 더 적절하다면 사용할 수 있다.

---

# 7. Inspector 구성

관련 필드가 많아지는 경우 `Header` 등을 사용해 Inspector를 정리한다.

```csharp
[Header("Neuron Settings")]
[SerializeField] private GameObject _neuronPrefab;
[SerializeField] private float _neuronScale;

[Header("Interaction Settings")]
[SerializeField] private float _interactionDistance;
[SerializeField] private LayerMask _interactionLayer;
```

단, 필드가 몇 개 없는 클래스까지 불필요하게 Attribute를 추가하지 않는다.

---

# 8. Unity 생명주기 메서드 순서

MonoBehaviour 내부에서는 가능하면 다음 순서를 따른다.

```text
1. Constants
2. Serialized Fields
3. Private Fields
4. Properties
5. Awake
6. OnEnable
7. Start
8. Update / FixedUpdate / LateUpdate
9. Public Methods
10. Private Methods
11. Event Handlers
12. OnDisable
13. OnDestroy
```

이는 코드 탐색을 쉽게 하기 위한 기본 순서다.

특정 클래스에서 다른 순서가 코드 흐름을 더 쉽게 이해하도록 해준다면 일부 조정할 수 있다.

---

# 9. Formatting

BSD / Allman 스타일을 사용한다.

여는 중괄호는 새로운 줄에 작성한다.

```csharp
if (isSelected)
{
    Highlight();
}

public void StartTraining()
{
    InitializeNetwork();
}

public class NeuronController : MonoBehaviour
{
}
```

한 줄 조건문이라도 기본적으로 중괄호를 작성한다.

```csharp
if (isSelected)
{
    Highlight();
}
```

Indent는 Space 4칸을 사용한다.

Formatting은 프로젝트 전체에서 일관성을 유지하는 것을 가장 중요하게 본다.

---

# 10. Update 계열 메서드

`Update()`, `FixedUpdate()`, `LateUpdate()`에는 가능한 한 복잡한 로직을 직접 작성하지 않는다.

```csharp
private void Update()
{
    UpdateHandInteraction();
    UpdateNeuronHighlight();
}
```

이는 반드시 메서드를 잘게 분리하라는 의미는 아니다.

오히려 지나친 메서드 분리가 코드 흐름을 이해하기 어렵게 만든다면 간단한 로직은 Update 안에 직접 작성할 수 있다.

핵심은 매 프레임 실행되는 작업의 비용과 역할을 쉽게 파악할 수 있도록 만드는 것이다.

---

# 11. 반복적인 객체 탐색 최소화

다음 작업을 매 프레임 반복하는 것은 가능한 한 피한다.

```csharp
GameObject.Find(...)
FindFirstObjectByType<T>()
GetComponent<T>()
```

반복해서 사용하는 참조는 일반적으로 한 번 찾아 저장한다.

```csharp
private Renderer _targetRenderer;

private void Awake()
{
    _targetRenderer = GetComponent<Renderer>();
}
```

또는 Inspector에서 할당한다.

```csharp
[SerializeField] private Renderer _targetRenderer;
```

다만 일회성 코드나 실행 빈도가 매우 낮고 성능 영향이 없는 경우까지 억지로 캐싱할 필요는 없다.

성능 최적화를 위해 오히려 구조가 복잡해지는 상황은 피한다.

---

# 12. 불필요한 메모리 할당 최소화

Meta Quest는 PC보다 제한된 하드웨어 환경에서 동작하므로 자주 반복되는 코드에서 불필요한 할당을 줄인다.

특히 매 프레임 반복되는 다음 작업에 주의한다.

```text
new List<T>()
new GameObject()
Instantiate(...)
LINQ
문자열 생성
```

반복적으로 생성되고 제거되는 GameObject가 많다면 Object Pooling을 고려한다.

단, 사용 빈도가 매우 낮거나 데이터 크기가 작은 코드까지 성능을 이유로 복잡하게 최적화하지 않는다.

먼저 가독성 있는 코드를 작성하고, 실제 성능 문제가 확인되는 부분을 중심으로 최적화한다.

---

# 13. Magic Number 최소화

의미를 이해하기 어려운 숫자는 직접 작성하지 않는 것을 기본으로 한다.

```csharp
if (distance < 1.5f)
{
}
```

의미가 있는 값이라면 이름을 부여한다.

```csharp
[SerializeField] private float _interactionDistance = 1.5f;
```

또는:

```csharp
private const float MinimumActivationValue = 0.01f;
```

다만 다음처럼 의미가 너무 명확한 숫자까지 모두 상수로 만들 필요는 없다.

```csharp
for (int i = 0; i < 3; i++)
{
}
```

숫자에 이름을 붙이는 것이 실제로 의미 전달에 도움이 되는지를 기준으로 판단한다.

---

# 14. 중복 코드 최소화

동일한 로직이 여러 곳에서 반복되고, 함께 변경될 가능성이 높다면 공통화를 고려한다.

```csharp
private void ResetNeuron()
{
    ...
}
```

그러나 단순히 코드 몇 줄이 비슷하다는 이유만으로 무조건 공통화하지 않는다.

다음과 같은 경우 중복을 허용할 수 있다.

- 서로 다른 기능이 우연히 비슷한 구현을 가지는 경우
- 향후 서로 다른 방향으로 변경될 가능성이 높은 경우
- 공통화하면 코드의 의미가 오히려 불명확해지는 경우
- 추상화를 위해 복잡한 상속이나 인터페이스 구조가 필요한 경우
- 매우 짧고 단순한 코드인 경우

예:

```csharp
private void EnableNeuron()
{
    _renderer.enabled = true;
    _collider.enabled = true;
}

private void EnableFeatureMap()
{
    _renderer.enabled = true;
    _collider.enabled = true;
}
```

현재 구현이 같더라도 두 기능의 의미가 다르고 앞으로 다르게 변경될 가능성이 있다면 반드시 합칠 필요는 없다.

중복 제거 자체를 목표로 하지 않는다.

**가독성과 유지보수성이 실제로 좋아지는 경우에만 공통화한다.**

---

# 15. 클래스 책임

가능하면 하나의 클래스에는 하나의 주요 책임을 둔다.

예:

```text
NeuronInteractionController
→ 뉴런과 사용자 상호작용

NeuronVisualizer
→ 뉴런 시각 표현

CNNModelController
→ CNN 모델 상태 관리

MissionManager
→ 미션 진행 관리
```

하나의 클래스가 지나치게 많은 역할을 담당하는 구조는 피한다.

예:

```text
GameManager

- UI
- Audio
- CNN 계산
- XR 입력
- Mission
- Animation
- Data 저장
```

다만 단일 책임 원칙을 지나치게 적용해 작은 클래스 수십 개로 분리하지 않는다.

클래스를 분리했을 때 책임과 흐름이 더 명확해지는지를 기준으로 판단한다.

---

# 16. 메서드 크기

하나의 메서드가 너무 많은 작업을 수행하면 분리를 고려한다.

```csharp
private void CompleteConvolutionStep()
{
    CalculateConvolution();
    UpdateFeatureMap();
    PlayCompletionEffect();
    UpdateMissionProgress();
}
```

단순히 "메서드는 몇 줄 이하"와 같은 기준은 사용하지 않는다.

짧은 메서드 여러 개를 따라다니는 것보다 하나의 명확한 메서드가 읽기 쉬운 경우에는 그대로 둘 수 있다.

메서드 길이보다 **하나의 명확한 목적을 가지고 있는지**를 중요하게 본다.

---

# 17. 조건문

조건문 중첩이 지나치게 깊어지는 경우 Early Return을 고려한다.

```csharp
if (target == null)
{
    return;
}

if (!target.IsActive)
{
    return;
}

if (!canInteract)
{
    return;
}

Interact();
```

하지만 무조건 Early Return을 사용하는 것은 아니다.

간단한 `if / else`가 논리 흐름을 더 자연스럽게 보여준다면 그대로 사용한다.

목적은 중첩 제거 자체가 아니라 조건의 흐름을 쉽게 이해하게 만드는 것이다.

---

# 18. Null 처리

Null 가능성이 있는 참조는 적절히 확인한다.

```csharp
if (_targetNeuron == null)
{
    return;
}
```

필수 Inspector Reference가 누락된 경우 개발 중 쉽게 발견할 수 있도록 오류를 표시할 수 있다.

```csharp
private void Awake()
{
    if (_targetRenderer == null)
    {
        Debug.LogError(
            $"{nameof(NeuronController)}: Target Renderer is not assigned.",
            this
        );

        return;
    }
}
```

모든 참조에 습관적으로 Null Check를 추가하지 않는다.

구조상 반드시 존재해야 하는 객체라면 오히려 빠르게 오류를 발견하는 것이 더 나을 수 있다.

---

# 19. 주석

주석은 가능한 한 코드가 **왜 이렇게 작성되었는지**를 설명한다.

```csharp
// 이전 미션의 활성화 값이 다음 실습에 영향을 주지 않도록 초기화한다.
activationValue = 0.0f;
```

코드를 그대로 읽어주는 주석은 피한다.

```csharp
// activationValue를 0으로 설정한다.
activationValue = 0.0f;
```

CNN, MLP, 역전파, 행렬 연산 등 구현 의도를 파악하기 어려운 부분에는 필요한 설명을 작성한다.

다만 코드 자체가 명확하다면 주석을 억지로 추가하지 않는다.

---

# 20. Debug.Log

임시 디버깅 로그는 기능 구현 후 정리한다.

```csharp
Debug.Log("test");
Debug.Log("123");
Debug.Log("여기 들어옴");
```

유지할 필요가 있는 로그에는 관련 시스템을 식별할 수 있는 정보를 포함하는 것을 권장한다.

```csharp
Debug.Log($"[CNN] Convolution step completed: {currentStep}");
```

개발 단계에서 유용한 Debug.Log까지 무조건 제거할 필요는 없다.

다만 반복적으로 호출되어 Console을 과도하게 채우거나 성능에 영향을 주는 로그는 피한다.

---

# 21. Event 이름

이벤트 이름은 어떤 일이 발생했는지 명확하게 나타낸다.

```csharp
public event Action OnTrainingStarted;
public event Action OnTrainingCompleted;
public event Action<int> OnLayerSelected;
```

이벤트 처리 메서드는 다음처럼 작성할 수 있다.

```csharp
private void HandleTrainingCompleted()
{
}
```

`Handle` 사용을 권장하지만 반드시 강제하지 않는다.

이벤트와 처리 메서드의 관계가 명확하면 다른 자연스러운 이름도 사용할 수 있다.

---

# 22. Coroutine

Coroutine의 동작을 알 수 있는 이름을 사용한다.

```csharp
private IEnumerator PlayNeuronHighlightSequence()
private IEnumerator WaitForConvolutionCompletion()
```

다음처럼 의미가 지나치게 모호한 이름은 피한다.

```csharp
IEnumerator Routine()
IEnumerator DoSomething()
```

Coroutine이라는 이유만으로 이름에 반드시 `Coroutine`을 붙일 필요는 없다.

---

# 23. 비동기 코드

일반적인 비동기 메서드는 `Task`를 반환하는 것을 우선한다.

```csharp
private async Task LoadModelAsync()
{
}
```

비동기 메서드는 가능하면 `Async` 접미사를 사용한다.

```csharp
LoadModelAsync()
InitializeNetworkAsync()
```

`async void`는 오류 처리와 호출 추적이 어려우므로 일반 메서드에서는 피한다.

다만 이벤트 핸들러 등 `void` 반환이 자연스러운 상황에서는 사용할 수 있다.

---

# 24. 접근 제한자

접근 제한자는 가능한 한 명시적으로 작성한다.

```csharp
private void Initialize()
public void StartMission()
```

필요 이상으로 넓은 접근 범위를 사용하지 않는다.

기본적으로 필요한 최소 범위를 선택한다.

```text
private
→ protected / internal
→ public
```

다만 접근 제한자를 줄이기 위해 구조가 지나치게 복잡해지는 것은 피한다.

---

# 25. `using`

사용하지 않는 `using`은 정리한다.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
```

파일 내부에서 전체 namespace를 반복해서 작성하기보다는 필요한 `using`을 사용하는 것을 기본으로 한다.

단, 타입 이름 충돌을 방지하기 위해 전체 namespace를 명시하는 것이 더 명확한 경우에는 사용할 수 있다.

---

# 26. Scene Object 의존성 최소화

가능하면 GameObject 이름 문자열에 직접 의존하는 구조를 피한다.

```csharp
GameObject.Find("Player");
GameObject.Find("CNN_Object");
```

대신 Inspector Reference나 Component Reference를 사용하는 것을 우선한다.

다만 초기화 과정에서 한 번만 사용하는 간단한 탐색까지 무조건 금지하지 않는다.

Scene 규모, 호출 빈도, 코드 복잡도를 함께 고려한다.

---

# 27. Singleton 및 전역 상태

Singleton과 static 전역 상태는 필요할 때만 사용한다.

게임 전체에서 실제로 하나만 존재해야 하고 여러 시스템에서 접근할 필요가 있는 경우에는 적절할 수 있다.

하지만 단순히 접근하기 편하다는 이유만으로 모든 Manager를 Singleton으로 만들지 않는다.

```text
편의성 때문에 Singleton 사용
→ 지양

명확한 전역 책임이 존재
→ 사용 가능
```

---

# 28. 과도한 추상화 방지

다음과 같은 구조를 단순히 "확장성"을 이유로 미리 만들지 않는다.

```text
Interface
→ Abstract Class
→ Base Class
→ Manager
→ Factory
→ Controller
```

현재 프로젝트에서 실제로 필요한 구조까지만 구현한다.

미래에 필요할지도 모른다는 이유만으로 복잡성을 추가하지 않는다.

필요해졌을 때 리팩터링할 수 있도록 코드를 명확하게 작성하는 것을 우선한다.

---

# 29. SerializedField 이름 변경

기존 `[SerializeField]` 필드 이름 변경은 주의한다.

Unity Scene이나 Prefab에 저장된 연결과 데이터에 영향을 줄 수 있으므로 단순한 스타일 개선 목적으로 이름을 임의로 변경하지 않는다.

AI 역시 기존 SerializedField의 이름을 별도 요청 없이 변경하지 않는다.

필요한 경우 Unity의 기존 데이터와의 호환성을 고려한다.

---

# 30. AI 코드 작성 규칙

GPT, Claude 등의 AI가 코드를 생성하거나 수정할 때도 본 컨벤션을 적용한다.

AI는 가능하면 새로운 코드를 작성하기 전에 다음을 확인한다.

- 기존 클래스 구조
- 기존 namespace
- 관련 컴포넌트
- 비슷한 기능이 이미 구현되어 있는지
- 기존 public API
- Scene / Prefab과 연결된 SerializedField

AI는 특별한 이유 없이 다음 작업을 하지 않는다.

- 기존 public API 이름 변경
- 기존 클래스 이름 변경
- namespace 변경
- SerializedField 이름 변경
- Prefab / Scene 연결을 깨뜨릴 수 있는 변경
- 기존 기능의 중복 구현
- 사용 중인 XR 프레임워크 변경
- 대규모 리팩터링
- 불필요한 디자인 패턴 도입

AI는 가능한 한 **요청된 범위만 수정한다.**

---

# 31. AI가 규칙을 해석하는 방법

AI는 본 문서의 규칙을 기계적으로 적용하지 않는다.

예를 들어 다음과 같이 판단한다.

```text
명시적 타입 때문에 코드가 더 복잡해진다.
→ var 사용 가능

중복 코드 몇 줄을 없애기 위해 복잡한 추상화가 필요하다.
→ 중복 유지 가능

GetComponent를 한 번 호출하는 것보다 캐싱 구조를 만드는 것이 복잡하다.
→ 직접 호출 가능

메서드를 분리했더니 코드 흐름이 더 이해하기 어렵다.
→ 하나의 메서드 유지 가능
```

규칙 적용 여부가 애매하다면 다음 질문을 기준으로 판단한다.

> "이 선택이 다음 사람이 코드를 이해하고 수정하기 쉽게 만드는가?"

그렇다면 규칙의 예외를 허용할 수 있다.

---

# 32. Merge 전 확인

개발 중에는 작업 내용을 작은 단위로 자유롭게 Commit할 수 있다.

매 Commit마다 전체 체크리스트를 확인하도록 강제하지 않는다.

다만 다른 팀원의 작업을 방해할 정도로 명백하게 문제가 있는 상태의 코드는 가능한 한 공유 브랜치에 올리지 않는다.

`main`, `develop` 등 팀의 공용 브랜치에 Merge하기 전에는 다음 항목을 확인한다.

본인이 직접 제작한 작은 브랜치에까지 적용할 필요는 없다.

- Compile Error가 없는가?
- 실행 중 치명적인 Console Error가 발생하지 않는가?
- 임시 코드가 불필요하게 남아 있지 않은가?
- 테스트용 Debug.Log가 불필요하게 남아 있지 않은가?
- Naming Convention이 대체로 일관적인가?
- Inspector Reference가 정상적으로 연결되어 있는가?
- Scene 또는 Prefab을 의도치 않게 수정하지 않았는가?
- 기존 기능이나 다른 팀원의 작업에 영향을 주지 않는가?
- 불필요한 대규모 리팩터링이 포함되어 있지 않은가?
- 코드가 규칙을 따르기 위해 오히려 복잡해지지는 않았는가?

Merge 전 검토는 모든 사소한 문제를 완벽하게 제거하는 것이 목적이 아니다.

**공용 브랜치의 안정성을 유지하고 다른 팀원이 해당 코드를 이어서 작업할 수 있는 상태인지 확인하는 것을 목적으로 한다.**

---

# 핵심 규칙 요약

```text
Class                 → PascalCase
Interface             → IPascalCase
Method                → PascalCase
Property              → PascalCase
private Field         → _camelCase
[SerializeField]      → _camelCase
Local Variable        → camelCase
Parameter             → camelCase
Constant              → PascalCase
Enum                  → PascalCase
C# File               → 대표 Class 이름과 일치 권장

Formatting            → BSD / Allman
Indent                → Space 4칸

var                    → 명시적 타입 우선, 더 읽기 쉬우면 허용
public field           → Property / SerializeField 우선
GameObject.Find        → 반복 사용 최소화
GetComponent           → 반복 호출 최소화
Update 내 할당         → 최소화
Magic Number           → 의미가 있을 때 이름 부여
중복 코드              → 무조건 제거하지 않음
Singleton              → 필요한 경우에만 사용
추상화                 → 실제 필요할 때만 적용
```

---

# 가장 중요한 원칙

코딩 규칙은 코드를 더 쉽게 이해하기 위해 존재한다.

**코딩 규칙 때문에 코드가 더 어렵고 복잡해진다면 그 규칙을 적용하지 않아도 된다.**

일관성을 중요하게 유지하되, 가독성·명확성·유지보수성을 위해 합리적인 예외를 허용한다.

팀원과 AI 모두 규칙 자체보다 코드의 의도와 이해도를 우선한다.
