// Prevents additional console window on Windows in release.
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod commands;
mod tray;

use tauri::{Manager, WindowBuilder, WindowUrl};

fn main() {
    let tray = tray::build_tray();

    tauri::Builder::default()
        .system_tray(tray)
        .on_system_tray_event(tray::handle_tray_event)
        .invoke_handler(tauri::generate_handler![
            commands::open_widget_window,
            commands::close_widget_window,
            commands::get_app_version,
        ])
        .setup(|app| {
            // Main window is created via tauri.conf.json
            // Optionally hide from taskbar initially if needed
            #[cfg(target_os = "windows")]
            {
                let main_window = app.get_window("main").unwrap();
                // Make window appear in taskbar
                main_window.set_skip_taskbar(false).ok();
            }
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running Mushub");
}
