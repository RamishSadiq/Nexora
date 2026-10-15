using System.Linq.Expressions;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;

namespace Nexora.Modules.Membership;

internal static class MembershipListingSorting
{
    private static IOrderedQueryable<T> Order<T, TValue>(IQueryable<T> query, Expression<Func<T, TValue>> value, bool descending)
        => descending ? query.OrderByDescending(value) : query.OrderBy(value);

    public static IOrderedQueryable<MemberSubscription> Members(IQueryable<MemberSubscription> query, MembershipDbContext db, string? sort)
    {
        sort ??= "name";
        var descending = sort.StartsWith('-');
        var key = descending ? sort[1..] : sort;
        Choice(key, "name", "product", "status", "termEnd", "rate");
        return key switch
        {
            "product" => Order(query, x => db.Set<MembershipProduct>().Where(p => p.Id == x.ProductId).Select(p => p.Name).FirstOrDefault(), descending),
            "status" => Order(query, x => x.Status, descending),
            "termEnd" => Order(query, x => x.EndsAtUtc, descending),
            "rate" => descending ? query.OrderByDescending(x => x.Currency).ThenByDescending(x => x.Rate) : query.OrderBy(x => x.Currency).ThenBy(x => x.Rate),
            _ => Order(query, x => x.AccountName ?? x.ContactName, descending)
        };
    }

    public static IOrderedQueryable<MembershipApplication> Applications(IQueryable<MembershipApplication> query, MembershipDbContext db, string? sort)
    {
        sort ??= "-created";
        var descending = sort.StartsWith('-');
        var key = descending ? sort[1..] : sort;
        Choice(key, "name", "product", "status", "rate", "created");
        return key switch
        {
            "product" => Order(query, x => db.Set<MembershipProduct>().Where(p => p.Id == x.ProductId).Select(p => p.Name).FirstOrDefault(), descending),
            "status" => Order(query, x => x.Status, descending),
            "rate" => descending ? query.OrderByDescending(x => x.Currency).ThenByDescending(x => x.QuotedRate) : query.OrderBy(x => x.Currency).ThenBy(x => x.QuotedRate),
            "created" => Order(query, x => x.CreatedAtUtc, descending),
            _ => Order(query, x => x.ContactName, descending)
        };
    }
}
