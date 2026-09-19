
create table order_items
(
	oi_id UUID primary key,
	oi_order_id BIGINT not null,
	oi_product_id UUID not null,
	oi_unit_value NUMERIC(18,2) not null,
	oi_quantity integer not null,
	oi_total_amount numeric(18,2) not null,
	constraint fk_order_items_order foreign key (oi_order_id) references orders (o_id),
	constraint fk_order_items_product foreign key (oi_product_id) references products (p_id),
	constraint ck_unit_value_positive check (oi_unit_value >= 0),
	constraint ck_quantity_positive check (oi_quantity >= 0),
	constraint ck_total_amount_positive check (oi_total_amount  >= 0)
	
);