-- RiskLedger platform persistence foundation.
-- Apply after ledger.sql. This schema uses synthetic identifiers and data only.
-- Tables are deliberately append/audit oriented for decisions and provider outcomes.

CREATE TABLE IF NOT EXISTS organizers (
    id uuid PRIMARY KEY,
    external_key varchar(120) NOT NULL UNIQUE,
    display_name varchar(200) NOT NULL,
    status varchar(24) NOT NULL CHECK (status IN ('PendingVerification','Active','Restricted','Suspended','Closed')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    row_version bigint NOT NULL DEFAULT 1 CHECK (row_version > 0)
);

CREATE TABLE IF NOT EXISTS verification_cases (
    id uuid PRIMARY KEY,
    organizer_id uuid NOT NULL REFERENCES organizers(id) ON DELETE RESTRICT,
    status varchar(24) NOT NULL CHECK (status IN ('Pending','InReview','Verified','Rejected','Expired')),
    evidence_reference varchar(240),
    reason_code varchar(80),
    reviewed_by varchar(120),
    reviewed_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK ((status <> 'Verified' AND status <> 'Rejected') OR reviewed_at IS NOT NULL)
);
CREATE INDEX IF NOT EXISTS ix_verification_cases_organizer_created ON verification_cases(organizer_id, created_at DESC);

CREATE TABLE IF NOT EXISTS trips (
    id uuid PRIMARY KEY,
    organizer_id uuid NOT NULL REFERENCES organizers(id) ON DELETE RESTRICT,
    external_key varchar(120) NOT NULL,
    name varchar(240) NOT NULL,
    departure_at timestamptz NOT NULL,
    return_at timestamptz,
    currency char(3) NOT NULL,
    status varchar(24) NOT NULL CHECK (status IN ('Draft','Open','Closed','Departed','Completed','Cancelled')),
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (organizer_id, external_key),
    CHECK (return_at IS NULL OR return_at >= departure_at)
);
CREATE INDEX IF NOT EXISTS ix_trips_organizer_departure ON trips(organizer_id, departure_at);

CREATE TABLE IF NOT EXISTS bookings (
    id uuid PRIMARY KEY,
    trip_id uuid NOT NULL REFERENCES trips(id) ON DELETE RESTRICT,
    external_key varchar(120) NOT NULL UNIQUE,
    status varchar(24) NOT NULL CHECK (status IN ('Pending','Confirmed','Cancelled','Refunded','Completed')),
    amount numeric(20,3) NOT NULL CHECK (amount >= 0),
    currency char(3) NOT NULL,
    booked_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL DEFAULT now(),
    row_version bigint NOT NULL DEFAULT 1 CHECK (row_version > 0)
);
CREATE INDEX IF NOT EXISTS ix_bookings_trip_status ON bookings(trip_id, status);

CREATE TABLE IF NOT EXISTS supplier_obligations (
    id uuid PRIMARY KEY,
    organizer_id uuid NOT NULL REFERENCES organizers(id) ON DELETE RESTRICT,
    trip_id uuid REFERENCES trips(id) ON DELETE RESTRICT,
    booking_id uuid REFERENCES bookings(id) ON DELETE RESTRICT,
    external_key varchar(120) NOT NULL UNIQUE,
    supplier_reference varchar(160) NOT NULL,
    status varchar(24) NOT NULL CHECK (status IN ('Expected','Committed','PartiallySettled','Settled','Cancelled','Disputed')),
    amount numeric(20,3) NOT NULL CHECK (amount >= 0),
    settled_amount numeric(20,3) NOT NULL DEFAULT 0 CHECK (settled_amount >= 0),
    currency char(3) NOT NULL,
    due_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    row_version bigint NOT NULL DEFAULT 1 CHECK (row_version > 0),
    CHECK (settled_amount <= amount)
);
CREATE INDEX IF NOT EXISTS ix_supplier_obligations_organizer_status_due ON supplier_obligations(organizer_id, status, due_at);

CREATE TABLE IF NOT EXISTS disputes (
    id uuid PRIMARY KEY,
    organizer_id uuid NOT NULL REFERENCES organizers(id) ON DELETE RESTRICT,
    booking_id uuid REFERENCES bookings(id) ON DELETE RESTRICT,
    external_key varchar(160) NOT NULL UNIQUE,
    severity varchar(16) NOT NULL CHECK (severity IN ('Low','Medium','High','Critical')),
    status varchar(24) NOT NULL CHECK (status IN ('Open','UnderReview','Won','Lost','Accepted','Closed')),
    amount numeric(20,3) NOT NULL CHECK (amount >= 0),
    currency char(3) NOT NULL,
    reason_code varchar(80) NOT NULL,
    opened_at timestamptz NOT NULL,
    resolved_at timestamptz,
    CHECK (resolved_at IS NULL OR resolved_at >= opened_at)
);
CREATE INDEX IF NOT EXISTS ix_disputes_organizer_status ON disputes(organizer_id, status);

CREATE TABLE IF NOT EXISTS exposure_snapshots (
    id uuid PRIMARY KEY,
    organizer_id uuid NOT NULL REFERENCES organizers(id) ON DELETE RESTRICT,
    calculation_version varchar(80) NOT NULL,
    currency char(3) NOT NULL,
    gross_obligations numeric(20,3) NOT NULL CHECK (gross_obligations >= 0),
    eligible_funds numeric(20,3) NOT NULL CHECK (eligible_funds >= 0),
    uncovered_exposure numeric(20,3) NOT NULL CHECK (uncovered_exposure >= 0),
    input_snapshot jsonb NOT NULL,
    calculated_at timestamptz NOT NULL,
    CHECK (uncovered_exposure = GREATEST(0, gross_obligations - eligible_funds))
);
CREATE INDEX IF NOT EXISTS ix_exposure_snapshots_organizer_calculated ON exposure_snapshots(organizer_id, calculated_at DESC);

CREATE TABLE IF NOT EXISTS risk_policy_versions (
    id uuid PRIMARY KEY,
    policy_key varchar(100) NOT NULL,
    version integer NOT NULL CHECK (version > 0),
    configuration jsonb NOT NULL,
    published_by varchar(120) NOT NULL,
    published_at timestamptz NOT NULL,
    is_active boolean NOT NULL DEFAULT false,
    UNIQUE (policy_key, version)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_risk_policy_one_active_version ON risk_policy_versions(policy_key) WHERE is_active;

CREATE TABLE IF NOT EXISTS risk_assessments (
    id uuid PRIMARY KEY,
    organizer_id uuid NOT NULL REFERENCES organizers(id) ON DELETE RESTRICT,
    policy_version_id uuid NOT NULL REFERENCES risk_policy_versions(id) ON DELETE RESTRICT,
    exposure_snapshot_id uuid REFERENCES exposure_snapshots(id) ON DELETE RESTRICT,
    outcome varchar(16) NOT NULL CHECK (outcome IN ('Approve','Review','Restrict')),
    reason_codes jsonb NOT NULL,
    input_snapshot jsonb NOT NULL,
    assessed_at timestamptz NOT NULL,
    created_by varchar(120) NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_risk_assessments_organizer_assessed ON risk_assessments(organizer_id, assessed_at DESC);

CREATE TABLE IF NOT EXISTS reserve_allocations (
    id uuid PRIMARY KEY,
    organizer_id uuid NOT NULL REFERENCES organizers(id) ON DELETE RESTRICT,
    source_reference varchar(160) NOT NULL,
    purpose varchar(80) NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('Held','PartiallyReleased','Released','Cancelled')),
    original_amount numeric(20,3) NOT NULL CHECK (original_amount >= 0),
    released_amount numeric(20,3) NOT NULL DEFAULT 0 CHECK (released_amount >= 0),
    currency char(3) NOT NULL,
    reason_code varchar(80) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NOT NULL,
    row_version bigint NOT NULL DEFAULT 1 CHECK (row_version > 0),
    CHECK (released_amount <= original_amount)
);
CREATE INDEX IF NOT EXISTS ix_reserve_allocations_organizer_status ON reserve_allocations(organizer_id, status);

CREATE TABLE IF NOT EXISTS payout_requests (
    id uuid PRIMARY KEY,
    organizer_id uuid NOT NULL REFERENCES organizers(id) ON DELETE RESTRICT,
    idempotency_key varchar(200) NOT NULL UNIQUE,
    request_hash char(64) NOT NULL,
    amount numeric(20,3) NOT NULL CHECK (amount > 0),
    currency char(3) NOT NULL,
    status varchar(24) NOT NULL CHECK (status IN ('Requested','Reserved','Submitted','Succeeded','Declined','Unknown','Cancelled','Reconciled')),
    provider_reference varchar(200),
    decision_snapshot jsonb NOT NULL,
    failure_code varchar(80),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    row_version bigint NOT NULL DEFAULT 1 CHECK (row_version > 0)
);
CREATE INDEX IF NOT EXISTS ix_payout_requests_organizer_created ON payout_requests(organizer_id, created_at DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_payout_provider_reference ON payout_requests(provider_reference) WHERE provider_reference IS NOT NULL;

CREATE TABLE IF NOT EXISTS audit_records (
    id uuid PRIMARY KEY,
    actor_id varchar(160) NOT NULL,
    action varchar(120) NOT NULL,
    target_type varchar(120) NOT NULL,
    target_id varchar(200) NOT NULL,
    reason_code varchar(100),
    correlation_id varchar(160) NOT NULL,
    before_reference varchar(240),
    after_reference varchar(240),
    occurred_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_audit_target_occurred ON audit_records(target_type, target_id, occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_correlation ON audit_records(correlation_id);

CREATE TABLE IF NOT EXISTS outbox_messages (
    id uuid PRIMARY KEY,
    message_type varchar(200) NOT NULL,
    schema_version integer NOT NULL CHECK (schema_version > 0),
    aggregate_type varchar(120) NOT NULL,
    aggregate_id varchar(200) NOT NULL,
    correlation_id varchar(160) NOT NULL,
    payload jsonb NOT NULL,
    occurred_at timestamptz NOT NULL DEFAULT now(),
    available_at timestamptz NOT NULL DEFAULT now(),
    attempts integer NOT NULL DEFAULT 0 CHECK (attempts >= 0),
    processed_at timestamptz,
    last_error_code varchar(100)
);
CREATE INDEX IF NOT EXISTS ix_outbox_pending ON outbox_messages(available_at, occurred_at) WHERE processed_at IS NULL;

CREATE TABLE IF NOT EXISTS inbox_messages (
    consumer_name varchar(160) NOT NULL,
    message_id uuid NOT NULL,
    received_at timestamptz NOT NULL DEFAULT now(),
    processed_at timestamptz,
    outcome varchar(24) NOT NULL CHECK (outcome IN ('Received','Processed','Failed','DeadLettered')),
    PRIMARY KEY (consumer_name, message_id)
);

CREATE TABLE IF NOT EXISTS reconciliation_runs (
    id uuid PRIMARY KEY,
    provider_name varchar(120) NOT NULL,
    statement_reference varchar(200) NOT NULL,
    status varchar(20) NOT NULL CHECK (status IN ('Running','Completed','Failed','HasDiscrepancies')),
    started_at timestamptz NOT NULL,
    completed_at timestamptz,
    imported_count integer NOT NULL DEFAULT 0 CHECK (imported_count >= 0),
    matched_count integer NOT NULL DEFAULT 0 CHECK (matched_count >= 0),
    discrepancy_count integer NOT NULL DEFAULT 0 CHECK (discrepancy_count >= 0),
    UNIQUE (provider_name, statement_reference)
);

CREATE TABLE IF NOT EXISTS reconciliation_findings (
    id uuid PRIMARY KEY,
    run_id uuid NOT NULL REFERENCES reconciliation_runs(id) ON DELETE RESTRICT,
    external_reference varchar(200) NOT NULL,
    status varchar(40) NOT NULL CHECK (status IN ('Matched','MissingInternally','MissingExternally','AmountMismatch','CurrencyMismatch','DuplicateExternalReference')),
    internal_reference varchar(200),
    expected_amount numeric(20,3),
    observed_amount numeric(20,3),
    expected_currency char(3),
    observed_currency char(3),
    resolution_status varchar(20) NOT NULL DEFAULT 'Open' CHECK (resolution_status IN ('Open','Investigating','Resolved','AcceptedRisk')),
    resolution_reason varchar(500),
    resolved_by varchar(120),
    resolved_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_reconciliation_findings_run_status ON reconciliation_findings(run_id, resolution_status, status);
