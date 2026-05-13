import React, { useEffect, useRef, useState, useCallback } from "react";
import { useMushubStore, PomodoroSettings, PomodoroPhase } from "../store";
import { isPermissionGranted, requestPermission, sendNotification } from "@tauri-apps/api/notification";

/* ── SVG circular progress ──────────────────────────────── */
function CircularTimer({
  progress,     // 0..1
  size = 280,
  phase,
  remaining,
}: {
  progress: number;
  size?: number;
  phase: PomodoroPhase;
  remaining: number;
}) {
  const stroke  = 10;
  const r       = (size - stroke) / 2;
  const circ    = 2 * Math.PI * r;
  const offset  = circ * (1 - progress);
  const cx = size / 2;
  const cy = size / 2;

  const PHASE_COLORS: Record<PomodoroPhase, string> = {
    "work":        "var(--accent-light)",
    "short-break": "var(--success)",
    "long-break":  "var(--info)",
  };
  const color = PHASE_COLORS[phase];

  const mins = Math.floor(remaining / 60).toString().padStart(2, "0");
  const secs = (remaining % 60).toString().padStart(2, "0");

  return (
    <div style={{ position: "relative", width: size, height: size }}>
      <svg width={size} height={size} style={{ transform: "rotate(-90deg)" }}>
        {/* Track */}
        <circle
          cx={cx} cy={cy} r={r}
          fill="none"
          stroke="rgba(255,255,255,0.07)"
          strokeWidth={stroke}
        />
        {/* Progress */}
        <circle
          cx={cx} cy={cy} r={r}
          fill="none"
          stroke={color}
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={circ}
          strokeDashoffset={offset}
          style={{ transition: "stroke-dashoffset 1s linear, stroke 400ms" }}
        />
        {/* Glow */}
        <circle
          cx={cx} cy={cy} r={r}
          fill="none"
          stroke={color}
          strokeWidth={stroke + 6}
          strokeLinecap="round"
          strokeDasharray={circ}
          strokeDashoffset={offset}
          opacity={0.12}
          style={{ transition: "stroke-dashoffset 1s linear, stroke 400ms" }}
        />
      </svg>
      {/* Time text */}
      <div
        style={{
          position: "absolute",
          inset: 0,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          gap: "4px",
        }}
      >
        <span
          style={{
            fontFamily: "'Segoe UI Variable', 'Segoe UI', monospace",
            fontSize: "52px",
            fontWeight: 300,
            color: "var(--text-primary)",
            letterSpacing: "0.04em",
            lineHeight: 1,
          }}
        >
          {mins}:{secs}
        </span>
        <PhaseLabel phase={phase} />
      </div>
    </div>
  );
}

function PhaseLabel({ phase }: { phase: PomodoroPhase }) {
  const labels: Record<PomodoroPhase, { text: string; color: string }> = {
    "work":        { text: "Travail",        color: "var(--accent-light)" },
    "short-break": { text: "Pause courte",   color: "var(--success)" },
    "long-break":  { text: "Longue pause",   color: "var(--info)" },
  };
  const { text, color } = labels[phase];
  return (
    <span style={{
      fontSize: "13px",
      fontWeight: 500,
      color,
      textTransform: "uppercase",
      letterSpacing: "0.12em",
    }}>
      {text}
    </span>
  );
}

/* ── Settings Panel ────────────────────────────────────── */
function SettingsSlider({
  label, value, min, max, step = 1, onChange, unit = "min"
}: {
  label: string; value: number; min: number; max: number;
  step?: number; onChange: (v: number) => void; unit?: string;
}) {
  return (
    <div style={{ marginBottom: "16px" }}>
      <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "6px" }}>
        <span style={{ fontSize: "13px", color: "var(--text-secondary)" }}>{label}</span>
        <span style={{
          fontSize: "13px", fontWeight: 600,
          color: "var(--accent-light)",
          minWidth: "40px", textAlign: "right"
        }}>
          {value} {unit}
        </span>
      </div>
      <input
        type="range" min={min} max={max} step={step} value={value}
        onChange={(e) => onChange(Number(e.target.value))}
        style={{ width: "100%", accentColor: "var(--accent)" }}
      />
    </div>
  );
}

