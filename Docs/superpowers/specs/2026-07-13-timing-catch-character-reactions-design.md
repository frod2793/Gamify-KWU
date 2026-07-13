# 타이밍 캐치 캐릭터 성공·실패 연출 설계

## 1. 목표

타이밍 캐치 미니게임의 각 스테이지 판정 직후, 로비에서 사용하는 동일한 SPUM 캐릭터가 판정에 맞는 반응을 보여준다.

- `Perfect`, `Good`: 전용 성공 애니메이션으로 두 번 펄쩍 뛰는 기쁨 표현
- `Miss`: 기존 SPUM 피격 애니메이션과 타격 이펙트 동시 재생
- 반응 종료 후 `IDLE` 복귀
- 빠르게 다음 판정이 발생하면 이전 반응을 중단하고 최신 반응을 우선 재생

## 2. 범위

### 포함

- 로비와 동일한 `Assets/_Game/Prefabs/Player/Player.prefab`을 타이밍 캐치 씬의 연출 캐릭터로 사용
- 기존 `0_Damaged.anim`의 SPUM 계층 바인딩을 분석해 프로젝트 전용 성공 클립 제작
- 타이밍 캐치 판정 이벤트와 캐릭터 연출 연결
- 실패 시 `Assets/_Game/Prefabs/Effects/Hit_Effect.prefab` 표시
- 연속 판정 시 반응 취소·교체 처리
- 코드, 애니메이션 클립, 씬 바인딩 검증

### 제외

- SPUM 서드파티 원본 코드 및 원본 애니메이션 수정
- 로비 플레이어의 이동 로직 변경
- 판정, 점수, 등급 계산 규칙 변경
- 캐릭터 커스터마이징 시스템 신규 구축
- 별도 성공 사운드 및 카메라 흔들림 추가

## 3. 확인된 현재 구조

### 판정 흐름

`TimingCatchGameViewModel`은 스테이지 판정마다 `OnJudgeEvaluated`를 발행한다.

- `TimingCatchJudgeType.Perfect`
- `TimingCatchJudgeType.Good`
- `TimingCatchJudgeType.Miss`

최종 결과용 `OnGameResult`가 아니라 `OnJudgeEvaluated`를 사용해야 매 판정 직후 반응할 수 있다.

### 로비 캐릭터

로비 플레이어는 `Assets/_Game/Prefabs/Player/Player.prefab` 기반 SPUM 캐릭터다. `Lobby`와 `TimingCatch`는 별도 씬이므로 로비의 런타임 `PlayerView` 오브젝트를 검색하거나 직접 전달하지 않는다. 타이밍 캐치 씬에 동일 프리팹의 별도 인스턴스를 배치한다.

### 피격 클립

로비 캐릭터가 사용하는 피격 클립은 다음 파일이다.

`Assets/ThirdParty/SPUM/Resources/Addons/Legacy/0_Unit/1_Animation/03_Damaged/0_Damaged.anim`

확인된 사양:

- 샘플레이트: 60 FPS
- 길이: 약 0.333초
- 루프: 비활성화
- 주요 키 시점: 0초, 0.1667초, 0.25초, 0.3333초
- 주요 바인딩: `Root`, 몸통, 좌우 팔, 머리, 좌우 발, 그림자, 눈 오브젝트

## 4. 아키텍처

```text
TimingCatchGameViewModel
    │ OnJudgeEvaluated
    ▼
TimingCatchCharacterPresenter
    │
    ▼
ITimingCatchCharacterView
    │
    ├─ Perfect / Good
    │      └─ SPUM OTHER → TimingCatchSuccess.anim
    │
    └─ Miss
           ├─ SPUM DAMAGED → 0_Damaged.anim
           └─ Hit_Effect 생성
```

### TimingCatchGameViewModel

기존 판정과 점수 계산 책임을 유지한다. 캐릭터, Animator, 이펙트에 대한 의존성을 추가하지 않는다.

### ITimingCatchCharacterView

Presenter가 구체적인 Unity 컴포넌트를 알지 않도록 반응 명령 계약을 제공한다.

