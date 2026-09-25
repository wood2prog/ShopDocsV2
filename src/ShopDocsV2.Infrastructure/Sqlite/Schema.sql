CREATE TABLE IF NOT EXISTS jobs (
    id TEXT PRIMARY KEY, customer_name TEXT NOT NULL, customer_phone TEXT, customer_email TEXT,
    address TEXT, date_created TEXT NOT NULL, due_date TEXT, updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS rooms (
    id TEXT PRIMARY KEY, job_id TEXT NOT NULL REFERENCES jobs(id) ON DELETE CASCADE,
    name TEXT NOT NULL, sort_order INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS idx_rooms_job_id ON rooms(job_id);

-- scalar answers (EAV): one row per (room, question) that is actually answered.
-- Absent row == unanswered. A checkbox is only ever stored when checked (value_kind='bool', value_text='1');
-- unchecked/never-touched are both "absent" (no tri-state needed).
CREATE TABLE IF NOT EXISTS room_answers (
    room_id TEXT NOT NULL REFERENCES rooms(id) ON DELETE CASCADE,
    question_id TEXT NOT NULL, value_kind TEXT NOT NULL, value_text TEXT,
    PRIMARY KEY (room_id, question_id)
);

-- list-question rows (one row per item, e.g. one cabinet_finishes entry)
CREATE TABLE IF NOT EXISTS room_list_items (
    id TEXT PRIMARY KEY, room_id TEXT NOT NULL REFERENCES rooms(id) ON DELETE CASCADE,
    question_id TEXT NOT NULL, sort_order INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS idx_list_items_room_question ON room_list_items(room_id, question_id);

CREATE TABLE IF NOT EXISTS room_list_item_values (
    list_item_id TEXT NOT NULL REFERENCES room_list_items(id) ON DELETE CASCADE,
    field_id TEXT NOT NULL, value_kind TEXT NOT NULL, value_text TEXT,
    PRIMARY KEY (list_item_id, field_id)
);

CREATE TABLE IF NOT EXISTS catalog_materials        (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE, sort_order INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS catalog_finishes         (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE, hex_color TEXT, sort_order INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS catalog_countertop_colors(id INTEGER PRIMARY KEY AUTOINCREMENT, material_name TEXT NOT NULL, color_name TEXT NOT NULL, sort_order INTEGER NOT NULL DEFAULT 0, UNIQUE(material_name, color_name));
CREATE TABLE IF NOT EXISTS catalog_pulls            (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE, sort_order INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS catalog_hardware_colors  (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL UNIQUE, sort_order INTEGER NOT NULL DEFAULT 0);
-- Added in 1.0.40; not seeded. Name isn't unique since one accessory can come in several models.
CREATE TABLE IF NOT EXISTS catalog_accessories      (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, model_number TEXT, url TEXT, sort_order INTEGER NOT NULL DEFAULT 0);

-- Hinges and guides catalogs were removed in 1.0.29 (no question ever used them); clean them out of older databases.
DROP TABLE IF EXISTS catalog_hinges;
DROP TABLE IF EXISTS catalog_guides;

CREATE TABLE IF NOT EXISTS schema_version (version INTEGER NOT NULL);
