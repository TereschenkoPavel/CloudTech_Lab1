using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using eventManager;
using eventManager.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(typeof(IRepository<>), typeof(InMemoryRepository<>));
builder.Services.AddSingleton<IEventService, EventService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    try { await next.Invoke(); }
    catch (KeyNotFoundException ex) { context.Response.StatusCode = 404; await context.Response.WriteAsJsonAsync(new { Error = ex.Message }); }
    catch (ArgumentException ex) { context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { Error = ex.Message }); }
    catch (InvalidOperationException ex) { context.Response.StatusCode = 409; await context.Response.WriteAsJsonAsync(new { Error = ex.Message }); }
    catch (Exception ex) { context.Response.StatusCode = 500; await context.Response.WriteAsJsonAsync(new { Error = "¬нутренн€€ ошибка.", Details = ex.Message }); }
});

app.MapGet("/event/search", (string? name, IEventService service) => Results.Ok(service.SearchEvents(name ?? "")));
app.MapPost("/event", (CreateEventDto req, IEventService service) => Results.Ok(service.CreateEvent(req)));
app.MapPost("/participant/register", (RegisterParticipantDto req, IEventService service) => Results.Ok(service.RegisterParticipant(req)));
app.MapPost("/ticket/issue", (IssueTicketDto req, IEventService service) => Results.Ok(service.IssueTicket(req)));
app.MapDelete("/participant/cancel/{id:guid}", (Guid id, IEventService service) =>
{
    service.CancelRegistration(id);
    return Results.Ok(new { Message = "–егистраци€ отменена." });
});

app.Run();
