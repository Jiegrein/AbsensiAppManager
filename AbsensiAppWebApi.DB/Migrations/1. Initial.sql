CREATE TABLE worker (
    id UUID,
    fullname TEXT,
    name TEXT,
    work_status boolean,
	created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
	updated_at TIMESTAMPTZ,

	CONSTRAINT pk_worker PRIMARY KEY (id)
);

CREATE TABLE worker_log (
    log_id TEXT,
    worker_id UUID,
    start_work TIMESTAMPTZ,
    start_break TIMESTAMPTZ,
    end_break TIMESTAMPTZ,
    end_work TIMESTAMPTZ,
    project_id UUID,
	created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
	created_by TEXT NOT NULL,
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	
	CONSTRAINT pk_worker_log PRIMARY KEY (log_id),
	CONSTRAINT fk_worker_log__worker FOREIGN KEY (worker_id) REFERENCES worker(id)
);

CREATE TABLE blob (
    blob_id UUID,
    path TEXT,
    file_name TEXT,
	created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
	created_by TEXT NOT NULL,
	
	CONSTRAINT pk_blob PRIMARY KEY (blob_id)
);

CREATE TABLE project (
    project_id UUID,
    project_name TEXT,
    blob_id UUID ,
	created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
	created_by TEXT NOT NULL,
	updated_at TIMESTAMPTZ,
	updated_by TEXT,
	
	CONSTRAINT pk_project PRIMARY KEY (project_id),
	CONSTRAINT fk_project__blob FOREIGN KEY (blob_id) REFERENCES blob(blob_id)
);

CREATE TABLE scan_enum (
    id INT,
	name TEXT,
	created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
	created_by TEXT NOT NULL
);