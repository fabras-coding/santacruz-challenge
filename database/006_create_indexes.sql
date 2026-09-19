
create index idx_orders_user_id on orders(o_user_id);
create index idx_orders_status on orders(o_status);
create index idx_order_items_order_id on order_items(oi_order_id);
create index idx_order_items_product_id on order_items(oi_product_id);
create index idx_outbox_orders_status_created_at on outbox_orders(obo_status, obo_createdAt);
create index idx_processing_attempts_order_id on order_processing_attempts(opa_order_id);
