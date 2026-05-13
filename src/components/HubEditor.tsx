import React, { useState, useEffect } from "react";
import { useMushubStore, HubItem } from "../store";

const PRESET_COLORS = [
  "#0078d4", "#00b4d8", "#10a37f", "#1db954", "#f24e1e",
  "#ff6b6b", "#ffd166", "#06d6a0", "#118ab2", "#7b2d8b",
  "#24292e", "#e63946", "#457b9d", "#2a9d8f", "#e9c46a",
];

const EMOJI_PRESETS = [
  "🐙","▶️","🤖","🎵","📝","🎨","🔥","⚡","🌐","📧",
  "💻","🎮","📊","🔐","☁️","🎯","📅","🛒","📚","🎬",
];

interface Props {
  item: HubItem | null;
  onClose: () => void;
}

export default function HubEditor({ item, onClose }: Props) {
  const addHubItem    = useMushubStore((s) => s.addHubItem);
  const updateHubItem = useMushubStore((s) => s.updateHubItem);

  const [label, setLabel]   = useState(item?.label ?? "");
  const [url, setUrl]       = useState(item?.url ?? "");
  const [icon, setIcon]     = useState(item?.icon ?? "🌐");
  const [color, setColor]   = useState(item?.color ?? "#0078d4");
  const [type, setType]     = useState<HubItem["type"]>(item?.type ?? "web");
  const [appPath, setAppPath] = useState(item?.appPath ?? "");
  const [emojiInput, setEmojiInput] = useState("");

  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") onClose();
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);

  function handleSave() {
    if (!label.trim()) return;
    const data = {
      label: label.trim(),
      url: url.trim() || undefined,
      appPath: appPath.trim() || undefined,
      icon,
      color,
      type,
    };
    if (item) {
      updateHubItem(item.id, data);
    } else {
      addHubItem(data);
    }
    onClose();
  }

  return (
    /* Backdrop */
    <div
      onClick={onClose}
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(0,0,0,0.55)",
        backdropFilter: "blur(4px)",
        zIndex: 1000,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
      }}
    >
      {/* Modal */}
      <div
        onClick={(e) => e.stopPropagation()}
        className="card-fluent animate-fade-in"
        style={{
          width: "420px",
          padding: "24px",
          background: "var(--surface-layer2)",
          border: "1px solid var(--border-default)",
          boxShadow: "var(--shadow-xl)",
        }}
      >
        <h3 style={{ fontSize: "16px", fontWeight: 600, marginBottom: "20px" }}>
          {item ? "✏️ Modifier le raccourci" : "✨ Nouveau raccourci"}
        </h3>

        {/* Preview tile */}
        <div style={{ display: "flex", justifyContent: "center", marginBottom: "20px" }}>
          <div
            style={{
              width: "90px",
              height: "90px",
              borderRadius: "var(--radius-lg)",
              background: color + "22",
              border: `2px solid ${color}66`,
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              justifyContent: "center",
              gap: "6px",
              boxShadow: `0 4px 16px ${color}33`,
            }}
          >
            <span style={{ fontSize: "28px" }}>{icon}</span>
            <span style={{ fontSize: "11px", fontWeight: 500, color: "var(--text-primary)", textAlign: "center", padding: "0 4px" }}>
              {label || "Nom"}
            </span>
          </div>
        </div>

        {/* Type */}
        <div style={{ marginBottom: "14px" }}>
          <label style={labelStyle}>Type</label>
          <div style={{ display: "flex", gap: "6px" }}>
            {(["web", "app"] as const).map((t) => (
              <button
                key={t}
                className={`btn ${type === t ? "btn-primary" : "btn-secondary"}`}
                onClick={() => setType(t)}
                style={{ flex: 1, fontSize: "13px" }}
              >
                {t === "web" ? "🌐 Site web" : "💻 Application"}
              </button>
            ))}
          </div>
        </div>

        {/* Label */}
        <div style={{ marginBottom: "12px" }}>
          <label style={labelStyle}>Nom du raccourci</label>
          <input
            className="input-win11"
            placeholder="Ex: GitHub"
            value={label}
            onChange={(e) => setLabel(e.target.value)}
          />
        </div>

        {/* URL / path */}
        {type === "web" ? (
          <div style={{ marginBottom: "12px" }}>
            <label style={labelStyle}>URL</label>
            <input
              className="input-win11"
              placeholder="https://example.com"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
            />
          </div>
        ) : (
          <div style={{ marginBottom: "12px" }}>
            <label style={labelStyle}>Chemin de l'application</label>
            <input
              className="input-win11"
              placeholder="C:\Program Files\App\app.exe"
              value={appPath}
              onChange={(e) => setAppPath(e.target.value)}
            />
          </div>
        )}

        {/* Icon emoji */}
        <div style={{ marginBottom: "12px" }}>
          <label style={labelStyle}>Icône (emoji)</label>
          <div style={{ display: "flex", gap: "6px", marginBottom: "8px", flexWrap: "wrap" }}>
            {EMOJI_PRESETS.map((e) => (
              <button
                key={e}
                onClick={() => setIcon(e)}
                style={{
                  width: "32px",
                  height: "32px",
                  borderRadius: "6px",
                  border: icon === e ? "2px solid var(--accent-light)" : "1px solid var(--border-default)",
                  background: icon === e ? "rgba(0,120,212,0.2)" : "rgba(255,255,255,0.05)",
                  cursor: "pointer",
                  fontSize: "16px",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                {e}
              </button>
            ))}
          </div>
          <input
            className="input-win11"
            placeholder="Ou collez votre emoji ici"
            value={emojiInput}
            maxLength={4}
            onChange={(e) => {
              setEmojiInput(e.target.value);
              if (e.target.value) setIcon(e.target.value);
            }}
            style={{ fontSize: "18px" }}
          />
        </div>

        {/* Color */}
        <div style={{ marginBottom: "20px" }}>
          <label style={labelStyle}>Couleur d'accent</label>
          <div style={{ display: "flex", gap: "6px", flexWrap: "wrap" }}>
            {PRESET_COLORS.map((c) => (
              <button
                key={c}
                onClick={() => setColor(c)}
                style={{
                  width: "26px",
                  height: "26px",
                  borderRadius: "50%",
                  background: c,
                  border: color === c
                    ? "3px solid white"
                    : "2px solid rgba(255,255,255,0.15)",
                  cursor: "pointer",
                  flexShrink: 0,
                  boxShadow: color === c ? `0 0 0 2px ${c}` : "none",
                }}
              />
            ))}
            <input
              type="color"
              value={color}
              onChange={(e) => setColor(e.target.value)}
              style={{
                width: "26px",
                height: "26px",
                borderRadius: "50%",
                border: "2px solid rgba(255,255,255,0.15)",
                cursor: "pointer",
                padding: 0,
                background: "none",
              }}
              title="Couleur personnalisée"
            />
          </div>
        </div>

        {/* Actions */}
        <div style={{ display: "flex", gap: "8px", justifyContent: "flex-end" }}>
          <button className="btn btn-secondary" onClick={onClose}>Annuler</button>
          <button
            className="btn btn-primary"
            onClick={handleSave}
            disabled={!label.trim()}
            style={{ opacity: !label.trim() ? 0.5 : 1 }}
          >
            {item ? "Enregistrer" : "Ajouter"}
          </button>
        </div>
      </div>
    </div>
  );
}

const labelStyle: React.CSSProperties = {
  display: "block",
  fontSize: "12px",
  fontWeight: 500,
  color: "var(--text-secondary)",
  marginBottom: "6px",
  textTransform: "uppercase",
  letterSpacing: "0.08em",
};
