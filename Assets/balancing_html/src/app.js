const STAT_KEYS = ["health", "piety", "politics"];
const STAT_LABELS = {
  health: "체력",
  piety: "경건",
  politics: "정치",
};

const statAliases = [
  { pattern: /체력/g, key: "health" },
  { pattern: /경건함|경건/g, key: "piety" },
  { pattern: /정치력|정치/g, key: "politics" },
];

const ACTIONS = [
  {
    id: "pray",
    label: "기도: 자신의 체력과 경건 +1",
    target: "self",
    apply(state, actor, target) {
      changeActionStat(actor, "health", 1);
      changeActionStat(actor, "piety", 1);
      return `${actor.name}이(가) 기도하여 체력과 경건을 회복했다.`;
    },
  },
  {
    id: "speech",
    label: "연설: 대상의 정치 +1",
    target: "any",
    apply(state, actor, target) {
      changeActionStat(target, "politics", 1);
      return `${actor.name}이(가) ${target.name}에게 연설했다. ${target.name}의 정치가 상승했다.`;
    },
  },
  {
    id: "scheme",
    label: "공작: 대상의 정치 -1, 실행자 경건 -1",
    target: "other",
    apply(state, actor, target) {
      changeActionStat(target, "politics", -1);
      if (!state.freeSchemeCost) changeStat(actor, "piety", -1);
      return `${actor.name}이(가) ${target.name}에게 공작을 실행했다. 대상의 정치가 낮아지고${state.freeSchemeCost ? " 경건 소모는 면제됐다" : " 실행자의 경건이 소모됐다"}.`;
    },
  },
  {
    id: "rest",
    label: "휴식: 자신의 체력 +2",
    target: "self",
    apply(state, actor, target) {
      changeActionStat(actor, "health", 2);
      return `${actor.name}이(가) 휴식하여 체력을 크게 회복했다.`;
    },
  },
];

