# Gamify-KWU (광운대학교 게임화개론 팀 프로젝트)

본 프로젝트는 **광운대학교 게임화개론** 수업의 팀 프로젝트 결과물로, 대학 생활 속 다양한 요소를 게임화(Gamification)하여 흥미를 유발하는 여러 미니게임과 캠퍼스 맵 시스템을 제공합니다.

---

## 1. 아키텍처 및 설계 원칙

본 프로젝트의 모든 기능 및 인게임 씬은 Unity Technologies의 'Clean Code' 철학과 GoF 디자인 패턴을 응용하여 설계되었으며, **유지보수성(Maintainability)**과 **성능 최적화(Optimization)**를 최우선으로 합니다.

### MVVM (Model-View-ViewModel) 패턴
UI와 인게임 코어 로직의 분리를 위해 엄격한 MVVM 아키텍처를 적용합니다.
* **Model**: 비즈니스 데이터 및 순수 상태 정보 데이터 클래스 (Pure C# POCO). Unity API 및 View에 대한 의존성이 전혀 없습니다.
* **ViewModel**: View가 구독할 상태(State)와 실행 명령(Command)을 담당합니다. `Action` 및 이벤트를 활용해 상태 전파를 통제합니다.
* **View**: `MonoBehaviour`를 상속하며, 사용자의 입력(Input) 전달 및 상태 변화에 따른 시각화(Rendering) 역할만을 철저히 수행합니다.

### SOLID 원칙 & 의존성 주입 (DI)
* **결합도 분리 (Decoupling)**: 인게임 물리 연산, 타이머 루프, 상태 계산 및 룰셋 통제 등 비즈니스 로직은 `MonoBehaviour`를 제외한 일반 C# 클래스로 작성하고, 의존성 관계는 인터페이스(`IViewModel` 등)를 통해 주입받아 모듈식 테스트가 가능하도록 설계했습니다.
* **싱글톤(Singleton) 사용 제한**: 전역 의존성과 데이터 추적의 불확실성을 유발하는 전통적인 싱글톤 사용을 배제하며, 데이터 흐름과 전환은 DTO(Data Transfer Object)와 초기화 기기(Initializer)를 활용해 명시적으로 흐르게 합니다.

---

## 2. 파일 및 폴더 계층 구조 (Directory Structure)

`Assets/_Game` 폴더 하위에 계층 구조를 명확히 설계하여 에셋 및 스크립트 관리의 가독성을 대폭 향상했습니다.

```
Assets/
├── _Game/
│   ├── Art/                              # 아트 리소스 총괄
│   │   ├── Fonts/                        # 프로젝트 내 모든 폰트 통합 (Galmuri, BMJUA 등)
│   │   ├── Sprites/                      # 스프라이트 이미지 분류 리소스
│   │   │   ├── GradeRunner/              # 2D 피하기 미니게임 전용 이미지 리소스
│   │   │   ├── ClawMachine/              # 인형 뽑기 미니게임 전용 이미지 리소스
│   │   │   └── Lobby/                    # 로비 및 캠퍼스 맵 전용 스프라이트
│   │   └── PhysicsMaterials/             # 2D/3D 물리 머티리얼 리소스
│   │
│   ├── Prefabs/                          # 모든 프리팹 에셋 총괄
│   │   ├── GradeRunner/                  # 2D 피하기 게임용 장애물, 족보, UI 프리팹
│   │   ├── Player/                       # 플레이어 캐릭터 및 SPUM 프리팹
│   │   └── UI/                           # 공용 TMP 텍스트 및 UI 요소 프리팹
│   │
│   ├── ScriptableObjects/                # 게임 데이터 및 환경 설정 SO 에셋 인스턴스
│   │   ├── GradeRunner/                  # 피하기 게임 설정(속도, 점수, 대사 SO)
│   │   ├── Player/                       # 플레이어 기본 속성 및 영구 저장용 SO
│   │   └── Quiz/                         # 퀴즈 데이터베이스 SO 인스턴스
│   │
│   ├── Scenes/                           # 게임 내 주요 씬
│   │   ├── Lobby.unity                   # 메인 로비 및 월드 맵 씬
│   │   ├── CraneGame.unity               # 인형 뽑기 미니게임 씬
│   │   └── GradeRunner.unity             # 학점 피하기 미니게임 씬
│   │
│   ├── Scripts/                          # C# 스크립트 총괄 (기능별 MVVM 구조 적용)
│   │   ├── GamifyKWU.Runtime.asmdef      # 프로젝트 조립 컴파일 모듈
│   │   ├── Camera/                       # 카메라 트래킹 및 효과 스크립트
│   │   │   └── CameraFollow.cs
│   │   ├── ClawMachine/                  # 인형 뽑기 미니게임 비즈니스 로직 및 뷰
│   │   ├── GradeRunner/                  # 학점 피하기 미니게임 로직 및 뷰
│   │   ├── Interaction/                  # 상호작용 가능한 월드 오브젝트 정의
│   │   ├── Map/                          # 캠퍼스 포탈 및 월드 맵 트리거 제어
│   │   ├── Player/                       # 플레이어 동작 물리 제어 및 영구 스탯
│   │   ├── QuizClassic/                  # 고전 퀴즈 풀이 기능 및 타이머 제어
│   │   └── UI/                           # 메인 타이틀, 아웃 게임 UI 로직
│   │
│   ├── Editor/                           # 에디터 폴더 정리기 및 빌드 자동화 스크립트
│   │   └── ProjectFolderOrganizer.cs
│   │
│   └── Tests/                            # 유닛 테스트 및 통합 테스트 검증 슈트
│       ├── Editor/                       # 에디터 환경에서의 유닛 테스트 코드
│       └── PlayMode/                     # 플레이모드 환경에서의 러닝 타임 테스트 코드
│
├── EasyTransitions/                      # [서드파티] 화면 전환 연출 플러그인
├── Plugins/                              # [서드파티] 멀티플랫폼 및 외부 네트워크 라이브러리
├── SPUM/                                 # [서드파티] 2D 캐릭터 픽셀 스프라이트 크리에이터
├── TextMesh Pro/                         # [서드파티] 고해상도 폰트 SDF 리소그래피 시스템
└── VirtualJoystick/                      # [서드파티] 모바일 화면 조이스틱 입력 모듈
```

---

## 3. 핵심 개발 표준 및 네이밍 룰

### 네이밍 규칙 (Naming Conventions)
* **클래스 및 메서드**: `PascalCase` 사용 (예: `PlayerController`, `StartGame()`)
* **공개 프로퍼티**: `PascalCase` 사용 (예: `int CurrentScore { get; }`)
* **Private 필드**: `m_` 접두사 + `camelCase` 사용 (예: `private float m_moveSpeed;`)
* **인터페이스**: `I` 접두사 + `PascalCase` 사용 (예: `interface IDamageable`)
* **UI 버튼 및 인터랙션 이벤트 핸들러**: Inspector에 할당할 목적으로 작성하는 이벤트 리스너 메서드는 반드시 **`func_` 접두사**를 붙여 명명합니다. (예: `public void func_OnExitButtonClicked()`)

### 코드 포맷팅 & 성능 안전 규칙
1. ** Allman Style**: 여는 중괄호 `{`와 닫는 중괄호 `}`는 항상 독단적인 행으로 줄 바꿈하여 작성합니다.
2. **단일 조건 행 중괄호**: `if (condition)` 문에 속한 실행문이 단 한 줄이더라도 반드시 중괄호 `{ }`를 예외 없이 감싸서 작성합니다.
3. **루프 최적화 (For Loop)**: `Update()` 등 매 프레임 발생하는 고주기 루프나 크기가 유의미한 컬렉션 순회 시, 불필요한 GC Allocation 및 이터레이션 오버헤드를 막기 위해 `foreach` 대신 반드시 인덱싱 기반의 **`for` 루프**를 적극 활용합니다.
4. **Unity Fake Null 안전성**: `UnityEngine.Object`를 상속받은 모든 클래스 객체(예: `GameObject`, `Transform`, `MonoBehaviour` 등)는 C# 널 조건부 연산자(`?.` 또는 `??`)를 사용할 경우 Unity 내부 C++ 래퍼 포인터 누수 및 널 체크 오류가 야기될 수 있습니다. 널 체크는 반드시 명시적인 비교 구문 `if (obj != null)` 만을 사용합니다.
5. **비동기 최적화**: 코루틴(Coroutine)의 리소스 누수와 할당을 방지하기 위해 가벼우면서도 안전한 **`UniTask`**를 적극 도입해 구현합니다.

---

## 4. 디버그 로그 정책

디버그 로그 작성 시 다음 규칙을 절대 준수합니다.
* **한국어(Korean)** 언어로만 디버그 메시지를 간단명료하게 작성합니다.
* 어떤 컴포넌트나 매니저로부터 출력되었는지 명확한 디바이스 출처 정보를 표기합니다.
* 형식: `Debug.Log($"[클래스명] 구체적인 상황 묘사: {관련 변수}");`
* 예시: `Debug.Log($"[GradeRunnerViewModel] 피하기 미니게임이 정상 종료되었습니다. 최종 학점: {finalPoint}");`


