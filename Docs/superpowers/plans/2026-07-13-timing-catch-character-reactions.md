# 타이밍 캐치 캐릭터 성공·실패 연출 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 타이밍 캐치의 매 스테이지 판정 직후 로비와 동일한 SPUM 캐릭터가 성공 점프 또는 피격 반응을 재생하고 안전하게 IDLE로 복귀하도록 구현한다.

**Architecture:** 기존 `TimingCatchGameViewModel.OnJudgeEvaluated`를 판정 이벤트 소스 인터페이스로 노출하고, 순수 C# `TimingCatchCharacterPresenter`가 판정을 `ITimingCatchCharacterView` 명령으로 변환한다. Unity 표현 계층인 `TimingCatchCharacterView`만 SPUM, AnimationClip, Hit Effect와 취소 토큰을 다루며, 성공 클립은 원본 피격 클립의 바인딩 경로를 복제·재키잉해 프로젝트 영역에 저장한다.

**Tech Stack:** Unity 6.3 `6000.3.16f1`, C# 9, VContainer, UniTask, SPUM, NUnit, Unity Test Framework

## 전역 제약

- 최종 답변, 코드 주석, XML 문서, 로그는 한국어로 작성한다.
- 신규 코드의 작성자는 반드시 `윤승종`으로 표기한다.
- 싱글톤을 추가하지 않는다.
- View는 Model을 직접 참조하지 않고 ViewModel 이벤트도 Presenter를 통해서만 받는다.
- View는 `RegisterComponentInHierarchy<T>()`로 등록하고 LifetimeScope의 SerializeField로 받지 않는다.
- UnityEngine.Object에는 `?.`, `??`를 사용하지 않는다.
- 모든 제어문에 Allman Style 중괄호를 사용한다.
- private 필드는 `m_` 접두사를 사용한다.
- 모든 신규 클래스와 메서드에 지정 형식의 XML 문서를 작성한다.
- SPUM과 다른 서드파티 원본 파일을 수정하지 않는다.
- 기존 주석과 `#region`을 보존한다.
- `Update`, `LateUpdate`, `FixedUpdate`에 할당, LINQ, Boxing, `foreach`를 추가하지 않는다.
- 비동기 반응 복귀는 `async UniTaskVoid`와 `CancellationToken`을 사용한다.
- 현재 작업 트리의 기존 사용자 변경은 보존하며, 각 커밋은 명시된 파일만 경로 지정해 스테이징한다.

---

## 파일 구조

### 신규 파일

- `Assets/_Game/Scripts/TimingCatch/Presenters/ITimingCatchJudgeEventSource.cs`: Presenter가 구독할 판정 이벤트 계약
- `Assets/_Game/Scripts/TimingCatch/Views/ITimingCatchCharacterView.cs`: 성공·실패·초기화 반응 View 계약
- `Assets/_Game/Scripts/TimingCatch/Presenters/TimingCatchCharacterPresenter.cs`: 판정을 캐릭터 반응으로 변환
- `Assets/_Game/Scripts/TimingCatch/Views/TimingCatchCharacterView.cs`: SPUM 애니메이션, Hit Effect, 취소와 IDLE 복귀 처리
- `Assets/_Game/Animations/TimingCatch/TimingCatchSuccess.anim`: 0.9초 2회 점프 성공 클립
- `Assets/_Game/Tests/Editor/TimingCatchCharacterPresenterTests.cs`: 판정 라우팅과 구독 해제 테스트
- `Assets/_Game/Tests/Editor/TimingCatchSuccessAnimationTests.cs`: 성공 클립 구조와 서드파티 비변경 검증
- `Assets/_Game/Tests/PlayMode/TimingCatchCharacterReactionIntegrationTests.cs`: 씬 배치와 반응 복귀 통합 검증

### 수정 파일

- `Assets/_Game/Scripts/TimingCatch/ViewModels/TimingCatchGameViewModel.cs`: 판정 이벤트 소스 인터페이스 구현
- `Assets/_Game/Scripts/TimingCatch/TimingCatchGameLifetimeScope.cs`: 기능별 등록 메서드 분리, View와 Presenter 등록
- `Assets/_Game/Scenes/TimingCatch.unity`: 연출용 Player 프리팹, 반응 View, Hit Anchor와 에셋 참조 배치

---

### Task 1: 판정 이벤트 계약과 Presenter 라우팅

**Files:**
- Create: `Assets/_Game/Scripts/TimingCatch/Presenters/ITimingCatchJudgeEventSource.cs`
- Create: `Assets/_Game/Scripts/TimingCatch/Views/ITimingCatchCharacterView.cs`
- Create: `Assets/_Game/Scripts/TimingCatch/Presenters/TimingCatchCharacterPresenter.cs`
- Modify: `Assets/_Game/Scripts/TimingCatch/ViewModels/TimingCatchGameViewModel.cs:11`
- Test: `Assets/_Game/Tests/Editor/TimingCatchCharacterPresenterTests.cs`

**Interfaces:**
- Consumes: `TimingCatchJudgeType`, 기존 `TimingCatchGameViewModel.OnJudgeEvaluated`
- Produces: `ITimingCatchJudgeEventSource.OnJudgeEvaluated`, `ITimingCatchCharacterView.PlaySuccessReaction()`, `PlayFailureReaction()`, `ResetToIdle()`, `TimingCatchCharacterPresenter`

