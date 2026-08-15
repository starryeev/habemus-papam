# EventTrigger 기반 이벤트 흐름 개편 핸드오프

## 1. 목적

현재 `EventManager`에 섞여 있는 이벤트 발생 시점, 선행 조건, 충돌 조건과 확률 계산을 분리한다.

- `EventTrigger.cs`: 이벤트 발생 조건과 시점 규칙을 판정한다.
- `EventManager.cs`: 이벤트 목록, 런타임 상태, 분기, 난수 추첨과 실행 대기열을 관리한다.
- `InGameManager.cs`: 행동 위치에 진입할 때 이벤트 실행을 요청하고, 이벤트 종료 후 같은 위치의 플레이어 행동을 이어서 진행한다.
- 개별 `E#####.cs`: 현재처럼 선택지 조건과 실제 효과를 적용한다.
- `EventResult.cs`: 선택 결과를 `EventManager.RecordChoice()`에 전달한다.

별도 규칙 엔진이나 이벤트별 Trigger 하위 클래스를 만들지 않는다. 현재 규모에서는 하나의 규칙 테이블과 명시적인 런타임 상태가 가장 단순하다.

## 2. 확정된 시간 및 행동 모델

### 2.1 시간 축

이벤트 시점은 다음 네 값으로 식별한다.

```text
Day + Conclave + Turn(x) + ActionPosition(y)
```

- `Day`: 별도로 존재하는 일차.
- `Conclave`: 각 Day 안의 `Dawn`, `Morning`, `Afternoon`, `Evening`이다.
- `x`: 각 Conclave 안의 `CurrentTurn`이다.
- `y`: 각 Turn 안의 행동 위치다.
- 현재 코드의 `Day > Conclave > Turn > Action` 계층을 유지한다.
- UI 표기는 현재 형식인 `Turn x-y`를 유지한다.

각 Conclave 안의 기본 진행은 다음과 같다.

```text
1-1 → 1-2 → 1-3 → 1-4
2-1 → 2-2 → 2-3 → 2-4
3-1 → 3-2 → 3-3 → 3-4
4-1 → 4-2 → 4-3 → 4-4
→ 다음 Conclave의 1-1
```

마지막 `Evening 4-4`가 끝나면 다음 Day의 `Dawn 1-1`로 이동한다. `GameContext.Conclave`의 `Dawn`, `Morning`, `Afternoon`, `Evening` 명칭과 순서는 변경하지 않는다.

### 2.2 기본 행동 수와 증감

- 각 Turn의 기본 플레이어 행동 수는 4회다.
- 행동 증가 `+1`이면 해당 Turn이 `x-5`까지 확장된다.
- 행동 감소 `-1`이면 플레이어가 실제로 행동할 수 있는 위치는 `x-3`까지다.
- 행동 감소 상태에서도 기준 위치 `x-4`까지 시간은 진행한다.
- `x-4`에서는 이벤트 조건을 정상적으로 검사한다.
- 행동 가능 횟수를 넘은 위치는 플레이어 행동을 받지 않고 이벤트 처리 후 즉시 다음 Turn으로 넘어간다.
- 행동이 완전히 차단된 경우에도 `x-1~x-4` 이벤트 검사는 생략하지 않는다.

현재 위치에서 검사해야 하는 마지막 `y`는 다음과 같다.

```text
TriggerSpan = max(4, ActionsThisTurn)
```

행동 가능 여부는 별도로 판단한다.

```text
CanPlayerAct = ActionPosition <= ActionsThisTurn
```

예시:

```text
기본: 2-1, 2-2, 2-3, 2-4에서 이벤트 검사와 플레이어 행동
+1:   2-1~2-5에서 이벤트 검사와 플레이어 행동
-1:   2-1~2-3에서 이벤트 검사와 플레이어 행동
      2-4에서 이벤트만 검사한 뒤 3-1로 자동 이동
```

### 2.3 이벤트와 플레이어 행동의 순서

모든 위치에서 같은 규칙을 적용한다. `x-4`도 예외가 아니다.

