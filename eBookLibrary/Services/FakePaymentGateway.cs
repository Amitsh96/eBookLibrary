using System;

namespace eBookLibrary.Services
{
    public static class FakePaymentGateway
    {
        public static (bool IsSuccess, string TransactionId) ProcessPayment(string cardNumber, string expiry, string cvv, decimal amount)
        {
            // Simulate a successful payment
            return (true, Guid.NewGuid().ToString());
        }
    }
}
