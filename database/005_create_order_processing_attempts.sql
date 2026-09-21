create table order_processing_attempts
(
	opa_id uuid primary key,
	opa_order_id bigint not null,
	opa_startedAt timestamp not null,
	opa_endedAt timestamp,
	opa_attempt_number integer not null,
	opa_success boolean not null,
	opa_error_message text,
	constraint fk_processing_attempts_order foreign key(opa_order_id) references orders (o_id),
	constraint uq_order_attempt unique (opa_order_id, opa_attempt_number)

);