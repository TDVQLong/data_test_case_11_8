using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Events;

namespace Marketing.API.IntegrationEvents.Events
{
    public class OrderStartedIntegrationEvent : IntegrationEvent
    {
        public string UserId { get; set; }
    }
}