```text
행동 위치 진입
→ 해당 위치 이벤트 조건 검사
→ 이벤트가 있으면 이벤트 실행
→ 연속 대기 이벤트가 있으면 모두 실행
→ 게임이 계속되고 해당 위치에서 행동 가능하면 같은 위치의 플레이어 행동 진행
→ 행동 완료 후 다음 행동 위치로 이동
```

예시:

```text
2-3 플레이어 행동 완료
→ 2-4 진입
→ 2-4 이벤트 발생 및 종료
→ 2-4 플레이어 행동 진행
→ 행동 완료
→ 3-1 진입
```

예외:

- 행동 감소로 `2-4`가 행동 불가 위치라면 이벤트 종료 후 플레이어 행동 없이 `3-1`로 이동한다.
- 이벤트 선택 결과로 엔딩이나 Turn/Conclave 강제 종료가 발생하면 같은 위치의 남은 플레이어 행동을 실행하지 않는다.
- `E11100 → E11200`처럼 같은 위치의 연속 이벤트는 모두 끝난 뒤 한 번만 플레이어 행동으로 복귀한다.

## 3. 현재 코드와 목표 모델의 차이

현재 `GameContext`에는 다음 상태가 있다.

- `CurrentDay`
- `CurrentConclave`
- `CurrentTurn`
- `CompletedActions`
- `ActionsThisTurn`
- `IsEventPhase`

현재 구현은 한 Conclave 안에서 `CurrentTurn`이 1~4로 진행되고 각 Turn마다 기본 행동을 2회 수행한 뒤 이벤트 단계로 들어간다. 이 시간 계층과 저장 구조는 유지하되, 기본 행동 수와 이벤트 검사 시점만 변경한다.

목표 상태:

- `CurrentDay`
- `CurrentConclave`: `Dawn`, `Morning`, `Afternoon`, `Evening`
- `CurrentTurn`: UI의 `x`
- `CompletedActions`: 현재 행동 진행값
- `ActionsThisTurn`: 현재 Turn에서 실제로 가능한 플레이어 행동 수
- `IsEventPhase`

`ActionPosition(y)`는 현재 행동을 실행하기 전에는 `CompletedActions + 1`이다. 행동 감소 때문에 실제 행동이 끝난 뒤에도 기준 위치 4까지 이벤트를 검사할 수 있도록, 이벤트 검사 완료 여부를 행동 완료 여부와 분리해 관리한다.

현재 `DisplayPhase`의 1~2/이벤트 3 표시는 제거한다.

```csharp
// 현재
public int DisplayPhase => isEventPhase ? 3 : Mathf.Clamp(completedActions + 1, 1, 2);

// 목표 의미
TurnX = CurrentTurn;
TurnY = CurrentActionPosition;
```

## 4. 책임 구조

### 4.1 `EventTrigger.cs`

발생 조건을 판정하는 순수 규칙 계층이다. 난수를 사용하거나 발생 상태를 직접 변경하지 않는다.

권장 최소 타입:

```csharp
public enum EventTier
{
    GuaranteedChain,
    GuaranteedStory,
    Story,
    Sub
}

public readonly struct EventTriggerContext
{
    public int Day { get; }
    public GameContext.Conclave Conclave { get; }
    public int Turn { get; }
    public int ActionPosition { get; }
}

public sealed class EventTrigger
{
    public string EventId { get; }
    public EventTier Tier { get; }

    public bool MatchesPosition(EventTriggerContext context);
    public bool IsEligible(EventTriggerContext context, EventManager eventManager);
    public float GetChance(EventTriggerContext context, EventManager eventManager);
}
```

트리거는 현재 `GameContext`에서 위 네 값만 읽는다. 시간 진행 방식이나 콘클라베 전환 방식은 소유하지 않는다.

### 4.2 `EventManager.cs`

다음을 관리한다.

- 이벤트 ID → `Event` 객체 조회
- 발생 완료 기록
- 선택지와 성공/실패 기록
- 해금 이벤트 집합
- 영구 차단 이벤트 집합
- 다음 행동 위치 확정 이벤트 대기열
- 이미 처리한 행동 위치 기록
- `E31100`/`E31200` 분기 승자 기록
- 확정 이벤트 우선 처리
- 확률형 스토리 추첨
- 서브 이벤트 균등 분배
- 이벤트 실행 대기열 반환
- 저장/복원