const EVENT_DECK = [
  {
    id: "12100",
    title: "태양의 눈",
    description: "태양의 시선을 받는 순간이다. 안정적인 정치력 상승과 위험한 대가 중 하나를 선택한다.",
    choices: [
      { label: "태양의 눈을 받아들인다", effects: ["정치력 +5"] },
      { label: "눈부심을 견딘다", effects: ["체력 -10"] },
    ],
  },
  {
    id: "12200",
    title: "태양의 발",
    description: "빠르게 움직일 기회가 왔다. 경건함을 얻거나 체력 손실을 감수한다.",
    choices: [
      { label: "성스러운 걸음을 따른다", effects: ["경건함 +10"] },
      { label: "무리해서 따라간다", effects: ["체력 -10"] },
    ],
  },
  {
    id: "12300",
    title: "태양의 입",
    description: "말과 신앙 중 어디에 힘을 실을지 선택한다.",
    choices: [
      { label: "체력을 다진다", effects: ["체력 +10"] },
      { label: "경건함을 드러낸다", effects: ["경건함 +20"] },
    ],
  },
  {
    id: "21000",
    title: "과학적 발견",
    description: "새 발견이 교단을 흔든다.",
    choices: [
      { label: "신앙으로 해석한다", effects: ["정치력 -10", "경건함 +10"] },
      { label: "성과로 포장한다", effects: ["정치력 +10"] },
    ],
  },
  {
    id: "30000",
    title: "새로운 전례",
    description: "전례 개편 논의가 시작됐다.",
    choices: [
      { label: "준비 시간을 줄인다", effects: ["체력 +30"] },
      { label: "즉시 판정으로 보낸다", effects: ["진행 중인 콘클라베 종료", "즉시 투표 및 판정 실시"] },
    ],
  },
  {
    id: "40000",
    title: "산책",
    description: "짧은 산책이 컨디션을 바꾼다.",
    choices: [
      { label: "천천히 걷는다", effects: ["체력 +15", "경건함 +5"] },
      { label: "길을 서두른다", effects: ["체력 -10"] },
    ],
  },
  {
    id: "40100",
    title: "소똥구리 토론",
    description: "기묘한 토론이 후보들의 태도를 바꾼다.",
    choices: [
      { label: "논쟁에 참여한다", effects: ["체력 +10", "정치력 +10"] },
      { label: "경건한 침묵을 지킨다", effects: ["경건함 +30"] },
    ],
  },
  {
    id: "40200",
    title: "고장난 문",
    description: "문제가 생긴 문 앞에서 체면과 체력을 저울질한다.",
    choices: [
      { label: "억지로 연다", effects: ["체력 +5", "정치력 -15"] },
      { label: "사람을 부른다", effects: ["정치력 -5"] },
    ],
  },
  {
    id: "40400",
    title: "호우주의보",
    description: "비가 쏟아지며 모두의 일정이 느슨해진다.",
    choices: [
      { label: "비를 받아들인다", effects: ["모든 후보의 체력 +20"] },
      { label: "현 상태를 유지한다", effects: ["-"] },
    ],
  },
  {
    id: "40500",
    title: "주화입마",
    description: "무리한 집중의 반동이 찾아온다.",
    choices: [
      { label: "버틴다", effects: ["정치력 -15", "경건함 -10"] },
      { label: "흐름을 이용한다", effects: ["정치력 +5"] },
    ],
  },
  {
    id: "40600",
    title: "운수 좋은 날",
    description: "오늘은 이상하리만큼 몸이 가볍다.",
    choices: [
      { label: "기세를 탄다", effects: ["체력 +40"] },
      { label: "아껴 둔다", effects: ["-"] },
    ],
  },
  {
    id: "40700",
    title: "묵주닦이",
    description: "사소한 노동이 생각보다 큰 영향을 준다.",
    choices: [
      { label: "끝까지 닦는다", effects: ["체력 +100"] },
      { label: "무리하지 않는다", effects: ["체력 -20"] },
    ],
  },
  {
    id: "50400",
    title: "삼위일체",
    description: "세 능력의 균형이 시험대에 오른다.",
    choices: [
      { label: "균형을 맞춘다", effects: ["체력 -33", "정치력 +3", "경건함 +3"] },
      { label: "넘긴다", effects: ["-"] },
    ],
  },
  {
    id: "50500",
    title: "이 불경한 자가",
    description: "강한 질책이 내려온다.",
    choices: [
      { label: "감수한다", effects: ["체력 -40"] },
      { label: "침묵한다", effects: ["-"] },
    ],
  },
  {
    id: "50600",
    title: "결코 다시 전쟁!",
    description: "후보들의 정치적 입장이 크게 요동친다.",
    choices: [
      { label: "모두를 설득한다", effects: ["모든 후보의 정치력 +40"] },
      { label: "공작을 봉인한다", effects: ["이번 콘클라베 동안 공작 경건함 소모량 0으로 고정"] },
    ],
  },
];

const defaultCandidates = [
  { id: "player", name: "플레이어", role: "player", stats: { health: 8, piety: 5, politics: 4 } },
  { id: "npc1", name: "NPC 1", role: "npc", stats: { health: 7, piety: 6, politics: 5 } },
  { id: "npc2", name: "NPC 2", role: "npc", stats: { health: 7, piety: 4, politics: 7 } },
  { id: "npc3", name: "NPC 3", role: "npc", stats: { health: 9, piety: 3, politics: 3 } },
];

let state = null;

