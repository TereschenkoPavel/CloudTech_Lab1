using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using eventManager.Repository;

namespace eventManager
{
    public interface IEntity { Guid Id { get; set; } }
    public enum ParticipantRole { Attendee, Speaker, VIP, Organizer }

    public class Event : IEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal BaseTicketPrice { get; set; }
    }

    public class Participant : IEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class Registration : IEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EventId { get; set; }
        public Guid ParticipantId { get; set; }
        public ParticipantRole Role { get; set; }
    }

    public class Ticket : IEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RegistrationId { get; set; }
        public decimal FinalPrice { get; set; }
    }

    public record CreateEventDto(string Title, DateTime Date, decimal BaseTicketPrice);
    public record RegisterParticipantDto(string EventName, string Name, string Email, ParticipantRole Role);
    public record IssueTicketDto(string EventName, string Email);

    

    public interface IEventService
    {
        IEnumerable<Event> SearchEvents(string query);
        Event CreateEvent(CreateEventDto dto);
        Registration RegisterParticipant(RegisterParticipantDto dto);
        Ticket IssueTicket(IssueTicketDto dto);
        void CancelRegistration(Guid registrationId);
    }

    public class EventService : IEventService
    {
        private readonly IRepository<Event> _events;
        private readonly IRepository<Participant> _participants;
        private readonly IRepository<Registration> _registrations;
        private readonly IRepository<Ticket> _tickets;

        public EventService(
            IRepository<Event> events, IRepository<Participant> participants,
            IRepository<Registration> registrations, IRepository<Ticket> tickets)
        {
            _events = events; _participants = participants;
            _registrations = registrations; _tickets = tickets;
        }

        public IEnumerable<Event> SearchEvents(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return _events.GetAll();
            return _events.GetAll().Where(e => e.Title.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        public Event CreateEvent(CreateEventDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title)) throw new ArgumentException("Название не может быть пустым.");
            if (_events.GetAll().Any(e => e.Title.Equals(dto.Title, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Мероприятие '{dto.Title}' уже существует.");

            var ev = new Event { Title = dto.Title, Date = dto.Date, BaseTicketPrice = dto.BaseTicketPrice };
            _events.Add(ev);
            return ev;
        }

        public Registration RegisterParticipant(RegisterParticipantDto dto)
        {
            if (!Regex.IsMatch(dto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Некорректный формат Email.");

            var ev = _events.GetAll().FirstOrDefault(e => e.Title.Equals(dto.EventName, StringComparison.OrdinalIgnoreCase))
                     ?? throw new KeyNotFoundException($"Мероприятие '{dto.EventName}' не найдено.");

            var participant = _participants.GetAll().FirstOrDefault(p => p.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase));
            if (participant == null)
            {
                participant = new Participant { Name = dto.Name, Email = dto.Email };
                _participants.Add(participant);
            }

            if (_registrations.GetAll().Any(r => r.EventId == ev.Id && r.ParticipantId == participant.Id))
                throw new InvalidOperationException("Пользователь уже зарегистрирован.");

            var reg = new Registration { EventId = ev.Id, ParticipantId = participant.Id, Role = dto.Role };
            _registrations.Add(reg);
            return reg;
        }

        public Ticket IssueTicket(IssueTicketDto dto)
        {
            var ev = _events.GetAll().FirstOrDefault(e => e.Title.Equals(dto.EventName, StringComparison.OrdinalIgnoreCase))
                     ?? throw new KeyNotFoundException($"Мероприятие '{dto.EventName}' не найдено.");

            var participant = _participants.GetAll().FirstOrDefault(p => p.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase))
                              ?? throw new KeyNotFoundException($"Участник с Email '{dto.Email}' не найден.");

            var reg = _registrations.GetAll().FirstOrDefault(r => r.EventId == ev.Id && r.ParticipantId == participant.Id)
                      ?? throw new KeyNotFoundException("Регистрация не найдена.");

            if (_tickets.GetAll().Any(t => t.RegistrationId == reg.Id))
                throw new InvalidOperationException("Билет уже выпущен.");

            decimal price = reg.Role switch
            {
                ParticipantRole.Speaker or ParticipantRole.Organizer => 0,
                ParticipantRole.VIP => ev.BaseTicketPrice * 2,
                _ => ev.BaseTicketPrice
            };

            var ticket = new Ticket { RegistrationId = reg.Id, FinalPrice = price };
            _tickets.Add(ticket);
            return ticket;
        }

        public void CancelRegistration(Guid registrationId)
        {
            var reg = _registrations.GetById(registrationId) ?? throw new KeyNotFoundException("Регистрация не найдена.");

            var ticket = _tickets.GetAll().FirstOrDefault(t => t.RegistrationId == registrationId);
            if (ticket != null)
            {
                _tickets.Delete(ticket.Id);
            }
            _registrations.Delete(registrationId);
        }
    }
}