권장 호출 형태:

```csharp
public void BeginTriggerWindow(EventTriggerContext context);
public Event TryDequeueNextEvent();
public void RecordChoice(string eventId, int optionIndex, bool succeeded);
```

난수 추첨은 `EventManager` 한 곳에서만 실행한다.

### 4.3 `InGameManager.cs`

```text
행동 위치 진입
→ EventManager.BeginTriggerWindow(context)
→ EventManager.TryDequeueNextEvent()
→ 이벤트 UI 표시
→ 이벤트 종료 시 다음 대기 이벤트 확인
→ 대기열이 비면 같은 위치의 플레이어 행동 가능 여부 확인
→ 행동 가능: 플레이어 입력 대기
→ 행동 불가: 다음 위치로 자동 이동
```

동일 위치에서 `BeginTriggerWindow()`를 두 번 호출하지 않도록 처리 위치 키를 저장한다.

### 4.4 개별 `Event` 클래스

다음 책임은 유지한다.

- `CanChoiceOption1/2`
- 성공 확률 판정
- 능력치 변경
- 후보 탈락
- 아이템 지급
- 콘클라베 종료
- 엔딩 실행

발생 시점과 스토리 해금 조건은 개별 이벤트 클래스에 추가하지 않는다.

## 5. 공통 발생 규칙

### 5.1 우선순위

한 행동 위치에서 다음 순서로 처리한다.

1. 연속 확정 이벤트
2. 해금된 확정 후속 이벤트
3. 고정 시점 확정 스토리 이벤트
4. 확률형 스토리 이벤트
5. 서브 이벤트

- 확정 스토리 이벤트가 발생한 위치에는 확률형 스토리와 서브 이벤트가 발생하지 않는다.
- 확정 이벤트 때문에 확률형 이벤트의 지정 위치가 건너뛰어지면 실패 처리하지 않고 이후 지정 위치에서 다시 시도한다.
- 한 이벤트가 발생하면 같은 런에서 다시 발생하지 않는다.

### 5.2 확률형 스토리

현재 위치에서 조건을 만족한 스토리 이벤트의 원래 확률 합계를 계산한다.

합계가 100% 이하:

```text
각 스토리 이벤트 = 지정 확률 유지
서브 이벤트 전체 = 100% - 스토리 확률 합계
```

합계가 100% 초과:

```text
보정 확률 = 원래 확률 / 전체 원래 확률 합계
서브 이벤트 전체 = 0%
```

예시:

```text
E21000 = 40
E31000 = 70
E32000 = 30
합계 = 140

E21000 = 28.5714%
E31000 = 50%
E32000 = 21.4286%
```

### 5.3 서브 이벤트

대상:

```text
E40000~E40700
E50000~E50600
```

기존 `eventWeightBase`, `eventWeightMultiplier`는 폐기한다.

```text
개별 서브 이벤트 확률
= 남은 확률 / 현재 사용 가능한 서브 이벤트 수
```

- 한 번 발생한 서브 이벤트는 다음 계산부터 제거한다.
- 모든 이벤트는 런당 최대 1회다.
- 사용 가능한 서브 이벤트가 없으면 남은 확률은 아무 이벤트도 발생하지 않는 결과가 된다.

## 6. 이벤트 스케줄 및 분기

아래 모든 `x-y`는 `CurrentTurn-ActionPosition`이다. `Day`와 `Conclave`는 별도 상위 상태다.

트리거 위치 판정 규칙:

- `Day n, x-y`로 지정된 이벤트는 `CurrentDay == n`, `CurrentTurn == x`, `ActionPosition == y`를 검사한다.
- Conclave가 별도로 지정되지 않은 규칙은 해당 Day의 `Dawn`, `Morning`, `Afternoon`, `Evening` 모두에서 같은 `x-y`를 검사한다.
- Day가 명시되지 않은 `x-y` 규칙은 모든 Day와 모든 Conclave에서 같은 `x-y`를 검사한다.
- 이벤트가 이미 발생했다면 위치가 다시 일치해도 후보에서 제외한다.
- 확률형 이벤트가 실패했다면 다음 유효 위치에서 다시 추첨한다.