const els = {
  phaseSummary: document.getElementById("phaseSummary"),
  conclaveTurns: document.getElementById("conclaveTurns"),
  decayEvery: document.getElementById("decayEvery"),
  decayAmount: document.getElementById("decayAmount"),
  candidateEditor: document.getElementById("candidateEditor"),
  candidateGrid: document.getElementById("candidateGrid"),
  actionSelect: document.getElementById("actionSelect"),
  targetSelect: document.getElementById("targetSelect"),
  applyActionButton: document.getElementById("applyActionButton"),
  resolveEventButton: document.getElementById("resolveEventButton"),
  resetButton: document.getElementById("resetButton"),
  clearLogButton: document.getElementById("clearLogButton"),
  actionCounter: document.getElementById("actionCounter"),
  eventBox: document.getElementById("eventBox"),
  logList: document.getElementById("logList"),
};

function clampStat(value) {
  return Math.max(0, Math.min(10, Math.round(Number(value) || 0)));
}

function scaleFromHundred(value) {
  return Math.round(Number(value) / 10);
}

function changeStat(candidate, key, delta) {
  candidate.stats[key] = clampStat(candidate.stats[key] + delta);
}

function adjustActionDelta(delta) {
  const modifier = state?.nextActionEffectModifier || 0;
  if (!modifier || !delta) return delta;
  if (delta > 0) return Math.max(0, delta + modifier);
  return Math.min(0, delta - modifier);
}

function changeActionStat(candidate, key, baseDelta) {
  changeStat(candidate, key, adjustActionDelta(baseDelta));
}

function consumeNextActionEffectModifier() {
  const modifier = state.nextActionEffectModifier;
  state.nextActionEffectModifier = 0;
  if (!modifier) return null;
  return `이동 속도 보정이 다음 행동에 적용됐다: 행동 효과 ${modifier > 0 ? `+${modifier}` : modifier}.`;
}

function activeCandidates(targetState = state) {
  return targetState.candidates.filter((candidate) => candidate.stats.health > 0);
}

function strongestActive(targetState, key) {
  return [...activeCandidates(targetState)].sort((a, b) => b.stats[key] - a.stats[key] || a.name.localeCompare(b.name, "ko"))[0];
}

function readConfig() {
  const candidates = defaultCandidates.map((candidate) => {
    const stats = {};
    for (const key of STAT_KEYS) {
      const input = document.querySelector(`[data-candidate="${candidate.id}"][data-stat="${key}"]`);
      stats[key] = clampStat(input?.value ?? candidate.stats[key]);
    }
    const nameInput = document.querySelector(`[data-candidate="${candidate.id}"][data-field="name"]`);
    return {
      ...candidate,
      name: String(nameInput?.value || candidate.name).trim() || candidate.name,
      stats,
    };
  });

  return {
    conclaveTurns: Math.max(1, Math.round(Number(els.conclaveTurns.value) || 4)),
    decayEvery: Math.max(1, Math.round(Number(els.decayEvery.value) || 1)),
    decayAmount: clampStat(els.decayAmount.value),
    candidates,
  };
}

function createInitialState() {
  const config = readConfig();
  return {
    config,
    candidates: config.candidates.map((candidate) => ({
      ...candidate,
      stats: { ...candidate.stats },
    })),
    conclaveTurn: 1,
    actionCount: 0,
    eventResolved: false,
    pendingEvent: null,
    completed: false,
    freeSchemeCost: false,
    nextActionEffectModifier: 0,
    log: [],
  };
}

function renderCandidateEditor() {
  els.candidateEditor.innerHTML = defaultCandidates.map((candidate) => `
    <article class="editor-card">
      <div class="editor-title">
        <span>${candidate.role === "player" ? "플레이어" : candidate.name}</span>
        <input data-candidate="${candidate.id}" data-field="name" value="${candidate.name}" aria-label="${candidate.name} 이름" />
      </div>
      <div class="editor-grid">
        ${STAT_KEYS.map((key) => `
          <label>
            <span>${STAT_LABELS[key]}</span>
            <input data-candidate="${candidate.id}" data-stat="${key}" type="number" min="0" max="10" step="1" value="${candidate.stats[key]}" />
          </label>
        `).join("")}
      </div>
    </article>
  `).join("");
}