```csharp
public interface ITimingCatchCharacterView
{
    void PlaySuccessReaction();
    void PlayFailureReaction();
    void ResetToIdle();
}
```

### TimingCatchCharacterPresenter

순수 C# EntryPoint로 구현한다.

- `IStartable.Start()`에서 `TimingCatchGameViewModel.OnJudgeEvaluated` 구독
- `IDisposable.Dispose()`에서 구독 해제
- `Perfect`, `Good`을 성공 반응으로 변환
- `Miss`를 실패 반응으로 변환
- View 인터페이스에만 의존

### TimingCatchCharacterView

`MonoBehaviour` 기반 표현 계층이다.

- SPUM 애니메이션 재생
- 성공 클립을 `OTHER` 목록에 중복 없이 런타임 등록
- 실패 이펙트 생성 및 정리
- 반응 취소 토큰 관리
- 반응 종료 후 `IDLE` 복귀
- Model 또는 `TimingCatchGameViewModel`을 직접 참조하지 않음

## 5. 성공 애니메이션 설계

신규 클립은 프로젝트 영역에 저장한다.

`Assets/_Game/Animations/TimingCatch/TimingCatchSuccess.anim`

원본 피격 클립을 수정하지 않고, 바인딩 경로와 직렬화 구조만 기준으로 사용한다.

### 클립 사양

- 길이: 약 0.9초
- 샘플레이트: 60 FPS
- 루프: 비활성화
- 재생 상태: SPUM `OTHER`

### 타임라인

| 시간 | 동작 |
|---:|---|
| 0.00초 | 기본 자세 |
| 0.10초 | 몸을 낮추고 양팔을 준비 |
| 0.22초 | 첫 점프 정점, 양팔을 위로 펼침 |
| 0.36초 | 첫 착지, 몸을 살짝 압축 |
| 0.48초 | 두 번째 점프 시작 |
| 0.60초 | 두 번째 점프 정점, 팔과 머리에 보조 동작 적용 |
| 0.78초 | 두 번째 착지 |
| 0.90초 | 기본 자세 완전 복귀 |

### 바인딩 원칙

- `Root`: Y 위치를 조절해 두 번 점프
- `P_Body`: 준비·착지 시 압축감을 주는 회전 및 위치 보정
- `P_LArm`, `P_RArm`: 점프 시 위로 펼치는 회전
- `P_Head`: 몸통과 반대 방향의 작은 보조 회전
- `P_LFoot`, `P_RFoot`: 공중에서 모으고 착지 시 원복
- `Shadow`: 점프 정점에서 축소하고 착지 시 원복
- 눈: 일반 눈 상태 유지
- 시작과 마지막 키는 동일한 기본 자세로 맞춰 상태 전환 시 튐을 방지

## 6. 실패 연출 설계

`Miss` 판정 시 다음을 동시에 실행한다.

1. SPUM `PlayerState.DAMAGED`, 인덱스 0 재생
2. 캐릭터의 지정된 피격 앵커에 `Hit_Effect.prefab` 생성
3. 피격 클립 종료 후 `IDLE` 복귀
4. 이펙트 재생 완료 후 인스턴스 정리

이펙트 프리팹이 누락되더라도 피격 애니메이션은 정상 재생한다. SPUM 또는 Animator 참조가 누락된 경우에는 한글 경고 로그를 남기고 예외 없이 연출을 종료한다.

## 7. 연속 판정과 수명주기

- View는 현재 반응용 `CancellationTokenSource`를 하나만 유지한다.
- 새로운 판정이 오면 기존 토큰을 취소하고 폐기한다.
- 캐릭터 자세와 Animator 속도를 안전한 기본값으로 복원한 뒤 최신 반응을 재생한다.
- `OnDestroy()`에서 진행 중인 반응을 취소하고 생성한 이펙트를 정리한다.
- `UnityEngine.Object` 참조에는 널 조건부 연산자를 사용하지 않는다.
- 프레임 루프에는 LINQ, Boxing, `new` 할당을 추가하지 않는다.

## 8. VContainer 구성

`TimingCatchGameLifetimeScope`의 기능 영역을 전용 private 메서드로 분리하고 다음을 등록한다.