예를 들어 `Day 1, 3-2`는 Day 1의 각 Conclave에서 `Turn 3, Action 2`에 도달할 때 조건을 검사한다. 이미 이벤트가 발생했다면 이후 Conclave에서는 검사 결과가 비활성화된다.

### 6.1 튜토리얼

```text
Day 1, 1-1
E11100 100%
→ 종료 직후 같은 위치에서 E11200 100%
→ 두 이벤트 종료 후 1-1 플레이어 행동

Day 1, 1-2
E11300 100%

Day 1, 2-1
E12300 100%

Day 1, 2-2
E12100 100%

Day 1, 2-3
E12200 100%
```

- `E11200`의 기존 첫 공작 트리거는 제거한다.
- `E11100` 종료 후 같은 위치 실행 대기열에 `E11200`을 추가한다.

### 6.2 E20000/E21000/E21100/E21101

#### E20000

```text
Day 1
3-2, 3-4, 4-2, 4-4
각 위치 40%
```

- 한 번 발생하면 나머지 위치에서 0%.
- 발생 즉시 `E21000`을 영구 차단한다.

#### E21000

```text
Day 2
2-2, 2-4, 3-2, 3-4
각 위치 40%
```

- `E20000`이 발생한 런에서는 항상 0%.
- `E31213` 같은 확정 이벤트가 동일 위치를 차지하면 해당 위치 시도는 건너뛰고 이후 지정 위치에서 다시 시도한다.
- 한 번 발생하면 나머지 위치에서 0%.

#### E21100

```text
조건: E20000 선택지 1 선택
시점: Day 3, 1-1
확률: 100%
```

#### E21101

```text
조건: E21100 미발생
기간: Day 3, 1-1부터 Day 3 종료까지
각 유효 시도 위치: 20%
```

- 한 번 발생하면 이후 0%.
- `E21100`이 확정 발생하는 1-1에는 추첨하지 않는다.
- 행동 증가로 생기는 `x-5` 이상의 실제 행동 위치에서도 20% 추첨을 수행한다.

### 6.3 E30000/E31000/E31100/E31200

#### E30000

```text
Day 2
2-1, 2-3, 3-1, 3-3
각 위치 100%
```

- 첫 유효 위치에서 발생하면 이후 제거한다.
- 앞 위치가 더 높은 우선순위의 확정 이벤트로 건너뛰어지면 다음 지정 위치에서 다시 시도한다.

#### E31000

```text
해금: E30000 발생
시작: E30000 발생 위치의 다음 행동 위치
종료: Day 3의 마지막 처리 위치
각 유효 시도 위치: 70%
```

- 한 번 발생하면 이후 제거한다.
- 다른 확률형 스토리와 겹치면 공통 정규화 규칙을 사용한다.
- 행동 증가로 생기는 `x-5` 이상의 실제 행동 위치에서도 70% 추첨을 수행한다.

#### E31100/E31200

```text
공통 해금: E31000 발생

E31100 조건:
- 후보 3(NPC 목록 index 2) 탈락
- 조건을 처음 감지한 다음 행동 위치에 100%

E31200 조건:
- 후보 2 탈락
- 후보 1 생존
- 조건을 처음 감지한 다음 행동 위치에 100%
```

- 하나가 발생하는 순간 다른 이벤트를 영구 차단한다.
- 같은 위치에서 두 조건을 모두 만족하면 50:50으로 하나를 선택한다.
- 선택되지 않은 이벤트는 영구 차단한다.
- 확정 분기가 발생한 위치에는 서브 이벤트가 발생하지 않는다.

### 6.4 E31100/E31200 후속 분기

#### E31101

```text
조건: E31100 선택지 1 선택
시점: Day 4, 1-1
확률: 100%
```

#### E31210

```text
조건: E31200 선택지 1 선택
시점: Day 4, 2-2
확률: 100%
```

#### E31211

```text
조건: E31210 선택지 2 실패
시점: E31210 발생 위치의 다음 행동 위치
확률: 100%
```