function renderActionOptions() {
  els.actionSelect.innerHTML = ACTIONS.map((action) => `<option value="${action.id}">${action.label}</option>`).join("");
  updateTargetOptions();
}

function updateTargetOptions() {
  const action = ACTIONS.find((item) => item.id === els.actionSelect.value);
  const player = state?.candidates.find((candidate) => candidate.role === "player");
  const candidates = activeCandidates();
  const targets = candidates.filter((candidate) => {
    if (!action || action.target === "any") return true;
    if (action.target === "self") return candidate.id === player?.id;
    if (action.target === "other") return candidate.id !== player?.id;
    return true;
  });

  els.targetSelect.innerHTML = targets.map((candidate) => `<option value="${candidate.id}">${candidate.name}</option>`).join("");
}

function renderCandidates() {
  els.candidateGrid.innerHTML = state.candidates.map((candidate) => {
    const isOut = candidate.stats.health <= 0;
    return `
      <article class="candidate-card ${candidate.role === "player" ? "player" : ""} ${isOut ? "out" : ""}">
        <div class="candidate-name">
          <span>${candidate.name}</span>
          <span class="badge">${candidate.role === "player" ? "조작" : "NPC"}${isOut ? " / 탈락" : ""}</span>
        </div>
        ${STAT_KEYS.map((key) => {
          const value = candidate.stats[key];
          const level = value <= 3 ? "low" : value <= 6 ? "mid" : "";
          return `
            <div class="stat-row">
              <span>${STAT_LABELS[key]}</span>
              <div class="meter" aria-hidden="true"><div class="meter-fill ${level}" style="width: ${value * 10}%"></div></div>
              <strong>${value}</strong>
            </div>
          `;
        }).join("")}
      </article>
    `;
  }).join("");
}

function renderLog() {
  els.logList.innerHTML = state.log.map((item) => `<li>${item}</li>`).join("");
  els.logList.scrollTop = els.logList.scrollHeight;
}

function renderPhase() {
  const phase = state.completed
    ? "완료"
    : state.pendingEvent
      ? "이벤트 선택 중"
      : state.actionCount < 2
        ? `행동 ${state.actionCount + 1}/2 선택 중`
        : "이벤트 대기";
  els.phaseSummary.textContent = `턴 ${state.conclaveTurn}/${state.config.conclaveTurns} · ${phase}`;
  els.actionCounter.textContent = `행동 ${state.actionCount} / 2`;
  els.applyActionButton.disabled = state.completed || state.pendingEvent || state.actionCount >= 2 || !activeCandidates().some((candidate) => candidate.role === "player");
  els.resolveEventButton.disabled = state.completed || state.pendingEvent || state.actionCount < 2 || state.eventResolved;
}

function renderEventPrompt() {
  if (!state.pendingEvent) return;
  const event = state.pendingEvent;
  els.eventBox.innerHTML = `
    <div class="event-title">${event.title}</div>
    <p>${event.description}</p>
    <div class="event-choice-grid">
      ${event.choices.map((choice, index) => `
        <button type="button" class="event-choice" data-event-choice="${index}">
          <strong>${choice.label}</strong>
          <span>${choice.effects.join(" / ")}</span>
        </button>
      `).join("")}
    </div>
  `;
}

function render() {
  renderCandidates();
  updateTargetOptions();
  renderPhase();
  renderLog();
  if (state.pendingEvent) renderEventPrompt();
}

function addLog(message) {
  state.log.push(`[턴 ${state.conclaveTurn}] ${message}`);
}

function applyAction() {
  if (state.completed || state.pendingEvent || state.actionCount >= 2) return;
  const action = ACTIONS.find((item) => item.id === els.actionSelect.value);
  const actor = state.candidates.find((candidate) => candidate.role === "player");
  const target = state.candidates.find((candidate) => candidate.id === els.targetSelect.value);
  if (!action || !actor || !target || actor.stats.health <= 0) return;

  const message = action.apply(state, actor, target);
  const modifierMessage = consumeNextActionEffectModifier();
  state.actionCount += 1;
  state.eventResolved = false;
  addLog(message);
  if (modifierMessage) addLog(modifierMessage);
  if (state.actionCount === 2) els.eventBox.textContent = "행동 2회가 끝났습니다. 이벤트를 확인하세요.";
  render();
}

