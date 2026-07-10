const metricKeys = [
  "analysisDays", "turns", "actionsPerTurn", "decayEvery", "decayAmount",
  "startHealth", "startPolitics", "startPiety",
  "praySuccessPiety", "praySuccessHealth", "prayFailPiety", "prayFailHealth",
  "speechSuccessPolitics", "speechSuccessHealth", "speechFailPolitics", "speechFailHealth",
  "prayRate", "speechRate", "prayersPerTurn", "speechesPerTurn",
  "schemeCost1", "schemeCost2", "schemeCost3", "schemeCost4Plus",
];

const metricEls = Object.fromEntries(metricKeys.map((key) => [key, document.querySelector(`[data-metric="${key}"]`)]));
const outputEls = {
  warning: document.getElementById("metricWarning"),
  requiredPrayer: document.getElementById("requiredPrayerOutput"),
  schemeCount: document.getElementById("schemeCountOutput"),
  finalSurvival: document.getElementById("finalSurvivalOutput"),
  tableBody: document.getElementById("metricTableBody"),
  healthChart: document.getElementById("healthChart"),
  politicsChart: document.getElementById("politicsChart"),
  pietyChart: document.getElementById("pietyChart"),
  currentHealthChart: document.getElementById("currentHealthChart"),
  currentPoliticsChart: document.getElementById("currentPoliticsChart"),
  currentPietyChart: document.getElementById("currentPietyChart"),
};

const statMeta = {
  health: { label: "체력", color: "#a33b2d" },
  politics: { label: "정치", color: "#2f6f73" },
  piety: { label: "경건", color: "#7353a6" },
};

function numberValue(key) {
  return Number(metricEls[key]?.value ?? 0) || 0;
}

function intValue(key) {
  return Math.round(numberValue(key));
}

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

function readMetricConfig() {
  const config = {
    analysisDays: clamp(intValue("analysisDays"), 1, 14),
    turns: clamp(intValue("turns"), 1, 60),
    actionsPerTurn: clamp(intValue("actionsPerTurn"), 1, 20),
    decayEvery: clamp(intValue("decayEvery"), 1, 60),
    decayAmount: clamp(intValue("decayAmount"), 0, 10),
    startHealth: clamp(numberValue("startHealth"), 1, 10),
    startPolitics: clamp(numberValue("startPolitics"), 0, 10),
    startPiety: clamp(numberValue("startPiety"), 0, 10),
    praySuccessPiety: numberValue("praySuccessPiety"),
    praySuccessHealth: numberValue("praySuccessHealth"),
    prayFailPiety: numberValue("prayFailPiety"),
    prayFailHealth: numberValue("prayFailHealth"),
    speechSuccessPolitics: numberValue("speechSuccessPolitics"),
    speechSuccessHealth: numberValue("speechSuccessHealth"),
    speechFailPolitics: numberValue("speechFailPolitics"),
    speechFailHealth: numberValue("speechFailHealth"),
    prayRate: clamp(numberValue("prayRate") / 100, 0, 1),
    speechRate: clamp(numberValue("speechRate") / 100, 0, 1),
    prayersPerDay: clamp(intValue("prayersPerTurn"), 0, 80),
    speechesPerDay: clamp(intValue("speechesPerTurn"), 0, 80),
    schemeCost1: clamp(numberValue("schemeCost1"), 0, 10),
    schemeCost2: clamp(numberValue("schemeCost2"), 0, 10),
    schemeCost3: clamp(numberValue("schemeCost3"), 0, 10),
    schemeCost4Plus: clamp(numberValue("schemeCost4Plus"), 0, 10),
  };
  config.actionSlotsPerDay = config.turns * config.actionsPerTurn;
  config.schemeActionsPerDay = Math.max(0, config.actionSlotsPerDay - config.prayersPerDay - config.speechesPerDay);
  config.actionOverflow = Math.max(0, config.prayersPerDay + config.speechesPerDay - config.actionSlotsPerDay);
  return config;
}

