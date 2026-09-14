using System.Net;
using static Nexora.Api.Tests.WorkflowTestClient;
namespace Nexora.Api.Tests;
public sealed class EventsEndpointTests
{
    private static object Offering(string kind="event")=>new{kind,name="Programme",venue="Online",startsAtUtc=DateTime.UtcNow.AddDays(1),endsAtUtc=DateTime.UtcNow.AddDays(2),capacity=1,passingScore=60};
    [Fact]
    public async Task Capacity_waitlist_transfer_and_attendance()
    {
        using var app=new IdentityApiFactory();using var c=await Login(app);var person=await Contact(c);var other=await Contact(c);
        var source=await Json(await Send(c,"/api/v1/events/offerings",HttpMethod.Post,Offering()));var target=await Json(await Send(c,"/api/v1/events/offerings",HttpMethod.Post,Offering()));
        var one=await Json(await Send(c,$"/api/v1/events/offerings/{Id(source)}/enrollments",HttpMethod.Post,new{contactId=Id(person)}));
        var two=await Json(await Send(c,$"/api/v1/events/offerings/{Id(source)}/enrollments",HttpMethod.Post,new{contactId=Id(other)}));Assert.Equal("waitlisted",two["status"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(c,$"/api/v1/events/enrollments/{Id(two)}/promote",HttpMethod.Post,new{version=Version(two),reason="No capacity"})).StatusCode);
        var moved=await Json(await Send(c,$"/api/v1/events/enrollments/{Id(one)}/transfer",HttpMethod.Post,new{version=Version(one),reason="Move booking",targetOfferingId=Id(target)}));Assert.Equal(Id(target),moved["offeringId"]!.GetValue<string>());
        var promoted=await Json(await Send(c,$"/api/v1/events/enrollments/{Id(two)}/promote",HttpMethod.Post,new{version=Version(two),reason="Place opened"}));
        var attended=await Json(await Send(c,$"/api/v1/events/enrollments/{Id(two)}/attend",HttpMethod.Post,new{version=Version(promoted),reason="Checked in"}));Assert.Equal("attended",attended["status"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict,(await Send(c,$"/api/v1/events/enrollments/{Id(two)}/attend",HttpMethod.Post,new{version=Version(promoted),reason="Stale retry"})).StatusCode);
    }
    [Fact]
    public async Task Training_decisions_exam_results_and_foreign_record_checks()
    {
        using var app=new IdentityApiFactory();using var c=await Login(app);var person=await Contact(c);
        var course=await Json(await Send(c,"/api/v1/events/offerings",HttpMethod.Post,Offering("course")));
        var application=await Json(await Send(c,$"/api/v1/events/offerings/{Id(course)}/enrollments",HttpMethod.Post,new{contactId=Id(person)}));Assert.Equal("submitted",application["status"]!.GetValue<string>());
        var approved=await Json(await Send(c,$"/api/v1/events/enrollments/{Id(application)}/approve",HttpMethod.Post,new{version=Version(application),reason="Approved"}));Assert.Equal("registered",approved["status"]!.GetValue<string>());
        var exam=await Json(await Send(c,"/api/v1/events/offerings",HttpMethod.Post,Offering("exam")));
        var booking=await Json(await Send(c,$"/api/v1/events/offerings/{Id(exam)}/enrollments",HttpMethod.Post,new{contactId=Id(person)}));
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(c,$"/api/v1/events/enrollments/{Id(booking)}/result",HttpMethod.Post,new{version=Version(booking),score=80})).StatusCode);
        var attended=await Json(await Send(c,$"/api/v1/events/enrollments/{Id(booking)}/attend",HttpMethod.Post,new{version=Version(booking),reason="Sat exam"}));
        var result=await Json(await Send(c,$"/api/v1/events/enrollments/{Id(booking)}/result",HttpMethod.Post,new{version=Version(attended),score=80}));Assert.Equal("pass",result["outcome"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict,(await Send(c,$"/api/v1/events/enrollments/{Id(booking)}/result",HttpMethod.Post,new{version=Version(attended),score=10})).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await Send(c,$"/api/v1/events/offerings/{Id(exam)}/enrollments",HttpMethod.Post,new{contactId=Guid.NewGuid()})).StatusCode);
        using var outside=await Login(app,"outside@nexora.test");Assert.Equal(HttpStatusCode.Forbidden,(await outside.GetAsync($"/api/v1/events/offerings/{Id(exam)}")).StatusCode);
    }
}
