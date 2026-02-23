class ActivityController < ApplicationController
  def index
    @search_query = params[:q].to_s
    @projects = Project.order(last_commit: :desc).limit(50)
    @filtered_projects = if @search_query.present?
      @projects.select { |p| p.name.downcase.include?(@search_query.downcase) || p.path.downcase.include?(@search_query.downcase) }
    else
      @projects.to_a
    end
    @scan_folders = ScanFolder.order(:created_at)
  end
end
