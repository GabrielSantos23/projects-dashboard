class ApplicationController < ActionController::Base
  helper_method :pinned_projects, :total_projects_count

  private

  def pinned_projects
    @pinned_projects ||= Project.where(is_pinned: true).order(:name)
  end

  def total_projects_count
    @total_projects_count ||= Project.count
  end
end
