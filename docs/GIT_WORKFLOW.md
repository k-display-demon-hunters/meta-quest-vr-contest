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
Squash Merge
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

작업 브랜치의 여러 commit은 최종적으로 Squash Merge를 통해 하나의 commit으로 `main`에 반영한다.

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

## 7. Squash Merge

Pull Request는 기본적으로 Squash Merge를 사용한다.

Squash Merge는 작업 브랜치의 여러 commit을 하나의 commit으로 합쳐 `main`에 반영하는 방식이다.

예:

```text
feature/cnn-filter-ui

A ─ UI 추가
    ─ 위치 수정
    ─ 상호작용 수정
    ─ 정리

        ↓ Squash Merge

main

A ─ CNN 필터 UI 구현
```

이를 통해 작업 과정에서 발생한 세부적인 수정 commit은 작업 브랜치에 남기고, `main`의 history는 작업 단위로 단순하게 유지한다.

---

## 8. Branch Cleanup

`main`에 정상적으로 병합된 작업 브랜치는 삭제한다.

```text
feature/cnn-filter-ui
        ↓
       PR
        ↓
  Squash Merge
        ↓
      main
        ↓
작업 브랜치 삭제
```

브랜치는 임시 작업 공간으로 사용하며, 과거 변경 내용은 Git history와 Pull Request 기록을 통해 확인한다.

---

## 9. Stable Versions

시연, 중간 완성본, 최종 제출 등 중요한 시점에는 **Git Tag**를 생성한다.

예:

```text
main

A ─ B ─ C ─ D ─ E
        ↑       ↑
   demo-ready  submission
```

```bash
git tag demo-ready
git push origin demo-ready
```

Tag를 통해 이후 개발이 진행되어도 중요한 시점의 상태를 다시 확인하거나 해당 시점에서 새 브랜치를 만들 수 있다.

---

## 10. Unity Scene / Prefab

Unity의 Scene과 Prefab은 여러 사람이 동시에 수정할 경우 충돌이 발생하기 쉽다.

따라서 다음 원칙을 따른다.

- 같은 Scene을 여러 사람이 동시에 수정하는 것을 가능한 한 피한다.
- Scene 작업을 시작하기 전에 팀원에게 공유한다.
- 재사용 가능한 오브젝트는 가능한 한 Prefab으로 분리한다.
- 서로 다른 기능은 가능한 한 서로 다른 Prefab에서 작업한다.
- Scene / Prefab을 수정하는 작업 브랜치는 오래 유지하지 않는다.
- Scene / Prefab을 수정한 경우 Pull Request에 해당 내용을 적는다.

가능하면 하나의 Scene 파일에 모든 팀원의 작업이 집중되지 않도록 구성한다.

---

## 11. Asset Management

Unity 프로젝트 실행에 필요한 Asset은 Git으로 관리한다.

Git에 포함:

```text
Assets/
Packages/
ProjectSettings/
*.meta
```

Git에서 제외:

```text
Library/
Temp/
Logs/
obj/
Build/
```

대용량 3D 모델, 이미지 원본, 오디오, 영상 등의 바이너리 파일은 필요에 따라 Git LFS를 사용한다.

`.meta` 파일은 Unity Asset 참조에 필요하므로 반드시 함께 commit한다.

---

## 12. Core Principle

**작은 단위로 작업하고, 작업 브랜치를 짧게 유지하며, 자주 `main`에 통합한다.**

```text
작은 작업
   ↓
짧은 브랜치
   ↓
Pull Request
   ↓
리뷰
   ↓
Squash Merge
   ↓
main
```
