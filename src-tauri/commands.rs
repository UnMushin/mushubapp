use tauri::{AppHandle, Manager, WindowBuilder, WindowUrl};

/// Ouvre (ou met au premier plan) la fenêtre widget flottante
#[tauri::command]
pub async fn open_widget_window(app: AppHandle) -> Result<(), String> {
    // Check if already open
    if let Some(window) = app.get_window("widget") {
        window.show().map_err(|e| e.to_string())?;
        window.set_focus().map_err(|e| e.to_string())?;
        return Ok(());
    }

    // Create widget window
    WindowBuilder::new(
        &app,
        "widget",
        WindowUrl::App("widget.html".into()),
    )
    .title("Mushub Widget")
    .inner_size(360.0, 280.0)
    .resizable(false)
    .decorations(false)
    .always_on_top(true)
    .skip_taskbar(true)
    .transparent(true)
    .visible(true)
    .build()
    .map_err(|e| e.to_string())?;

    Ok(())
}

/// Ferme la fenêtre widget si elle existe
#[tauri::command]
pub async fn close_widget_window(app: AppHandle) -> Result<(), String> {
    if let Some(window) = app.get_window("widget") {
        window.close().map_err(|e| e.to_string())?;
    }
    Ok(())
}

/// Retourne la version actuelle de l'application
#[tauri::command]
pub fn get_app_version(app: AppHandle) -> String {
    app.package_info().version.to_string()
}