- [ ] **Step 1: 실패하는 Presenter 테스트 작성**

다음 네 동작을 하나의 테스트 파일에 작성한다.

```csharp
using System;
using NUnit.Framework;
using GameArifiction.TimingCatch;

namespace GameArifiction.Tests.Editor
{
    /// <summary>
    /// [기능]: 타이밍 캐치 판정이 캐릭터 성공·실패 반응으로 정확히 라우팅되는지 검증합니다.
    /// [작성자]: 윤승종
    /// </summary>
    [TestFixture]
    public sealed class TimingCatchCharacterPresenterTests
    {
        #region 내부 필드 (Private Fields)
        private FakeJudgeEventSource m_eventSource;
        private FakeCharacterView m_characterView;
        private TimingCatchCharacterPresenter m_presenter;
        #endregion

        #region 테스트 생명주기 (Test Lifecycle)
        [SetUp]
        public void SetUp()
        {
            m_eventSource = new FakeJudgeEventSource();
            m_characterView = new FakeCharacterView();
            m_presenter = new TimingCatchCharacterPresenter
            {
                JudgeEventSource = m_eventSource,
                CharacterView = m_characterView
            };
            m_presenter.Start();
        }

        [TearDown]
        public void TearDown()
        {
            m_presenter.Dispose();
        }
        #endregion

        #region 테스트 (Tests)
        [TestCase(TimingCatchJudgeType.Perfect)]
        [TestCase(TimingCatchJudgeType.Good)]
        public void JudgeEvaluated_WithSuccessJudge_PlaysSuccessReaction(TimingCatchJudgeType judge)
        {
            m_eventSource.Raise(judge);

            Assert.AreEqual(1, m_characterView.SuccessCount);
            Assert.AreEqual(0, m_characterView.FailureCount);
        }

        [Test]
        public void JudgeEvaluated_WithMiss_PlaysFailureReaction()
        {
            m_eventSource.Raise(TimingCatchJudgeType.Miss);

            Assert.AreEqual(0, m_characterView.SuccessCount);
            Assert.AreEqual(1, m_characterView.FailureCount);
        }

        [Test]
        public void Dispose_AfterStart_StopsReactionRequests()
        {
            m_presenter.Dispose();

            m_eventSource.Raise(TimingCatchJudgeType.Perfect);

            Assert.AreEqual(0, m_characterView.SuccessCount);
            Assert.AreEqual(0, m_characterView.FailureCount);
        }
        #endregion

        #region 테스트 대역 (Test Doubles)
        private sealed class FakeJudgeEventSource : ITimingCatchJudgeEventSource
        {
            public event Action<TimingCatchJudgeType> OnJudgeEvaluated;

            public void Raise(TimingCatchJudgeType judge)
            {
                OnJudgeEvaluated?.Invoke(judge);
            }
        }

        private sealed class FakeCharacterView : ITimingCatchCharacterView
        {
            public int SuccessCount { get; private set; }
            public int FailureCount { get; private set; }

            public void PlaySuccessReaction()
            {
                SuccessCount++;
            }

            public void PlayFailureReaction()
            {
                FailureCount++;
            }

            public void ResetToIdle()
            {
            }
        }
        #endregion
    }
}
```

- [ ] **Step 2: 테스트가 계약 타입 부재로 실패하는지 확인**

Run: Unity Test Runner 또는 UCP EditMode 필터 `TimingCatchCharacterPresenterTests`

Expected: `ITimingCatchJudgeEventSource`, `ITimingCatchCharacterView`, `TimingCatchCharacterPresenter` 타입 부재로 컴파일 실패

- [ ] **Step 3: 두 인터페이스 작성**

```csharp
using System;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 캐치 판정 이벤트를 구독자에게 제공하는 읽기 전용 계약입니다.
    /// [작성자]: 윤승종
    /// </summary>
    public interface ITimingCatchJudgeEventSource
    {
        event Action<TimingCatchJudgeType> OnJudgeEvaluated;
    }
}
```

```csharp
namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 캐치 캐릭터의 성공·실패·대기 반응 명령을 정의합니다.
    /// [작성자]: 윤승종
    /// </summary>
    public interface ITimingCatchCharacterView
    {
        void PlaySuccessReaction();
        void PlayFailureReaction();
        void ResetToIdle();
    }
}
```

- [ ] **Step 4: Presenter 최소 구현 작성**

`TimingCatchCharacterPresenter`는 프로젝트 규칙에 따라 `[Inject]` 프로퍼티 주입을 사용하고 두 구체 클래스가 아닌 인터페이스에만 의존한다. `Start()` 중복 호출과 `Dispose()` 중복 호출을 방어하는 `m_isSubscribed` 플래그를 둔다. `HandleJudgeEvaluated()`는 `Perfect`, `Good`만 성공으로, `Miss`만 실패로 전달하며 알 수 없는 값은 한글 경고 로그를 남긴다.

