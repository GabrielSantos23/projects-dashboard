module ApplicationHelper
  def status_classes(status)
    case status
    when "Active"  then "text-[#34c759] bg-[#34c759]/10"
    when "Recent"  then "text-[#0088ff] bg-[#0088ff]/10"
    when "Stalled" then "text-[#ffcc00] bg-[#ffcc00]/10"
    when "Archived" then "text-[#8b8b90] bg-[#8b8b90]/10"
    else "text-[#8b8b90] bg-[#8b8b90]/10"
    end
  end

  def nav_class(href)
    path = request.path
    target = href.delete_prefix("/")

    if target.blank?
      path == "/" ? "bg-white/10 text-white" : "text-[#8b8b90] hover:bg-white/5 hover:text-[#d4d4d8]"
    elsif path.start_with?("/#{target}")
      "bg-white/10 text-white"
    else
      "text-[#8b8b90] hover:bg-white/5 hover:text-[#d4d4d8]"
    end
  end

  def format_commit_date(date)
    span = Time.current - date
    days = span / 86400
    hours = span / 3600
    return "#{hours.to_i}h ago" if days < 1
    return "#{days.to_i}d ago" if days < 7
    date.strftime("%b %d, %Y")
  end

  TECH_COLORS = %w[#0088ff #ff3b30 #ffcc00 #34c759 #00E5FF].freeze

  def tech_color(index)
    TECH_COLORS[index % TECH_COLORS.length]
  end

  def tech_icon_svg(tech_name)
    filename = tech_name.to_s.downcase + ".svg"
    filepath = Rails.root.join('..', 'Shared', 'svgs', filename)
    if File.exist?(filepath)
      File.read(filepath).html_safe
    else
      nil
    end
  end
end