예: `Day 4, 2-2`에서 E31210이 발생하면 E31211은 `Day 4, 2-3`에 발생한다.

### 6.5 E31212/E31213/E32000/E32001/E32002

#### E31212

```text
Day 1
4-1, 4-2, 4-3, 4-4
각 위치 30%
```

- 한 번 발생하면 이후 제거한다.
- 선택 결과와 관계없이 발생 사실로 `E31213`을 해금한다.
- 선택지 1 성공으로 엔딩에 진입하면 이후 분기는 실행하지 않는다.

#### E31213

```text
조건: E31212 발생 후 게임 계속
시점: Day 2, 2-2
확률: 100%
```

- 동일 위치의 `E21000` 시도보다 우선한다.
- 건너뛴 `E21000`은 다음 지정 위치에서 다시 시도한다.

#### E32000

```text
해금: E31213 선택지 2 선택
Day 2
3-1, 3-2, 3-3
각 위치 30%
```

- 한 번 발생하면 이후 제거한다.
- 선택지 1 실패 시 `E32001`을 해금한다.

#### E32001

```text
조건: E32000 선택지 1 실패
시점: E32000 발생 위치의 다음 행동 위치
확률: 100%
```

- 선택지 1로 게임이 계속될 때만 `E32002`를 다음 행동 위치에 예약한다.
- 선택지 2로 엔딩에 진입하면 예약된 `E32002`를 폐기한다.

#### E32002

```text
조건: E32001 선택지 1로 게임 계속
시점: E32001 발생 위치의 다음 행동 위치
확률: 100%
```

## 7. 다음 행동 위치 계산

현재 `GameContext`의 Turn 및 Conclave 진행을 유지하고, 현재 Turn의 실제 행동 수로 다음 위치를 계산한다.

```csharp
public readonly struct EventPosition
{
    public int Day { get; }
    public GameContext.Conclave Conclave { get; }
    public int Turn { get; }
    public int ActionPosition { get; }
}
```

진행 규칙:

```text
lastY = max(4, ActionsThisTurn)

현재 y < lastY
→ 같은 Turn의 y + 1

현재 y == lastY && Turn < 4
→ 같은 Conclave의 다음 Turn, x+1의 y=1

현재 y == lastY && Turn == 4 && Conclave가 Evening이 아님
→ 다음 Conclave의 Turn 1, y=1

현재 y == lastY && Turn == 4 && Conclave가 Evening
→ 다음 Day의 Dawn, Turn 1, y=1
```

행동 감소는 `lastY`를 4보다 작게 만들지 않는다. 행동 증가는 `lastY`를 5 이상으로 확장한다.

## 8. 저장 데이터 변경

기존 `GameContextSaveData` 시간 구조를 유지한다.

```csharp
public int day;
public int conclave;
public int currentTurn;
public int completedActions;
public int actionsThisTurn;
public bool isEventPhase;
```

시간 계층을 바꾸지 않으므로 기존 저장 구조를 폐기하거나 진행 중 저장을 강제로 무효화하지 않는다. 구버전 저장의 현재 Turn은 저장된 `completedActions/actionsThisTurn` 값으로 마무리하고, 다음 `BeginTurn()`부터 기본 행동 수 4를 적용한다. 새 이벤트 트리거 상태가 없는 저장은 기존 이벤트 발생/선택 기록을 기준으로 초기화한다.

`EventManagerSaveData.scheduleVersion`을 올리고 다음 데이터를 추가한다.

```csharp
public List<string> unlockedEventIds;
public List<string> permanentlyBlockedEventIds;
public List<PendingGuaranteedEventSaveData> pendingGuaranteedEvents;
public List<string> resolvedTriggerPositions;
public string exclusiveBranchEventId;
```

```csharp
[Serializable]
public class PendingGuaranteedEventSaveData
{
    public string eventId;
    public int day;
    public int conclave;
    public int turn;
    public int actionPosition;
}
```

저장 후 복원 검증 대상:

