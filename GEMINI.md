# [PROJECT MODULE: Gamify-KWU Strict Rules]

## 1. IDENTITY & MISSION
- **프로젝트 명**: Gamify-KWU (GameArifiction)
- **수석 개발자**: 윤승종 (작성자 명칭은 절대로 '에이전트'가 될 수 없음)
- **임무**: 본 프로젝트의 아키텍처와 코딩 표준을 엄격히 준수하여 상용급 게임 시스템을 구축함.

## 2. CORE CODING STANDARDS (최우선 적용)

### 2.1. COMMENTS & REGIONS (주석 및 리전)
- `#region`을 적극적으로 사용하여 코드 섹션을 명확히 구분하십시오.
- 기존 코드의 주석이나 리전 블록은 절대 삭제하지 말고 유지하십시오.

### 2.2. CONDITIONAL STATEMENTS & FORMATTING (조건문 및 포맷팅)
- **중괄호 { } 생략 절대 금지**: 모든 if, else 문에는 예외 없이 중괄호를 사용합니다.
- **Allman Style 준수**: 중괄호는 항상 줄 바꿈 후 작성합니다.
- **들여쓰기**: 4개의 공백(Space)을 사용합니다.

### 2.3. LOOP OPTIMIZATION (루프 최적화)
- **for 루프 권장**: `Update` 등 빈번한 호출 구간에서는 GC 할당을 최소화하기 위해 `foreach` 대신 `for` 루프를 사용하십시오.

### 2.4. THIRD-PARTY & PACKAGE POLICY (서드파티 보호)
- SPUM, DOTween, UPM 패키지 등 외부 스크립트 원본을 직접 수정하지 마십시오. 확장이 필요하면 상속이나 래퍼 패턴을 활용하십시오.

### 2.5. NAMING CONVENTIONS (명명 규칙)
- **Private Field**: `m_` 접두사 + camelCase (예: `m_playerData`)
- **Public Property/Method**: PascalCase (예: `PlayerLevel`)
- **Interface**: `I` 접두사 (예: `IViewModel`)
- **UI Event Callbacks**: UI 버튼 등에 연결되는 public 메서드는 반드시 `func_` 접두사를 붙이십시오. (예: `public void func_OnStartButtonClick()`)

### 2.6. ASYNC & TWEENING (비동기 및 연출)
- **UniTask**: 코루틴 대신 UniTask를 사용하며, CancellationToken을 필수적으로 고려하십시오.
- **DOTween**: 모든 트윈 연출은 DOTween을 사용하여 구현하십시오.

### 2.7. LOGGING RULE (로그 규칙)
- **한글 로그 필수**: 모든 로그는 `[클래스명]`을 포함하여 한글로 작성하십시오. (예: `Debug.Log("[PlayerController] 동작 완료");`)

### 2.8. UNITY SAFETY (유니티 안전성)
- **Fake Null 방지**: `UnityEngine.Object` 파생 타입에는 `?.` 또는 `??` 연산자를 사용하지 말고 `if (obj != null)`을 명시적으로 사용하십시오.

### 2.9. USING DIRECTIVES (using 지시문 활용)
- **using 적극 사용 및 코드 간결화**: 불필요하게 네임스페이스 경로 전체를 매번 명시하여 코드가 길어지고 가독성이 떨어지는 것을 적극 방지합니다. 파일 최상단에 `using` 지시문을 명시하고, 본문 코드 내에서는 클래스 이름 등을 최대한 간결하게 호출하십시오. (예: `UnityEngine.SceneManagement.SceneManager` -> 상단 `using UnityEngine.SceneManagement;` 추가 후 본문 내 `SceneManager`로 단축)

## 3. AUTHORSHIP
- **지침**: 코드 내 모든 작성자(Author) 및 마지막 수정자 명칭은 **'윤승종'**으로 기재합니다. 에이전트 명칭(예: Antigravity Agent)은 절대 사용하지 않습니다.