function schemeCostForDay(config, day) {
  if (day <= 1) return config.schemeCost1;
  if (day === 2) return config.schemeCost2;
  if (day === 3) return config.schemeCost3;
  return config.schemeCost4Plus;
}

function decayForDay(config, day) {
  const startTurn = (day - 1) * config.turns + 1;
  const endTurn = day * config.turns;
  let decayEvents = 0;
  for (let turn = startTurn; turn <= endTurn; turn += 1) {
    if (turn % config.decayEvery === 0) decayEvents += 1;
  }
  return decayEvents * config.decayAmount;
}

function binomial(n, k) {
  if (k < 0 || k > n) return 0;
  let result = 1;
  for (let i = 1; i <= k; i += 1) result = result * (n - i + 1) / i;
  return result;
}

function binomialProbability(n, k, p) {
  if (p === 0) return k === 0 ? 1 : 0;
  if (p === 1) return k === n ? 1 : 0;
  return binomial(n, k) * (p ** k) * ((1 - p) ** (n - k));
}

function outcomeKey(state) {
  return `${state.health}|${state.politics}|${state.piety}`;
}

function parseOutcomeKey(key) {
  const [health, politics, piety] = key.split("|").map(Number);
  return { health, politics, piety };
}

function addWeighted(map, key, probability) {
  map.set(key, (map.get(key) || 0) + probability);
}

function dayOutcomeDistribution(config, day, prayerCount = config.prayersPerDay) {
  const distribution = [];
  const schemeActions = Math.max(0, config.actionSlotsPerDay - prayerCount - config.speechesPerDay);
  const schemeCost = schemeActions * schemeCostForDay(config, day);
  const decay = decayForDay(config, day);

  for (let praySuccesses = 0; praySuccesses <= prayerCount; praySuccesses += 1) {
    const prayProbability = binomialProbability(prayerCount, praySuccesses, config.prayRate);
    const prayFails = prayerCount - praySuccesses;
    const prayerHealth = praySuccesses * config.praySuccessHealth + prayFails * config.prayFailHealth;
    const prayerPiety = praySuccesses * config.praySuccessPiety + prayFails * config.prayFailPiety;

    for (let speechSuccesses = 0; speechSuccesses <= config.speechesPerDay; speechSuccesses += 1) {
      const speechProbability = binomialProbability(config.speechesPerDay, speechSuccesses, config.speechRate);
      const speechFails = config.speechesPerDay - speechSuccesses;
      const speechHealth = speechSuccesses * config.speechSuccessHealth + speechFails * config.speechFailHealth;
      const speechPolitics = speechSuccesses * config.speechSuccessPolitics + speechFails * config.speechFailPolitics;

      distribution.push({
        probability: prayProbability * speechProbability,
        delta: {
          health: prayerHealth + speechHealth - decay,
          politics: speechPolitics,
          piety: prayerPiety - schemeCost,
        },
      });
    }
  }

  return distribution;
}

function weightedSummary(entries) {
  const cleaned = entries
    .filter((entry) => Number.isFinite(entry.value) && entry.probability > 0)
    .sort((a, b) => a.value - b.value);
  const totalProbability = cleaned.reduce((sum, entry) => sum + entry.probability, 0);
  if (!cleaned.length || totalProbability <= 0) return { q1: 0, avg: 0, q3: 0 };

  const average = cleaned.reduce((sum, entry) => sum + entry.value * entry.probability, 0) / totalProbability;
  const quantile = (target) => {
    let cumulative = 0;
    for (const entry of cleaned) {
      cumulative += entry.probability / totalProbability;
      if (cumulative >= target) return entry.value;
    }
    return cleaned.at(-1).value;
  };

  return { q1: quantile(0.25), avg: average, q3: quantile(0.75) };
}

function summarizeDeltaDistribution(distribution, statKey) {
  return weightedSummary(distribution.map((entry) => ({ value: entry.delta[statKey], probability: entry.probability })));
}

