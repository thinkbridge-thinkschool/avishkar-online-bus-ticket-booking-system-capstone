using System.Security.Claims;
using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Feedback;
using BusBooking.Application.Feedback.Commands.CreateFeedback;
using BusBooking.Application.Feedback.Commands.DeleteFeedback;
using BusBooking.Application.Feedback.Commands.UpdateFeedback;
using BusBooking.Application.Feedback.Queries.GetFeedbackBySchedule;
using BusBooking.Application.Feedback.Queries.GetFeedbackByUser;
using BusBooking.Application.Feedback.Queries.GetFeedbackStatistics;
using BusBooking.Domain.Feedback.Enums;

namespace BusBooking.Api.Feedback;

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/feedback")
            .WithTags("Feedback")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapPost("/", CreateFeedback);
        group.MapPut("/{feedbackId:guid}", UpdateFeedback);
        group.MapDelete("/{feedbackId:guid}", DeleteFeedback);
        group.MapGet("/user/{userId:guid}", GetFeedbackByUser);
        group.MapGet("/schedule/{scheduleId:guid}", GetFeedbackBySchedule);
        group.MapGet("/schedule/{scheduleId:guid}/stats", GetFeedbackStatistics);
    }

    private static async Task<IResult> CreateFeedback(     // Submits passenger feedback and rating for a completed booking.
        CreateFeedbackBody body, HttpContext httpContext,
        IFeedbackRepository feedbackRepo, IBookingRepository bookingRepo, CancellationToken ct)
    {
        if (!GetAppUserId(httpContext, out var userId)) return Results.Unauthorized();

        var command = new CreateFeedbackCommand(
            userId, body.BookingId, body.ScheduleId, body.Rating,
            body.Comment ?? string.Empty, body.Category ?? FeedbackCategory.General);

        var handler = new CreateFeedbackHandler(feedbackRepo, bookingRepo);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/feedback/{id}", new { feedbackId = id });
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> UpdateFeedback(     // Updates the rating, comment, or category of the authenticated user's feedback entry.
        Guid feedbackId, UpdateFeedbackRequest body, HttpContext httpContext,
        IFeedbackRepository feedbackRepo, CancellationToken ct)
    {
        if (!GetAppUserId(httpContext, out var userId)) return Results.Unauthorized();

        var command = new UpdateFeedbackCommand(feedbackId, userId, body.Rating, body.Comment, body.Category);
        var handler = new UpdateFeedbackHandler(feedbackRepo);
        try
        {
            await handler.HandleAsync(command, ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> DeleteFeedback(     // Deletes the authenticated user's feedback entry.
        Guid feedbackId, HttpContext httpContext, IFeedbackRepository feedbackRepo, CancellationToken ct)
    {
        if (!GetAppUserId(httpContext, out var userId)) return Results.Unauthorized();

        var handler = new DeleteFeedbackHandler(feedbackRepo);
        try
        {
            await handler.HandleAsync(new DeleteFeedbackCommand(feedbackId, userId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> GetFeedbackByUser(     // Returns all feedback submitted by the specified user.
        Guid userId, IFeedbackRepository feedbackRepo, CancellationToken ct)
    {
        var handler = new GetFeedbackByUserHandler(feedbackRepo);
        var entries = await handler.HandleAsync(new GetFeedbackByUserQuery(userId), ct);
        return Results.Ok(entries);
    }

    private static async Task<IResult> GetFeedbackBySchedule(     // Returns all feedback submitted for the specified schedule.
        Guid scheduleId, IFeedbackRepository feedbackRepo, CancellationToken ct)
    {
        var handler = new GetFeedbackByScheduleHandler(feedbackRepo);
        var entries = await handler.HandleAsync(new GetFeedbackByScheduleQuery(scheduleId), ct);
        return Results.Ok(entries);
    }

    private static async Task<IResult> GetFeedbackStatistics(     // Returns aggregated rating statistics for the specified schedule.
        Guid scheduleId, IFeedbackRepository feedbackRepo, CancellationToken ct)
    {
        var handler = new GetFeedbackStatisticsHandler(feedbackRepo);
        var stats = await handler.HandleAsync(new GetFeedbackStatisticsQuery(scheduleId), ct);
        return Results.Ok(stats);
    }

    private static bool GetAppUserId(HttpContext ctx, out Guid userId)
    {
        var claim = ctx.User.FindFirst("app:userId")?.Value;
        return Guid.TryParse(claim, out userId);
    }
}

public sealed record CreateFeedbackBody(
    Guid BookingId,
    Guid ScheduleId,
    int Rating,
    string? Comment = null,
    FeedbackCategory? Category = null);

public sealed record UpdateFeedbackRequest(
    int Rating, string Comment, FeedbackCategory Category);