function currentEvent() {
  return EVENT_DECK[(state.conclaveTurn - 1) % EVENT_DECK.length];
}

function showEvent() {
  if (state.completed || state.actionCount < 2 || state.eventResolved) return;
  state.pendingEvent = currentEvent();
  addLog(`${state.pendingEvent.title} 이벤트 발생.`);
  render();
}

function resolveEffectTarget(effectText) {
  if (/모든 후보|플레이어와 후보|후보 1, 2, 3/.test(effectText)) return activeCandidates();
  const npcMatch = effectText.match(/후보\s*(\d)/);
  if (npcMatch) {
    const candidate = state.candidates[Number(npcMatch[1])];
    return candidate ? [candidate] : [];
  }
  return [state.candidates.find((candidate) => candidate.role === "player")];
}

function applyNumericEffect(effectText) {
  let applied = false;
  const targets = resolveEffectTarget(effectText);

  if (/부활/.test(effectText)) {
    const valueMatch = effectText.match(/(\d+)\s*으로\s*부활/);
    const revivedValue = valueMatch ? clampStat(scaleFromHundred(valueMatch[1])) : 1;
    for (const target of targets) {
      target.stats.health = Math.max(target.stats.health, revivedValue);
      if (/정치력|정치/.test(effectText)) target.stats.politics = Math.max(target.stats.politics, revivedValue);
      if (/경건함|경건/.test(effectText)) target.stats.piety = Math.max(target.stats.piety, revivedValue);
    }
    applied = true;
  }

  for (const alias of statAliases) {
    const normalized = effectText.replace(alias.pattern, alias.key);
    const signedMatch = normalized.match(new RegExp(`${alias.key}\\s*([+-])\\s*(\\d+)`));
    const wordMatch = normalized.match(new RegExp(`${alias.key}\\s*(\\d+)\\s*(증가|감소)`));
    const match = signedMatch || wordMatch;
    if (!match) continue;
    const sign = signedMatch ? (match[1] === "+" ? 1 : -1) : (match[2] === "증가" ? 1 : -1);
    const rawValue = signedMatch ? match[2] : match[1];
    const delta = scaleFromHundred(rawValue) * sign;
    for (const target of targets) changeStat(target, alias.key, delta);
    applied = true;
  }
  return applied;
}