function summarizeStateDistribution(distribution, statKey) {
  const entries = [];
  for (const [key, probability] of distribution.entries()) {
    const state = parseOutcomeKey(key);
    entries.push({ value: state[statKey], probability });
  }
  return weightedSummary(entries);
}

function applyDayDistribution(stateDistribution, dayDistribution) {
  const next = new Map();

  for (const [stateKey, stateProbability] of stateDistribution.entries()) {
    const state = parseOutcomeKey(stateKey);
    if (state.health <= 0) {
      addWeighted(next, stateKey, stateProbability);
      continue;
    }

    for (const outcome of dayDistribution) {
      const health = clamp(Math.round(state.health + outcome.delta.health), 0, 10);
      const politics = clamp(Math.round(state.politics + outcome.delta.politics), 0, 10);
      const piety = clamp(Math.round(state.piety + outcome.delta.piety), 0, 10);
      const nextState = { health, politics, piety };
      addWeighted(next, outcomeKey(nextState), stateProbability * outcome.probability);
    }
  }

  return next;
}

function buildMetricSeries(config, prayerCount = config.prayersPerDay) {
  let stateDistribution = new Map([[outcomeKey({
    health: config.startHealth,
    politics: config.startPolitics,
    piety: config.startPiety,
  }), 1]]);
  const productionRows = [];
  const currentRows = [];
  const survival = [];

  for (let day = 1; day <= config.analysisDays; day += 1) {
    const dayDistribution = dayOutcomeDistribution(config, day, prayerCount);
    productionRows.push({
      day,
      health: summarizeDeltaDistribution(dayDistribution, "health"),
      politics: summarizeDeltaDistribution(dayDistribution, "politics"),
      piety: summarizeDeltaDistribution(dayDistribution, "piety"),
    });

    stateDistribution = applyDayDistribution(stateDistribution, dayDistribution);
    currentRows.push({
      day,
      health: summarizeStateDistribution(stateDistribution, "health"),
      politics: summarizeStateDistribution(stateDistribution, "politics"),
      piety: summarizeStateDistribution(stateDistribution, "piety"),
    });

    const aliveProbability = [...stateDistribution.entries()].reduce((sum, [key, probability]) => {
      return sum + (parseOutcomeKey(key).health > 0 ? probability : 0);
    }, 0);
    survival.push(aliveProbability);
  }

  return { productionRows, currentRows, survival };
}

function worstCaseSurvives(config, prayerCount) {
  let health = config.startHealth;
  for (let day = 1; day <= config.analysisDays; day += 1) {
    const worstOutcome = dayOutcomeDistribution(config, day, prayerCount)
      .reduce((worst, outcome) => Math.min(worst, outcome.delta.health), Infinity);
    health = Math.min(10, health + worstOutcome);
    if (health <= 0) return false;
  }
  return true;
}

function requiredPrayerCount(config) {
  const maxPrayerSlots = Math.max(0, config.actionSlotsPerDay - config.speechesPerDay);
  for (let prayers = 0; prayers <= maxPrayerSlots; prayers += 1) {
    if (worstCaseSurvives(config, prayers)) return prayers;
  }
  return 0;
}

function formatNumber(value) {
  const rounded = Math.round(value * 10) / 10;
  return Number.isInteger(rounded) ? String(rounded) : rounded.toFixed(1);
}

