class CreateScanFolders < ActiveRecord::Migration[8.0]
  def change
    create_table :scan_folders do |t|
      t.string :path, null: false

      t.timestamps
    end

    add_index :scan_folders, :path, unique: true
  end
end
