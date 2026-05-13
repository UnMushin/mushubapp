# 🍄 Mushub

> **Productivity hub Windows 11** — Todo list, Pomodoro, App Hub StreamDeck-like, widget flottant & auto-updater cute.

[![GitHub release](https://img.shields.io/github/v/release/UnMushin/mushubapp?style=flat-square)](https://github.com/UnMushin/mushubapp/releases)
[![Tauri](https://img.shields.io/badge/Tauri-1.7-blue?style=flat-square)](https://tauri.app)
[![React](https://img.shields.io/badge/React-18-61dafb?style=flat-square)](https://react.dev)

---

## ✨ Fonctionnalités

| Module | Description |
|---|---|
| **Todo list** | Page d'accueil avec message contextuel (matin/après-midi/soir), priorités, filtres, stats |
| **Pomodoro** | Minuteur circulaire style Win11 Clock, entièrement configurable, notifications |
| **App Hub** | Grille StreamDeck-like, drag & drop, ajout/suppression de raccourcis (sites & apps) |
| **Widget flottant** | Mini-fenêtre always-on-top, accessible depuis le system tray |
| **System Tray** | Icône tray + icône barre des tâches, clic pour ouvrir le widget |
| **Auto-updater** | Exécutable séparé avec UI terminal "cute", vérifie GitHub au démarrage |

---

## 🏗️ Architecture

```
mushub/
├── src-tauri/          # Backend Rust (Tauri)
│   └── src/
│       ├── main.rs     # Entrée Tauri + system tray
│       ├── commands.rs # Commandes invoke
│       └── tray.rs     # Tray icon + widget toggle
├── src/                # Frontend React
│   ├── pages/
│   │   ├── Home.tsx    # Todo list
│   │   ├── Pomodoro.tsx
│   │   └── Hub.tsx     # StreamDeck grid
│   ├── components/
│   │   ├── TitleBar.tsx
│   │   ├── Sidebar.tsx
│   │   └── HubEditor.tsx
│   ├── windows/
│   │   └── Widget.tsx  # Mini fenêtre flottante
│   └── store/
│       └── index.ts    # Zustand store (persisté)
└── updater/            # Auto-updater (crate séparée)
    ├── src/main.rs     # Logique Rust + API GitHub
    └── updater.html    # UI terminal cute
```

---

## 🚀 Démarrage rapide

### Prérequis

- [Node.js](https://nodejs.org) ≥ 18
- [Rust](https://rustup.rs) (stable)
- [Tauri CLI](https://tauri.app/v1/guides/getting-started/prerequisites)

```bash
# Windows (PowerShell)
winget install Microsoft.VisualStudio.2022.BuildTools
winget install Rustlang.Rust.MSVC
```

### Installation

```bash
git clone https://github.com/UnMushin/mushubapp.git
cd mushubapp
npm install
```

### Développement

```bash
npm run tauri dev
```

### Build production

```bash
npm run tauri build
```

Les binaires seront dans `src-tauri/target/release/bundle/`.

### Build de l'updater

```bash
cd updater
cargo tauri build
```

---

## ⚙️ Configuration

### Ajouter un raccourci Hub

1. Ouvrez Mushub → onglet **App Hub**
2. Cliquez sur **+ Ajouter** ou sur la tuile `+`
3. Choisissez **Site web** ou **Application**
4. Remplissez le nom, l'URL/chemin, l'emoji et la couleur
5. Cliquez **Ajouter**

Les raccourcis sont **glissables** pour être réorganisés.

### Paramètres Pomodoro

Ouvrez l'onglet **Pomodoro** puis cliquez sur ⚙️ pour ajuster :
- Durée de travail (1–120 min)
- Pause courte (1–30 min)  
- Longue pause (5–60 min)
- Sessions avant longue pause
- Auto-start breaks/work
- Notifications

---

## 🔄 Système de mises à jour

Au démarrage, **`mushub-updater.exe`** se lance en premier :

1. Vérifie la version actuelle (`Cargo.toml`)
2. Contacte l'API GitHub : `GET /repos/UnMushin/mushubapp/releases/latest`
3. Compare les versions (semver)
4. **Si à jour** → lance `mushub.exe` automatiquement après 1.5s
5. **Si mise à jour dispo** → affiche les notes de release + bouton de téléchargement
6. **Si erreur** → affiche une croix ASCII + message d'erreur détaillé

### Publier une release (pour le développeur)

```bash
# 1. Bumper la version dans src-tauri/Cargo.toml et package.json
# 2. Builder
npm run tauri build
# 3. Créer une GitHub Release avec le tag vX.Y.Z
# 4. Uploader les fichiers .msi / setup.exe en assets de la release
```

---

## 🎨 Design

- **Fluent Design** Windows 11 (acrylic, rounded corners, Segoe UI Variable)
- Thème sombre avec accent bleu `#0078d4`
- Animations CSS légères (fade-in, slide)
- Widget transparent avec backdrop-filter blur

---

## 🐛 Signaler un problème

👉 [github.com/UnMushin/mushubapp/issues](https://github.com/UnMushin/mushubapp/issues)

---

## 📝 Licence

MIT — © UnMushin
