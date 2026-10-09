# Git Workflow

## 1. Branch Strategy

본 프로젝트는 Trunk-Based Development를 기반으로 한다.

- `main`을 유일한 장기 브랜치로 사용한다.
- 모든 작업은 `main`에서 분기한 작업 브랜치에서 수행한다.
- 작업 브랜치는 작은 작업 단위로 만들고 가능한 한 짧게 유지한다.
- 작업이 완료되면 Pull Request를 생성하고 `main`에 병합한다.
- `main`에는 직접 push하지 않는다.

기본 흐름:

```text
main
  ↓
작업 브랜치 생성
  ↓
작업 및 commit
  ↓
push
  ↓
Pull Request
  ↓
리뷰
  ↓
Merge Commit
  ↓
작업 브랜치 삭제
```

---

## 2. Branch Naming

브랜치 이름은 다음 형식을 사용한다.

```text
<type>/<short-description>
```

주요 type:

- `feature/` : 새로운 기능
- `fix/` : 버그 수정
- `refactor/` : 기능 변화 없는 코드 구조 개선

예:

```text
feature/cnn-filter-ui
feature/mlp-node-interaction
fix/quest-controller-input
refactor/network-client
```

브랜치 이름은 하나의 작업을 나타내도록 구체적으로 작성한다.

---

## 3. Working Branches

작업 시작 전 최신 `main`을 기준으로 브랜치를 생성한다.

```bash
git switch main
git pull
git switch -c feature/cnn-filter-ui
```

하나의 브랜치에서 지나치게 많은 기능을 구현하지 않는다.

예를 들어:

```text
feature/cnn
```

처럼 큰 단위로 오래 작업하기보다,

```text
feature/cnn-filter-ui
feature/cnn-filter-drag
feature/convolution-popup
```

처럼 가능한 한 독립적인 작업 단위로 나눈다.

---

## 4. Commits

Commit은 **의미 있는 변경 단위**로 작성한다.

- 서로 관련 없는 변경사항을 하나의 commit에 섞지 않는다.
- 무엇을 변경했는지 알아볼 수 있는 메시지를 작성한다.
- 작은 수정 commit이 여러 개 생기는 것은 허용한다.
- commit history를 지나치게 깔끔하게 만들기 위해 개발 흐름을 방해하지 않는다.

작업 브랜치의 commit은 일반 Merge로 이력을 유지한 채 `main`에 반영한다.

---

## 5. Pull Requests

작업이 완료되면 `main`을 대상으로 Pull Request를 생성한다.

Pull Request에는 최소한 다음 내용을 알아볼 수 있도록 작성한다.

- 무엇을 변경했는지
- 확인이 필요한 부분이 있는지
- Scene이나 Prefab 등 충돌 가능성이 있는 파일을 수정했는지

Pull Request는 가능한 한 하나의 작업 단위를 다룬다.

---

## 6. Review

Pull Request는 다른 팀원 한 명 이상이 확인한 후 병합한다.

주요 확인 항목:

- 프로젝트가 정상적으로 동작하는가
- 기존 기능에 문제가 생기지 않는가
- 불필요한 파일이 포함되지 않았는가
- 명백한 코드 문제가 없는가
- Scene / Prefab 충돌 가능성이 없는가

리뷰 절차는 프로젝트 진행을 방해하지 않는 범위에서 간단하게 유지한다.

---

## 7. 일반 Merge

Pull Request는 일반 Merge를 사용한다. GitHub에서는 **Create a merge commit**을 선택하고, Squash Merge는 사용하지 않는다.

일반 Merge는 작업 브랜치의 개별 commit을 보존하고, 병합 commit을 생성해 `main`에 반영하는 방식이다.

예:

```text
main:                   A ───────────── M
                         ╲             ╱
feature/cnn-filter-ui:     B ─ C ─ D ─ E

B~E: 작업 브랜치의 개별 commit
M: 병합 commit
```

이를 통해 작업 과정의 개별 commit과 브랜치 병합 이력을 `main`에서 확인할 수 있다.
