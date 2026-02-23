class ScansController < ApplicationController
  def create
    folders = ScanFolder.all

    Thread.new do
      scanner = ScannerService.new
      folders.each do |folder|
        scanner.scan(folder.path, params[:cutoff_days]&.to_i || 999_999)
      end
    end

    redirect_to root_path, notice: "Scan started in the background. Refresh the page in a moment to see results."
  end
end
