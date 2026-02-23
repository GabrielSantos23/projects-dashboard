class Project < ApplicationRecord
  validates :name, presence: true
  validates :path, presence: true, uniqueness: { case_sensitive: false }

  def metadata
    JSON.parse(metadata_json.presence || "{}")
  rescue JSON::ParserError
    {}
  end

  def metadata=(hash)
    self.metadata_json = hash.to_json
  end

  def tag_list
    return [] if tags.blank?
    tags.split(",").map(&:strip).reject(&:blank?)
  end

  def goal_items
    JSON.parse(goals.presence || "[]")
  rescue JSON::ParserError
    []
  end

  def goal_items=(items)
    self.goals = items.to_json
  end

  def status
    return "Unknown" if last_commit.nil?
    days = (Time.current - last_commit).to_f / 1.day
    return "Active"   if days <= 7
    return "Recent"   if days <= 30
    return "Stalled"  if days <= 90
    "Archived"
  end

  def time_ago
    return "never" if last_commit.nil?
    span = Time.current - last_commit
    minutes = span / 60
    hours   = span / 3600
    days    = span / 86400

    return "just now"                               if minutes < 1
    return "#{minutes.to_i}m ago"                   if hours < 1
    return "#{hours.to_i}h ago"                     if days < 1
    return "#{days.to_i}d ago"                      if days < 7
    weeks = (days / 7).to_i
    return "#{weeks} week#{'s' if weeks != 1} ago"  if days < 30
    months = (days / 30).to_i
    return "#{months} month#{'s' if months != 1} ago" if days < 365
    years = (days / 365).to_i
    "#{years} year#{'s' if years != 1} ago"
  end

  def tech_pills
    meta = metadata
    if meta["techs"].present?
      meta["techs"].split(",").map(&:strip).reject(&:blank?)
    elsif meta["inferred_type"].present? && meta["inferred_type"] != "Unknown"
      [meta["inferred_type"]]
    else
      []
    end
  end

  def contributor_count
    meta = metadata
    meta["contributors"].to_i
  end

  def total_commits
    meta = metadata
    meta["total_commits"].to_i
  end

  def branch_count
    meta = metadata
    meta["branch_count"].to_i
  end

  def doc_count
    meta = metadata
    meta["doc_count"].to_i
  end

  def contributor_names
    meta = metadata
    return [] if meta["contributor_names"].blank?
    meta["contributor_names"].split(",").map(&:strip)
  end

  def recent_commits
    meta = metadata
    return [] if meta["recent_commits"].blank?
    meta["recent_commits"].split(";;").filter_map do |entry|
      parts = entry.split("|", 3)
      date = Time.parse(parts[0]) rescue nil
      next nil unless date
      { date: date, author: parts[1] || "unknown", message: parts[2] || "" }
    end
  end

  def ownership_label
    is_forked ? "Fork" : "Owned"
  end

  def initial
    name.present? ? name[0].upcase : "?"
  end

  def status_color
    case status
    when "Active"  then "#34c759"
    when "Recent"  then "#0088ff"
    when "Stalled" then "#ffcc00"
    else "#8b8b90"
    end
  end

  def stall_risk
    case status
    when "Active"  then { label: "Low",      color: "#34c759", width: "15%" }
    when "Recent"  then { label: "Normal",   color: "#0088ff", width: "50%" }
    when "Stalled" then { label: "High",     color: "#ffcc00", width: "85%" }
    else                { label: "Archived", color: "#5b5b60", width: "100%" }
    end
  end
end