```csharp
using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: 타이밍 판정을 캐릭터 성공·실패 반응 명령으로 변환합니다.
    /// [작성자]: 윤승종
    /// </summary>
    public sealed class TimingCatchCharacterPresenter : IStartable, IDisposable
    {
        #region 내부 필드 (Private Fields)
        private bool m_isSubscribed;
        #endregion

        #region 주입 프로퍼티 (Injected Properties)
        [Inject]
        public ITimingCatchJudgeEventSource JudgeEventSource { get; set; }

        [Inject]
        public ITimingCatchCharacterView CharacterView { get; set; }
        #endregion

        public void Start()
        {
            if (m_isSubscribed || JudgeEventSource == null)
            {
                return;
            }

            JudgeEventSource.OnJudgeEvaluated += HandleJudgeEvaluated;
            m_isSubscribed = true;
        }

        public void Dispose()
        {
            if (m_isSubscribed == false || JudgeEventSource == null)
            {
                return;
            }

            JudgeEventSource.OnJudgeEvaluated -= HandleJudgeEvaluated;
            m_isSubscribed = false;
        }

        private void HandleJudgeEvaluated(TimingCatchJudgeType judge)
        {
            if (CharacterView == null)
            {
                return;
            }

            if (judge == TimingCatchJudgeType.Perfect || judge == TimingCatchJudgeType.Good)
            {
                CharacterView.PlaySuccessReaction();
                return;
            }

            if (judge == TimingCatchJudgeType.Miss)
            {
                CharacterView.PlayFailureReaction();
                return;
            }

            Debug.LogWarning($"[TimingCatchCharacterPresenter] 지원하지 않는 판정이 전달되었습니다: {judge}");
        }
    }
}
```

위 코드의 주입 프로퍼티, `Start()`, `Dispose()`, `HandleJudgeEvaluated()`에는 각각 기능 설명과 `[작성자]: 윤승종`, `[수정 날짜]: 2026-07-13`, `[마지막 수정 작성자]: 윤승종`, 해당 멤버의 구체적인 수정 내용을 포함한 XML 문서를 작성한다. 테스트 파일의 `SetUp()`, `TearDown()`, 각 테스트와 테스트 대역 메서드에도 같은 형식의 XML 문서를 작성한다.

- [ ] **Step 5: ViewModel을 이벤트 소스 계약에 연결**

`TimingCatchGameViewModel` 선언만 다음과 같이 변경하고 기존 이벤트와 발행 코드는 유지한다.

```csharp
public sealed class TimingCatchGameViewModel : ITimingCatchJudgeEventSource
```

- [ ] **Step 6: Presenter 테스트 통과 확인**

Run: Unity Test Runner 또는 UCP EditMode 필터 `TimingCatchCharacterPresenterTests`

Expected: 4개 케이스 PASS (`Perfect`, `Good`, `Miss`, `Dispose`)

- [ ] **Step 7: Task 1 파일만 커밋**

```bash
git add Assets/_Game/Scripts/TimingCatch/Presenters/ITimingCatchJudgeEventSource.cs Assets/_Game/Scripts/TimingCatch/Views/ITimingCatchCharacterView.cs Assets/_Game/Scripts/TimingCatch/Presenters/TimingCatchCharacterPresenter.cs Assets/_Game/Scripts/TimingCatch/ViewModels/TimingCatchGameViewModel.cs Assets/_Game/Tests/Editor/TimingCatchCharacterPresenterTests.cs
git commit -m "feat: 타이밍 판정 캐릭터 반응 라우팅 추가"
```

---

### Task 2: SPUM 성공·실패 반응 View

**Files:**
- Create: `Assets/_Game/Scripts/TimingCatch/Views/TimingCatchCharacterView.cs`

**Interfaces:**
- Consumes: `ITimingCatchCharacterView`, `SPUM_Prefabs`, `PlayerState`, `AnimationClip`, `Hit_Effect.prefab`
- Produces: 최신 판정 우선 취소 정책, 성공·실패 재생, 이펙트 정리, IDLE 복귀

- [ ] **Step 1: View의 직렬화 계약과 초기화 작성**

다음 필드를 사용한다.

```csharp
[SerializeField] private SPUM_Prefabs m_spumPrefab;
[SerializeField] private AnimationClip m_successClip;
[SerializeField] private GameObject m_hitEffectPrefab;
[SerializeField] private Transform m_hitEffectAnchor;
[SerializeField] private float m_hitEffectLifetime = 0.35f;

private CancellationTokenSource m_reactionCancellation;
private GameObject m_activeHitEffect;
private int m_successClipIndex = -1;
private bool m_isInitialized;
```

`Awake()`에서 다음 순서로 초기화한다.

1. `m_spumPrefab`이 없으면 `GetComponentInChildren<SPUM_Prefabs>(true)`로 탐색
2. `_anim`이 없으면 SPUM 하위 `Animator` 탐색
3. Animator Controller가 없으면 오류 로그 후 false 반환
4. 애니메이션 목록이 비어 있으면 `PopulateAnimationLists()` 호출
5. `m_successClip`이 `OTHER_List`에 없으면 추가하고 인덱스 저장
6. `OverrideControllerInit()` 호출
7. `ResetToIdle()` 호출

- [ ] **Step 2: 최신 판정 우선 반응 실행 작성**

`PlaySuccessReaction()`은 `PlayerState.OTHER`, 성공 클립 인덱스, `m_successClip.length`, 이펙트 없음으로 실행한다. `PlayFailureReaction()`은 `PlayerState.DAMAGED`, 인덱스 0, 실제 피격 클립 길이 또는 안전 기본값 `0.33333334f`, 이펙트 있음으로 실행한다.

반응 코어의 서명은 다음과 같이 고정한다.

```csharp
private async UniTaskVoid PlayReactionAsync(
    PlayerState state,
    int animationIndex,
    float duration,
    bool hasHitEffect,
    CancellationToken cancellationToken)
```

