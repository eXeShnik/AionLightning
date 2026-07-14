-- Java `town`(id, level, points, race, level_up_date) — per-district (Sanctum/Pandaemonium) activity
-- level, imported once from houses.xml addresses on first boot (see Services/TownService.cs) and then
-- persisted here for every subsequent level-up.
CREATE TABLE IF NOT EXISTS towns (
    id INT NOT NULL,
    level INT NOT NULL DEFAULT 1,
    points INT NOT NULL DEFAULT 0,
    race ENUM('ELYOS','ASMODIANS') NOT NULL,
    level_up_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id)
);
