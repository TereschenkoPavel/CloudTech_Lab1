using eventManager;
using eventManager.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
namespace EventApi.Tests
{
    public class EventServiceTests
    {
        private EventService CreateService()
        {
            var events = new InMemoryRepository<Event>();
            var participants = new InMemoryRepository<Participant>();
            var registrations = new InMemoryRepository<Registration>();
            var tickets = new InMemoryRepository<Ticket>();

            return new EventService(events, participants, registrations, tickets);
        }

        [Fact]
        public void CreateEvent_ValidData_ReturnsEvent()
        {
            var service = CreateService();
            var dto = new CreateEventDto("Конференция", DateTime.Now, 1000m);
            var result = service.CreateEvent(dto);

            Assert.NotNull(result);
            Assert.Equal("Конференция", result.Title);
        }

        [Fact]
        public void CreateEvent_EmptyTitle_ThrowsArgumentException()
        {
            var service = CreateService();
            var dto = new CreateEventDto("", DateTime.Now, 1000m);

            // ЯВНОЕ ПРИВЕДЕНИЕ К ACTION ИСПРАВЛЯЕТ ОШИБКУ XUNIT
            Action act = () => service.CreateEvent(dto);

            var ex = Assert.Throws<ArgumentException>(act);
            Assert.Equal("Название не может быть пустым.", ex.Message);
        }

        [Fact]
        public void CreateEvent_DuplicateName_ThrowsInvalidOperationException()
        {
            var service = CreateService();
            service.CreateEvent(new CreateEventDto("IT Форум", DateTime.Now, 1000m));

            var dto = new CreateEventDto("it форум", DateTime.Now, 1000m);

            Action act = () => service.CreateEvent(dto);

            var ex = Assert.Throws<InvalidOperationException>(act);
            Assert.Contains("уже существует", ex.Message);
        }

        [Fact]
        public void RegisterParticipant_InvalidEmail_ThrowsArgumentException()
        {
            var service = CreateService();
            var dto = new RegisterParticipantDto("Форум", "Иван", "bad-email", ParticipantRole.Attendee);

            Action act = () => service.RegisterParticipant(dto);

            var ex = Assert.Throws<ArgumentException>(act);
            Assert.Equal("Некорректный формат Email.", ex.Message);
        }

        [Fact]
        public void RegisterParticipant_EventNotFound_ThrowsKeyNotFoundException()
        {
            var service = CreateService();
            var dto = new RegisterParticipantDto("Секретный концерт", "Иван", "ivan@mail.ru", ParticipantRole.Attendee);

            Action act = () => service.RegisterParticipant(dto);

            var ex = Assert.Throws<KeyNotFoundException>(act);
            Assert.Contains("не найдено", ex.Message);
        }

        [Fact]
        public void IssueTicket_ForSpeaker_PriceIsZero()
        {
            var service = CreateService();
            service.CreateEvent(new CreateEventDto("Test Event", DateTime.Now, 1000m));
            service.RegisterParticipant(new RegisterParticipantDto("Test Event", "Спикер", "sp@mail.ru", ParticipantRole.Speaker));

            var ticket = service.IssueTicket(new IssueTicketDto("Test Event", "sp@mail.ru"));

            Assert.Equal(0m, ticket.FinalPrice);
        }

        [Fact]
        public void SearchEvents_FindsCorrectEvents()
        {
            var service = CreateService();
            service.CreateEvent(new CreateEventDto("C# Conference", DateTime.Now, 1000m));
            service.CreateEvent(new CreateEventDto("Java Meetup", DateTime.Now, 500m));

            var results = service.SearchEvents("c#").ToList();

            Assert.Single(results);
            Assert.Equal("C# Conference", results[0].Title);
        }
    }
}