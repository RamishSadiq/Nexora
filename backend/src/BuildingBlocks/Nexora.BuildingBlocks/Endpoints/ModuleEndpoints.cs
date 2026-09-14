using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Persistence;
namespace Nexora.BuildingBlocks.Endpoints;
public static class ModuleEndpoints
{
    public static RouteGroupBuilder Module(this IEndpointRouteBuilder routes, string name)
    {
        var group = routes.MapGroup("/api/v1/" + name).WithTags(name).RequireAuthorization(name + ".read");
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            try
            {
                if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
                    await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext);
                return await next(context);
            }
            catch (ValidationException e) { return Results.ValidationProblem(new Dictionary<string,string[]> { ["request"] = [e.Message] }); }
            catch (AntiforgeryValidationException) { return Results.Problem(statusCode:400, title:"The security token is invalid or expired."); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (TenantBoundaryException) { return Results.Forbid(); }
            catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode:409, title:"This record changed. Reload before saving."); }
            catch (DbUpdateException) { return Results.Problem(statusCode:409, title:"The change conflicts with existing data."); }
        });
        return group;
    }
    public static string Text(string? text, int max = 160)
    { if (string.IsNullOrWhiteSpace(text) || text.Length > max) throw new ValidationException($"Enter a value of 1–{max} characters."); return text.Trim(); }
    public static void Choice(string value, params string[] choices) { if (!choices.Contains(value)) throw new ValidationException("Unsupported choice."); }
    public static void Check(bool valid, string message) { if (!valid) throw new ValidationException(message); }
    public static IResult Conflict() => Results.Problem(statusCode:409, title:"This record changed. Reload before saving.");
}
