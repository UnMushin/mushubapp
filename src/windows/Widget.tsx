import React, { useState } from "react";
import { appWindow } from "@tauri-apps/api/window";
import { open as shellOpen } from "@tauri-apps/api/shell";
import { useMushubStore, HubItem } from "../store";

/* ── Widget: mini fenêtre flottante style StreamDeck ─────── */
export default function Widget() {
  const hubItems = useMushubStore((s) => s.hubItems);
  const sorted   = [...hubItems].sort((a, b) => a.order - b.order);

  async function handleLaunch(item: HubItem) {
    if (item.type === "web" && item.url) await shellOpen(item.url);
    else if (item.type === "app" && item.appPath) await shellOpen(item.appPath);
  }

  return (
    <div
      style={{
        width: "100%",
        height: "100%",
        background: "rgba(20, 20, 20, 0.88)",
        backdropFilter: "blur(32px) saturate(180%)",
        WebkitBackdropFilter: "blur(32px) saturate(180%)",
        borderRadius: "16px",
        border: "1px solid rgba(255,255,255,0.10)",
        display: "flex",
        flexDirection: "column",
        overflow: "hidden",
        boxShadow: "0 16px 48px rgba(0,0,0,0.7), 0 0 0 1px rgba(255,255,255,0.05)",
      }}
    >
      {/* Title bar */}
      <div
        data-tauri-drag-region
        style={{
          height: "32px",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "0 10px",
          borderBottom: "1px solid rgba(255,255,255,0.07)",
          flexShrink: 0,
          cursor: "grab",
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: "6px" }}>
          <span style={{ fontSize: "13px" }}>🍄</span>
          <span style={{
            fontSize: "11px",
            fontWeight: 600,
            color: "rgba(255,255,255,0.5)",
            letterSpacing: "0.08em",
            textTransform: "uppercase",
          }}>
            Mushub
          </span>
        </div>
        <button
          onClick={() => appWindow.close()}
          style={{
            background: "none",
            border: "none",
            color: "rgba(255,255,255,0.35)",
            cursor: "pointer",
            fontSize: "14px",
            padding: "2px 5px",
            borderRadius: "4px",
            lineHeight: 1,
          }}
          onMouseEnter={(e) => (e.currentTarget.style.background = "rgba(196,43,28,0.7)")}
          onMouseLeave={(e) => (e.currentTarget.style.background = "none")}
        >
          ✕
        </button>
      </div>

      {/* Tiles grid */}
      <div
        style={{
          flex: 1,
          padding: "10px",
          display: "grid",
          gridTemplateColumns: "repeat(4, 1fr)",
          gap: "8px",
          overflowY: "auto",
          alignContent: "start",
        }}
      >
        {sorted.map((item) => (
          <WidgetTile key={item.id} item={item} onLaunch={() => handleLaunch(item)} />
        ))}
      </div>
    </div>
  );
}

function WidgetTile({
  item, onLaunch,
}: {
  item: HubItem;
  onLaunch: () => void;
}) {
  const [hovered, setHovered] = useState(false);
  const [pressed, setPressed] = useState(false);

  return (
    <div
      onClick={onLaunch}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => { setHovered(false); setPressed(false); }}
      onMouseDown={() => setPressed(true)}
      onMouseUp={() => setPressed(false)}
      style={{
        aspectRatio: "1",
        borderRadius: "10px",
        background: hovered
          ? item.color + "33"
          : item.color + "18",
        border: `1px solid ${hovered ? item.color + "66" : item.color + "30"}`,
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        gap: "4px",
        cursor: "pointer",
        transition: "transform 80ms, background 100ms, border-color 100ms, box-shadow 100ms",
        transform: pressed
          ? "scale(0.93)"
          : hovered
          ? "scale(1.05)"
          : "scale(1)",
        boxShadow: hovered ? `0 4px 12px ${item.color}33` : "none",
      }}
    >
      <span style={{ fontSize: "22px", lineHeight: 1 }}>{item.icon}</span>
      <span
        style={{
          fontSize: "9px",
          fontWeight: 500,
          color: hovered ? "rgba(255,255,255,0.9)" : "rgba(255,255,255,0.55)",
          textAlign: "center",
          maxWidth: "100%",
          overflow: "hidden",
          textOverflow: "ellipsis",
          whiteSpace: "nowrap",
          padding: "0 4px",
          transition: "color 100ms",
        }}
      >
        {item.label}
      </span>
    </div>
  );
}
