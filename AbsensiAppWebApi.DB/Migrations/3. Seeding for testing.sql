INSERT INTO worker(id, fullname, "name", work_status) VALUES('aaadcf32-80a7-4199-890d-b9b5659f2f0d', 'Heriyanto', 'Yanto', false);

INSERT INTO blob(blob_id, path, file_name, created_by) VALUES('b5e1fc6b-ff05-4f38-a4e8-432d4895a16b', 'test path', 'QR Cikande', 'SYSTEM');

INSERT INTO project(project_id, project_name, blob_id, created_by) VALUES('142554ad-1df6-4902-a29a-3e948a706b6f', 'Cikande', 'b5e1fc6b-ff05-4f38-a4e8-432d4895a16b', 'SYSTEM');