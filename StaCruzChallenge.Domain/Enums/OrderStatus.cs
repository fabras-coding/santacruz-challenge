namespace StaCruzChallenge.Domain.Enums
{
    public enum OrderStatus
    {
        Pending = 1, //when the order is created but not yet processed
        InProgress = 2, //when the order is being queued
        Enqueued = 3, //when the order has been added to the processing queue
        Failed = 4, //when the order processing has failed
        Processed = 5 //when the order has been successfully completed (update outbox and order status)

    }
}