각 public 반응 메서드는 기존 `CancellationTokenSource`를 취소·폐기하고 `GetCancellationTokenOnDestroy()`와 연결한 새 토큰을 만든다. 새 판정이 이전 반응을 취소한 경우 이전 비동기 흐름은 `IDLE`을 재생하지 않고 종료해야 한다. 정상 완료된 최신 흐름만 `ResetToIdle()`과 이펙트 정리를 수행한다.

- [ ] **Step 3: Hit Effect 생성과 정리 작성**

`Miss` 시작 시 기존 `m_activeHitEffect`가 있으면 명시적 널 검사 후 파괴한다. `m_hitEffectAnchor`가 있으면 해당 위치에, 없으면 View의 `transform.position`에 생성한다. `m_hitEffectLifetime`은 0보다 큰 값으로 `Mathf.Max(0.01f, m_hitEffectLifetime)` 보정하며, 피격 애니메이션과 이펙트 중 더 긴 시간까지 재생한 후 파괴한다.

빈 이펙트 참조는 경고만 남기며 피격 애니메이션은 계속 재생한다. 최대 동시 이펙트가 하나이고 기본 7스테이지의 저빈도 이벤트이므로 별도 풀은 추가하지 않는다.

- [ ] **Step 4: 파괴 수명주기와 로그 작성**

`OnDestroy()`에서 반응 토큰을 취소·폐기하고 활성 이펙트를 파괴한다. 모든 로그는 `[TimingCatchCharacterView]` 접두사와 한글 메시지를 사용한다. `UnityEngine.Object` 참조에는 널 조건부 연산자를 사용하지 않는다.

