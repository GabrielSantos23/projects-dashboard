class SettingsController < ApplicationController
  def show
    @folders = ScanFolder.order(:created_at)
    @editor = session[:editor_name] || "Cursor"
    @watcher_enabled = session[:watcher_enabled] != false
    @selected_theme = (session[:theme] || 1).to_i
    @scan_folders = @folders
  end

  def update
    session[:editor_name] = params[:editor_name] if params[:editor_name].present?
    session[:watcher_enabled] = params[:watcher_enabled] == "1"
    session[:theme] = params[:theme].to_i if params[:theme].present?
    redirect_to settings_path, notice: "Settings saved."
  end

  def add_folder
    path = params[:folder_path].to_s.strip
    if path.blank?
      redirect_to settings_path, alert: "Path cannot be empty."
    elsif !Dir.exist?(path)
      redirect_to settings_path, alert: "Folder does not exist: #{path}"
    elsif ScanFolder.exists?(path: path)
      redirect_to settings_path, alert: "This folder is already in the list."
    else
      ScanFolder.create!(path: path)
      redirect_to settings_path, notice: "Folder added."
    end
  end

  def remove_folder
    folder = ScanFolder.find(params[:folder_id])
    folder.destroy
    redirect_to settings_path, notice: "Folder removed."
  end
end
