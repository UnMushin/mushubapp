import React, { useState } from "react";
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  DragEndEvent,
} from "@dnd-kit/core";
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  rectSortingStrategy,
  useSortable,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { open as shellOpen } from "@tauri-apps/api/shell";
import { useMushubStore, HubItem } from "../store";
import HubEditor from "../components/HubEditor";

/* ── Sortable tile ──────────────────────────────────────── */
function SortableTile({
  item, onEdit, onRemove,
}: {
  item: HubItem;
  onEdit: () => void;
  onRemove: () => void;
}) {
  const {
    attributes, listeners, setNodeRef,
    transform, transition, isDragging,
  } = useSortable({ id: item.id });

  const style: React.CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
    zIndex: isDragging ? 999 : "auto",
  };

  const [hovered, setHovered] = useState(false);

  async function handleLaunch() {
    if (item.type === "web" && item.url) {
      await shellOpen(item.url);
    } else if (item.type === "app" && item.appPath) {
      await shellOpen(item.appPath);
    }
  }

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
    >
      <div
        style={{
          width: "100%",
          aspectRatio: "1",
          borderRadius: "var(--radius-lg)",
          background: item.color + "22",
          border: `1px solid ${item.color}44`,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          gap: "8px",
          cursor: "pointer",
          position: "relative",
          overflow: "hidden",
          transition: "transform 120ms, box-shadow 120ms, border-color 120ms",
          transform: hovered && !isDragging ? "scale(1.03)" : "scale(1)",
          boxShadow: hovered
            ? `0 8px 24px ${item.color}33, var(--shadow-md)`
            : "var(--shadow-sm)",
          borderColor: hovered ? item.color + "88" : item.color + "44",
        }}
        onClick={handleLaunch}
      >
        {/* Subtle glow background */}
        <div
          style={{
            position: "absolute",
            inset: 0,
            background: `radial-gradient(circle at 50% 40%, ${item.color}1a 0%, transparent 70%)`,
            pointerEvents: "none",
          }}
        />

        {/* Icon */}
        <span style={{ fontSize: "32px", lineHeight: 1, userSelect: "none" }}>
          {item.icon}
        </span>

        {/* Label */}
        <span
          style={{
            fontSize: "12px",
            fontWeight: 500,
            color: "var(--text-primary)",
            textAlign: "center",
            padding: "0 8px",
            lineHeight: 1.2,
            userSelect: "none",
          }}
        >
          {item.label}
        </span>

        {/* Hover actions */}
        {hovered && (
          <div
            style={{
              position: "absolute",
              top: "4px",
              right: "4px",
              display: "flex",
              gap: "2px",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <button
              onClick={onEdit}
              style={actionBtnStyle}
              title="Modifier"
            >✏️</button>
            <button
              onClick={onRemove}
              style={{ ...actionBtnStyle, color: "var(--error)" }}
              title="Supprimer"
            >✕</button>
          </div>
        )}

        {/* Drag handle */}
        <div
          {...listeners}
          style={{
            position: "absolute",
            bottom: "4px",
            right: "4px",
            padding: "2px",
            cursor: "grab",
            color: "rgba(255,255,255,0.3)",
            fontSize: "14px",
            opacity: hovered ? 1 : 0,
            transition: "opacity 120ms",
          }}
          onClick={(e) => e.stopPropagation()}
        >
          ⠿
        </div>
      </div>
    </div>
  );
}

const actionBtnStyle: React.CSSProperties = {
  background: "rgba(0,0,0,0.5)",
  backdropFilter: "blur(8px)",
  border: "none",
  borderRadius: "4px",
  padding: "2px 5px",
  cursor: "pointer",
  fontSize: "11px",
  lineHeight: 1,
};

/* ── Hub page ───────────────────────────────────────────── */
export default function Hub() {
  const hubItems       = useMushubStore((s) => s.hubItems);
  const reorderHubItems = useMushubStore((s) => s.reorderHubItems);
  const removeHubItem  = useMushubStore((s) => s.removeHubItem);

  const [editItem, setEditItem]   = useState<HubItem | null>(null);
  const [showEditor, setShowEditor] = useState(false);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  const sorted = [...hubItems].sort((a, b) => a.order - b.order);

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    const oldIdx = sorted.findIndex((i) => i.id === active.id);
    const newIdx = sorted.findIndex((i) => i.id === over.id);
    const reordered = arrayMove(sorted, oldIdx, newIdx).map((item, idx) => ({
      ...item, order: idx,
    }));
    reorderHubItems(reordered);
  }

  return (
    <div
      className="animate-fade-in"
      style={{ padding: "32px 40px", height: "100%", overflow: "auto" }}
    >
      {/* Header */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: "28px",
        }}
      >
        <div>
          <h2 style={{ fontSize: "20px", fontWeight: 600, color: "var(--text-primary)" }}>
            ⚡ App Hub
          </h2>
          <p style={{ fontSize: "13px", color: "var(--text-tertiary)", marginTop: "2px" }}>
            {sorted.length} raccourci{sorted.length > 1 ? "s" : ""} • Glissez pour réorganiser
          </p>
        </div>
        <button
          className="btn btn-primary"
          onClick={() => { setEditItem(null); setShowEditor(true); }}
        >
          + Ajouter
        </button>
      </div>

      {/* Grid */}
      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        onDragEnd={handleDragEnd}
      >
        <SortableContext items={sorted.map((i) => i.id)} strategy={rectSortingStrategy}>
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(110px, 1fr))",
              gap: "14px",
              maxWidth: "900px",
            }}
          >
            {sorted.map((item) => (
              <SortableTile
                key={item.id}
                item={item}
                onEdit={() => { setEditItem(item); setShowEditor(true); }}
                onRemove={() => removeHubItem(item.id)}
              />
            ))}

            {/* Add placeholder tile */}
            <div
              onClick={() => { setEditItem(null); setShowEditor(true); }}
              style={{
                aspectRatio: "1",
                borderRadius: "var(--radius-lg)",
                border: "2px dashed var(--border-default)",
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                gap: "6px",
                cursor: "pointer",
                color: "var(--text-tertiary)",
                fontSize: "13px",
                transition: "border-color 120ms, color 120ms",
              }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLDivElement).style.borderColor = "var(--accent)";
                (e.currentTarget as HTMLDivElement).style.color = "var(--accent-light)";
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLDivElement).style.borderColor = "var(--border-default)";
                (e.currentTarget as HTMLDivElement).style.color = "var(--text-tertiary)";
              }}
            >
              <span style={{ fontSize: "28px" }}>+</span>
              <span>Nouveau</span>
            </div>
          </div>
        </SortableContext>
      </DndContext>

      {/* Editor modal */}
      {showEditor && (
        <HubEditor
          item={editItem}
          onClose={() => { setShowEditor(false); setEditItem(null); }}
        />
      )}
    </div>
  );
}
