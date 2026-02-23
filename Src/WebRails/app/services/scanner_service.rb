require "open3"
require "time"

class ScannerService
  IGNORED_DIRS = %w[node_modules bin obj .vs packages dist build target out .next .nuxt .cache __pycache__ .venv venv vendor .tox coverage].freeze

  def scan(root_path, cutoff_days = 9999)
    return unless Dir.exist?(root_path)

    cutoff_date = Time.now.utc - (cutoff_days * 86400)
    git_folders = find_git_folders(root_path)
    Rails.logger.info "Found #{git_folders.length} git repos under #{root_path}"

    git_folders.each do |git_dir|
      process_repo(git_dir, cutoff_date)
    rescue StandardError => e
      Rails.logger.error "Failed to process #{git_dir}: #{e.message}"
    end
  end

  private

  def process_repo(git_dir, cutoff_date)
    parent_dir = File.dirname(git_dir)
    
    stdout, _, status = Open3.capture3("git log -1 --format=%cI", chdir: parent_dir)
    return unless status.success? && !stdout.strip.empty?

    last_commit_date_str = stdout.strip
    last_commit_date = Time.iso8601(last_commit_date_str).utc rescue nil
    return unless last_commit_date
    return if last_commit_date < cutoff_date

    normalized_path = File.expand_path(parent_dir).tr('\\', '/').downcase
    project_name = File.basename(parent_dir)
    project = Project.find_or_initialize_by(path: normalized_path)
    project.name = project_name
    project.last_commit = last_commit_date
    
    msg, _, _ = Open3.capture3("git log -1 --format=%B", chdir: parent_dir)
    project.last_commit_message = msg.lines.first&.strip || ""
    
    branch, _, _ = Open3.capture3("git branch --show-current", chdir: parent_dir)
    project.current_branch = branch.strip.presence || "main"
    project.scanned_at = Time.current

    origin_url, _, _ = Open3.capture3("git remote get-url origin", chdir: parent_dir)
    upstream_url, _, _ = Open3.capture3("git remote get-url upstream", chdir: parent_dir)
    
    project.remote_url = origin_url.strip.presence
    project.upstream_url = upstream_url.strip.presence
    project.is_forked = project.upstream_url.present?

    meta = project.metadata
    meta["inferred_type"] = detect_primary_stack(parent_dir)
    techs = detect_all_techs(parent_dir)
    meta["techs"] = techs.join(",")

    log_out, _, _ = Open3.capture3("git log -n 500 --format=\"%aN|%cI|%s\"", chdir: parent_dir)
    commits = log_out.lines.map(&:strip).reject(&:empty?)
    
    contributors = Set.new
    recent_commits_arr = []
    
    commits.each_with_index do |line, i|
      parts = line.split("|", 3)
      next unless parts.length == 3
      
      author = parts[0]
      date_str = parts[1]
      message = parts[2]
      
      contributors.add(author)
      
      if i < 10
        recent_commits_arr << "#{date_str}|#{author}|#{message.lines.first&.strip}"
      end
    end
    
    count_out, _, _ = Open3.capture3("git rev-list --count HEAD", chdir: parent_dir)
    commit_count = count_out.strip.to_i
    
    meta["contributors"] = contributors.size.to_s
    meta["contributor_names"] = contributors.to_a.first(20).join(",")
    meta["total_commits"] = commit_count.to_s
    meta["recent_commits"] = recent_commits_arr.join(";;")

    branches_out, _, _ = Open3.capture3("git branch --format=\"%(refname:short)\"", chdir: parent_dir)
    local_branches = branches_out.lines.map(&:strip).reject(&:empty?)
    
    meta["branch_count"] = local_branches.size.to_s
    meta["branches"] = local_branches.first(20).join(",")

    docs = list_docs(parent_dir)
    meta["doc_count"] = docs.size.to_s
    meta["doc_files"] = docs.first(50).join(",")

    meta["has_dockerfile"] = (File.exist?(File.join(parent_dir, "Dockerfile")) ||
      File.exist?(File.join(parent_dir, "docker-compose.yml")) ||
      File.exist?(File.join(parent_dir, "docker-compose.yaml"))).to_s
    meta["has_ci"] = (Dir.exist?(File.join(parent_dir, ".github", "workflows")) ||
      File.exist?(File.join(parent_dir, ".gitlab-ci.yml")) ||
      File.exist?(File.join(parent_dir, "Jenkinsfile"))).to_s
    meta["has_readme"] = File.exist?(File.join(parent_dir, "README.md")).to_s
    meta["has_license"] = (File.exist?(File.join(parent_dir, "LICENSE")) ||
      File.exist?(File.join(parent_dir, "LICENSE.md"))).to_s

    project.metadata = meta
    project.save!
  end

  def find_git_folders(start_location)
    repos = []
    queue = [start_location]

    while queue.any?
      current = queue.shift
      git_path = File.join(current, ".git")

      if Dir.exist?(git_path)
        repos << git_path
        next
      end

      begin
        Dir.children(current).each do |child|
          full = File.join(current, child)
          next unless File.directory?(full)
          next if IGNORED_DIRS.include?(child.downcase)
          queue << full
        end
      rescue Errno::EACCES
      end
    end

    repos
  end

  def detect_primary_stack(path)
    return "Ruby/Rails" if File.exist?(File.join(path, "Gemfile"))
    return ".NET"       if Dir.glob(File.join(path, "*.csproj")).any?
    return "Go"         if File.exist?(File.join(path, "go.mod"))
    return "Java"       if File.exist?(File.join(path, "pom.xml"))
    return "Rust"       if File.exist?(File.join(path, "Cargo.toml"))
    return "Python"     if File.exist?(File.join(path, "requirements.txt")) || File.exist?(File.join(path, "pyproject.toml"))
    return "Node/JS"    if File.exist?(File.join(path, "package.json"))
    "Unknown"
  end

  def detect_all_techs(path)
    techs = []

    files_stdout, _, _ = Open3.capture3("git", "ls-files", "--cached", "--others", "--exclude-standard", chdir: path)
    all_files = files_stdout.lines.map(&:strip).reject(&:empty?)
    extensions = all_files.map { |f| File.extname(f).downcase }.uniq
    basenames = all_files.map { |f| File.basename(f).downcase }.uniq

    if basenames.include?("gemfile")
      techs << "Ruby"
      begin
        content = File.read(File.join(path, "Gemfile"))
        techs << "Rails" if content.include?("rails")
      rescue; end
    end

    techs << "Ruby" if extensions.include?(".rb")
    techs << "C#" if extensions.include?(".cs") || basenames.any? { |f| f.end_with?(".csproj") }
    techs << ".NET" if basenames.any? { |f| f.end_with?(".csproj") || f.end_with?(".sln") }
    techs << "Go" if extensions.include?(".go") || basenames.include?("go.mod")
    techs << "Java" if extensions.include?(".java") || basenames.include?("pom.xml") || basenames.include?("build.gradle")
    techs << "Rust" if extensions.include?(".rs") || basenames.include?("cargo.toml")
    techs << "Python" if extensions.include?(".py") || basenames.include?("requirements.txt") || basenames.include?("pyproject.toml")
    
    techs << "HTML" if extensions.include?(".html") || extensions.include?(".htm")
    techs << "CSS" if extensions.include?(".css") || extensions.include?(".scss") || extensions.include?(".sass")
    techs << "PHP" if extensions.include?(".php")
    techs << "Lua" if extensions.include?(".lua")
    techs << "C" if extensions.include?(".c") || (extensions.include?(".h") && !(extensions.include?(".cpp") || extensions.include?(".cxx")))
    techs << "C++" if extensions.include?(".cpp") || extensions.include?(".hpp") || extensions.include?(".cxx") || extensions.include?(".cc")

    if basenames.include?("package.json")
      begin
        pkg_path = File.join(path, all_files.find { |f| File.basename(f).downcase == "package.json" })
        content = File.read(pkg_path)
        techs << "React" if content.include?('"react"')
        techs << "Vue" if content.include?('"vue"')
        techs << "Svelte" if content.include?('"svelte"')
        techs << "Next.js" if content.include?('"next"')
        techs << "Angular" if content.include?('"angular"')

        if content.include?('"typescript"') || basenames.include?("tsconfig.json")
          techs << "TypeScript"
        end

        techs << "Electron" if content.include?('"electron"')
        techs << "Tailwind" if content.include?('"tailwindcss"')
      rescue
      end
    end

    techs << "TypeScript" if extensions.include?(".ts") || extensions.include?(".tsx")
    techs << "JavaScript" if extensions.include?(".js") || extensions.include?(".jsx")

    techs << "Docker" if basenames.include?("dockerfile") || basenames.include?("docker-compose.yml") || basenames.include?("docker-compose.yaml")

    techs.compact.uniq
  end

  def list_docs(project_path)
    stdout, _, status = Open3.capture3("git", "ls-files", "*.md", chdir: project_path)
    if status.success?
      stdout.lines.map(&:strip).reject(&:empty?).sort
    else
      []
    end
  end
end
