import React, { useState, useMemo } from "react";
import { useMushubStore, Todo } from "../store";

/* ── Greeting ──────────────────────────────────────────── */
function getGreeting(): { text: string; emoji: string } {
  const h = new Date().getHours();
  if (h < 6)  return { text: "Bonne nuit",     emoji: "🌙" };
  if (h < 12) return { text: "Bonjour",         emoji: "☀️" };
  if (h < 14) return { text: "Bon appétit",     emoji: "🍽️" };
  if (h < 18) return { text: "Bonne après-midi",emoji: "🌤️" };
  if (h < 22) return { text: "Bonne soirée",    emoji: "🌆" };
  return           { text: "Bonne nuit",         emoji: "🌃" };
}

const PRIORITY_COLORS: Record<Todo["priority"], string> = {
  low:    "var(--text-tertiary)",
  medium: "var(--accent-light)",
  high:   "var(--error)",
};
const PRIORITY_LABELS: Record<Todo["priority"], string> = {
  low: "Faible", medium: "Normale", high: "Haute"
};

export default function Home() {
  const todos         = useMushubStore((s) => s.todos);
  const addTodo       = useMushubStore((s) => s.addTodo);
  const toggleTodo    = useMushubStore((s) => s.toggleTodo);
  const removeTodo    = useMushubStore((s) => s.removeTodo);
  const editTodo      = useMushubStore((s) => s.editTodo);

  const [input, setInput]         = useState("");
  const [priority, setPriority]   = useState<Todo["priority"]>("medium");
  const [filter, setFilter]       = useState<"all" | "active" | "done">("all");
  const [editId, setEditId]       = useState<string | null>(null);
  const [editText, setEditText]   = useState("");

  const greeting = useMemo(() => getGreeting(), []);

  const filtered = useMemo(() => {
    const sorted = [...todos].sort((a, b) => {
      const pri = { high: 0, medium: 1, low: 2 };
      return pri[a.priority] - pri[b.priority] || a.createdAt - b.createdAt;
    });
    if (filter === "active") return sorted.filter((t) => !t.done);
    if (filter === "done")   return sorted.filter((t) => t.done);
    return sorted;
  }, [todos, filter]);

  const stats = useMemo(() => ({
    total:  todos.length,
    done:   todos.filter((t) => t.done).length,
    active: todos.filter((t) => !t.done).length,
  }), [todos]);

  function handleAdd() {
    if (!input.trim()) return;
    addTodo(input.trim(), priority);
    setInput("");
    setPriority("medium");
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === "Enter") handleAdd();
  }

  function startEdit(todo: Todo) {
    setEditId(todo.id);
    setEditText(todo.text);
  }
  function commitEdit(id: string) {
    if (editText.trim()) editTodo(id, editText.trim());
    setEditId(null);
  }

  return (
    <div
      className="animate-fade-in"
      style={{ padding: "32px 40px", maxWidth: "720px", margin: "0 auto" }}
    >
      {/* ── Greeting ─────────────────────────────────────── */}
      <div style={{ marginBottom: "32px" }}>
        <div style={{ fontSize: "28px", marginBottom: "4px" }}>
          {greeting.emoji}
        </div>
        <h1
          style={{
            fontSize: "26px",
            fontWeight: 700,
            color: "var(--text-primary)",
            letterSpacing: "-0.02em",
            lineHeight: 1.2,
          }}
        >
          {greeting.text} 👋
        </h1>
        <p style={{ color: "var(--text-secondary)", marginTop: "6px", fontSize: "14px" }}>
          {new Date().toLocaleDateString("fr-FR", {
            weekday: "long", day: "numeric", month: "long", year: "numeric",
          })}
        </p>
      </div>

      {/* ── Stats bar ─────────────────────────────────────── */}
      <div
        style={{
          display: "flex",
          gap: "12px",
          marginBottom: "24px",
        }}
      >
        {[
          { label: "Total",     value: stats.total,  color: "var(--text-secondary)" },
          { label: "À faire",   value: stats.active, color: "var(--accent-light)" },
          { label: "Terminées", value: stats.done,   color: "var(--success)" },
        ].map((s) => (
          <div
            key={s.label}
            className="card-fluent"
            style={{ padding: "12px 20px", flex: 1, textAlign: "center" }}
          >
            <div style={{ fontSize: "22px", fontWeight: 700, color: s.color }}>
              {s.value}
            </div>
            <div style={{ fontSize: "12px", color: "var(--text-tertiary)", marginTop: "2px" }}>
              {s.label}
            </div>
          </div>
        ))}
      </div>

      {/* ── Add task ─────────────────────────────────────── */}
      <div
        className="card-fluent"
        style={{ padding: "16px", marginBottom: "20px" }}
      >
        <div style={{ display: "flex", gap: "8px", alignItems: "center" }}>
          <input
            className="input-win11"
            placeholder="Ajouter une tâche…"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            style={{ flex: 1 }}
          />
          {/* Priority selector */}
          <select
            value={priority}
            onChange={(e) => setPriority(e.target.value as Todo["priority"])}
            style={{
              background: "rgba(255,255,255,0.06)",
              border: "1px solid var(--border-default)",
              borderRadius: "var(--radius-md)",
              color: "var(--text-primary)",
              padding: "7px 10px",
              fontFamily: "var(--font-ui)",
              fontSize: "13px",
              cursor: "pointer",
              outline: "none",
            }}
          >
            <option value="low">🔵 Faible</option>
            <option value="medium">🟡 Normale</option>
            <option value="high">🔴 Haute</option>
          </select>
          <button className="btn btn-primary" onClick={handleAdd}>
            + Ajouter
          </button>
        </div>
      </div>

      {/* ── Filters ─────────────────────────────────────── */}
      <div style={{ display: "flex", gap: "4px", marginBottom: "16px" }}>
        {(["all", "active", "done"] as const).map((f) => (
          <button
            key={f}
            className={`btn ${filter === f ? "btn-primary" : "btn-secondary"}`}
            onClick={() => setFilter(f)}
            style={{ fontSize: "13px", padding: "5px 14px" }}
          >
            {f === "all" ? "Toutes" : f === "active" ? "À faire" : "Terminées"}
          </button>
        ))}

        {stats.done > 0 && (
          <button
            className="btn btn-ghost"
            style={{ marginLeft: "auto", fontSize: "13px", color: "var(--error)" }}
            onClick={() => todos.filter((t) => t.done).forEach((t) => removeTodo(t.id))}
          >
            🗑 Supprimer terminées
          </button>
        )}
      </div>

      {/* ── Todo list ─────────────────────────────────────── */}
      {filtered.length === 0 ? (
        <div
          style={{
            textAlign: "center",
            padding: "48px",
            color: "var(--text-tertiary)",
            fontSize: "13px",
          }}
        >
          {filter === "done"
            ? "Aucune tâche terminée"
            : "Aucune tâche — profitez-en ! 🎉"}
        </div>
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: "6px" }}>
          {filtered.map((todo) => (
            <TodoItem
              key={todo.id}
              todo={todo}
              isEditing={editId === todo.id}
              editText={editText}
              onToggle={() => toggleTodo(todo.id)}
              onRemove={() => removeTodo(todo.id)}
              onStartEdit={() => startEdit(todo)}
              onEditChange={setEditText}
              onEditCommit={() => commitEdit(todo.id)}
              onEditKeyDown={(e) => {
                if (e.key === "Enter") commitEdit(todo.id);
                if (e.key === "Escape") setEditId(null);
              }}
            />
          ))}
        </div>
      )}
    </div>
  );
}

