const screenMeta = {
  manual: {
    title: "썬클라베 수동 플레이",
    eyebrow: "Manual Play Simulator",
    phaseVisible: true,
  },
  metrics: {
    title: "기도/연설 행동 수치 분석",
    eyebrow: "Numeric Analysis",
    phaseVisible: false,
  },
};

const screenTabs = [...document.querySelectorAll("[data-screen-target]")];
const screenPanels = [...document.querySelectorAll("[data-screen-panel]")];
const screenTitle = document.getElementById("screenTitle");
const screenEyebrow = document.getElementById("screenEyebrow");
const phaseSummary = document.getElementById("phaseSummary");

function activateScreen(screenId) {
  const target = screenMeta[screenId] ? screenId : "manual";
  for (const panel of screenPanels) {
    const isActive = panel.dataset.screenPanel === target;
    panel.hidden = !isActive;
    panel.classList.toggle("is-active", isActive);
  }
  for (const tab of screenTabs) {
    const isActive = tab.dataset.screenTarget === target;
    tab.classList.toggle("is-active", isActive);
    tab.setAttribute("aria-pressed", String(isActive));
  }
  screenTitle.textContent = screenMeta[target].title;
  screenEyebrow.textContent = screenMeta[target].eyebrow;
  phaseSummary.hidden = !screenMeta[target].phaseVisible;
  if (location.hash !== `#${target}`) history.replaceState(null, "", `#${target}`);
  window.dispatchEvent(new Event("balancing-screen-change"));
}

for (const tab of screenTabs) {
  tab.addEventListener("click", () => activateScreen(tab.dataset.screenTarget));
}

window.addEventListener("hashchange", () => activateScreen(location.hash.replace("#", "")));
activateScreen(location.hash.replace("#", "") || "manual");
