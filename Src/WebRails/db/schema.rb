# This file is auto-generated from the current state of the database. Instead
# of editing this file, please use the migrations feature of Active Record to
# incrementally modify your database, and then regenerate this schema definition.
#
# This file is the source Rails uses to define your schema when running `bin/rails
# db:schema:load`. When creating a new database, `bin/rails db:schema:load` tends to
# be faster and is potentially less error prone than running all of your
# migrations from scratch. Old migrations may fail to apply correctly if those
# migrations use external dependencies or application code.
#
# It's strongly recommended that you check this file into your version control system.

ActiveRecord::Schema[8.1].define(version: 2026_02_22_000002) do
  create_table "projects", force: :cascade do |t|
    t.datetime "created_at", null: false
    t.string "current_branch"
    t.text "goals", default: "[]"
    t.boolean "is_forked", default: false
    t.boolean "is_pinned", default: false
    t.datetime "last_commit"
    t.string "last_commit_message"
    t.text "metadata_json", default: "{}", null: false
    t.string "name", null: false
    t.text "notes", default: ""
    t.string "path", null: false
    t.string "remote_url"
    t.datetime "scanned_at"
    t.string "tags", default: ""
    t.datetime "updated_at", null: false
    t.string "upstream_url"
    t.index ["path"], name: "index_projects_on_path", unique: true
  end

  create_table "scan_folders", force: :cascade do |t|
    t.datetime "created_at", null: false
    t.string "path", null: false
    t.datetime "updated_at", null: false
    t.index ["path"], name: "index_scan_folders_on_path", unique: true
  end
end
