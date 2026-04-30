CREATE TABLE IF NOT EXISTS mail (
    id               INT           NOT NULL AUTO_INCREMENT,
    sender_id        INT           NOT NULL,
    sender_name      VARCHAR(50)   NOT NULL,
    receiver_id      INT           NOT NULL,
    receiver_name    VARCHAR(50)   NOT NULL,
    title            VARCHAR(100)  NOT NULL DEFAULT '',
    message          TEXT          NOT NULL,
    attached_item_id INT           NOT NULL DEFAULT 0,
    attached_count   BIGINT        NOT NULL DEFAULT 0,
    attached_kinah   BIGINT        NOT NULL DEFAULT 0,
    letter_type      TINYINT       NOT NULL DEFAULT 0,
    send_date        DATETIME      NOT NULL,
    is_read          TINYINT(1)    NOT NULL DEFAULT 0,
    attachment_taken TINYINT(1)    NOT NULL DEFAULT 0,
    recipient_deleted TINYINT(1)   NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    INDEX idx_mail_receiver (receiver_id, recipient_deleted)
);
