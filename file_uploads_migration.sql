-- Add file_uploads table

CREATE TABLE public.file_uploads (
    id            uuid                        NOT NULL,
    file_name     character varying(255),
    file_extension character varying(20),
    file_size     bigint,
    file_path     character varying(500),
    file_url      character varying(1000),
    mime_type     character varying(100),
    uploaded_by_user_id uuid,
    description   text,
    status        character varying(20)       NOT NULL,
    created_at    timestamp without time zone NOT NULL,
    created_by    character varying(255),
    updated_at    timestamp without time zone,
    updated_by    character varying(255),
    deleted_at    timestamp without time zone,
    deleted_by    character varying(255),
    CONSTRAINT file_uploads_pkey PRIMARY KEY (id)
);

-- Create index on uploaded_by_user_id for faster queries
CREATE INDEX idx_file_uploads_uploaded_by_user_id ON public.file_uploads(uploaded_by_user_id);

-- Create index on deleted_at for soft delete queries
CREATE INDEX idx_file_uploads_deleted_at ON public.file_uploads(deleted_at);
