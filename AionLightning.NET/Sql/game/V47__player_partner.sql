-- Java Player.partnerId — the spouse's player id (0 == unmarried). See Services/WeddingService.cs.
ALTER TABLE players ADD COLUMN partner_id INT NOT NULL DEFAULT 0;
