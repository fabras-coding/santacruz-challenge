using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using StaCruzChallenge.Application.Interfaces.ExternalServices;

namespace StaCruzChallenge.Infrastructure.ExternalServices
{
    public class FakeExternalOrderCaller : IExternalOrderCaller
    {
        private readonly IConfiguration _configuration;

        public FakeExternalOrderCaller(IConfiguration configuration) => _configuration = configuration;

        public async Task<bool> ProcessOrderAsync(long orderId, CancellationToken cancellationToken)
        {
            var duration = _configuration.GetValue<int>("ProcessingAttempt:FakeCallDurationInMilliseconds");
            var willSucceed = _configuration.GetValue<bool>("ProcessingAttempt:FakeCallsWillSucceed");

            await Task.Delay(duration, cancellationToken);
            return willSucceed;
        }
    }
}