class CreateProjects < ActiveRecord::Migration[8.0]
  def change
    create_table :projects do |t|
      t.string :name, null: false
      t.string :path, null: false
      t.datetime :last_commit
      t.string :last_commit_message
      t.string :current_branch
      t.boolean :is_pinned, default: false
      t.datetime :scanned_at
      t.boolean :is_forked, default: false
      t.string :remote_url
      t.string :upstream_url
      t.string :tags, default: ""
      t.text :notes, default: ""
      t.text :goals, default: "[]"
      t.text :metadata_json, default: "{}", null: false

      t.timestamps
    end

    add_index :projects, :path, unique: true
  end
end