/* ── TodoItem ───────────────────────────────────────────── */
function TodoItem({
  todo,
  isEditing,
  editText,
  onToggle,
  onRemove,
  onStartEdit,
  onEditChange,
  onEditCommit,
  onEditKeyDown,
}: {
  todo: Todo;
  isEditing: boolean;
  editText: string;
  onToggle: () => void;
  onRemove: () => void;
  onStartEdit: () => void;
  onEditChange: (v: string) => void;
  onEditCommit: () => void;
  onEditKeyDown: (e: React.KeyboardEvent) => void;
}) {
  const [hovered, setHovered] = React.useState(false);

  return (
    <div
      className="card-fluent animate-fade-in"
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      style={{
        padding: "12px 14px",
        display: "flex",
        alignItems: "center",
        gap: "10px",
        opacity: todo.done ? 0.6 : 1,
        transition: "opacity 150ms",
      }}
    >
      {/* Priority dot */}
      <span
        style={{
          width: "6px",
          height: "6px",
          borderRadius: "50%",
          background: PRIORITY_COLORS[todo.priority],
          flexShrink: 0,
        }}
        title={`Priorité : ${PRIORITY_LABELS[todo.priority]}`}
      />

      {/* Checkbox */}
      <input
        type="checkbox"
        className="checkbox-win11"
        checked={todo.done}
        onChange={onToggle}
      />

      {/* Text / Edit */}
      {isEditing ? (
        <input
          autoFocus
          value={editText}
          onChange={(e) => onEditChange(e.target.value)}
          onBlur={onEditCommit}
          onKeyDown={onEditKeyDown}
          className="input-win11"
          style={{ flex: 1, padding: "2px 6px" }}
        />
      ) : (
        <span
          onDoubleClick={onStartEdit}
          style={{
            flex: 1,
            textDecoration: todo.done ? "line-through" : "none",
            color: todo.done ? "var(--text-tertiary)" : "var(--text-primary)",
            fontSize: "14px",
            cursor: "text",
            overflow: "hidden",
            textOverflow: "ellipsis",
            whiteSpace: "nowrap",
          }}
          title={todo.text}
        >
          {todo.text}
        </span>
      )}

      {/* Actions */}
      <div
        style={{
          display: "flex",
          gap: "4px",
          opacity: hovered || isEditing ? 1 : 0,
          transition: "opacity 120ms",
        }}
      >
        <button
          className="btn btn-ghost"
          style={{ padding: "4px 8px", fontSize: "13px" }}
          onClick={onStartEdit}
          title="Modifier"
        >
          ✏️
        </button>
        <button
          className="btn btn-ghost"
          style={{ padding: "4px 8px", fontSize: "13px", color: "var(--error)" }}
          onClick={onRemove}
          title="Supprimer"
        >
          🗑
        </button>
      </div>
    </div>
  );
}
