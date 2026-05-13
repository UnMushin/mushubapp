// Prevents console window on Windows in release
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use serde::Deserialize;
use std::process::Command;
use std::sync::{Arc, Mutex};
use tauri::{AppHandle, Manager, Window};

// ─── GitHub API response ───────────────────────────────────────────────────
#[derive(Debug, Deserialize)]
struct GithubRelease {
    tag_name: String,
    html_url: String,
    assets: Vec<GithubAsset>,
    body: Option<String>,
}

#[derive(Debug, Deserialize)]
struct GithubAsset {
    name: String,
    browser_download_url: String,
}

// ─── State shared between backend and frontend ─────────────────────────────
#[derive(Clone, serde::Serialize)]
struct LogMessage {
    kind: String,     // "info" | "success" | "warn" | "error" | "step"
    text: String,
}

type LogSink = Arc<Mutex<Vec<LogMessage>>>;

// ─── Tauri commands ────────────────────────────────────────────────────────

/// Called by the frontend on load — runs the full update check pipeline
#[tauri::command]
async fn run_update_check(window: Window) -> Result<UpdateResult, String> {
    let current_version = env!("CARGO_PKG_VERSION").to_string();

    // Helper closure to emit a log line to the frontend
    let emit = |kind: &str, text: &str| {
        window
            .emit(
                "updater-log",
                LogMessage {
                    kind: kind.to_string(),
                    text: text.to_string(),
                },
            )
            .ok();
        // Small artificial delay for readability
        std::thread::sleep(std::time::Duration::from_millis(280));
    };

    emit("step",    "[ Mushub Updater v0.1 ]");
    emit("info",    "Initialisation du vérificateur de mises à jour...");
    emit("info",    &format!("Version actuelle : {}", current_version));
    emit("info",    "Connexion à GitHub...");

    // ── Fetch latest release ─────────────────────────────────────────────
    let client = reqwest::Client::builder()
        .user_agent("mushub-updater/0.1")
        .build()
        .map_err(|e| e.to_string())?;

    let response = client
        .get("https://api.github.com/repos/UnMushin/mushubapp/releases/latest")
        .send()
        .await;

    let release: GithubRelease = match response {
        Err(e) => {
            emit("error", &format!("Échec de la connexion à GitHub : {}", e));
            emit("error", "Vérifiez votre connexion internet.");
            return Ok(UpdateResult {
                status: "error".into(),
                error: Some(format!(
                    "Impossible de contacter l'API GitHub.\n\nErreur: {}\n\nSolutions possibles:\n• Vérifiez votre connexion internet\n• Vérifiez si github.com est accessible\n• Signaler: https://github.com/UnMushin/mushubapp/issues",
                    e
                )),
                latest_version: None,
                download_url: None,
                release_notes: None,
            });
        }
        Ok(resp) => {
            if !resp.status().is_success() {
                let status = resp.status();
                let msg = if status.as_u16() == 404 {
                    "Aucune release trouvée sur GitHub.\nLe repository n'a pas encore de release publiée.".to_string()
                } else {
                    format!("Erreur HTTP {} depuis GitHub.", status)
                };
                emit("error", &msg);
                return Ok(UpdateResult {
                    status: "error".into(),
                    error: Some(format!(
                        "{}\n\nSignaler ce problème : https://github.com/UnMushin/mushubapp/issues",
                        msg
                    )),
                    latest_version: None,
                    download_url: None,
                    release_notes: None,
                });
            }

            match resp.json::<GithubRelease>().await {
                Err(e) => {
                    emit("error", &format!("Impossible de lire la réponse GitHub : {}", e));
                    return Ok(UpdateResult {
                        status: "error".into(),
                        error: Some(format!(
                            "Format de réponse GitHub inattendu.\n\nErreur: {}\n\nSignaler: https://github.com/UnMushin/mushubapp/issues",
                            e
                        )),
                        latest_version: None,
                        download_url: None,
                        release_notes: None,
                    });
                }
                Ok(r) => r,
            }
        }
    };

    emit("success", &format!("Dernière version disponible : {}", release.tag_name));

    // ── Compare versions ─────────────────────────────────────────────────
    let latest = release.tag_name.trim_start_matches('v');
    let current = current_version.trim_start_matches('v');

    emit("info", "Comparaison des versions...");

    let is_newer = match (semver::Version::parse(latest), semver::Version::parse(current)) {
        (Ok(l), Ok(c)) => l > c,
        _ => latest != current, // Fallback to string comparison
    };

    if !is_newer {
        emit("success", "✓ Vous avez déjà la dernière version !");
        emit("success", "Tout est à jour. Lancement de Mushub...");
        return Ok(UpdateResult {
            status: "up_to_date".into(),
            error: None,
            latest_version: Some(release.tag_name),
            download_url: None,
            release_notes: None,
        });
    }

    // ── Update available ──────────────────────────────────────────────────
    emit("warn",    &format!("Mise à jour disponible : {} → {}", current_version, release.tag_name));
    emit("info",    "Recherche de l'installeur Windows...");

    // Find .msi or .exe installer
    let installer_asset = release
        .assets
        .iter()
        .find(|a| a.name.ends_with(".msi") || a.name.ends_with("_setup.exe"));

    let download_url = installer_asset
        .map(|a| a.browser_download_url.clone())
        .unwrap_or_else(|| release.html_url.clone());

    let release_notes = release.body.clone();

    emit("info", "Mise à jour disponible et prête à installer.");

    Ok(UpdateResult {
        status: "update_available".into(),
        error: None,
        latest_version: Some(release.tag_name),
        download_url: Some(download_url),
        release_notes,
    })
}

#[derive(Clone, serde::Serialize)]
struct UpdateResult {
    status: String,
    error: Option<String>,
    latest_version: Option<String>,
    download_url: Option<String>,
    release_notes: Option<String>,
}

/// Lance Mushub principal et ferme l'updater
#[tauri::command]
async fn launch_mushub_and_exit(app: AppHandle) {
    // Try to find mushub.exe next to updater
    let exe_dir = std::env::current_exe()
        .ok()
        .and_then(|p| p.parent().map(|d| d.to_path_buf()));

    if let Some(dir) = exe_dir {
        let mushub_exe = dir.join("mushub.exe");
        if mushub_exe.exists() {
            Command::new(mushub_exe).spawn().ok();
        }
    }
    app.exit(0);
}

/// Ouvre le lien de téléchargement dans le navigateur
#[tauri::command]
async fn open_download_url(url: String) {
    opener::open(&url).ok();
}

// ─── Main ──────────────────────────────────────────────────────────────────
fn main() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![
            run_update_check,
            launch_mushub_and_exit,
            open_download_url,
        ])
        .run(tauri::generate_context!())
        .expect("error running updater");
}
