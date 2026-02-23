class DashboardController < ApplicationController
  def index
    @projects = Project.order(last_commit: :desc)

    now = Time.current
    @total_count    = @projects.count
    @active_count   = @projects.count { |p| p.status == "Active" || (p.last_commit && (now - p.last_commit) <= 7.days) }
    @stalled_count  = @projects.count { |p| p.status == "Stalled" || (p.last_commit && (now - p.last_commit) > 30.days) }
    @archived_count = @projects.count { |p| p.status == "Archived" }

    tech_groups = @projects.flat_map(&:tech_pills)
                           .tally
                           .sort_by { |_, count| -count }
    @top_techs = tech_groups.first(5).map { |name, count| { name: name, count: count } }
    @total_tech_count = tech_groups.sum { |_, count| count }

    @search_query = params[:q].to_s
    @filtered_projects = if @search_query.present?
      @projects.select { |p| p.name.downcase.include?(@search_query.downcase) || p.path.downcase.include?(@search_query.downcase) }
    else
      @projects
    end

    @scan_folders = ScanFolder.order(:created_at)
  end
end