다음 전체 골격을 기준으로 구현해 취소된 이전 반응이 최신 반응의 `IDLE` 복귀나 이펙트 정리를 덮어쓰지 않도록 한다.

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameArifiction.TimingCatch
{
    /// <summary>
    /// [기능]: SPUM 캐릭터의 성공·실패 애니메이션과 타격 이펙트를 재생합니다.
    /// [작성자]: 윤승종
    /// </summary>
    public sealed class TimingCatchCharacterView : MonoBehaviour, ITimingCatchCharacterView
    {
        #region 인스펙터 참조 (Inspector References)
        [SerializeField] private SPUM_Prefabs m_spumPrefab;
        [SerializeField] private AnimationClip m_successClip;
        [SerializeField] private GameObject m_hitEffectPrefab;
        [SerializeField] private Transform m_hitEffectAnchor;
        [SerializeField] private float m_hitEffectLifetime = 0.35f;
        #endregion

        #region 내부 필드 (Private Fields)
        private CancellationTokenSource m_reactionCancellation;
        private GameObject m_activeHitEffect;
        private int m_successClipIndex = -1;
        private bool m_isInitialized;
        #endregion

        #region 유니티 생명주기 (Unity Lifecycle)
        /// <summary>
        /// [기능]: SPUM 참조와 성공 클립을 초기화하고 기본 자세로 전환합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타이밍 캐릭터 반응 초기화 추가.
        /// </summary>
        private void Awake()
        {
            m_isInitialized = InitializeSpum();
            if (m_isInitialized)
            {
                ResetToIdle();
            }
        }

        /// <summary>
        /// [기능]: 진행 중인 반응과 생성된 타격 이펙트를 정리합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: View 파괴 시 비동기 반응 정리 추가.
        /// </summary>
        private void OnDestroy()
        {
            CancelCurrentReaction();
        }
        #endregion

        #region 공개 메서드 (Public Methods)
        /// <summary>
        /// [기능]: 성공 전용 OTHER 클립을 재생하고 완료 후 IDLE로 복귀합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타이밍 성공 반응 추가.
        /// </summary>
        public void PlaySuccessReaction()
        {
            if (m_isInitialized == false || m_successClip == null || m_successClipIndex < 0)
            {
                Debug.LogWarning("[TimingCatchCharacterView] 성공 애니메이션을 재생할 수 없습니다. SPUM 및 성공 클립 참조를 확인하십시오.");
                return;
            }

            BeginReaction(PlayerState.OTHER, m_successClipIndex, m_successClip.length, false);
        }

        /// <summary>
        /// [기능]: SPUM 피격 클립과 타격 이펙트를 동시에 재생하고 IDLE로 복귀합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타이밍 실패 반응 추가.
        /// </summary>
        public void PlayFailureReaction()
        {
            if (m_isInitialized == false || m_spumPrefab.DAMAGED_List == null || m_spumPrefab.DAMAGED_List.Count == 0)
            {
                Debug.LogWarning("[TimingCatchCharacterView] 피격 애니메이션을 재생할 수 없습니다. SPUM 피격 클립을 확인하십시오.");
                return;
            }

            AnimationClip damagedClip = m_spumPrefab.DAMAGED_List[0];
            float duration = damagedClip != null ? damagedClip.length : 0.33333334f;
            BeginReaction(PlayerState.DAMAGED, 0, duration, true);
        }

        /// <summary>
        /// [기능]: 캐릭터 Animator를 IDLE 첫 프레임과 기본 속도로 복원합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 판정 반응 종료 후 기본 자세 복원 추가.
        /// </summary>
        public void ResetToIdle()
        {
            if (m_spumPrefab == null || m_spumPrefab._anim == null)
            {
                return;
            }

            m_spumPrefab._anim.speed = 1f;
            m_spumPrefab._anim.Play(PlayerState.IDLE.ToString(), 0, 0f);
        }
        #endregion

        #region 내부 메서드 (Private Methods)
        /// <summary>
        /// [기능]: SPUM 애니메이터와 목록을 초기화하고 성공 클립 인덱스를 확보합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: SPUM 런타임 OverrideController 초기화 추가.
        /// </summary>
        private bool InitializeSpum()
        {
            if (m_spumPrefab == null)
            {
                m_spumPrefab = GetComponentInChildren<SPUM_Prefabs>(true);
            }

            if (m_spumPrefab == null)
            {
                Debug.LogError("[TimingCatchCharacterView] SPUM 캐릭터 참조를 찾을 수 없습니다.");
                return false;
            }

            if (m_spumPrefab._anim == null)
            {
                m_spumPrefab._anim = m_spumPrefab.GetComponentInChildren<Animator>(true);
            }

            if (m_spumPrefab._anim == null || m_spumPrefab._anim.runtimeAnimatorController == null)
            {
                Debug.LogError("[TimingCatchCharacterView] SPUM Animator 또는 RuntimeAnimatorController가 없습니다.");
                return false;
            }

            if (m_spumPrefab.allListsHaveItemsExist() == false)
            {
                m_spumPrefab.PopulateAnimationLists();
            }

            if (m_successClip != null)
            {
                m_successClipIndex = m_spumPrefab.OTHER_List.IndexOf(m_successClip);
                if (m_successClipIndex < 0)
                {
                    m_spumPrefab.OTHER_List.Add(m_successClip);
                    m_successClipIndex = m_spumPrefab.OTHER_List.Count - 1;
                }
            }
            else
            {
                Debug.LogWarning("[TimingCatchCharacterView] 성공 AnimationClip이 할당되지 않았습니다.");
            }

            m_spumPrefab.OverrideControllerInit();
            return true;
        }

        /// <summary>
        /// [기능]: 이전 반응을 취소하고 최신 판정 반응을 시작합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 최신 판정 우선 취소 정책 추가.
        /// </summary>
        private void BeginReaction(PlayerState state, int animationIndex, float duration, bool hasHitEffect)
        {
            CancelCurrentReaction();

            m_reactionCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy()
            );

            CancellationTokenSource reactionOwner = m_reactionCancellation;
            PlayReactionAsync(
                state,
                animationIndex,
                duration,
                hasHitEffect,
                reactionOwner,
                reactionOwner.Token
            );
        }

        /// <summary>
        /// [기능]: 지정된 SPUM 반응과 선택적 타격 이펙트를 재생하고 정상 완료 시 IDLE로 복귀합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 취소 가능한 판정 반응 비동기 흐름 추가.
        /// </summary>
        private async UniTaskVoid PlayReactionAsync(
            PlayerState state,
            int animationIndex,
            float duration,
            bool hasHitEffect,
            CancellationTokenSource reactionOwner,
            CancellationToken cancellationToken)
        {
            try
            {
                m_spumPrefab.PlayAnimation(state, animationIndex);
                m_spumPrefab._anim.Play(state.ToString(), 0, 0f);

                if (hasHitEffect)
                {
                    SpawnHitEffect();
                }

                float waitSeconds = Mathf.Max(0.01f, duration);
                if (hasHitEffect)
                {
                    waitSeconds = Mathf.Max(waitSeconds, Mathf.Max(0.01f, m_hitEffectLifetime));
                }

                await UniTask.Delay(
                    TimeSpan.FromSeconds(waitSeconds),
                    DelayType.DeltaTime,
                    PlayerLoopTiming.Update,
                    cancellationToken
                );

                if (cancellationToken.IsCancellationRequested == false &&
                    ReferenceEquals(m_reactionCancellation, reactionOwner))
                {
                    ResetToIdle();
                    ClearActiveHitEffect();
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (ReferenceEquals(m_reactionCancellation, reactionOwner))
                {
                    m_reactionCancellation.Dispose();
                    m_reactionCancellation = null;
                }
            }
        }

        /// <summary>
        /// [기능]: 지정된 피격 앵커에 타격 이펙트를 하나만 생성합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 실패 판정 타격 이펙트 생성 추가.
        /// </summary>
        private void SpawnHitEffect()
        {
            ClearActiveHitEffect();

            if (m_hitEffectPrefab == null)
            {
                Debug.LogWarning("[TimingCatchCharacterView] 타격 이펙트 프리팹이 없어 피격 애니메이션만 재생합니다.");
                return;
            }

            Vector3 spawnPosition = transform.position;
            if (m_hitEffectAnchor != null)
            {
                spawnPosition = m_hitEffectAnchor.position;
            }

            m_activeHitEffect = Instantiate(m_hitEffectPrefab, spawnPosition, Quaternion.identity);
        }

        /// <summary>
        /// [기능]: 현재 판정 반응 토큰과 타격 이펙트를 정리합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 연속 판정 시 이전 반응 정리 추가.
        /// </summary>
        private void CancelCurrentReaction()
        {
            if (m_reactionCancellation != null)
            {
                m_reactionCancellation.Cancel();
                m_reactionCancellation.Dispose();
                m_reactionCancellation = null;
            }

            ClearActiveHitEffect();
        }

        /// <summary>
        /// [기능]: 활성 타격 이펙트 인스턴스를 안전하게 파괴합니다.
        /// [작성자]: 윤승종
        /// [수정 날짜]: 2026-07-13
        /// [마지막 수정 작성자]: 윤승종
        /// [수정 내용]: 타격 이펙트 잔류 방지 정리 추가.
        /// </summary>
        private void ClearActiveHitEffect()
        {
            if (m_activeHitEffect != null)
            {
                Destroy(m_activeHitEffect);
                m_activeHitEffect = null;
            }
        }
        #endregion
    }
}
```

- [ ] **Step 5: 컴파일 확인**

Run: Unity Editor 스크립트 리컴파일 후 Console 확인

Expected: 신규 오류 0건, 신규 경고 0건

- [ ] **Step 6: Task 2 파일만 커밋**

```bash
git add Assets/_Game/Scripts/TimingCatch/Views/TimingCatchCharacterView.cs
git commit -m "feat: SPUM 캐릭터 판정 반응 뷰 추가"
```

---

### Task 3: 피격 클립 기반 성공 AnimationClip 제작

**Files:**
- Create: `Assets/_Game/Animations/TimingCatch/TimingCatchSuccess.anim`
- Test: `Assets/_Game/Tests/Editor/TimingCatchSuccessAnimationTests.cs`
- Reference only: `Assets/ThirdParty/SPUM/Resources/Addons/Legacy/0_Unit/1_Animation/03_Damaged/0_Damaged.anim`

**Interfaces:**
- Consumes: SPUM 계층 경로와 60 FPS 비루프 클립 형식
- Produces: `TimingCatchSuccess.anim`, SPUM `OTHER` 상태에서 재생 가능한 0.9초 성공 반응

- [ ] **Step 1: 실패하는 AnimationClip 에셋 테스트 작성**

`AssetDatabase.LoadAssetAtPath<AnimationClip>()`으로 성공 클립을 읽고 다음을 검증한다.

```csharp
Assert.IsNotNull(clip);
Assert.AreEqual(60f, clip.frameRate);
Assert.AreEqual(0.9f, clip.length, 0.02f);
Assert.IsFalse(AnimationUtility.GetAnimationClipSettings(clip).loopTime);

EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
Assert.IsTrue(Array.Exists(bindings, binding => binding.path == "Root"));
Assert.IsTrue(Array.Exists(bindings, binding => binding.path == "Root/BodySet/P_Body"));
Assert.IsTrue(Array.Exists(bindings, binding => binding.path == "Root/BodySet/P_Body/ArmSet/ArmL/P_LArm"));
Assert.IsTrue(Array.Exists(bindings, binding => binding.path == "Root/BodySet/P_Body/ArmSet/ArmR/P_RArm"));
Assert.IsTrue(Array.Exists(bindings, binding => binding.path == "Root/BodySet/P_Body/HeadSet/P_Head"));
Assert.IsTrue(Array.Exists(bindings, binding => binding.path == "Root/P_LFoot"));
Assert.IsTrue(Array.Exists(bindings, binding => binding.path == "Root/P_RFoot"));
Assert.IsTrue(Array.Exists(bindings, binding => binding.path == "Shadow"));
```

또한 `Root`의 `m_LocalPosition.y` 곡선에서 두 개의 서로 분리된 양수 정점이 존재하고 마지막 값이 0인지 검증한다.

- [ ] **Step 2: 성공 클립 부재로 테스트가 실패하는지 확인**

Run: Unity Test Runner 또는 UCP EditMode 필터 `TimingCatchSuccessAnimationTests`

Expected: `TimingCatchSuccess.anim`을 찾지 못해 FAIL

- [ ] **Step 3: 원본 피격 클립 복제**

Unity Project 창에서 `0_Damaged.anim`을 `Assets/_Game/Animations/TimingCatch/TimingCatchSuccess.anim`으로 복제한다. 원본 GUID와 파일 내용은 변경하지 않는다.

- [ ] **Step 4: 성공 키프레임으로 재구성**

Animation 창에서 기존 피격 동작 키를 제거하고 아래 값을 입력한다. 회전은 Z축, 위치는 Y축, 그림자는 X/Y Scale을 사용한다.

| Path / Property | 0.00 | 0.10 | 0.22 | 0.36 | 0.48 | 0.60 | 0.78 | 0.90 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `Root` Position Y | 0 | -0.04 | 0.36 | 0 | -0.03 | 0.30 | 0 | 0 |
| `P_Body` Rotation Z | 0 | -6 | 4 | -3 | -5 | 5 | -2 | 0 |
| `P_LArm` Rotation Z | 0 | -25 | -120 | -15 | -30 | -110 | -10 | 0 |
| `P_RArm` Rotation Z | 0 | 25 | 120 | 15 | 30 | 110 | 10 | 0 |
| `P_Head` Rotation Z | 0 | 3 | -6 | 3 | 4 | -7 | 2 | 0 |
| `P_LFoot` Rotation Z | 0 | -8 | 18 | 0 | -8 | 16 | 0 | 0 |
| `P_RFoot` Rotation Z | 0 | 8 | -18 | 0 | 8 | -16 | 0 | 0 |
| `Shadow` Scale X/Y | 1 | 1.05 | 0.72 | 1 | 1.04 | 0.78 | 1 | 1 |

클립 샘플레이트를 60, 길이를 0.9초, Loop Time을 false로 설정한다. 눈의 활성 상태 곡선은 제거하여 기본 눈을 유지한다. 시작과 마지막 키를 동일하게 맞춘다.

- [ ] **Step 5: 성공 클립 테스트와 원본 비변경 확인**

Run: Unity Test Runner 또는 UCP EditMode 필터 `TimingCatchSuccessAnimationTests`

Expected: 모든 에셋·곡선 검증 PASS

Run: `git diff -- Assets/ThirdParty/SPUM`

Expected: 이번 작업에서 생성된 SPUM 원본 변경 없음. 기존 사용자 변경이 보이면 신규 성공 클립 작업과 무관함을 `git diff`로 분리 확인하고 스테이징하지 않는다.

- [ ] **Step 6: Task 3 파일만 커밋**

```bash
git add Assets/_Game/Animations/TimingCatch/TimingCatchSuccess.anim Assets/_Game/Animations/TimingCatch/TimingCatchSuccess.anim.meta Assets/_Game/Tests/Editor/TimingCatchSuccessAnimationTests.cs
git commit -m "feat: 타이밍 성공 점프 애니메이션 추가"
```

---

### Task 4: VContainer 등록과 타이밍 씬 배치

**Files:**
- Modify: `Assets/_Game/Scripts/TimingCatch/TimingCatchGameLifetimeScope.cs`
- Modify: `Assets/_Game/Scenes/TimingCatch.unity`

**Interfaces:**
- Consumes: `TimingCatchCharacterView`, `ITimingCatchCharacterView`, `TimingCatchCharacterPresenter`, `ITimingCatchJudgeEventSource`
- Produces: 타이밍 씬에서 자동 주입되는 캐릭터 반응 파이프라인

- [ ] **Step 1: LifetimeScope를 기능별 private 메서드로 분리**

`Configure()`는 다음 호출만 담당하도록 정리한다.

```csharp
protected override void Configure(IContainerBuilder builder)
{
    ConfigureData(builder);
    ConfigureCore(builder);
    ConfigureViews(builder);
    ConfigureEntryPoints(builder);
}
```

기존 등록 내용을 삭제하지 않고 해당 메서드로 이동한다. `ConfigureCore()`에서 `TimingCatchGameViewModel`을 `.AsSelf().As<ITimingCatchJudgeEventSource>()`로 등록한다.

- [ ] **Step 2: View와 Presenter 등록**

`ConfigureViews()`에 다음 등록을 추가한다.

```csharp
builder.RegisterComponentInHierarchy<TimingCatchCharacterView>()
    .As<ITimingCatchCharacterView>();
