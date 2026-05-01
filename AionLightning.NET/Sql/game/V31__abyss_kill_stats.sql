ALTER TABLE players
    ADD COLUMN abyss_all_kill    INT     NOT NULL DEFAULT 0,
    ADD COLUMN abyss_max_rank    INT     NOT NULL DEFAULT 1,
    ADD COLUMN abyss_daily_kill  INT     NOT NULL DEFAULT 0,
    ADD COLUMN abyss_daily_ap    BIGINT  NOT NULL DEFAULT 0,
    ADD COLUMN abyss_weekly_kill INT     NOT NULL DEFAULT 0,
    ADD COLUMN abyss_weekly_ap   BIGINT  NOT NULL DEFAULT 0,
    ADD COLUMN abyss_last_kill   INT     NOT NULL DEFAULT 0,
    ADD COLUMN abyss_last_ap     BIGINT  NOT NULL DEFAULT 0;