- E11100 종료 후 같은 위치 E11200 대기 상태
- E31210 선택지 2 실패 후 E31211 대기 상태
- E32000 실패 후 E32001 대기 상태
- E32001 선택지 1 후 E32002 대기 상태
- E31100/E31200 분기 승자 및 패자 차단 상태
- 현재 위치의 확률 추첨 중복 방지 상태

## 9. 씬 및 이벤트 등록

`GameScene`의 `EventManager.allEvents`에 정식 이벤트 SO 37개를 모두 등록한다.

- 개발용 템플릿이며 SO가 없는 `E00000`은 제외한다.
- 이벤트 ID 중복과 `null` 참조가 없어야 한다.
- 37개 ID를 `GetEventById()`로 모두 조회할 수 있어야 한다.

## 10. 파일별 변경 범위

### `EventManager.cs`

교체 대상:

- `RequiredPreEventIds`
- `ConflictEventIds`
- `ScheduledTurnSlots`
- `GetScheduledChance()`
- `NarrativeConditionSatisfied()` 이벤트별 `switch`
- 기존 Turn 기반 스케줄 슬롯 인코딩
- 기존 `attemptedScheduledSlots`

유지 대상:

- 이벤트 조회
- 발생 및 선택 기록
- 공작 피해 보너스
- 기도/연설 확정 성공
- 공작 경건함 면제
- 저장/복원 골격

### `PlotManager.cs`

E11200을 별도 호출하는 기존 첫 공작 트리거를 제거한다.

### `InGameManager.cs` / `GameContext`

- 기존 `Day + Conclave + CurrentTurn + 행동` 시간 계층을 유지한다.
- 기본 행동 수를 4로 변경한다.
- 행동 증가 시 `y` 최대값을 확장한다.
- 행동 감소 시 기준 `y=4`까지 이벤트 검사만 계속한다.
- 모든 위치 진입 시 이벤트를 먼저 요청한다.
- 이벤트 종료 후 같은 위치의 플레이어 행동으로 복귀한다.
- 이벤트가 엔딩/콘클라베 종료를 실행하면 같은 위치 행동을 취소한다.
- 저장/복원 시 현재 행동 위치와 대기 이벤트를 복원한다.

### `TimeUI.cs`

표시 형식은 유지한다.

```text
Turn {CurrentTurn}-{ActionPosition}
```

- `DisplayPhase`의 1~2/이벤트 3 표현을 제거한다.
- 행동 증가 시 `Turn 2-5`처럼 확장된 위치도 그대로 표시한다.
- 행동 감소로 플레이어 행동이 없는 `x-4`도 이벤트 검사 중에는 해당 위치를 표시한다.

## 11. 구현 순서

1. 문서 마지막의 미확정 질문을 확정한다.
2. 기존 `GameContext` 시간 계층을 유지하면서 기본 행동 수와 위치별 이벤트 검사 상태를 변경한다.
3. 행동 수 증감과 다음 위치 계산을 변경한다.
4. `TimeUI`가 실제 `CurrentTurn-ActionPosition`을 표시하도록 변경한다.
5. `EventTrigger.cs`와 규칙 테이블을 추가한다.
6. `EventManager`에 상태, 우선순위, 확률과 대기열을 구현한다.
7. `InGameManager` 위치 진입/이벤트/행동 흐름을 연결한다.
8. `EventResult.RecordChoice()` 이후 분기 해금과 예약을 연결한다.
9. `PlotManager`의 E11200 예외 트리거를 제거한다.
10. 저장 모델 버전과 복원 로직을 확장한다.
11. `GameScene.allEvents`에 37개 SO를 등록한다.
12. 컴파일과 최소 런타임 검증을 수행한다.

## 12. 최소 검증 시나리오

### 시간과 행동

- 기본 행동: `1-1 → 1-2 → 1-3 → 1-4 → 2-1`.
- 행동 +1: `1-4 → 1-5 → 2-1`.
- 행동 -1: `1-3` 행동 후 `1-4` 이벤트 검사, 플레이어 행동 없이 `2-1`.
- 각 Conclave의 `4-4` 또는 확장된 마지막 위치 후 다음 Conclave의 `1-1`.
- `Evening 4-4` 또는 확장된 마지막 위치 후 다음 Day의 `Dawn 1-1`.
- 이벤트가 있는 `2-4`에서 이벤트 종료 후 같은 위치 플레이어 행동 실행.
- 이벤트가 콘클라베를 끝냈다면 같은 위치 플레이어 행동 미실행.
- 저장/불러오기 후 같은 위치와 행동 가능 횟수 복원.

