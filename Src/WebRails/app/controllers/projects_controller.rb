class ProjectsController < ApplicationController
  before_action :set_project, only: [:show, :destroy, :rescan, :toggle_pin, :open_terminal, :open_editor]

  def index
    @search_query = params[:q].to_s
    @projects = Project.order(:name)
    @filtered_projects = if @search_query.present?
      @projects.where("name LIKE ? OR path LIKE ?", "%#{@search_query}%", "%#{@search_query}%")
    else
      @projects
    end
    @scan_folders = ScanFolder.order(:created_at)
  end

  def show
    @scan_folders = ScanFolder.order(:created_at)
    meta = @project.metadata
    @editor_name = session[:editor_name] || "Cursor"
  end

  def destroy
    path = @project.path
    @project.destroy

    if Dir.exist?(path)
      FileUtils.rm_rf(path)
    end

    redirect_to root_path, notice: "Project deleted."
  end

  def rescan
    scanner = ScannerService.new
    scanner.scan(@project.path, 9999)
    redirect_to project_path(@project), notice: "Project rescanned."
  end

  def toggle_pin
    @project.update!(is_pinned: !@project.is_pinned)
    redirect_to project_path(@project)
  end

  def open_terminal
    if Gem.win_platform? && Dir.exist?(@project.path)
      system("start cmd /k cd /d \"#{@project.path}\"")
    end
    head :ok
  end

  def open_editor
    editor = session[:editor_name] || "Cursor"
    cmd = case editor
          when "VS Code" then "code ."
          when "Rider"   then "rider64 ."
          when "Zed"     then "zed ."
          when "Antigravity" then "antigravity ."
          else "cursor ."
          end

    if Dir.exist?(@project.path)
      system("start /d \"#{@project.path}\" cmd /c #{cmd}")
    end
    head :ok
  end

  private

  def set_project
    @project = Project.find(params[:id])
  end
end
