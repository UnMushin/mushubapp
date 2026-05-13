import { create } from "zustand";
import { persist } from "zustand/middleware";

/* ─── Types ─────────────────────────────────────────────── */

export interface Todo {
  id: string;
  text: string;
  done: boolean;
  createdAt: number;
  priority: "low" | "medium" | "high";
}

export interface HubItem {
  id: string;
  label: string;
  url?: string;       // site web ou chemin app
  appPath?: string;   // chemin vers un exe
  icon: string;       // emoji ou url d'icône
  type: "web" | "app" | "folder";
  color: string;      // couleur de fond de la tile
  order: number;
}

export interface PomodoroSettings {
  workMinutes: number;
  shortBreakMinutes: number;
  longBreakMinutes: number;
  sessionsBeforeLong: number;
  autoStartBreaks: boolean;
  autoStartWork: boolean;
  tickSoundEnabled: boolean;
  notifEnabled: boolean;
}

export type PomodoroPhase = "work" | "short-break" | "long-break";

/* ─── Store ─────────────────────────────────────────────── */

interface MushubState {
  // Todos
  todos: Todo[];
  addTodo: (text: string, priority?: Todo["priority"]) => void;
  toggleTodo: (id: string) => void;
  removeTodo: (id: string) => void;
  editTodo: (id: string, text: string) => void;
  reorderTodos: (from: number, to: number) => void;

  // Hub
  hubItems: HubItem[];
  addHubItem: (item: Omit<HubItem, "id" | "order">) => void;
  updateHubItem: (id: string, updates: Partial<HubItem>) => void;
  removeHubItem: (id: string) => void;
  reorderHubItems: (newOrder: HubItem[]) => void;

  // Pomodoro settings
  pomodoroSettings: PomodoroSettings;
  updatePomodoroSettings: (s: Partial<PomodoroSettings>) => void;

  // Active page
  activePage: "home" | "pomodoro" | "hub";
  setActivePage: (p: MushubState["activePage"]) => void;
}

const defaultPomodoroSettings: PomodoroSettings = {
  workMinutes: 25,
  shortBreakMinutes: 5,
  longBreakMinutes: 15,
  sessionsBeforeLong: 4,
  autoStartBreaks: false,
  autoStartWork: false,
  tickSoundEnabled: false,
  notifEnabled: true,
};

const defaultHubItems: HubItem[] = [
  { id: "h1", label: "GitHub",    url: "https://github.com",          icon: "🐙", type: "web", color: "#24292e", order: 0 },
  { id: "h2", label: "YouTube",   url: "https://youtube.com",         icon: "▶️", type: "web", color: "#ff0000", order: 1 },
  { id: "h3", label: "ChatGPT",   url: "https://chat.openai.com",     icon: "🤖", type: "web", color: "#10a37f", order: 2 },
  { id: "h4", label: "Spotify",   url: "https://open.spotify.com",    icon: "🎵", type: "web", color: "#1db954", order: 3 },
  { id: "h5", label: "Notion",    url: "https://notion.so",           icon: "📝", type: "web", color: "#000000", order: 4 },
  { id: "h6", label: "Figma",     url: "https://figma.com",           icon: "🎨", type: "web", color: "#f24e1e", order: 5 },
];

export const useMushubStore = create<MushubState>()(
  persist(
    (set, get) => ({
      // ── Todos ──────────────────────────────────────────
      todos: [],
      addTodo: (text, priority = "medium") =>
        set((s) => ({
          todos: [
            ...s.todos,
            { id: crypto.randomUUID(), text, done: false, createdAt: Date.now(), priority },
          ],
        })),
      toggleTodo: (id) =>
        set((s) => ({
          todos: s.todos.map((t) => (t.id === id ? { ...t, done: !t.done } : t)),
        })),
      removeTodo: (id) =>
        set((s) => ({ todos: s.todos.filter((t) => t.id !== id) })),
      editTodo: (id, text) =>
        set((s) => ({
          todos: s.todos.map((t) => (t.id === id ? { ...t, text } : t)),
        })),
      reorderTodos: (from, to) =>
        set((s) => {
          const arr = [...s.todos];
          const [item] = arr.splice(from, 1);
          arr.splice(to, 0, item);
          return { todos: arr };
        }),

      // ── Hub ────────────────────────────────────────────
      hubItems: defaultHubItems,
      addHubItem: (item) =>
        set((s) => ({
          hubItems: [
            ...s.hubItems,
            { ...item, id: crypto.randomUUID(), order: s.hubItems.length },
          ],
        })),
      updateHubItem: (id, updates) =>
        set((s) => ({
          hubItems: s.hubItems.map((h) => (h.id === id ? { ...h, ...updates } : h)),
        })),
      removeHubItem: (id) =>
        set((s) => ({ hubItems: s.hubItems.filter((h) => h.id !== id) })),
      reorderHubItems: (newOrder) => set({ hubItems: newOrder }),

      // ── Pomodoro ───────────────────────────────────────
      pomodoroSettings: defaultPomodoroSettings,
      updatePomodoroSettings: (s) =>
        set((state) => ({
          pomodoroSettings: { ...state.pomodoroSettings, ...s },
        })),

      // ── Navigation ─────────────────────────────────────
      activePage: "home",
      setActivePage: (p) => set({ activePage: p }),
    }),
    {
      name: "mushub-storage",
      // Only persist these keys
      partialize: (s) => ({
        todos: s.todos,
        hubItems: s.hubItems,
        pomodoroSettings: s.pomodoroSettings,
      }),
    }
  )
);
