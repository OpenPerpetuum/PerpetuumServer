using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.EventServices;
using System;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class EPBonusEvent : IRequestHandler
    {
        private readonly EPBonusEventService _eventService;
        private TimeSpan MAX_DURATION = TimeSpan.FromDays(14);
        private const int MIN_BONUS = 0;
        private const int MAX_BONUS = 25;

        public EPBonusEvent(EPBonusEventService eventService)
        {
            _eventService = eventService;
        }

        public void HandleRequest(IRequest request)
        {
            using (System.Transactions.TransactionScope scope = Db.CreateTransaction())
            {
                int existingBonus = _eventService.GetBonus();
                int bonusAmount = request.Data.GetOrDefault<int>(k.bonus);
                int resultingBonus = existingBonus + bonusAmount;

                int timeRemained = _eventService.GetTimeRemained().Hours;
                int durationHours = request.Data.GetOrDefault<int>(k.duration);
                int resultingTime = timeRemained + durationHours;

                bool checkArgs = resultingBonus >= MIN_BONUS && resultingBonus <= MAX_BONUS;
                checkArgs = checkArgs && resultingTime <= MAX_DURATION.TotalHours;
                checkArgs.ThrowIfFalse(ErrorCodes.InputTooHigh);

                _eventService.SetEvent(resultingBonus, TimeSpan.FromHours(resultingTime));

                scope.Complete();
            }
        }
    }
}