/* ── Main Pomodoro page ─────────────────────────────────── */
export default function Pomodoro() {
  const settings        = useMushubStore((s) => s.pomodoroSettings);
  const updateSettings  = useMushubStore((s) => s.updatePomodoroSettings);

  const [phase, setPhase]         = useState<PomodoroPhase>("work");
  const [remaining, setRemaining] = useState(settings.workMinutes * 60);
  const [running, setRunning]     = useState(false);
  const [session, setSession]     = useState(1); // 1..sessionsBeforeLong
  const [showSettings, setShowSettings] = useState(false);

  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const totalRef    = useRef(settings.workMinutes * 60);

  /* Compute total for current phase */
  const phaseTotal = useCallback((p: PomodoroPhase, s: PomodoroSettings) => {
    if (p === "work")        return s.workMinutes * 60;
    if (p === "short-break") return s.shortBreakMinutes * 60;
    return                          s.longBreakMinutes * 60;
  }, []);

  /* When settings change: reset current phase duration if not running */
  useEffect(() => {
    if (!running) {
      const total = phaseTotal(phase, settings);
      totalRef.current = total;
      setRemaining(total);
    }
  }, [settings, phase, running, phaseTotal]);

  /* Timer tick */
  useEffect(() => {
    if (running) {
      intervalRef.current = setInterval(() => {
        setRemaining((r) => {
          if (r <= 1) {
            clearInterval(intervalRef.current!);
            handlePhaseEnd();
            return 0;
          }
          return r - 1;
        });
      }, 1000);
    } else {
      if (intervalRef.current) clearInterval(intervalRef.current);
    }
    return () => { if (intervalRef.current) clearInterval(intervalRef.current); };
  }, [running]);

  async function handlePhaseEnd() {
    // Notification
    if (settings.notifEnabled) {
      const granted = await isPermissionGranted();
      if (!granted) await requestPermission();
      const msgs: Record<PomodoroPhase, string> = {
        "work":        "⏰ Session terminée ! Prenez une pause.",
        "short-break": "💪 Pause terminée ! Au boulot.",
        "long-break":  "🚀 Longue pause finie ! On reprend.",
      };
      sendNotification({ title: "Mushub Pomodoro", body: msgs[phase] });
    }

    // Transition to next phase
    setRunning(false);
    if (phase === "work") {
      const nextSession = session + 1;
      if (nextSession > settings.sessionsBeforeLong) {
        setPhase("long-break");
        setSession(1);
        const total = settings.longBreakMinutes * 60;
        totalRef.current = total;
        setRemaining(total);
        if (settings.autoStartBreaks) setRunning(true);
      } else {
        setSession(nextSession);
        setPhase("short-break");
        const total = settings.shortBreakMinutes * 60;
        totalRef.current = total;
        setRemaining(total);
        if (settings.autoStartBreaks) setRunning(true);
      }
    } else {
      setPhase("work");
      const total = settings.workMinutes * 60;
      totalRef.current = total;
      setRemaining(total);
      if (settings.autoStartWork) setRunning(true);
    }
  }

  function handleReset() {
    setRunning(false);
    const total = phaseTotal(phase, settings);
    totalRef.current = total;
    setRemaining(total);
  }

  function handleSkip() {
    setRunning(false);
    handlePhaseEnd();
  }

  const progress = 1 - remaining / (totalRef.current || 1);

  /* Dot indicators */
  const dots = Array.from({ length: settings.sessionsBeforeLong }, (_, i) => i + 1);

  return (
    <div
      className="animate-fade-in"
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "flex-start",
        padding: "40px 32px",
        height: "100%",
        gap: "0",
      }}
    >
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", width: "100%", maxWidth: "520px", marginBottom: "28px" }}>
        <h2 style={{ fontSize: "20px", fontWeight: 600, color: "var(--text-primary)" }}>
          ⏱ Pomodoro
        </h2>
        <button
          className="btn btn-ghost"
          onClick={() => setShowSettings(!showSettings)}
          style={{ fontSize: "18px", padding: "6px 10px" }}
          title="Paramètres"
        >
          ⚙️
        </button>
      </div>

      {/* Settings panel */}
      {showSettings && (
        <div
          className="card-fluent animate-fade-in"
          style={{ width: "100%", maxWidth: "520px", padding: "20px", marginBottom: "24px" }}
        >
          <h3 style={{ fontSize: "15px", fontWeight: 600, marginBottom: "16px", color: "var(--text-primary)" }}>
            Paramètres du minuteur
          </h3>
          <SettingsSlider
            label="Durée de travail"
            value={settings.workMinutes} min={1} max={120}
            onChange={(v) => updateSettings({ workMinutes: v })}
          />
          <SettingsSlider
            label="Pause courte"
            value={settings.shortBreakMinutes} min={1} max={30}
            onChange={(v) => updateSettings({ shortBreakMinutes: v })}
          />
          <SettingsSlider
            label="Longue pause"
            value={settings.longBreakMinutes} min={5} max={60}
            onChange={(v) => updateSettings({ longBreakMinutes: v })}
          />
          <SettingsSlider
            label="Sessions avant longue pause"
            value={settings.sessionsBeforeLong} min={1} max={10} unit=""
            onChange={(v) => updateSettings({ sessionsBeforeLong: v })}
          />
          <div style={{ display: "flex", flexDirection: "column", gap: "10px", marginTop: "8px" }}>
            {[
              { key: "autoStartBreaks", label: "Démarrer les pauses automatiquement" },
              { key: "autoStartWork",   label: "Reprendre le travail automatiquement" },
              { key: "notifEnabled",    label: "Notifications de fin de session" },
            ].map(({ key, label }) => (
              <label key={key} style={{ display: "flex", alignItems: "center", gap: "10px", cursor: "pointer" }}>
                <input
                  type="checkbox"
                  className="checkbox-win11"
                  checked={settings[key as keyof PomodoroSettings] as boolean}
                  onChange={(e) => updateSettings({ [key]: e.target.checked })}
                />
                <span style={{ fontSize: "13px", color: "var(--text-secondary)" }}>{label}</span>
              </label>
            ))}
          </div>
        </div>
      )}

      {/* Timer circle */}
      <CircularTimer
        progress={progress}
        size={280}
        phase={phase}
        remaining={remaining}
      />

      {/* Session dots */}
      <div style={{ display: "flex", gap: "8px", margin: "24px 0 28px" }}>
        {dots.map((i) => (
          <div
            key={i}
            style={{
              width: i <= (phase === "work" ? session - 1 : session - 1) ? 10 : 8,
              height: i <= (phase === "work" ? session - 1 : session - 1) ? 10 : 8,
              borderRadius: "50%",
              background: i < session
                ? "var(--accent-light)"
                : i === session && phase === "work"
                ? "rgba(96,205,255,0.40)"
                : "rgba(255,255,255,0.15)",
              transition: "background 300ms, width 200ms, height 200ms",
            }}
          />
        ))}
      </div>

      {/* Controls */}
      <div style={{ display: "flex", gap: "10px" }}>
        <button
          className="btn btn-secondary"
          onClick={handleReset}
          style={{ padding: "10px 20px" }}
        >
          ↺ Réinitialiser
        </button>
        <button
          className="btn btn-primary"
          onClick={() => setRunning(!running)}
          style={{ padding: "10px 32px", fontSize: "15px", fontWeight: 600 }}
        >
          {running ? "⏸ Pause" : "▶ Démarrer"}
        </button>
        <button
          className="btn btn-secondary"
          onClick={handleSkip}
          style={{ padding: "10px 20px" }}
        >
          ⏭ Passer
        </button>
      </div>

      {/* Phase tabs */}
      <div style={{ display: "flex", gap: "4px", marginTop: "24px" }}>
        {(["work", "short-break", "long-break"] as PomodoroPhase[]).map((p) => (
          <button
            key={p}
            className={`btn ${phase === p ? "btn-primary" : "btn-ghost"}`}
            onClick={() => {
              if (running) return;
              setPhase(p);
              const total = phaseTotal(p, settings);
              totalRef.current = total;
              setRemaining(total);
            }}
            style={{ fontSize: "12px", padding: "5px 12px", opacity: running ? 0.5 : 1 }}
          >
            {p === "work" ? "Travail" : p === "short-break" ? "Pause courte" : "Longue pause"}
          </button>
        ))}
      </div>
    </div>
  );
}