- `TimingCatchCharacterView`: `RegisterComponentInHierarchy<TimingCatchCharacterView>()`
- View 인터페이스: 등록된 컴포넌트를 `ITimingCatchCharacterView`로 노출
- `TimingCatchCharacterPresenter`: `RegisterEntryPoint<TimingCatchCharacterPresenter>(Lifetime.Scoped)`

LifetimeScope가 View를 `[SerializeField]`로 직접 받아 `RegisterInstance`하지 않는다.

## 9. 에디터 바인딩

### Button.OnClick

에디터에서 직접 연결할 `func_` 메서드는 없다.

### SerializeField

`TimingCatchCharacterView`에 다음 참조를 할당한다.

- `m_spumPrefab`: 타이밍 캐치 씬에 배치된 SPUM 캐릭터
- `m_successClip`: `TimingCatchSuccess.anim`
- `m_hitEffectPrefab`: `Hit_Effect.prefab`
- `m_hitEffectAnchor`: 캐릭터 몸통 중심의 타격 이펙트 위치

### 이벤트 구독

- 구독: `TimingCatchCharacterPresenter.Start()`
- 해제: `TimingCatchCharacterPresenter.Dispose()`
- 이벤트: `TimingCatchGameViewModel.OnJudgeEvaluated`

## 10. 오류 처리

- SPUM 참조 누락: 경고 로그 후 해당 반응 생략
- Animator 누락: 경고 로그 후 해당 반응 생략
- 성공 클립 누락: 경고 로그 후 `IDLE` 유지
- `Hit_Effect` 누락: 피격 애니메이션만 재생
- 중복 성공 클립 등록: 기존 인덱스를 재사용
- 취소된 이전 반응: `IDLE` 복귀를 실행하지 않아 최신 반응을 덮어쓰지 않음
- View가 파괴될 때: 토큰과 이펙트 정리

모든 로그는 `[TimingCatchCharacterView]` 또는 `[TimingCatchCharacterPresenter]` 접두사와 한글 메시지를 사용한다.

## 11. 검증 계획

### 정적 검증

- 서드파티 SPUM 파일에 변경이 없는지 확인
- View가 Model을 직접 참조하지 않는지 확인
- LifetimeScope가 View를 `RegisterComponentInHierarchy`로 등록하는지 확인
- 이벤트 구독과 해제가 대칭인지 확인
- Unity Object에 `?.`, `??`를 사용하지 않았는지 확인
- 모든 신규 메서드와 클래스에 지정된 XML 문서 주석이 있는지 확인

### EditMode 테스트

- `Perfect` 판정이 성공 반응을 한 번 요청하는지 검증
- `Good` 판정이 성공 반응을 한 번 요청하는지 검증
- `Miss` 판정이 실패 반응을 한 번 요청하는지 검증
- Presenter를 Dispose한 뒤 판정이 발생해도 반응하지 않는지 검증

### PlayMode 검증

- 타이밍 캐치 씬에서 로비와 동일한 SPUM 캐릭터가 표시되는지 확인
- `Perfect`, `Good` 직후 두 번 점프하는 성공 클립이 재생되는지 확인
- `Miss` 직후 피격 클립과 `Hit_Effect`가 동시에 재생되는지 확인
- 각 반응 종료 후 `IDLE`로 복귀하는지 확인
- 빠른 연속 판정에서 이전 반응이 최신 반응을 덮어쓰지 않는지 확인
- 결과 팝업과 로비 복귀 흐름이 기존과 동일하게 동작하는지 회귀 확인

## 12. 완료 조건

- 매 스테이지 판정 직후 올바른 캐릭터 반응이 한 번 실행된다.
- 성공 시 신규 0.9초 두 번 점프 클립이 재생된다.
- 실패 시 기존 피격 클립과 타격 이펙트가 함께 재생된다.
- 반응 종료 후 캐릭터가 `IDLE`로 복귀한다.
- 연속 판정과 씬 종료 시 예외, 잔류 이펙트, 잘못된 지연 복귀가 발생하지 않는다.
- SPUM 서드파티 원본에는 변경이 없다.
- 자동 테스트와 Unity 런타임 검증 결과가 기록된다.
