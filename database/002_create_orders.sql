
create table orders 
(
	o_id BIGINT generated always as identity primary key,
	o_user_id VARCHAR (255) not null,
	o_createdAt timestamp not null default now(),
	o_updatedAt timestamp not null default now(),
	o_total_amount numeric(18,2) not null,
	o_status varchar(30) not null,
	constraint ck_orders_total_amount_positive check (o_total_amount >= 0)
		
);