function movementSpeedModifierFromEffect(effectText) {
  return effectText
    .split(/\n|,|\//)
    .filter((part) => /이동\s*속도|이동속도/.test(part))
    .reduce((modifier, part) => {
      if (/증가|\+/.test(part)) return modifier + 1;
      if (/감소|-/.test(part)) return modifier - 1;
      return modifier;
    }, 0);
}

function applySpecialEffect(effectText) {
  const movementModifier = movementSpeedModifierFromEffect(effectText);
  if (movementModifier) {
    state.nextActionEffectModifier += movementModifier;
    return `이동 속도 효과를 다음 행동 효과 ${movementModifier > 0 ? `+${movementModifier}` : movementModifier} 보정으로 변환했다.`;
  }
  if (/공작 경건함 소모량 0/.test(effectText)) {
    state.freeSchemeCost = true;
    return "이번 콘클라베 동안 공작 경건 소모 0 효과를 기록했다.";
  }
  if (/콘클라베 종료|즉시 투표|게임 종료|게임 오버/.test(effectText)) {
    state.completed = true;
    return "이벤트 효과로 현재 시뮬레이션을 종료했다.";
  }
  if (/탈락/.test(effectText)) {
    for (const target of resolveEffectTarget(effectText)) target.stats.health = 0;
    return "이벤트 효과로 대상 후보를 탈락 처리했다.";
  }
  return null;
}

function applyEventChoice(choice) {
  const applied = [];
  for (const effect of choice.effects) {
    if (!effect || effect.trim() === "-") continue;
    const special = applySpecialEffect(effect);
    const numericApplied = applyNumericEffect(effect);
    if (special) applied.push(special);
    if (numericApplied) applied.push(`${effect} -> 1/10 스케일 적용`);
    if (!special && !numericApplied) applied.push(`${effect} 효과는 현재 수동 모드에서 로그로만 기록`);
  }
  return applied;
}

function applyHealthDecay() {
  if (state.config.decayAmount <= 0) return null;
  if (state.conclaveTurn % state.config.decayEvery !== 0) return null;
  for (const candidate of activeCandidates()) changeStat(candidate, "health", -state.config.decayAmount);
  return `${state.config.decayEvery}턴마다 적용되는 체력 감소: 모든 생존 후보 체력 -${state.config.decayAmount}.`;
}

function completeTurn() {
  const decayMessage = applyHealthDecay();
  if (decayMessage) addLog(decayMessage);

  const player = state.candidates.find((candidate) => candidate.role === "player");
  if (player.stats.health <= 0) {
    state.completed = true;
    addLog("플레이어 체력이 0이 되어 시뮬레이션이 종료됐다.");
  } else if (!state.completed && state.conclaveTurn >= state.config.conclaveTurns) {
    state.completed = true;
    const winner = strongestActive(state, "politics");
    addLog(`썬클라베 종료. 현재 판정 승자는 ${winner?.name ?? "없음"}이다.`);
  } else if (!state.completed) {
    state.conclaveTurn += 1;
    state.actionCount = 0;
    state.eventResolved = false;
    els.eventBox.textContent = "행동 2회를 실행하면 이벤트가 발생합니다.";
  }
}

function chooseEvent(index) {
  if (!state.pendingEvent) return;
  const event = state.pendingEvent;
  const choice = event.choices[index];
  if (!choice) return;
  const applied = applyEventChoice(choice);
  addLog(`${event.title}: ${choice.label}. ${applied.join(" ") || "효과 없음."}`);
  state.pendingEvent = null;
  state.eventResolved = true;
  els.eventBox.textContent = `${event.title}: ${choice.label} 선택 완료.`;
  completeTurn();
  render();
}

function resetSimulation() {
  state = createInitialState();
  els.eventBox.textContent = "행동 2회를 실행하면 이벤트가 발생합니다.";
  addLog(`새 시뮬레이션 시작. 썬클라베 ${state.config.conclaveTurns}턴, 체력 감소 ${state.config.decayEvery}턴마다 ${state.config.decayAmount}.`);
  renderActionOptions();
  render();
}

function clearLog() {
  state.log = [];
  renderLog();
}

function syncIntegerInput(event) {
  const input = event.target;
  if (!input.matches('input[type="number"]')) return;
  const min = Number(input.min || 0);
  const max = Number(input.max || 10);
  const parsed = input.value === "" ? min : Number(input.value);
  const value = Number.isFinite(parsed) ? Math.round(parsed) : min;
  input.value = Math.max(min, Math.min(max, value));
}

renderCandidateEditor();
resetSimulation();

els.resetButton.addEventListener("click", resetSimulation);
els.applyActionButton.addEventListener("click", applyAction);
els.resolveEventButton.addEventListener("click", showEvent);
els.clearLogButton.addEventListener("click", clearLog);
els.actionSelect.addEventListener("change", updateTargetOptions);
els.eventBox.addEventListener("click", (event) => {
  const button = event.target.closest("[data-event-choice]");
  if (!button) return;
  chooseEvent(Number(button.dataset.eventChoice));
});
document.addEventListener("change", syncIntegerInput);





