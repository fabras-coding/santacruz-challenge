
create table products 
(
	p_id UUID primary key,
	p_name VARCHAR(100) not null,
	p_description VARCHAR(200),
	p_price NUMERIC(18,2) not null,
	created_at timestamp not null default now(),
	constraint ck_products_value_positive check (p_price >= 0)
	
);