### 확정 이벤트

- Day 1의 1-1에서 E11100 종료 직후 E11200 발생 후 1-1 행동으로 복귀.
- Day 1의 1-2에서 E11300 발생.
- 확정 이벤트 위치에 확률형/서브 이벤트가 나오지 않음.

### 확률과 분기

- E20000 발생 후 나머지 E20000 시도와 E21000 제거.
- E20000 미발생 시 E21000 지정 위치 재시도.
- 확률 합이 100%를 넘으면 비례 정규화.
- 남은 확률을 사용 가능한 서브 이벤트에 균등 분배.
- 발생한 서브 이벤트가 다시 후보에 들어오지 않음.
- 후보 3만 탈락하면 다음 행동에 E31100.
- 후보 2 탈락 및 후보 1 생존이면 다음 행동에 E31200.
- 동시 조건에서 50:50 선택 후 패자 영구 차단.
- E31210 선택지 2 실패 후 다음 행동 E31211.
- E32000 선택지 1 실패 후 다음 행동 E32001.
- E32001 선택지 1 후 다음 행동 E32002.
- E32001 선택지 2 엔딩 후 E32002 대기열 폐기.

## 13. 확정된 추가 규칙

### 13.1 시간 구조 유지

- 기존 `Day > Conclave > CurrentTurn > Action` 구조를 변경하지 않는다.
- Conclave 명칭은 `Dawn`, `Morning`, `Afternoon`, `Evening`을 유지한다.
- `x-y`에서 `x`는 `CurrentTurn`, `y`는 행동 위치다.
- 시간 저장 필드도 유지하고 이벤트 트리거 런타임 상태만 추가 저장한다.

### 13.2 확장 행동 위치

- E31000 70%, E21101 20%, 서브 이벤트는 `x-5` 이상의 확장 위치에서도 검사한다.
- E20000, E21000, E31212, E32000처럼 정확한 위치 목록이 있는 이벤트는 목록에 없는 확장 위치에서 검사하지 않는다.

### 13.3 행동 증감 누적

```text
ActionsThisTurn = max(0, 4 + 누적 증감치)
```

- 누적 `+2`면 해당 Turn은 `x-6`까지 확장한다.
- 누적 `-2`면 실제 플레이어 행동은 2회다.
- 행동 수가 4보다 적어도 `x-4`까지 이벤트를 검사한다.

### 13.4 위치 조건의 범위

- Day와 `x-y`가 명시되면 해당 Day의 모든 Conclave에서 일치 여부를 검사한다.
- Day가 명시되지 않은 `x-y` 조건은 모든 Day와 모든 Conclave에서 검사한다.
- 이미 발생한 이벤트는 모든 이후 검사에서 제외한다.

## 14. 완료 조건

- UI가 `Day`와 실제 `Turn x-y`를 일치하게 표시한다.
- `x`가 `CurrentTurn`, `y`가 행동 위치로만 사용된다.
- 모든 위치 진입 시 이벤트 조건을 정확히 한 번 계산한다.
- 이벤트 종료 후 같은 위치의 플레이어 행동을 진행한다.
- 행동 감소 위치에서도 기준 `y=4`까지 이벤트 검사가 유지된다.
- 행동 증가 시 `y=5` 이상으로 시간과 UI가 확장된다.
- 확정 스토리가 있는 위치에는 다른 이벤트가 나오지 않는다.
- 확률형 스토리와 서브 이벤트 합계가 100% 이하가 된다.
- 모든 정식 이벤트가 씬에 등록된다.
- 모든 이벤트가 런당 최대 1회 발생한다.
- 분기 해금, 영구 차단과 다음 행동 이벤트가 저장 후 유지된다.
- 기존 개별 이벤트 효과는 유지된다.
- 컴파일 오류가 없다.