```

`ConfigureEntryPoints()`에 다음을 추가한다.

```csharp
builder.RegisterEntryPoint<TimingCatchCharacterPresenter>(Lifetime.Scoped);
```

LifetimeScope에 캐릭터 View SerializeField를 추가하지 않는다.

- [ ] **Step 3: 타이밍 씬에 연출 캐릭터 배치**

Unity Editor 또는 Unity MCP로 `TimingCatch.unity`를 열고 다음 하이어라키를 추가한다.

```text
TimingCatchCharacterRoot  Position (-5.5, -2.2, 0)
├─ Player                Player.prefab 인스턴스, Local Position (0, 0, 0)
└─ HitEffectAnchor       Local Position (0, 1.0, 0)
```

`Player.prefab` 인스턴스의 `PlayerView`는 비활성화하여 이동 입력을 막는다. `Rigidbody2D.simulated`와 이동 Collider도 비활성화한다. SPUM 렌더러는 활성 상태로 유지한다.

`TimingCatchCharacterRoot`에 `TimingCatchCharacterView`를 추가하고 다음을 할당한다.

- `m_spumPrefab`: Player 자식의 `SPUM_Prefabs`
- `m_successClip`: `TimingCatchSuccess.anim`
- `m_hitEffectPrefab`: `Assets/_Game/Prefabs/Effects/Hit_Effect.prefab`
- `m_hitEffectAnchor`: `HitEffectAnchor`
- `m_hitEffectLifetime`: `0.35`

씬 저장 전 기존 `TimingCatch.unity` 사용자 변경을 확인하고 위 하이어라키·참조 외의 변경을 덮어쓰지 않는다.

- [ ] **Step 4: 씬 DI와 컴파일 확인**

TimingCatch 씬 진입 후 Console에서 다음이 없어야 한다.

- `TimingCatchCharacterView` 미등록
- `ITimingCatchCharacterView` Resolve 실패
- `ITimingCatchJudgeEventSource` Resolve 실패
- SPUM Animator Controller 누락
- 성공 클립 누락

- [ ] **Step 5: Task 4 파일만 커밋**

```bash
git add Assets/_Game/Scripts/TimingCatch/TimingCatchGameLifetimeScope.cs Assets/_Game/Scenes/TimingCatch.unity
git commit -m "feat: 타이밍 씬 캐릭터 반응 연결"
```

---

### Task 5: PlayMode 통합 검증과 회귀 확인

**Files:**
- Create: `Assets/_Game/Tests/PlayMode/TimingCatchCharacterReactionIntegrationTests.cs`

**Interfaces:**
- Consumes: `TimingCatchCharacterView`, `SPUM_Prefabs`, `TimingCatch.unity`
- Produces: 씬 배치, 성공·실패 재생, IDLE 복귀에 대한 자동 검증 증거

- [ ] **Step 1: 씬 배치 통합 테스트 작성**

`SceneManager.LoadSceneAsync("TimingCatch")` 후 한 프레임 대기하고 다음을 검증한다.

```csharp
TimingCatchCharacterView view = Object.FindFirstObjectByType<TimingCatchCharacterView>();
Assert.IsNotNull(view);

