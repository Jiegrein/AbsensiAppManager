﻿ALTER TABLE worker
ADD daily_pay DECIMAL DEFAULT 0 NOT NULL;

ALTER TABLE project
ADD hour_offset_gmt int DEFAULT 7 NOT NULL;