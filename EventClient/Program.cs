using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventClient
{
    public enum ParticipantRole { Attendee, Speaker, VIP, Organizer }

    // Обновленные DTO
    public record CreateEventDto(string Title, DateTime Date, decimal BaseTicketPrice);
    public record RegisterParticipantDto(string EventName, string Name, string Email, ParticipantRole Role);
    public record IssueTicketDto(string EventName, string Email);

    // Классы для чтения ответов
    public class ErrorResponse { public string Error { get; set; } }
    public class EventResponse { public string Title { get; set; } public decimal BaseTicketPrice { get; set; } }

    class Program
    {
        private static readonly HttpClient client = new HttpClient 
        { BaseAddress = new Uri("http://localhost:5000") };

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Управление Мероприятиями ===");

            while (true)
            {
                Console.WriteLine("\nВыберите операцию:");
                Console.WriteLine("1. Создать мероприятие");
                Console.WriteLine("2. Поиск мероприятий"); 
                Console.WriteLine("3. Зарегистрировать участника");
                Console.WriteLine("4. Выпустить билет");
                Console.WriteLine("0. Выход");
                Console.Write("Ваш выбор: ");

                var choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1": await CreateEventAsync(); break;
                        case "2": await SearchEventsAsync(); break;
                        case "3": await RegisterParticipantAsync(); break;
                        case "4": await IssueTicketAsync(); break;
                        case "0": return;
                        default: Console.WriteLine(" ОШИБКА: Неизвестная команда."); break;
                    }
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine("\n[СЕТЕВАЯ ОШИБКА]: Сервер недоступен. Убедитесь, что сервер запущен.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[ОШИБКА]: {ex.Message}");
                }
            }
        }

        private static async Task CreateEventAsync()
        {
            Console.Write("Введите уникальное название мероприятия: ");
            string title = Console.ReadLine();

            Console.Write("Введите базовую стоимость билета: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal price))
            {
                Console.WriteLine("Ошибка: Некорректный формат цены.");
                return;
            }

            var dto = new CreateEventDto(title, DateTime.Now.AddDays(30), price);
            var response = await client.PostAsJsonAsync("/event", dto);
            await ProcessResponse(response, "Мероприятие создано!");
        }

        private static async Task SearchEventsAsync()
        {
            Console.Write("Введите название для поиска (или нажмите Enter, чтобы показать все): ");
            string query = Console.ReadLine();

            var response = await client.GetAsync($"/event/search?name={Uri.EscapeDataString(query)}");

            if (response.IsSuccessStatusCode)
            {
                var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
                Console.WriteLine("\n--- Найденные мероприятия ---");
                if (events == null || events.Count == 0)
                {
                    Console.WriteLine("Ничего не найдено.");
                }
                else
                {
                    foreach (var ev in events)
                        Console.WriteLine($"Название: {ev.Title} | Цена: {ev.BaseTicketPrice} руб.");
                }
                Console.WriteLine("-----------------------------");
            }
            else await ProcessResponse(response, "");
        }

        private static async Task RegisterParticipantAsync()
        {
            Console.Write("Введите НАЗВАНИЕ мероприятия: ");
            string eventName = Console.ReadLine();

            Console.Write("Введите ваше Имя: ");
            string name = Console.ReadLine();

            Console.Write("Введите ваш Email: ");
            string email = Console.ReadLine();

            Console.WriteLine("Выберите роль (0 - Посетитель, 1 - Спикер, 2 - VIP, 3 - Организатор): ");
            if (!Enum.TryParse(Console.ReadLine(), out ParticipantRole role))
                role = ParticipantRole.Attendee;

            var dto = new RegisterParticipantDto(eventName, name, email, role);
            var response = await client.PostAsJsonAsync("/participant/register", dto);
            await ProcessResponse(response, "Вы успешно зарегистрированы!");
        }

        private static async Task IssueTicketAsync()
        {
            Console.Write("Введите НАЗВАНИЕ мероприятия: ");
            string eventName = Console.ReadLine();

            Console.Write("Введите ваш Email (указанный при регистрации): ");
            string email = Console.ReadLine();

            var dto = new IssueTicketDto(eventName, email);
            var response = await client.PostAsJsonAsync("/ticket/issue", dto);
            await ProcessResponse(response, "Билет успешно выпущен!");
        }

        private static async Task ProcessResponse(HttpResponseMessage response, string successMessage)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"\n[УСПЕХ]: {successMessage}");
                Console.WriteLine($"Детали: {content}");
            }
            else
            {
                try
                {
                    var errorObj = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    string errorMsg = errorObj?.Error ?? content;

                    switch ((int)response.StatusCode)
                    {
                        case 400: Console.WriteLine($"\n[ОШИБКА ДАННЫХ 400]: {errorMsg}"); break;
                        case 404: Console.WriteLine($"\n[НЕ НАЙДЕНО 404]: {errorMsg}"); break;
                        case 409: Console.WriteLine($"\n[КОНФЛИКТ 409]: {errorMsg}"); break;
                        default: Console.WriteLine($"\n[ОШИБКА СЕРВЕРА {(int)response.StatusCode}]: {errorMsg}"); break;
                    }
                }
                catch { Console.WriteLine($"\n[ОШИБКА]: {content}"); }
            }
        }
    }
}