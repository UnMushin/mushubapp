use tauri::{
    AppHandle, CustomMenuItem, Manager,
    SystemTray, SystemTrayEvent, SystemTrayMenu, SystemTrayMenuItem,
};

/// Construit le menu du system tray
pub fn build_tray() -> SystemTray {
    let open_widget  = CustomMenuItem::new("open_widget",  "⚡ Ouvrir le Hub widget");
    let open_main    = CustomMenuItem::new("open_main",    "🍄 Ouvrir Mushub");
    let separator    = SystemTrayMenuItem::Separator;
    let quit         = CustomMenuItem::new("quit",         "✕ Quitter");

    let menu = SystemTrayMenu::new()
        .add_item(open_widget)
        .add_item(open_main)
        .add_native_item(separator)
        .add_item(quit);

    SystemTray::new()
        .with_menu(menu)
        .with_tooltip("Mushub")
}

/// Gestion des événements du tray
pub fn handle_tray_event(app: &AppHandle, event: SystemTrayEvent) {
    match event {
        // Double-clic sur l'icône tray → ouvrir/montrer le widget
        SystemTrayEvent::DoubleClick { .. } => {
            toggle_widget(app);
        }

        // Clic gauche simple → ouvrir le widget aussi
        SystemTrayEvent::LeftClick { .. } => {
            toggle_widget(app);
        }

        // Menu items
        SystemTrayEvent::MenuItemClick { id, .. } => match id.as_str() {
            "open_widget" => {
                toggle_widget(app);
            }
            "open_main" => {
                if let Some(window) = app.get_window("main") {
                    window.show().ok();
                    window.set_focus().ok();
                    window.unminimize().ok();
                }
            }
            "quit" => {
                std::process::exit(0);
            }
            _ => {}
        },

        _ => {}
    }
}

/// Toggle le widget flottant
fn toggle_widget(app: &AppHandle) {
    if let Some(window) = app.get_window("widget") {
        if window.is_visible().unwrap_or(false) {
            window.hide().ok();
        } else {
            // Position near taskbar (bottom-right)
            position_widget_near_tray(app, &window);
            window.show().ok();
            window.set_focus().ok();
        }
    } else {
        // Create it
        let app_clone = app.clone();
        tauri::async_runtime::spawn(async move {
            crate::commands::open_widget_window(app_clone).await.ok();
        });
    }
}

/// Place le widget en bas à droite de l'écran (près de la taskbar)
fn position_widget_near_tray(app: &AppHandle, window: &tauri::Window) {
    if let Ok(monitor) = window.primary_monitor() {
        if let Some(monitor) = monitor {
            let size     = monitor.size();
            let scale    = monitor.scale_factor();
            let w_phys   = (360.0 * scale) as u32;
            let h_phys   = (280.0 * scale) as u32;
            let margin_x = (12.0 * scale) as i32;
            let margin_y = (48.0 * scale) as i32; // above taskbar

            let x = (size.width  as i32) - (w_phys as i32) - margin_x;
            let y = (size.height as i32) - (h_phys as i32) - margin_y;

            window
                .set_position(tauri::PhysicalPosition { x, y })
                .ok();
        }
    }
}
