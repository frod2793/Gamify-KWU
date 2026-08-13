# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

광운대학교 "게임화개론" 팀 프로젝트. 캠퍼스 로비 맵(`Lobby` 씬)에서 포탈로 미니게임 4종(`CraneGame` 인형뽑기, `GradeRunner` 학점 피하기, `TimingCatch`, `CardMatch`)에 진입하고, 결과가 학점으로 누적되어 최종 성적표 엔딩으로 이어지는 2D 모바일 게임.

- Unity **6000.3.19f1** (Unity 6.3) + URP. Unity Editor를 통해 열고 빌드한다 (CLI 빌드 스크립트 없음).
- 핵심 의존성: VContainer(DI), UniTask, DOTween, Cinemachine, Input System, SPUM, VirtualJoystick, EasyTransitions.
- 응답·디버그 로그·커밋 메시지는 **한국어**로 작성한다.

## 에이전트 하네스 (필독)

`.agents/AGENTS.md`가 이 프로젝트의 에이전트 규칙 진입점이다. 코드 작업 전에 반드시 읽는다. 코딩 표준 상세는 `.agents/docs/coding-standards/`, 프로젝트 프로필은 `.agents/config/project-profile.conf`에 있다.

주요 명령:

```bash
# 프로필 감지 / 확정
bash .agents/scripts/init/initialize-harness.sh --project-root .
bash .agents/scripts/init/initialize-harness.sh --project-root . --confirm

# 하네스 계약 테스트
bash .agents/tests/run-tests.sh

# 2D 모바일 최적화 정적 스캔
bash .agents/scripts/optimization/run-scan.sh .
```

하네스 운영 원칙: 자동 commit/push/PR 금지, `not_run`을 통과로 처리하지 않음, 서드파티 원본(Assets/SPUM, Assets/Plugins 등) 수정 금지.

### 에이전트 역할 배정

`.agents/AGENTS.md`의 역할 분리 정책(메인=계획·리뷰, 서브=구현)에 따라 역할별 사용 에이전트를 다음과 같이 고정한다:

| 역할 | 에이전트 / 모델 | reasoning |
|---|---|---|
| 계획 (Plan) | Claude Opus | — |
| 구현 (Implementation) | Codex terra (`gpt-5.6-terra`) | medium |
| 검수 (Review) | Codex sol | medium |

## 테스트

Unity Test Framework 사용. Unity Editor의 Test Runner 창에서 실행하거나 CLI로:

```bash
# EditMode 테스트 (Assets/_Game/Tests/Editor → GamifyKWU.Tests.Editor.asmdef)
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -logFile -

# PlayMode 테스트 (Assets/_Game/Tests/PlayMode → GamifyKWU.Tests.PlayMode.asmdef)
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -logFile -

# 단일 테스트: -testFilter "클래스명 또는 네임스페이스.클래스명.메서드명"
```

게임 로직은 MonoBehaviour 밖 순수 C#으로 분리되어 있으므로 새 로직의 단위 테스트는 EditMode에 둔다.

## 아키텍처

`Assets/_Game/Scripts` 아래 기능별 폴더가 각각 `Models / ViewModels / Views (+ DTOs, Presenters, Data)` 하위 구조를 반복하는 **엄격한 MVVM**:

- **Model**: 순수 C# POCO. Unity API 의존 금지.
- **ViewModel**: 상태 + Command. `Action` 이벤트로 View에 전파.
- **View**: MonoBehaviour. 입력 전달과 렌더링만 담당.
- 일부 영역(TimingCatch, GradeRunner)은 View↔ViewModel 사이에 **Presenter**를 추가로 둔다.

씬 단위 조립·흐름:

- **DI**: 씬마다 VContainer `LifetimeScope`가 조립 루트 (`LobbyLifetimeScope`, `ClawGameLifetimeScope` 등). 새 의존성은 여기에 등록한다.
- **FlowController**: 씬별 게임 루프 오케스트레이터 (`LobbyFlowController` 등).
- **씬 간 데이터**: 싱글톤 금지 (`static Instance` 패턴 0건 유지). DTO(`*ResultDTO`, `*ContextDTO`)와 `PlayerSO`(위치·미니게임 등급·플레이타임 영구 저장, `Assets/_Game/ScriptableObjects/Player/`)로 명시적으로 전달한다.
- **설정 데이터**: 게임 밸런스·퀴즈·대사는 ScriptableObject (`Assets/_Game/ScriptableObjects/`).
- **비동기**: 코루틴 대신 UniTask, 연출은 DOTween.

주의: 네임스페이스가 `GameArifiction.*`(다수, 오타지만 표준)와 `GamifyKWU.*`(소수)로 혼재한다. 수정하는 영역의 기존 네임스페이스를 따른다.

## 코딩 규약 (README.md에 명문화, 상세는 README 참조)

- Private 필드: `m_` + camelCase. Inspector 할당용 이벤트 핸들러: `func_` 접두사 (예: `func_OnExitButtonClicked`).
- Allman 스타일. 단일 실행문 `if`에도 중괄호 필수.
- 고주기 루프에서 `foreach` 대신 인덱스 `for`.
- **`UnityEngine.Object` 파생 객체에 `?.`/`??` 금지** — 반드시 `if (obj != null)` 명시 비교 (Unity fake null).
- 디버그 로그: 한국어, `Debug.Log($"[클래스명] 상황 묘사: {변수}");` 형식.
