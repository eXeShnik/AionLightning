-- Java `base`(mapid, id, race, last_time) — renamed/normalized to this project's snake_case table
-- naming convention (see houses/siege_locations). Race enum values match Model.Siege.SiegeRace exactly
-- (this port persists SiegeRace rather than Java's own model.Race, whose values never actually matched
-- this same enum literal set — see Dao/BaseDaoImpl.cs's doc comment).
CREATE TABLE IF NOT EXISTS bases (
    id INT NOT NULL,
    map_id INT NOT NULL,
    race ENUM('ELYOS','ASMODIANS','BALAUR') NOT NULL DEFAULT 'BALAUR',
    last_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id)
);
