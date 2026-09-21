
create table outbox_orders
(
	obo_id uuid primary key,
	obo_event_type varchar (50) not null,
	obo_payload jsonb not null,
	obo_status varchar(30) not null,
	obo_createdAt timestamp not null default now(),
	obo_processedAt timestamp 
	
);