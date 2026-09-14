using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Finance.Domain;
using Nexora.Modules.Finance.Infrastructure;
using Nexora.Modules.Identity;
using System.Net.Http.Json;
using static Nexora.Api.Tests.WorkflowTestClient;
namespace Nexora.Api.Tests;
public sealed class FinanceEndpointTests
{
 [Fact]
 public async Task Invoice_receipt_credit_reversal_refund_and_reconciliation_are_balanced()
 {
  using var app=new IdentityApiFactory();using var c=await Login(app);var customer=await Contact(c);
  var product=await Json(await Send(c,"/api/v1/finance/products",HttpMethod.Post,new{sku="TEST",name="Service",unitPrice=10,currency="GBP",vatBasisPoints=2000}));
  var order=await Json(await Send(c,"/api/v1/finance/orders",HttpMethod.Post,new{customerId=Id(customer),lines=new[]{new{productId=Id(product),quantity=2}}}));Assert.Equal(24,order["total"]!.GetValue<decimal>());
  var invoice=await Json(await Send(c,$"/api/v1/finance/orders/{Id(order)}/post",HttpMethod.Post,new{version=Version(order)}));
  Assert.Equal(HttpStatusCode.Conflict,(await Send(c,$"/api/v1/finance/orders/{Id(order)}/post",HttpMethod.Post,new{version=Version(order)})).StatusCode);
  var receipt=await Json(await Send(c,"/api/v1/finance/receipts",HttpMethod.Post,new{customerId=Id(customer),amount=24,currency="GBP",reference="BANK-1"}));
  var allocation=await Json(await Send(c,"/api/v1/finance/allocations",HttpMethod.Post,new{receiptId=Id(receipt),invoiceId=Id(invoice),amount=24}));
  Assert.Equal(HttpStatusCode.BadRequest,(await Send(c,"/api/v1/finance/allocations",HttpMethod.Post,new{receiptId=Id(receipt),invoiceId=Id(invoice),amount=1})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await Send(c,$"/api/v1/finance/receipts/{Id(receipt)}/refunds",HttpMethod.Post,new{amount=1,reference="NO",reason="Allocated"})).StatusCode);
  await Json(await Send(c,$"/api/v1/finance/invoices/{Id(invoice)}/credits",HttpMethod.Post,new{amount=24,reference="CREDIT-1",reason="Returned"}));
  await Json(await Send(c,$"/api/v1/finance/allocations/{Id(allocation)}/reverse",HttpMethod.Post,new{reason="Release credited funds"}));
  Assert.Equal(HttpStatusCode.BadRequest,(await Send(c,$"/api/v1/finance/allocations/{Id(allocation)}/reverse",HttpMethod.Post,new{reason="Duplicate"})).StatusCode);
  await Json(await Send(c,$"/api/v1/finance/receipts/{Id(receipt)}/refunds",HttpMethod.Post,new{amount=24,reference="REFUND-1",reason="External refund completed"}));
  var detail=await Json(await c.GetAsync($"/api/v1/finance/receipts/{Id(receipt)}"));Assert.Equal(0,detail["available"]!.GetValue<decimal>());
  await Json(await Send(c,$"/api/v1/finance/receipts/{Id(receipt)}/reconcile",HttpMethod.Post,new{version=Version(detail["receipt"]!),statementReference="STATEMENT-1",amount=24,currency="GBP"}));
  Assert.Equal(0,(await Json(await c.GetAsync($"/api/v1/finance/invoices/{Id(invoice)}")))["balance"]!.GetValue<decimal>());
  Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/v1/finance/export")).StatusCode);
 }
 [Fact]
 public async Task Posted_amounts_and_cross_tenant_data_are_protected()
 {
  using var app=new IdentityApiFactory();using var c=await Login(app);using var outside=await Login(app,"outside@nexora.test");var customer=await Contact(c);
  var receipt=await Json(await Send(c,"/api/v1/finance/receipts",HttpMethod.Post,new{customerId=Id(customer),amount=10,currency="GBP",reference="BANK-2"}));
  Assert.Equal(HttpStatusCode.Conflict,(await Send(c,"/api/v1/finance/receipts",HttpMethod.Post,new{customerId=Id(customer),amount=10,currency="GBP",reference="BANK-2"})).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await outside.GetAsync("/api/v1/finance/export")).StatusCode);
  var who=(await c.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;using var scope=app.Services.CreateScope();var options=new DbContextOptionsBuilder<FinanceDbContext>().UseSqlite(scope.ServiceProvider.GetRequiredService<FinanceDbContext>().Database.GetDbConnection()).Options;
  await using(var db=new FinanceDbContext(options,new Identity(who.TenantId,who.UserId))){(await db.Set<Receipt>().SingleAsync()).Amount=999;await Assert.ThrowsAsync<TenantBoundaryException>(()=>db.SaveChangesAsync());}
  await using(var db=new FinanceDbContext(options,new Identity(Guid.NewGuid(),Guid.NewGuid()))){Assert.Empty(await db.Set<Receipt>().ToListAsync());}
 }
 private sealed record Identity(Guid? TenantId,Guid? UserId):IRequestIdentity;
}