SPUM_Prefabs spumPrefab = view.GetComponentInChildren<SPUM_Prefabs>(true);
Assert.IsNotNull(spumPrefab);
Assert.IsNotNull(spumPrefab._anim);
```

- [ ] **Step 2: 성공 반응과 IDLE 복귀 테스트 작성**

`view.PlaySuccessReaction()` 호출 직후 한 프레임 대기해 Animator가 활성 상태인지 확인하고, 1.0초 대기 후 다음을 검증한다.

```csharp
AnimatorStateInfo stateInfo = spumPrefab._anim.GetCurrentAnimatorStateInfo(0);
Assert.IsTrue(stateInfo.IsName(PlayerState.IDLE.ToString()));
```

- [ ] **Step 3: 실패 반응과 이펙트 정리 테스트 작성**

`view.PlayFailureReaction()` 호출 직후 `Hit_Effect(Clone)`이 존재하는지 확인하고, 0.5초 대기 후 인스턴스가 정리되고 Animator가 `IDLE`인지 확인한다.

- [ ] **Step 4: 빠른 연속 판정 최신 우선 테스트 작성**

성공 반응 호출 후 0.1초 뒤 실패 반응을 호출한다. 0.5초 뒤 실패 반응이 정상 종료되어 `IDLE`이고 잔류 `Hit_Effect(Clone)`이 없는지 확인한다. 성공 반응의 늦은 복귀가 실패 반응을 중간에 덮지 않는지 Console 예외와 Animator 상태로 검증한다.

- [ ] **Step 5: PlayMode 테스트 실행**

Run: Unity Test Runner 또는 UCP PlayMode 필터 `TimingCatchCharacterReactionIntegrationTests`

Expected: 씬 배치, 성공 복귀, 실패 이펙트, 연속 판정 테스트 전부 PASS

- [ ] **Step 6: 전체 관련 테스트 실행**

Run: EditMode 필터 `GameArifiction.Tests.Editor`

Expected: 기존 테스트와 신규 Presenter·AnimationClip 테스트 전부 PASS

Run: PlayMode 필터 `TimingCatchCharacterReactionIntegrationTests|LobbyFlowIntegrationTests`

Expected: 타이밍 반응과 로비 흐름 회귀 테스트 PASS

- [ ] **Step 7: 수동 Game View 검증**

1. TimingCatch 씬을 재생한다.
2. Perfect 영역에서 입력해 첫 번째와 두 번째 점프 정점이 구분되는지 확인한다.
3. Good 영역에서도 동일한 성공 반응이 판정 직후 실행되는지 확인한다.
4. Miss를 발생시켜 피격 동작과 Hit Effect가 동시에 보이는지 확인한다.
5. 연속 입력 시 최신 판정 반응으로 즉시 교체되는지 확인한다.
6. 7스테이지 종료 후 결과 팝업과 로비 복귀가 기존대로 동작하는지 확인한다.

- [ ] **Step 8: 정적 규칙 검증**

```bash
rg -n "\?\.|\?\?" Assets/_Game/Scripts/TimingCatch/Views/TimingCatchCharacterView.cs
rg -n "Antigravity|AI Agent|Assistant|Codex" Assets/_Game/Scripts/TimingCatch Assets/_Game/Tests/Editor/TimingCatchCharacterPresenterTests.cs Assets/_Game/Tests/PlayMode/TimingCatchCharacterReactionIntegrationTests.cs
git diff --check
git diff -- Assets/ThirdParty/SPUM
```

Expected:

- 첫 두 검색 결과 없음
- `git diff --check` 오류 없음
- 이번 작업으로 추가된 SPUM 원본 변경 없음

- [ ] **Step 9: Task 5 파일만 커밋**

```bash
git add Assets/_Game/Tests/PlayMode/TimingCatchCharacterReactionIntegrationTests.cs
git commit -m "test: 타이밍 캐릭터 반응 통합 검증 추가"
```

---

## 구현 후 사용자 안내 체크리스트

### 에디터 참조 필요 버튼

- 없음. 신규 `func_` 버튼 콜백을 추가하지 않는다.

### SerializeField 참조

- `TimingCatchCharacterView.m_spumPrefab` → 타이밍 씬 Player 인스턴스의 `SPUM_Prefabs`
- `TimingCatchCharacterView.m_successClip` → `TimingCatchSuccess.anim`
- `TimingCatchCharacterView.m_hitEffectPrefab` → `Hit_Effect.prefab`
- `TimingCatchCharacterView.m_hitEffectAnchor` → `HitEffectAnchor`
- `TimingCatchCharacterView.m_hitEffectLifetime` → `0.35`

### 이벤트 구독

- 구독: `TimingCatchCharacterPresenter.Start()`
- 해제: `TimingCatchCharacterPresenter.Dispose()`
- 이벤트: `ITimingCatchJudgeEventSource.OnJudgeEvaluated`

## 완료 증거

- 신규 EditMode 테스트 결과
- 신규 PlayMode 테스트 결과
- Unity Console 신규 오류 0건
- Game View에서 성공 2회 점프, 실패 피격+이펙트, 최신 판정 우선 동작 확인
- 결과 팝업과 로비 복귀 회귀 확인
- `git diff --check` 성공
- 서드파티 SPUM 원본 비변경 확인