function drawChart(canvas, rows, statKey, yLabel) {
  const ctx = canvas.getContext("2d");
  const width = canvas.width;
  const height = canvas.height;
  const pad = { left: 44, right: 16, top: 18, bottom: 30 };
  const series = ["q1", "avg", "q3"];
  const values = rows.flatMap((row) => series.map((mode) => row[statKey][mode]));
  const min = Math.min(0, ...values);
  const max = Math.max(1, ...values);
  const range = max - min || 1;
  const xFor = (index) => pad.left + (rows.length <= 1 ? 0 : index * ((width - pad.left - pad.right) / (rows.length - 1)));
  const yFor = (value) => height - pad.bottom - ((value - min) / range) * (height - pad.top - pad.bottom);

  ctx.clearRect(0, 0, width, height);
  ctx.fillStyle = "#fffdf7";
  ctx.fillRect(0, 0, width, height);
  ctx.strokeStyle = "#d8cdbb";
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(pad.left, pad.top);
  ctx.lineTo(pad.left, height - pad.bottom);
  ctx.lineTo(width - pad.right, height - pad.bottom);
  ctx.stroke();

  ctx.fillStyle = "#6d665d";
  ctx.font = "12px Segoe UI, sans-serif";
  ctx.fillText(formatNumber(max), 6, pad.top + 4);
  ctx.fillText(formatNumber(min), 6, height - pad.bottom + 4);
  ctx.fillText("일", width - 28, height - 8);
  ctx.fillText(yLabel, pad.left, height - 8);

  const styles = {
    q1: { color: "#a33b2d", dash: [5, 4], label: "하위25" },
    avg: { color: statMeta[statKey].color, dash: [], label: "평균" },
    q3: { color: "#3f7d44", dash: [2, 3], label: "상위25" },
  };

  for (const mode of series) {
    ctx.save();
    ctx.strokeStyle = styles[mode].color;
    ctx.lineWidth = mode === "avg" ? 2.5 : 1.8;
    ctx.setLineDash(styles[mode].dash);
    ctx.beginPath();
    rows.forEach((row, index) => {
      const x = xFor(index);
      const y = yFor(row[statKey][mode]);
      if (index === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    });
    ctx.stroke();
    ctx.restore();
  }

  let legendX = pad.left;
  for (const mode of series) {
    ctx.fillStyle = styles[mode].color;
    ctx.fillRect(legendX, 8, 10, 3);
    ctx.fillStyle = "#25211b";
    ctx.fillText(styles[mode].label, legendX + 14, 12);
    legendX += 68;
  }
}

function renderTable(currentRows, survival) {
  outputEls.tableBody.innerHTML = currentRows.map((row, index) => `
    <tr>
      <td>${row.day}</td>
      <td>${formatNumber(row.health.q1)}</td><td>${formatNumber(row.health.avg)}</td><td>${formatNumber(row.health.q3)}</td>
      <td>${formatNumber(row.politics.q1)}</td><td>${formatNumber(row.politics.avg)}</td><td>${formatNumber(row.politics.q3)}</td>
      <td>${formatNumber(row.piety.q1)}</td><td>${formatNumber(row.piety.avg)}</td><td>${formatNumber(row.piety.q3)}</td>
      <td>${formatNumber(survival[index] * 100)}%</td>
    </tr>
  `).join("");
}

function updateMetricModule() {
  const config = readMetricConfig();
  const { productionRows, currentRows, survival } = buildMetricSeries(config);
  const requiredPrayers = requiredPrayerCount(config);

  outputEls.requiredPrayer.textContent = String(requiredPrayers);
  outputEls.schemeCount.textContent = String(config.schemeActionsPerDay);
  outputEls.finalSurvival.textContent = `${formatNumber((survival.at(-1) || 0) * 100)}%`;
  outputEls.warning.textContent = config.actionOverflow
    ? `일자별 행동 슬롯 초과: ${config.actionOverflow}회`
    : "입력 변경 시 즉시 계산";
  outputEls.warning.classList.toggle("warning", Boolean(config.actionOverflow));

  drawChart(outputEls.healthChart, productionRows, "health", "생산");
  drawChart(outputEls.politicsChart, productionRows, "politics", "생산");
  drawChart(outputEls.pietyChart, productionRows, "piety", "생산");
  drawChart(outputEls.currentHealthChart, currentRows, "health", "현재");
  drawChart(outputEls.currentPoliticsChart, currentRows, "politics", "현재");
  drawChart(outputEls.currentPietyChart, currentRows, "piety", "현재");
  renderTable(currentRows, survival);
}

for (const input of Object.values(metricEls)) {
  input?.addEventListener("input", updateMetricModule);
  input?.addEventListener("change", updateMetricModule);
}

updateMetricModule();


