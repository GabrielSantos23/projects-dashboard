require_relative 'config/environment'
require_relative 'app/services/scanner_service'
ss = ScannerService.new
folders = ss.send(:find_git_folders, 'D:\\projects')
puts "Found #{folders.length} git repos."

folders.each do |f|
  parent = File.dirname(f)
  print "Testing #{parent} - "
  stdout, err, status = Open3.capture3("git log -1 --format=%cI", chdir: parent)
  unless status.success? && !stdout.strip.empty?
    puts "FAILED INIT LOG: out=#{stdout.inspect}, err=#{err.inspect}"
    next
  end

  msg, err2, msgstat = Open3.capture3("git log -1 --format=%B", chdir: parent)
  unless msgstat.success?
    puts "FAILED MSG LOG: err=#{err2.inspect}"
    next
  end
  
  log_out, err_out, _ = Open3.capture3("git log -n 500 --format=\"%aN|%cI|%s\"", chdir: parent)
  unless _ .success?
    puts "FAILED 500 LOG: err=#{err_out.inspect}"
    next
  end

  puts "OK"
end
